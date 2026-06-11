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

        [Header("Contextual families")]
        public List<MoveData> groundUpperAttacks = new List<MoveData>();
        public List<MoveData> groundLowerAttacks = new List<MoveData>();
        public DirectionalMoveSet directionalQuickGrapples = new DirectionalMoveSet();
        public DirectionalMoveSet directionalPowerGrapples = new DirectionalMoveSet();
        public List<MoveData> cornerStrikes = new List<MoveData>();
        public List<MoveData> cornerGrapples = new List<MoveData>();
        public List<MoveData> ropeStaggerAttacks = new List<MoveData>();
        public List<MoveData> ropeReboundAttacks = new List<MoveData>();

        public MoveData RandomLightStrike() => Pick(lightStrikes);
        public MoveData RandomHeavyStrike() => Pick(heavyStrikes);
        public MoveData RandomQuickGrapple() => Pick(quickGrapples);
        public MoveData RandomPowerGrapple() => Pick(powerGrapples);
        public MoveData RandomRunningAttack() => Pick(runningAttacks);
        public MoveData RandomGroundSubmission() => Pick(groundSubmissions);
        public MoveData RandomGroundUpperAttack() => Pick(groundUpperAttacks);
        public MoveData RandomGroundLowerAttack() => Pick(groundLowerAttacks);
        public MoveData RandomCornerStrike() => Pick(cornerStrikes);
        public MoveData RandomCornerGrapple() => Pick(cornerGrapples);
        public MoveData RandomRopeStaggerAttack() => Pick(ropeStaggerAttacks);
        public MoveData RandomRopeReboundAttack() => Pick(ropeReboundAttacks);

        public IEnumerable<MoveData> AllMoves =>
            lightStrikes.Concat(heavyStrikes).Concat(quickGrapples)
                .Concat(powerGrapples).Concat(runningAttacks).Concat(groundSubmissions)
                .Concat(groundUpperAttacks).Concat(groundLowerAttacks)
                .Concat(directionalQuickGrapples.AllMoves())
                .Concat(directionalPowerGrapples.AllMoves())
                .Concat(cornerStrikes).Concat(cornerGrapples)
                .Concat(ropeStaggerAttacks).Concat(ropeReboundAttacks);

        static MoveData Pick(List<MoveData> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }
    }
}
