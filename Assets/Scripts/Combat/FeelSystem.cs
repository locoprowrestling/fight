using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Presentation-only impact feedback: tier-scaled hit-stop and camera
    /// punches, driven by impact notifications from the combat routines.
    /// Self-bootstrapping; disabling it (F1 overlay shows the toggle state,
    /// `FeelSystem.Enabled = false`) changes nothing about match outcomes.
    public class FeelSystem : MonoBehaviour
    {
        public static bool Enabled = true;

        /// Last impact notification, for the F1 overlay.
        public static string LastImpactDebug { get; private set; } = "-";

        static FeelSystem _instance;

        TwoTargetMatchCamera _camera;
        float _hitStopUntil; // unscaled time
        bool _stopping;

        static FeelSystem EnsureInstance()
        {
            if (_instance == null)
            {
                var go = new GameObject("FeelSystem");
                _instance = go.AddComponent<FeelSystem>();
            }
            return _instance;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_stopping && (MatchManager.Instance == null || !MatchManager.Instance.IsPaused))
                Time.timeScale = 1f;
        }

        /// A move connected. Heavier tiers freeze the frame longer; downing
        /// impacts also punch the camera.
        public static void NotifyImpact(MoveTier tier, bool downsDefender)
        {
            LastImpactDebug = $"{tier} downs={downsDefender} @{Time.unscaledTime:0.0}";
            if (!Enabled) return;
            float stop = tier == MoveTier.Light ? 0.03f
                : tier == MoveTier.Medium ? 0.05f
                : 0.08f; // Heavy and Special
            FeelSystem fs = EnsureInstance();
            fs.HitStop(stop);
            if (downsDefender || tier == MoveTier.Heavy || tier == MoveTier.Special)
                fs.PunchCamera(downsDefender ? 0.35f : 0.2f);
        }

        void HitStop(float duration)
        {
            // Extend rather than stack, so rapid light hits never accumulate
            // into slow motion.
            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + duration);
            if (!_stopping) StartCoroutine(HitStopRoutine());
        }

        IEnumerator HitStopRoutine()
        {
            var mm = MatchManager.Instance;
            if (mm != null && mm.IsPaused) yield break; // never fight the pause
            _stopping = true;
            Time.timeScale = 0.05f;
            while (Time.unscaledTime < _hitStopUntil)
            {
                yield return null;
                mm = MatchManager.Instance;
                if (mm != null && mm.IsPaused) break; // pause takes over
            }
            _stopping = false;
            mm = MatchManager.Instance;
            if (mm == null || !mm.IsPaused) Time.timeScale = 1f;
        }

        void PunchCamera(float strength)
        {
            if (_camera == null) _camera = FindAnyObjectByType<TwoTargetMatchCamera>();
            if (_camera != null) _camera.Punch(strength);
        }
    }
}
