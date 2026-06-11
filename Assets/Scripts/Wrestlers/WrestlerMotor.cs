using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Movement only: walking, running, facing, knockback, rope rebound running,
    /// and scripted repositioning for specials. No damage, no rules.
    [RequireComponent(typeof(CharacterController))]
    public class WrestlerMotor : MonoBehaviour
    {
        public float rotationSpeed = 12f;
        public const float CloseCombatRange = 1.5f;
        public const float ReboundSpeedMultiplier = 1.15f;
        public const float ReboundTurnDuration = 0.2f;
        public const float ReboundControlLock = 0.3f;

        const float Acceleration = 26f;
        const float Deceleration = 34f;

        WrestlerCore _core;
        CharacterController _cc;
        Vector3 _moveInput;
        Vector3 _smoothedMotion;
        bool _runInput;
        bool _scriptedControl;
        float _verticalVelocity;
        Coroutine _knockback;
        Coroutine _rebound;

        public bool IsRunning { get; private set; }
        public float CurrentSpeedNormalized { get; private set; }

        public void Bind(WrestlerCore core)
        {
            _core = core;
            _cc = GetComponent<CharacterController>();
        }

        public void SetMoveInput(Vector3 worldDirection, bool run)
        {
            _moveInput = MathUtil.Flat(worldDirection);
            if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();
            _runInput = run;
        }

        void Update()
        {
            if (_core == null || _scriptedControl) return;

            var profile = _core.States.Profile;
            Vector3 motion = Vector3.zero;
            float speed = 0f;

            if (profile.canMove && _moveInput.sqrMagnitude > 0.001f)
            {
                var stats = _core.Stats.Data;
                float walkSpeed = stats != null ? stats.walkSpeed : 3f;
                float runSpeed = stats != null ? stats.runSpeed : 5.5f;
                bool canRun = _runInput && _core.Stats.Stamina > 1f;
                IsRunning = canRun;
                speed = (canRun ? runSpeed : walkSpeed) * _core.Buffs.MoveSpeedMult;
                motion = _moveInput * speed;

                if (canRun)
                {
                    _core.Stats.DrainStamina(4f * Time.deltaTime);
                    if (_core.States.Current != WrestlerState.Running) _core.States.Set(WrestlerState.Running);
                }
                else if (_core.States.Current == WrestlerState.Idle || _core.States.Current == WrestlerState.Running)
                {
                    _core.States.Set(WrestlerState.Moving);
                }

                if (profile.canRotate)
                {
                    var targetRot = Quaternion.LookRotation(_moveInput);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }

                TryAutoRebound();
            }
            else
            {
                IsRunning = false;
                if (_core.States.Current == WrestlerState.Moving || _core.States.Current == WrestlerState.Running)
                    _core.States.Set(WrestlerState.Idle);

                // Auto-face the opponent at close range when standing still.
                if (profile.canRotate && _core.Opponent != null &&
                    _core.DistanceToOpponent() <= CloseCombatRange)
                {
                    FaceOpponent(rotationSpeed * Time.deltaTime);
                }
            }

            // Weight: ramp toward the target velocity instead of snapping,
            // so starts, stops, and turns read as footwork rather than glide.
            float rate = motion.sqrMagnitude >= _smoothedMotion.sqrMagnitude ? Acceleration : Deceleration;
            _smoothedMotion = Vector3.MoveTowards(_smoothedMotion, motion, rate * Time.deltaTime);

            float runSpeedRef = _core.Stats.Data != null ? _core.Stats.Data.runSpeed : 5.5f;
            CurrentSpeedNormalized = _smoothedMotion.magnitude / Mathf.Max(0.1f, runSpeedRef);
            // Anim is Bind()-wired and dies on a mid-play domain reload
            // (script recompile while playing); skip presentation that frame.
            _core.Anim?.SetMovementSpeed(CurrentSpeedNormalized);

            ApplyGravityAndMove(_smoothedMotion);
        }

        void ApplyGravityAndMove(Vector3 horizontal)
        {
            if (_cc == null || !_cc.enabled) return;
            _verticalVelocity = _cc.isGrounded ? -1f : _verticalVelocity - 20f * Time.deltaTime;
            var motion = horizontal;
            motion.y = _verticalVelocity;
            _cc.Move(motion * Time.deltaTime);
        }

        void TryAutoRebound()
        {
            if (!IsRunning || _rebound != null) return;
            var ring = RingInteractionSystem.Instance;
            if (ring == null) return;
            float dist = ring.Bounds.DistanceToNearestRope(transform.position, out var side);
            if (dist > RingInteractionSystem.RopeReboundActivationRange) return;
            // Only rebound when actually running toward that rope.
            Vector3 outward = -ring.Bounds.RopeInwardDirection(side);
            if (Vector3.Dot(_moveInput, outward) < 0.5f) return;
            BeginRopeRebound(side);
        }

        public void BeginRopeRebound(RopeSide side)
        {
            if (_rebound != null) StopCoroutine(_rebound);
            _rebound = StartCoroutine(ReboundRoutine(side));
        }

        IEnumerator ReboundRoutine(RopeSide side)
        {
            var ring = RingInteractionSystem.Instance;
            _core.States.Set(WrestlerState.RopeReboundRun, 2f);
            Debug.Log($"[Rope] {_core.DisplayName} rebounds off the {side} ropes");

            Vector3 inward = ring.Bounds.RopeInwardDirection(side);

            // Turn back into the ring.
            float t = 0f;
            Quaternion startRot = transform.rotation;
            Quaternion endRot = Quaternion.LookRotation(inward);
            while (t < ReboundTurnDuration)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(startRot, endRot, t / ReboundTurnDuration);
                yield return null;
            }

            // Locked sprint back toward center.
            float speed = (_core.Stats.Data != null ? _core.Stats.Data.runSpeed : 5.5f) * ReboundSpeedMultiplier;
            t = 0f;
            while (t < ReboundControlLock)
            {
                t += Time.deltaTime;
                ApplyGravityAndMove(inward * speed);
                yield return null;
            }

            _core.States.Set(WrestlerState.RopeReboundReturn, 1.2f);
            _rebound = null;
        }

        public void CancelRebound()
        {
            if (_rebound != null) { StopCoroutine(_rebound); _rebound = null; }
        }

        public void FaceOpponent(float maxStep = 1f)
        {
            if (_core.Opponent == null) return;
            FaceDirection(MathUtil.FlatDirection(transform.position, _core.Opponent.transform.position), maxStep);
        }

        public void FaceDirection(Vector3 dir, float maxStep = 1f)
        {
            dir = MathUtil.Flat(dir);
            if (dir.sqrMagnitude < 0.001f) return;
            var target = Quaternion.LookRotation(dir.normalized);
            transform.rotation = maxStep >= 1f ? target : Quaternion.Slerp(transform.rotation, target, maxStep);
        }

        public void Teleport(Vector3 position)
        {
            _smoothedMotion = Vector3.zero;
            bool wasEnabled = _cc != null && _cc.enabled;
            if (_cc != null) _cc.enabled = false;
            transform.position = position;
            if (_cc != null) _cc.enabled = wasEnabled;
        }

        /// While scripted control is on, normal input/gravity is suspended and
        /// special executors may move the transform directly.
        public void SetScriptedControl(bool on)
        {
            _scriptedControl = on;
            if (_cc != null) _cc.enabled = !on;
            if (on) CancelRebound();
        }

        public void ApplyKnockback(Vector3 direction, float distance)
        {
            _smoothedMotion = Vector3.zero;
            if (_knockback != null) StopCoroutine(_knockback);
            _knockback = StartCoroutine(KnockbackRoutine(MathUtil.Flat(direction).normalized, distance));
        }

        IEnumerator KnockbackRoutine(Vector3 dir, float distance)
        {
            float duration = 0.15f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (_cc != null && _cc.enabled) _cc.Move(dir * (distance / duration) * Time.deltaTime);
                yield return null;
            }
            _knockback = null;
        }
    }
}
