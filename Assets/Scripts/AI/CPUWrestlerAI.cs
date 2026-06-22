using UnityEngine;

namespace LoCoFight
{
    /// Finite-state CPU controller. Consumes the exact same WrestlerCombat API
    /// as the human player.
    public class CPUWrestlerAI : MonoBehaviour
    {
        public AIState CurrentState { get; private set; } = AIState.IdleThink;

        WrestlerCore _core;
        AIDifficultyData _difficulty;
        readonly AIMemory _memory = new AIMemory();

        float _nextDecisionAt;
        float _reversalCooldownUntil;
        Vector3? _moveTarget;

        public void Bind(WrestlerCore core, AIDifficultyData difficulty)
        {
            _core = core;
            _difficulty = difficulty;
        }

        public void ResetForMatch()
        {
            _memory.Clear();
            CurrentState = AIState.IdleThink;
            _moveTarget = null;
        }

        WrestlerCore Opp => _core != null ? _core.Opponent : null;

        void Update()
        {
            if (_core == null || Opp == null || _difficulty == null) return;
            var mm = MatchManager.Instance;
            if (mm == null) return;

            if (mm.State == MatchState.Finished)
            {
                CurrentState = _core.States.Current == WrestlerState.Victory ? AIState.Victory : AIState.Defeat;
                _core.Motor.SetMoveInput(Vector3.zero, false);
                return;
            }
            if (!mm.IsCombatAllowed)
            {
                _core.Motor.SetMoveInput(Vector3.zero, false);
                return;
            }

            ReactDefensively();

            if (Time.time >= _nextDecisionAt)
            {
                Decide();
                _nextDecisionAt = Time.time + Random.Range(_difficulty.reactionDelayMin, _difficulty.reactionDelayMax);
            }

            Act();
        }

        // ---------------- Defense (reaction-time gated) ----------------

        void ReactDefensively()
        {
            if (Time.time < _reversalCooldownUntil) return;

            // Reverse incoming strikes/grapples/specials.
            bool windowOpen = Opp.Combat.IsReversalWindowOpenFor(_core) ||
                              (Opp.Specials != null && Opp.Specials.ReversalWindowOpen);
            if (windowOpen && _core.States.Profile.canReverse)
            {
                _reversalCooldownUntil = Time.time + _difficulty.reversalCooldown;
                if (Random.value < _difficulty.reversalAccuracy)
                {
                    CurrentState = AIState.DefensiveReversal;
                    _core.Combat.TryReversal();
                }
                return;
            }

            // Grapple lock escape as defender.
            if (_core.Combat.Role == GrappleRole.Defender &&
                _core.States.Current == WrestlerState.GrappleLock &&
                Random.value < _difficulty.reversalAccuracy * 0.5f * Time.deltaTime * 10f)
            {
                _core.Combat.TryReversal();
                return;
            }

            // Vanishing Dodge (The Vigilante as CPU) against incoming specials/lifts.
            if (_core.Dodge != null && _core.Dodge.HasVanishingDodge &&
                Opp.Specials != null && Opp.Specials.IsExecuting &&
                Random.value < _difficulty.dodgeAccuracy * Time.deltaTime * 8f)
            {
                CurrentState = AIState.DefensiveDodge;
                _core.Dodge.TryDodge();
                return;
            }

            // Roll away from incoming aerials while downed.
            if (_core.States.IsDowned && Opp.Specials != null && Opp.Specials.IsExecuting &&
                (Opp.States.Current == WrestlerState.TurnbuckleClimb || Opp.States.Current == WrestlerState.AerialSetup))
            {
                if (Random.value < _difficulty.dodgeAccuracy * Time.deltaTime * 5f)
                {
                    CurrentState = AIState.DefensiveDodge;
                    RollAway();
                }
            }
        }

        void RollAway()
        {
            Vector3 side = Vector3.Cross(Vector3.up,
                MathUtil.FlatDirection(transform.position, Opp.transform.position));
            if (Random.value > 0.5f) side = -side;
            var ring = RingInteractionSystem.Instance;
            Vector3 target = transform.position + side * 1.5f;
            if (ring != null) target = ring.Bounds.ClampInside(target);
            _core.States.Set(WrestlerState.RollingAway, 0.5f);
            _core.Motor.Teleport(target);
            Debug.Log($"[AI] {_core.DisplayName} rolls away");
        }

        // ---------------- Offense ----------------

