using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace cs2rtv
{
    public partial class Cs2rtv
    {
        private void GetPlayersCount()
        {
            playercount = IsPlayer().Count();
            rtvrequired = (int)Math.Ceiling(playercount * 0.6f);
        }

        private static void PlayClientSound(CCSPlayerController ccsplayer, string sound, float volume = 1.0f, float pitch = 1.0f)
        {
            var parameters = new Dictionary<string, float>
        {
            { "volume", volume },
            { "pitch", pitch }
        };
            ccsplayer.EmitSound(sound, parameters);
        }
        
        private IEnumerable<CCSPlayerController> IsPlayer()
        {
            var player = Utilities.GetPlayers().Where((x) =>
            x.TeamNum > 0 &&
            x.IsValid &&
            x.Connected == PlayerConnectedState.PlayerConnected
            );
            return player;
        }
    }
}