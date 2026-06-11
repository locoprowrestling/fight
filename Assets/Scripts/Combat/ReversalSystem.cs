using UnityEngine;

namespace LoCoFight
{
    /// Resolved values for one reversal: what the defender gains and what the
    /// attacker suffers. Built from authored MoveData, never mutated back.
    public readonly struct ReversalOutcomeData
    {
        public readonly float Momentum;
        public readonly float Stagger;
        public readonly float Separation;
        public readonly string PresentationId;

        public ReversalOutcomeData(
            float momentum,
            float stagger,
            float separation,
            string presentationId)
        {
            Momentum = momentum;
            Stagger = stagger;
            Separation = separation;
            PresentationId = presentationId;
        }
    }

    /// Reversal costs and window math, shared by human and CPU paths.
    public static class ReversalSystem
    {
        public const float StrikeReversalCost = 8f;
        public const float GrappleReversalCost = 12f;
        public const float SpecialReversalCost = 18f;
        public const float HumanReversalCooldown = 0.35f;
        public const float GrappleLockEscapeWindow = 0.45f;

        // Built-in defaults when a move carries no reversal metadata.
        const float DefaultBasicMomentum = 8f;
        const float DefaultStrongMomentum = 14f;
        const float DefaultBasicStagger = 0.8f;
        const float DefaultStrongStagger = 1.2f;
        const float DefaultBasicSeparation = 0.7f;
        const float DefaultStrongSeparation = 1.25f;
        public const string DefaultBasicPresentationId = "reversal-basic";
        public const string DefaultStrongPresentationId = "reversal-strong";

        /// Maps a resolved read outcome to the authored momentum, stagger,
        /// separation, and presentation values. momentumGainOnReversal is the
        /// pre-read legacy field; it backfills basic momentum only when the
        /// authored basic value is unset (<= 0).
        public static ReversalOutcomeData ResolveOutcome(
            MoveData move,
            ReversalOutcome outcome)
        {
            if (move == null)
            {
                return outcome == ReversalOutcome.Strong
                    ? new ReversalOutcomeData(
                        DefaultStrongMomentum, DefaultStrongStagger,
                        DefaultStrongSeparation, DefaultStrongPresentationId)
                    : new ReversalOutcomeData(
                        DefaultBasicMomentum, DefaultBasicStagger,
                        DefaultBasicSeparation, DefaultBasicPresentationId);
            }

            if (outcome == ReversalOutcome.Strong)
            {
                return new ReversalOutcomeData(
                    move.strongReversalMomentum,
                    move.strongReversalStagger,
                    move.strongReversalSeparation,
                    string.IsNullOrEmpty(move.strongReversalPresentationId)
                        ? DefaultStrongPresentationId
                        : move.strongReversalPresentationId);
            }

            float momentum = move.basicReversalMomentum > 0f
                ? move.basicReversalMomentum
                : move.momentumGainOnReversal;
            return new ReversalOutcomeData(
                momentum,
                move.basicReversalStagger,
                move.basicReversalSeparation,
                string.IsNullOrEmpty(move.basicReversalPresentationId)
                    ? DefaultBasicPresentationId
                    : move.basicReversalPresentationId);
        }

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
