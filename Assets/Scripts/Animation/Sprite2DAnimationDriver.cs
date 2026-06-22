using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Procedural bone animation over the paper-doll rig. Implements the gameplay-facing
    /// IAnimationDriver so combat, AI, and state code are untouched.
    public class Sprite2DAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        WrestlerRig _rig;
        float _moveSpeed;
        float _cycle;
        float _torsoTilt;       // degrees, eased toward target
        float _torsoTiltTarget;
        Coroutine _swing;
        Coroutine _flash;

        public void Bind(WrestlerRig rig) => _rig = rig;

        void Update()
        {
            if (_rig == null) return;

            // Locomotion: swing thighs and arms by a sin cycle scaled by speed.
            _cycle += Time.deltaTime * (4f + _moveSpeed * 10f);
            float amp = Mathf.Clamp01(_moveSpeed) * 28f;
            float s = Mathf.Sin(_cycle) * amp;

            SetLocalZ(RigSlot.ThighNear, s);
            SetLocalZ(RigSlot.ThighFar, -s);
            SetLocalZ(RigSlot.ShinNear, -Mathf.Max(0f, s) * 0.6f);
            SetLocalZ(RigSlot.ShinFar, -Mathf.Max(0f, -s) * 0.6f);

            // Arms counter-swing unless a strike/grapple swing coroutine owns them.
            if (_swing == null)
            {
                SetLocalZ(RigSlot.UpperArmNear, -s * 0.6f);
                SetLocalZ(RigSlot.UpperArmFar, s * 0.6f);
            }

            // Torso tilt eases toward the current state's target.
            _torsoTilt = Mathf.Lerp(_torsoTilt, _torsoTiltTarget, Time.deltaTime * 10f);
            var torso = _rig.Joint(RigSlot.Torso);
            if (torso != null) torso.localRotation = Quaternion.Euler(0f, 0f, _torsoTilt);

            // Subtle idle bob.
            float bob = Mathf.Sin(Time.time * 6f) * (0.01f + _moveSpeed * 0.02f);
            var pelvis = _rig.Joint(RigSlot.Pelvis);
            if (pelvis != null)
            {
                var lp = pelvis.localPosition; lp.y = 0.78f + bob; pelvis.localPosition = lp;
            }
        }

        public void SetMovementSpeed(float speed) => _moveSpeed = speed;

        public void PlayMove(string animationStateName, string placeholderPoseName, float speed = 1f)
        {
            switch (placeholderPoseName)
            {
                case "strike": StartSwing(RigSlot.UpperArmNear, RigSlot.ForearmNear, -110f, 0.10f, 0.16f); break;
                case "grapple": StartSwing(RigSlot.UpperArmNear, RigSlot.ForearmNear, -70f, 0.12f, 0.18f, RigSlot.UpperArmFar, RigSlot.ForearmFar); break;
                case "special": Flash(new Color(1f, 0.6f, 0f)); StartSwing(RigSlot.UpperArmNear, RigSlot.ForearmNear, -120f, 0.08f, 0.20f, RigSlot.UpperArmFar, RigSlot.ForearmFar); break;
            }
        }

        public void PlayState(string stateName)
        {
            switch (stateName)
            {
                case "Downed":
                case "Pinned":
                case "RollingAway":
                case "Defeat": _torsoTiltTarget = 82f; break;
                case "Stunned": _torsoTiltTarget = 14f; break;
                case "RopeStaggered": _torsoTiltTarget = -18f; break;
                case "Cornered": _torsoTiltTarget = -10f; break;
                case "GettingUp": _torsoTiltTarget = 40f; break;
                case "TurnbuckleClimb":
                case "AerialSetup": _torsoTiltTarget = -8f; break;
                case "Pinning":
                case "SubmissionApplying": _torsoTiltTarget = 55f; break;
                case "SubmissionDefending": _torsoTiltTarget = 70f; break;
                case "RopeTrapLocked": _torsoTiltTarget = -42f; break;
                case "Victory": _torsoTiltTarget = 0f; ArmsUp(); break;
                default: _torsoTiltTarget = 0f; break;
            }
        }

        public void TriggerHitReact() { Flash(Color.red); _torsoTiltTarget = Mathf.Max(_torsoTiltTarget, 18f); }
        public void TriggerReversal() => Flash(Color.cyan);
        public void TriggerDodge() => Flash(Color.white);
        public void TriggerDowned() => _torsoTiltTarget = 82f;
        public void TriggerGetUp() => _torsoTiltTarget = 40f;
        public void TriggerRopeStagger() => _torsoTiltTarget = -18f;
        public void TriggerCornered() => _torsoTiltTarget = -10f;
        public void TriggerAerialLaunch() => _torsoTiltTarget = -28f;
        public void TriggerAerialLanding(bool hit) => Flash(hit ? Color.green : Color.magenta);
        public void TriggerSpecial(string specialId) => Flash(new Color(1f, 0.6f, 0f));

        void ArmsUp()
        {
            SetLocalZ(RigSlot.UpperArmNear, 150f);
            SetLocalZ(RigSlot.UpperArmFar, -150f);
        }

        void StartSwing(RigSlot upper, RigSlot fore, float peakDeg, float outT, float inT,
            RigSlot upper2 = RigSlot.Pelvis, RigSlot fore2 = RigSlot.Pelvis)
        {
            if (_swing != null) StopCoroutine(_swing);
            _swing = StartCoroutine(SwingRoutine(upper, fore, peakDeg, outT, inT, upper2, fore2));
        }

        IEnumerator SwingRoutine(RigSlot upper, RigSlot fore, float peak, float outT, float inT, RigSlot upper2, RigSlot fore2)
        {
            float t = 0f;
            while (t < outT)
            {
                t += Time.deltaTime;
                float k = t / outT;
                SetLocalZ(upper, Mathf.Lerp(0f, peak, k));
                SetLocalZ(fore, Mathf.Lerp(0f, peak * 0.5f, k));
                if (upper2 != RigSlot.Pelvis) SetLocalZ(upper2, Mathf.Lerp(0f, -peak, k));
                yield return null;
            }
            t = 0f;
            while (t < inT)
            {
                t += Time.deltaTime;
                float k = t / inT;
                SetLocalZ(upper, Mathf.Lerp(peak, 0f, k));
                SetLocalZ(fore, Mathf.Lerp(peak * 0.5f, 0f, k));
                if (upper2 != RigSlot.Pelvis) SetLocalZ(upper2, Mathf.Lerp(-peak, 0f, k));
                yield return null;
            }
            _swing = null;
        }

        void SetLocalZ(RigSlot slot, float degrees)
        {
            var t = _rig.Joint(slot);
            if (t != null) t.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        void Flash(Color color)
        {
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine(color));
        }

        IEnumerator FlashRoutine(Color color)
        {
            var torso = _rig.Joint(RigSlot.Torso);
            var sr = torso != null ? torso.GetComponent<SpriteRenderer>() : null;
            if (sr == null) { _flash = null; yield break; }
            Color baseColor = sr.color;
            sr.color = color;
            yield return new WaitForSeconds(0.12f);
            sr.color = baseColor;
            _flash = null;
        }
    }
}
