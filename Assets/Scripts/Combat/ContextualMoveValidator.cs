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
    }
}
