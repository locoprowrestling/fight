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
        void TriggerReversal();
        void TriggerDodge();
        void TriggerDowned();
        void TriggerGetUp();
        void TriggerRopeStagger();
        void TriggerCornered();
        void TriggerAerialLaunch();
        void TriggerAerialLanding(bool hit);
        void TriggerSpecial(string specialId);
    }
}
