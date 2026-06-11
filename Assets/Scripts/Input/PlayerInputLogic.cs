using UnityEngine;

namespace LoCoFight
{
    public static class PlayerInputLogic
    {
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
    }
}
