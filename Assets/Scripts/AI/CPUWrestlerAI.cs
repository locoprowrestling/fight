using UnityEngine;

namespace LoCoFight
{
    /// Debug pacing for manual testing (F3 cycles): Full = normal AI,
    /// NoOffense = defends/escapes but never initiates, Dummy = stands there.
    public enum CpuBehaviorMode { Full, NoOffense, Dummy }

    /// Finite-state CPU controller. Consumes the exact same WrestlerCombat API
    /// as the human player.
    public class CPUWrestlerAI : MonoBehaviour
    {
        public AIState CurrentState { get; private set; } = AIState.IdleThink;
        public CpuBehaviorMode BehaviorMode { get; private set; } = CpuBehaviorMode.Full;

        WrestlerCore _core;
        AIDifficultyData _difficulty;
        readonly AIMemory _memory = new AIMemory();
        AIPersonalityProfile _personality = AIPersonalityProfiles.Balanced;
        AIPersonality _personalityKind = AIPersonality.Balanced;

        float _nextDecisionAt;
        float _reversalCooldownUntil;

        // F1 diagnostics: what the last weighted decision saw and chose.
        public AIPersonality PersonalityKind => _personalityKind;
        public string LastSelectedFamily { get; private set; } = "-";
        public string LastWeightsDebug { get; private set; } = "-";
        public string MemoryDebug => _memory.DebugSummary(Time.time);

        public void Bind(WrestlerCore core, AIDifficultyData difficulty)
        {
            Unsubscribe();
            _core = core;
            _difficulty = difficulty;
            ResolvePersonality();
            if (_core != null)
            {
                // Success memory comes from resolved combat outcomes, not
                // from the attempt sites: a whiff is an attempt, a landed
                // move or completed reversal is a success.
                _core.Combat.OnLandedHit += HandleLandedHit;
                _core.Combat.OnReversedOpponent += HandleReversedOpponent;
            }
        }

        void OnDestroy() => Unsubscribe();

        void Unsubscribe()
        {
            if (_core == null) return;
            _core.Combat.OnLandedHit -= HandleLandedHit;
            _core.Combat.OnReversedOpponent -= HandleReversedOpponent;
        }

        void HandleLandedHit(MoveData move) =>
            _memory.NoteSuccess(FamilyFor(move), Time.time);

        void HandleReversedOpponent() =>
            _memory.NoteSuccess("reversal", Time.time);

        void ResolvePersonality()
        {
            _personalityKind = _core != null && _core.Stats != null && _core.Stats.Data != null
                ? _core.Stats.Data.aiPersonality
                : AIPersonality.Balanced;
            _personality = AIPersonalityProfiles.For(_personalityKind);
        }

        /// Maps a landed move back to the decision family that produced it.
        static string FamilyFor(MoveData move)
        {
            if (move == null) return "unknown";
            switch (move.category)
            {
                case MoveCategory.LightStrike: return "light";
                case MoveCategory.HeavyStrike: return "heavy";
                case MoveCategory.QuickGrapple:
                case MoveCategory.PowerGrapple:
                case MoveCategory.RunningGrapple: return "grapple";
                case MoveCategory.RunningStrike:
                case MoveCategory.RopeReboundAttack: return "running";
                case MoveCategory.Submission: return "submission";
                case MoveCategory.Pin: return "pin";
                case MoveCategory.GroundUpperAttack:
                case MoveCategory.GroundLowerAttack: return "ground";
                case MoveCategory.CornerAttack:
                case MoveCategory.CornerStrike:
                case MoveCategory.CornerGrapple: return "corner";
                case MoveCategory.RopeStaggerAttack: return "rope";
                default: return "other";
            }
        }

        public void ResetForMatch()
        {
            _memory.Clear();
            ResolvePersonality();
            LastSelectedFamily = "-";
            LastWeightsDebug = "-";
            CurrentState = AIState.IdleThink;
        }

        WrestlerCore Opp => _core != null ? _core.Opponent : null;

