using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Match Rules")]
    public class MatchRulesData : ScriptableObject
    {
        public string displayName = "Standard Match";

        [Header("Win conditions")]
        public bool pinfallsEnabled = true;
        public bool submissionsEnabled = true;

        [Header("Rope rules")]
        public bool ropeBreaksEnabled = true;
        public bool refereeFiveCountEnabled = true;
        public bool noRopeBreaks = false;
        public bool ropeTrapSubmissionAllowed = false;
        public bool autoReleaseIllegalRopeHoldsAtFive = true;
        public float ropeBreakRange = 0.65f;

        [Header("Match style")]
        public bool disqualificationEnabled = false;
        public bool countOutsEnabled = false;
        public bool hardcoreRules = false;
        public bool allowOutOfRing = true;
        public bool allowWeapons = false;
        public bool allowDirtyMoves = true;
        public bool allowRefDistraction = true;

        [Header("Counts")]
        public float standardPinCountSeconds = 3.0f;
        public float refereeFiveCountSeconds = 5.0f;

        public bool RopeBreaksActive => ropeBreaksEnabled && !noRopeBreaks && !hardcoreRules;
    }
}
