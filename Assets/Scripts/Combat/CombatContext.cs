namespace LoCoFight
{
    /// Transient combat situation resolved at the moment an action is
    /// attempted. Never stored as a second persistent state — the
    /// WrestlerStateMachine remains the only state authority.
    public enum CombatContext
    {
        Standing,
        GrappleLock,
        GroundUpper,
        GroundLower,
        Corner,
        RopeStagger,
        RopeRebound
    }

    /// Which half of a downed defender the attacker is standing over,
    /// measured along the defender's facing axis.
    public enum GroundTargetZone
    {
        None,
        Upper,
        Lower
    }

    /// Diagnostic record of the most recent contextual move request, surfaced
    /// through the F1 overlay. Presentation/debug only — never gameplay input.
    public readonly struct CombatContextSnapshot
    {
        public CombatContext Context { get; }
        public GroundTargetZone GroundZone { get; }
        public MoveDirection Direction { get; }
        public string RequestedFamily { get; }
        public int CandidateCount { get; }
        public string SelectedMove { get; }
        public MoveTier Tier { get; }
        public MoveValidationResult Validation { get; }
        public bool UsedFallback { get; }

        public CombatContextSnapshot(
            CombatContext context,
            GroundTargetZone groundZone,
            MoveDirection direction,
            string requestedFamily,
            int candidateCount,
            string selectedMove,
            MoveTier tier,
            MoveValidationResult validation,
            bool usedFallback)
        {
            Context = context;
            GroundZone = groundZone;
            Direction = direction;
            RequestedFamily = requestedFamily;
            CandidateCount = candidateCount;
            SelectedMove = selectedMove;
            Tier = tier;
            Validation = validation;
            UsedFallback = usedFallback;
        }
    }
}
