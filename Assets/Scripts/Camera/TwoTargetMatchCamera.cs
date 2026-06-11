using UnityEngine;

namespace LoCoFight
{
    /// Broadcast-style two-target camera: tracks the midpoint, zooms with
    /// separation, boosts height during aerial moves. No player camera input.
    public class TwoTargetMatchCamera : MonoBehaviour
    {
        public Transform targetA;
        public Transform targetB;

        public float baseDistance = 7.5f;
        public float minDistance = 5.5f;
        public float maxDistance = 12.5f;
        public float height = 4f;
        public float aerialHeightBoost = 1.2f;
        public float lookAtHeightOffset = 1.5f; // tracks mid-torso of the 1.25x-scaled rigs
        public float smoothTime = 0.15f;
        [Tooltip("Fixed diagonal view direction from outside the ring.")]
        public Vector3 viewDirection = new Vector3(0.45f, 0f, -1f);

        Vector3 _velocity;
        float _currentDistance;

        public void SetTargets(Transform a, Transform b)
        {
            targetA = a;
            targetB = b;
            _currentDistance = baseDistance;
            SnapNow();
        }

        void SnapNow()
        {
            if (targetA == null || targetB == null) return;
            transform.position = ComputeDesiredPosition(out Vector3 look);
            transform.LookAt(look);
        }

        void LateUpdate()
        {
            if (targetA == null || targetB == null) return;
            Vector3 desired = ComputeDesiredPosition(out Vector3 look);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            transform.LookAt(look);
        }

        Vector3 ComputeDesiredPosition(out Vector3 lookAt)
        {
            Vector3 mid = (targetA.position + targetB.position) * 0.5f;
            float separation = Vector3.Distance(targetA.position, targetB.position);

            float desiredDistance = Mathf.Clamp(baseDistance + (separation - 3f) * 0.9f, minDistance, maxDistance);
            _currentDistance = Mathf.Lerp(_currentDistance, desiredDistance, Time.deltaTime * 4f);

            float h = height;
            if (IsAerial(targetA) || IsAerial(targetB)) h += aerialHeightBoost;

            Vector3 dir = MathUtil.Flat(viewDirection).normalized;
            lookAt = mid + Vector3.up * lookAtHeightOffset;
            return mid + dir * _currentDistance + Vector3.up * h;
        }

        static bool IsAerial(Transform t)
        {
            var core = t.GetComponent<WrestlerCore>();
            if (core == null) return false;
            var s = core.States.Current;
            return s == WrestlerState.AerialAirborne || s == WrestlerState.TurnbuckleClimb ||
                   s == WrestlerState.AerialSetup || s == WrestlerState.SpecialActive;
        }
    }
}
