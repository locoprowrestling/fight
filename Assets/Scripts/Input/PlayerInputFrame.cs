using UnityEngine;

namespace LoCoFight
{
    public struct PlayerInputFrame
    {
        public Vector2 Move;
        public PlayerInputDevice Device;

        public bool RunHeld;

        // Press phases for the two tap/hold core buttons: Strike (J /
        // joystick 2) and Control (K / joystick 0). LightPressed doubles as
        // the Strike pressed edge.
        public bool LightPressed;
        public bool StrikeHeld;
        public bool StrikeReleased;
        public bool ControlPressed;
        public bool ControlHeld;
        public bool ControlReleased;
        public bool ReversalPressed;
        public bool DodgePressed;
        public bool SpecialPressed;

        public bool PausePressed;
        public bool ResetPressed;
        public bool DebugPressed;
        public bool MashPressed;

        public bool HandshakeAcceptPressed;
        public bool HandshakeCheapShotPressed;
        public bool HandshakeRefusePressed;
    }
}
