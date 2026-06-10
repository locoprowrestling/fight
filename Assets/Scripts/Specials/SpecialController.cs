using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    public enum SpecialEscapePhase { None, Startup, Lift, FullLock }

    /// Shared context handed to special executors.
    public class SpecialContext
    {
        public WrestlerCore Self;
        public WrestlerCore Target;
        public SpecialAbilityData Data;
        public SpecialController Controller;
        public bool Aborted;
    }

    /// Owns the wrestler's special: validation, resource spending, dispatch to
    /// the right executor, and escape/reversal bookkeeping during execution.
    public class SpecialController : MonoBehaviour
    {
        WrestlerCore _core;
        public SpecialAbilityData Data { get; private set; }

        public bool IsExecuting { get; private set; }
        public SpecialEscapePhase EscapePhase { get; set; }
        public bool ReversalWindowOpen { get; set; }
        public string LastValidationReason { get; private set; } = "";

        Coroutine _routine;
        SpecialContext _ctx;
        float _cooldownUntil;
        bool _usedOnce;

        public void Bind(WrestlerCore core, SpecialAbilityData data)
        {
            _core = core;
            Data = data;
        }

        public void ResetForMatch()
        {
            _cooldownUntil = 0f;
            _usedOnce = false;
            IsExecuting = false;
            EscapePhase = SpecialEscapePhase.None;
            ReversalWindowOpen = false;
        }

        public bool IsCurrentlyValid(out string reason)
        {
            reason = "";
            if (Data == null) { reason = "No special assigned"; return false; }
            if (IsExecuting) { reason = "Already executing"; return false; }
            if (Time.time < _cooldownUntil) { reason = "On cooldown"; return false; }
            if (Data.oncePerMatch && _usedOnce) { reason = "Already used this match"; return false; }
            if (Data.requiresFullMomentum && !_core.Stats.HasFullMomentum) { reason = "Momentum not full"; return false; }
            if (_core.Stats.Stamina < Data.staminaCost) { reason = "Not enough stamina"; return false; }
            return SpecialRequirementValidator.Validate(_core, _core.Opponent, Data, out reason);
        }

        public bool TryActivate()
        {
            if (!IsCurrentlyValid(out string reason))
            {
                LastValidationReason = reason;
                Debug.Log($"[Special] {_core.DisplayName} special failed validation: {reason}");
                if (_core.IsPlayer) MatchHUD.TryShowMessage(reason, 1.2f);
                return false;
            }

            // Spend resources.
            if (Data.spendsAllMomentum) _core.Stats.SpendAllMomentum();
            else _core.Stats.SpendMomentum(Data.momentumCost);
            _core.Stats.DrainStamina(Data.staminaCost);
            _cooldownUntil = Time.time + Data.cooldown;
            _usedOnce = true;

            _ctx = new SpecialContext { Self = _core, Target = _core.Opponent, Data = Data, Controller = this };
            IsExecuting = true;
            EscapePhase = SpecialEscapePhase.Startup;
            ReversalWindowOpen = Data.canBeReversedAtStart;

            _core.Anim.TriggerSpecial(Data.specialId);
            MatchHUD.TryShowMessage($"{_core.DisplayName}: {Data.displayName}!");
            Debug.Log($"[Special] {_core.DisplayName} starts {Data.displayName}");

            _routine = StartCoroutine(RunThenCleanup(Dispatch(_ctx)));
            return true;
        }

        IEnumerator Dispatch(SpecialContext ctx)
        {
            switch (Data.category)
            {
                case SpecialCategory.DefensiveSpecial:
                case SpecialCategory.CounterSubmission:
                    return CounterSpecialExecutor.Run(ctx);
                case SpecialCategory.SpecialSubmission:
                    return SpecialExecutor.RunSubmissionSpecial(ctx);
                case SpecialCategory.SpecialAerial:
                    return AerialSpecialExecutor.Run(ctx);
                case SpecialCategory.SpecialPowerGrapple:
                    return SpecialExecutor.RunPowerGrappleSpecial(ctx);
                case SpecialCategory.SpecialCombo:
                    return SequenceSpecialExecutor.RunCombo(ctx);
                case SpecialCategory.SpecialRopeTrap:
                    return RopeTrapSpecialExecutor.Run(ctx);
                case SpecialCategory.DirtySpecial:
                    return DirtySpecialExecutor.Run(ctx);
                case SpecialCategory.SpecialRush:
                    return RushSpecialExecutor.Run(ctx);
                case SpecialCategory.SpecialGroundedSequence:
                    return SequenceSpecialExecutor.RunSequence(ctx);
                default:
                    return SpecialExecutor.RunPowerGrappleSpecial(ctx);
            }
        }

        IEnumerator RunThenCleanup(IEnumerator inner)
        {
            yield return inner;
            FinishExecution();
        }

        void FinishExecution()
        {
            IsExecuting = false;
            EscapePhase = SpecialEscapePhase.None;
            ReversalWindowOpen = false;
            _routine = null;
            _ctx = null;
        }

        /// Called by the opponent's reversal during our reversal window.
        public void OnReversed(WrestlerCore reverser)
        {
            if (!IsExecuting) return;
            Abort();
            _core.States.Set(WrestlerState.Stunned, 1.0f);
            Debug.Log($"[Special] {Data.displayName} was reversed");
        }

        /// Called by the target's Vanishing Dodge or roll-away during an escapable phase.
        public void OnTargetEscaped(WrestlerCore escapee)
        {
            if (!IsExecuting) return;
            Abort();
            _core.States.Set(WrestlerState.Stunned, 0.8f);
            Debug.Log($"[Special] {Data.displayName} escaped by {escapee.DisplayName}");
        }

        void Abort()
        {
            if (_ctx != null) _ctx.Aborted = true;
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;

            // Undo any scripted control / carried target.
            _core.Motor.SetScriptedControl(false);
            var target = _core.Opponent;
            if (target != null && (target.States.Current == WrestlerState.CarryLift ||
                                   target.States.Current == WrestlerState.CarryParade ||
                                   target.States.Current == WrestlerState.ComboSequence ||
                                   target.States.Current == WrestlerState.RopeTrapLocked))
            {
                target.Motor.SetScriptedControl(false);
                target.States.Set(WrestlerState.Idle);
            }
            FinishExecution();
        }
    }
}