        void Update()
        {
            if (_core == null || Opp == null || _difficulty == null) return;
            var mm = MatchManager.Instance;
            if (mm == null) return;

            // Debug-only pacing toggle, same convention as F1/F2.
            if (Input.GetKeyDown(KeyCode.F3)) CycleBehaviorMode();

            if (mm.State == MatchState.Finished)
            {
                CurrentState = _core.States.Current == WrestlerState.Victory ? AIState.Victory : AIState.Defeat;
                _core.Motor.SetMoveInput(Vector3.zero, false);
                return;
            }
            // Submission defense runs outside the combat-allowed gate (the
            // match state is SubmissionInProgress, not Active) and applies in
            // every behavior mode — escaping holds is defense, not offense.
            if (DefendSubmission()) return;

            if (!mm.IsCombatAllowed)
            {
                _core.Motor.SetMoveInput(Vector3.zero, false);
                return;
            }

            if (BehaviorMode == CpuBehaviorMode.Dummy)
            {
                _core.Motor.SetMoveInput(Vector3.zero, false);
                return;
            }

            ReactDefensively();

            if (BehaviorMode == CpuBehaviorMode.NoOffense)
            {
                // Defensive reactions above stay live; never initiate offense.
                if (_core.Combat.InGrappleLockAsAttacker) _core.Combat.ReleaseGrapple();
                CurrentState = AIState.IdleThink;
                _core.Motor.SetMoveInput(Vector3.zero, false);
                return;
            }

            if (Time.time >= _nextDecisionAt)
            {
                Decide();
                _nextDecisionAt = Time.time + Random.Range(_difficulty.reactionDelayMin, _difficulty.reactionDelayMax);
            }

            Act();
        }

        void CycleBehaviorMode()
        {
            BehaviorMode = BehaviorMode == CpuBehaviorMode.Full ? CpuBehaviorMode.NoOffense
                : BehaviorMode == CpuBehaviorMode.NoOffense ? CpuBehaviorMode.Dummy
                : CpuBehaviorMode.Full;
            CurrentState = AIState.IdleThink;
            MatchHUD.TryShowMessage($"CPU behavior: {BehaviorMode}");
            Debug.Log($"[AI] {_core.DisplayName} behavior mode: {BehaviorMode}");
        }

        // ---------------- Defense (reaction-time gated) ----------------

