using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace Cs2Rtv;

public partial class Cs2Rtv {
    [ConsoleCommand("css_timeleft")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void TimeLeftCommand(CCSPlayerController? controller, CommandInfo command) {
        if (controller != null) {
            controller.PrintToChat($"当前地图还剩余 {timeLeft} 分钟");
        } else {
            Server.PrintToConsole($"当前地图还剩余 {timeLeft} 分钟");
        }
    }

    [ConsoleCommand("css_stopsound")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void StopSoundCommand(CCSPlayerController? controller, CommandInfo command) {
        PlayClientSound(controller!, "StopSoundEvents.StopAllMusic");
    }

    [ConsoleCommand("css_maplistreload")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ReloadMaplistCommand(CCSPlayerController? controller, CommandInfo command) {
        mapList = loadMaps(new StreamReader(new FileStream(Path.Join(ModuleDirectory, "maplist.txt"), FileMode.Open)));
    }

    [ConsoleCommand("css_rtv")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void RtvCommand(CCSPlayerController? controller, CommandInfo command) {
        if (!canRtv) {
            command.ReplyToCommand("投票冷却中。。。");
            return;
        }

        if (isRtving) {
            command.ReplyToCommand("投票已在进行中");
            return;
        }

        GetPlayersCount();

        if (rtvCount.Contains(controller!.SteamID)) {
            Server.PrintToChatAll($"{controller.PlayerName} 已投票更换地图，当前 {rtvCount.Count}/{rtvRequired}");
            return;
        }

        rtvCount.Add(controller.SteamID);
        if (rtvCount.Count < rtvRequired) {
            Server.PrintToChatAll($"{controller.PlayerName} 已投票更换地图，当前 {rtvCount.Count}/{rtvRequired}");
        } else {
            isRtving = true;
            isRtv = true;
            rtvCount.Clear();
            RepeatBroadcast(10, 1f, "地图投票即将开始");
        }
    }

    [ConsoleCommand("css_forceext")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ForceExtCommand(CCSPlayerController? controller, CommandInfo command) {
        timeLeft += 30;
        Server.PrintToChatAll("管理员已延长地图");
    }

    [ConsoleCommand("css_nextmap")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/changemap")]
    public void NextMapCommand(CCSPlayerController? controller, CommandInfo command) {
        controller!.PrintToChat(nextMapPass ? $"下一张地图为{nextMap?.name}" : "还未决定下一张地图");
    }

    [ConsoleCommand("css_ext")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void ExtCommand(CCSPlayerController? controller, CommandInfo command) {
        if (isRtving) {
            command.ReplyToCommand("投票已在进行中");
            return;
        }

        if (extRound >= 3) {
            command.ReplyToCommand("已达到延长命令上限，请在下次正常投票过程中决定是否延长");
            return;
        }

        GetPlayersCount();
        if (extCount.Contains(controller!.SteamID)) {
            Server.PrintToChatAll($"{controller.PlayerName} 已投票延长地图，当前 {extCount.Count}/{rtvRequired}");
            return;
        }

        extCount.Add(controller.SteamID);
        if (extCount.Count < rtvRequired) {
            Server.PrintToChatAll($"{controller.PlayerName} 已投票延长地图，当前 {extCount.Count}/{rtvRequired}");
        } else {
            Server.PrintToChatAll("地图已延长");
            timeLeft += 30;
            extRound++;
            extCount.Clear();
        }
    }

    [ConsoleCommand("css_forcertv")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ForceRtvCommand(CCSPlayerController? controller, CommandInfo command) {
        if (isRtving) {
            command.ReplyToCommand("投票已在进行中");
            return;
        }

        isRtving = true;
        isRtv = true;
        RepeatBroadcast(10, 1f, "管理员已强制开始地图投票");
    }

    [ConsoleCommand("css_map")]
    [CommandHelper(minArgs: 1, usage: "[mapId]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ChangeMapCommand(CCSPlayerController? controller, CommandInfo command) {
        var mapId = int.Parse(command.GetArg(1));
        Server.ExecuteCommand($"host_workshop_map {mapId}");
    }


    [ConsoleCommand("css_yd")]
    [CommandHelper(minArgs: 0, usage: "[mapName]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void NominateCommand(CCSPlayerController? controller, CommandInfo command) {
        if (isRtving) {
            command.ReplyToCommand("投票已在进行中");
            return;
        }

        if (mapNominateList.Count >= 5) {
            command.ReplyToCommand("当前预定地图已满");
            return;
        }

        var mapName = command.GetArg(1);
        Map findMap;
        if (mapList.Exists(map => map.name.Equals(mapName, StringComparison.CurrentCultureIgnoreCase))) {
            var findMapCache = mapList.Where(x => x.name.Contains(mapName, StringComparison.CurrentCultureIgnoreCase)).ToList();
            if (findMapCache.Count == 1 || findMapCache.First().name == mapName) {
                findMap = findMapCache.First();
            } else {
                var randomMap = findMapCache.First();
                command.ReplyToCommand($"你是否在寻找 {randomMap}");
                return;
            }
        } else {
            command.ReplyToCommand($"未找到地图{mapName},打开控制台输入 css_maplist 查看服务器地图列表");
            return;
        }

        if (mapNominateList.Find(x => x == findMap) != null) {
            command.ReplyToCommand($"地图 {findMap} 已被他人预定");
            return;
        }

        if (findMap.name == Server.MapName) {
            command.ReplyToCommand($"地图 {findMap} 为当前地图");
            return;
        }

        if (mapCooldown.Find(x => x == findMap) != null) {
            command.ReplyToCommand($"地图 {findMap} 最近已经游玩过了");
            return;
        }

        mapNominateList.Add(findMap);
        Server.PrintToChatAll($"{controller!.PlayerName} 预定了地图 {findMap}");
    }

    [ConsoleCommand("css_maplist")]
    [CommandHelper(minArgs: 1, usage: "[number]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void MapListCommand(CCSPlayerController? controller, CommandInfo command) {
        var x = mapList.Count / 10;
        var y = mapList.Count - (x * 10);
        var z = 1;

        if (command.GetArg(1) != null) {
            if (int.TryParse(command.GetArg(1), out var numValue)) {
                z = numValue;
            }
        } else {
            controller!.PrintToConsole("请正确输入数字如 css_maplist 1");
            return;
        }

        if (z - 1 > x || z <= 0) {
            controller!.PrintToConsole("输入的数字超出当前服务器地图池范围");
            return;
        }

        if (z - 1 > 0) {
            controller!.PrintToConsole($"输入 css_maplist {z - 1} 查看上一组列表");
        }

        for (var i = 0; i < 10; i++) {
            if (z == x && i >= y) break;
            controller!.PrintToConsole(mapList[(z - 1) * 10 + i].ToString());
        }

        if (z - 1 < x) {
            controller!.PrintToConsole($"输入 css_maplist {z + 1} 查看下一组列表");
        }
    }
}