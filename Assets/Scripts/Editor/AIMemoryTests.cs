using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class AIMemoryTests
    {
        [Test]
        public void RepetitionPenalty_SuccessCountsMoreThanAttempt()
        {
            var memory = new AIMemory();
            memory.NoteAttempt("grapple", 10f);
            float afterAttempt = memory.RepetitionPenalty("grapple", 10f);
            memory.NoteSuccess("grapple", 11f);
            float afterSuccess = memory.RepetitionPenalty("grapple", 11f);

            Assert.That(afterAttempt, Is.GreaterThan(0f));
            Assert.That(afterSuccess, Is.GreaterThan(afterAttempt));
        }

        [Test]
        public void RepetitionPenalty_DecaysOverTime()
        {
            var memory = new AIMemory();
            memory.NoteSuccess("strike", 0f);

            Assert.That(
                memory.RepetitionPenalty("strike", 8f),
                Is.LessThan(memory.RepetitionPenalty("strike", 1f)));
        }

        [Test]
        public void RepetitionPenalty_UnknownFamilyIsZero()
        {
            Assert.That(
                new AIMemory().RepetitionPenalty("never-used", 5f),
                Is.EqualTo(0f));
        }

        [Test]
        public void RepetitionPenalty_RepeatedSuccessGrows()
        {
            var memory = new AIMemory();
            memory.NoteSuccess("grapple", 1f);
            float one = memory.RepetitionPenalty("grapple", 1f);
            memory.NoteSuccess("grapple", 2f);
            float two = memory.RepetitionPenalty("grapple", 2f);

            Assert.That(two, Is.GreaterThan(one));
        }

        [Test]
        public void RepetitionPenalty_IsBoundedBelowOne()
        {
            var memory = new AIMemory();
            for (int i = 0; i < 50; i++) memory.NoteSuccess("spam", i * 0.1f);

            Assert.That(memory.RepetitionPenalty("spam", 5f), Is.LessThan(1f));
        }

        [Test]
        public void CanUse_RespectsCooldownWithInjectedTime()
        {
            var memory = new AIMemory();
            Assert.That(memory.CanUse("pin", 3f, 100f), Is.True);
            memory.NoteAttempt("pin", 100f);
            Assert.That(memory.CanUse("pin", 3f, 101f), Is.False);
            Assert.That(memory.CanUse("pin", 3f, 103.5f), Is.True);
        }

        [Test]
        public void Clear_ForgetsEverything()
        {
            var memory = new AIMemory();
            memory.NoteSuccess("grapple", 10f);
            memory.Clear();

            Assert.That(memory.RepetitionPenalty("grapple", 10f), Is.EqualTo(0f));
            Assert.That(memory.CanUse("grapple", 5f, 10f), Is.True);
        }

        [Test]
        public void DebugSummary_ListsPenalizedFamilies()
        {
            var memory = new AIMemory();
            memory.NoteSuccess("grapple", 10f);

            Assert.That(memory.DebugSummary(10f), Does.Contain("grapple"));
        }
    }
}
