namespace LoCoFight
{
    public enum PressKind { None, Tap, HoldCommitted }

    /// Tap/hold resolution for one button. Tap fires on release before the
    /// threshold; HoldCommitted fires the moment the threshold is crossed
    /// while still held; one physical press resolves at most one of them.
    /// No UnityEngine dependency so edit-mode tests cover it directly.
    public class PressTracker
    {
        bool _isDown;
        bool _committed;
        float _heldDuration;

        public bool IsDown => _isDown;
        public float HeldDuration => _heldDuration;
        public bool Committed => _committed;

        public PressKind Update(bool pressed, bool held, bool released, float deltaTime, float holdThreshold)
        {
            if (pressed)
            {
                _isDown = true;
                _committed = false;
                _heldDuration = 0f;
            }

            if (_isDown && held)
            {
                _heldDuration += deltaTime;
                if (!_committed && _heldDuration >= holdThreshold)
                {
                    _committed = true;
                    return PressKind.HoldCommitted;
                }
            }

            if (released && _isDown)
            {
                bool tap = !_committed && _heldDuration < holdThreshold;
                _isDown = false;
                return tap ? PressKind.Tap : PressKind.None;
            }

            return PressKind.None;
        }

        /// Pause, match end, and reset call this so nothing fires on resume.
        public void Reset()
        {
            _isDown = false;
            _committed = false;
            _heldDuration = 0f;
        }
    }
}
