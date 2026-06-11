using UnityEngine;

namespace LoCoFight
{
    public struct PlayerInputFrame
    {
        public Vector2 Move;
        public PlayerInputDevice Device;

        public bool RunHeld;
        public bool LightPressed;
        public bool HeavyPressed;
        public bool GrapplePressed;
        public bool ReversalPressed;
        public bool DodgePressed;
        public bool SpecialPressed;
        public bool PinPressed;
        public bool SubmissionPressed;

        public bool PausePressed;
        public bool ResetPressed;
        public bool DebugPressed;
        public bool MashPressed;

        public bool HandshakeAcceptPressed;
        public bool HandshakeCheapShotPressed;
        public bool HandshakeRefusePressed;
    }
}
