using UnityEngine;

namespace LoCoFight
{
    /// Reversal costs and window math, shared by human and CPU paths.
    public static class ReversalSystem
    {
        public const float StrikeReversalCost = 8f;
        public const float GrappleReversalCost = 12f;
        public const float SpecialReversalCost = 18f;
        public const float HumanReversalCooldown = 0.35f;
        public const float GrappleLockEscapeWindow = 0.45f;

        public static float CostFor(MoveCategory category)
        {
            switch (category)
            {
                case MoveCategory.LightStrike:
                case MoveCategory.HeavyStrike:
                case MoveCategory.RunningStrike:
                case MoveCategory.RopeReboundAttack:
                    return StrikeReversalCost;
                default:
                    return GrappleReversalCost;
            }
        }

        /// Total extra time added to reversal windows for this defender.
        public static float LeniencyFor(WrestlerCore defender)
        {
            float leniency = defender.Buffs.ReversalLeniencyBonus;
            if (defender.Traits != null) leniency += defender.Traits.ReversalLeniencyBonus;
            return leniency;
        }

        public static float StaminaCostFor(WrestlerCore defender, float baseCost)
        {
            float mult = defender.Buffs.ReversalStaminaCostMult;
            if (defender.Traits != null) mult *= defender.Traits.ReversalStaminaCostMult;
            return baseCost * mult;
        }
    }
}
