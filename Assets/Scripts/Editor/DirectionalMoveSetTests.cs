using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class DirectionalMoveSetTests
    {
        MoveData CreateMove(string id)
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = id;
            return move;
        }

        [Test]
        public void Pick_UsesRequestedBucket()
        {
            var set = new DirectionalMoveSet();
            var forward = CreateMove("forward");
            set.forward.Add(forward);

            Assert.That(set.Pick(MoveDirection.Forward, out bool fallback), Is.SameAs(forward));
            Assert.That(fallback, Is.False);
        }

        [Test]
        public void Pick_LeftAndRightShareTheLateralBucket()
        {
            var set = new DirectionalMoveSet();
            var lateral = CreateMove("lateral");
            set.lateral.Add(lateral);

            Assert.That(set.Pick(MoveDirection.Left, out _), Is.SameAs(lateral));
            Assert.That(set.Pick(MoveDirection.Right, out _), Is.SameAs(lateral));
        }

        [Test]
        public void Pick_FallsBackToNeutralWhenRequestedBucketIsEmpty()
        {
            var set = new DirectionalMoveSet();
            var neutral = CreateMove("neutral");
            set.neutral.Add(neutral);

            Assert.That(set.Pick(MoveDirection.Left, out bool fallback), Is.SameAs(neutral));
            Assert.That(fallback, Is.True);
        }

        [Test]
        public void Pick_NeutralRequestIsNotReportedAsFallback()
        {
            var set = new DirectionalMoveSet();
            set.neutral.Add(CreateMove("neutral"));

            set.Pick(MoveDirection.Neutral, out bool fallback);
            Assert.That(fallback, Is.False);
        }

        [Test]
        public void Pick_ReturnsNullWhenRequestedAndNeutralBucketsAreEmpty()
        {
            var set = new DirectionalMoveSet();
            Assert.That(set.Pick(MoveDirection.Backward, out _), Is.Null);
        }
    }
}