        void ReactDefensively()
        {
            if (Time.time < _reversalCooldownUntil) return;

            // Reverse incoming strikes/grapples/specials. Difficulty alone
            // decides whether the reversal happens; the directional read
            // below only shapes how strong a successful one is.
            bool windowOpen = Opp.Combat.IsReversalWindowOpenFor(_core) ||
                              (Opp.Specials != null && Opp.Specials.ReversalWindowOpen);
            if (windowOpen && _core.States.Profile.canReverse)
            {
                _reversalCooldownUntil = Time.time + _difficulty.reversalCooldown;
                if (Random.value < _difficulty.reversalAccuracy)
                {
                    CurrentState = AIState.DefensiveReversal;
                    MoveData incoming = Opp.Combat.CurrentMove;
                    MoveDirection read = ChooseReversalRead(
                        incoming != null
                            ? incoming.preferredCounterDirection
                            : ReversalReadDirection.Neutral,
                        _personalityKind,
                        Random.value);
                    _core.Combat.TryReversal(read, read != MoveDirection.Neutral);
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

        float _nextSubmissionEffortAt;

        /// While the CPU defends a submission it crawls toward the nearest
        /// rope and mashes on a difficulty-gated cadence, through the same
        /// SubmissionSystem APIs the player uses. Reversal accuracy never
        /// feeds submission escape.
        bool DefendSubmission()
        {
            var subs = SubmissionSystem.Instance;
            if (subs == null || !subs.Active || subs.Defender != _core) return false;

            CurrentState = AIState.Recover;

            // Crawl toward the nearest rope: with rope breaks active it earns
            // a release; with them disabled the same intent still converts to
            // reduced escape effort, so the input is never dead.
            var ring = RingInteractionSystem.Instance;
            if (ring != null && ring.Bounds != null)
            {
                var info = ring.GetNearestRopeContactInfo(_core);
                Vector3 toRope = MathUtil.Flat(info.contactPoint - transform.position);
                if (toRope.sqrMagnitude > 0.0001f)
                    subs.SetDefenderCrawlIntent(_core, toRope.normalized);
            }

            if (Time.time >= _nextSubmissionEffortAt)
            {
                subs.AddEscapeEffort(_core);
                float cadenceBonus = 1f + (_difficulty != null ? _difficulty.submissionEscapeBonus : 0f);
                _nextSubmissionEffortAt = Time.time + Mathf.Max(0.08f, 0.22f / cadenceBonus);
            }
            return true;
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
            // Grapple lock attacker: pick a follow-up. This must be checked
            // before the canAttack gate — the GrappleLock state profile has
            // canAttack=false, which previously sent the AI to Recover and let
            // every lock time out (endless lockup → re-grapple loop).
            if (_core.Combat.InGrappleLockAsAttacker)
            {
                CurrentState = AIState.ChooseGrappleMove;
                return;
            }

            if (!_core.States.Profile.canAttack)
            {
                CurrentState = _core.States.IsDowned ? AIState.GetUp : AIState.Recover;
                return;
            }

            float now = Time.time;

            // Stamina caution: back off and breathe. Personalities that rest
            // more (Showman, Evasive) raise the threshold; Brawler lowers it.
            if (_core.Stats.StaminaPercent <
                _difficulty.staminaCautionThreshold * _personality.BreatherFrequency)
            {
                CurrentState = AIState.BackOff;
                return;
            }

            // Special: fire it or set it up. A valid setup is a tactically
            // obvious opportunity, so personality only shades the chance.
            if (_core.Stats.HasFullMomentum &&
                Random.value < AIDecisionWeights.Apply(
                    _difficulty.specialSetupPreference, _personality.SpecialSetup, 0f))
            {
                CurrentState = AIState.AttemptSpecial;
                return;
            }

            // Opponent downed: pin, submit, or work the ground. A credible
            // late pin stays ahead of personality preferences.
            if (Opp.States.IsDowned)
            {
                bool hurtEnough = Opp.Stats.HealthPercent <=
                    Mathf.Clamp01(_difficulty.pinAttemptThreshold * _personality.PinUrgency);
                if (hurtEnough && _memory.CanUse("pin", 3f, now))
                {
                    CurrentState = AIState.AttemptPin;
                    return;
                }
                if (Opp.Stats.HealthPercent <=
                    Mathf.Clamp01(_difficulty.submissionAttemptThreshold * _personality.Submission) &&
                    _core.Stats.StaminaPercent > 0.35f && _memory.CanUse("submission", 6f, now))
                {
                    CurrentState = AIState.AttemptSubmission;
                    return;
                }
                // Ground offense never outranks a credible pin or submission —
                // both checks above already returned.
                if (_memory.CanUse("ground", 1.4f, now) &&
                    Random.value < AIDecisionWeights.Apply(
                        0.9f, _personality.GroundOffense, _memory.RepetitionPenalty("ground", now)))
                {
                    CurrentState = AIState.AttemptGroundAttack;
                    return;
                }
                CurrentState = AIState.Circle;
                return;
            }

            float dist = _core.DistanceToOpponent();
            float roll = Random.value;

            if (dist > 4.5f)
            {
                CurrentState = roll < AIDecisionWeights.Apply(
                        _difficulty.ropeStrategyPreference, _personality.RopeCornerStrategy, 0f)
                    ? AIState.UseRopeRebound
                    : AIState.Approach;
            }
            else if (dist > 2.2f)
            {
                // The charge is the risky play at mid range.
                if (roll < AIDecisionWeights.Apply(
                        _difficulty.aggression * 0.5f, _personality.RiskTolerance,
                        _memory.RepetitionPenalty("running", now)))
                    CurrentState = AIState.AttemptRunningAttack;
                else if (roll < AIDecisionWeights.Apply(
                        _difficulty.aggression, _personality.Aggression, 0f))
                    CurrentState = AIState.Approach;
                else CurrentState = AIState.Circle;
            }
            else
            {
                // Close range: strike, grapple, or herd toward ropes/corner.
                // A cornered opponent gets corner offense, never more herding.
                if (Opp.States.Current == WrestlerState.Cornered &&
                    RingInteractionSystem.Instance != null &&
                    RingInteractionSystem.Instance.IsInCornerZone(Opp))
                {
                    CurrentState = AIState.AttemptCornerOffense;
                }
                else if (Opp.States.Current == WrestlerState.RopeStaggered &&
                         RingInteractionSystem.Instance != null &&
                         RingInteractionSystem.Instance.IsNearRope(
                             Opp, RingInteractionSystem.RopeContactRange + 0.2f))
                {
                    CurrentState = AIState.AttemptRopeOffense;
                }
                else if (Opp.States.Current == WrestlerState.RopeStaggered || Opp.States.Current == WrestlerState.Cornered)
                {
                    CurrentState = roll < 0.5f ? AIState.AttemptHeavyStrike : AIState.AttemptGrapple;
                }
                // Breather gate: against a neutral opponent only this fraction
                // of close-range decisions attack; the rest circle or back off
                // so the match has readable pauses. Contextual windows above
                // (staggered/cornered/downed) are exempt — exploiting those
                // is correct urgency.
                else if (Random.value > AIDecisionWeights.Apply(
                        _difficulty.aggression, _personality.Aggression, 0f))
                {
                    CurrentState = Random.value < 0.3f * _personality.BreatherFrequency
                        ? AIState.BackOff
                        : AIState.Circle;
                }
                else
                {
                    ChooseNeutralCloseAction(now);
                }
            }
        }

        /// Weighted neutral close-range decision: difficulty supplies the
        /// base envelope, personality reshapes it inside bounds, and the
        /// repetition memory taxes families the CPU keeps leaning on.
        void ChooseNeutralCloseAction(float now)
        {
            float lightWeight = _memory.CanUse("light", 0.6f, now)
                ? AIDecisionWeights.Apply(
                    _difficulty.strikePreference * 0.6f, _personality.Strike,
                    _memory.RepetitionPenalty("light", now))
                : 0f;
            float heavyWeight = _memory.CanUse("heavy", 1.4f, now)
                ? AIDecisionWeights.Apply(
                    _difficulty.strikePreference * 0.5f,
                    (_personality.Strike + _personality.PowerMove) * 0.5f,
                    _memory.RepetitionPenalty("heavy", now))
                : 0f;
            float grappleWeight = _memory.CanUse("grapple", 1.2f, now)
                ? AIDecisionWeights.Apply(
                    _difficulty.grapplePreference, _personality.Grapple,
                    _memory.RepetitionPenalty("grapple", now))
                : 0f;
            float herdWeight = AIDecisionWeights.Apply(
                0.25f, _personality.RopeCornerStrategy, 0f);

            CurrentState = AIDecisionWeights.ChooseWeighted(
                Random.value,
                new WeightedAIAction(AIState.AttemptLightStrike, lightWeight),
                new WeightedAIAction(AIState.AttemptHeavyStrike, heavyWeight),
                new WeightedAIAction(AIState.AttemptGrapple, grappleWeight),
                new WeightedAIAction(AIState.ForceOpponentToRopes, herdWeight),
                new WeightedAIAction(AIState.Circle, 0.1f));

            LastSelectedFamily = CurrentState switch
            {
                AIState.AttemptLightStrike => "light",
                AIState.AttemptHeavyStrike => "heavy",
                AIState.AttemptGrapple => "grapple",
                AIState.ForceOpponentToRopes => "herd",
                _ => "circle",
            };
            LastWeightsDebug =
                $"light={lightWeight:0.00} heavy={heavyWeight:0.00} " +
                $"grapple={grappleWeight:0.00} herd={herdWeight:0.00}";
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
                    if (InRange(WrestlerCombat.StrikeRange))
                    {
                        // Success rule: only a successful (or state-changing)
                        // action re-arms the decision timer; a failed Try*
                        // must yield to Decide through IdleThink.
                        bool litConnected = _core.Combat.TryLightStrike();
                        _memory.NoteAttempt("light", Time.time);
                        if (litConnected) Rethink();
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.AttemptHeavyStrike:
                    if (InRange(WrestlerCombat.StrikeRange))
                    {
                        bool heavyStarted = _core.Combat.TryHeavyStrike();
                        _memory.NoteAttempt("heavy", Time.time);
                        if (heavyStarted) Rethink();
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.AttemptGrapple:
                    if (_core.Combat.InGrappleLockAsAttacker)
                    {
                        // The lock formed: hand off to the follow-up state.
                        // Re-attempting here would fail silently and re-arm
                        // Rethink every frame, suppressing Decide until the
                        // lock timed out (endless lockup → re-lock loop).
                        CurrentState = AIState.ChooseGrappleMove;
                    }
                    else if (InRange(WrestlerCombat.GrappleRange))
                    {
                        bool locked = _core.Combat.TryGrappleAttempt();
                        _memory.NoteAttempt("grapple", Time.time);
                        if (locked) Rethink();
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.ChooseGrappleMove:
                    if (_core.Combat.InGrappleLockAsAttacker)
                    {
                        // Power preference is a bias only; ExecuteDirectionalGrapple
                        // downgrades unaffordable picks and keeps the lock when a
                        // family has nothing affordable, so the quick fallback below
                        // still gets its chance.
                        // Power follow-up preference is personality-shaped.
                        bool power = Random.value < AIDecisionWeights.Apply(
                                         _difficulty.grapplePreference, _personality.PowerMove, 0f) &&
                                     MovePacingRules.CanAttempt(
                                         _core.Moveset != null ? _core.Moveset.RandomPowerGrapple() : null,
                                         _core.Stats.Stamina);
                        MoveDirection direction = ChooseGrappleDirection(power, _core.Stats.StaminaPercent);
                        bool executed = power
                            ? _core.Combat.TryPowerGrappleFromLock(direction) || _core.Combat.TryQuickGrappleFromLock(direction)
                            : _core.Combat.TryQuickGrappleFromLock(direction) || _core.Combat.TryPowerGrappleFromLock(direction);
                        _memory.NoteAttempt("grapple", Time.time);
                        if (executed)
                        {
                            Rethink();
                        }
                        else
                        {
                            // Never leave a lock dangling, and never dangle
                            // silently: release, say why, and breathe.
                            _core.Combat.ReleaseGrapple();
                            Debug.Log($"[AI] {_core.DisplayName} releases the lock — no affordable follow-up");
                            CurrentState = AIState.BackOff;
                        }
                    }
                    else
                    {
                        // Lock already resolved (move executing, timeout, or
                        // reversal). Yield to Decide instead of re-arming the
                        // decision timer every frame.
                        CurrentState = AIState.IdleThink;
                    }
                    break;

                case AIState.AttemptRunningAttack:
                case AIState.UseRopeRebound:
                    MoveToward(Opp.transform.position, run: true);
                    if (InRange(WrestlerCombat.StrikeRange + 0.3f))
                    {
                        // Rebound-specific attack while rebounding; ordinary
                        // running attack outside the rebound states.
                        bool charged = _core.Combat.TryRopeReboundAttack() ||
                                       _core.Combat.TryRunningAttack();
                        _memory.NoteAttempt("running", Time.time);
                        if (charged) Rethink();
                        else CurrentState = AIState.IdleThink;
                    }
                    break;

                case AIState.AttemptRopeOffense:
                    if (InRange(1.6f))
                    {
                        // Rope-context attack with a normal-offense fallback.
                        bool roped = _core.Combat.TryRopeStaggerAttack() ||
                                     _core.Combat.TryHeavyStrike();
                        _memory.NoteAttempt("rope", Time.time);
                        if (roped) Rethink();
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.ForceOpponentToRopes:
                case AIState.AttemptCornerSetup:
                    // Keep striking at close range — knockback pushes them toward ropes/corners.
                    if (InRange(WrestlerCombat.StrikeRange))
                    {
                        bool herded = _core.Combat.TryLightStrike();
                        _memory.NoteAttempt("light", Time.time);
                        if (herded) Rethink();
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.AttemptSpecial:
                    if (AISpecialPlanner.PlanOrExecute(_core, Opp, out var setupTarget)) { Rethink(); }
                    else if (setupTarget.HasValue) MoveToward(setupTarget.Value, false);
                    else Circle();
                    break;

                case AIState.AttemptCornerOffense:
                    if (InRange(1.55f))
                    {
                        bool cornerGrapple = Random.value < AIDecisionWeights.Apply(
                                                 _difficulty.cornerStrategyPreference,
                                                 _personality.RopeCornerStrategy, 0f) &&
                                             MovePacingRules.CanAttempt(
                                                 _core.Moveset != null ? _core.Moveset.RandomCornerGrapple() : null,
                                                 _core.Stats.Stamina);
                        bool done = cornerGrapple
                            ? _core.Combat.TryCornerGrapple() || _core.Combat.TryCornerStrike()
                            : _core.Combat.TryCornerStrike() || _core.Combat.TryCornerGrapple();
                        _memory.NoteAttempt("corner", Time.time);
                        if (done) Rethink();
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.AttemptGroundAttack:
                    if (InRange(1.5f))
                    {
                        bool grounded = _core.Combat.TryGroundAttack();
                        _memory.NoteAttempt("ground", Time.time);
                        if (grounded) Rethink();
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, false);
                    break;

                case AIState.AttemptPin:
                    if (InRange(1.3f))
                    {
                        bool covered = _core.Combat.TryPin();
                        _memory.NoteAttempt("pin", Time.time);
                        if (covered)
                        {
                            // A started pin is already its own success.
                            _memory.NoteSuccess("pin", Time.time);
                            Rethink();
                        }
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, run: true);
                    break;

                case AIState.AttemptSubmission:
                    if (InRange(1.3f))
                    {
                        bool lockedIn = _core.Combat.TrySubmission();
                        _memory.NoteAttempt("submission", Time.time);
                        if (lockedIn)
                        {
                            _memory.NoteSuccess("submission", Time.time);
                            Rethink();
                        }
                        else CurrentState = AIState.IdleThink;
                    }
                    else MoveToward(Opp.transform.position, false);
                    break;

                default:
                    _core.Motor.SetMoveInput(Vector3.zero, false);
                    break;
            }
        }

        /// Picks the CPU's directional read for a reversal it has already won
        /// on the difficulty accuracy roll. Personality biases how often the
        /// CPU commits to the move's authored counter direction (a stronger
        /// but riskier-looking read) versus the safe neutral read; it never
        /// touches reversalAccuracy itself. Pure for deterministic tests.
        public static MoveDirection ChooseReversalRead(
            ReversalReadDirection preferred,
            AIPersonality personality,
            float roll)
        {
            if (preferred == ReversalReadDirection.Neutral)
                return MoveDirection.Neutral;

            float commit = personality switch
            {
                AIPersonality.Technician => 0.7f,
                AIPersonality.Showman => 0.55f,
                AIPersonality.Trickster => 0.55f,
                AIPersonality.Evasive => 0.45f,
                AIPersonality.Brawler => 0.3f,
                AIPersonality.Powerhouse => 0.35f,
                _ => 0.45f,
            };
            if (roll >= commit) return MoveDirection.Neutral;

            switch (preferred)
            {
                case ReversalReadDirection.Toward: return MoveDirection.Forward;
                case ReversalReadDirection.Away: return MoveDirection.Backward;
                case ReversalReadDirection.Left: return MoveDirection.Left;
                case ReversalReadDirection.Right: return MoveDirection.Right;
                default: return MoveDirection.Neutral;
            }
        }

        /// Confident power attempts commit forward; a tired CPU keeps the safe
        /// neutral follow-up; otherwise spread across the remaining buckets.
        static MoveDirection ChooseGrappleDirection(bool power, float staminaPercent)
        {
            if (power && staminaPercent > 0.65f) return MoveDirection.Forward;
            if (staminaPercent < 0.35f) return MoveDirection.Neutral;
            float roll = Random.value;
            if (roll < 0.25f) return MoveDirection.Backward;
            if (roll < 0.5f) return MoveDirection.Left;
            if (roll < 0.75f) return MoveDirection.Right;
            return MoveDirection.Neutral;
        }

        bool InRange(float range) => _core.DistanceToOpponent() <= range;

        void Rethink() => _nextDecisionAt = Time.time + Random.Range(_difficulty.reactionDelayMin, _difficulty.reactionDelayMax);

        void MoveToward(Vector3 target, bool run)
        {
            Vector3 dir = MathUtil.FlatDirection(transform.position, target);
            if (MathUtil.FlatDistance(transform.position, target) < 0.15f) dir = Vector3.zero;
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
