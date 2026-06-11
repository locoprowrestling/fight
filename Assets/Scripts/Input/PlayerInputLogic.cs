using UnityEngine;

namespace LoCoFight
{
    public static class PlayerInputLogic
    {
        /// Single tunable feel constant: presses shorter than this are taps,
        /// crossing it while held commits the hold action.
        public const float HoldThreshold = 0.22f;

        public static PlayerAction ResolveLockAction(bool heavyPressed, bool grapplePressed)
        {
            if (heavyPressed) return PlayerAction.Heavy;
            if (grapplePressed) return PlayerAction.Grapple;
            return PlayerAction.None;
        }

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

        /// Convert held movement into a facing-relative grapple direction.
        /// Input inside the dead zone is Neutral; otherwise the dominant axis
        /// (forward vs lateral, relative to the attacker's facing) wins.
        public static MoveDirection ResolveMoveDirection(
            Vector2 moveInput,
            Vector3 facingForward,
            Vector3 facingRight,
            float deadZone)
        {
            Vector2 filtered = ApplyDeadZone(moveInput, deadZone);
            if (filtered == Vector2.zero) return MoveDirection.Neutral;

            Vector3 world = MathUtil.Flat(facingRight) * filtered.x +
                            MathUtil.Flat(facingForward) * filtered.y;
            if (world.sqrMagnitude < 0.0001f) return MoveDirection.Neutral;

            float forward = Vector3.Dot(world.normalized, MathUtil.Flat(facingForward).normalized);
            float right = Vector3.Dot(world.normalized, MathUtil.Flat(facingRight).normalized);

            if (Mathf.Abs(forward) >= Mathf.Abs(right))
                return forward >= 0f ? MoveDirection.Forward : MoveDirection.Backward;
            return right >= 0f ? MoveDirection.Right : MoveDirection.Left;
        }
    }
}
