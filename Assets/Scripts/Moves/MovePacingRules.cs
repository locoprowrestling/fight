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
    }
}
