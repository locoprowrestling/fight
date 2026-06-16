using UnityEngine;

namespace LoCoFight
{
    // Controller layout — 8BitDo SN30 Pro in DInput mode on macOS.
    // Shoulder/system buttons follow the SDL gamecontrollerdb mapping for this
    // pad: L=6, R=7, ZL=8, ZR=9, Select=10, Start=11.
    //
    //   JoystickButton0  → A (right face, red)    — Grapple / Tie-up
    //   JoystickButton2  → B (bottom face, yellow) — Strike
    //   JoystickButton3  → X (top face, blue)      — Special
    //   JoystickButton4  → Y (left face, green)    — Run
    //   JoystickButton7  → R bumper                — Reversal
    //   JoystickButton6  → L bumper                — Dodge
    //   JoystickButton11 → Start/+                 — Pause / Reset
    //   Left stick / D-pad                         — Move
    //
    // In the editor / dev builds, every joystick button press is logged with
    // its KeyCode so mappings can be verified against real hardware.
    //
    // Keyboard fallback:
    //   WASD          — Move
    //   J             — Strike      K — Grapple    L — Special
    //   Space         — Reversal    ;  or LAlt — Dodge
    //   LShift        — Run         Esc — Pause     R — Reset
    //   T             — Handshake accept

    /// Reads keyboard and one legacy Input Manager joystick into device-neutral commands.
    public sealed class LegacyPlayerInputSource
    {
        const float StickDeadZone = 0.2f;
        const float StickMashThreshold = 0.75f;
        const float StickMashResetThreshold = 0.35f;

        PlayerInputDevice _lastDevice = PlayerInputDevice.Keyboard;
        bool _stickMashArmed = true;

        public PlayerInputFrame ReadFrame()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogJoystickButtonPresses();
#endif
            Vector2 keyboardMove = ReadKeyboardMove();
            Vector2 stickMove    = ReadStickMove();
            Vector2 dpadMove     = ReadDPadMove();

            // Prefer analog stick > D-pad > keyboard for movement, then dead-zone filter.
            Vector2 rawMove = stickMove.sqrMagnitude > 0.01f ? stickMove
                            : dpadMove.sqrMagnitude  > 0.01f ? dpadMove
                            : keyboardMove;
            Vector2 move = PlayerInputLogic.ApplyDeadZone(rawMove, StickDeadZone);

            bool gamepadActivity = HasGamepadButtonDown()
                || stickMove.sqrMagnitude > StickDeadZone * StickDeadZone
                || dpadMove.sqrMagnitude  > 0.01f;
            bool keyboardActivity = HasKeyboardActivity(keyboardMove);

            if (gamepadActivity)      _lastDevice = PlayerInputDevice.Controller;
            else if (keyboardActivity) _lastDevice = PlayerInputDevice.Keyboard;

            bool stickMash = ReadStickMash(stickMove);
            bool reversal  = Input.GetKeyDown(KeyCode.Space)     || Input.GetKeyDown(KeyCode.JoystickButton7)
                           || Input.GetKeyDown(KeyCode.JoystickButton5);
            bool dodge     = Input.GetKeyDown(KeyCode.Semicolon) || Input.GetKeyDown(KeyCode.LeftAlt)
                           || Input.GetKeyDown(KeyCode.JoystickButton6);

            return new PlayerInputFrame
            {
                Move           = move,
                Device         = _lastDevice,
                RunHeld        = Input.GetKey(KeyCode.LeftShift)       || Input.GetKey(KeyCode.JoystickButton4),
                LightPressed   = Input.GetKeyDown(KeyCode.J)           || Input.GetKeyDown(KeyCode.JoystickButton2),
                StrikeHeld     = Input.GetKey(KeyCode.J)               || Input.GetKey(KeyCode.JoystickButton2),
                StrikeReleased = Input.GetKeyUp(KeyCode.J)             || Input.GetKeyUp(KeyCode.JoystickButton2),
                ControlPressed = Input.GetKeyDown(KeyCode.K)           || Input.GetKeyDown(KeyCode.JoystickButton0),
                ControlHeld    = Input.GetKey(KeyCode.K)               || Input.GetKey(KeyCode.JoystickButton0),
                ControlReleased= Input.GetKeyUp(KeyCode.K)             || Input.GetKeyUp(KeyCode.JoystickButton0),
                ReversalPressed= reversal,
                DodgePressed   = dodge,
                SpecialPressed = Input.GetKeyDown(KeyCode.L)           || Input.GetKeyDown(KeyCode.JoystickButton3),
                PausePressed   = Input.GetKeyDown(KeyCode.Escape)      || Input.GetKeyDown(KeyCode.JoystickButton11),
                ResetPressed   = Input.GetKeyDown(KeyCode.R)           || Input.GetKeyDown(KeyCode.JoystickButton11),
                DebugPressed   = Input.GetKeyDown(KeyCode.F1),
                MashPressed    = reversal || dodge || HasKeyboardMovementPress()
                              || HasGamepadFaceButtonDown() || stickMash,
                HandshakeAcceptPressed    = Input.GetKeyDown(KeyCode.T)          || Input.GetKeyDown(KeyCode.JoystickButton0),
                HandshakeCheapShotPressed = Input.GetKeyDown(KeyCode.J)          || Input.GetKeyDown(KeyCode.JoystickButton2),
                HandshakeRefusePressed    = Input.GetKeyDown(KeyCode.K)          || Input.GetKeyDown(KeyCode.JoystickButton1)
            };
        }

