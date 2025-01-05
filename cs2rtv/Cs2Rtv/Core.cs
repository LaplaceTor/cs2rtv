using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;

namespace Cs2Rtv;

public partial class Cs2Rtv {
    private void StartRtv() {
        KillTimer();
        Logger.LogInformation("开始投票换图");

        // if(maplist.Count() == 0)
        //     maplist = new List<string>(File.ReadAllLines(Path.Join(ModuleDirectory, "maplist.txt")));
        GetPlayersCount();
        if (playerCount == 0) {
            isRtv = true;
            Map? randomMap = null;
            var index = random.Next(0, mapList.Count - 1);
            while (!rtvWin) {
                if (mapCooldown.Find(x => x == mapList[index]) == null) continue;
                randomMap = mapList[index];
                rtvWin = true;
            }

            Logger.LogInformation("空服换图");
            VoteEnd(randomMap!);
            return;
        }

        var music = rtvmusiclist[random.Next(0, rtvmusiclist.Count - 1)];
        foreach (var player in IsPlayer()) {
            AddTimer(5f, () => PlayClientSound(player, music, 0.5f));
        }

        if (!isRtvAgain) {
            voteMapList = mapNominateList;
            
            var find = mapList.Find(map => map.name == Server.MapName);
            if (find != null) {
                voteMapList.Add(find);
            }
            
            while (voteMapList.Count < 6) {
                var index = random.Next(0, mapList.Count - 1);
                if (voteMapList.Find(x => x == mapList[index]) != null ||
                    mapCooldown.Find(x => x == mapList[index]) != null) continue;
                voteMapList.Add(mapList[index]);
            }
        }

        ChatMenu voteMenu = new("请从以下地图中选择一张");
        Map? nextMap = null;
        var totalVotes = 0;
        Dictionary<Map, int> votes = new();
        votes.Clear();

        foreach (var map in voteMapList) {
            votes[map] = 0;
            if (map.name == Server.MapName) {
                voteMenu.AddMenuOption("不更换地图", (player, _) => {
                    votes[map] += 1;
                    totalVotes += 1;
                    player.PrintToChat("你已投票给不更换地图");
                    Logger.LogInformation("{PlayerName} 投票给不换图", player.PlayerName);
                    MenuManager.CloseActiveMenu(player);
                    GetPlayersCount();
                    if (votes[map] < rtvRequired) return;
                    nextMap = map;
                    rtvWin = true;
                    Server.PrintToChatAll("地图投票已结束");
                    VoteEnd(nextMap);
                });
            } else {
                voteMenu.AddMenuOption(map.name, (player, _) => {
                    votes[map] += 1;
                    totalVotes += 1;
                    player.PrintToChat($"你已投票给地图 {map}");
                    Logger.LogInformation("{PlayerName} 投票给地图 {mapname}", player.PlayerName, map);
                    MenuManager.CloseActiveMenu(player);
                    GetPlayersCount();
                    if (votes[map] < rtvRequired) return;
                    nextMap = map;
                    rtvWin = true;
                    Server.PrintToChatAll("地图投票已结束");
                    VoteEnd(nextMap);
                });
            }
        }

        foreach (var player in IsPlayer())
            MenuManager.OpenChatMenu(player, voteMenu);

        Server.NextFrame(() => {
            rtvTimer = AddTimer(30f, () => {
                if (!isRtving) return;
                if (totalVotes == 0) {
                    nextMap = mapNominateList[random.Next(0, mapNominateList.Count - 1)];
                    Server.PrintToChatAll("地图投票已结束");
                    rtvWin = true;
                } else if (votes.Select(x => x.Value).Max() > (totalVotes * 0.5f)) {
                    votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
                    nextMap = votes.First().Key;
                    Server.PrintToChatAll("地图投票已结束");
                    rtvWin = true;
                } else if (votes.Select(x => x.Value).Max() <= (totalVotes * 0.5f) && voteMapList.Count >= 4 &&
                           totalVotes > 2) {
                    Server.PrintToChatAll("本轮投票未有地图投票比例超过50%，将进行下一轮投票");
                    votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
                    var y = voteMapList.Count;
                    voteMapList.Clear();
                    var x = 0;
                    while (x < y * 0.5f) {
                        if (votes.ElementAt(x).Key != null) {
                            if (votes.ElementAt(x).Value != 0) {
                                voteMapList!.Add(votes.ElementAt(x).Key);
                                x++;
                            } else {
                                break;
                            }
                        } else {
                            break;
                        }
                    }
                } else if (votes.Select(x => x.Value).Max() <= (totalVotes * 0.5f) &&
                           (voteMapList.Count < 4 || totalVotes <= 2)) {
                    votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
                    nextMap = votes.First().Key;
                    var map = votes.Keys.FirstOrDefault(map => map.name == Server.MapName);
                    if (map != null && votes.GetValueOrDefault(map) != 0 &&
                        votes.First().Value <= votes.GetValueOrDefault(map) + 1) {
                        nextMap = map;
                    }
                    Server.PrintToChatAll("地图投票已结束");
                    rtvWin = true;
                }

                VoteEnd(nextMap!);
            });
        });
    }

    private void VoteEnd(Map map) {
        foreach (var player in IsPlayer())
            MenuManager.CloseActiveMenu(player);
        if (rtvWin) {
            rtvWin = false;
            voteMapList.Clear();
            isRtving = false;
            isRtvAgain = false;
            if (rtvTimer != null) {
                rtvTimer.Kill();
                rtvTimer = null;
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
                if (!nextMapPass)
                    StartMapTimer();
                else
                    EndMapTimer();
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
                } else
                    ChangeMapRepeat(map);
            }
        } else {
            isRtvAgain = true;
            RepeatBroadcast(10, 1f, "即将进行下一轮投票");
        }
    }
}