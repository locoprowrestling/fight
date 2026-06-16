using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class PlayerInputLogicTests
    {
        static Type LogicType =>
            typeof(PlayerInputController).Assembly.GetType("LoCoFight.PlayerInputLogic");

        static object Invoke(string methodName, params object[] args)
        {
            Assert.That(LogicType, Is.Not.Null, "PlayerInputLogic must exist.");
            var method = LogicType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"{methodName} must exist.");
            return method.Invoke(null, args);
        }

        [Test]
        public void CalculateRollTarget_StartsFromWrestlerPosition()
        {
            var wrestlerPosition = new Vector3(7f, 0.5f, -3f);
            var result = (Vector3)Invoke(
                "CalculateRollTarget",
                wrestlerPosition,
                Vector3.right,
                1.5f);

            Assert.That(result, Is.EqualTo(new Vector3(8.5f, 0.5f, -3f)));
        }

        [Test]
        public void ApplyDeadZone_RemovesSmallStickDrift()
        {
            var result = (Vector2)Invoke("ApplyDeadZone", new Vector2(0.1f, -0.1f), 0.2f);
            Assert.That(result, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void CanProcessGameplay_IsFalseWhilePaused()
        {
            var result = (bool)Invoke("CanProcessGameplay", true, MatchState.Active);
            Assert.That(result, Is.False);
        }

        [TestCase(PressKind.None, "None")]
        [TestCase(PressKind.Tap, "Quick")]
        [TestCase(PressKind.HoldCommitted, "Power")]
        public void ResolveGrapplePress_MapsTapAndHoldToDifferentMoveFamilies(
            PressKind pressKind,
            string expected)
        {
            Assert.That(Invoke("ResolveGrapplePress", pressKind).ToString(), Is.EqualTo(expected));
        }

        static string ResolveDirection(Vector2 move, Vector3 camForward, Vector3 attackerForward)
        {
            Vector3 camRight = Vector3.Cross(Vector3.up, camForward);
            return Invoke("ResolveMoveDirection", move, camForward, camRight, attackerForward, 0.2f).ToString();
        }

        [Test]
        public void ResolveMoveDirection_TowardOpponentIsForwardForEveryCameraYaw()
        {
            // The attacker faces +z (toward the opponent). Whatever the camera
            // yaw, pushing the stick so the on-screen motion points at the
            // opponent must resolve Forward.
            foreach (Vector3 camForward in new[]
                     { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                Vector3 camRight = Vector3.Cross(Vector3.up, camForward);
                // Stick input whose camera-mapped world vector is +z:
                var stick = new Vector2(
                    Vector3.Dot(Vector3.forward, camRight),
                    Vector3.Dot(Vector3.forward, camForward));
                Assert.That(ResolveDirection(stick, camForward, Vector3.forward),
                    Is.EqualTo("Forward"), $"camera {camForward}");
            }
        }

        [Test]
        public void ResolveMoveDirection_UsesNeutralInsideDeadZone()
        {
            Assert.That(
                ResolveDirection(new Vector2(0.05f, 0.05f), Vector3.forward, Vector3.forward),
                Is.EqualTo("Neutral"));
        }

        [Test]
        public void ResolveMoveDirection_UsesLateralAxis()
        {
            Assert.That(
                ResolveDirection(new Vector2(-1f, 0f), Vector3.forward, Vector3.forward),
                Is.EqualTo("Left"));
        }

        [Test]
        public void ResolveMoveDirection_AwayFromFacingResolvesBackward()
        {
            Assert.That(
                ResolveDirection(new Vector2(0f, -1f), Vector3.forward, Vector3.forward),
                Is.EqualTo("Backward"));
        }

        [Test]
        public void ResolveMoveDirection_CameraBehindAttackerFlipsScreenInput()
        {
            // Camera looking -z while the attacker faces +z: pushing stick-up
            // moves away from the opponent on screen → Backward.
            Assert.That(
                ResolveDirection(new Vector2(0f, 1f), Vector3.back, Vector3.forward),
                Is.EqualTo("Backward"));
        }
    }
}
