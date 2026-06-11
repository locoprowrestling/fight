using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class CombatContextResolverTests
    {
        [Test]
        public void ResolvePriority_GrappleLockWinsOverAllOtherContexts()
        {
            var result = CombatContextResolver.ResolvePriority(
                grappleLock: true,
                targetDowned: true,
                targetCornered: true,
                targetRopeStaggered: true,
                attackerRebounding: true);

            Assert.That(result, Is.EqualTo(CombatContext.GrappleLock));
        }

        [Test]
        public void ResolvePriority_DownedWinsOverCornerAndRope()
        {
            var result = CombatContextResolver.ResolvePriority(
                grappleLock: false,
                targetDowned: true,
                targetCornered: true,
                targetRopeStaggered: true,
                attackerRebounding: false);

            Assert.That(result, Is.EqualTo(CombatContext.GroundUpper));
        }

        [Test]
        public void ResolvePriority_CornerWinsOverRopeStagger()
        {
            var result = CombatContextResolver.ResolvePriority(
                grappleLock: false,
                targetDowned: false,
                targetCornered: true,
                targetRopeStaggered: true,
                attackerRebounding: true);

            Assert.That(result, Is.EqualTo(CombatContext.Corner));
        }

        [Test]
        public void ResolvePriority_RopeStaggerWinsOverRebound()
        {
            var result = CombatContextResolver.ResolvePriority(
                grappleLock: false,
                targetDowned: false,
                targetCornered: false,
                targetRopeStaggered: true,
                attackerRebounding: true);

            Assert.That(result, Is.EqualTo(CombatContext.RopeStagger));
        }

        [Test]
        public void ResolvePriority_DefaultsToStanding()
        {
            var result = CombatContextResolver.ResolvePriority(
                grappleLock: false,
                targetDowned: false,
                targetCornered: false,
                targetRopeStaggered: false,
                attackerRebounding: false);

            Assert.That(result, Is.EqualTo(CombatContext.Standing));
        }

        [Test]
        public void ResolveGroundZone_UsesDefenderFacingAxis()
        {
            Assert.That(
                CombatContextResolver.ResolveGroundZone(
                    Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 1f), 0.2f),
                Is.EqualTo(GroundTargetZone.Upper));
            Assert.That(
                CombatContextResolver.ResolveGroundZone(
                    Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -1f), 0.2f),
                Is.EqualTo(GroundTargetZone.Lower));
        }

        [Test]
        public void ResolveGroundZone_SideOnAttackerIsNeitherZone()
        {
            Assert.That(
                CombatContextResolver.ResolveGroundZone(
                    Vector3.zero, Vector3.forward, new Vector3(1f, 0f, 0f), 0.2f),
                Is.EqualTo(GroundTargetZone.None));
        }

        [Test]
        public void ResolveGroundZone_CoincidentPositionsResolveToNone()
        {
            Assert.That(
                CombatContextResolver.ResolveGroundZone(
                    Vector3.zero, Vector3.forward, Vector3.zero, 0.2f),
                Is.EqualTo(GroundTargetZone.None));
        }
    }
}
