using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;

namespace Cs2Rtv;

public partial class Cs2Rtv {
    private void StartRtv() {
        try {
            // 初始化投票状态
            InitializeVoteState();
            
            // 检查地图列表是否为空
            if (IsMapListEmpty()) {
                HandleEmptyMapList();
                return;
            }

            // 处理空服情况
            if (IsServerEmpty()) {
                HandleEmptyServer();
                return;
            }

            // 播放投票音乐
            PlayVoteMusic();

            // 准备投票地图列表
            PrepareVoteMapList();

            // 创建并显示投票菜单
            var voteMenu = CreateVoteMenu();
            ShowVoteMenu(voteMenu);

            // 设置投票计时器
            SetupVoteTimer();
            
        } catch (Exception ex) {
            Logger.LogError(ex, "StartRtv执行失败");
            ResetVoteState();
        }
    }

    private void InitializeVoteState() {
        KillTimer();
        Logger.LogInformation("开始投票换图");
    }

    private bool IsMapListEmpty() {
        return mapList.Count == 0;
    }

    private void HandleEmptyMapList() {
        Server.PrintToChatAll("地图列表为空，请联系管理员");
        Logger.LogError("地图列表为空");
        ResetVoteState();
    }

    private bool IsServerEmpty() {
        Utils.GetPlayersCount();
        return playerCount == 0;
    }

    private void HandleEmptyServer() {
        isRtv = true;
        var randomMap = GetRandomMap();
        Logger.LogInformation("空服换图");
        VoteEnd(randomMap);
    }

    private Map GetRandomMap() {
        Map? randomMap = null;
        while (!rtvWin) {
            var index = random.Next(0, mapList.Count - 1);
            if (mapCooldown.Find(x => x == mapList[index]) == null) continue;
            randomMap = mapList[index];
            rtvWin = true;
        }
        return randomMap!;
    }

    private void PlayVoteMusic() {
        var music = rtvmusiclist[random.Next(0, rtvmusiclist.Count - 1)];
        foreach (var player in Utils.GetPlayers()) {
            AddTimer(5f, () => player.EmitSound(music, new Dictionary<string, float> { { "volume", 0.5f }, { "pitch", 1.0f } }));
        }
    }

    private void PrepareVoteMapList() {
        if (!isRtvAgain) {
            voteMapList = mapNominateList;
            
            var currentMap = mapList.Find(map => map.name == Server.MapName);
            if (currentMap != null) {
                voteMapList.Add(currentMap);
            }
            
            while (voteMapList.Count < 6) {
                var index = random.Next(0, mapList.Count - 1);
                if (voteMapList.Find(x => x == mapList[index]) != null ||
                    mapCooldown.Find(x => x == mapList[index]) != null) continue;
                voteMapList.Add(mapList[index]);
            }
        }
    }

    private ChatMenu CreateVoteMenu() {
        var voteMenu = new ChatMenu("请从以下地图中选择一张");
        var totalVotes = 0;
        var votes = new Dictionary<Map, int>();

        foreach (var map in voteMapList) {
            votes[map] = 0;
            if (map.name == Server.MapName) {
                voteMenu.AddMenuOption("不更换地图", CreateVoteHandler(map, votes, totalVotes, "不更换地图"));
            } else {
                voteMenu.AddMenuOption($"{map.name}(Tier {map.tier})", CreateVoteHandler(map, votes, totalVotes, map.name));
            }
        }

        return voteMenu;
    }

    private Action<CounterStrikeSharp.API.Core.CCSPlayerController, ChatMenuOption> CreateVoteHandler(Map map, Dictionary<Map, int> votes, int totalVotes, string mapName) {
        return (player, _) => {
            votes[map] += 1;
            totalVotes += 1;
            player.PrintToChat($"你已投票给{mapName}");
            Logger.LogInformation("{PlayerName} 投票给 {mapname}", player.PlayerName, mapName);
            MenuManager.CloseActiveMenu(player);
            Utils.GetPlayersCount();
            if (votes[map] < rtvRequired) return;
            rtvWin = true;
            Server.PrintToChatAll("地图投票已结束");
            VoteEnd(map);
        };
    }

    private void ShowVoteMenu(ChatMenu voteMenu) {
        foreach (var player in Utils.GetPlayers()) {
            MenuManager.OpenChatMenu(player, voteMenu);
        }
    }

