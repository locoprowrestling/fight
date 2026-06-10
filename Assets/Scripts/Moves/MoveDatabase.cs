using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Move Database")]
    public class MoveDatabase : ScriptableObject
    {
        public List<MoveData> lightStrikes = new List<MoveData>();
        public List<MoveData> heavyStrikes = new List<MoveData>();
        public List<MoveData> quickGrapples = new List<MoveData>();
        public List<MoveData> powerGrapples = new List<MoveData>();
        public List<MoveData> runningAttacks = new List<MoveData>();
        public List<MoveData> groundSubmissions = new List<MoveData>();

        public MoveData RandomLightStrike() => Pick(lightStrikes);
        public MoveData RandomHeavyStrike() => Pick(heavyStrikes);
        public MoveData RandomQuickGrapple() => Pick(quickGrapples);
        public MoveData RandomPowerGrapple() => Pick(powerGrapples);
        public MoveData RandomRunningAttack() => Pick(runningAttacks);
        public MoveData RandomGroundSubmission() => Pick(groundSubmissions);

        public IEnumerable<MoveData> AllMoves =>
            lightStrikes.Concat(heavyStrikes).Concat(quickGrapples)
                .Concat(powerGrapples).Concat(runningAttacks).Concat(groundSubmissions);

        static MoveData Pick(List<MoveData> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }
    }
}
