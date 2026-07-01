# Example: Safe Gameplay Script Change

This example shows the preferred workflow; it is not a record of a specific code
change.

## Scenario

Change grapple stamina cost without breaking CPU behavior, reversals, specials,
or HUD feedback.

## Inspect

1. Locate the stamina calculation and all callers.
2. Inspect `WrestlerCombat`, `CombatResolver`, reversal logic, AI action gates,
   and relevant ScriptableObject data.
3. Check whether the value is authored data or hard-coded runtime policy.
4. Check for dirty user changes in affected files.

## Decide

Keep authored tuning in data when the cost varies by move or wrestler. Keep a
shared formula in one runtime authority when it is universal.

## Implement

Make the narrowest edit that preserves player and CPU use of the shared combat
API. Avoid adding a character-specific branch to general combat code.

## Verify

1. Wait for Unity compilation.
2. Read console errors.
3. Run focused EditMode formula tests if available.
4. Test player quick and power grapples.
5. Observe CPU grapple choices at low and high stamina.
6. Test reversal stamina costs and failed lift behavior.
7. Confirm HUD stamina matches runtime state.
8. Run the relevant manual checklist section.

## Record

Add a decision only if ownership or data placement changed. Add a learning if a
non-obvious dependency or failure was found.
