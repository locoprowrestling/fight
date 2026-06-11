using UnityEngine;

namespace LoCoFight
{
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
            Vector2 keyboardMove = ReadKeyboardMove();
            Vector2 combinedMove = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));
            Vector2 move = PlayerInputLogic.ApplyDeadZone(combinedMove, StickDeadZone);

            bool gamepadActivity = HasGamepadButtonDown() ||
                                   (keyboardMove.sqrMagnitude < 0.01f && move.sqrMagnitude > 0.01f);
            bool keyboardActivity = HasKeyboardActivity(keyboardMove);
            if (gamepadActivity) _lastDevice = PlayerInputDevice.Controller;
            else if (keyboardActivity) _lastDevice = PlayerInputDevice.Keyboard;

            bool stickMash = ReadStickMash(move);
            bool reversal = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton5);
            bool dodge = Input.GetKeyDown(KeyCode.Semicolon) || Input.GetKeyDown(KeyCode.LeftAlt) ||
                         Input.GetKeyDown(KeyCode.JoystickButton1);

            return new PlayerInputFrame
            {
                Move = move,
                Device = _lastDevice,
                RunHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.JoystickButton4),
                LightPressed = Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.JoystickButton2),
                StrikeHeld = Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.JoystickButton2),
                StrikeReleased = Input.GetKeyUp(KeyCode.J) || Input.GetKeyUp(KeyCode.JoystickButton2),
                ControlPressed = Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.JoystickButton0),
                ControlHeld = Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.JoystickButton0),
                ControlReleased = Input.GetKeyUp(KeyCode.K) || Input.GetKeyUp(KeyCode.JoystickButton0),
                ReversalPressed = reversal,
                DodgePressed = dodge,
                SpecialPressed = Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.JoystickButton3),
                PausePressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7),
                ResetPressed = Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton7),
                DebugPressed = Input.GetKeyDown(KeyCode.F1),
                MashPressed = reversal || dodge || HasKeyboardMovementPress() ||
                              HasGamepadFaceButtonDown() || stickMash,
                HandshakeAcceptPressed = Input.GetKeyDown(KeyCode.T) ||
                                         Input.GetKeyDown(KeyCode.JoystickButton0),
                HandshakeCheapShotPressed = Input.GetKeyDown(KeyCode.J) ||
                                            Input.GetKeyDown(KeyCode.JoystickButton2),
                HandshakeRefusePressed = Input.GetKeyDown(KeyCode.K) ||
                                         Input.GetKeyDown(KeyCode.JoystickButton1)
            };
        }

        static Vector2 ReadKeyboardMove()
        {
            Vector2 move = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) move.y += 1f;
            if (Input.GetKey(KeyCode.S)) move.y -= 1f;
            if (Input.GetKey(KeyCode.A)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D)) move.x += 1f;
            return move.normalized;
        }

        bool ReadStickMash(Vector2 move)
        {
            float magnitude = move.magnitude;
            if (magnitude <= StickMashResetThreshold) _stickMashArmed = true;
            if (!_stickMashArmed || magnitude < StickMashThreshold) return false;
            _stickMashArmed = false;
            return true;
        }

        static bool HasKeyboardActivity(Vector2 keyboardMove)
        {
            return keyboardMove.sqrMagnitude > 0.01f ||
                   Input.GetKeyDown(KeyCode.J) ||
                   Input.GetKeyDown(KeyCode.K) ||
                   Input.GetKeyDown(KeyCode.L) ||
                   Input.GetKeyDown(KeyCode.Semicolon) ||
                   Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.LeftAlt) ||
                   Input.GetKeyDown(KeyCode.Escape);
        }

        static bool HasKeyboardMovementPress()
        {
            return Input.GetKeyDown(KeyCode.W) ||
                   Input.GetKeyDown(KeyCode.A) ||
                   Input.GetKeyDown(KeyCode.S) ||
                   Input.GetKeyDown(KeyCode.D);
        }

        static bool HasGamepadFaceButtonDown()
        {
            return Input.GetKeyDown(KeyCode.JoystickButton0) ||
                   Input.GetKeyDown(KeyCode.JoystickButton1) ||
                   Input.GetKeyDown(KeyCode.JoystickButton2) ||
                   Input.GetKeyDown(KeyCode.JoystickButton3);
        }

        static bool HasGamepadButtonDown()
        {
            return HasGamepadFaceButtonDown() ||
                   Input.GetKeyDown(KeyCode.JoystickButton4) ||
                   Input.GetKeyDown(KeyCode.JoystickButton5) ||
                   Input.GetKeyDown(KeyCode.JoystickButton6) ||
                   Input.GetKeyDown(KeyCode.JoystickButton7) ||
                   Input.GetKeyDown(KeyCode.JoystickButton8) ||
                   Input.GetKeyDown(KeyCode.JoystickButton9);
        }
    }
}
