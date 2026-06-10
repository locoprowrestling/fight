using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Wrestler Definition")]
    public class WrestlerDefinition : ScriptableObject
    {
        public string wrestlerId;
        public string displayName;
        public WrestlerStatsData stats;
        public MoveDatabase moveset;
        public SpecialAbilityData special;
        [Tooltip("Optional enhanced dodge ability (The Vigilante's Vanishing Dodge).")]
        public SpecialAbilityData dodgeAbility;
        public List<PassiveTraitData> passiveTraits = new List<PassiveTraitData>();
        public Color placeholderColor = Color.gray;
        [TextArea] public string notes;
    }
}
