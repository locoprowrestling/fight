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
        public void ResolveLockAction_HeavyTakesPriorityWhenBothButtonsArePressed()
        {
            var result = Invoke("ResolveLockAction", true, true);
            Assert.That(result.ToString(), Is.EqualTo("Heavy"));
        }

        [Test]
        public void ResolveLockAction_GrappleIsUsedWhenHeavyIsNotPressed()
        {
            var result = Invoke("ResolveLockAction", false, true);
            Assert.That(result.ToString(), Is.EqualTo("Grapple"));
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
    }
}
