using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/AI Difficulty")]
    public class AIDifficultyData : ScriptableObject
    {
        public string displayName = "Normal";

        [Range(0f, 1f)] public float aggression = 0.55f;
        [Range(0f, 1f)] public float grapplePreference = 0.5f;
        [Range(0f, 1f)] public float strikePreference = 0.5f;
        [Range(0f, 1f)] public float ropeStrategyPreference = 0.35f;
        [Range(0f, 1f)] public float cornerStrategyPreference = 0.3f;
        [Range(0f, 1f)] public float specialSetupPreference = 0.8f;
        [Range(0f, 1f)] public float reversalAccuracy = 0.4f;
        [Range(0f, 1f)] public float dodgeAccuracy = 0.3f;
        public float reactionDelayMin = 0.3f;
        public float reactionDelayMax = 0.6f;
        [Tooltip("Pin when opponent health percent is at or below this value.")]
        [Range(0f, 1f)] public float pinAttemptThreshold = 0.6f;
        [Range(0f, 1f)] public float submissionAttemptThreshold = 0.45f;
        [Range(0f, 1f)] public float staminaCautionThreshold = 0.25f;
        [Range(0f, 1f)] public float randomness = 0.25f;
        public float kickoutBonus = 0f;
        public float submissionEscapeBonus = 0f;
        public float reversalCooldown = 0.8f;
    }
}
