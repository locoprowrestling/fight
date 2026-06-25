using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class LaneSystemTests
    {
        [Test]
        public void ThreeLanesFrontMidBack()
        {
            Assert.AreEqual(3, LaneSystem.LaneCount);
            Assert.AreEqual(-1.2f, LaneSystem.LaneZ[0], 0.0001f);
            Assert.AreEqual(0f, LaneSystem.LaneZ[1], 0.0001f);
            Assert.AreEqual(1.2f, LaneSystem.LaneZ[2], 0.0001f);
        }

        [Test]
        public void NearestLaneIndexPicksClosest()
        {
            Assert.AreEqual(0, LaneSystem.NearestLaneIndex(-2f));
            Assert.AreEqual(0, LaneSystem.NearestLaneIndex(-0.7f));
            Assert.AreEqual(1, LaneSystem.NearestLaneIndex(0.2f));
            Assert.AreEqual(2, LaneSystem.NearestLaneIndex(5f));
        }

        [Test]
        public void SnapZReturnsLaneValue()
        {
            Assert.AreEqual(0f, LaneSystem.SnapZ(0.3f), 0.0001f);
            Assert.AreEqual(1.2f, LaneSystem.SnapZ(0.9f), 0.0001f);
        }

        [Test]
        public void StepLaneClampsToEnds()
        {
            Assert.AreEqual(0, LaneSystem.StepLane(0, -1));
            Assert.AreEqual(1, LaneSystem.StepLane(0, 1));
            Assert.AreEqual(2, LaneSystem.StepLane(2, 1));
        }

        [Test]
        public void LanesAlignedWithinTolerance()
        {
            Assert.IsTrue(LaneSystem.LanesAligned(0f, 0.5f, 0.6f));
            Assert.IsFalse(LaneSystem.LanesAligned(0f, 1.2f, 0.6f));
        }
    }
}
