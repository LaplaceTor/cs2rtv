using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;

namespace Cs2Rtv;

public partial class Cs2Rtv {
    private void KillTimer() {
        canRtvTimer?.Kill();
        mapTimer?.Kill();
        rtvTimer?.Kill();
        changeMapRepeat?.Kill();
        repeatTimer?.Kill();
    }

    private void StartMapTimer() {
        Server.NextFrame(() => { mapTimer = AddTimer(60f, StartMapTimerHandler, TimerFlags.STOP_ON_MAPCHANGE); });
    }

    private void StartMapTimerHandler() {
        timeLeft--;
        if (timeLeft % 10 == 0 && timeLeft > 0) {
            Server.PrintToChatAll($"距离投票下一张地图还有{timeLeft}分钟");
        }

        if (timeLeft <= 0) {
            isRtving = true;
            RepeatBroadcast(10, 1f, "当前地图时长还剩5分钟");
        } else {
            mapTimer = AddTimer(60f, StartMapTimerHandler, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }

    private void EndMapTimer() {
        Server.NextFrame(() => { mapTimer = AddTimer(60f, EndMapTimerHandler, TimerFlags.STOP_ON_MAPCHANGE); });
    }

    private void EndMapTimerHandler() {
        timeLeft--;
        if (timeLeft <= 0)
            ChangeMapRepeat(nextMap!);
        else {
            if (timeLeft is < 2 and > 0)
                Server.PrintToChatAll("距离换图还有60秒");
            mapTimer = AddTimer(60f, EndMapTimerHandler, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }

    private void CanRtvTimer() {
        canRtv = false;
        Server.NextFrame(() => canRtvTimer = AddTimer(5 * 60f, () => canRtv = true));
    }

    private void ChangeMapRepeat(Map map) {
        var music = mapendmusiclist[random.Next(0, mapendmusiclist.Count - 1)];
        RepeatBroadcast(10, 1f, $"即将更换地图为{map}......");
        ChangeMapRepeatHandler(map, 5);
    }

    private void ChangeMapRepeatHandler(Map map, int tryRound) {
        changeMapRepeat = AddTimer(10f, () => {
            tryRound--;
            if (tryRound < 0) {
                map = mapList[random.Next(0, mapList.Count - 1)];
            }

            Server.ExecuteCommand($"host_workshop_map {map.id}");
            ChangeMapRepeatHandler(map, tryRound);
        });
    }

    private void RepeatBroadcast(int repeatRound, float eachRepeatTime, string chatMessage) {
        repeatTimer = AddTimer(eachRepeatTime, () => {
            if (repeatRound <= 0) {
                Server.NextFrame(StartRtv);
            } else {
                foreach (var player in IsPlayer()) {
                    PlayClientSound(player, "Alert.WarmupTimeoutBeep");
                    player.PrintToChat(chatMessage);
                }

                repeatRound--;
                RepeatBroadcast(repeatRound, eachRepeatTime, chatMessage);
            }
        });
    }
}