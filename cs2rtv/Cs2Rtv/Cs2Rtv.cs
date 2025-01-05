using System.Collections.Immutable;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using System.Text.RegularExpressions;

namespace Cs2Rtv;

public partial class Cs2Rtv : BasePlugin {
    public override string ModuleAuthor => "lapl";
    public override string ModuleName => "MapChanger for kz/bhop/surf";
    public override string ModuleVersion => "1.0.1";
    private ImmutableList<Map> mapList = [];
    private readonly List<Map> mapNominateList = [];
    private readonly List<ulong> rtvCount = [];
    private readonly List<ulong> extCount = [];
    private List<Map> voteMapList = [];
    private readonly List<Map> mapCooldown = [];
    private Map? nextMap = null;
    private bool nextMapPass;
    private bool canRtv;
    private bool firstMapRandom;
    private bool isRtving;
    private bool isRtv;
    private bool rtvWin;
    private bool isRtvAgain;
    private int playerCount;
    private int rtvRequired;
    private int timeLeft;
    private int extRound;
    private Timer? canRtvTimer;
    private Timer? mapTimer;
    private Timer? rtvTimer;
    private Timer? changeMapRepeat;
    private Timer? repeatTimer;
    private readonly Random random = new();

    public override void Load(bool hotReload) {
        Logger.LogInformation("load mapList from {Path}", Path.Join(ModuleDirectory, "mapList.json"));
        mapList = loadMaps(new StreamReader(new FileStream(Path.Join(ModuleDirectory, "mapList.json"), FileMode.Open)));
        // EmitSoundExtension.Init();

        if (hotReload) {
            Server.NextFrame(() => {
                mapCooldown.Clear();
                var find = mapList.Find(map => map.name == Server.MapName);
                if (find != null) {
                    mapCooldown.Add(find);
                }

                rtvWin = false;
                rtvCount.Clear();
                extCount.Clear();
                mapNominateList.Clear();
                voteMapList = [];
                isRtv = false;
                isRtving = false;
                isRtvAgain = false;
                canRtv = true;
                nextMapPass = false;
                firstMapRandom = true;
                KillTimer();
                timeLeft = 30;
                StartMapTimer();
            });
        }

        RegisterListener<Listeners.OnMapStart>((onMapStartHandler) => {
            if (!firstMapRandom) {
                if (!MyRegex().IsMatch(Server.MapName)) {
                    Server.NextFrame(() => {
                        firstMapRandom = true;
                        var index = random.Next(0, mapList.Count - 1);
                        var randomMap = mapList[index];
                        if (randomMap.name == Server.MapName) return;
                        Server.ExecuteCommand($"host_workshop_map {randomMap.id}");
                    });
                    return;
                }
            }

            Server.NextFrame(() => {
                var find = mapList.Find(map => map.name == Server.MapName);
                if (find != null) {
                    mapCooldown.Add(find);
                }

                if (mapCooldown.Count > 5) {
                    mapCooldown.Remove(mapCooldown.First());
                }

                rtvWin = false;
                rtvCount.Clear();
                extCount.Clear();
                mapNominateList.Clear();
                voteMapList = [];
                isRtv = false;
                isRtving = false;
                isRtvAgain = false;
                nextMapPass = false;
                KillTimer();
                timeLeft = 30;
                extRound = 0;
                CanRtvTimer();
                StartMapTimer();
            });
        });
    }

    private static ImmutableList<Map> loadMaps(StreamReader reader) {
        var content = reader.ReadToEnd();
        return JsonSerializer.Deserialize<List<Map>>(content)?.ToImmutableList() ?? ImmutableList<Map>.Empty;
    }

    [GeneratedRegex(@"\bde_")]
    private static partial Regex MyRegex();
}