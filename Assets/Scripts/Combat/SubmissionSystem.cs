using UnityEngine;

namespace LoCoFight
{
    /// One submission hold at a time. Pressure vs escape race with rope breaks.
    /// Specials (armbar, crab, choke, tarantula-as-submission) feed in their own rates.
    public class SubmissionSystem : MonoBehaviour
    {
        public static SubmissionSystem Instance { get; private set; }

        public const float SubmitThreshold = 100f;
        public const float EscapeThreshold = 100f;

        public bool Active { get; private set; }
        public WrestlerCore Attacker { get; private set; }
        public WrestlerCore Defender { get; private set; }
        public float Pressure { get; private set; }
        public float Escape { get; private set; }
        public string HoldLabel { get; private set; } = "";

        float _pressureRate;
        float _damageRate;
        float _opponentStaminaDrain;
        float _selfStaminaDrain;
        bool _ignoreRopeBreak;
        bool _canWin;
        float _escapePenalty;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        MatchRulesData Rules => MatchManager.Instance.Rules;

        public bool TryStart(WrestlerCore attacker, WrestlerCore defender, float pressurePerSecond,
            float damagePerSecond, string label, float pressureBonus = 0f, float opponentStaminaDrain = 0f,
            float selfStaminaDrain = 0f, bool ignoreRopeBreak = false, bool canWin = true,
            float initialDamage = 0f, float escapePenalty = 0f)
        {
            if (Active) return false;
            if (MatchManager.Instance == null || !MatchManager.Instance.IsCombatAllowed) return false;
            if (Rules == null || !Rules.submissionsEnabled) return false;

            Active = true;
            Attacker = attacker;
            Defender = defender;
            HoldLabel = label;
            float subMod = attacker.Stats.Data != null ? attacker.Stats.Data.baseSubmissionModifier : 1f;
            _pressureRate = pressurePerSecond * subMod * (1f + pressureBonus);
            _damageRate = damagePerSecond;
            _opponentStaminaDrain = opponentStaminaDrain;
            _selfStaminaDrain = selfStaminaDrain;
            _ignoreRopeBreak = ignoreRopeBreak;
            _canWin = canWin;
            _escapePenalty = escapePenalty;
            Pressure = 0f;
            Escape = 0f;

            if (initialDamage > 0f) defender.Stats.ApplyDamage(initialDamage, attacker);

            attacker.Combat.ForceRelease();
            defender.Combat.ForceRelease();
            attacker.States.Set(WrestlerState.SubmissionApplying);
            defender.States.Set(WrestlerState.SubmissionDefending);
            attacker.Motor.Teleport(defender.transform.position +
                MathUtil.FlatDirection(defender.transform.position, attacker.transform.position) * 0.7f);

            MatchManager.Instance.SetState(MatchState.SubmissionInProgress);
            MatchHUD.TryShowMessage($"{attacker.DisplayName} locks in {label}!");
            Debug.Log($"[Submission] {label} started by {attacker.DisplayName}");
            return true;
        }

        public void AddPlayerEscapeEffort()
        {
            if (!Active || Defender == null || !Defender.IsPlayer) return;
            Escape += 4f * Defender.Buffs.SubmissionEscapeMult * (1f - _escapePenalty);
        }

        void Update()
        {
            if (!Active) return;
            float dt = Time.deltaTime;

            if (!_ignoreRopeBreak && Rules.RopeBreaksActive && RingInteractionSystem.Instance.IsInRopeBreak(Defender))
            {
                Release("Rope break!", ropeBreak: true);
                return;
            }

            // Pressure ramps slightly the longer the hold is maintained.
            float ramp = 1f + Mathf.Min(0.2f, Pressure / SubmitThreshold * 0.2f);
            Pressure += _pressureRate * ramp * dt;
            if (_damageRate > 0f) Defender.Stats.ApplyDamage(_damageRate * dt, Attacker);
            if (_opponentStaminaDrain > 0f) Defender.Stats.DrainStamina(_opponentStaminaDrain * dt);
            if (_selfStaminaDrain > 0f) Attacker.Stats.DrainStamina(_selfStaminaDrain * dt);

            if (!Defender.IsPlayer)
            {
                var s = Defender.Stats;
                float resistance = s.Data != null ? s.Data.submissionResistance : 0.5f;
                float diffBonus = MatchManager.Instance.CpuDifficulty != null ? MatchManager.Instance.CpuDifficulty.submissionEscapeBonus : 0f;
                float rate = (6f + 14f * s.StaminaPercent + 10f * resistance) * (1f + diffBonus) * (1f - _escapePenalty);
                Escape += rate * Defender.Buffs.SubmissionEscapeMult * dt;
            }

            if (Pressure >= SubmitThreshold)
            {
                if (_canWin)
                {
                    Active = false;
                    Debug.Log($"[Submission] {Defender.DisplayName} taps out to {HoldLabel}");
                    MatchManager.Instance.AnnounceWin(Attacker, WinCondition.Submission);
                }
                else
                {
                    Pressure = SubmitThreshold; // hold at max; rules say this hold can't win
                }
                return;
            }

            if (Escape >= EscapeThreshold)
            {
                MatchHUD.TryShowMessage("Submission escaped!");
                Debug.Log($"[Submission] {Defender.DisplayName} escapes {HoldLabel}");
                Defender.Stats.AddMomentum(6f);
                Release(null, ropeBreak: false);
            }
        }

        public void Release(string message, bool ropeBreak)
        {
            Active = false;
            if (!string.IsNullOrEmpty(message)) MatchHUD.TryShowMessage(message);
            if (ropeBreak) Debug.Log("[Submission] Rope break!");
            if (Attacker != null) Attacker.States.Set(WrestlerState.GrappleMoveRecovery, 0.8f);
            if (Defender != null) Defender.States.Set(WrestlerState.Downed, 1.0f);
            if (MatchManager.Instance.State == MatchState.SubmissionInProgress)
                MatchManager.Instance.SetState(MatchState.Active);
            Attacker = null;
            Defender = null;
        }

        public void CancelIfActive()
        {
            if (Active) Release(null, false);
        }
    }
}
