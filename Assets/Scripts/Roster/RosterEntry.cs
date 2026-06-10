using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Roster Entry")]
    public class RosterEntry : ScriptableObject
    {
        public string rosterId;
        public string displayName;
        public string sourceImageFileName;
        public Sprite portraitSprite;
        public WrestlerDefinition wrestlerDefinition;
        public GameObject placeholderViewPrefab;
        [TextArea] public string notes;

        public WrestlerStatsData DefaultStats => wrestlerDefinition != null ? wrestlerDefinition.stats : null;
        public MoveDatabase DefaultMoveset => wrestlerDefinition != null ? wrestlerDefinition.moveset : null;
        public SpecialAbilityData SpecialAbility => wrestlerDefinition != null ? wrestlerDefinition.special : null;
    }
}