        void Decide()
        {
            _moveTarget = null;

            if (!_core.States.Profile.canAttack)
            {
                CurrentState = _core.States.IsDowned ? AIState.GetUp : AIState.Recover;
                return;
            }

            // Stamina caution: back off and breathe.
            if (_core.Stats.StaminaPercent < _difficulty.staminaCautionThreshold)
            {
                CurrentState = AIState.BackOff;
                return;
            }

            // Grapple lock attacker: pick a follow-up.
            if (_core.Combat.InGrappleLockAsAttacker)
            {
                CurrentState = AIState.ChooseGrappleMove;
                return;
            }

            // Special: fire it or set it up.
            if (_core.Stats.HasFullMomentum && Random.value < _difficulty.specialSetupPreference)
            {
                CurrentState = AIState.AttemptSpecial;
                return;
            }

            // Opponent downed: pin, submit, or wait for them to rise.
            if (Opp.States.IsDowned)
            {
                bool hurtEnough = Opp.Stats.HealthPercent <= _difficulty.pinAttemptThreshold;
                if (hurtEnough && _memory.CanUse("pin", 3f))
                {
                    CurrentState = AIState.AttemptPin;
                    return;
                }
                if (Opp.Stats.HealthPercent <= _difficulty.submissionAttemptThreshold &&
                    _core.Stats.StaminaPercent > 0.35f && _memory.CanUse("submission", 6f))
                {
                    CurrentState = AIState.AttemptSubmission;
                    return;
                }
                CurrentState = AIState.Circle;
                return;
            }

            float dist = _core.DistanceToOpponent();
            float roll = Random.value;

            if (dist > 4.5f)
            {
                CurrentState = roll < _difficulty.ropeStrategyPreference ? AIState.UseRopeRebound : AIState.Approach;
            }
            else if (dist > 2.2f)
            {
                if (roll < _difficulty.aggression * 0.5f) CurrentState = AIState.AttemptRunningAttack;
                else if (roll < _difficulty.aggression) CurrentState = AIState.Approach;
                else CurrentState = AIState.Circle;
            }
            else
            {
                // Close range: strike, grapple, or herd toward ropes/corner.
                if (Opp.States.Current == WrestlerState.RopeStaggered || Opp.States.Current == WrestlerState.Cornered)
                {
                    CurrentState = roll < 0.5f ? AIState.AttemptHeavyStrike : AIState.AttemptGrapple;
                }
                else if (roll < _difficulty.strikePreference * 0.6f && _memory.CanUse("light", 0.6f))
                    CurrentState = AIState.AttemptLightStrike;
                else if (roll < _difficulty.strikePreference && _memory.CanUse("heavy", 1.4f))
                    CurrentState = AIState.AttemptHeavyStrike;
                else if (roll < _difficulty.strikePreference + _difficulty.grapplePreference && _memory.CanUse("grapple", 1.2f))
                    CurrentState = AIState.AttemptGrapple;
                else if (roll < 0.9f)
                    CurrentState = AIState.ForceOpponentToRopes;
                else
                    CurrentState = AIState.Circle;
            }
        }

        void Act()
        {
            switch (CurrentState)
            {
                case AIState.Approach:
                    MoveToward(Opp.transform.position, run: _core.DistanceToOpponent() > 3f);
                    break;

                case AIState.Circle:
                    Circle();
                    break;

                case AIState.BackOff:
                    MoveToward(transform.position + MathUtil.FlatDirection(Opp.transform.position, transform.position) * 2f, false);
                    break;

                case AIState.AttemptLightStrike:
                    if (InRange(WrestlerCombat.StrikeRange)) { _memory.Note("light"); _core.Combat.TryLightStrike(); Rethink(); }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.AttemptHeavyStrike:
                    if (InRange(WrestlerCombat.StrikeRange)) { _memory.Note("heavy"); _core.Combat.TryHeavyStrike(); Rethink(); }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.AttemptGrapple:
                    if (InRange(WrestlerCombat.GrappleRange)) { _memory.Note("grapple"); _core.Combat.TryGrappleAttempt(); Rethink(); }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.ChooseGrappleMove:
                    bool power = Random.value < _difficulty.grapplePreference && _core.Stats.StaminaPercent > 0.3f;
                    if (power) _core.Combat.TryPowerGrappleFromLock();
                    else _core.Combat.TryQuickGrappleFromLock();
                    Rethink();
                    break;

                case AIState.AttemptRunningAttack:
                case AIState.UseRopeRebound:
                    MoveToward(Opp.transform.position, run: true);
                    if (InRange(WrestlerCombat.StrikeRange + 0.3f)) { _core.Combat.TryRunningAttack(); Rethink(); }
                    break;

                case AIState.ForceOpponentToRopes:
                case AIState.AttemptCornerSetup:
                    // Keep striking at close range — knockback pushes them toward ropes/corners.
                    if (InRange(WrestlerCombat.StrikeRange)) { _core.Combat.TryLightStrike(); Rethink(); }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.AttemptSpecial:
                    if (AISpecialPlanner.PlanOrExecute(_core, Opp, out var setupTarget)) { Rethink(); }
                    else if (setupTarget.HasValue) MoveToward(setupTarget.Value, false);
                    else Circle();
                    break;

                case AIState.AttemptPin:
                    if (InRange(1.1f)) { _memory.Note("pin"); _core.Combat.TryPin(); Rethink(); }
                    else MoveToward(Opp.transform.position, run: true);
                    break;

                case AIState.AttemptSubmission:
                    if (InRange(1.1f)) { _memory.Note("submission"); _core.Combat.TrySubmission(); Rethink(); }
                    else MoveToward(Opp.transform.position, false);
                    break;

                default:
                    _core.Motor.SetMoveInput(Vector3.zero, false);
                    break;
            }
        }

        bool InRange(float range) => _core.DistanceToOpponent() <= range;

        void Rethink() => _nextDecisionAt = Time.time + Random.Range(_difficulty.reactionDelayMin, _difficulty.reactionDelayMax);

        void MoveToward(Vector3 target, bool run)
        {
            Vector3 dir = MathUtil.FlatDirection(transform.position, target);
            if (MathUtil.FlatDistance(transform.position, target) < 0.15f) dir = Vector3.zero;

            // 2D lane bias: if off the opponent's lane, prioritize closing depth so
            // strikes can land in the side-on view.
            float zSelf = transform.position.z;
            float zOpp = _core.Opponent != null ? _core.Opponent.transform.position.z : zSelf;
            if (!LaneSystem.LanesAligned(zSelf, zOpp, LaneSystem.StrikeAlignmentTolerance))
            {
                dir.z = Mathf.Sign(zOpp - zSelf);
                dir.x *= 0.4f; // ease off horizontal until aligned
            }

            _core.Motor.SetMoveInput(dir, run);
        }

        void Circle()
        {
            Vector3 toOpp = MathUtil.FlatDirection(transform.position, Opp.transform.position);
            Vector3 side = Vector3.Cross(Vector3.up, toOpp);
            _core.Motor.SetMoveInput((side + toOpp * 0.1f).normalized, false);
        }
    }
}
