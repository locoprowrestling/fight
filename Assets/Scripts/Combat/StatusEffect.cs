namespace LoCoFight
{
    /// A timed buff/debuff. Multipliers default to 1 (no effect), additive bonuses to 0.
    [System.Serializable]
    public class StatusEffect
    {
        public string Id = "effect";
        public string UiLabel = "";
        public float Remaining = 1f;

        public float StaminaRecoveryMult = 1f;
        public float KickoutMult = 1f;          // <1 = weaker kickouts
        public float MomentumGainMult = 1f;
        public float GetUpSpeedMult = 1f;
        public float MoveSpeedMult = 1f;
        public float ReversalStaminaCostMult = 1f;
        public float ReversalLeniencyBonus = 0f; // seconds added to reversal windows
        public float SubmissionEscapeMult = 1f;
        public float NextMoveMomentumBonus = 0f;

        public StatusEffect(string id, float duration)
        {
            Id = id;
            Remaining = duration;
        }
    }
}
