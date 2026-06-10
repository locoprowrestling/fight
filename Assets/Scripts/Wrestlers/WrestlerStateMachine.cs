using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    public enum WrestlerState
    {
        Idle, Moving, Running,
        StrikeStartup, StrikeActive, StrikeRecovery,
        GrappleAttempt, GrappleLock, GrappleMoveStartup, GrappleMoveActive, GrappleMoveRecovery,
        Reversing, Dodging, Stunned, Downed, RollingAway, GettingUp,
        RopeContact, RopeStaggered, RopeReboundRun, RopeReboundReturn,
        RopeTrapSetup, RopeTrapLocked,
        Cornered, TurnbuckleClimb,
        AerialSetup, AerialAirborne, AerialLandingHit, AerialLandingMiss,
        CarryLift, CarryParade, ComboSequence,
        SpecialStartup, SpecialActive, SpecialRecovery,
        Pinning, Pinned, SubmissionApplying, SubmissionDefending,
        RefereeCounting, Victory, Defeat
    }

    /// What a wrestler is allowed to do while in a given state.
    public struct StateProfile
    {
        public bool canMove, canRotate, canAttack, canGrapple, canReverse, canDodge;
        public bool canBePinned, canBeSubmitted, canBeGrappled, canBeStruck;
        public bool canRopeInteract, canClimb, canBeInterrupted;
        public float defaultTimeout;     // <=0 = no auto-exit
        public WrestlerState exitState;  // where the timeout sends us
    }

    public class WrestlerStateMachine : MonoBehaviour
    {
        public WrestlerState Current { get; private set; } = WrestlerState.Idle;
        public float TimeInState { get; private set; }
        public StateProfile Profile => GetProfile(Current);

        float _timeoutOverride = -1f;
        WrestlerCore _core;

        public bool IsDowned => Current == WrestlerState.Downed || Current == WrestlerState.RollingAway;
        public bool IsStanding =>
            Current == WrestlerState.Idle || Current == WrestlerState.Moving || Current == WrestlerState.Running ||
            Current == WrestlerState.Stunned || Current == WrestlerState.RopeStaggered || Current == WrestlerState.Cornered ||
            Current == WrestlerState.RopeContact;
        public bool IsGroggy => Current == WrestlerState.Stunned || Current == WrestlerState.RopeStaggered || Current == WrestlerState.Cornered;
        public bool IsBusy => !Profile.canAttack;

        public void Bind(WrestlerCore core) => _core = core;

        /// Force-set a state. timeout overrides the profile default (-1 = use default).
        public void Set(WrestlerState state, float timeout = -1f)
        {
            Current = state;
            TimeInState = 0f;
            _timeoutOverride = timeout;
            if (_core != null && _core.Anim != null) _core.Anim.PlayState(state.ToString());
        }

        public void ExtendTimeout(float extraSeconds)
        {
            if (_timeoutOverride > 0f) _timeoutOverride += extraSeconds;
        }

        void Update()
        {
            TimeInState += Time.deltaTime;
            float timeout = _timeoutOverride > 0f ? _timeoutOverride : Profile.defaultTimeout;
            if (timeout > 0f && TimeInState >= timeout)
                Set(Profile.exitState);
        }

        static readonly Dictionary<WrestlerState, StateProfile> Profiles = BuildProfiles();

        public static StateProfile GetProfile(WrestlerState s) =>
            Profiles.TryGetValue(s, out var p) ? p : Profiles[WrestlerState.Idle];

        static Dictionary<WrestlerState, StateProfile> BuildProfiles()
        {
            var d = new Dictionary<WrestlerState, StateProfile>();
            StateProfile P(bool move = false, bool rotate = false, bool attack = false, bool grapple = false,
                bool reverse = false, bool dodge = false, bool pinnable = false, bool submittable = false,
                bool grabbable = false, bool strikable = true, bool rope = false, bool climb = false,
                bool interrupt = true, float timeout = 0f, WrestlerState exit = WrestlerState.Idle)
                => new StateProfile
                {
                    canMove = move, canRotate = rotate, canAttack = attack, canGrapple = grapple,
                    canReverse = reverse, canDodge = dodge, canBePinned = pinnable, canBeSubmitted = submittable,
                    canBeGrappled = grabbable, canBeStruck = strikable, canRopeInteract = rope, canClimb = climb,
                    canBeInterrupted = interrupt, defaultTimeout = timeout, exitState = exit
                };

            d[WrestlerState.Idle] = P(move: true, rotate: true, attack: true, grapple: true, reverse: true, dodge: true, grabbable: true, rope: true, climb: true);
            d[WrestlerState.Moving] = P(move: true, rotate: true, attack: true, grapple: true, reverse: true, dodge: true, grabbable: true, rope: true, climb: true);
            d[WrestlerState.Running] = P(move: true, rotate: true, attack: true, grapple: false, reverse: false, dodge: true, grabbable: true, rope: true);
            d[WrestlerState.StrikeStartup] = P(rotate: true, grabbable: true);
            d[WrestlerState.StrikeActive] = P(grabbable: false);
            d[WrestlerState.StrikeRecovery] = P(grabbable: true, timeout: 1.5f);
            d[WrestlerState.GrappleAttempt] = P(rotate: true, timeout: 0.6f);
            d[WrestlerState.GrappleLock] = P(reverse: true, timeout: 1.8f);
            d[WrestlerState.GrappleMoveStartup] = P();
            d[WrestlerState.GrappleMoveActive] = P(strikable: false);
            d[WrestlerState.GrappleMoveRecovery] = P(grabbable: true, timeout: 1.5f);
            d[WrestlerState.Reversing] = P(timeout: 0.45f);
            d[WrestlerState.Dodging] = P(move: true, strikable: false, timeout: 0.4f);
            // reverse:true keeps grapple-move reversal windows live for the victim
            // (TryReversal still requires the attacker's window to be open).
            d[WrestlerState.Stunned] = P(grabbable: true, reverse: true, timeout: 1.2f);
            d[WrestlerState.Downed] = P(pinnable: true, submittable: true, dodge: false, strikable: true, timeout: 3f, exit: WrestlerState.GettingUp);
            d[WrestlerState.RollingAway] = P(strikable: false, timeout: 0.5f, exit: WrestlerState.GettingUp);
            d[WrestlerState.GettingUp] = P(timeout: 0.7f);
            d[WrestlerState.RopeContact] = P(move: true, rotate: true, attack: true, grapple: true, reverse: true, dodge: true, grabbable: true, rope: true);
            d[WrestlerState.RopeStaggered] = P(grabbable: true, reverse: true, timeout: 0.9f);
            d[WrestlerState.RopeReboundRun] = P(rope: true, strikable: true, timeout: 1.5f, exit: WrestlerState.RopeReboundReturn);
            d[WrestlerState.RopeReboundReturn] = P(move: true, rotate: true, attack: true, rope: true, strikable: true, timeout: 1.2f);
            d[WrestlerState.RopeTrapSetup] = P(timeout: 1.2f);
            d[WrestlerState.RopeTrapLocked] = P(strikable: false, interrupt: false);
            d[WrestlerState.Cornered] = P(grabbable: true, reverse: true, timeout: 1.0f);
            d[WrestlerState.TurnbuckleClimb] = P(climb: true, strikable: true, timeout: 2f);
            d[WrestlerState.AerialSetup] = P(strikable: true, timeout: 2f);
            d[WrestlerState.AerialAirborne] = P(strikable: false, interrupt: false);
            d[WrestlerState.AerialLandingHit] = P(timeout: 0.6f);
            d[WrestlerState.AerialLandingMiss] = P(timeout: 2.25f);
            d[WrestlerState.CarryLift] = P(strikable: false, interrupt: false);
            d[WrestlerState.CarryParade] = P(strikable: false, interrupt: false);
            d[WrestlerState.ComboSequence] = P(strikable: false, interrupt: false);
            d[WrestlerState.SpecialStartup] = P(interrupt: true);
            d[WrestlerState.SpecialActive] = P(strikable: false, interrupt: false);
            d[WrestlerState.SpecialRecovery] = P(grabbable: true, timeout: 1.5f);
            d[WrestlerState.Pinning] = P(strikable: false, interrupt: false);
            d[WrestlerState.Pinned] = P(strikable: false, interrupt: false);
            d[WrestlerState.SubmissionApplying] = P(strikable: false, interrupt: false);
            d[WrestlerState.SubmissionDefending] = P(strikable: false, interrupt: false);
            d[WrestlerState.RefereeCounting] = P(strikable: false, interrupt: false);
            d[WrestlerState.Victory] = P(interrupt: false);
            d[WrestlerState.Defeat] = P(interrupt: false);
            return d;
        }
    }
}
