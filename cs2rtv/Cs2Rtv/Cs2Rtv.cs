﻿﻿﻿﻿﻿using System.Collections.Immutable;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using System.Text.RegularExpressions;

namespace Cs2Rtv;

public partial class Cs2Rtv : BasePlugin {
    public override string ModuleAuthor => "lapl && Nyayurin";
    public override string ModuleName => "MapChanger for kz/bhop/surf";
    public override string ModuleVersion => "2.0beta";
    
    private ImmutableList<Map> mapList = [];
    private Dictionary<string, Map> mapDictionary = new();
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
        try {
            Utils.Initialize(Logger, (delay, action) => AddTimer(delay, action));
            Logger.LogInformation("Loading map list from {Path}", Path.Join(ModuleDirectory, "mapList.json"));
            mapList = LoadMaps(Path.Join(ModuleDirectory, "mapList.json"));
            mapDictionary = mapList.ToDictionary(map => map.name);
            
            if (hotReload) {
                Server.NextFrame(() => {
                    ResetState();
                    firstMapRandom = true;
                    StartMapTimer();
                });
            }
        } catch (Exception ex) {
            Logger.LogError(ex, "Failed to load map list");
            return;
        }

        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
    }

    private void OnMapStartHandler(string mapName) {
        if (!firstMapRandom) {
            if (!MyRegex().IsMatch(Server.MapName)) {
                Server.NextFrame(() => {
                    firstMapRandom = true;
                    var index = random.Next(0, mapList.Count - 1);
                    var randomMap = mapList[index];
                    if (randomMap.name != Server.MapName) {
                        Server.ExecuteCommand($"host_workshop_map {randomMap.id}");
                    }
                });
                return;
            }
        }

        Server.NextFrame(() => {
            ResetState();
            
            if (mapCooldown.Count > 5) {
                mapCooldown.Remove(mapCooldown.First());
            }
            
            extRound = 0;
            CanRtvTimer();
            StartMapTimer();
        });
    }

    private void ResetState() {
        mapCooldown.Clear();
        if (mapDictionary.TryGetValue(Server.MapName, out var currentMap)) {
            mapCooldown.Add(currentMap);
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
        KillTimer();
        timeLeft = 30;
    }

    private static ImmutableList<Map> LoadMaps(string filePath) {
        using var reader = new StreamReader(new FileStream(filePath, FileMode.Open));
        var content = reader.ReadToEnd();
        return JsonSerializer.Deserialize<List<Map>>(content)?.ToImmutableList() ?? ImmutableList<Map>.Empty;
    }

    [GeneratedRegex(@"\bde_")]
    private static partial Regex MyRegex();
}
