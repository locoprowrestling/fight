namespace LoCoFight
{
    public enum MoveRejectionReason
    {
        None,
        MissingMove,
        MatchInactive,
        WrongAttackerState,
        WrongTargetState,
        WrongGroundZone,
        NotInCorner,
        NotNearRopes,
        NotRebounding,
        OutOfRange,
        InsufficientStamina,
        InsufficientLiftStrength,
        TargetTooHeavy
    }

    /// Structured accept/reject for contextual move requests. Produced before
    /// any stamina is spent or temporary state is taken.
    public readonly struct MoveValidationResult
    {
        public bool IsValid { get; }
        public MoveRejectionReason Reason { get; }
        public string DebugMessage { get; }

        MoveValidationResult(bool valid, MoveRejectionReason reason, string message)
        {
            IsValid = valid;
            Reason = reason;
            DebugMessage = message;
        }

        public static MoveValidationResult Valid() =>
            new MoveValidationResult(true, MoveRejectionReason.None, "Valid");

        public static MoveValidationResult Reject(
            MoveRejectionReason reason,
            string message) =>
            new MoveValidationResult(false, reason, message);
    }
}
