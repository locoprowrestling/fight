namespace LoCoFight
{
    /// Pure mapping from the resolved combat context to the HUD prompt labels
    /// for the two tap/hold core buttons, plus short feedback text for
    /// rejected actions. Presentation only: a wrong label is a bug here,
    /// never in combat — nothing reads these strings back.
    public static class ControlPromptLogic
    {
        /// Advisory "close enough" distance for prompts. Family ranges vary
        /// 1.25–1.35 in move data; validation remains the authority.
        public const float PromptRange = WrestlerCombat.GrappleRange;

        const string MoveCloser = " — move closer";

        static string StrikeGlyph(PlayerInputDevice device) =>
            device == PlayerInputDevice.Controller ? "X" : "J";

        static string ControlGlyph(PlayerInputDevice device) =>
            device == PlayerInputDevice.Controller ? "A" : "K";

        public static string StrikePrompt(CombatContext context, bool inRange, PlayerInputDevice device)
        {
            string label;
            bool rangeMatters = true;
            switch (context)
            {
                case CombatContext.GrappleLock: label = "-"; rangeMatters = false; break;
                case CombatContext.GroundUpper: label = "Ground Attack (upper)"; break;
                case CombatContext.GroundLower: label = "Ground Attack (lower)"; break;
                case CombatContext.Corner: label = "Corner Strike"; break;
                case CombatContext.RopeStagger: label = "Rope Attack"; break;
                case CombatContext.RopeRebound: label = "Rebound Attack"; break;
                default: label = "Strike (+direction: Heavy)"; rangeMatters = false; break;
            }
            string suffix = rangeMatters && !inRange ? MoveCloser : "";
            return $"[{StrikeGlyph(device)}] {label}{suffix}";
        }

        public static string ControlPrompt(
            CombatContext context,
            bool opponentDownedInRange,
            bool inRange,
            PlayerInputDevice device)
        {
            string label;
            bool rangeMatters = true;
            if (context == CombatContext.GrappleLock)
            {
                label = "+direction: tap Quick / hold POWER";
                rangeMatters = false;
            }
            else if (context == CombatContext.GroundUpper || context == CombatContext.GroundLower ||
                     opponentDownedInRange)
            {
                label = "Pin (hold: Submission)";
                rangeMatters = !opponentDownedInRange;
            }
            else if (context == CombatContext.Corner)
            {
                label = "Corner Grapple";
            }
            else
            {
                label = "Grapple (tap Quick / hold POWER)";
                rangeMatters = false;
            }
            string suffix = rangeMatters && !inRange ? MoveCloser : "";
            return $"[{ControlGlyph(device)}] {label}{suffix}";
        }

        /// Short feedback for a rejected player action. Returns "" for
        /// reasons that are chain noise (e.g. the corner try inside the
        /// standing precedence chain rejecting because nobody is cornered).
        public static string RejectionText(MoveRejectionReason reason)
        {
            switch (reason)
            {
                case MoveRejectionReason.OutOfRange: return "Too far away";
                case MoveRejectionReason.InsufficientStamina: return "Not enough stamina";
                case MoveRejectionReason.WrongGroundZone: return "Wrong side of the body";
                case MoveRejectionReason.NotInCorner: return "Not in the corner";
                case MoveRejectionReason.NotNearRopes: return "Not at the ropes";
                case MoveRejectionReason.InsufficientLiftStrength: return "Not strong enough";
                case MoveRejectionReason.TargetTooHeavy: return "Too heavy to lift";
                default: return "";
            }
        }
    }
}
