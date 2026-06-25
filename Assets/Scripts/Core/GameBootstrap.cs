using UnityEngine;

namespace LoCoFight
{
    /// One-component scene bootstrap. Drop this on an empty GameObject in an
    /// empty scene, press Play, and the entire prototype builds itself:
    /// arena, systems, wrestlers, camera, light, and HUD.
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Optional asset overrides (in-memory defaults used when empty)")]
        public RosterDatabase rosterDatabase;
        public MatchRulesData matchRules;
        public AIDifficultyData cpuDifficulty;
        public string defaultPlayerRosterId = "tas-zeak-gallent";
        public string defaultCpuRosterId = "tas-jt-staten";

        void Awake()
        {
            // 1. Arena + ring interaction queries.
            var arena = FindObjectOfType<ArenaRig>();
            if (arena == null) arena = ArenaRig.BuildPrimitiveArena();

            var ringGo = new GameObject("RingInteractionSystem");
            ringGo.AddComponent<RingInteractionSystem>().Init(arena);

            // Visible 2D ring (gameplay zones come from ArenaRig).
            Arena2DBackdrop.Build(4f);

            // 2. Data: prefer assigned/saved assets, fall back to code defaults.
            DefaultGameDataSet fallback = null;
            var database = rosterDatabase != null ? rosterDatabase : RosterLoader.LoadDatabase(out fallback);
            var rules = matchRules != null ? matchRules
                : Resources.Load<MatchRulesData>("LoCoData/StandardMatchRules");
            var difficulty = cpuDifficulty != null ? cpuDifficulty
                : Resources.Load<AIDifficultyData>("LoCoData/NormalDifficulty");
            if (rules == null) rules = fallback != null ? fallback.standardRules : DefaultGameData.CreateAll().standardRules;
            if (difficulty == null)
            {
                if (fallback == null) fallback = DefaultGameData.CreateAll();
                difficulty = fallback.normal;
            }

            // 3. Match systems.
            var managers = new GameObject("MatchManagers");
            var mm = managers.AddComponent<MatchManager>();
            managers.AddComponent<PinSystem>();
            managers.AddComponent<SubmissionSystem>();
            managers.AddComponent<RefereeCountSystem>();
            managers.AddComponent<DebugOverlay>();
            managers.AddComponent<RosterSelectDebug>();

            mm.rosterDatabase = database;
            mm.matchRules = rules;
            mm.cpuDifficulty = difficulty;
            mm.defaultPlayerRosterId = RosterSelectDebug.SelectedPlayerId ?? defaultPlayerRosterId;
            mm.defaultCpuRosterId = RosterSelectDebug.SelectedCpuId ?? defaultCpuRosterId;

            // 4. Input.
            new GameObject("PlayerInput").AddComponent<PlayerInputController>();

            // 5. Camera + light.
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                cam = camGo.GetComponent<UnityEngine.Camera>();
            }
            var rig = cam.GetComponent<TwoTargetMatchCamera>();
            if (rig == null) rig = cam.gameObject.AddComponent<TwoTargetMatchCamera>();

            cam.orthographic = true;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Unlit sprites need no scene light in 2D.

            // 6. HUD.
            MatchHUD.CreateHud();

            // 7. Spawn wrestlers and start the match flow.
            mm.SetupMatch(arena);
            rig.SetTargets(mm.Player.transform, mm.Cpu.transform);

            Debug.Log("[Bootstrap] Prototype match initialized");
        }
    }
}
