using UnityEngine;

namespace LoCoFight
{
    /// One candidate CPU action with its composed weight.
    public readonly struct WeightedAIAction
    {
        public readonly AIState Action;
        public readonly float Weight;

        public WeightedAIAction(AIState action, float weight)
        {
            Action = action;
            Weight = weight;
        }
    }

    /// Pure composition of difficulty base weights, bounded personality
    /// multipliers, and repetition penalties. Accepts no accuracy values:
    /// difficulty alone owns whether the CPU succeeds at reactions.
    public static class AIDecisionWeights
    {
        public static float Apply(
            float baseWeight,
            float personalityMultiplier,
            float repetitionPenalty) =>
            Mathf.Clamp01(
                Mathf.Clamp01(baseWeight) *
                Mathf.Clamp(
                    personalityMultiplier,
                    AIPersonalityProfile.MinMultiplier,
                    AIPersonalityProfile.MaxMultiplier) *
                Mathf.Clamp01(1f - repetitionPenalty));

        /// Deterministic weighted pick for a supplied 0..1 roll. Non-positive
        /// weights are skipped; with no positive weight the CPU returns to
        /// IdleThink so a starved decision can never freeze the FSM.
        public static AIState ChooseWeighted(
            float roll,
            params WeightedAIAction[] actions)
        {
            if (actions == null || actions.Length == 0) return AIState.IdleThink;

            float total = 0f;
            foreach (var action in actions) total += Mathf.Max(0f, action.Weight);
            if (total <= 0f) return AIState.IdleThink;

            float pick = Mathf.Clamp01(roll) * total;
            float cumulative = 0f;
            AIState lastPositive = AIState.IdleThink;
            foreach (var action in actions)
            {
                float weight = Mathf.Max(0f, action.Weight);
                if (weight <= 0f) continue;
                cumulative += weight;
                lastPositive = action.Action;
                if (pick <= cumulative) return action.Action;
            }
            return lastPositive;
        }
    }
}