    private void SetupVoteTimer() {
        Server.NextFrame(() => {
            rtvTimer = AddTimer(30f, HandleVoteTimer);
        });
    }

    private void HandleVoteTimer() {
        if (!isRtving) return;
        
        var votes = voteMapList.ToDictionary(map => map, _ => 0);
        var totalVotes = votes.Values.Sum();

        if (totalVotes == 0) {
            // 无人投票时随机选择地图
            var nextMap = mapNominateList[random.Next(0, mapNominateList.Count - 1)];
            Server.PrintToChatAll("地图投票已结束");
            rtvWin = true;
            VoteEnd(nextMap);
        } else if (votes.Select(x => x.Value).Max() > (totalVotes * 0.5f)) {
            // 有地图获得超过50%投票
            votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
            var nextMap = votes.First().Key;
            Server.PrintToChatAll("地图投票已结束");
            rtvWin = true;
            VoteEnd(nextMap);
        } else if (votes.Select(x => x.Value).Max() <= (totalVotes * 0.5f) && voteMapList.Count >= 4 && totalVotes > 2) {
            // 平局且符合条件时进入下一轮投票
            Server.PrintToChatAll("本轮投票未有地图投票比例超过50%，将进行下一轮投票");
            votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
            var y = voteMapList.Count;
            voteMapList.Clear();
            var x = 0;
            while (x < y * 0.5f) {
                if (votes.ElementAt(x).Key != null && votes.ElementAt(x).Value != 0) {
                    voteMapList!.Add(votes.ElementAt(x).Key);
                    x++;
                } else {
                    break;
                }
            }
            isRtvAgain = true;
            RepeatBroadcast(10, 1f, "即将进行下一轮投票");
        } else {
            // 默认投票处理
            votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
            var nextMap = votes.First().Key;
            var currentMap = votes.Keys.FirstOrDefault(map => map.name == Server.MapName);
            if (currentMap != null && votes.GetValueOrDefault(currentMap) != 0 &&
                votes.First().Value <= votes.GetValueOrDefault(currentMap) + 1) {
                nextMap = currentMap;
            }
            Server.PrintToChatAll("地图投票已结束");
            rtvWin = true;
            VoteEnd(nextMap);
        }
    }

    private void ResetVoteState() {
        rtvWin = false;
        isRtving = false;
        isRtvAgain = false;
    }

    private void VoteEnd(Map map) {
        try {
            foreach (var player in Utils.GetPlayers()) {
                try {
                    MenuManager.CloseActiveMenu(player);
                } catch (Exception ex) {
                    Logger.LogError(ex, "关闭玩家菜单失败");
                }
            }

            if (rtvWin) {
                rtvWin = false;
                voteMapList.Clear();
                isRtving = false;
                isRtvAgain = false;
                
                try {
                    if (rtvTimer != null) {
                        rtvTimer.Kill();
                        rtvTimer = null;
                    }
                } catch (Exception ex) {
                    Logger.LogError(ex, "关闭rtvTimer失败");
                }

                if (map.name == Server.MapName) {
                    if (!isRtv) {
                        Server.PrintToChatAll("地图已延长");
                        Logger.LogInformation("地图已延长");
                        timeLeft = 30;
                    } else {
                        isRtv = false;
                        Server.PrintToChatAll("投票结果为不更换地图");
                        Logger.LogInformation("投票结果为不更换地图");
                    }

                    CanRtvTimer();
                    if (!nextMapPass){
                        if(timeLeft < 5){
                            timeLeft = 5;
                            StartMapTimer();
                        }
                    } else {
                        EndMapTimer();
                    }
                } else {
                    mapNominateList.Clear();
                    Server.PrintToChatAll($"投票决定为 {map.name}");
                    Logger.LogInformation($"投票决定为 {map.name}");
                    nextMapPass = true;
                    nextMap = map;
                    CanRtvTimer();
                    if (!isRtv) {
                        timeLeft = 5;
                        EndMapTimer();
                    } else {
                        ChangeMapRepeat(map);
                    }
                }
            } else {
                isRtvAgain = true;
                RepeatBroadcast(10, 1f, "即将进行下一轮投票");
            }
        } catch (Exception ex) {
            Logger.LogError(ex, "VoteEnd执行失败");
            rtvWin = false;
            isRtving = false;
            isRtvAgain = false;
        }
    }
}
