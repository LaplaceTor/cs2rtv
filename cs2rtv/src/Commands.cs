

using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;

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
                var x = 0;
                _repeattimer = AddTimer(1f, () =>
                {
                    x++;
                    if (x >= 10)
                    {
                        Server.NextFrame(() => StartRtv());
                    }else
                    {
                        foreach (var player in IsPlayer())
                        {
                            PlayClientSound(player, "Alert.WarmupTimeoutBeep");
                            player.PrintToChat("地图投票即将开始");
                        }
                    }

                }, TimerFlags.REPEAT);
            }
        }

        [ConsoleCommand("css_forceext")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/changemap")]
        public void ForceExtCommand(CCSPlayerController? cCSPlayer, CommandInfo command)
        {
            timeleft += 15;
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
                timeleft += 15;
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
            var x = 0;
            _repeattimer = AddTimer(1f, () =>
            {
                x++;
                if (x >= 10)
                {
                        Server.NextFrame(() => StartRtv());
                }else
                {
                    foreach (var player in IsPlayer())
                    {
                        PlayClientSound(player, "Alert.WarmupTimeoutBeep");
                        player.PrintToChat("管理员已强制开始地图投票");
                    }
                }
            }, TimerFlags.REPEAT);
        }

        [ConsoleCommand("css_nominate")]
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
                mapname = maplist.Find(x => Regex.IsMatch(mapname, x) && Regex.IsMatch(x, mapname));
                if (mapname == null)
                {
                    List<string> findmapcache = maplist.Where(x => x.Contains(mapname!)).ToList();
                    var randommap = findmapcache.FirstOrDefault();
                    command.ReplyToCommand($"你是否在寻找 {randommap}");
                    return;
                }
                else
                {
                    if (mapnominatelist.Find(x => Regex.IsMatch(mapname, x) && Regex.IsMatch(x, mapname)) != null)
                    {
                        command.ReplyToCommand($"地图 {mapname} 已被他人预定");
                        return;
                    }
                    else if (mapname == Server.MapName)
                    {
                        command.ReplyToCommand($"地图 {mapname} 为当前地图");
                        return;
                    }
                    else if (mapcooldown.Find(x => Regex.IsMatch(mapname, x) && Regex.IsMatch(x, mapname)) != null)
                    {
                        command.ReplyToCommand($"地图 {mapname} 最近已经游玩过了");
                        return;
                    }
                }
                mapnominatelist.Add(mapname);
                Server.PrintToChatAll($"{cCSPlayer!.PlayerName} 预定了地图 {mapname}");
            }
            else
            {
                command.ReplyToCommand($"打开控制台查看地图列表");
                for(var x = 0; x<maplist.Count -1; x++)
                    cCSPlayer!.PrintToConsole($"{maplist[x]}");
            }
        }
    }
}