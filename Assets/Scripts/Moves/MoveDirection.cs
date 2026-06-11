namespace LoCoFight
{
    /// Facing-relative direction held when a grapple follow-up is requested.
    /// Left and right share one lateral data bucket for this milestone.
    public enum MoveDirection
    {
        Neutral,
        Forward,
        Backward,
        Left,
        Right
    }
}
