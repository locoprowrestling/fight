using UnityEngine;

namespace LoCoFight
{
    /// One submission hold at a time. Pressure vs escape race with rope
    /// breaks, plus positional defense: defender movement becomes crawl
    /// intent that drags the attached pair toward the nearest rope.
    /// Specials (armbar, crab, choke, tarantula-as-submission) feed in their
    /// own rates.
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

        /// F1 diagnostics, refreshed every update while a hold is active.
        public float LastCrawlRate { get; private set; }
        public float LastRopeDistance { get; private set; }

        float _pressureRate;
        float _damageRate;
        float _opponentStaminaDrain;
        float _selfStaminaDrain;
        bool _ignoreRopeBreak;
        bool _canWin;
        float _escapePenalty;
        Vector3 _crawlIntent;

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
            _crawlIntent = Vector3.zero;
            LastCrawlRate = 0f;

            if (initialDamage > 0f) defender.Stats.ApplyDamage(initialDamage, attacker);

            attacker.Combat.ForceRelease();
            defender.Combat.ForceRelease();
            attacker.States.Set(WrestlerState.SubmissionApplying);
            defender.States.Set(WrestlerState.SubmissionDefending);
            attacker.Motor.Teleport(defender.transform.position +
                MathUtil.FlatDirection(defender.transform.position, attacker.transform.position) * 0.7f);

            // The hold owns both gameplay roots until a release path runs.
            attacker.Motor.SetScriptedControl(true);
            defender.Motor.SetScriptedControl(true);

            MatchManager.Instance.SetState(MatchState.SubmissionInProgress);
            MatchHUD.TryShowMessage($"{attacker.DisplayName} locks in {label}!");
            Debug.Log($"[Submission] {label} started by {attacker.DisplayName}");
            return true;
        }

        /// Defender movement becomes crawl intent while the hold is active.
        /// Shared by the player controller and the CPU AI.
        public void SetDefenderCrawlIntent(WrestlerCore defender, Vector3 worldIntent)
        {
            if (!Active || defender != Defender) return;
            _crawlIntent = MathUtil.Flat(worldIntent);
            if (_crawlIntent.sqrMagnitude > 1f) _crawlIntent.Normalize();
        }

        public void ClearDefenderCrawlIntent(WrestlerCore defender)
        {
            if (defender == Defender) _crawlIntent = Vector3.zero;
        }

        /// Shared mash-effort API for human and CPU defenders.
        public void AddEscapeEffort(WrestlerCore defender)
        {
            if (!Active || defender == null || defender != Defender) return;
            Escape += SubmissionEscapeRules.ActiveEscapePerPress(
                defender.Stats.StaminaPercent,
                defender.Buffs.SubmissionEscapeMult,
                _escapePenalty);
        }

        void Update()
        {
            if (!Active) return;
            float dt = Time.deltaTime;

            bool ropeBreaksActive = !_ignoreRopeBreak && Rules != null && Rules.RopeBreaksActive;

            UpdateCrawl(dt, ropeBreaksActive);

            if (ropeBreaksActive && RingInteractionSystem.Instance != null &&
                RingInteractionSystem.Instance.IsInRopeBreak(Defender))
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

            if (Pressure >= SubmitThreshold)
            {
                if (_canWin)
                {
                    var winner = Attacker;
                    Debug.Log($"[Submission] {Defender.DisplayName} taps out to {HoldLabel}");
                    // Cleanup first so AnnounceWin never inherits stale
                    // submission ownership or scripted control.
                    Release(null, ropeBreak: false, tapOut: true);
                    MatchManager.Instance.AnnounceWin(winner, WinCondition.Submission);
                }
                else
                {
                    Pressure = SubmitThreshold; // hold at max; rules say this hold can't win
                }
                return;
            }

            if (Escape >= EscapeThreshold)
            {
                Debug.Log($"[Submission] {Defender.DisplayName} escapes {HoldLabel}");
                Defender.Stats.AddMomentum(6f);
                Release("Submission escaped!", ropeBreak: false);
            }
        }

        /// Converts crawl intent into paired movement toward the nearest
        /// rope. Missing rope geometry disables crawl safely; intent away
        /// from the rope produces no movement and no stamina drain.
        void UpdateCrawl(float dt, bool ropeBreaksActive)
        {
            LastCrawlRate = 0f;
            var ring = RingInteractionSystem.Instance;
            if (ring == null || ring.Bounds == null)
            {
                LastRopeDistance = -1f;
                return;
            }

            var info = ring.GetNearestRopeContactInfo(Defender);
            LastRopeDistance = info.distanceToRope;

            if (_crawlIntent.sqrMagnitude < 0.0001f) return;

            Vector3 toRope = MathUtil.Flat(info.contactPoint - Defender.transform.position);
            if (toRope.sqrMagnitude < 0.0001f) return;
            toRope.Normalize();

            float dot = Vector3.Dot(_crawlIntent.normalized, toRope);
            float quality = SubmissionEscapeRules.DirectionQuality(dot);
            if (quality <= 0f) return;

            float resistance = Defender.Stats.Data != null ? Defender.Stats.Data.submissionResistance : 0.5f;
            float rate = SubmissionEscapeRules.CrawlRate(
                quality, Defender.Stats.StaminaPercent, resistance);
            if (rate <= 0f) return;

            LastCrawlRate = rate;
            Defender.Stats.DrainStamina(SubmissionEscapeRules.CrawlStaminaPerSecond * quality * dt);

            // One shared delta keeps the pair attached; the defender's clamp
            // result decides the movement so the hold never separates at the
            // boundary. Ring legality stays owned by RingBoundary.
            Vector3 defenderPos = Defender.transform.position;
            Vector3 attackerPos = Attacker.transform.position;
            Vector3 clampedTarget = ring.Bounds.ClampInside(defenderPos + toRope * rate * dt);
            Vector3 delta = clampedTarget - defenderPos;
            SubmissionEscapeRules.ApplyPairDelta(ref attackerPos, ref defenderPos, delta);
            Defender.Motor.Teleport(defenderPos);
            Attacker.Motor.Teleport(ring.Bounds.ClampInside(attackerPos));

            // With rope breaks disabled, position alone cannot end the hold,
            // so crawl effort converts into reduced escape meter instead.
            Escape += SubmissionEscapeRules.CrawlEscapeRate(quality, ropeBreaksActive) * dt;
        }

        public void Release(string message, bool ropeBreak) =>
            Release(message, ropeBreak, tapOut: false);

        /// The single cleanup path: escape, rope break, tap-out, cancel,
        /// reset, and match end all come through here exactly once.
        void Release(string message, bool ropeBreak, bool tapOut)
        {
            var attacker = Attacker;
            var defender = Defender;

            Active = false;
            _crawlIntent = Vector3.zero;
            LastCrawlRate = 0f;

            if (attacker != null) attacker.Motor.SetScriptedControl(false);
            if (defender != null) defender.Motor.SetScriptedControl(false);

            if (!string.IsNullOrEmpty(message)) MatchHUD.TryShowMessage(message);
            if (ropeBreak) Debug.Log("[Submission] Rope break!");

            // Tap-out leaves state changes to AnnounceWin (Victory/Defeat).
            if (!tapOut)
            {
                if (attacker != null) attacker.States.Set(WrestlerState.GrappleMoveRecovery, 0.8f);
                if (defender != null) defender.States.Set(WrestlerState.Downed, 1.0f);
            }
            if (MatchManager.Instance != null &&
                MatchManager.Instance.State == MatchState.SubmissionInProgress)
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
