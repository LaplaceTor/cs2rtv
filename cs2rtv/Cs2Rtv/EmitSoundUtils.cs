using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace Cs2Rtv;

public static class EmitSoundExtension {
    private static MemoryFunctionVoid<CBaseEntity, string, int, float, float> CBaseEntity_EmitSoundParamsFunc =
        new (
            Cs2Rtv.IsLinux
                ? @"\x48\xB8\x2A\x2A\x2A\x2A\x2A\x2A\x2A\x2A\x55\x48\x89\xE5\x41\x55\x41\x54\x49\x89\xFC\x53\x48\x89\xF3"
                : @"\x48\x8B\xC4\x48\x89\x58\x10\x48\x89\x70\x18\x55\x57\x41\x56\x48\x8D\xA8\x08\xFF\xFF\xFF"
            );

    [ThreadStatic] private static IReadOnlyDictionary<string, float>? CurrentParameters;

    public static void EmitSound(this CBaseEntity entity, string soundName,
        IReadOnlyDictionary<string, float>? parameters = null) {
        if (!entity.IsValid) {
            throw new ArgumentException("Entity is not valid.");
        }

        try {
            CurrentParameters = parameters;
            CBaseEntity_EmitSoundParamsFunc.Invoke(entity, soundName, 100, 1f, 0f);
        } finally {
            CurrentParameters = null;
        }
    }
}