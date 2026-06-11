# Examples & Postmortems

Short, real, dated. Newest first within each section.

## Postmortem: the endless lockup loop

_2026-06-10 — fixed in
[CPUWrestlerAI.cs](../../Assets/Scripts/AI/CPUWrestlerAI.cs)_

**Symptom:** match soft-locked when the CPU grappled; console showed
`[Grapple] JT Staten locks up with Zeak Gallent` repeating every ~2 s, forever.

**Root cause:** `Decide()` early-returned on `!Profile.canAttack` before
reaching the `InGrappleLockAsAttacker` branch. The `GrappleLock` profile sets
`canAttack: false` (correct — you can't strike mid-lock), so the CPU attacker
went to `Recover`, the 1.8 s lock timed out, both wrestlers reset to Idle, and
the AI immediately re-grappled. The bug was _ordering_, not the flag: capability
gates describe the state for everyone, but the attacker role had a legal
follow-up action the gate hid.

**Fix:** check the role-specific branch first; in `Act()`, fall back quick↔power
grapple and `ReleaseGrapple()` if neither executes, so a lock can never dangle.

**Diagnosis clue worth remembering:** the ~2 s log cadence matched lock timeout
(1.8 s) + AI reaction delay exactly — uniform `[Tag]` logging turned a "game
frozen" report into a readable timing signature.

**Prevention:** the "role-specific before capability gates" and "three things
every locking state needs" rules in [BestPractices.md](BestPractices.md#states),
plus a regression line in [TestingChecklist.md](../TestingChecklist.md).

## Worked example: the articulated humanoid rig

_2026-06-10 —
[WrestlerView.cs](../../Assets/Scripts/Wrestlers/WrestlerView.cs) +
[PlaceholderAnimationDriver.cs](../../Assets/Scripts/Animation/PlaceholderAnimationDriver.cs)_

The placeholder body is a joint hierarchy of empty pivots (pelvis → spine →
neck; shoulders → elbows; hips → knees) with primitive meshes offset by half
their length under each pivot, so rotating a pivot bends the body at the joint.
The animation driver computes a full-body `BodyPose` every frame (state base +
walk cycle + one-shot overlay) and eases joints toward it with
`1 - exp(-14 · dt)`.

### Joint sign conventions

Limbs hang along local −Y with pivots axis-aligned to the body, so consistent
rules apply (left/right mirror on Z):

| Rotation               | Effect                                                               |
| ---------------------- | -------------------------------------------------------------------- |
| `shoulder.x = -90`     | arm straight forward; `-180` straight up; `+40` behind the back      |
| `elbow.x` negative     | elbow flexes (forearm toward chest); `0` straight                    |
| `hip.x` negative       | leg swings forward; positive = backward                              |
| `knee.x` positive      | knee flexes (shin/boot swing back); never negative                   |
| `shoulder.z`           | arm out to the side: negative on the left arm, positive on the right |
| `spine.y` / `pelvis.x` | torso twist / whole-upper-body lean                                  |

### The lying-pose trick

`visualRoot` pivots at the feet, so a ±88° root tilt alone would sprawl the body
~1.8 m off its collider. `LyingPose()` compensates with `shift` (slide the root
along z so the body re-centers over the CharacterController — `+0.85` on the
back, `-0.85` face-down) and `lift` (`+0.16` so the torso rests on the mat
instead of sinking in). Gameplay distances are unaffected: they use the wrestler
root transform, which never moves.

### Scaling rule

Weight class scales mesh x/z per part (`BulkFor`) and the root _uniformly_
(`HeightFor`). A non-uniform root scale shears rotated child joints — if a bent
elbow ever looks smeared, look for a non-uniform scale upstream.

## Offline compile check

No CLI build exists, and Unity batch mode is unsafe while the editor is open.
This compiles every non-Editor script against Unity 6000.4.10f1's reference
assemblies in ~2 s (exit 0 + warnings only = good):

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/Resources/Scripting
ROSLYN=$(ls -d /usr/local/share/dotnet/sdk/*/Roslyn/bincore/csc.dll | tail -1)
RSP=/tmp/loco_check.rsp; : > $RSP
echo "-nologo -nostdlib+ -target:library -out:/tmp/loco_check.dll -langversion:9.0 -nowarn:0169,0414" >> $RSP
echo "-r:$UNITY/NetStandard/ref/2.1.0/netstandard.dll" >> $RSP
for d in $UNITY/Managed/UnityEngine/*.dll; do echo "-r:$d" >> $RSP; done
echo "-r:Library/ScriptAssemblies/UnityEngine.UI.dll" >> $RSP
find Assets/Scripts -name "*.cs" -not -path "*/Editor/*" >> $RSP
dotnet "$ROSLYN" @$RSP
```

Gotcha that motivated the response file: passing a multi-line file list as a
shell variable mangles it into one giant "filename" (CS1504).

### Editor + test variant

To also type-check `Assets/Scripts/Editor/` (asset builder, validators, NUnit
edit-mode tests), extend the same response file with the netfx mscorlib shim and
the nunit package DLL, and compile **all** of `Assets/Scripts`:

```bash
echo "-r:$UNITY/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll" >> $RSP
echo "-r:$(ls -d Library/PackageCache/com.unity.ext.nunit@*/net40/unity-custom/nunit.framework.dll | tail -1)" >> $RSP
find Assets/Scripts -name "*.cs" >> $RSP
```

Two gotchas: do **not** also reference `$UNITY/Managed/UnityEditor.dll` — the
`Managed/UnityEngine/*.dll` glob already pulls in `UnityEditor.CoreModule` and
the duplicate types collide (CS0433); and the net40 nunit DLL needs that
mscorlib shim or every `[Test]` attribute fails with CS0012.
