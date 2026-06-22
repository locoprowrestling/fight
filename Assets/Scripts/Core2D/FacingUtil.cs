namespace LoCoFight
{
    /// Side-on facing helpers. The rig is authored facing screen-right and flips via X scale.
    public static class FacingUtil
    {
        public static bool FacingRight(float selfX, float opponentX) => opponentX >= selfX;

        public static float FlipScaleX(bool facingRight) => facingRight ? 1f : -1f;
    }
}
