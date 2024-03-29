﻿using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace cs2rtv;

public class Cs2rtv : BasePlugin
{
    public override string ModuleAuthor => "lapl";
    public override string ModuleName => "CS2 RTV LITE";
    public override string ModuleVersion => "0.1.0";
    private List<string> maplist = new();
    private List<string> mapnominatelist = new();
    private List<ulong> rtvcount = new();
    private List<string> votemaplist = new();
    private bool isrtv = false;
    private bool rtvwin = false;
    private bool isrtvagain = false;
    private int playercount = 0;
    public override void Load(bool hotReload)
    {
        Logger.LogInformation("load maplist from {Path}", Path.Join(ModuleDirectory, "maplist.txt"));
        maplist = new List<string>(File.ReadAllLines(Path.Join(ModuleDirectory, "maplist.txt")));
        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (rtvcount.Contains(@event.Userid.SteamID))
                rtvcount.Remove(@event.Userid.SteamID);
            return HookResult.Continue;
        });
    }

    [ConsoleCommand("css_rtv")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void RtvCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if (isrtv)
        {
            command.ReplyToCommand("投票已在进行中");
            return;
        }
        GetPlayersCount();
        int rtvrequired = (int)Math.Ceiling(playercount * 0.6f);
        if (rtvcount.Contains(cCSPlayer!.SteamID))
        {
            command.ReplyToCommand($"你已投票更换地图，当前 {rtvcount.Count}/{rtvrequired}");
            return;
        }
        rtvcount.Add(cCSPlayer.SteamID);
        if (rtvcount.Count < rtvrequired)
        {
            command.ReplyToCommand($"你已投票更换地图，当前 {rtvcount.Count}/{rtvrequired}");
        }
        else
        {
            isrtv = true;
            Server.PrintToChatAll("地图投票进行中");
            StartRtv();
        }
    }


    [ConsoleCommand("css_forcertv")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ForceRtvCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if (isrtv)
        {
            command.ReplyToCommand("投票已在进行中");
            return;
        }
        isrtv = true;
        Server.PrintToChatAll($"管理员已强制开始地图投票");
        StartRtv();
    }

    [ConsoleCommand("css_nominate")]
    [CommandHelper(minArgs: 1, usage: "[mapname]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void NominateCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if (isrtv)
        {
            command.ReplyToCommand("投票已在进行中");
            return;
        }
        if (mapnominatelist.Count >= 5)
        {
            command.ReplyToCommand("当前预定地图已满");
            return;
        }
        string? mapname = command.GetArg(1);

        List<string> blocklist = new List<string>
        {
            "surf","surf_","bhop","bhop_","kz","kz_"
        };
        foreach (string bug in blocklist)
        {
            if (Regex.IsMatch(mapname, @$"\b{bug}\b"))
            {
                command.ReplyToCommand($"你输入的字段太少，无法查到符合条件的地图");
                return;
            }
        }

        if (maplist.Contains(mapname) && mapname.Length > 2)
        {
            mapname = maplist.Find(x => x.Contains(mapname));
            if (mapnominatelist.Contains(mapname!))
            {
                command.ReplyToCommand($"地图 {mapname} 已被他人预定");
                return;
            }
            else if (mapname == Server.MapName)
            {
                command.ReplyToCommand($"地图 {mapname} 为当前地图");
                return;
            }
            mapnominatelist.Add(mapname!);
            Server.PrintToChatAll($"{cCSPlayer!.PlayerName} 预定了地图 {mapname}");
        }
        else if (mapname.Length <= 2)
        {
            command.ReplyToCommand($"你输入的字段太少，无法查到符合条件的地图");
        }
        else
        {
            command.ReplyToCommand($"地图 {mapname} 不存在,地图列表已输出到控制台");
            foreach (string map in maplist)
                cCSPlayer!.PrintToConsole($"{map}");
        }
    }


    public void StartRtv()
    {
        Random random = new();
        if (!isrtvagain)
        {
            votemaplist = mapnominatelist;
            while (votemaplist.Count < 5)
            {
                int index = random.Next(0, maplist.Count - 1);
                if (Server.MapName.Contains(maplist[index]) || mapnominatelist.Contains(maplist[index])) continue;
                votemaplist.Add(maplist[index]);
            }
        }
        ChatMenu votemenu = new ChatMenu("请从以下地图中选择一张");
        string nextmap = "";
        int totalvotes = 0;
        Dictionary<string, int> votes = new();
        votes.Clear();
        totalvotes = 0;
        foreach (string mapname in votemaplist)
        {
            votes[mapname] = 0;
            votemenu.AddMenuOption(mapname, (player, options) =>
            {
                Server.PrintToConsole("创建投票列表中");
                votes[mapname] += 1;
                totalvotes += 1;
                player.PrintToChat($"你已投票给地图 {mapname}");
                GetPlayersCount();
                if (votes[mapname] > playercount * 0.5f)
                {
                    nextmap = mapname;
                    rtvwin = true;
                    Server.PrintToChatAll($"地图投票已结束，正在更换为地图 {nextmap}");
                    VoteEnd(nextmap);
                    return;
                }
            });
        }

        foreach (CCSPlayerController? player in Utilities.GetPlayers().Where((x) =>
            x.TeamNum > 0 &&
            x.IsValid &&
            x.Connected == PlayerConnectedState.PlayerConnected))
        {
            MenuManager.OpenChatMenu(player, votemenu);
        }
        Timer votetimer = AddTimer(30f, () =>
        {
            if (totalvotes == 0)
            {
                nextmap = votemaplist[random.Next(0, votemaplist.Count - 1)];
                Server.PrintToChatAll($"地图投票已结束，正在更换为地图 {nextmap}");
                rtvwin = true;
            }
            else if (votes.Select(x => x.Value).Max() > (totalvotes * 0.5f))
            {
                int winnervotes = votes.Select(x => x.Value).Max();
                IEnumerable<KeyValuePair<string, int>> winner = votes.Where(x => x.Value == winnervotes);
                nextmap = winner.ElementAt(0).Key;
                Server.PrintToChatAll($"地图投票已结束，正在更换为地图 {nextmap}");
                rtvwin = true;
            }
            else if (votes.Select(x => x.Value).Max() <= (totalvotes * 0.5f) && votemaplist.Count >= 4 && totalvotes > 2)
            {
                Server.PrintToChatAll("本轮投票未有地图投票比例超过50%，将进行下一轮投票");
                votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
                votemaplist.Clear();
                for (int x = 0; x < (votes.Count / 2); x++)
                    votemaplist!.Add(votes.ElementAt(x).Key);
            }
            else if (votes.Select(x => x.Value).Max() <= (totalvotes * 0.5f) && (votemaplist.Count < 4 || totalvotes <= 2))
            {
                nextmap = votemaplist[random.Next(0, votemaplist.Count - 1)];
                Server.PrintToChatAll($"地图投票已结束，正在更换为地图 {nextmap}");
                rtvwin = true;
            }
            VoteEnd(nextmap);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    public void VoteEnd(string mapname)
    {

        if (rtvwin)
        {
            rtvwin = false;
            rtvcount.Clear();
            mapnominatelist.Clear();
            votemaplist.Clear();
            isrtv = false;
            isrtvagain = false;
            Server.ExecuteCommand($"ds_workshop_changelevel {mapname}");
        }
        else
        {
            isrtvagain = true;
            StartRtv();
        }
    }

    private void GetPlayersCount()
    {
        playercount = Utilities.GetPlayers().Where((x) =>
        x.TeamNum > 0 &&
        x.IsValid &&
        x.Connected == PlayerConnectedState.PlayerConnected
        ).Count();
    }
}
