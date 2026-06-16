using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class PressTrackerTests
    {
        const float Threshold = 0.22f;

        [Test]
        public void ShortPress_ResolvesTapOnReleaseOnly()
        {
            var tracker = new PressTracker();
            Assert.That(tracker.Update(pressed: true, held: true, released: false, 0.016f, Threshold),
                Is.EqualTo(PressKind.None));
            Assert.That(tracker.Update(false, true, false, 0.05f, Threshold),
                Is.EqualTo(PressKind.None));
            Assert.That(tracker.Update(false, false, true, 0.016f, Threshold),
                Is.EqualTo(PressKind.Tap));
        }

        [Test]
        public void LongPress_CommitsHoldExactlyOnceAtThreshold()
        {
            var tracker = new PressTracker();
            tracker.Update(true, true, false, 0.016f, Threshold);
            Assert.That(tracker.Update(false, true, false, 0.25f, Threshold),
                Is.EqualTo(PressKind.HoldCommitted));
            Assert.That(tracker.Update(false, true, false, 0.25f, Threshold),
                Is.EqualTo(PressKind.None), "hold must not re-fire while still held");
        }

        [Test]
        public void ReleaseAfterCommittedHold_FiresNothing()
        {
            var tracker = new PressTracker();
            tracker.Update(true, true, false, 0.016f, Threshold);
            tracker.Update(false, true, false, 0.30f, Threshold);
            Assert.That(tracker.Update(false, false, true, 0.016f, Threshold),
                Is.EqualTo(PressKind.None));
        }

        [Test]
        public void OnePress_NeverFiresBothTapAndHold()
        {
            var tracker = new PressTracker();
            int fired = 0;
            tracker.Update(true, true, false, 0.016f, Threshold);
            for (int i = 0; i < 30; i++)
                if (tracker.Update(false, true, false, 0.05f, Threshold) != PressKind.None) fired++;
            if (tracker.Update(false, false, true, 0.016f, Threshold) != PressKind.None) fired++;
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void Reset_ClearsInFlightPress()
        {
            var tracker = new PressTracker();
            tracker.Update(true, true, false, 0.016f, Threshold);
            tracker.Reset();
            Assert.That(tracker.Update(false, true, false, 0.5f, Threshold),
                Is.EqualTo(PressKind.None), "stale press must not commit after Reset");
            Assert.That(tracker.Update(false, false, true, 0.016f, Threshold),
                Is.EqualTo(PressKind.None), "stale release must not tap after Reset");
        }

        [Test]
        public void ReleaseWithoutPress_FiresNothing()
        {
            var tracker = new PressTracker();
            Assert.That(tracker.Update(false, false, true, 0.016f, Threshold),
                Is.EqualTo(PressKind.None));
        }

        [Test]
        public void GrappleIntent_TapRetainsDirectionFromPressThroughRelease()
        {
            var intent = new GrappleInputIntent();

            intent.Begin(MoveDirection.Forward, held: true, 0.016f, Threshold);
            intent.Advance(MoveDirection.Neutral, held: false, released: true, 0.016f, Threshold);

            Assert.That(intent.PendingAction, Is.EqualTo(GrapplePressAction.Quick));
            Assert.That(intent.Direction, Is.EqualTo(MoveDirection.Forward));
        }

        [Test]
        public void GrappleIntent_HoldCommitsPowerExactlyOnce()
        {
            var intent = new GrappleInputIntent();

            intent.Begin(MoveDirection.Left, held: true, 0.016f, Threshold);
            intent.Advance(MoveDirection.Left, held: true, released: false, 0.25f, Threshold);

            Assert.That(intent.ConsumeAction(), Is.EqualTo(GrapplePressAction.Power));
            Assert.That(intent.ConsumeAction(), Is.EqualTo(GrapplePressAction.None));
        }

        [Test]
        public void GrappleIntent_ResolvedActionWaitsForLockConsumption()
        {
            var intent = new GrappleInputIntent();

            intent.Begin(MoveDirection.Backward, held: true, 0.016f, Threshold);
            intent.Advance(MoveDirection.Backward, held: false, released: true, 0.016f, Threshold);

            Assert.That(intent.Active, Is.True);
            Assert.That(intent.PendingAction, Is.EqualTo(GrapplePressAction.Quick));
            Assert.That(intent.ConsumeAction(), Is.EqualTo(GrapplePressAction.Quick));
            Assert.That(intent.Active, Is.False);
        }

        [Test]
        public void GrappleIntent_ResolvedActionKeepsItsChosenDirection()
        {
            var intent = new GrappleInputIntent();

            intent.Begin(MoveDirection.Left, held: true, 0.016f, Threshold);
            intent.Advance(MoveDirection.Left, held: false, released: true, 0.016f, Threshold);
            intent.Advance(MoveDirection.Right, held: false, released: false, 0.016f, Threshold);

            Assert.That(intent.Direction, Is.EqualTo(MoveDirection.Left));
        }
    }

    public class WrestlerMotorInputTests
    {
        [Test]
        public void ResolveStateMotion_ClearsApproachVelocityWhenMovementIsDisabled()
        {
            Vector3 approachVelocity = new Vector3(0f, 0f, 3f);

            Assert.That(
                WrestlerMotor.ResolveStateMotion(approachVelocity, canMove: false),
                Is.EqualTo(Vector3.zero));
        }
    }
}
