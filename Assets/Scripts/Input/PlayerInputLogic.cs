using UnityEngine;

namespace LoCoFight
{
    public enum GrapplePressAction { None, Quick, Power }

    public static class PlayerInputLogic
    {
        /// Single tunable feel constant: presses shorter than this are taps,
        /// crossing it while held commits the hold action.
        public const float HoldThreshold = 0.18f;

        public static Vector3 CalculateRollTarget(
            Vector3 wrestlerPosition,
            Vector3 worldDirection,
            float distance)
        {
            worldDirection = MathUtil.Flat(worldDirection);
            if (worldDirection.sqrMagnitude < 0.001f) return wrestlerPosition;
            return wrestlerPosition + worldDirection.normalized * distance;
        }

        public static Vector2 ApplyDeadZone(Vector2 input, float deadZone)
        {
            float magnitude = input.magnitude;
            if (magnitude <= deadZone) return Vector2.zero;

            float scaledMagnitude = Mathf.InverseLerp(deadZone, 1f, Mathf.Min(1f, magnitude));
            return input.normalized * scaledMagnitude;
        }

        public static bool CanProcessGameplay(bool paused, MatchState state)
        {
            return !paused && state == MatchState.Active;
        }

        public static GrapplePressAction ResolveGrapplePress(PressKind pressKind)
        {
            switch (pressKind)
            {
                case PressKind.Tap: return GrapplePressAction.Quick;
                case PressKind.HoldCommitted: return GrapplePressAction.Power;
                default: return GrapplePressAction.None;
            }
        }

        /// One direction frame: held movement is mapped through the camera
        /// into a world vector (exactly like locomotion), then classified
        /// against the attacker's facing — so pushing toward the opponent on
        /// screen is always Forward, for any camera yaw. Input inside the
        /// dead zone is Neutral.
        public static MoveDirection ResolveMoveDirection(
            Vector2 moveInput,
            Vector3 cameraForward,
            Vector3 cameraRight,
            Vector3 attackerForward,
            float deadZone)
        {
            Vector2 filtered = ApplyDeadZone(moveInput, deadZone);
            if (filtered == Vector2.zero) return MoveDirection.Neutral;

            Vector3 world = MathUtil.Flat(cameraRight).normalized * filtered.x +
                            MathUtil.Flat(cameraForward).normalized * filtered.y;
            if (world.sqrMagnitude < 0.0001f) return MoveDirection.Neutral;

            Vector3 facing = MathUtil.Flat(attackerForward).normalized;
            Vector3 facingRight = Vector3.Cross(Vector3.up, facing);
            float forward = Vector3.Dot(world.normalized, facing);
            float right = Vector3.Dot(world.normalized, facingRight);

            if (Mathf.Abs(forward) >= Mathf.Abs(right))
                return forward >= 0f ? MoveDirection.Forward : MoveDirection.Backward;
            return right >= 0f ? MoveDirection.Right : MoveDirection.Left;
        }
    }
}
