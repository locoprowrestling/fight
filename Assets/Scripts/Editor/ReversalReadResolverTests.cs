using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class ReversalReadResolverTests
    {
        [Test]
        public void Resolve_CorrectDirectionalReadIsStrong()
        {
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Toward,
                    MoveDirection.Forward,
                    hasDirectionalInput: true,
                    allowsStrongCounter: true),
                Is.EqualTo(ReversalOutcome.Strong));
        }

        [TestCase(MoveDirection.Neutral, false)]
        [TestCase(MoveDirection.Left, true)]
        public void Resolve_NeutralOrIncorrectReadIsBasic(
            MoveDirection submitted,
            bool hasDirection)
        {
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Toward,
                    submitted,
                    hasDirection,
                    allowsStrongCounter: true),
                Is.EqualTo(ReversalOutcome.Basic));
        }

        [Test]
        public void Resolve_DisabledStrongCounterIsBasic()
        {
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Left,
                    MoveDirection.Left,
                    hasDirectionalInput: true,
                    allowsStrongCounter: false),
                Is.EqualTo(ReversalOutcome.Basic));
        }

        [Test]
        public void Resolve_AwayReadRequiresBackward()
        {
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Away,
                    MoveDirection.Backward,
                    hasDirectionalInput: true,
                    allowsStrongCounter: true),
                Is.EqualTo(ReversalOutcome.Strong));
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Away,
                    MoveDirection.Forward,
                    hasDirectionalInput: true,
                    allowsStrongCounter: true),
                Is.EqualTo(ReversalOutcome.Basic));
        }

        [Test]
        public void Resolve_NeutralPreferredDirectionNeverUpgrades()
        {
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Neutral,
                    MoveDirection.Neutral,
                    hasDirectionalInput: true,
                    allowsStrongCounter: true),
                Is.EqualTo(ReversalOutcome.Basic));
        }
    }
}
