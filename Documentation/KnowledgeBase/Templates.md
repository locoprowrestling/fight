# Templates

Copy-paste recipes for the common additions. All data lives in [DefaultGameData.cs](../../Assets/Scripts/Roster/DefaultGameData.cs); after any data change, regenerate assets via **Tools > LoCo Fight Game > Create Default Prototype Assets**.

## New wrestler

In `CreateRoster()`:

```csharp
Add(set, "tas-new-wrestler", "New Wrestler", new Color(0.5f, 0.5f, 0.5f),   // placeholder body color
    Stats(set, "New", WeightClass.Middleweight, LiftStrengthClass.Average,  // weight drives rig bulk + CC size
        AIPersonality.Balanced, reversal: 0.5f, dodge: 0.5f, kickout: 0.5f, subResist: 0.5f),
    NewWrestlerSpecial(set),   // factory below; one special per wrestler
    NewWrestlerTraits(set));   // or null if no passive traits
```

Checklist beyond the code:
1. Portrait: drop `tas-new-wrestler.png` in `players-web/`, run the importer (`RosterAssetImporter`) so it lands in `Assets/Art/RosterPortraits/`. `RosterEntry.sourceImageFileName` is derived from the roster id.
2. Regenerate assets (menu above) — otherwise the saved `RosterDatabase` asset won't contain the new entry.
3. Document the character in [Roster.md](../Roster.md) and the special in [SpecialAbilities.md](../SpecialAbilities.md).
4. Add a [TestingChecklist.md](../TestingChecklist.md) line if the wrestler has unique mechanics.

## New move

In `CreateMoveDatabase()` — use `Move` for strikes (explicit phases) or `Grapple` for grapples (one duration split 30/40/30):

```csharp
// Move(set, id, name, category, dmg, stamina, startup, active, recovery, stun, momentum, ...)
db.heavyStrikes.Add(Move(set, "spinning-backfist", "Spinning Backfist", MoveCategory.HeavyStrike,
    10, 11, 0.40f, 0.12f, 0.55f, 0.6f, 9, tags: new[] { MoveTag.Clean }));

// Grapple(set, id, name, category, dmg, stamina, totalDuration, stun, momentum, downed:, canPin:, lift:, ...)
db.powerGrapples.Add(Grapple(set, "powerbomb", "Powerbomb", MoveCategory.PowerGrapple,
    17, 21, 1.5f, 0f, 14, downed: 2.1f, canPin: true, lift: true,
    tags: new[] { MoveTag.Clean, MoveTag.Lift, MoveTag.Major }));
```

