# Part Manifest

Each wrestler is a paper-doll rig of separate sprite parts. Generate one
transparent PNG per slot, authored facing screen-right, at roughly 100 pixels
per world unit. Set each sprite's pivot in the Unity Sprite Editor to the joint
listed below so the parts assemble correctly. Save files to
`Assets/Resources/Parts/<character-id>/<slot>.png` (character-id is the portrait
slug without the `tas-` prefix, for example `zeak-gallent`).

| Slot file | Joint pivot | Target size (px) | Notes |
|---|---|---|---|
| `pelvis` | center | 42 x 22 | Hips, waistband. |
| `torso` | bottom-center | 50 x 55 | Chest and abdomen, neck stub at top. |
| `head` | bottom-center (neck) | 34 x 34 | Face, neutral expression, no headpiece. |
| `headpiece` | bottom-center | 40 x 30 | Hood, mask, or hat. Empty/transparent if none. |
| `upper-arm-near` | top-center (shoulder) | 17 x 35 | Camera-side upper arm. |
| `forearm-near` | top-center (elbow) | 15 x 31 | Camera-side forearm. |
| `hand-near` | top-center (wrist) | 17 x 17 | Camera-side hand, relaxed fist. |
| `upper-arm-far` | top-center (shoulder) | 16 x 34 | Far-side upper arm, slightly darker. |
| `forearm-far` | top-center (elbow) | 14 x 30 | Far-side forearm. |
| `hand-far` | top-center (wrist) | 16 x 16 | Far-side hand. |
| `thigh-near` | top-center (hip) | 19 x 35 | Camera-side thigh. |
| `shin-near` | top-center (knee) | 17 x 33 | Camera-side shin. |
| `foot-near` | top-back (ankle) | 23 x 13 | Camera-side boot, toe pointing right. |
| `thigh-far` | top-center (hip) | 18 x 34 | Far-side thigh, slightly darker. |
| `shin-far` | top-center (knee) | 16 x 32 | Far-side shin. |
| `foot-far` | top-back (ankle) | 22 x 12 | Far-side boot. |

Joint guidance: a "top-center" pivot means the joint where this part attaches
to its parent sits at the top-middle of the image. Leave a few transparent
pixels of overlap margin around each joint so parts meet cleanly when rotated.
Far-side limbs read about 15 percent darker than near-side to suggest depth.
