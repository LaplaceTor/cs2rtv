using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace cs2rtv
{
    public partial class Cs2rtv
    {
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
                RepeatBroadcast(10,1f,"地图投票即将开始");
            }
            if (extcount.Count >= rtvrequired && playercount != 0)
            {
                Server.PrintToChatAll("地图已延长");
                timeleft += 30;
                extround++;
                extcount.Clear();
            }
            return HookResult.Continue;
        }
    }
}