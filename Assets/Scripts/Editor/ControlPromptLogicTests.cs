using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class ControlPromptLogicTests
    {
        [Test]
        public void StandingPrompts_ShowStrikeAndGrappleWithHoldVariants()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.Standing, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Strike (+direction: Heavy)"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.Standing, false, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Grapple (tap Quick / hold POWER)"));
        }

        [Test]
        public void PromptRange_MatchesActualGrappleAcquisitionRange()
        {
            Assert.That(ControlPromptLogic.PromptRange, Is.EqualTo(WrestlerCombat.GrappleRange));
        }

        [Test]
        public void LockPrompt_ExplainsQuickPressAndLongPress()
        {
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.GrappleLock, false, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] +direction: tap Quick / hold POWER"));
        }

        [Test]
        public void DownedNearbyPrompts_ShowGroundAttackAndPin()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.GroundUpper, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Ground Attack (upper)"));
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.GroundLower, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Ground Attack (lower)"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.GroundUpper, true, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Pin (hold: Submission)"));
        }

        [Test]
        public void ContextualPromptsOutOfRange_TellThePlayerToMoveCloser()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.GroundUpper, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Ground Attack (upper) — move closer"));
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.Corner, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Corner Strike — move closer"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.GroundUpper, false, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Pin (hold: Submission) — move closer"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.Corner, false, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Corner Grapple — move closer"));
        }

        [Test]
        public void StandingPrompts_IgnoreRange()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.Standing, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Strike (+direction: Heavy)"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.Standing, false, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Grapple (tap Quick / hold POWER)"));
        }

        [Test]
        public void CornerAndRopePrompts_NameTheContextFamily()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.Corner, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Corner Strike"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.Corner, false, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Corner Grapple"));
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.RopeStagger, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Rope Attack"));
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.RopeRebound, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Rebound Attack"));
        }

        [Test]
        public void ControllerDevice_UsesPadGlyphs()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.Standing, true, PlayerInputDevice.Controller),
                Is.EqualTo("[X] Strike (+direction: Heavy)"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.GrappleLock, false, true, PlayerInputDevice.Controller),
                Is.EqualTo("[A] +direction: tap Quick / hold POWER"));
        }

        [Test]
        public void RejectionText_ExplainsActionableReasonsOnly()
        {
            Assert.That(ControlPromptLogic.RejectionText(MoveRejectionReason.OutOfRange),
                Is.EqualTo("Too far away"));
            Assert.That(ControlPromptLogic.RejectionText(MoveRejectionReason.InsufficientStamina),
                Is.EqualTo("Not enough stamina"));
            Assert.That(ControlPromptLogic.RejectionText(MoveRejectionReason.WrongGroundZone),
                Is.EqualTo("Wrong side of the body"));
            // Chain noise (a corner try while nobody is cornered, etc.) stays silent.
            Assert.That(ControlPromptLogic.RejectionText(MoveRejectionReason.WrongTargetState), Is.Empty);
            Assert.That(ControlPromptLogic.RejectionText(MoveRejectionReason.NotRebounding), Is.Empty);
            Assert.That(ControlPromptLogic.RejectionText(MoveRejectionReason.None), Is.Empty);
        }
    }
}