        // ── movement readers ────────────────────────────────────────────────

        static Vector2 ReadKeyboardMove()
        {
            Vector2 move = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) move.y += 1f;
            if (Input.GetKey(KeyCode.S)) move.y -= 1f;
            if (Input.GetKey(KeyCode.A)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D)) move.x += 1f;
            return move.normalized;
        }

        static Vector2 ReadStickMove()
        {
            return new Vector2(
                Input.GetAxisRaw("Joy_Horizontal"),
                Input.GetAxisRaw("Joy_Vertical"));
        }

        // D-pad: try the dedicated axis pair first (works on Xbox/Mac via
        // axis 5/6); fall back to the digital JoystickButton14-17 set that
        // some platforms expose instead.
        static Vector2 ReadDPadMove()
        {
            float dx = Input.GetAxisRaw("DPad_Horizontal");
            float dy = Input.GetAxisRaw("DPad_Vertical");
            if (Mathf.Abs(dx) > 0.5f || Mathf.Abs(dy) > 0.5f)
                return new Vector2(dx, dy).normalized;

            // Digital D-pad buttons (Xbox on Windows via XInput, and others).
            Vector2 digital = Vector2.zero;
            if (Input.GetKey(KeyCode.JoystickButton14)) digital.y += 1f; // Up
            if (Input.GetKey(KeyCode.JoystickButton15)) digital.y -= 1f; // Down
            if (Input.GetKey(KeyCode.JoystickButton16)) digital.x -= 1f; // Left
            if (Input.GetKey(KeyCode.JoystickButton17)) digital.x += 1f; // Right
            return digital.normalized;
        }

        // ── mash / device helpers ────────────────────────────────────────────

        bool ReadStickMash(Vector2 stick)
        {
            float magnitude = stick.magnitude;
            if (magnitude <= StickMashResetThreshold) _stickMashArmed = true;
            if (!_stickMashArmed || magnitude < StickMashThreshold) return false;
            _stickMashArmed = false;
            return true;
        }

        static bool HasKeyboardActivity(Vector2 keyboardMove)
        {
            return keyboardMove.sqrMagnitude > 0.01f
                || Input.GetKeyDown(KeyCode.J)
                || Input.GetKeyDown(KeyCode.K)
                || Input.GetKeyDown(KeyCode.L)
                || Input.GetKeyDown(KeyCode.Semicolon)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.LeftAlt)
                || Input.GetKeyDown(KeyCode.Escape);
        }

        static bool HasKeyboardMovementPress()
        {
            return Input.GetKeyDown(KeyCode.W)
                || Input.GetKeyDown(KeyCode.A)
                || Input.GetKeyDown(KeyCode.S)
                || Input.GetKeyDown(KeyCode.D);
        }

        static bool HasGamepadFaceButtonDown()
        {
            return Input.GetKeyDown(KeyCode.JoystickButton0)
                || Input.GetKeyDown(KeyCode.JoystickButton1)
                || Input.GetKeyDown(KeyCode.JoystickButton2)
                || Input.GetKeyDown(KeyCode.JoystickButton3);
        }

        static bool HasGamepadButtonDown()
        {
            return HasGamepadFaceButtonDown()
                || Input.GetKeyDown(KeyCode.JoystickButton4)
                || Input.GetKeyDown(KeyCode.JoystickButton5)
                || Input.GetKeyDown(KeyCode.JoystickButton6)
                || Input.GetKeyDown(KeyCode.JoystickButton7)
                || Input.GetKeyDown(KeyCode.JoystickButton8)
                || Input.GetKeyDown(KeyCode.JoystickButton9)
                || Input.GetKeyDown(KeyCode.JoystickButton10)
                || Input.GetKeyDown(KeyCode.JoystickButton11)
                || Input.GetKeyDown(KeyCode.JoystickButton14)
                || Input.GetKeyDown(KeyCode.JoystickButton15)
                || Input.GetKeyDown(KeyCode.JoystickButton16)
                || Input.GetKeyDown(KeyCode.JoystickButton17);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static void LogJoystickButtonPresses()
        {
            for (var code = KeyCode.JoystickButton0; code <= KeyCode.JoystickButton19; code++)
                if (Input.GetKeyDown(code)) Debug.Log($"[Input] {code} pressed");
        }
#endif
    }
}
