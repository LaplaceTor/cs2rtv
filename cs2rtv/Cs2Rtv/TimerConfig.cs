using CounterStrikeSharp.API;

namespace Cs2Rtv;

public class TimerConfig
{
    // Map timer settings
    public static float MapTimerInterval { get; } = 60f; // in seconds
    public static int MapWarningThreshold { get; } = 10; // minutes
    public static int MapFinalCountdown { get; } = 5; // minutes

    // RTV timer settings
    public static float RtvTimerInterval { get; } = 30f; // in seconds
    public static int RtvCooldown { get; } = 5; // minutes

    // Repeat timer settings
    public static float RepeatInterval { get; } = 1f; // in seconds
    public static int DefaultRepeatCount { get; } = 10;

    // Change map retry settings
    public static float ChangeMapRetryInterval { get; } = 10f; // in seconds
    public static int MaxRetryAttempts { get; } = 5;

    // Error handling
    public static float ErrorRetryInterval { get; } = 30f; // in seconds
    public static int MaxErrorRetries { get; } = 3;
}
