using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    public enum GrappleRole { None, Attacker, Defender }

    /// All offensive/defensive actions for one wrestler. Reads MoveData; never
    /// hard-codes move stats. Works identically for human and CPU controllers.
    public class WrestlerCombat : MonoBehaviour
    {
        public const float GrappleRange = 1.25f;
        public const float StrikeRange = 1.35f;
        public const float GrappleTieWindow = 0.2f;

        WrestlerCore _core;
        Coroutine _moveRoutine;

        public MoveData CurrentMove { get; private set; }
        public float MoveElapsed { get; private set; }
        public string LastMoveName { get; private set; } = "-";
        public GrappleRole Role { get; private set; } = GrappleRole.None;
        public float GrappleLockStartTime { get; private set; }
        public float LastGrappleAttemptTime { get; private set; } = -99f;
        public float LastReversalTime { get; private set; } = -99f;
        public float LastDodgeTime { get; private set; } = -99f;
        public float ReversalCooldownUntil { get; set; }
        public float PendingKickoutPenalty { get; private set; }
        public float PendingKickoutPenaltyExpires { get; private set; }

        public event System.Action<MoveData> OnLandedHit;
        public event System.Action OnReversedOpponent;

        public CombatContextSnapshot LastContextSnapshot { get; private set; }
        public CombatContext CurrentContext =>
            CombatContextResolver.Resolve(_core, Opp);

        public void Bind(WrestlerCore core) => _core = core;

        /// Records every contextual move request for the F1 overlay and logs
        /// rejections; the single funnel for context diagnostics.
        void RecordContext(
            CombatContext context,
            GroundTargetZone zone,
            MoveDirection direction,
            string family,
            int candidates,
            MoveData selected,
            MoveValidationResult validation,
            bool fallback)
        {
            LastContextSnapshot = new CombatContextSnapshot(
                context, zone, direction, family, candidates,
                selected != null ? selected.displayName : "",
                selected != null ? selected.tier : MoveTier.Light,
                validation, fallback);

            if (!validation.IsValid)
                Debug.Log($"[Move] {_core.DisplayName} rejected {family}: " +
                          $"{validation.Reason} ({validation.DebugMessage})");
        }

        void Update()
        {
            // Safety net: if a grapple lock dissolved without a move (timeout,
            // stun, interrupt), clear the stale role so future grapples work.
            if (Role != GrappleRole.None && CurrentMove == null &&
                _core.States.Current != WrestlerState.GrappleLock &&
                _core.States.Current != WrestlerState.GrappleMoveStartup &&
                _core.States.Current != WrestlerState.GrappleMoveActive &&
                _core.States.Current != WrestlerState.Stunned)
            {
                Role = GrappleRole.None;
            }
        }

        WrestlerCore Opp => _core.Opponent;
        bool MatchActive => MatchManager.Instance == null || MatchManager.Instance.IsCombatAllowed;

        // ---------------- Strikes ----------------

        public bool TryLightStrike() => TryStrike(_core.Moveset != null ? _core.Moveset.RandomLightStrike() : null);
        public bool TryHeavyStrike() => TryStrike(_core.Moveset != null ? _core.Moveset.RandomHeavyStrike() : null);

        public bool TryRunningAttack()
        {
            var s = _core.States.Current;
            bool running = s == WrestlerState.Running || s == WrestlerState.RopeReboundRun || s == WrestlerState.RopeReboundReturn;
            if (!running) return false;
            return TryStrike(_core.Moveset != null ? _core.Moveset.RandomRunningAttack() : null);
        }

        bool TryStrike(MoveData move)
        {
            if (move == null || !MatchActive) return false;
            if (!_core.States.Profile.canAttack) return false;
            if (move.requiresRunning && !(_core.States.Current == WrestlerState.Running ||
                _core.States.Current == WrestlerState.RopeReboundRun || _core.States.Current == WrestlerState.RopeReboundReturn))
                return false;
            if (!_core.Stats.SpendStamina(move.staminaCost)) return false;
            _moveRoutine = StartCoroutine(StrikeRoutine(move));
            return true;
        }

        IEnumerator StrikeRoutine(MoveData move)
        {
            BeginMove(move);
            _core.Motor.FaceOpponent();
            _core.States.Set(WrestlerState.StrikeStartup, move.TotalDuration + 0.5f);
            _core.Anim.PlayMove(move.animationStateName, move.placeholderPoseName, move.animationSpeed);
            Debug.Log($"[Move] {_core.DisplayName} starts {move.displayName}");

            yield return Phase(move.startupTime);

            _core.States.Set(WrestlerState.StrikeActive, move.activeTime + 0.1f);
            bool hit = CheckHit(move);
            if (hit) ApplyHit(move);
            else Debug.Log($"[Move] {move.displayName} missed");

            yield return Phase(move.activeTime);

            _core.States.Set(WrestlerState.StrikeRecovery, move.recoveryTime);
            yield return Phase(move.recoveryTime);

            EndMove();
            _core.States.Set(WrestlerState.Idle);
        }

        IEnumerator Phase(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                MoveElapsed += Time.deltaTime;
                yield return null;
            }
        }

        void BeginMove(MoveData move)
        {
            CurrentMove = move;
            MoveElapsed = 0f;
            LastMoveName = move.displayName;
        }

        void EndMove()
        {
            CurrentMove = null;
            _moveRoutine = null;
        }

        bool CheckHit(MoveData move)
        {
            if (Opp == null) return false;
            if (!Opp.States.Profile.canBeStruck) return false;
            if (!HitboxProbe.InRange(transform, Opp.transform, move.range)) return false;
            if (HitboxProbe.FacingDot(transform, Opp.transform) < move.requiredFacingDot) return false;
            if (Opp.States.Current == WrestlerState.Dodging) return false;
            // Standing strikes whiff on downed targets (ground submissions handle grounded offense).
            if (Opp.States.IsDowned && move.category != MoveCategory.Submission) return false;
            return true;
        }

        public void ApplyHit(MoveData move)
        {
            var defender = Opp;
            float damage = CombatResolver.ScaleDamage(_core, move);
            defender.Stats.ApplyDamage(damage, _core);
            if (move.staminaDamage > 0f) defender.Stats.DrainStamina(move.staminaDamage);

            float momentum = move.momentumGainOnHit;
            _core.Stats.AddMomentum(momentum);
            if (_core.Traits != null && move.HasTag(MoveTag.Clean)) _core.Traits.OnCleanOffense(move);

            bool downs = move.causesDownedState ||
                (move.downsBelowHealthPercent > 0f && defender.Stats.HealthPercent * 100f < move.downsBelowHealthPercent);

            if (downs)
            {
                EnterDowned(defender, move.downedDuration > 0f ? move.downedDuration : 1.5f);
            }
            else
            {
                Vector3 dir = MathUtil.FlatDirection(transform.position, defender.transform.position);
                defender.Motor.ApplyKnockback(dir, move.knockbackDistance);
                ApplyPostKnockbackState(defender, dir, move);
            }

            Debug.Log($"[Move] {move.displayName} hit {defender.DisplayName} for {damage:0.#}");
            OnLandedHit?.Invoke(move);
        }

        /// After knockback, decide Stunned vs RopeStaggered vs Cornered.
        void ApplyPostKnockbackState(WrestlerCore defender, Vector3 dir, MoveData move)
        {
            var ring = RingInteractionSystem.Instance;
            Vector3 predicted = defender.transform.position + dir * move.knockbackDistance;

            if (ring != null && ring.NearestCornerZone(predicted) is CornerZone corner && corner.Contains(predicted))
            {
                defender.States.Set(WrestlerState.Cornered, 1.0f);
                defender.Anim.TriggerCornered();
                return;
            }

            if (ring != null && (move.causesRopeStagger ||
                ring.Bounds.DistanceToNearestRope(predicted, out _) <= 0.45f))
            {
                ApplyRopeStagger(defender);
                return;
            }

            if (move.stunDuration > 0f)
                defender.States.Set(WrestlerState.Stunned, move.stunDuration);
        }

        public void ApplyRopeStagger(WrestlerCore defender)
        {
            float duration = 0.9f;
            if (defender.Stats.StaminaPercent < 0.3f) duration += 0.35f;
            if (_core.Traits != null) duration += _core.Traits.OpponentRopeStaggerBonus;
            defender.States.Set(WrestlerState.RopeStaggered, duration);
            defender.Anim.TriggerRopeStagger();
            Debug.Log($"[Rope] {defender.DisplayName} is staggered on the ropes");
        }

        public void EnterDowned(WrestlerCore defender, float duration)
        {
            if (defender.Traits != null) duration = defender.Traits.ModifyDownedDuration(duration);
            duration /= Mathf.Max(0.25f, defender.Buffs.GetUpSpeedMult *
                (defender.Stats.Data != null ? defender.Stats.Data.getUpSpeed : 1f));
            defender.Combat.ForceRelease();
            defender.States.Set(WrestlerState.Downed, duration);
            defender.Anim.TriggerDowned();
        }

        // ---------------- Ground offense ----------------

        /// Positional attack on a downed defender. Zone (upper/lower) comes
        /// from the attacker's position along the defender's facing axis and
        /// selects the matching move family. Validation happens before any
        /// stamina is spent; side-on positions reject with WrongGroundZone.
        public bool TryGroundAttack()
        {
            if (!MatchActive || Opp == null || !Opp.States.IsDowned) return false;
            if (!_core.States.Profile.canAttack) return false;

            GroundTargetZone zone = CombatContextResolver.ResolveGroundZone(
                Opp.transform.position,
                Opp.transform.forward,
                transform.position,
                0.2f);
            bool lower = zone == GroundTargetZone.Lower;
            MoveData move = _core.Moveset != null
                ? (lower ? _core.Moveset.RandomGroundLowerAttack() : _core.Moveset.RandomGroundUpperAttack())
                : null;

            MoveValidationResult validation = ContextualMoveValidator.ValidateGround(
                move,
                Opp.States.IsDowned,
                zone,
                move != null && HitboxProbe.InRange(transform, Opp.transform, move.range),
                _core.Stats.Stamina);
            RecordContext(
                lower ? CombatContext.GroundLower : CombatContext.GroundUpper,
                zone, MoveDirection.Neutral, "GroundAttack",
                _core.Moveset != null
                    ? (lower ? _core.Moveset.groundLowerAttacks.Count : _core.Moveset.groundUpperAttacks.Count)
                    : 0,
                move, validation, false);
            if (!validation.IsValid) return false;
            if (!_core.Stats.SpendStamina(move.staminaCost)) return false;

            _moveRoutine = StartCoroutine(GroundAttackRoutine(move));
            return true;
        }

        IEnumerator GroundAttackRoutine(MoveData move)
        {
            BeginMove(move);
            _core.Motor.FaceOpponent();
            _core.States.Set(WrestlerState.StrikeStartup, move.TotalDuration + 0.5f);
            _core.Anim.PlayMove(move.animationStateName, move.placeholderPoseName, move.animationSpeed);
            Debug.Log($"[Move] {_core.DisplayName} starts {move.displayName}");

            yield return Phase(move.startupTime);

            _core.States.Set(WrestlerState.StrikeActive, move.activeTime + 0.1f);
            if (Opp != null && Opp.States.IsDowned &&
                HitboxProbe.InRange(transform, Opp.transform, move.range))
                ApplyGroundHit(move);
            else
                Debug.Log($"[Move] {move.displayName} missed");

            yield return Phase(move.activeTime);

            _core.States.Set(WrestlerState.StrikeRecovery, move.recoveryTime);
            yield return Phase(move.recoveryTime);

            EndMove();
            _core.States.Set(WrestlerState.Idle);
        }

        /// Ground hits bypass CheckHit (which rejects downed targets by design)
        /// and ApplyHit (whose non-knockdown branch would replace the
        /// defender's Downed state). The defender's existing downed timeout
        /// keeps running, so repeated ground attacks cannot pin someone to the
        /// mat forever.
        void ApplyGroundHit(MoveData move)
        {
            if (Opp == null || !Opp.States.IsDowned) return;

            float damage = CombatResolver.ScaleDamage(_core, move);
            Opp.Stats.ApplyDamage(damage, _core);
            if (move.staminaDamage > 0f) Opp.Stats.DrainStamina(move.staminaDamage);
            _core.Stats.AddMomentum(move.momentumGainOnHit);
            Debug.Log($"[Move] {move.displayName} hit grounded {Opp.DisplayName} for {damage:0.#}");
            OnLandedHit?.Invoke(move);
        }

        // ---------------- Grapples ----------------

        public bool TryGrappleAttempt()
        {
            if (!MatchActive || Opp == null) return false;
            if (Role != GrappleRole.None) return false;
            if (!_core.States.Profile.canGrapple) return false;
            if (!HitboxProbe.InRange(transform, Opp.transform, GrappleRange)) return false;

            // Recorded before the grabbable check so counter stances (Anuka) can
            // read whiffed grapple attempts against them.
            LastGrappleAttemptTime = Time.time;
            if (!Opp.States.Profile.canBeGrappled) return false;

            // Simultaneous tie-up: 40% timing / 40% stamina / 20% random (see CombatResolver).
            if (Time.time - Opp.Combat.LastGrappleAttemptTime <= GrappleTieWindow && Opp.Combat.Role == GrappleRole.None)
            {
                bool selfWins = CombatResolver.ResolveGrappleTie(_core, Opp, Opp.Combat.LastGrappleAttemptTime, LastGrappleAttemptTime);
                EnterGrappleLock(selfWins ? _core : Opp, selfWins ? Opp : _core);
                return true;
            }

            EnterGrappleLock(_core, Opp);
            return true;
        }

        static void EnterGrappleLock(WrestlerCore attacker, WrestlerCore defender)
        {
            attacker.Combat.Role = GrappleRole.Attacker;
            defender.Combat.Role = GrappleRole.Defender;
            attacker.Combat.GrappleLockStartTime = Time.time;
            defender.Combat.GrappleLockStartTime = Time.time;

            // Snap into the tie-up.
            Vector3 dir = MathUtil.FlatDirection(attacker.transform.position, defender.transform.position);
            defender.Motor.Teleport(attacker.transform.position + dir * 0.95f);
            attacker.Motor.FaceOpponent();
            defender.Motor.FaceOpponent();

            attacker.States.Set(WrestlerState.GrappleLock, 1.8f);
            defender.States.Set(WrestlerState.GrappleLock, 1.8f);
            // Both wrestlers reach into the collar-and-elbow tie-up.
            attacker.Anim.PlayMove("", "grapple");
            defender.Anim.PlayMove("", "grapple");
            Debug.Log($"[Grapple] {attacker.DisplayName} locks up with {defender.DisplayName}");
        }

        public bool InGrappleLockAsAttacker =>
            Role == GrappleRole.Attacker && _core.States.Current == WrestlerState.GrappleLock;

        public bool TryQuickGrappleFromLock() =>
            ExecuteGrappleMove(_core.Moveset != null ? _core.Moveset.RandomQuickGrapple() : null);

        public bool TryPowerGrappleFromLock() =>
            ExecuteGrappleMove(_core.Moveset != null ? _core.Moveset.RandomPowerGrapple() : null);

        // Directional selection lands with the directional-grapple milestone
        // task; until then these keep direction-aware callers playable.
        public bool TryQuickGrappleFromLock(MoveDirection direction) =>
            TryQuickGrappleFromLock();

        public bool TryPowerGrappleFromLock(MoveDirection direction) =>
            TryPowerGrappleFromLock();

        bool ExecuteGrappleMove(MoveData move)
        {
            if (move == null || !InGrappleLockAsAttacker || Opp == null) return false;

            if (move.requiresLift && !CombatResolver.ValidateLift(_core, Opp, move.minimumLiftClass, out string failReason))
            {
                if (move.fallbackMoveIfLiftFails != null) return ExecuteGrappleMove(move.fallbackMoveIfLiftFails);
                FailLift(failReason);
                return true; // the attempt happened, it just failed
            }

            if (!_core.Stats.SpendStamina(move.staminaCost))
            {
                ReleaseGrapple();
                return false;
            }

            _moveRoutine = StartCoroutine(GrappleMoveRoutine(move));
            return true;
        }

        void FailLift(string reason)
        {
            Debug.Log($"[Grapple] {_core.DisplayName} lift failed: {reason}");
            MatchHUD.TryShowMessage(reason ?? "Lift failed!");
            _core.Stats.DrainStamina(10f);
            ReleaseGrapple();
            _core.States.Set(WrestlerState.Stunned, 0.45f);
            if (Opp != null && Opp.Traits != null) Opp.Traits.NotifyLiftBlocked(_core);
        }

        IEnumerator GrappleMoveRoutine(MoveData move)
        {
            BeginMove(move);
            var defender = Opp;
            float total = move.TotalDuration;
            float startup = Mathf.Min(move.startupTime, total * 0.4f);

            _core.States.Set(WrestlerState.GrappleMoveStartup, total + 0.5f);
            defender.States.Set(WrestlerState.Stunned, total + 0.2f);
            _core.Anim.PlayMove(move.animationStateName, move.placeholderPoseName, move.animationSpeed);
            Debug.Log($"[Move] {_core.DisplayName} starts {move.displayName}");

            yield return Phase(startup);

            _core.States.Set(WrestlerState.GrappleMoveActive, total);
            ApplyHit(move);

            yield return Phase(total - startup);

            ClearGrappleRoles(defender);
            _core.States.Set(WrestlerState.GrappleMoveRecovery, move.recoveryTime > 0f ? move.recoveryTime : 0.4f);
            EndMove();
        }

        public void ReleaseGrapple()
        {
            var partner = Opp;
            ClearGrappleRoles(partner);
            if (_core.States.Current == WrestlerState.GrappleLock) _core.States.Set(WrestlerState.Idle);
            if (partner != null && partner.States.Current == WrestlerState.GrappleLock) partner.States.Set(WrestlerState.Idle);
        }

        void ClearGrappleRoles(WrestlerCore partner)
        {
            Role = GrappleRole.None;
            if (partner != null) partner.Combat.Role = GrappleRole.None;
        }

        // ---------------- Reversals ----------------

        public bool IsReversalWindowOpenFor(WrestlerCore defender)
        {
            if (CurrentMove == null) return false;
            float leniency = ReversalSystem.LeniencyFor(defender);
            return MoveElapsed >= CurrentMove.reversalWindowStart &&
                   MoveElapsed <= CurrentMove.reversalWindowEnd + leniency;
        }

        public bool TryReversal()
        {
            if (!MatchActive || Opp == null) return false;
            if (Time.time < ReversalCooldownUntil) return false;
            if (!_core.States.Profile.canReverse) return false;

            // Grapple lock escape (defender side, early window only).
            if (Role == GrappleRole.Defender && _core.States.Current == WrestlerState.GrappleLock)
            {
                float window = ReversalSystem.GrappleLockEscapeWindow + ReversalSystem.LeniencyFor(_core);
                if (Time.time - GrappleLockStartTime <= window)
                    return DoReversal(ReversalSystem.GrappleReversalCost, grappleEscape: true);
                return false;
            }

            // Special reversal.
            if (Opp.Specials != null && Opp.Specials.ReversalWindowOpen)
                return DoReversal(ReversalSystem.SpecialReversalCost, special: true);

            // Normal move reversal.
            if (Opp.Combat.IsReversalWindowOpenFor(_core))
                return DoReversal(ReversalSystem.CostFor(Opp.Combat.CurrentMove.category));

            ReversalCooldownUntil = Time.time + ReversalSystem.HumanReversalCooldown;
            return false;
        }

        bool DoReversal(float baseCost, bool grappleEscape = false, bool special = false)
        {
            float cost = ReversalSystem.StaminaCostFor(_core, baseCost);
            if (!_core.Stats.SpendStamina(cost)) return false;

            ReversalCooldownUntil = Time.time + ReversalSystem.HumanReversalCooldown;
            _core.States.Set(WrestlerState.Reversing, 0.45f);
            _core.Anim.TriggerReversal();

            if (special)
            {
                Opp.Specials.OnReversed(_core);
            }
            else if (grappleEscape)
            {
                ReleaseGrapple();
                Opp.States.Set(WrestlerState.Stunned, 0.8f);
                _core.Stats.AddMomentum(8f);
            }
            else
            {
                float momentum = Opp.Combat.CurrentMove != null ? Opp.Combat.CurrentMove.momentumGainOnReversal : 8f;
                Opp.Combat.InterruptByReversal(_core);
                _core.Stats.AddMomentum(momentum);
            }

            NotifyReversalSuccess();
            MatchHUD.TryShowMessage("Reversal!");
            Debug.Log($"[Reversal] {_core.DisplayName} reverses!");
            return true;
        }

        public void NotifyReversalSuccess()
        {
            LastReversalTime = Time.time;
            bool nearRopes = RingInteractionSystem.Instance != null &&
                             RingInteractionSystem.Instance.IsNearRope(_core, 1.2f);
            if (_core.Traits != null) _core.Traits.NotifyReversal(nearRopes);
            OnReversedOpponent?.Invoke();
        }

        public void InterruptByReversal(WrestlerCore reverser)
        {
            InterruptMove();
            _core.States.Set(WrestlerState.Stunned, 1.0f);
        }

        public void InterruptMove()
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = null;
            var partner = Opp;
            if (Role != GrappleRole.None) ClearGrappleRoles(partner);
            CurrentMove = null;
        }

        /// Full cleanup used on knockdowns, resets, and match end.
        public void ForceRelease()
        {
            InterruptMove();
        }

        public void NotifyDodged() => LastDodgeTime = Time.time;

        // ---------------- Pins / submissions ----------------

        public bool TryPin()
        {
            if (!MatchActive || Opp == null) return false;
            return PinSystem.Instance != null && PinSystem.Instance.TryStartPin(_core, Opp);
        }

        public bool TrySubmission()
        {
            if (!MatchActive || Opp == null) return false;
            var move = _core.Moveset != null ? _core.Moveset.RandomGroundSubmission() : null;
            if (move == null) return false;
            if (!Opp.States.Profile.canBeSubmitted) return false;
            if (!HitboxProbe.InRange(transform, Opp.transform, 1.2f)) return false;
            if (!_core.States.Profile.canAttack) return false;
            if (!_core.Stats.SpendStamina(move.staminaCost)) return false;
            return SubmissionSystem.Instance != null && SubmissionSystem.Instance.TryStart(
                _core, Opp, move.submissionPressurePerSecond, move.damagePerSecond, move.displayName);
        }

        public bool TrySpecial() => _core.Specials != null && _core.Specials.TryActivate();

        // ---------------- Kickout penalties from specials ----------------

        public void SetPendingKickoutPenalty(float penalty, float windowSeconds)
        {
            PendingKickoutPenalty = penalty;
            PendingKickoutPenaltyExpires = Time.time + windowSeconds;
        }

        public float ConsumeKickoutPenalty()
        {
            if (Time.time > PendingKickoutPenaltyExpires) return 0f;
            float p = PendingKickoutPenalty;
            PendingKickoutPenalty = 0f;
            return p;
        }
    }
}
