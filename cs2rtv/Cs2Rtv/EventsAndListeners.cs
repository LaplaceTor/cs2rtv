using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace Cs2Rtv;

public partial class Cs2Rtv {
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info) {
        if (rtvCount.Contains(@event.Userid!.SteamID))
            rtvCount.Remove(@event.Userid.SteamID);
        if (extCount.Contains(@event.Userid!.SteamID))
            extCount.Remove(@event.Userid.SteamID);
        GetPlayersCount();
        if (rtvCount.Count >= rtvRequired && playerCount != 0) {
            isRtving = true;
            isRtv = true;
            rtvCount.Clear();
            RepeatBroadcast(10, 1f, "地图投票即将开始");
        }

        if (extCount.Count >= rtvRequired && playerCount != 0) {
            Server.PrintToChatAll("地图已延长");
            timeLeft += 30;
            extRound++;
            extCount.Clear();
        }

        return HookResult.Continue;
    }
}