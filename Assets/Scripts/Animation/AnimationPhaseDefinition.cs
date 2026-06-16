using UnityEngine;

namespace LoCoFight
{
    [System.Serializable]
    public sealed class AnimationPhaseDefinition
    {
        public AnimationPhase phase;
        [Range(0f, 1f)] public float normalizedStart;
        [Range(0f, 1f)] public float normalizedEnd = 1f;
        public bool allowsInterruption;
        public bool allowsSpecialEscape;
        public string presentationMarker;
    }
}
