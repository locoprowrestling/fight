using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class HarnessSmokeTest
    {
        [Test]
        public void TestRunnerExecutes()
        {
            Assert.AreEqual(4, 2 + 2);
        }
    }
}
