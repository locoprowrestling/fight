namespace LoCoFight
{
    /// Pacing class used for stamina gating and AI selection. Authored
    /// MoveData fields stay authoritative for concrete timing and costs.
    public enum MoveTier
    {
        Light,
        Medium,
        Heavy,
        Special
    }
}
