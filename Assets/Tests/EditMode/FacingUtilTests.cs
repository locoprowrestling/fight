using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class FacingUtilTests
    {
        [Test]
        public void FacesOpponentToTheRight()
        {
            Assert.IsTrue(FacingUtil.FacingRight(0f, 3f));
            Assert.IsFalse(FacingUtil.FacingRight(3f, 0f));
        }

        [Test]
        public void FlipScaleMatchesFacing()
        {
            Assert.AreEqual(1f, FacingUtil.FlipScaleX(true), 0.0001f);
            Assert.AreEqual(-1f, FacingUtil.FlipScaleX(false), 0.0001f);
        }
    }
}
