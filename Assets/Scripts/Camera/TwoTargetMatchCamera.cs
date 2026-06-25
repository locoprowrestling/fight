using UnityEngine;

namespace LoCoFight
{
    /// Orthographic, straight-on broadcast camera for the side-on 2D view.
    /// Tracks the horizontal midpoint and zooms (orthographic size) with separation.
    public class TwoTargetMatchCamera : MonoBehaviour
    {
        public Transform targetA;
        public Transform targetB;

        [Header("Framing")]
        public float baseSize = 3.2f;
        public float sizePerUnit = 0.45f;
        public float minSize = 3.0f;
        public float maxSize = 6.5f;
        public float aerialSizeBoost = 0.8f;

        [Header("Placement")]
        [Tooltip("Vertical center of the framing, in world units above the mat.")]
        public float framingHeight = 1.3f;
        [Tooltip("Distance the camera sits in front of the ring along Z.")]
        public float cameraDepth = 12f;
        [Tooltip("+1 views from -Z, -1 views from +Z. Flip once if the scene renders mirrored.")]
        public float viewSide = 1f;
        public float smoothTime = 0.12f;

        UnityEngine.Camera _cam;
        Vector3 _velocity;
        float _currentSize;

        public void SetTargets(Transform a, Transform b)
        {
            targetA = a;
            targetB = b;
            EnsureCamera();
            _currentSize = baseSize;
            transform.position = ComputeDesiredPosition();
            transform.rotation = ComputeRotation();
            _cam.orthographic = true;
            _cam.orthographicSize = _currentSize;
        }

        void EnsureCamera()
        {
            if (_cam == null) _cam = GetComponent<UnityEngine.Camera>();
            if (_cam != null) _cam.orthographic = true;
        }

        void LateUpdate()
        {
            if (targetA == null || targetB == null) return;
            EnsureCamera();

            Vector3 desired = ComputeDesiredPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            transform.rotation = ComputeRotation();

            float separation = Mathf.Abs(targetA.position.x - targetB.position.x);
            float targetSize = CameraFraming.OrthographicSizeFor(separation, baseSize, sizePerUnit, minSize, maxSize);
            if (IsAerial(targetA) || IsAerial(targetB)) targetSize += aerialSizeBoost;
            _currentSize = Mathf.Lerp(_currentSize, targetSize, Time.deltaTime * 6f);
            if (_cam != null) _cam.orthographicSize = _currentSize;
        }

        Vector3 ComputeDesiredPosition()
        {
            float midX = CameraFraming.MidpointX(targetA.position.x, targetB.position.x);
            return new Vector3(midX, framingHeight, -viewSide * cameraDepth);
        }

        Quaternion ComputeRotation()
        {
            // Look straight along Z toward the ring.
            return Quaternion.LookRotation(new Vector3(0f, 0f, viewSide), Vector3.up);
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
