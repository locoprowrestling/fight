namespace LoCoFight
{
    public enum AIState
    {
        IdleThink,
        Approach,
        Circle,
        BackOff,
        AttemptLightStrike,
        AttemptHeavyStrike,
        AttemptGrapple,
        ChooseGrappleMove,
        AttemptRunningAttack,
        UseRopeRebound,
        ForceOpponentToRopes,
        AttemptCornerSetup,
        AttemptSpecial,
        AttemptPin,
        AttemptSubmission,
        DefensiveReversal,
        DefensiveDodge,
        Recover,
        GetUp,
        Victory,
        Defeat
    }
}
