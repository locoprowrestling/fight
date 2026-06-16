namespace LoCoFight
{
    /// One hit of a multi-hit combo special (Franky's 6-7 Moves of Doom).
    [System.Serializable]
    public class ComboStep
    {
        public string StepName = "Hit";
        public float Duration = 0.4f;
        public float Damage = 4f;
        public float StaminaDamage = 2f;
        public string PoseName = "strike";
        public string AttackerStateKey = "";
        public string DefenderStateKey = "";
        public string PresentationMarker = "";
        public bool ReversalWindow = false;
        public bool CausesDowned = false;
    }
}
