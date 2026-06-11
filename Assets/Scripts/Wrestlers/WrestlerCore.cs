using UnityEngine;

namespace LoCoFight
{
    /// Root component of a wrestler. Owns references to every subsystem and
    /// wires them together. Never touches meshes or input directly.
    public class WrestlerCore : MonoBehaviour
    {
        public RosterEntry Entry { get; private set; }
        public bool IsPlayer { get; private set; }
        public WrestlerCore Opponent { get; private set; }

        public WrestlerMotor Motor { get; private set; }
        public WrestlerCombat Combat { get; private set; }
        public WrestlerStateMachine States { get; private set; }
        public WrestlerStatsRuntime Stats { get; private set; }
        public WrestlerView View { get; private set; }
        public IAnimationDriver Anim { get; private set; }
        public PassiveTraitController Traits { get; private set; }
        public SpecialController Specials { get; private set; }
        public BuffDebuffController Buffs { get; private set; }
        public DodgeSystem Dodge { get; private set; }

        public string DisplayName => Entry != null ? Entry.displayName : name;

        public static WrestlerCore Create(string goName, RosterEntry entry, bool isPlayer, Vector3 spawnPos, Color fallbackColor)
        {
            var go = new GameObject(goName);
            go.transform.position = spawnPos;

            var def = entry != null ? entry.wrestlerDefinition : null;
            var weight = def != null && def.stats != null ? def.stats.weightClass : WeightClass.Middleweight;

            // The collider tracks the humanoid rig's silhouette: taller and a
            // touch wider for the heavier classes. Visual limbs never collide;
            // this capsule is the only physics volume on a wrestler.
            var cc = go.AddComponent<CharacterController>();
            cc.height = WrestlerView.RigHeight * WrestlerView.HeightFor(weight) * WrestlerView.GlobalScale;
            cc.radius = 0.35f * WrestlerView.GlobalScale * Mathf.Lerp(1f, WrestlerView.BulkFor(weight), 0.5f);
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

            var core = go.AddComponent<WrestlerCore>();
            core.Motor = go.AddComponent<WrestlerMotor>();
            core.States = go.AddComponent<WrestlerStateMachine>();
            core.Stats = go.AddComponent<WrestlerStatsRuntime>();
            core.Buffs = go.AddComponent<BuffDebuffController>();
            core.View = go.AddComponent<WrestlerView>();
            core.Combat = go.AddComponent<WrestlerCombat>();
            core.Traits = go.AddComponent<PassiveTraitController>();
            core.Specials = go.AddComponent<SpecialController>();
            core.Dodge = go.AddComponent<DodgeSystem>();
            var driver = go.AddComponent<PlaceholderAnimationDriver>();
            core.Anim = driver;

            core.Entry = entry;
            core.IsPlayer = isPlayer;

            Color bodyColor = def != null ? def.placeholderColor : fallbackColor;
            core.View.BuildPlaceholder(bodyColor, weight);
            driver.Bind(core.View);

            core.States.Bind(core);
            core.Motor.Bind(core);
            core.Stats.Initialize(core, def != null ? def.stats : null);
            core.Combat.Bind(core);
            core.Traits.Initialize(core, def != null ? def.passiveTraits : null);
            core.Specials.Bind(core, def != null ? def.special : null);
            core.Dodge.Bind(core, def != null ? def.dodgeAbility : null);

            return core;
        }

        public void SetOpponent(WrestlerCore opponent) => Opponent = opponent;

        public float DistanceToOpponent() =>
            Opponent == null ? 999f : MathUtil.FlatDistance(transform.position, Opponent.transform.position);

        public MoveDatabase Moveset =>
            Entry != null && Entry.wrestlerDefinition != null ? Entry.wrestlerDefinition.moveset : null;

        public void ResetForMatch(Vector3 spawnPos)
        {
            Stats.ResetMeters();
            Buffs.Clear();
            Traits.ResetForMatch();
            Specials.ResetForMatch();
            Combat.ForceRelease();
            Motor.SetScriptedControl(false);
            Motor.Teleport(spawnPos);
            States.Set(WrestlerState.Idle);
            Motor.FaceOpponent();
        }
    }
}
