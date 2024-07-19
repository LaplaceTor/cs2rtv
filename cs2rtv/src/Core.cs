using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;

namespace cs2rtv
{
    public partial class Cs2rtv
    {
        public void StartRtv()
        {
            KillTimer();
            Logger.LogInformation("开始投票换图");
            GetPlayersCount();
            if (playercount == 0)
            {
                isrtv = true;
                var randommap = "";
                int index = random.Next(0, maplist.Count - 1);
                while (!rtvwin)
                {
                    if (mapcooldown.Find(x => x == maplist[index]) != null)
                        continue;
                    else
                    {
                        randommap = maplist[index];
                        rtvwin = true;
                    }
                }
                Logger.LogInformation("空服换图");
                VoteEnd(randommap);
                return;
            }
            var music = rtvmusiclist[random.Next(0, rtvmusiclist.Count - 1)];
            foreach (var player in IsPlayer())
            {
                AddTimer(5f, () => PlayClientSound(player, music, 0.5f, 1f));
            }
            if (!isrtvagain)
            {
                votemaplist = mapnominatelist;
                votemaplist.Add(Server.MapName);
                while (votemaplist.Count < 6)
                {
                    int index = random.Next(0, maplist.Count - 1);
                    if (votemaplist.Find(x => x == maplist[index]) != null || mapcooldown.Find(x => x == maplist[index]) != null) continue;
                    votemaplist.Add(maplist[index]);
                }
            }
            ChatMenu votemenu = new("请从以下地图中选择一张");
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
                        player.PrintToChat("你已投票给不更换地图");
                        Logger.LogInformation("{PlayerName} 投票给不换图", player.PlayerName);
                        MenuManager.CloseActiveMenu(player);
                        GetPlayersCount();
                        if (votes[mapname] >= rtvrequired)
                        {
                            nextmap = mapname;
                            rtvwin = true;
                            Server.PrintToChatAll("地图投票已结束");
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
                        MenuManager.CloseActiveMenu(player);
                        GetPlayersCount();
                        if (votes[mapname] >= rtvrequired)
                        {
                            nextmap = mapname;
                            rtvwin = true;
                            Server.PrintToChatAll("地图投票已结束");
                            VoteEnd(nextmap);
                            return;
                        }
                    });
                }
            }

            foreach (var player in IsPlayer())
                MenuManager.OpenChatMenu(player, votemenu);

            Server.NextFrame(() =>
            {
                _rtvtimer = AddTimer(30f, () =>
                {
                    if (!isrtving) return;
                    if (totalvotes == 0)
                    {
                        nextmap = mapnominatelist[random.Next(0, mapnominatelist.Count - 1)];
                        Server.PrintToChatAll("地图投票已结束");
                        rtvwin = true;
                    }
                    else if (votes.Select(x => x.Value).Max() > (totalvotes * 0.5f))
                    {
                        votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
                        nextmap = votes.First().Key;
                        Server.PrintToChatAll("地图投票已结束");
                        rtvwin = true;
                    }
                    else if (votes.Select(x => x.Value).Max() <= (totalvotes * 0.5f) && votemaplist.Count >= 4 && totalvotes > 2)
                    {
                        Server.PrintToChatAll("本轮投票未有地图投票比例超过50%，将进行下一轮投票");
                        votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
                        var y = votemaplist.Count();
                        votemaplist.Clear();
                        var x = 0;
                        while (x < (y * 0.5f))
                        {
                            if (votes.ElementAt(x).Key != null)
                            {
                                if (votes.ElementAt(x).Value != 0)
                                {
                                    votemaplist!.Add(votes.ElementAt(x).Key);
                                    x++;
                                }
                                else
                                    break;
                            }
                            else
                                break;
                        }
                    }
                    else if (votes.Select(x => x.Value).Max() <= (totalvotes * 0.5f) && (votemaplist.Count < 4 || totalvotes <= 2))
                    {
                        votes = votes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
                        nextmap = votes.First().Key;
                        if (votes.ContainsKey(Server.MapName) && votes.GetValueOrDefault(Server.MapName) != 0 && votes.First().Value <= (votes.GetValueOrDefault(Server.MapName) + 1))
                            nextmap = Server.MapName;
                        Server.PrintToChatAll("地图投票已结束");
                        rtvwin = true;
                    }
                    VoteEnd(nextmap);
                });
            });
        }

        public void VoteEnd(string mapname)
        {
            foreach(var player in IsPlayer())
                MenuManager.CloseActiveMenu(player);
            if (rtvwin)
            {
                rtvwin = false;
                votemaplist.Clear();
                isrtving = false;
                isrtvagain = false;
                if (_rtvtimer != null)
                {
                    _rtvtimer.Kill();
                    _rtvtimer = null;
                }
                if (mapname == Server.MapName)
                {
                    if (!isrtv)
                    {
                        Server.PrintToChatAll("地图已延长");
                        Logger.LogInformation("地图已延长");
                        timeleft = 30;
                    }
                    else
                    {
                        isrtv = false;
                        Server.PrintToChatAll("投票结果为不更换地图");
                        Logger.LogInformation("投票结果为不更换地图");
                    }
                    CanRtvtimer();
                    if(!nextmappass)                        
                        StartMaptimer();
                    else
                        EndMaptimer();
                }
                else
                {
                    mapnominatelist.Clear();
                    Server.PrintToChatAll($"投票决定为 {mapname}");
                    Logger.LogInformation("投票决定为 {mapname}", mapname);
                    nextmappass = true;
                    nextmapname = mapname;
                    CanRtvtimer();
                    if (!isrtv)
                    {
                        timeleft = 5;
                        EndMaptimer();
                    }
                    else
                        ChangeMapRepeat(mapname);
                }
            }
            else
            {
                isrtvagain = true;
                RepeatBroadcast(10,1f,"即将进行下一轮投票");
            }
        }
    }

}