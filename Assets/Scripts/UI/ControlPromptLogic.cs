namespace LoCoFight
{
    /// Pure mapping from the resolved combat context to the HUD prompt labels
    /// for the two tap/hold core buttons. Presentation only: a wrong label is
    /// a bug here, never in combat — nothing reads these strings back.
    public static class ControlPromptLogic
    {
        static string StrikeGlyph(PlayerInputDevice device) =>
            device == PlayerInputDevice.Controller ? "X" : "J";

        static string ControlGlyph(PlayerInputDevice device) =>
            device == PlayerInputDevice.Controller ? "A" : "K";

        public static string StrikePrompt(CombatContext context, PlayerInputDevice device)
        {
            string label;
            switch (context)
            {
                case CombatContext.GrappleLock: label = "-"; break;
                case CombatContext.GroundUpper: label = "Ground Attack (upper)"; break;
                case CombatContext.GroundLower: label = "Ground Attack (lower)"; break;
                case CombatContext.Corner: label = "Corner Strike"; break;
                case CombatContext.RopeStagger: label = "Rope Attack"; break;
                case CombatContext.RopeRebound: label = "Rebound Attack"; break;
                default: label = "Strike (hold: Heavy)"; break;
            }
            return $"[{StrikeGlyph(device)}] {label}";
        }

        public static string ControlPrompt(
            CombatContext context,
            bool opponentDownedInRange,
            PlayerInputDevice device)
        {
            string label;
            if (context == CombatContext.GrappleLock)
                label = "Quick Grapple (hold: Power)";
            else if (opponentDownedInRange)
                label = "Pin (hold: Submission)";
            else if (context == CombatContext.Corner)
                label = "Corner Grapple";
            else
                label = "Grapple";
            return $"[{ControlGlyph(device)}] {label}";
        }
    }
}
