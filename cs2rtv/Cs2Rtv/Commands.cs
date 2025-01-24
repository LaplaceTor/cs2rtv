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
        controller!.EmitSound("StopSoundEvents.StopAllMusic", new Dictionary<string, float> { { "volume", 1.0f }, { "pitch", 1.0f } });
    }

    [ConsoleCommand("css_maplistreload")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ReloadMaplistCommand(CCSPlayerController? controller, CommandInfo command) {
        mapList = LoadMaps(Path.Join(ModuleDirectory, "mapList.json"));
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

        Utils.GetPlayersCount();

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

        Utils.GetPlayersCount();
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
    [CommandHelper(minArgs: 1, usage: "[mapName/mapID]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/changemap")]
    public void ChangeMapCommand(CCSPlayerController? controller, CommandInfo command) {
        var mapName = command.GetArg(1);
        var findMapCache = mapList.Where(x => x.name.Contains(mapName, StringComparison.CurrentCultureIgnoreCase)).ToList();
        if (findMapCache.Count == 1 || findMapCache.First().name == mapName) {
              Server.ExecuteCommand($"host_workshop_map {findMapCache.First().id}");
        }else{
            Server.ExecuteCommand($"host_workshop_map {mapName}");
        }
    }


    [ConsoleCommand("css_yd")]
    [CommandHelper(minArgs: 1, usage: "[mapName/mapID]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void NominateCommand(CCSPlayerController? controller, CommandInfo command) {
        if (controller == null) return;

        if (isRtving) {
            command.ReplyToCommand("投票已在进行中");
            return;
        }

        if (mapNominateList.Count >= 5) {
            command.ReplyToCommand($"当前预定地图已满（{mapNominateList.Count}/5）");
            return;
        }

        var input = command.GetArg(1);
        Map? findMap = null;

        // 先尝试通过ID查找
        if (int.TryParse(input, out var mapId)) {
            findMap = mapList.FirstOrDefault(m => m.id == mapId);
        }

        // 如果ID查找失败，尝试通过名称查找
        if (findMap == null) {
            var matches = mapList
                .Where(m => m.name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1) {
                findMap = matches[0];
            } else if (matches.Count > 1) {
                command.ReplyToCommand($"你是否在寻找 {matches[0].name}");
                return;
            }
        }

        if (findMap == null) {
            command.ReplyToCommand($"未找到地图 '{input}'，使用 css_maplist 查看地图列表");
            return;
        }

        if (mapNominateList.Contains(findMap)) {
            command.ReplyToCommand($"地图 {findMap.name} 已被他人预定");
            return;
        }

        if (findMap.name == Server.MapName) {
            command.ReplyToCommand($"地图 {findMap.name} 为当前地图");
            return;
        }

        if (mapCooldown.Contains(findMap)) {
            command.ReplyToCommand($"地图 {findMap.name} 最近已经游玩过了");
            return;
        }

        mapNominateList.Add(findMap);
        Server.PrintToChatAll($"{controller.PlayerName} 预定了地图 {findMap.name} (ID: {findMap.id})");
    }

    [ConsoleCommand("css_maplist")]
    [CommandHelper(minArgs: 0, usage: "[page] [tier]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void MapListCommand(CCSPlayerController? controller, CommandInfo command) {
        if (controller == null) return;

        const int pageSize = 10;
        var totalPages = (int)Math.Ceiling(mapList.Count / (double)pageSize);
        var currentPage = 1;
        var tierFilter = string.Empty;

        // 解析页码参数
        if (command.ArgCount > 1 && int.TryParse(command.GetArg(1), out var page) && page > 0) {
            currentPage = Math.Min(page, totalPages);
        }

        // 解析tier过滤参数
        if (command.ArgCount > 2) {
            tierFilter = command.GetArg(2);
        }

        // 过滤地图列表
        var filteredMaps = mapList
            .Where(m => string.IsNullOrEmpty(tierFilter) || 
                       m.tier.ToString() == tierFilter)
            .ToList();

        // 计算过滤后的分页信息
        var filteredPages = (int)Math.Ceiling(filteredMaps.Count / (double)pageSize);
        currentPage = Math.Min(currentPage, filteredPages);

        // 显示地图列表
        controller.PrintToConsole($"=== 地图列表 (第 {currentPage}/{filteredPages} 页) ===");
        controller.PrintToConsole($"总地图数: {filteredMaps.Count}");

        var startIndex = (currentPage - 1) * pageSize;
        var endIndex = Math.Min(startIndex + pageSize, filteredMaps.Count);

        for (var i = startIndex; i < endIndex; i++) {
            var map = filteredMaps[i];
            controller.PrintToConsole($"{i + 1}. {map.name} (ID: {map.id}, Tier: {map.tier})");
        }

        // 显示导航提示
        if (currentPage > 1) {
            controller.PrintToConsole($"输入 css_maplist {currentPage - 1} {tierFilter} 查看上一页");
        }
        if (currentPage < filteredPages) {
            controller.PrintToConsole($"输入 css_maplist {currentPage + 1} {tierFilter} 查看下一页");
        }
        if (!string.IsNullOrEmpty(tierFilter)) {
            controller.PrintToConsole($"当前过滤: Tier {tierFilter}");
        }
    }
}
