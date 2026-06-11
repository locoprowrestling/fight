namespace LoCoFight
{
    /// Authored counter direction for a move's reversal window, relative to
    /// the attacker: Toward = step into the move, Away = pull back, lateral
    /// values sidestep it. Neutral means the move has no strong read.
    public enum ReversalReadDirection
    {
        Neutral,
        Toward,
        Away,
        Left,
        Right
    }

    public enum ReversalOutcome
    {
        Basic,
        Strong
    }

    /// Pure read resolution: maps the defender's submitted camera-relative
    /// direction against the move's authored counter direction. Timing, state
    /// permission, cooldown, and stamina stay owned by WrestlerCombat.
    public static class ReversalReadResolver
    {
        public static ReversalOutcome Resolve(
            ReversalReadDirection preferred,
            MoveDirection submitted,
            bool hasDirectionalInput,
            bool allowsStrongCounter)
        {
            if (!allowsStrongCounter || !hasDirectionalInput)
                return ReversalOutcome.Basic;

            MoveDirection expected = preferred == ReversalReadDirection.Toward
                ? MoveDirection.Forward
                : preferred == ReversalReadDirection.Away
                    ? MoveDirection.Backward
                    : preferred == ReversalReadDirection.Left
                        ? MoveDirection.Left
                        : preferred == ReversalReadDirection.Right
                            ? MoveDirection.Right
                            : MoveDirection.Neutral;

            return submitted == expected && expected != MoveDirection.Neutral
                ? ReversalOutcome.Strong
                : ReversalOutcome.Basic;
        }
    }
}
