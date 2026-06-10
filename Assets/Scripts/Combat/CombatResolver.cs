using UnityEngine;

namespace LoCoFight
{
    /// Stateless combat math: damage scaling, lift validation, grapple ties.
    public static class CombatResolver
    {
        public static float ScaleDamage(WrestlerCore attacker, MoveData move)
        {
            var s = attacker.Stats.Data;
            if (s == null) return move.damage;
            switch (move.category)
            {
                case MoveCategory.LightStrike:
                case MoveCategory.HeavyStrike:
                case MoveCategory.RunningStrike:
                case MoveCategory.RopeReboundAttack:
                    return move.damage * s.baseStrikeDamageModifier;
                case MoveCategory.QuickGrapple:
                case MoveCategory.PowerGrapple:
                case MoveCategory.RunningGrapple:
                    return move.damage * s.baseGrappleDamageModifier;
                default:
                    return move.damage;
            }
        }

        /// Lift rules: the attacker's lift class must cover the defender's weight class,
        /// and passive traits (Johnny's Heavyweight Anchor) can veto the lift outright.
        public static bool ValidateLift(WrestlerCore attacker, WrestlerCore defender, LiftStrengthClass minimumClass, out string failReason)
        {
            failReason = null;
            var aStats = attacker.Stats.Data;
            var dStats = defender.Stats.Data;
            if (aStats == null || dStats == null) return true;

            if (aStats.liftStrengthClass < minimumClass)
            {
                failReason = "Not strong enough for this move!";
                return false;
            }

            if (defender.Traits != null && defender.Traits.BlocksLiftBy(attacker))
            {
                failReason = "Too heavy to lift!";
                return false;
            }

            // General weight check: each lift class can lift up to a weight class.
            int liftCap;
            switch (aStats.liftStrengthClass)
            {
                case LiftStrengthClass.Low: liftCap = (int)WeightClass.Lightweight; break;
                case LiftStrengthClass.Average: liftCap = (int)WeightClass.Middleweight; break;
                case LiftStrengthClass.Strong: liftCap = (int)WeightClass.Heavyweight; break;
                default: liftCap = (int)WeightClass.SuperHeavyweight; break;
            }
            if ((int)dStats.weightClass > liftCap)
            {
                failReason = "Too heavy to lift!";
                return false;
            }
            return true;
        }

        /// Simultaneous grapple resolution (both attempted within 0.2s):
        ///   40% timing advantage (earlier press wins more often)
        ///   40% stamina ratio
        ///   20% random factor
        /// Returns true when 'a' wins the tie-up.
        public static bool ResolveGrappleTie(WrestlerCore a, WrestlerCore b, float aAttemptTime, float bAttemptTime)
        {
            float timingScore = aAttemptTime <= bAttemptTime ? 0.4f : 0f;
            float total = a.Stats.Stamina + b.Stats.Stamina;
            float staminaScore = total <= 0f ? 0.2f : (a.Stats.Stamina / total) * 0.4f;
            float randomScore = Random.value * 0.2f;
            return timingScore + staminaScore + randomScore >= 0.5f;
        }
    }
}
