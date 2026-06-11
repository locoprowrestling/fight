namespace LoCoFight
{
    /// Gameplay-facing animation interface. The placeholder implementation poses
    /// primitives; a future implementation maps these calls onto real Animator states.
    public interface IAnimationDriver
    {
        void PlayMove(string animationStateName, string placeholderPoseName, float speed = 1f);
        void PlayState(string stateName);
        void SetMovementSpeed(float speed);
        void TriggerHitReact();
        /// Resolved reversal outcome; presentationId is the authored hook
        /// from MoveData (audio/VFX selection for a future driver).
        void TriggerReversal(bool strong, string presentationId);
        void TriggerDodge();
        void TriggerDowned();
        void TriggerGetUp();
        void TriggerRopeStagger();
        void TriggerCornered();
        void TriggerAerialLaunch();
        void TriggerAerialLanding(bool hit);
        void TriggerSpecial(string specialId);
        /// Persistent full-momentum accent; on/off follows the readiness
        /// transitions, never per frame.
        void SetSpecialReady(bool ready);
        void TriggerSubmissionApply(bool attacker);
        void TriggerSubmissionStruggle();
        void TriggerSubmissionRelease(bool ropeBreak, bool escaped);
        void TriggerSubmissionTapOut();
    }
}
