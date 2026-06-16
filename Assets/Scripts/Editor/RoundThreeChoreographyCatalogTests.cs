using System.Linq;
using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class RoundThreeChoreographyCatalogTests
    {
        [Test]
        public void DefaultData_CreatesQuickAndWaveOneChoreographies()
        {
            DefaultGameDataSet set = DefaultGameData.CreateAll();

            Assert.That(set.choreographies.Count, Is.EqualTo(15));
            Assert.That(set.choreographies.Select(c => c.presentationId).Distinct().Count(),
                Is.EqualTo(15));
        }

        [Test]
        public void DefaultData_AllChoreographiesValidate()
        {
            DefaultGameDataSet set = DefaultGameData.CreateAll();

            foreach (MoveChoreographyData choreography in set.choreographies)
                Assert.That(
                    MoveChoreographyValidator.Validate(choreography),
                    Is.Empty,
                    choreography.presentationId);
        }

        [Test]
        public void DefaultData_BindsPlayableMovesAndSpecials()
        {
            DefaultGameDataSet set = DefaultGameData.CreateAll();

            Assert.That(set.moves.Count(m => m.choreography != null),
                Is.GreaterThanOrEqualTo(8));
            Assert.That(set.specials.Count(s => s.choreography != null),
                Is.GreaterThanOrEqualTo(8));
        }

        [Test]
        public void DefaultData_EveryDirectionalQuickGrappleHasApprovedPairedChoreography()
        {
            DefaultGameDataSet set = DefaultGameData.CreateAll();

            foreach (MoveData move in set.moveDatabase.directionalQuickGrapples.AllMoves())
            {
                Assert.That(move.choreography, Is.Not.Null, move.displayName);
                Assert.That(move.choreography.referenceStatus,
                    Is.EqualTo(ReferenceStatus.Approved), move.displayName);
                Assert.That(move.choreography.participantMode,
                    Is.EqualTo(AnimationParticipantMode.Paired), move.displayName);
                Assert.That(move.choreography.attackerStateKey, Is.Not.Empty, move.displayName);
                Assert.That(move.choreography.defenderStateKey, Is.Not.Empty, move.displayName);
            }
        }

        [Test]
        public void DefaultData_ComboStepsHavePairedPresentationKeys()
        {
            DefaultGameDataSet set = DefaultGameData.CreateAll();
            SpecialAbilityData combo =
                set.specials.Single(s => s.specialId == "6-7-moves-of-doom");

            Assert.That(combo.comboSteps, Is.Not.Empty);
            Assert.That(combo.comboSteps.All(step =>
                !string.IsNullOrWhiteSpace(step.AttackerStateKey) &&
                !string.IsNullOrWhiteSpace(step.DefenderStateKey)), Is.True);
        }
    }
}
