using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
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
    private List<ulong> extcount = new();
    private List<string> votemaplist = new();
    private List<string> mapcooldown = new();
    private bool canrtv = false;
    private bool firstmaprandom = false;
    private bool isrtving = false;
    private bool isforcertv = false;
    private bool isrtv = false;
    private bool rtvwin = false;
    private bool isrtvagain = false;
    private bool isext = false;
    private int playercount = 0;
    private int rtvrequired = 0;
    private Timer? _canrtvtimer;
    private Timer? _maptimer;
    private Timer? _rtvtimer;

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("load maplist from {Path}", Path.Join(ModuleDirectory, "maplist.txt"));
        maplist = new List<string>(File.ReadAllLines(Path.Join(ModuleDirectory, "maplist.txt")));
        mapcooldown.Add(Server.MapName);

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (rtvcount.Contains(@event.Userid!.SteamID))
                rtvcount.Remove(@event.Userid.SteamID);
            if (extcount.Contains(@event.Userid!.SteamID))
                extcount.Remove(@event.Userid.SteamID);
            GetPlayersCount();
            if (rtvcount.Count >= rtvrequired && playercount != 0)
            {
                isrtving = true;
                isrtv = true;
                Server.PrintToChatAll("地图投票进行中");
                rtvcount.Clear();
                StartRtv();
            }
            if (extcount.Count >= rtvrequired && playercount != 0)
            {
                isext = true;
                Server.PrintToChatAll("地图已延长");
                extcount.Clear();
            }
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnMapStart>(OnMapStart =>
        {
            if(!hotReload  && !firstmaprandom)
            {
                if(!Server.MapName.Contains("de"))
                {
                    Server.NextFrame(()=>
                    {
                        firstmaprandom = true;
                        Random random = new();
                        int index = random.Next(0, maplist.Count - 1);
                        var randommap = maplist[index];
                        if(randommap == Server.MapName)
                            return;
                        Server.ExecuteCommand($"ds_workshop_changelevel {randommap}");
                    });
                    return;
                }
            }
            Server.NextFrame(() =>
            {
                mapcooldown.Add(Server.MapName);
                if (mapcooldown.Count > 5)
                    mapcooldown.Remove(mapcooldown.First());

                rtvwin = false;
                rtvcount.Clear();
                extcount.Clear();
                mapnominatelist.Clear();
                votemaplist.Clear();
                isrtving = false;
                isrtvagain = false;
                isforcertv = false;
                canrtv = false;
                KillTimer();
                _canrtvtimer = AddTimer(5 * 60f, () =>
                {
                    canrtv = true;
                });
                _maptimer = AddTimer(25 * 60f, () =>
                {
                    isrtving = true;
                    if (!isext)
                        Server.PrintToChatAll("当前地图时长还剩5分钟");
                    StartRtv();
                });
            });
        });
    }

    [ConsoleCommand("css_rtv")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void RtvCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if (!canrtv)
        {
            command.ReplyToCommand("投票冷却中。。。");
            return;
        }
        if (isrtving)
        {
            command.ReplyToCommand("投票已在进行中");
            return;
        }
        GetPlayersCount();

        if (rtvcount.Contains(cCSPlayer!.SteamID))
        {
            Server.PrintToChatAll($"{cCSPlayer.PlayerName} 已投票更换地图，当前 {rtvcount.Count}/{rtvrequired}");
            return;
        }
        rtvcount.Add(cCSPlayer.SteamID);
        if (rtvcount.Count < rtvrequired)
        {
            Server.PrintToChatAll($"{cCSPlayer.PlayerName} 已投票更换地图，当前 {rtvcount.Count}/{rtvrequired}");
        }
        else
        {
            isrtving = true;
            isrtv = true;
            Server.PrintToChatAll("地图投票进行中");
            rtvcount.Clear();
            StartRtv();
        }
    }
    
    [ConsoleCommand("css_ext")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void ExtCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if (!canrtv)
        {
            command.ReplyToCommand("投票冷却中。。。");
            return;
        }
        if (isrtving)
        {
            command.ReplyToCommand("投票已在进行中");
            return;
        }
        if (isext)
        {
            command.ReplyToCommand("短时间内只能延长一次哦，下次投票再看看要不要延长吧");
            return;
        }
        GetPlayersCount();
        if (extcount.Contains(cCSPlayer!.SteamID))
        {
            Server.PrintToChatAll($"{cCSPlayer.PlayerName} 已投票延长地图，当前 {extcount.Count}/{rtvrequired}");
            return;
        }
        extcount.Add(cCSPlayer.SteamID);
        if (extcount.Count < rtvrequired)
        {
            Server.PrintToChatAll($"{cCSPlayer.PlayerName} 已投票延长地图，当前 {extcount.Count}/{rtvrequired}");
        }
        else
        {
            isext = true;
            Server.PrintToChatAll("地图已延长");
            extcount.Clear();
        }
    }

    [ConsoleCommand("css_forcertv")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ForceRtvCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if (isrtving)
        {
            command.ReplyToCommand("投票已在进行中");
            return;
        }
        isrtving = true;
        isrtv = true;
        isforcertv = true;
        Server.PrintToChatAll($"管理员已强制开始地图投票");
        StartRtv();
    }

    [ConsoleCommand("css_nominate")]
    [CommandHelper(minArgs: 1, usage: "[mapname]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void NominateCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if (isrtving)
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
            mapname = maplist.Find(x => Regex.IsMatch(mapname, x));
            if (mapnominatelist.Find(x => Regex.IsMatch(mapname!, x)) != null)
            {
                command.ReplyToCommand($"地图 {mapname} 已被他人预定");
                return;
            }
            else if (mapname == Server.MapName)
            {
                command.ReplyToCommand($"地图 {mapname} 为当前地图");
                return;
            }
            else if (mapcooldown.Find(x => Regex.IsMatch(mapname!, x)) != null)
            {
                command.ReplyToCommand($"地图 {mapname} 最近已经游玩过了");
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
            List<string> findmapcache = maplist.Where(x => x.Contains(mapname)).ToList();
            var randommap = findmapcache.FirstOrDefault();
            command.ReplyToCommand($"你是否在寻找 {randommap}");
        }
    }


    public void StartRtv()
    {
        if (isext)
        {
            rtvwin = true;
            VoteEnd(Server.MapName);
            isext = false;
            return;
        }
        Logger.LogInformation("开始投票换图");
        Random random = new();
        GetPlayersCount();
        if (playercount == 0)
        {
            isrtv = true;
            isforcertv = true;
            string? randommap = null;
            while (randommap == null)
            {
                int index = random.Next(0, maplist.Count - 1);
                if (mapcooldown.Find(x => Regex.IsMatch(maplist[index], x)) != null) continue;
                else randommap = maplist[index];
            }
            rtvwin = true;
            Logger.LogInformation("空服换图");
            VoteEnd(randommap);
            return;
        }
        if (!isrtvagain)
        {
            votemaplist = mapnominatelist;
            if (!isforcertv)
                votemaplist.Add(Server.MapName);
            while (votemaplist.Count < 6)
            {
                int index = random.Next(0, maplist.Count - 1);
                if (votemaplist.Find(x => Regex.IsMatch(maplist[index], x)) != null || mapcooldown.Find(x => Regex.IsMatch(maplist[index], x)) != null) continue;
                votemaplist.Add(maplist[index]);
            }
        }
        ChatMenu votemenu = new ChatMenu("请从以下地图中选择一张");
        string nextmap = "";
        int totalvotes = 0;
        Dictionary<string, int> votes = new();
        votes.Clear();

        foreach (string mapname in votemaplist)
        {
            votes[mapname] = 0;
            if (mapname == Server.MapName)
            {
                votemenu.AddMenuOption("不更换地图", (player, options) =>
                {
                    votes[mapname] += 1;
                    totalvotes += 1;
                    player.PrintToChat($"你已投票给不更换地图");
                    Logger.LogInformation("{PlayerName} 投票给不换图", player.PlayerName);
                    GetPlayersCount();
                    if (votes[mapname] > rtvrequired)
                    {
                        nextmap = mapname;
                        rtvwin = true;
                        Server.PrintToChatAll($"地图投票已结束");
                        VoteEnd(nextmap);
                        return;
                    }
                });
            }
            else
            {
                votemenu.AddMenuOption(mapname, (player, options) =>
                {
                    votes[mapname] += 1;
                    totalvotes += 1;
                    player.PrintToChat($"你已投票给地图 {mapname}");
                    Logger.LogInformation("{PlayerName} 投票给地图 {mapname}", player.PlayerName, mapname);
                    GetPlayersCount();
                    if (votes[mapname] > rtvrequired)
                    {
                        nextmap = mapname;
                        rtvwin = true;
                        Server.PrintToChatAll($"地图投票已结束");
                        VoteEnd(nextmap);
                        return;
                    }
                });
            }
        }

        foreach (CCSPlayerController? player in Utilities.GetPlayers().Where((x) =>
            x.TeamNum > 0 &&
            x.IsValid &&
            x.Connected == PlayerConnectedState.PlayerConnected))
        {
            MenuManager.OpenChatMenu(player, votemenu);
        }

        _rtvtimer = AddTimer(30f, () =>
        {
            if (!isrtving) return;
            if (totalvotes == 0)
            {
                nextmap = votemaplist[random.Next(0, votemaplist.Count - 1)];
                Server.PrintToChatAll($"地图投票已结束");
                rtvwin = true;
            }
            else if (votes.Select(x => x.Value).Max() > (totalvotes * 0.5f))
            {
                int winnervotes = votes.Select(x => x.Value).Max();
                IEnumerable<KeyValuePair<string, int>> winner = votes.Where(x => x.Value == winnervotes);
                nextmap = winner.ElementAt(0).Key;
                Server.PrintToChatAll($"地图投票已结束");
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
                Server.PrintToChatAll($"地图投票已结束");
                rtvwin = true;
            }
            VoteEnd(nextmap);
        });
    }

    public void VoteEnd(string mapname)
    {

        if (rtvwin)
        {
            rtvwin = false;
            if (!isext)
            {
                rtvcount.Clear();
                canrtv = false;
            }
            votemaplist.Clear();
            isrtving = false;
            isrtvagain = false;
            isforcertv = false;

            if (_rtvtimer != null)
            {
                _rtvtimer.Kill();
                _rtvtimer = null;
            }

            if (mapname == Server.MapName)
            {
                if (!isext)
                {
                    Server.PrintToChatAll($"地图已延长");
                    Logger.LogInformation("地图已延长");
                    Server.NextFrame(() =>
                    {
                        _canrtvtimer = AddTimer(5 * 60f, () =>
                    {
                        canrtv = true;
                    });
                    });
                }
                if (!isrtv)
                {
                    Server.NextFrame(() =>
                    {
                        _maptimer = AddTimer(25 * 60f, () =>
                        {
                            isrtving = true;
                            Server.PrintToChatAll("当前地图时长还剩5分钟");
                            StartRtv();
                        });
                    });
                }
                else
                    isrtv = false;
                return;
            }
            mapnominatelist.Clear();
            Logger.LogInformation("投票决定为 {mapname}", mapname);
            if (!isrtv)
            {
                Server.PrintToChatAll($"5分钟后将更换为地图 {mapname}");
                AddTimer(240f, () =>
                {
                    Server.PrintToChatAll("距离换图还有60s");
                });
                AddTimer(270f, () =>
                {
                    Server.PrintToChatAll("距离换图还有30s");
                });
                AddTimer(290f, () =>
                {
                    Server.PrintToChatAll("距离换图还有10s");
                });
                _canrtvtimer = AddTimer(5 * 60f, () =>
        {
            canrtv = true;
            Server.PrintToChatAll($"正在更换为地图 {mapname}");
            Server.ExecuteCommand($"ds_workshop_changelevel {mapname}");
        });
            }
            else
            {
                isrtv = false;
                canrtv = true;
                Server.PrintToChatAll($"正在更换为地图 {mapname}");
                Server.ExecuteCommand($"ds_workshop_changelevel {mapname}");
            }
        }
        else
        {
            isrtvagain = true;
            StartRtv();
        }
    }

    private void KillTimer()
    {
        if (_canrtvtimer != null)
            _canrtvtimer.Kill();
        if (_maptimer != null)
            _maptimer.Kill();
        if (_rtvtimer != null)
            _rtvtimer.Kill();
    }
    private void GetPlayersCount()
    {
        playercount = Utilities.GetPlayers().Where((x) =>
        x.TeamNum > 0 &&
        x.IsValid &&
        x.Connected == PlayerConnectedState.PlayerConnected
        ).Count();

        rtvrequired = (int)Math.Ceiling(playercount * 0.6f);
    }
}
