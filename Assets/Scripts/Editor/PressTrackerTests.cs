using NUnit.Framework;

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
    }
}
