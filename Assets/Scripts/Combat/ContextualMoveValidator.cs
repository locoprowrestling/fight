using UnityEngine;

namespace LoCoFight
{
    /// Shared validation for contextual move requests. Player and CPU both go
    /// through these checks; nothing here spends stamina or mutates state.
    /// ValidatePure fixes the common rejection ordering; runtime overloads are
    /// added per context as each contextual family is implemented.
    public static class ContextualMoveValidator
    {
        public static MoveValidationResult ValidatePure(
            bool moveExists,
            bool matchActive,
            bool attackerCanAct,
            bool targetStateValid,
            bool contextValid,
            float currentStamina,
            float requiredStamina)
        {
            if (!moveExists)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.MissingMove, "No move assigned");
            if (!matchActive)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.MatchInactive, "Match is not active");
            if (!attackerCanAct)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongAttackerState, "Attacker cannot act");
            if (!targetStateValid)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongTargetState, "Target state is invalid");
            if (!contextValid)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongGroundZone, "Move context is invalid");
            if (currentStamina < requiredStamina)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.InsufficientStamina, "Insufficient stamina");
            return MoveValidationResult.Valid();
        }

        public static MoveValidationResult ValidateGround(
            MoveData move,
            bool targetDowned,
            GroundTargetZone actualZone,
            bool inRange,
            float currentStamina)
        {
            if (move == null)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.MissingMove, "No ground move assigned");
            if (!targetDowned)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongTargetState, "Target is not downed");
            if (move.requiredGroundZone != GroundTargetZone.None &&
                move.requiredGroundZone != actualZone)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongGroundZone, "Wrong ground target zone");
            if (!inRange)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.OutOfRange, "Ground target is out of range");
            if (currentStamina < Mathf.Max(move.staminaCost, move.minimumStamina))
                return MoveValidationResult.Reject(
                    MoveRejectionReason.InsufficientStamina, "Insufficient stamina");
            return MoveValidationResult.Valid();
        }

        /// Corner actions require both the defender state and live corner
        /// geometry — either alone is not enough.
        public static MoveValidationResult ValidateCorner(
            MoveData move,
            bool targetCornered,
            bool inCornerZone,
            bool inRange,
            float currentStamina)
        {
            if (move == null)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.MissingMove, "No corner move assigned");
            if (!targetCornered)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongTargetState, "Target is not cornered");
            if (!inCornerZone)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.NotInCorner, "Target left the corner zone");
            if (!inRange)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.OutOfRange, "Corner target is out of range");
            if (currentStamina < Mathf.Max(move.staminaCost, move.minimumStamina))
                return MoveValidationResult.Reject(
                    MoveRejectionReason.InsufficientStamina, "Insufficient stamina");
            return MoveValidationResult.Valid();
        }

        /// Rope-context moves: stagger attacks check the defender's state and
        /// live rope proximity; rebound attacks check the attacker's rebound
        /// state. Which checks apply comes from the move's own requirements.
        public static MoveValidationResult ValidateRope(
            MoveData move,
            bool targetRopeStaggered,
            bool targetNearRope,
            bool attackerRebounding,
            bool inRange,
            float currentStamina)
        {
            if (move == null)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.MissingMove, "No rope move assigned");
            if (move.requiresTargetRopeStaggered && !targetRopeStaggered)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongTargetState, "Target is not rope-staggered");
            if (move.requiresOpponentNearRopes && !targetNearRope)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.NotNearRopes, "Target left the ropes");
            if (move.requiresRopeRebound && !attackerRebounding)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.NotRebounding, "Attacker is not rebounding");
            if (!inRange)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.OutOfRange, "Rope target is out of range");
            if (currentStamina < Mathf.Max(move.staminaCost, move.minimumStamina))
                return MoveValidationResult.Reject(
                    MoveRejectionReason.InsufficientStamina, "Insufficient stamina");
            return MoveValidationResult.Valid();
        }
    }
}
