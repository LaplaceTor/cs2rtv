using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;

namespace cs2rtv;

public class Cs2rtv : BasePlugin
{
    public override string ModuleAuthor => "lapl";
    public override string ModuleName => "CS2 RTV LITE";
    public override string ModuleVersion => "0.1.0";
    private List<string> maplist = new();
    private List<string> mapnominatelist = new();
    private List<ulong> rtvcount = new();
    private bool isrtv = false;
    private bool rtvwin = false;
    private int GetPlayersCount()
    {
        var count = Utilities.GetPlayers().Where((x) =>
            x.IsValid &&
            x.Connected == PlayerConnectedState.PlayerConnected &&
            x.TeamNum > 1
            ).Count();
        return count;
    }
    public override void Load(bool hotReload)
    {
        Logger.LogInformation("load maplist from {Path}",Path.Join(ModuleDirectory,"maplist.txt"));
        maplist = new List<string>(File.ReadAllLines(Path.Join(ModuleDirectory,"maplist.txt")));
    }

    [ConsoleCommand("css_rtv")]
    [CommandHelper(whoCanExecute:CommandUsage.CLIENT_ONLY)]
    public void RtvCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if(isrtv)
        {
            command.ReplyToCommand("投票已在进行中");
        }else{
            var rtvrequired = (int)Math.Floor(GetPlayersCount()*0.6f);
            if(rtvcount.Contains(cCSPlayer!.SteamID))
            {
                command.ReplyToCommand($"你已投票，当前 {rtvcount.Count}/{rtvrequired}");
            }else{
                rtvcount.Add(cCSPlayer.SteamID);
                if(rtvcount.Count < rtvrequired)
                {
                    command.ReplyToCommand($"你已投票，当前 {rtvcount.Count}/{rtvrequired}");
                }else{
                    isrtv = true;
                    Server.PrintToChatAll("地图投票进行中");
                    StartRtv();
                }
            }
        }
    }

    [ConsoleCommand("css_forcertv")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ForceRtvCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if(isrtv)
        {
            command.ReplyToCommand("投票已在进行中");
        }else{
            isrtv = true;
            Server.PrintToChatAll($"管理员已强制开始地图投票");
            StartRtv();
        }
    }

    [ConsoleCommand("css_nominate")]
    [CommandHelper(minArgs:1, usage: "[mapname]",whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void NominateCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
    {
        if(isrtv)
        {
            command.ReplyToCommand("投票已在进行中");
        }else if(mapnominatelist.Count>=5)
        {
            command.ReplyToCommand("当前预定地图已满");
        }else{
            var mapname = command.GetArg(1);
            if(maplist.Contains(mapname) && (!Regex.IsMatch(mapname, @"\bsurf_\b") || !Regex.IsMatch(mapname, @"\bbhop_\b") || !Regex.IsMatch(mapname, @"\bkz_\b")))
            {
                if(mapname.Contains("surf_") || mapname.Contains("bhop_") || mapname.Contains("kz_"))
                {
                    mapname = maplist.Find (x => x.Contains(mapname));
                    if(mapnominatelist.Contains(mapname!))
                    {
                        command.ReplyToCommand($"地图 {mapname} 已被他人预定");
                        return;
                    }else if(mapname == Server.MapName){
                        command.ReplyToCommand($"地图 {mapname} 为当前地图");
                        return;
                    }
                }else{
                    command.ReplyToCommand("请输入完整的地图名称如 surf_ace bhop_leaf kz_aaaa");
                    return;
                }
                mapnominatelist.Add(mapname!);
                Server.PrintToChatAll($"{cCSPlayer!.PlayerName} 预定了地图 {mapname}");
            }else{
               command.ReplyToCommand($"地图 {mapname} 不存在");
            }
        }
    }

    public void StartRtv()
    {
        var random = new Random();
        var votemaplist = new List<string>(mapnominatelist);
        while(votemaplist.Count < 5)
        {
            var index = random.Next(0,maplist.Count - 1);
            if(Server.MapName.Contains(maplist[index]) || mapnominatelist.Contains(maplist[index])) continue;
            votemaplist.Add(maplist[index]);
        }
        
        var votemenu = new ChatMenu("请从以下地图中选择一张");
        var nextmap = "";
        var totalvotes = 0;
        Dictionary<string, int> votes = new();
        
        var votestep = 0;
        while(!rtvwin){
            votes.Clear();
            totalvotes = 0;
            foreach(var mapname in votemaplist)
            {
                votemenu.AddMenuOption(mapname,(player,options) =>
                {
                    votes[mapname] += 1;
                    totalvotes += 1;
                    player.PrintToChat($"你已投票给地图 {mapname}");
                    if (votes[mapname] > (GetPlayersCount() * 0.5f))
                    {
                        nextmap = mapname;
                        Server.PrintToChatAll($"地图投票已结束，正在更换为地图 {nextmap}");
                        VoteEndMapChange(nextmap);
                    }
                });
            }

            foreach(var player in Utilities.GetPlayers().Where((x) =>
                    x.IsValid &&
                    x.Connected == PlayerConnectedState.PlayerConnected &&
                    x.TeamNum > 1
                    )){
                    MenuManager.OpenChatMenu(player,votemenu);
            }

            AddTimer(30f,()=>
            {
                if(totalvotes == 0){
                    nextmap = votemaplist[random.Next(0,votemaplist.Count-1)];
                    Server.PrintToChatAll($"地图投票已结束，正在更换为地图 {nextmap}");
                    rtvwin = true;
                }else if(votes.Select(x=>x.Value).Max()>totalvotes*0.5f){
                    var winnervotes = votes.Select(x=>x.Value).Max();
                    IEnumerable<KeyValuePair<string, int>> winner = votes.Where(x => x.Value == winnervotes);
                    nextmap = winner.ElementAt(0).Key;
                    Server.PrintToChatAll($"地图投票已结束，正在更换为地图 {nextmap}");
                    rtvwin = true;
                }else if(votes.Select(x=>x.Value).Max()<=totalvotes*0.5f && votestep < 2){
                    Server.PrintToChatAll("本轮投票未有地图投票比例超过50%，将进行下一轮投票");
                    votestep++;
                    votemaplist.Clear();
                    votes = votes.OrderByDescending(x => x.Value).ToDictionary(x=>x.Key,y=>y.Value);
                    votemaplist.Add(votes.ElementAt(0).Key);
                    votemaplist.Add(votes.ElementAt(1).Key);
                }else if(votes.Select(x=>x.Value).Max()<=totalvotes*0.5f && votestep >= 2){
                    nextmap = votemaplist[random.Next(0,votemaplist.Count-1)];
                    Server.PrintToChatAll($"地图投票已结束，正在更换为地图 {nextmap}");
                    rtvwin = true;
                }
            },TimerFlags.STOP_ON_MAPCHANGE);
        }
        VoteEndMapChange(nextmap);
    }

    public void VoteEndMapChange(string mapname)
    {
        rtvcount.Clear();
        mapnominatelist.Clear();
        isrtv = false;
        rtvwin = false;
        Server.ExecuteCommand($"ds_workshop_changelevel {mapname}");
    }
}