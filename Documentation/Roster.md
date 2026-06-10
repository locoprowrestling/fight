# Roster

Portraits come from `players-web/` — only files prefixed `tas-` are roster assets, and the prefix is preserved as the roster id. Display names are explicit mappings; never inferred from filenames.

| Roster ID | Display Name | Source Image |
|---|---|---|
| tas-anuka-gutierrez | Anuka Gutierrez | tas-anuka-gutierrez.png |
| tas-avalon | Michael Avalon | tas-avalon.png |
| tas-carter-cash | Carter Cash | tas-carter-cash.png |
| tas-codah | Codah Alexander | tas-codah.png |
| tas-cody-devine | Cody Devine | tas-cody-devine.png |
| tas-dean-mercer | Dean Mercer | tas-dean-mercer.png |
| tas-erza | Erza Menagerie Tinker | tas-erza.png |
| tas-franky-gonzales | Franky Gonzales | tas-franky-gonzales.png |
| tas-hussy | Hussy Steele | tas-hussy.png |
| tas-johnny-crash | Johnny Crash | tas-johnny-crash.png |
| tas-jt-staten | JT Staten | tas-jt-staten.png |
| tas-major-glory | Major Glory | tas-major-glory.png |
| tas-morgana-lavey | Morgana Lavey | tas-morgana-lavey.png |
| tas-nicky-hyde | Nicky Hyde | tas-nicky-hyde.png |
| tas-vigilante-oai | The Vigilante | tas-vigilante-oai.png |
| tas-zeak-gallent | Zeak Gallent | tas-zeak-gallent.png |

Default match: player `tas-zeak-gallent` (Zeak Gallent) vs CPU `tas-jt-staten` (JT Staten). These are inspector fields on `GameBootstrap` / `MatchManager`, not hard-coded wrestlers — any of the 16 entries can fill either slot (F2 debug selector in Play mode).

## Importing portraits

Automatic (preferred): **Tools > LoCo Fight Game > Import TAS Roster Portraits**
1. Reads `players-web/` (project-relative, falling back to `/Users/gecko/locoprowrestling/fightgame/players-web/`).
2. Copies all `tas-*.png` into `Assets/Art/RosterPortraits/` with original filenames.
3. Marks them as Sprites and refreshes the AssetDatabase.
4. Assigns sprites to the matching `RosterEntry` assets.
5. Warns about: missing expected files, extra unmapped `tas-` files, duplicate roster ids, and missing sprites.

Manual copy:

```bash
mkdir -p Assets/Art/RosterPortraits
cp /Users/gecko/locoprowrestling/fightgame/players-web/tas-*.png Assets/Art/RosterPortraits/
```

(then still run the importer once so textures become Sprites and entries link up).

A missing portrait never blocks the match: the HUD shows the wrestler's placeholder color and logs a warning.

Each `RosterEntry` carries: rosterId, displayName, sourceImageFileName, portraitSprite, wrestlerDefinition (stats, moveset, special, optional dodge ability, passive traits, placeholder color), and notes.
