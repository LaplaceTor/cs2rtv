using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;

namespace Cs2Rtv;

public static class Utils
{
    private static ILogger? _logger;
    private static Func<float, Action, CounterStrikeSharp.API.Modules.Timers.Timer>? _addTimer;

    public static void Initialize(ILogger logger, Func<float, Action, CounterStrikeSharp.API.Modules.Timers.Timer> addTimer)
    {
        _logger = logger;
        _addTimer = addTimer;
    }

    public static void RetryTimer(Action action, float delay)
    {
        try
        {
            _addTimer?.Invoke(delay, () => 
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in retry timer");
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create retry timer");
        }
    }

    public static void BroadcastMessage(string message, CCSPlayerController? specificPlayer = null)
    {
        try
        {
            if (specificPlayer != null)
            {
                specificPlayer.PrintToChat(message);
            }
            else
            {
                Server.PrintToChatAll(message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to broadcast message");
        }
    }

    public static int GetPlayersCount()
    {
        return GetPlayers().Count();
    }

    public static IEnumerable<CCSPlayerController> GetPlayers()
    {
        return Utilities.GetPlayers().Where(x =>
            x is { TeamNum: > 0, IsValid: true, Connected: PlayerConnectedState.PlayerConnected }
        );
    }

    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
