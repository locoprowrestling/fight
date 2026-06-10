using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Passive Trait")]
    public class PassiveTraitData : ScriptableObject
    {
        public string traitId;
        public string displayName;
        public string owningRosterId;
        [TextArea] public string description;

        public PassiveTraitTrigger triggerCondition = PassiveTraitTrigger.Continuous;
        public PassiveTraitEffectType gameplayEffectType;

        [Header("Thresholds (percent 0..1, 0 = unused)")]
        public float healthThreshold = 0f;
        [Tooltip("Stronger second tier (e.g. below 25% instead of 50%). 0 = unused.")]
        public float healthThresholdTier2 = 0f;

        [Header("Effect values")]
        [Tooltip("Primary effect magnitude. Meaning depends on effect type (e.g. 0.15 = +15% recovery).")]
        public float value = 0f;
        [Tooltip("Tier-2 magnitude when below healthThresholdTier2.")]
        public float valueTier2 = 0f;
        public float momentumOnTrigger = 0f;
        public float cooldown = 0f;
        public float duration = 0f;
        public bool oncePerMatch = false;
        public string uiMessage = "";

        [Header("Effect flags (for debug overlay)")]
        public bool affectsReversals;
        public bool affectsStaminaRecovery;
        public bool affectsKickout;
        public bool affectsDodge;
        public bool affectsLiftRules;
        public bool affectsCleanMomentum;
        public bool affectsSpecialStartup;
        public bool affectsSubmissionEscape;
        public bool affectsDownedDuration;
    }
}
