using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Ugly-but-functional animation: tilts and offsets the primitive body parts
    /// per state, and flashes the torso on hits/reversals/dodges.
    public class PlaceholderAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        WrestlerView _view;
        Coroutine _flash;
        Color _baseColor;
        bool _baseColorCached;

        public void Bind(WrestlerView view)
        {
            _view = view;
            CacheBaseColor();
        }

        void CacheBaseColor()
        {
            if (_view != null && _view.torsoRenderer != null)
            {
                _baseColor = _view.torsoRenderer.material.color;
                _baseColorCached = true;
            }
        }

        public void PlayMove(string animationStateName, string placeholderPoseName, float speed = 1f)
        {
            ApplyPose(placeholderPoseName);
        }

        public void PlayState(string stateName)
        {
            switch (stateName)
            {
                case "Downed":
                case "Pinned":
                case "RollingAway":
                    SetBodyTilt(85f); break;
                case "Stunned":
                    SetBodyTilt(15f); break;
                case "RopeStaggered":
                    SetBodyTilt(-20f); break;
                case "Cornered":
                    SetBodyTilt(-10f); break;
                case "GettingUp":
                    SetBodyTilt(40f); break;
                case "TurnbuckleClimb":
                case "AerialSetup":
                    SetBodyTilt(-8f); break;
                case "Pinning":
                case "SubmissionApplying":
                    SetBodyTilt(60f); break;
                case "SubmissionDefending":
                    SetBodyTilt(80f); break;
                case "RopeTrapLocked":
                    SetBodyTilt(-45f); break;
                case "Victory":
                    SetBodyTilt(0f); ArmsUp(true); return;
                case "Defeat":
                    SetBodyTilt(85f); break;
                default:
                    SetBodyTilt(0f); break;
            }
            ArmsUp(false);
        }

        public void SetMovementSpeed(float speed)
        {
            if (_view == null || _view.visualRoot == null) return;
            // Cheap walk-cycle indication: bob the body while moving.
            float bob = speed > 0.05f ? Mathf.Sin(Time.time * 10f * Mathf.Max(0.5f, speed)) * 0.04f : 0f;
            var p = _view.visualRoot.localPosition;
            p.y = bob;
            _view.visualRoot.localPosition = p;
        }

        public void TriggerHitReact() => Flash(Color.red);
        public void TriggerReversal() => Flash(Color.cyan);
        public void TriggerDodge() => Flash(Color.white);
        public void TriggerDowned() => SetBodyTilt(85f);
        public void TriggerGetUp() => SetBodyTilt(40f);
        public void TriggerRopeStagger() => SetBodyTilt(-20f);
        public void TriggerCornered() => SetBodyTilt(-10f);
        public void TriggerAerialLaunch() => SetBodyTilt(-30f);
        public void TriggerAerialLanding(bool hit) => Flash(hit ? Color.green : Color.magenta);
        public void TriggerSpecial(string specialId) => Flash(new Color(1f, 0.6f, 0f));

        void ApplyPose(string pose)
        {
            switch (pose)
            {
                case "strike": Punch(); break;
                case "grapple": ArmsForward(); break;
                case "special": Flash(new Color(1f, 0.6f, 0f)); ArmsForward(); break;
                default: break;
            }
        }

        void SetBodyTilt(float degrees)
        {
            if (_view == null || _view.visualRoot == null) return;
            _view.visualRoot.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }

        void Punch()
        {
            if (_view == null || _view.rightArm == null) return;
            StartCoroutine(ArmJab(_view.rightArm));
        }

        void ArmsForward()
        {
            if (_view == null || _view.leftArm == null) return;
            StartCoroutine(ArmJab(_view.leftArm));
            StartCoroutine(ArmJab(_view.rightArm));
        }

        void ArmsUp(bool up)
        {
            if (_view == null || _view.leftArm == null) return;
            var rot = up ? Quaternion.Euler(180f, 0f, 0f) : Quaternion.identity;
            _view.leftArm.localRotation = rot;
            _view.rightArm.localRotation = rot;
        }

        IEnumerator ArmJab(Transform arm)
        {
            Vector3 start = arm.localPosition;
            Vector3 outPos = start + new Vector3(0f, 0f, 0.35f);
            float t = 0f;
            while (t < 0.12f) { t += Time.deltaTime; arm.localPosition = Vector3.Lerp(start, outPos, t / 0.12f); yield return null; }
            t = 0f;
            while (t < 0.15f) { t += Time.deltaTime; arm.localPosition = Vector3.Lerp(outPos, start, t / 0.15f); yield return null; }
            arm.localPosition = start;
        }

        void Flash(Color color)
        {
            if (_view == null || _view.torsoRenderer == null) return;
            if (!_baseColorCached) CacheBaseColor();
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine(color));
        }

        IEnumerator FlashRoutine(Color color)
        {
            _view.torsoRenderer.material.color = color;
            yield return new WaitForSeconds(0.12f);
            if (_view.torsoRenderer != null) _view.torsoRenderer.material.color = _baseColor;
            _flash = null;
        }
    }
}
