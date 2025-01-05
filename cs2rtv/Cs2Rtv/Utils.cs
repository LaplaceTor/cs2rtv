using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Cs2Rtv;

public partial class Cs2Rtv {
    private void GetPlayersCount() {
        playerCount = IsPlayer().Count();
        rtvRequired = (int)Math.Ceiling(playerCount * 0.6f);
    }

    private static void PlayClientSound(CCSPlayerController controller, string sound, float volume = 1.0f,
        float pitch = 1.0f) {
        var parameters = new Dictionary<string, float> {
            { "volume", volume },
            { "pitch", pitch }
        };
        controller.EmitSound(sound, parameters);
    }

    private IEnumerable<CCSPlayerController> IsPlayer() {
        var player = Utilities.GetPlayers().Where(x =>
            x is { TeamNum: > 0, IsValid: true, Connected: PlayerConnectedState.PlayerConnected }
        );
        return player;
    }
}