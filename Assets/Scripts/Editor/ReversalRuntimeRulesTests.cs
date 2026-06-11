using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class ReversalRuntimeRulesTests
    {
        [Test]
        public void ResolveOutcome_UsesLegacyMomentumWhenBasicValueIsUnset()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.basicReversalMomentum = 0f;
            move.momentumGainOnReversal = 7f;

            var result = ReversalSystem.ResolveOutcome(move, ReversalOutcome.Basic);

            Assert.That(result.Momentum, Is.EqualTo(7f));
        }

        [Test]
        public void ResolveOutcome_BasicUsesBasicAuthoredValues()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.basicReversalMomentum = 9f;
            move.basicReversalStagger = 0.9f;
            move.basicReversalSeparation = 0.75f;

            var result = ReversalSystem.ResolveOutcome(move, ReversalOutcome.Basic);

            Assert.That(result.Momentum, Is.EqualTo(9f));
            Assert.That(result.Stagger, Is.EqualTo(0.9f));
            Assert.That(result.Separation, Is.EqualTo(0.75f));
            Assert.That(result.PresentationId, Is.EqualTo("reversal-basic"));
        }

        [Test]
        public void ResolveOutcome_StrongUsesStrongAuthoredValues()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.strongReversalMomentum = 15f;
            move.strongReversalStagger = 1.3f;
            move.strongReversalSeparation = 1.4f;

            var result = ReversalSystem.ResolveOutcome(move, ReversalOutcome.Strong);

            Assert.That(result.Momentum, Is.EqualTo(15f));
            Assert.That(result.Stagger, Is.EqualTo(1.3f));
            Assert.That(result.Separation, Is.EqualTo(1.4f));
            Assert.That(result.PresentationId, Is.EqualTo("reversal-strong"));
        }

        [Test]
        public void ResolveOutcome_NullMoveReturnsSafeDefaults()
        {
            var basic = ReversalSystem.ResolveOutcome(null, ReversalOutcome.Basic);
            var strong = ReversalSystem.ResolveOutcome(null, ReversalOutcome.Strong);

            Assert.That(basic.Momentum, Is.GreaterThan(0f));
            Assert.That(basic.Stagger, Is.GreaterThan(0f));
            Assert.That(basic.PresentationId, Is.Not.Empty);
            Assert.That(strong.Momentum, Is.GreaterThan(basic.Momentum));
            Assert.That(strong.Stagger, Is.GreaterThan(basic.Stagger));
        }

        [Test]
        public void ResolveOutcome_EmptyPresentationIdFallsBackToBuiltIn()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.basicReversalPresentationId = "";
            move.strongReversalPresentationId = null;

            Assert.That(
                ReversalSystem.ResolveOutcome(move, ReversalOutcome.Basic).PresentationId,
                Is.EqualTo("reversal-basic"));
            Assert.That(
                ReversalSystem.ResolveOutcome(move, ReversalOutcome.Strong).PresentationId,
                Is.EqualTo("reversal-strong"));
        }

        [Test]
        public void ResolveOutcome_DoesNotMutateTheMove()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.basicReversalMomentum = 0f;
            move.momentumGainOnReversal = 7f;

            ReversalSystem.ResolveOutcome(move, ReversalOutcome.Basic);

            Assert.That(move.basicReversalMomentum, Is.EqualTo(0f));
            Assert.That(move.momentumGainOnReversal, Is.EqualTo(7f));
        }
    }
}
