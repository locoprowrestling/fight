namespace LoCoFight
{
    /// One step of a scripted special sequence (JT's Statutes in Stone, etc.).
    [System.Serializable]
    public class SequenceStep
    {
        public string StepName = "Step";
        public float Duration = 0.5f;
        public float Damage = 0f;
        public float StaminaDamage = 0f;
        public string PoseName = "";
        public string AttackerStateKey = "";
        public string DefenderStateKey = "";
        public string PresentationMarker = "";
        public bool AllowsOpponentEscape = false;
    }
}
