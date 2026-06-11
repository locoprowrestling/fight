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
}
