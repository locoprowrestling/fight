using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Tier pacing: a move is attemptable only with the greater of its spend
    /// cost and its authored minimum stamina. Only staminaCost is ever spent —
    /// minimumStamina is a gate, not a price, and nothing here rewrites
    /// authored move values at runtime.
    public static class MovePacingRules
    {
        public static float RequiredStamina(MoveData move)
        {
            if (move == null) return float.PositiveInfinity;
            return Mathf.Max(move.staminaCost, move.minimumStamina);
        }

        public static bool CanAttempt(MoveData move, float currentStamina)
        {
            return move != null && currentStamina >= RequiredStamina(move);
        }

        /// Lowest-requirement candidate the attacker can afford right now, or
        /// null. Used to downgrade an unaffordable pick instead of silently
        /// failing it.
        public static MoveData CheapestAffordable(
            IEnumerable<MoveData> candidates,
            float currentStamina)
        {
            MoveData best = null;
            if (candidates == null) return null;
            foreach (MoveData move in candidates)
            {
                if (!CanAttempt(move, currentStamina)) continue;
                if (best == null || RequiredStamina(move) < RequiredStamina(best))
                    best = move;
            }
            return best;
        }
    }
}
