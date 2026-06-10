using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoCoFight
{
    public enum HandshakeResponse { Accept, Refuse, Ignore, CheapShot }

    /// Owns match flow: setup, handshake ritual, active play, win/loss, reset.
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        [Header("Configuration")]
        public RosterDatabase rosterDatabase;
        public string defaultPlayerRosterId = "tas-zeak-gallent";
        public string defaultCpuRosterId = "tas-jt-staten";
        public MatchRulesData matchRules;
        public AIDifficultyData cpuDifficulty;

        public MatchState State { get; private set; } = MatchState.Loading;
        public WrestlerCore Player { get; private set; }
        public WrestlerCore Cpu { get; private set; }
        public RefereeAttentionState RefereeAttention { get; private set; } = RefereeAttentionState.Normal;
        public WrestlerCore Winner { get; private set; }
        public WinCondition WinningCondition { get; private set; }

        public MatchRulesData Rules => matchRules;
        public AIDifficultyData CpuDifficulty => cpuDifficulty;
        public bool IsCombatAllowed => State == MatchState.Active;

        ArenaRig _arena;
        bool _awaitingHandshakeInput;
        WrestlerCore _handshakeOfferer;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetState(MatchState state)
        {
            State = state;
            MatchHUD.TrySetMatchState(state.ToString());
        }

        public void SetupMatch(ArenaRig arena)
        {
            _arena = arena;
            SetState(MatchState.Loading);

            var playerEntry = rosterDatabase != null ? rosterDatabase.Find(defaultPlayerRosterId) : null;
            var cpuEntry = rosterDatabase != null ? rosterDatabase.Find(defaultCpuRosterId) : null;
            if (playerEntry == null) Debug.LogWarning($"[Match] Player roster id '{defaultPlayerRosterId}' not found — using placeholder");
            if (cpuEntry == null) Debug.LogWarning($"[Match] CPU roster id '{defaultCpuRosterId}' not found — using placeholder");

            Player = WrestlerCore.Create("PlayerWrestler", playerEntry, true, arena.playerSpawn.position, new Color(0.2f, 0.5f, 1f));
            Cpu = WrestlerCore.Create("CPUWrestler", cpuEntry, false, arena.cpuSpawn.position, new Color(1f, 0.3f, 0.2f));
            Player.SetOpponent(Cpu);
            Cpu.SetOpponent(Player);
            Player.Motor.FaceOpponent();
            Cpu.Motor.FaceOpponent();

            var ai = Cpu.gameObject.AddComponent<CPUWrestlerAI>();
            ai.Bind(Cpu, cpuDifficulty);

            var input = FindObjectOfType<PlayerInputController>();
            if (input != null) input.Bind(Player);

            if (MatchHUD.Instance != null) MatchHUD.Instance.BindWrestlers(Player, Cpu);

            StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            SetState(MatchState.Ready);
            MatchHUD.TryShowMessage($"{Player.DisplayName} vs {Cpu.DisplayName}", 2f);
            yield return new WaitForSeconds(1.5f);

            // Honorable Handshake (Zeak Gallent) runs before the bell.
            WrestlerCore offerer = null;
            if (Player.Traits != null && Player.Traits.HasHandshakeRitual) offerer = Player;
            else if (Cpu.Traits != null && Cpu.Traits.HasHandshakeRitual) offerer = Cpu;

            if (offerer != null)
                yield return HandshakeRoutine(offerer);

            SetState(MatchState.Active);
            MatchHUD.TryShowMessage("Fight!", 1.5f);
            Debug.Log("[Match] Fight!");
        }

        IEnumerator HandshakeRoutine(WrestlerCore offerer)
        {
            SetState(MatchState.HandshakeSequence);
            _handshakeOfferer = offerer;
            var responder = offerer.Opponent;
            MatchHUD.TryShowMessage($"{offerer.DisplayName} offers a handshake...", 2.5f);
            yield return new WaitForSeconds(0.8f);

            HandshakeResponse response;
            if (responder.IsPlayer)
            {
                // Player chooses: T = accept, J = cheap shot, L = refuse, or ignore.
                _awaitingHandshakeInput = true;
                MatchHUD.TryShowMessage("T: shake  J: cheap shot  L: refuse", 3f);
                float deadline = Time.time + 3f;
                _playerHandshakeChoice = null;
                while (Time.time < deadline && _playerHandshakeChoice == null) yield return null;
                _awaitingHandshakeInput = false;
                response = _playerHandshakeChoice ?? HandshakeResponse.Ignore;
            }
            else
            {
                // CPU picks based on a weighted roll.
                float roll = Random.value;
                response = roll < 0.5f ? HandshakeResponse.Accept
                    : roll < 0.7f ? HandshakeResponse.Refuse
                    : roll < 0.85f ? HandshakeResponse.Ignore
                    : HandshakeResponse.CheapShot;
                yield return new WaitForSeconds(0.7f);
            }

            ApplyHandshakeOutcome(offerer, responder, response);
            yield return new WaitForSeconds(1.0f);
            _handshakeOfferer = null;
        }

        HandshakeResponse? _playerHandshakeChoice;

        public void HandshakeRespond(HandshakeResponse response)
        {
            if (_awaitingHandshakeInput) _playerHandshakeChoice = response;
        }

        void ApplyHandshakeOutcome(WrestlerCore offerer, WrestlerCore responder, HandshakeResponse response)
        {
            switch (response)
            {
                case HandshakeResponse.Accept:
                    offerer.Stats.AddMomentum(5f);
                    responder.Stats.AddMomentum(5f);
                    MatchHUD.TryShowMessage("Respect shown.");
                    break;
                case HandshakeResponse.Refuse:
                    offerer.Stats.AddMomentum(8f);
                    MatchHUD.TryShowMessage("Handshake refused.");
                    break;
                case HandshakeResponse.Ignore:
                    offerer.Stats.AddMomentum(3f);
                    MatchHUD.TryShowMessage("No response.");
                    break;
                case HandshakeResponse.CheapShot:
                    offerer.Stats.ApplyDamage(5f, responder);
                    offerer.States.Set(WrestlerState.Stunned, 0.45f);
                    offerer.Buffs.Apply(SpecialExecutor.BuildEffect("honor-tested", 8f));
                    MatchHUD.TryShowMessage("Cheap shot!");
                    break;
            }
            Debug.Log($"[Match] Handshake outcome: {response}");
        }

        public void DistractReferee(float duration) => StartCoroutine(DistractRoutine(duration));

        IEnumerator DistractRoutine(float duration)
        {
            RefereeAttention = RefereeAttentionState.Distracted;
            Debug.Log("[Referee] Distracted!");
            yield return new WaitForSeconds(duration);
            RefereeAttention = RefereeAttentionState.Normal;
        }

        public void AnnounceWin(WrestlerCore winner, WinCondition condition)
        {
            if (State == MatchState.Finished) return;
            Winner = winner;
            WinningCondition = condition;
            SetState(MatchState.Finished);

            PinSystem.Instance.CancelIfActive();
            SubmissionSystem.Instance.CancelIfActive();
            RefereeCountSystem.Instance.Cancel();

            var loser = winner.Opponent;
            winner.Combat.ForceRelease();
            loser.Combat.ForceRelease();
            winner.Motor.SetScriptedControl(false);
            loser.Motor.SetScriptedControl(false);
            winner.States.Set(WrestlerState.Victory);
            loser.States.Set(WrestlerState.Defeat);

            string method = condition == WinCondition.Pinfall ? "Pinfall" : "Submission";
            string who = winner.IsPlayer ? "Player" : "CPU";
            MatchHUD.TryShowWinner($"{who} Wins by {method}! ({winner.DisplayName})  —  Press R to Reset");
            Debug.Log($"[Match] {winner.DisplayName} wins by {method}");
        }

        public void RequestReset()
        {
            SetState(MatchState.Resetting);
            Debug.Log("[Match] Resetting");
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
            else
            {
                // Untitled / not-in-build scenes can't be reloaded: respawn in place.
                SoftReset();
            }
        }

        void SoftReset()
        {
            PinSystem.Instance.CancelIfActive();
            SubmissionSystem.Instance.CancelIfActive();
            RefereeCountSystem.Instance.Cancel();
            StopAllCoroutines();

            if (Player != null) Destroy(Player.gameObject);
            if (Cpu != null) Destroy(Cpu.gameObject);
            Winner = null;
            WinningCondition = WinCondition.None;

            defaultPlayerRosterId = RosterSelectDebug.SelectedPlayerId ?? defaultPlayerRosterId;
            defaultCpuRosterId = RosterSelectDebug.SelectedCpuId ?? defaultCpuRosterId;
            MatchHUD.TryShowWinner("");
            MatchHUD.TryShowCount("");

            SetupMatch(_arena);
            var cam = FindObjectOfType<TwoTargetMatchCamera>();
            if (cam != null) cam.SetTargets(Player.transform, Cpu.transform);
        }
    }
}
