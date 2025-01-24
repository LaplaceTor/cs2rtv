using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace Cs2Rtv;

public static class EmitSoundExtension {
    private static readonly MemoryFunctionVoid<CBaseEntity, string, int, float, float> CBaseEntity_EmitSoundParamsFunc =
        new (
            Utils.IsLinux
                ? @"\x48\xB8\x2A\x2A\x2A\x2A\x2A\x2A\x2A\x2A\x55\x48\x89\xE5\x41\x55\x41\x54\x49\x89\xFC\x53\x48\x89\xF3"
                : @"\x48\x8B\xC4\x48\x89\x58\x10\x48\x89\x70\x18\x55\x57\x41\x56\x48\x8D\xA8\x08\xFF\xFF\xFF"
            );

    /// <summary>
    /// 播放默认音量和音调的声音
    /// </summary>
    /// <param name="entity">目标实体</param>
    /// <param name="soundName">声音名称</param>
    public static void EmitSound(this CBaseEntity entity, string soundName) {
        EmitSound(entity, soundName, null);
    }

    /// <summary>
    /// 播放指定音量和音调的声音
    /// </summary>
    /// <param name="entity">目标实体</param>
    /// <param name="soundName">声音名称</param>
    /// <param name="volume">音量 (0-1)</param>
    /// <param name="pitch">音调 (通常0.5-2.0)</param>
    public static void EmitSound(this CBaseEntity entity, string soundName, float volume, float pitch) {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(soundName)) throw new ArgumentException("Sound name cannot be null or whitespace.", nameof(soundName));
        
        CBaseEntity_EmitSoundParamsFunc.Invoke(entity, soundName, (int)(volume * 100), pitch, 0f);
    }

    [ThreadStatic] private static IReadOnlyDictionary<string, float>? CurrentParameters;

    /// <summary>
    /// 播放声音并传入参数字典
    /// </summary>
    /// <param name="entity">目标实体</param>
    /// <param name="soundName">声音名称</param>
    /// <param name="parameters">声音参数键值对</param>
    public static void EmitSound(this CBaseEntity entity, string soundName,
        IReadOnlyDictionary<string, float>? parameters = null) {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(soundName)) throw new ArgumentException("Sound name cannot be null or whitespace.", nameof(soundName));
        if (!entity.IsValid) throw new ArgumentException("Entity is not valid.");

        try {
            CurrentParameters = parameters;
            CBaseEntity_EmitSoundParamsFunc.Invoke(entity, soundName, 100, 1f, 0f);
        } finally {
            CurrentParameters = null;
        }
    }
}
