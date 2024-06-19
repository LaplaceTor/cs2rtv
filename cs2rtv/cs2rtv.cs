using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace cs2rtv
{
    public partial class Cs2rtv : BasePlugin
    {
        public override string ModuleAuthor => "lapl";
        public override string ModuleName => "MapChanger for kz/bhop/surf";
        public override string ModuleVersion => "1.0.1";
        private List<string> maplist = [];
        private readonly List<string> mapnominatelist = [];
        private readonly List<ulong> rtvcount = [];
        private readonly List<ulong> extcount = [];
        private List<string> votemaplist = [];
        private readonly List<string> mapcooldown = [];
        private string nextmapname = "";
        private bool nextmappass = false;
        private bool canrtv = false;
        private bool firstmaprandom = false;
        private bool isrtving = false;
        private bool isrtv = false;
        private bool rtvwin = false;
        private bool isrtvagain = false;
        private int playercount = 0;
        private int rtvrequired = 0;
        private int timeleft = 0;
        private int extround = 0;
        private Timer? _canrtvtimer;
        private Timer? _maptimer;
        private Timer? _rtvtimer;
        private Timer? _endmaptimer;
        private Timer? _changemaprepeat;
        private Timer? _repeattimer;
        private readonly Random random = new();

        public override void Load(bool hotReload)
        {
            Logger.LogInformation("load maplist from {Path}", Path.Join(ModuleDirectory, "maplist.txt"));
            maplist = new List<string>(File.ReadAllLines(Path.Join(ModuleDirectory, "maplist.txt")));
            EmitSoundExtension.Init();

            if (hotReload)
            {
                Server.NextFrame(() =>
                {
                    mapcooldown.Clear();
                    mapcooldown.Add(Server.MapName);
                    rtvwin = false;
                    rtvcount.Clear();
                    extcount.Clear();
                    mapnominatelist.Clear();
                    votemaplist.Clear();
                    isrtv = false;
                    isrtving = false;
                    isrtvagain = false;
                    canrtv = true;
                    nextmappass = false;
                    firstmaprandom = true;
                    KillTimer();
                    timeleft = 15;
                    StartMaptimer();
                });
            }

            RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
        }
    }
}