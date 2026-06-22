using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class DepthProjectionTests
    {
        [Test]
        public void BackLaneDrawsHigher()
        {
            float front = DepthProjection.ScreenYOffset(-1.2f, 0.35f);
            float back = DepthProjection.ScreenYOffset(1.2f, 0.35f);
            Assert.Less(front, back);
            Assert.AreEqual(0f, DepthProjection.ScreenYOffset(0f, 0.35f), 0.0001f);
        }

        [Test]
        public void BackLaneDrawsSmaller()
        {
            float front = DepthProjection.DepthScale(-1.2f, 0.06f, 0.8f, 1.15f);
            float mid = DepthProjection.DepthScale(0f, 0.06f, 0.8f, 1.15f);
            float back = DepthProjection.DepthScale(1.2f, 0.06f, 0.8f, 1.15f);
            Assert.Greater(front, mid);
            Assert.Greater(mid, back);
            Assert.AreEqual(1f, mid, 0.0001f);
        }

        [Test]
        public void DepthScaleClamps()
        {
            Assert.AreEqual(0.8f, DepthProjection.DepthScale(100f, 0.06f, 0.8f, 1.15f), 0.0001f);
            Assert.AreEqual(1.15f, DepthProjection.DepthScale(-100f, 0.06f, 0.8f, 1.15f), 0.0001f);
        }

        [Test]
        public void FrontLaneSortsInFront()
        {
            int front = DepthProjection.SortingOrder(-1.2f, 100);
            int back = DepthProjection.SortingOrder(1.2f, 100);
            Assert.Greater(front, back);
        }
    }
}
