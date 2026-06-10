using UnityEngine;

namespace LoCoFight
{
    /// Simple countdown helper used by states, cooldowns, and counts.
    [System.Serializable]
    public struct Timer
    {
        public float Duration;
        public float Remaining;

        public bool Running => Remaining > 0f;
        public bool Done => Remaining <= 0f;
        public float Elapsed => Duration - Remaining;
        public float Normalized => Duration <= 0f ? 1f : Mathf.Clamp01(Elapsed / Duration);

        public void Start(float duration)
        {
            Duration = duration;
            Remaining = duration;
        }

        public void Stop() => Remaining = 0f;

        /// Returns true on the tick the timer completes.
        public bool Tick(float dt)
        {
            if (Remaining <= 0f) return false;
            Remaining -= dt;
            return Remaining <= 0f;
        }
    }
}