Notes:
- Range, reversal window, `requiresRunning`, and `placeholderPoseName` are derived from the category inside `Move()` — only override on the returned instance for exceptions (see `big-boot`'s `downsBelowHealthPercent`).
- `lift: true` moves are gated by `LiftStrengthClass` vs `WeightClass` (`CombatResolver.ValidateLift`); set `fallbackMoveIfLiftFails` or accept the fail-and-stun path.
- Both player and CPU pick moves via `MoveDatabase.Random*` — adding to the right list is the whole integration.
- Put the move in the database list matching its legal context. A new context needs an explicit list/resolver path, not inclusion in a loosely related existing list.
- Declare every compatibility requirement the resolver needs: running, grapple role, lift strength, rope/corner state, target state, or other positional constraint.
- Verify the failure path as well as the success path: insufficient stamina, invalid context, failed lift, missed hit, and interrupted grapple must leave both wrestlers in valid states.
- Keep damage, timing, stamina, momentum, reversal windows, and state consequences in move/combat data. Animation names and placeholder poses are presentation hooks only.

## New special

Factory next to the other `*Special(set)` methods:

```csharp
static SpecialAbilityData NewWrestlerSpecial(DefaultGameDataSet set)
{
    var s = Special(set, "special-id", "Display Name", "tas-new-wrestler", SpecialCategory.Rush, stamina: 25, damage: 22);
    // then category-specific fields, e.g. for a counter:
    // s.counterWindow = 0.75f; s.requiresOpponentStanding = true;
    return s;
}
```

Existing categories each have an executor in `Assets/Scripts/Specials/` (`RushSpecialExecutor`, `AerialSpecialExecutor`, `CounterSpecialExecutor`, `DirtySpecialExecutor`, `RopeTrapSpecialExecutor`, `SequenceSpecialExecutor`). For a **new category**: add the enum value, write a `NewCategoryExecutor : SpecialExecutor`, wire it in `SpecialController`, and document it in [SpecialAbilities.md](../SpecialAbilities.md). Specials must respect reversal windows (`SpecialController.ReversalWindowOpen`) so the defender always has counterplay.

## New passive trait

```csharp
// Trait(set, id, name, ownerRosterId, effectType, value:, tier1:, tier2Threshold:, tier2Value:, momentum:, duration:, once:, ui:)
Trait(set, "iron-chin", "Iron Chin", "tas-new-wrestler",
    PassiveTraitEffectType.DamageReductionBelowHealth, value: 0.15f, tier1: 30f,
    ui: "Iron Chin: taking less damage!");
```

If the effect type is new, add it to `PassiveTraitEffectType` and handle it in [PassiveTraitController.cs](../../Assets/Scripts/Traits/PassiveTraitController.cs) — that controller is the only consumer.

## New wrestler state

1. Add the enum value in [WrestlerStateMachine.cs](../../Assets/Scripts/Wrestlers/WrestlerStateMachine.cs).
2. Add its `StateProfile` in `BuildProfiles()` — decide every flag deliberately, and give any non-terminal state a `timeout` + `exit` so it can't strand a wrestler:

```csharp
d[WrestlerState.NewState] = P(rotate: true, strikable: true, timeout: 1.0f, exit: WrestlerState.Idle);
```

3. Give it a visual: add a `case "NewState":` pose in `ComputePose()` in [PlaceholderAnimationDriver.cs](../../Assets/Scripts/Animation/PlaceholderAnimationDriver.cs) (unhandled states fall back to locomotion/stand).
4. If it's a mutual state (two wrestlers locked together), apply the three-things rule from [BestPractices.md](BestPractices.md#states): timeout, owner, external-dissolve cleanup.

## New animation pose

Poses are `BodyPose` cases in `ComputePose()` — local Euler targets per joint plus `root` tilt, `lift` (y), `shift` (z). Joint sign conventions are in [Examples.md](Examples.md#joint-sign-conventions); start from a helper (`StandPose()`, `FightStance()`, `Crouch()`, `LyingPose()`) and override:

```csharp
case "NewState":
    p = FightStance();
    p.spine = new Vector3(20f, 0f, 0f);            // bend forward 20°
    p.lShoulder = new Vector3(-90f, 0f, -10f);     // left arm straight forward
    p.lElbow = new Vector3(-45f, 0f, 0f);          // elbow bent 45°
    p.lift = -0.10f;                               // crouch: lower the root with bent knees
    break;
```

One-shot gestures (a punch, a reach) are `ActionKind` overlays in `ApplyAction()`, not states — they ride on top of whatever pose is active and expire on their own.

## New AI behavior

1. Add the `AIState` value and the decision in `Decide()` in [CPUWrestlerAI.cs](../../Assets/Scripts/AI/CPUWrestlerAI.cs). Placement is priority: role-specific follow-ups first, then the `canAttack` gate, then stamina caution, then situational offense.
2. Add the `case` in `Act()` — call the same `WrestlerCombat` methods the player uses, `_memory.Note(...)`/`_memory.CanUse(...)` to avoid spamming, and `Rethink()` after acting.
3. Tune per difficulty via [AIDifficultyData](../../Assets/Scripts/AI/AIDifficultyData.cs) fields rather than hard-coded constants.
