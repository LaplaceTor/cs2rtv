using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace cs2rtv
{
    public partial class Cs2rtv
    {
        [ConsoleCommand("css_timeleft")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void TimeleftCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            if (cCSPlayer != null)
                cCSPlayer.PrintToChat($"当前地图还剩余 {timeleft} 分钟");
            else
                Server.PrintToConsole($"当前地图还剩余 {timeleft} 分钟");
        }

        [ConsoleCommand("css_stopsound")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void StopSoundCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            PlayClientSound(cCSPlayer!, "StopSoundEvents.StopAllMusic");
        }

        [ConsoleCommand("css_maplistreload")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/changemap")]
        public void ReloadMaplistCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            maplist = new List<string>(File.ReadAllLines(Path.Join(ModuleDirectory, "maplist.txt")));
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
                rtvcount.Clear();
                RepeatBroadcast(10,1f,"地图投票即将开始");
            }
        }

        [ConsoleCommand("css_forceext")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/changemap")]
        public void ForceExtCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            timeleft += 30;
            Server.PrintToChatAll("管理员已延长地图");
        }

        [ConsoleCommand("css_nextmap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/changemap")]
        public void NextMapCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            if(nextmappass)
                cCSPlayer!.PrintToChat($"下一张地图为{nextmapname}");
            else
                cCSPlayer!.PrintToChat("还未决定下一张地图");
        }

        [ConsoleCommand("css_ext")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ExtCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            if (isrtving)
            {
                command.ReplyToCommand("投票已在进行中");
                return;
            }
            if (extround >= 3)
            {
                command.ReplyToCommand("已达到延长命令上限，请在下次正常投票过程中决定是否延长");
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
                Server.PrintToChatAll("地图已延长");
                timeleft += 30;
                extround++;
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
            RepeatBroadcast(10,1f,"管理员已强制开始地图投票");
        }

        [ConsoleCommand("css_map")]
        [CommandHelper(minArgs: 1, usage: "[mapname]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/changemap")]
        public void ChangeMapCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            string? mapname = command.GetArg(1);
            Server.ExecuteCommand($"ds_workshop_changelevel {mapname}");
            Server.ExecuteCommand($"host_workshop_map {mapname}");
        }


        [ConsoleCommand("css_yd")]
        [CommandHelper(minArgs: 0, usage: "[mapname]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
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
            var findmapname = "";
            if (maplist.Contains(mapname.ToLower()))
            {
                
                List<string> findmapcache = maplist.Where(x => x.Contains(mapname.ToLower())).ToList();
                if(findmapcache.Count == 1 || findmapcache.First() == mapname)
                    findmapname = findmapcache.First();
                else
                {
                    var randommap = findmapcache.First();
                    command.ReplyToCommand($"你是否在寻找 {randommap}");
                    return;
                }
            }
            else
            {
                command.ReplyToCommand($"未找到地图{mapname},打开控制台输入 css_maplist 查看服务器地图列表");
                return;
            }

            if (mapnominatelist.Find(x=> x == findmapname) != null)
            {
                command.ReplyToCommand($"地图 {findmapname} 已被他人预定");
                return;
            }
            else if (findmapname == Server.MapName)
            {
                command.ReplyToCommand($"地图 {findmapname} 为当前地图");
                return;
            }
            else if (mapcooldown.Find(x=> x == findmapname) != null)
            {
                command.ReplyToCommand($"地图 {findmapname} 最近已经游玩过了");
                return;
            }
            mapnominatelist.Add(findmapname);
            Server.PrintToChatAll($"{cCSPlayer!.PlayerName} 预定了地图 {findmapname}");
        }

        [ConsoleCommand("css_maplist")]
        [CommandHelper(minArgs: 1, usage: "[number]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void MapListCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            var x = maplist.Count /10;
            var y = maplist.Count - (x * 10);
            var z = 1;

            if(command.GetArg(1) != null)
            {
                if(Int32.TryParse(command.GetArg(1),out int numValue))
                    z = numValue;
            }
            else
            {
                cCSPlayer!.PrintToConsole("请正确输入数字如 css_maplist 1");
                return;
            }

            if(z-1 > x || z <= 0)
            {
                cCSPlayer!.PrintToConsole("输入的数字超出当前服务器地图池范围");
                return;
            }
            
            if(z-1 > 0)
                cCSPlayer!.PrintToConsole($"输入 css_maplist {z-1} 查看上一组列表");
            for(var i=0; i <10; i++)
            {
                if(z == x && i >= y) break;
                cCSPlayer!.PrintToConsole(maplist[(z-1)*10+i]);
            }
            if(z-1 < x)
                cCSPlayer!.PrintToConsole($"输入 css_maplist {z+1} 查看下一组列表");
        }
    }
}