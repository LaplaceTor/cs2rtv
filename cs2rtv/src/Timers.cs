using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace cs2rtv
{
    public partial class Cs2rtv
    {
        private void KillTimer()
        {
            if (_canrtvtimer != null)
                _canrtvtimer.Kill();
            if (_maptimer != null)
                _maptimer.Kill();
            if (_rtvtimer != null)
                _rtvtimer.Kill();
            if (_endmaptimer != null)
                _endmaptimer.Kill();
            if (_changemaprepeat != null)
                _changemaprepeat.Kill();
            if (_repeattimer != null)
                _repeattimer.Kill();
        }

        private void StartMaptimer()
        {
            Server.NextFrame(() =>
            {
                _maptimer = AddTimer(60f, StartMaptimerHandler, TimerFlags.STOP_ON_MAPCHANGE);
            });
        }

        private void StartMaptimerHandler()
        {
            timeleft--;
            if (timeleft <= 0)
            {
                isrtving = true;
                // var x = 0;
                // _repeattimer = AddTimer(1f, () =>
                // {
                //     x++;
                //     if (x >= 10)
                //     {
                //             Server.NextFrame(() => StartRtv());
                //     }else
                //     {
                //         foreach (var player in IsPlayer())
                //         {
                //             PlayClientSound(player, "Alert.WarmupTimeoutBeep");
                //             player.PrintToChat("当前地图时长还剩5分钟");
                //         }
                //     }
                // }, TimerFlags.REPEAT);
                RepeatBroadcast(10,1f,"当前地图时长还剩5分钟");
            }
            else
            {
                _maptimer = AddTimer(60f, StartMaptimerHandler, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        private void EndMaptimer(string mapname)
        {
            _endmaptimer = AddTimer(60f, () =>
            {
                timeleft--;
                if (timeleft <= 0)
                    ChangeMapRepeat(mapname);
                else if (timeleft < 2 && timeleft > 0)
                    Server.PrintToChatAll("距离换图还有60秒");
                else
                    EndMaptimer(mapname);
            });
        }

        private void CanRtvtimer()
        {
            canrtv = false;
            Server.NextFrame(() => _canrtvtimer = AddTimer(5 * 60f, () => canrtv = true));
        }

        private void ChangeMapRepeat(string mapname)
        {
            var music = mapendmusiclist[random.Next(0, mapendmusiclist.Count - 1)];
            foreach (var player in IsPlayer())
                    PlayClientSound(player, music);
            var second = 10;
            _repeattimer = AddTimer(1.0f, () =>
            {
                if (second <= 0)
                {
                    if (_repeattimer != null)
                        Server.NextFrame(() => _repeattimer.Kill());
                    return;
                }
                foreach (var player in IsPlayer())
                    player.PrintToChat($"距离换图还有{second}秒");
                second--;
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            var tryround = 0;
            _changemaprepeat = AddTimer(10f, () =>
            {
                Server.ExecuteCommand($"ds_workshop_changelevel {mapname}");
                tryround++;
                if (tryround > 6)
                {
                    var randommap = maplist[random.Next(0, maplist.Count - 1)];
                    Server.ExecuteCommand($"ds_workshop_changelevel {randommap}");
                }
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void RepeatBroadcast(int repeatround, float eachrepeattime, string chatmessage)
        {
            _repeattimer = AddTimer(eachrepeattime, ()=>
            {
                if(repeatround <= 0)
                    Server.NextFrame(() => StartRtv());
                else
                {
                    foreach (var player in IsPlayer())
                    {
                        PlayClientSound(player, "Alert.WarmupTimeoutBeep");
                        player.PrintToChat(chatmessage);
                    }
                    repeatround--;
                    RepeatBroadcast(repeatround, eachrepeattime, chatmessage);
                }
            });
        }
    }
}