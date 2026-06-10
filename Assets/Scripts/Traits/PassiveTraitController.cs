using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Runtime owner of a wrestler's passive traits. Other systems query the
    /// aggregate effects; the controller handles thresholds, once-per-match
    /// usage, and UI announcements.
    public class PassiveTraitController : MonoBehaviour
    {
        WrestlerCore _core;
        readonly List<PassiveTraitData> _traits = new List<PassiveTraitData>();
        readonly HashSet<string> _usedOnce = new HashSet<string>();
        readonly HashSet<string> _announced = new HashSet<string>();

        float _ropeTrapBonusUntil;

        public IReadOnlyList<PassiveTraitData> Traits => _traits;

        public void Initialize(WrestlerCore core, List<PassiveTraitData> traits)
        {
            _core = core;
            _traits.Clear();
            if (traits != null) _traits.AddRange(traits);
        }

        public void ResetForMatch()
        {
            _usedOnce.Clear();
            _announced.Clear();
            _ropeTrapBonusUntil = 0f;
        }

        public bool HasHandshakeRitual => Find(PassiveTraitEffectType.HandshakeRitual) != null;

        PassiveTraitData Find(PassiveTraitEffectType type)
        {
            foreach (var t in _traits)
                if (t != null && t.gameplayEffectType == type)
                    return t;
            return null;
        }

        /// Tiered threshold value: returns value below tier 1, valueTier2 below tier 2, else 0.
        float TieredValue(PassiveTraitData t)
        {
            float hp = _core.Stats.HealthPercent;
            if (t.healthThresholdTier2 > 0f && hp < t.healthThresholdTier2)
            {
                Announce(t);
                return t.valueTier2;
            }
            if (t.healthThreshold > 0f && hp < t.healthThreshold)
            {
                Announce(t);
                return t.value;
            }
            if (t.healthThreshold <= 0f) return t.value;
            return 0f;
        }

        void Announce(PassiveTraitData t)
        {
            if (string.IsNullOrEmpty(t.uiMessage) || _announced.Contains(t.traitId)) return;
            _announced.Add(t.traitId);
            MatchHUD.TryShowMessage(t.uiMessage);
        }

        // ---------------- Aggregates queried by other systems ----------------

        public float StaminaRecoveryMult
        {
            get
            {
                float mult = 1f;
                var t = Find(PassiveTraitEffectType.StaminaRecoveryBonus);
                if (t != null) mult *= 1f + TieredValue(t);
                return mult;
            }
        }

        public float ReversalLeniencyBonus
        {
            get
            {
                var t = Find(PassiveTraitEffectType.ReversalLeniency);
                return t != null ? TieredValue(t) : 0f;
            }
        }

        public float ReversalStaminaCostMult
        {
            get
            {
                var t = Find(PassiveTraitEffectType.ReversalStaminaDiscount);
                return t != null ? 1f - TieredValue(t) : 1f;
            }
        }

        /// Kickout bonus, including Major Glory's once-per-match last-chance
        /// kickout when near the three count at low health.
        public float GetKickoutBonus(bool nearThreeCount)
        {
            float bonus = 0f;
            var k = Find(PassiveTraitEffectType.KickoutBonus);
            if (k != null) bonus += TieredValue(k);

            var last = Find(PassiveTraitEffectType.LastChanceKickout);
            if (last != null && nearThreeCount && !_usedOnce.Contains(last.traitId) &&
                _core.Stats.HealthPercent < last.healthThreshold)
            {
                bonus += last.value; // consumed in NotifyKickout on success
            }
            return bonus;
        }

        public void NotifyKickout(bool nearThreeCount)
        {
            var last = Find(PassiveTraitEffectType.LastChanceKickout);
            if (last != null && nearThreeCount && !_usedOnce.Contains(last.traitId) &&
                _core.Stats.HealthPercent < last.healthThreshold)
            {
                _usedOnce.Add(last.traitId);
                _core.Stats.AddMomentum(last.momentumOnTrigger);
                if (!string.IsNullOrEmpty(last.uiMessage)) MatchHUD.TryShowMessage(last.uiMessage);
                Debug.Log($"[Trait] {last.displayName} triggered");
            }
        }

        /// Johnny's Heavyweight Anchor: only heavyweights can lift him.
        public bool BlocksLiftBy(WrestlerCore attacker)
        {
            var t = Find(PassiveTraitEffectType.LiftImmunity);
            if (t == null) return false;
            var aStats = attacker.Stats.Data;
            return aStats == null || aStats.weightClass < WeightClass.Heavyweight;
        }

        public void NotifyLiftBlocked(WrestlerCore attacker)
        {
            var t = Find(PassiveTraitEffectType.LiftImmunity);
            if (t == null) return;
            _core.Stats.AddMomentum(t.momentumOnTrigger);
            if (!string.IsNullOrEmpty(t.uiMessage)) MatchHUD.TryShowMessage(t.uiMessage);
        }

        /// Johnny's Heart of Crash: once per match below tier-2 health, shorten
        /// a long downed duration.
        public float ModifyDownedDuration(float duration)
        {
            var t = Find(PassiveTraitEffectType.DownedDurationReduction);
            if (t == null || _usedOnce.Contains(t.traitId)) return duration;
            if (duration < 2f || _core.Stats.HealthPercent >= t.healthThreshold) return duration;
            _usedOnce.Add(t.traitId);
            _core.Stats.AddMomentum(t.momentumOnTrigger);
            if (!string.IsNullOrEmpty(t.uiMessage)) MatchHUD.TryShowMessage(t.uiMessage);
            Debug.Log($"[Trait] {t.displayName}: downed duration reduced");
            return duration * (1f - t.value);
        }

        /// Zeak's Clean Momentum: bonus momentum on clean offense.
        public void OnCleanOffense(MoveData move, bool isSpecial = false)
        {
            var t = Find(PassiveTraitEffectType.CleanMomentumBonus);
            if (t == null) return;
            if (move != null && move.HasTag(MoveTag.Dirty)) return;
            float pct = isSpecial || (move != null && move.HasTag(MoveTag.Aerial)) ? 0.08f : 0.05f;
            _core.Stats.AddMomentum(_core.Stats.MaxMomentum * pct);
        }

        /// Reversal hooks: Morgana's Smoke and Mirrors, Zeak's clean reversal bonus.
        public void NotifyReversal(bool nearRopes)
        {
            var smoke = Find(PassiveTraitEffectType.RopeTrapSetupBonus);
            if (smoke != null && nearRopes)
            {
                _ropeTrapBonusUntil = Time.time + smoke.duration;
                if (!string.IsNullOrEmpty(smoke.uiMessage)) MatchHUD.TryShowMessage(smoke.uiMessage);
            }

            var clean = Find(PassiveTraitEffectType.CleanMomentumBonus);
            if (clean != null)
                _core.Stats.AddMomentum(_core.Stats.MaxMomentum * 0.08f);
        }

        public float TarantulaRangeBonus =>
            Time.time < _ropeTrapBonusUntil ? 0.25f : 0f;

        /// Extra rope-stagger time inflicted on opponents while Smoke and Mirrors is live.
        public float OpponentRopeStaggerBonus =>
            Time.time < _ropeTrapBonusUntil ? 0.35f : 0f;
    }
}
