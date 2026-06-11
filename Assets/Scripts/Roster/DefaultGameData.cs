using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Everything created by this factory is plain in-memory ScriptableObjects.
    /// At runtime it lets the game boot with zero assets; in the editor,
    /// PrototypeAssetBuilder serializes the same set to .asset files.
    public class DefaultGameDataSet
    {
        public MoveDatabase moveDatabase;
        public List<MoveData> moves = new List<MoveData>();
        public List<WrestlerStatsData> stats = new List<WrestlerStatsData>();
        public List<SpecialAbilityData> specials = new List<SpecialAbilityData>();
        public List<PassiveTraitData> traits = new List<PassiveTraitData>();
        public List<WrestlerDefinition> definitions = new List<WrestlerDefinition>();
        public List<RosterEntry> entries = new List<RosterEntry>();
        public RosterDatabase database;
        public MatchRulesData standardRules;
        public MatchRulesData noRopeBreakRules;
        public MatchRulesData hardcoreRules;
        public AIDifficultyData easy, normal, hard;
    }

    public static class DefaultGameData
    {
        public static DefaultGameDataSet CreateAll()
        {
            var set = new DefaultGameDataSet();
            set.moveDatabase = CreateMoveDatabase(set);
            CreateRules(set);
            CreateDifficulties(set);
            CreateRoster(set);
            return set;
        }

        // ------------------------------------------------------------------
        // Moves
        // ------------------------------------------------------------------
        static MoveData Move(DefaultGameDataSet set, string id, string name, MoveCategory cat,
            float dmg, float stam, float startup, float active, float recovery,
            float stun, float momentum, float downed = 0f, bool canPin = false,
            bool lift = false, params MoveTag[] tags)
        {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.name = id;
            m.moveId = id;
            m.displayName = name;
            m.category = cat;
            m.damage = dmg;
            m.staminaCost = stam;
            m.startupTime = startup;
            m.activeTime = active;
            m.recoveryTime = recovery;
            m.stunDuration = stun;
            m.momentumGainOnHit = momentum;
            m.momentumGainOnReversal = Mathf.Max(6f, momentum * 0.8f);
            m.downedDuration = downed;
            m.causesDownedState = downed > 0f;
            m.canPinAfter = canPin;
            m.requiresLift = lift;
            m.requiresRunning = cat == MoveCategory.RunningStrike || cat == MoveCategory.RunningGrapple;
            m.reversalWindowStart = 0.05f;
            m.reversalWindowEnd = Mathf.Max(0.15f, startup);
            m.tier = cat == MoveCategory.PowerGrapple
                ? MoveTier.Heavy
                : cat == MoveCategory.HeavyStrike
                    ? MoveTier.Medium
                    : MoveTier.Light;
            m.minimumStamina = cat == MoveCategory.PowerGrapple ? stam : 0f;
            m.range = cat == MoveCategory.LightStrike || cat == MoveCategory.HeavyStrike ? 1.35f : 1.25f;
            m.tags.AddRange(tags);
            m.placeholderPoseName = cat == MoveCategory.QuickGrapple || cat == MoveCategory.PowerGrapple ? "grapple" : "strike";
            set.moves.Add(m);
            return m;
        }

        static void ConfigureGroundAttack(MoveData m, GroundTargetZone zone)
        {
            m.requiresTargetDowned = true;
            m.requiredGroundZone = zone;
            m.placeholderPoseName = "ground";
            m.range = 1.25f;
        }

        static void ConfigureCornerMove(MoveData m)
        {
            m.requiresTargetCornered = true;
            m.requiresCornerZone = true;
            m.placeholderPoseName = "corner";
            m.range = 1.3f;
        }

        static void ConfigureRopeStaggerMove(MoveData m)
        {
            m.requiresTargetRopeStaggered = true;
            m.requiresOpponentNearRopes = true;
            m.range = 1.3f;
        }

        /// Grapple timing helper: split a single duration into phases.
        static MoveData Grapple(DefaultGameDataSet set, string id, string name, MoveCategory cat,
            float dmg, float stam, float duration, float stun, float momentum,
            float downed = 0f, bool canPin = false, bool lift = false, params MoveTag[] tags)
        {
            return Move(set, id, name, cat, dmg, stam, duration * 0.3f, duration * 0.4f, duration * 0.3f,
                stun, momentum, downed, canPin, lift, tags);
        }

        public static MoveDatabase CreateMoveDatabase(DefaultGameDataSet set)
        {
            var db = ScriptableObject.CreateInstance<MoveDatabase>();
            db.name = "StarterMoveDatabase";

            db.lightStrikes.Add(Move(set, "quick-jab", "Quick Jab", MoveCategory.LightStrike, 4, 4, 0.15f, 0.10f, 0.25f, 0.25f, 4, tags: new[] { MoveTag.Clean }));
            db.lightStrikes.Add(Move(set, "short-kick", "Short Kick", MoveCategory.LightStrike, 5, 5, 0.20f, 0.10f, 0.30f, 0.30f, 5, tags: new[] { MoveTag.Clean }));

            db.heavyStrikes.Add(Move(set, "heavy-forearm", "Heavy Forearm", MoveCategory.HeavyStrike, 9, 10, 0.35f, 0.12f, 0.50f, 0.55f, 8, tags: new[] { MoveTag.Clean }));
            var boot = Move(set, "big-boot", "Big Boot", MoveCategory.HeavyStrike, 11, 13, 0.45f, 0.15f, 0.60f, 0.65f, 10, tags: new[] { MoveTag.Clean });
            boot.downsBelowHealthPercent = 35f;
            boot.downedDuration = 1.5f;
            db.heavyStrikes.Add(boot);

            var armDrag = Grapple(set, "snap-arm-drag", "Snap Arm Drag", MoveCategory.QuickGrapple, 8, 8, 0.90f, 0.50f, 8, tags: new[] { MoveTag.Clean });
            var headlock = Grapple(set, "headlock-takedown", "Side Headlock Takedown", MoveCategory.QuickGrapple, 10, 10, 1.10f, 0f, 9, downed: 1.25f, tags: new[] { MoveTag.Clean });
            var kneeLift = Grapple(set, "knee-lift", "Knee Lift", MoveCategory.QuickGrapple, 9, 8, 0.80f, 0.75f, 8, tags: new[] { MoveTag.Clean });
            var snapmare = Grapple(set, "snapmare", "Snapmare", MoveCategory.QuickGrapple, 7, 7, 0.85f, 0f, 7, downed: 1.0f, tags: new[] { MoveTag.Clean });
            db.quickGrapples.Add(armDrag);
            db.quickGrapples.Add(headlock);
            db.quickGrapples.Add(kneeLift);
            db.quickGrapples.Add(snapmare);
            db.directionalQuickGrapples.neutral.Add(kneeLift);
            db.directionalQuickGrapples.forward.Add(snapmare);
            db.directionalQuickGrapples.backward.Add(headlock);
            db.directionalQuickGrapples.lateral.Add(armDrag);
            armDrag.placeholderPoseName = "snap";
            headlock.placeholderPoseName = "snap";
            snapmare.placeholderPoseName = "snap";
            kneeLift.placeholderPoseName = "stomp"; // knee drive reads as the chambered leg

            var bodySlam = Grapple(set, "body-slam", "Body Slam", MoveCategory.PowerGrapple, 15, 18, 1.70f, 0f, 13, downed: 2.0f, canPin: true, lift: true, tags: new[] { MoveTag.Clean, MoveTag.Lift, MoveTag.Major });
            var verticalDrop = Grapple(set, "vertical-drop", "Vertical Drop", MoveCategory.PowerGrapple, 18, 22, 1.90f, 0f, 15, downed: 2.25f, canPin: true, lift: true, tags: new[] { MoveTag.Clean, MoveTag.Lift, MoveTag.Major });
            var backbreaker = Grapple(set, "backbreaker", "Backbreaker", MoveCategory.PowerGrapple, 16, 20, 1.70f, 0f, 14, downed: 1.75f, lift: true, tags: new[] { MoveTag.Clean, MoveTag.Lift });
            var shoulderThrow = Grapple(set, "shoulder-throw", "Shoulder Throw", MoveCategory.PowerGrapple, 14, 17, 1.55f, 0f, 12, downed: 1.8f, tags: new[] { MoveTag.Clean });
            db.powerGrapples.Add(bodySlam);
            db.powerGrapples.Add(verticalDrop);
            db.powerGrapples.Add(backbreaker);
            db.powerGrapples.Add(shoulderThrow);
            db.directionalPowerGrapples.neutral.Add(bodySlam);
            db.directionalPowerGrapples.forward.Add(verticalDrop);
            db.directionalPowerGrapples.backward.Add(backbreaker);
            db.directionalPowerGrapples.lateral.Add(shoulderThrow);
            bodySlam.placeholderPoseName = "slam";
            verticalDrop.placeholderPoseName = "slam";
            backbreaker.placeholderPoseName = "slam";
            shoulderThrow.placeholderPoseName = "snap";

            var clothesline = Move(set, "running-clothesline", "Running Clothesline", MoveCategory.RunningStrike, 13, 15, 0.25f, 0.20f, 0.70f, 0f, 12, downed: 1.5f, tags: new[] { MoveTag.Clean, MoveTag.Running });
            clothesline.placeholderPoseName = "lariat";
            db.runningAttacks.Add(clothesline);
            db.runningAttacks.Add(Grapple(set, "running-tackle", "Running Tackle", MoveCategory.RunningGrapple, 12, 16, 1.0f, 0f, 11, downed: 1.6f, tags: new[] { MoveTag.Clean, MoveTag.Running }));

            var elbowDrop = Move(set, "ground-elbow-drop", "Elbow Drop", MoveCategory.GroundUpperAttack, 8, 9, 0.35f, 0.15f, 0.55f, 0f, 8, tags: new[] { MoveTag.Clean, MoveTag.Ground, MoveTag.GroundUpper });
            elbowDrop.tier = MoveTier.Medium;
            var headStomp = Move(set, "ground-head-stomp", "Head Stomp", MoveCategory.GroundUpperAttack, 5, 5, 0.25f, 0.10f, 0.40f, 0f, 5, tags: new[] { MoveTag.Clean, MoveTag.Ground, MoveTag.GroundUpper });
            var kneeDrop = Move(set, "ground-knee-drop", "Knee Drop", MoveCategory.GroundLowerAttack, 8, 9, 0.35f, 0.15f, 0.55f, 0f, 8, tags: new[] { MoveTag.Clean, MoveTag.Ground, MoveTag.GroundLower });
            kneeDrop.tier = MoveTier.Medium;
            var legStomp = Move(set, "ground-leg-stomp", "Leg Stomp", MoveCategory.GroundLowerAttack, 5, 5, 0.25f, 0.10f, 0.40f, 0f, 5, tags: new[] { MoveTag.Clean, MoveTag.Ground, MoveTag.GroundLower });
            ConfigureGroundAttack(elbowDrop, GroundTargetZone.Upper);
            ConfigureGroundAttack(headStomp, GroundTargetZone.Upper);
            ConfigureGroundAttack(kneeDrop, GroundTargetZone.Lower);
            ConfigureGroundAttack(legStomp, GroundTargetZone.Lower);
            headStomp.placeholderPoseName = "stomp";
            legStomp.placeholderPoseName = "stomp";
            db.groundUpperAttacks.Add(elbowDrop);
            db.groundUpperAttacks.Add(headStomp);
            db.groundLowerAttacks.Add(kneeDrop);
            db.groundLowerAttacks.Add(legStomp);

            var cornerForearm = Move(set, "corner-forearm", "Corner Forearm Smash", MoveCategory.CornerStrike, 9, 10, 0.30f, 0.12f, 0.45f, 0.8f, 8, tags: new[] { MoveTag.Clean, MoveTag.Corner });
            cornerForearm.tier = MoveTier.Medium;
            ConfigureCornerMove(cornerForearm);
            var cornerBulldog = Move(set, "corner-bulldog", "Corner Bulldog", MoveCategory.CornerGrapple, 14, 16, 0.40f, 0.25f, 0.60f, 0f, 12, downed: 1.8f, tags: new[] { MoveTag.Clean, MoveTag.Corner });
            cornerBulldog.tier = MoveTier.Heavy;
            cornerBulldog.minimumStamina = 16f;
            ConfigureCornerMove(cornerBulldog);
            db.cornerStrikes.Add(cornerForearm);
            db.cornerGrapples.Add(cornerBulldog);

            var ropeChops = Move(set, "rope-chop-combination", "Rope Chop Combination", MoveCategory.RopeStaggerAttack, 8, 9, 0.30f, 0.15f, 0.50f, 0.6f, 8, tags: new[] { MoveTag.Clean, MoveTag.Rope });
            ropeChops.tier = MoveTier.Medium;
            ConfigureRopeStaggerMove(ropeChops);
            ropeChops.placeholderPoseName = "chop";
            var ropeSnapmare = Move(set, "rope-snapmare", "Rope Snapmare", MoveCategory.RopeStaggerAttack, 9, 10, 0.30f, 0.30f, 0.30f, 0f, 9, downed: 1.25f, tags: new[] { MoveTag.Clean, MoveTag.Rope });
            ropeSnapmare.tier = MoveTier.Medium;
            ConfigureRopeStaggerMove(ropeSnapmare);
            ropeSnapmare.placeholderPoseName = "snap";
            db.ropeStaggerAttacks.Add(ropeChops);
            db.ropeStaggerAttacks.Add(ropeSnapmare);

            var reboundLariat = Move(set, "rebound-lariat", "Rebound Lariat", MoveCategory.RopeReboundAttack, 14, 15, 0.20f, 0.20f, 0.65f, 0f, 13, downed: 1.6f, tags: new[] { MoveTag.Clean, MoveTag.Running, MoveTag.Rope });
            reboundLariat.tier = MoveTier.Heavy;
            reboundLariat.minimumStamina = 15f;
            reboundLariat.requiresRopeRebound = true;
            reboundLariat.range = 1.5f;
            reboundLariat.placeholderPoseName = "lariat";
            db.ropeReboundAttacks.Add(reboundLariat);

            var armLock = Move(set, "ground-arm-lock", "Ground Arm Lock", MoveCategory.Submission, 0, 15, 0.3f, 0.2f, 0.3f, 0f, 4, tags: new[] { MoveTag.Clean });
            armLock.submissionPressurePerSecond = 12f;
            armLock.damagePerSecond = 2f;
            db.groundSubmissions.Add(armLock);

            return db;
        }

        // ------------------------------------------------------------------
        // Rules / difficulty
        // ------------------------------------------------------------------
        static void CreateRules(DefaultGameDataSet set)
        {
            var std = ScriptableObject.CreateInstance<MatchRulesData>();
            std.name = "StandardMatchRules";
            std.displayName = "Standard Match";
            set.standardRules = std;

            var nrb = ScriptableObject.CreateInstance<MatchRulesData>();
            nrb.name = "NoRopeBreaksRules";
            nrb.displayName = "No Rope Breaks";
            nrb.ropeBreaksEnabled = false;
            nrb.refereeFiveCountEnabled = false;
            nrb.noRopeBreaks = true;
            nrb.ropeTrapSubmissionAllowed = true;
            set.noRopeBreakRules = nrb;

            var hc = ScriptableObject.CreateInstance<MatchRulesData>();
            hc.name = "HardcoreRules";
            hc.displayName = "Hardcore";
            hc.ropeBreaksEnabled = false;
            hc.refereeFiveCountEnabled = false;
            hc.hardcoreRules = true;
            hc.ropeTrapSubmissionAllowed = true;
            set.hardcoreRules = hc;
        }

        static AIDifficultyData Difficulty(string name, float aggression, float reversal, float dodge,
            float delayMin, float delayMax, float randomness, float kickout, float subEscape, float reversalCooldown)
        {
            var d = ScriptableObject.CreateInstance<AIDifficultyData>();
            d.name = name + "Difficulty";
            d.displayName = name;
            d.aggression = aggression;
            d.reversalAccuracy = reversal;
            d.dodgeAccuracy = dodge;
            d.reactionDelayMin = delayMin;
            d.reactionDelayMax = delayMax;
            d.randomness = randomness;
            d.kickoutBonus = kickout;
            d.submissionEscapeBonus = subEscape;
            d.reversalCooldown = reversalCooldown;
            return d;
        }

        static void CreateDifficulties(DefaultGameDataSet set)
        {
            set.easy = Difficulty("Easy", 0.35f, 0.20f, 0.15f, 0.50f, 0.90f, 0.35f, -0.10f, -0.10f, 1.2f);
            set.normal = Difficulty("Normal", 0.55f, 0.40f, 0.30f, 0.30f, 0.60f, 0.25f, 0f, 0f, 0.8f);
            set.hard = Difficulty("Hard", 0.75f, 0.60f, 0.45f, 0.15f, 0.35f, 0.15f, 0.15f, 0.15f, 0.5f);
        }

        // ------------------------------------------------------------------
        // Stats / traits / specials helpers
        // ------------------------------------------------------------------
        static WrestlerStatsData Stats(DefaultGameDataSet set, string name, WeightClass weight,
            LiftStrengthClass lift, AIPersonality personality,
            float reversal = 0.5f, float dodge = 0.5f, float kickout = 0.5f, float subResist = 0.5f)
        {
            var s = ScriptableObject.CreateInstance<WrestlerStatsData>();
            s.name = name + "Stats";
            s.displayName = name;
            s.weightClass = weight;
            s.liftStrengthClass = lift;
            s.aiPersonality = personality;
            s.reversalSkill = reversal;
            s.dodgeSkill = dodge;
            s.kickoutSkill = kickout;
            s.submissionResistance = subResist;
            set.stats.Add(s);
            return s;
        }

        static PassiveTraitData Trait(DefaultGameDataSet set, string id, string name, string owner,
            PassiveTraitEffectType type, float value = 0f, float tier1 = 0f, float tier2Threshold = 0f,
            float tier2Value = 0f, float momentum = 0f, float duration = 0f, bool once = false, string ui = "")
        {
            var t = ScriptableObject.CreateInstance<PassiveTraitData>();
            t.name = id;
            t.traitId = id;
            t.displayName = name;
            t.owningRosterId = owner;
            t.gameplayEffectType = type;
            t.value = value;
            t.healthThreshold = tier1;
            t.healthThresholdTier2 = tier2Threshold;
            t.valueTier2 = tier2Value;
            t.momentumOnTrigger = momentum;
            t.duration = duration;
            t.oncePerMatch = once;
            t.uiMessage = ui;
            set.traits.Add(t);
            return t;
        }

        static SpecialAbilityData Special(DefaultGameDataSet set, string id, string name, string owner,
            SpecialCategory category, float stamina, float damage)
        {
            var s = ScriptableObject.CreateInstance<SpecialAbilityData>();
            s.name = id;
            s.specialId = id;
            s.displayName = name;
            s.owningRosterId = owner;
            s.category = category;
            s.staminaCost = stamina;
            s.damage = damage;
            set.specials.Add(s);
            return s;
        }

        // ------------------------------------------------------------------
        // The 16-wrestler roster
        // ------------------------------------------------------------------
        static void CreateRoster(DefaultGameDataSet set)
        {
            set.database = ScriptableObject.CreateInstance<RosterDatabase>();
            set.database.name = "RosterDatabase";

            Add(set, "tas-anuka-gutierrez", "Anuka Gutierrez", new Color(0.55f, 0.3f, 0.15f),
                Stats(set, "Anuka", WeightClass.Middleweight, LiftStrengthClass.Average, AIPersonality.Technician, reversal: 0.75f, subResist: 0.7f),
                AnukaSpecial(set), null);

            Add(set, "tas-avalon", "Michael Avalon", new Color(0.9f, 0.75f, 0.2f),
                Stats(set, "Avalon", WeightClass.Middleweight, LiftStrengthClass.Average, AIPersonality.Showman),
                AvalonSpecial(set), null);

            Add(set, "tas-carter-cash", "Carter Cash", new Color(0.15f, 0.7f, 0.3f),
                Stats(set, "Carter", WeightClass.Lightweight, LiftStrengthClass.Low, AIPersonality.HighFlyer, dodge: 0.65f),
                CarterSpecial(set), null);

            Add(set, "tas-codah", "Codah Alexander", new Color(0.6f, 0.2f, 0.8f),
                Stats(set, "Codah", WeightClass.Lightweight, LiftStrengthClass.Average, AIPersonality.HighFlyer, reversal: 0.6f, dodge: 0.6f),
                CodahSpecial(set), null);

            Add(set, "tas-cody-devine", "Cody Devine", new Color(0.4f, 0.85f, 0.85f),
                Stats(set, "Cody", WeightClass.Middleweight, LiftStrengthClass.Average, AIPersonality.Trickster),
                CodySpecial(set), null);

            Add(set, "tas-dean-mercer", "Dean Mercer", new Color(0.25f, 0.25f, 0.3f),
                Stats(set, "Dean", WeightClass.Heavyweight, LiftStrengthClass.Heavyweight, AIPersonality.Powerhouse, kickout: 0.6f),
                DeanSpecial(set), null);

            Add(set, "tas-erza", "Erza Menagerie Tinker", new Color(0.9f, 0.4f, 0.7f),
                Stats(set, "Erza", WeightClass.Lightweight, LiftStrengthClass.Low, AIPersonality.HighFlyer, dodge: 0.75f),
                ErzaSpecial(set), null);

            Add(set, "tas-franky-gonzales", "Franky Gonzales", new Color(0.95f, 0.55f, 0.1f),
                Stats(set, "Franky", WeightClass.Middleweight, LiftStrengthClass.Average, AIPersonality.Brawler),
                FrankySpecial(set), null);

            Add(set, "tas-hussy", "Hussy Steele", new Color(0.7f, 0.1f, 0.4f),
                Stats(set, "Hussy", WeightClass.Heavyweight, LiftStrengthClass.Strong, AIPersonality.Powerhouse, kickout: 0.6f),
                HussySpecial(set), null);

            Add(set, "tas-johnny-crash", "Johnny Crash", new Color(0.3f, 0.3f, 0.9f),
                Stats(set, "Johnny", WeightClass.SuperHeavyweight, LiftStrengthClass.Heavyweight, AIPersonality.Brawler, kickout: 0.7f),
                JohnnySpecial(set), JohnnyTraits(set));

            Add(set, "tas-jt-staten", "JT Staten", new Color(0.75f, 0.75f, 0.75f),
                Stats(set, "JT", WeightClass.Middleweight, LiftStrengthClass.Average, AIPersonality.Technician, reversal: 0.6f),
                JtSpecial(set), null);

            Add(set, "tas-major-glory", "Major Glory", new Color(0.85f, 0.1f, 0.1f),
                Stats(set, "Glory", WeightClass.Middleweight, LiftStrengthClass.Average, AIPersonality.Balanced, kickout: 0.65f),
                GlorySpecial(set), GloryTraits(set));

            Add(set, "tas-morgana-lavey", "Morgana Lavey", new Color(0.35f, 0.1f, 0.45f),
                Stats(set, "Morgana", WeightClass.Lightweight, LiftStrengthClass.Low, AIPersonality.Trickster, reversal: 0.6f),
                MorganaSpecial(set), MorganaTraits(set));

            Add(set, "tas-nicky-hyde", "Nicky Hyde", new Color(0.1f, 0.55f, 0.5f),
                Stats(set, "Nicky", WeightClass.Middleweight, LiftStrengthClass.Strong, AIPersonality.Technician, reversal: 0.8f),
                NickySpecial(set), NickyTraits(set));

            var vigilante = Add(set, "tas-vigilante-oai", "The Vigilante", new Color(0.15f, 0.15f, 0.15f),
                Stats(set, "Vigilante", WeightClass.Middleweight, LiftStrengthClass.Average, AIPersonality.Evasive, dodge: 0.85f),
                VigilanteSpecial(set), null);
            vigilante.wrestlerDefinition.dodgeAbility = VanishingDodge(set);

            Add(set, "tas-zeak-gallent", "Zeak Gallent", new Color(0.95f, 0.85f, 0.3f),
                Stats(set, "Zeak", WeightClass.Middleweight, LiftStrengthClass.Strong, AIPersonality.Balanced, reversal: 0.6f, dodge: 0.6f, kickout: 0.6f),
                ZeakSpecial(set), ZeakTraits(set));
        }

        static RosterEntry Add(DefaultGameDataSet set, string rosterId, string displayName, Color color,
            WrestlerStatsData stats, SpecialAbilityData special, List<PassiveTraitData> traits)
        {
            stats.displayName = displayName;

            var def = ScriptableObject.CreateInstance<WrestlerDefinition>();
            def.name = rosterId + "-definition";
            def.wrestlerId = rosterId;
            def.displayName = displayName;
            def.stats = stats;
            def.moveset = set.moveDatabase;
            def.special = special;
            def.placeholderColor = color;
            if (traits != null) def.passiveTraits.AddRange(traits);
            set.definitions.Add(def);

            var entry = ScriptableObject.CreateInstance<RosterEntry>();
            entry.name = rosterId;
            entry.rosterId = rosterId;
            entry.displayName = displayName;
            entry.sourceImageFileName = rosterId + ".png";
            entry.wrestlerDefinition = def;
            set.entries.Add(entry);
            set.database.entries.Add(entry);
            return entry;
        }

        // ------------------------------------------------------------------
        // Wrestler specials (values per design spec)
        // ------------------------------------------------------------------
        static SpecialAbilityData AnukaSpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "trap-and-snap-armbar", "Trap-and-Snap Armbar", "tas-anuka-gutierrez", SpecialCategory.CounterSubmission, 20, 0);
            s.counterWindow = 0.75f;
            s.counterWhiffRecovery = 0.65f;
            s.initialDamage = 8f;
            s.submissionPressurePerSecond = 12f;
            s.submissionPressureBonus = 0.25f;
            s.requiresOpponentStanding = true;
            s.causesDownedState = false;
            return s;
        }

        static SpecialAbilityData AvalonSpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "spotlight-crab", "Spotlight Crab", "tas-avalon", SpecialCategory.SpecialSubmission, 22, 0);
            s.requiresOpponentDowned = true;
            s.setupDuration = 0.65f;
            s.initialDamage = 5f;
            s.submissionPressurePerSecond = 14f;
            s.debuffId = "slowed-leg";
            s.debuffDuration = 5f;
            s.causesDownedState = false;
            return s;
        }

        static SpecialAbilityData Aerial(DefaultGameDataSet set, string id, string name, string owner,
            AerialAnchorType anchorType, float stamina, float setup, float air, float damage,
            float selfMiss, float downed, float missRecovery, float tolerance = 1.0f)
        {
            var s = Special(set, id, name, owner, SpecialCategory.SpecialAerial, stamina, damage);
            s.requiresOpponentDowned = true;
            s.requiredLaunchAnchorType = anchorType;
            s.requiresTopCornerAnchor = anchorType == AerialAnchorType.TopCorner;
            s.requiresMiddleCornerAnchor = anchorType == AerialAnchorType.MiddleCorner;
            s.requiresRopeMiddleAnchor = anchorType == AerialAnchorType.RopeMiddle;
            s.usesJumpArc = true;
            s.canMiss = true;
            s.aerialSetupDuration = setup;
            s.climbDuration = setup;
            s.airborneDuration = air;
            s.selfDamageOnMiss = selfMiss;
            s.downedDuration = downed;
            s.missRecoveryDuration = missRecovery;
            s.landingTolerance = tolerance;
            s.canPinAfter = true;
            return s;
        }

        static SpecialAbilityData CarterSpecial(DefaultGameDataSet set) =>
            Aerial(set, "cash-out-splash", "Cash Out Splash", "tas-carter-cash",
                AerialAnchorType.TopCorner, 30, 0.85f, 0.75f, 30, 14, 3.0f, 2.25f);

        static SpecialAbilityData CodahSpecial(DefaultGameDataSet set)
        {
            var s = Aerial(set, "sky-high-leg-drop", "Sky-High Leg Drop", "tas-codah",
                AerialAnchorType.TopCorner, 28, 0.75f, 0.65f, 26, 12, 3.0f, 2.0f);
            s.buffId = "technical-advantage";
            s.buffDuration = 4f;
            return s;
        }

        static SpecialAbilityData CodySpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "cloud-cover", "Cloud Cover", "tas-cody-devine", SpecialCategory.DirtySpecial, 12, 14);
            s.requiresOpponentStanding = true;
            s.usesRefereeDistraction = true;
            s.usesConeHitDetection = true;
            s.coneRange = 1.35f;
            s.coneAngle = 55f;
            s.refDistractionSetup = 0.60f;
            s.hiddenObjectSetup = 0.45f;
            s.cloudActiveDuration = 0.50f;
            s.stunDuration = 2.25f;
            s.downedDuration = 3.0f;
            s.pinWindow = 2.5f;
            s.caughtPenaltyMomentumLoss = 50f;
            return s;
        }

        static SpecialAbilityData DeanSpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "final-notice", "Final Notice", "tas-dean-mercer", SpecialCategory.SpecialPowerGrapple, 28, 30);
            s.specialVariant = "dual-position";
            s.requiresOpponentStanding = true;
            s.requiresTargetLiftable = true;
            s.downedDuration = 3.25f;
            s.appliesKickoutPenaltyOnImmediatePin = true;
            s.kickoutPenaltyValue = 0.15f;
            // Choke variant numbers:
            s.initialDamage = 6f;
            s.submissionPressurePerSecond = 18f;
            s.opponentStaminaDrainPerSecond = 10f;
            return s;
        }

        static SpecialAbilityData ErzaSpecial(DefaultGameDataSet set)
        {
            var s = Aerial(set, "erzasault", "Erzasault", "tas-erza",
                AerialAnchorType.RopeMiddle, 26, 0.45f, 0.70f, 27, 11, 3.0f, 1.35f, tolerance: 1.0f);
            s.usesCrescentArc = true;
            s.disallowCornerAnchor = true;
            s.validLandingLaneWidth = 1.3f;
            s.buffId = "agility-recovery";
            s.buffDuration = 3f;
            return s;
        }

        static SpecialAbilityData FrankySpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "6-7-moves-of-doom", "6-7 Moves of Doom", "tas-franky-gonzales", SpecialCategory.SpecialCombo, 30, 0);
            s.requiresOpponentStanding = true;
            s.requiresOpponentCornered = true;
            s.requiresCornerZone = true;
            s.downedDuration = 3.0f;
            s.pinWindow = 2.5f;
            s.debuffId = "dazed";
            s.debuffDuration = 3f;
            s.appliesKickoutPenaltyOnImmediatePin = true;
            s.kickoutPenaltyValue = 0.12f;
            s.comboSteps = new List<ComboStep>
            {
                new ComboStep { StepName = "Vicious back leg kicks", Duration = 0.5f, Damage = 4, StaminaDamage = 3, ReversalWindow = true },
                new ComboStep { StepName = "Unforgiving chop", Duration = 0.4f, Damage = 4, StaminaDamage = 2 },
                new ComboStep { StepName = "Back elbow", Duration = 0.4f, Damage = 4, StaminaDamage = 2 },
                new ComboStep { StepName = "Double knee strike", Duration = 0.45f, Damage = 5, StaminaDamage = 3 },
                new ComboStep { StepName = "Leg sweep", Duration = 0.4f, Damage = 3, StaminaDamage = 2 },
                new ComboStep { StepName = "Second double knee", Duration = 0.45f, Damage = 5, StaminaDamage = 3, ReversalWindow = true },
                new ComboStep { StepName = "Head-first mat drive", Duration = 0.6f, Damage = 6, StaminaDamage = 3, CausesDowned = true }
            };
            return s;
        }

        static SpecialAbilityData HussySpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "steele-backbreaker", "Steele Backbreaker", "tas-hussy", SpecialCategory.SpecialPowerGrapple, 30, 27);
            s.requiresOpponentStanding = true;
            s.requiresFrontPosition = true;
            s.requiresTargetLiftable = true;
            s.startsCarryPhase = true;
            s.carryDuration = 1.25f;
            s.carryMoveSpeed = 2f;
            s.staminaDamage = 22f;
            s.downedDuration = 3.25f;
            s.debuffId = "back-damage";
            s.debuffDuration = 6f;
            return s;
        }

        static SpecialAbilityData JohnnySpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "human-wrecking-ball", "Human Wrecking Ball", "tas-johnny-crash", SpecialCategory.SpecialRush, 24, 30);
            s.requiresOpponentStanding = true;
            s.usesChargeMovement = true;
            s.chargeMaxDuration = 1.25f;
            s.chargeMaxDistance = 5.5f;
            s.damageShort = 16f;
            s.damageMedium = 22f;
            s.selfDamageOnWallCollision = 6f;
            s.downedDuration = 2.5f;
            return s;
        }

        static List<PassiveTraitData> JohnnyTraits(DefaultGameDataSet set) => new List<PassiveTraitData>
        {
            Trait(set, "heavyweight-anchor", "Heavyweight Anchor", "tas-johnny-crash",
                PassiveTraitEffectType.LiftImmunity, momentum: 8f, ui: "Too heavy to lift!"),
            Trait(set, "heart-of-crash-recovery", "Heart of Crash", "tas-johnny-crash",
                PassiveTraitEffectType.StaminaRecoveryBonus, value: 0.15f, tier1: 0.5f, tier2Threshold: 0.25f, tier2Value: 0.30f,
                ui: "Johnny won't stay down!"),
            Trait(set, "heart-of-crash-getup", "Heart of Crash (Get-Up)", "tas-johnny-crash",
                PassiveTraitEffectType.DownedDurationReduction, value: 0.35f, tier1: 0.25f, momentum: 10f, once: true,
                ui: "Johnny won't stay down!")
        };

        static SpecialAbilityData JtSpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "statutes-in-stone", "Statutes in Stone", "tas-jt-staten", SpecialCategory.SpecialGroundedSequence, 26, 24);
            s.requiresOpponentDowned = true;
            s.requiresOpponentHeadPosition = true;
            s.requiresRopeReboundLane = true;
            s.usesRopeRun = true;
            s.setupDuration = 0.65f;
            s.initialDamage = 4f; // chest slap
            s.canMiss = true;
            s.selfDamageOnMiss = 6f;
            s.missRecoveryDuration = 1.35f;
            s.landingTolerance = 1.1f;
            s.downedDuration = 3.0f;
            s.autoPinOnHit = true;
            s.autoPinDelay = 0.25f;
            s.appliesKickoutPenaltyOnImmediatePin = true;
            s.kickoutPenaltyValue = 0.12f;
            return s;
        }

        static SpecialAbilityData GlorySpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "patriot-plunge", "Patriot Plunge", "tas-major-glory", SpecialCategory.SpecialPowerGrapple, 24, 25);
            s.specialVariant = "side-by-side";
            s.requiresOpponentStanding = true;
            s.requiresSideBySidePosition = true;
            s.downedDuration = 3.0f;
            s.pinWindow = 2.5f;
            return s;
        }

        static List<PassiveTraitData> GloryTraits(DefaultGameDataSet set) => new List<PassiveTraitData>
        {
            Trait(set, "national-resolve-recovery", "National Resolve", "tas-major-glory",
                PassiveTraitEffectType.StaminaRecoveryBonus, value: 0.10f, tier1: 0.4f, ui: "Major Glory digs deep!"),
            Trait(set, "national-resolve-reversal", "National Resolve (Reversals)", "tas-major-glory",
                PassiveTraitEffectType.ReversalLeniency, value: 0.05f, tier1: 0.4f),
            Trait(set, "national-resolve-kickout", "National Resolve (Last Chance)", "tas-major-glory",
                PassiveTraitEffectType.LastChanceKickout, value: 0.30f, tier1: 0.2f, momentum: 15f, once: true,
                ui: "Major Glory rallies!")
        };

        static SpecialAbilityData MorganaSpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "the-tarantula", "The Tarantula", "tas-morgana-lavey", SpecialCategory.SpecialRopeTrap, 22, 0);
            s.requiresOpponentStanding = true;
            s.requiresOpponentNearRopes = true;
            s.requiresOpponentInRopeStagger = true;
            s.requiresRopeTrapZone = true;
            s.startsRopeTrapState = true;
            s.startsRefereeFiveCount = true;
            s.forceReleaseAtFiveIfRopeBreaksEnabled = true;
            s.canSubmitOnlyIfNoRopeBreaks = true;
            s.setupDuration = 0.55f;
            s.lockDuration = 0.35f;
            s.standardMaxHold = 5.0f;
            s.damagePerSecond = 3f;
            s.opponentStaminaDrainPerSecond = 8f;
            s.selfStaminaDrainPerSecond = 5f;
            s.submissionPressurePerSecond = 13f;
            s.causesDownedState = false;
            return s;
        }

        static List<PassiveTraitData> MorganaTraits(DefaultGameDataSet set) => new List<PassiveTraitData>
        {
            Trait(set, "smoke-and-mirrors", "Smoke and Mirrors", "tas-morgana-lavey",
                PassiveTraitEffectType.RopeTrapSetupBonus, value: 0.25f, duration: 4f, ui: "Morgana sets the trap.")
        };

        static SpecialAbilityData NickySpecial(DefaultGameDataSet set)
        {
            var s = Special(set, "hyde-bomb", "Hyde Bomb", "tas-nicky-hyde", SpecialCategory.SpecialPowerGrapple, 32, 31);
            s.requiresOpponentStanding = true;
            s.requiresFrontPosition = true;
            s.requiresTargetLiftable = true;
            s.spinCount = 3;
            s.spinDuration = 1.20f;
            s.downedDuration = 3.40f;
            s.debuffId = "disoriented";
            s.debuffDuration = 4f;
            s.appliesKickoutPenaltyOnImmediatePin = true;
            s.kickoutPenaltyValue = 0.10f;
            s.benefitsFromRecentReversal = true;
            return s;
        }

        static List<PassiveTraitData> NickyTraits(DefaultGameDataSet set) => new List<PassiveTraitData>
        {
            Trait(set, "hide-the-pain-leniency", "Hide the Pain", "tas-nicky-hyde",
                PassiveTraitEffectType.ReversalLeniency, value: 0.05f, tier1: 0.5f, tier2Threshold: 0.25f, tier2Value: 0.09f,
                ui: "Nicky laughs through the pain."),
            Trait(set, "hide-the-pain-cost", "Hide the Pain (Cost)", "tas-nicky-hyde",
                PassiveTraitEffectType.ReversalStaminaDiscount, value: 0.10f, tier1: 0.5f, tier2Threshold: 0.25f, tier2Value: 0.15f)
        };

        static SpecialAbilityData VigilanteSpecial(DefaultGameDataSet set)
        {
            var s = Aerial(set, "vigilante-moonsault", "Vigilante Moonsault", "tas-vigilante-oai",
                AerialAnchorType.MiddleCorner, 24, 0.55f, 0.65f, 25, 10, 2.8f, 1.5f, tolerance: 1.05f);
            s.pinWindow = 2.2f;
            s.benefitsFromRecentDodge = true;
            return s;
        }

        static SpecialAbilityData VanishingDodge(DefaultGameDataSet set)
        {
            var s = Special(set, "vanishing-dodge", "Vanishing Dodge", "tas-vigilante-oai", SpecialCategory.EvasiveDodge, 18, 0);
            s.requiresFullMomentum = false;
            s.momentumCost = 0f;
            s.spendsAllMomentum = false;
            s.cooldown = 8f;
            s.canEscapeMajorMoves = true;
            s.escapableMoveTags = new List<MoveTag> { MoveTag.Lift, MoveTag.Carry, MoveTag.Major, MoveTag.Running };
            s.manualTimingWindow = 0.18f;
            s.emergencyTimingWindow = 0.25f;
            s.repositionDistance = 1.25f;
            s.invulnerabilityDuration = 0.35f;
            s.hasOncePerMatchEmergencyVersion = true;
            s.failedDodgeStaminaCost = 6f;
            return s;
        }

        static SpecialAbilityData ZeakSpecial(DefaultGameDataSet set)
        {
            var s = Aerial(set, "falling-star", "Falling Star", "tas-zeak-gallent",
                AerialAnchorType.TopCorner, 28, 0.70f, 0.75f, 29, 12, 3.25f, 2.0f);
            s.pinWindow = 2.6f;
            s.appliesKickoutPenaltyOnImmediatePin = true;
            s.kickoutPenaltyValue = 0.08f;
            s.buffId = "clean-follow-up";
            s.buffDuration = 4f;
            return s;
        }

        static List<PassiveTraitData> ZeakTraits(DefaultGameDataSet set) => new List<PassiveTraitData>
        {
            Trait(set, "honorable-handshake", "Honorable Handshake", "tas-zeak-gallent",
                PassiveTraitEffectType.HandshakeRitual, ui: "Respect shown."),
            Trait(set, "clean-momentum", "Clean Momentum", "tas-zeak-gallent",
                PassiveTraitEffectType.CleanMomentumBonus)
        };
    }
}
