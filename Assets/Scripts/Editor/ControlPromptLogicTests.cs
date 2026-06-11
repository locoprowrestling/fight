using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class ControlPromptLogicTests
    {
        [Test]
        public void StandingPrompts_ShowStrikeAndGrappleWithHoldVariants()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.Standing, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Strike (hold: Heavy)"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.Standing, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Grapple"));
        }

        [Test]
        public void LockPrompts_ShowQuickAndPower()
        {
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.GrappleLock, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Quick Grapple (hold: Power)"));
        }

        [Test]
        public void DownedNearbyPrompts_ShowGroundAttackAndPin()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.GroundUpper, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Ground Attack (upper)"));
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.GroundLower, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Ground Attack (lower)"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.GroundUpper, true, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Pin (hold: Submission)"));
        }

        [Test]
        public void DownedOutOfRange_ControlFallsBackToGrapple()
        {
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.GroundUpper, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Grapple"));
        }

        [Test]
        public void CornerAndRopePrompts_NameTheContextFamily()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.Corner, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Corner Strike"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.Corner, false, PlayerInputDevice.Keyboard),
                Is.EqualTo("[K] Corner Grapple"));
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.RopeStagger, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Rope Attack"));
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.RopeRebound, PlayerInputDevice.Keyboard),
                Is.EqualTo("[J] Rebound Attack"));
        }

        [Test]
        public void ControllerDevice_UsesPadGlyphs()
        {
            Assert.That(
                ControlPromptLogic.StrikePrompt(CombatContext.Standing, PlayerInputDevice.Controller),
                Is.EqualTo("[X] Strike (hold: Heavy)"));
            Assert.That(
                ControlPromptLogic.ControlPrompt(CombatContext.GrappleLock, false, PlayerInputDevice.Controller),
                Is.EqualTo("[A] Quick Grapple (hold: Power)"));
        }
    }
}
