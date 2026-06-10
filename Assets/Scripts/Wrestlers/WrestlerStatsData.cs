using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Wrestler Stats")]
    public class WrestlerStatsData : ScriptableObject
    {
        public string displayName;

        [Header("Meters")]
        public float maxHealth = 100f;
        public float maxStamina = 100f;
        public float maxMomentum = 100f;
        public float staminaRecoveryPerSecond = 12f;
        public float momentumDecayPerSecond = 0f;

        [Header("Movement")]
        public float walkSpeed = 3f;
        public float runSpeed = 5.5f;

        [Header("Modifiers (1 = baseline)")]
        public float baseStrikeDamageModifier = 1f;
        public float baseGrappleDamageModifier = 1f;
        public float baseAerialDamageModifier = 1f;
        public float baseSubmissionModifier = 1f;

        [Header("Skills (0..1)")]
        [Range(0f, 1f)] public float reversalSkill = 0.5f;
        [Range(0f, 1f)] public float dodgeSkill = 0.5f;
        [Range(0f, 1f)] public float kickoutSkill = 0.5f;
        [Range(0f, 1f)] public float submissionResistance = 0.5f;
        [Range(0.5f, 2f)] public float getUpSpeed = 1f;

        [Header("Body")]
        public WeightClass weightClass = WeightClass.Middleweight;
        public LiftStrengthClass liftStrengthClass = LiftStrengthClass.Average;

        [Header("AI")]
        public AIPersonality aiPersonality = AIPersonality.Balanced;
    }
}
