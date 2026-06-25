using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class CameraFramingTests
    {
        [Test]
        public void MidpointIsAverage()
        {
            Assert.AreEqual(0f, CameraFraming.MidpointX(-2f, 2f), 0.0001f);
            Assert.AreEqual(1f, CameraFraming.MidpointX(0f, 2f), 0.0001f);
        }

        [Test]
        public void SizeGrowsWithSeparationAndClamps()
        {
            float near = CameraFraming.OrthographicSizeFor(1f, 3f, 0.5f, 3f, 7f);
            float far = CameraFraming.OrthographicSizeFor(6f, 3f, 0.5f, 3f, 7f);
            Assert.Less(near, far);
            Assert.AreEqual(3f, CameraFraming.OrthographicSizeFor(0f, 3f, 0.5f, 3f, 7f), 0.0001f);
            Assert.AreEqual(7f, CameraFraming.OrthographicSizeFor(100f, 3f, 0.5f, 3f, 7f), 0.0001f);
        }
    }
}
