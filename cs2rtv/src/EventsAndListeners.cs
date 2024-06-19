
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;

namespace cs2rtv
{
    public partial class Cs2rtv
    {
        public void OnMapStartHandler(string mapName)
        {
            if (!firstmaprandom)
            {
                if (!Regex.IsMatch(Server.MapName, @$"\bde_"))
                {
                    Server.NextFrame(() =>
                    {
                        firstmaprandom = true;
                        Random random = new();
                        int index = random.Next(0, maplist.Count - 1);
                        var randommap = maplist[index];
                        if (randommap == Server.MapName)
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
                isrtv = false;
                isrtving = false;
                isrtvagain = false;
                nextmappass = false;
                KillTimer();
                timeleft = 30;
                extround = 0;
                CanRtvtimer();
                StartMaptimer();
            });
        }

        [GameEventHandler]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
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
            if (extcount.Count >= rtvrequired && playercount != 0)
            {
                Server.PrintToChatAll("地图已延长");
                timeleft += 30;
                extcount.Clear();
                CanRtvtimer();
            }
            return HookResult.Continue;
        }
    }
}