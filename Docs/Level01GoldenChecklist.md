# Level01 Golden Checklist

Use this before duplicating `Level01` into a new level.

## Scene Wiring

- `ProjectSettings/EditorBuildSettings.asset` has only `Assets/Scenes/Level01.unity` enabled.
- `Game Systems > Interaction Flow Manager` has these references assigned:
  - `Board Manager`
  - `Player Mover`
  - `Player Object`
  - `Start Tile`
  - `Start Door`
  - `Destination Door`
  - `Result Flash UI`
- `Control UI` has its panel references, sounds, icons, pagination buttons, and `Interaction Flow Manager` assigned.
- `Interaction Counter` has its text, fill image, and `Interaction Flow Manager` assigned.
- No `BoardObject` IDs are duplicated in the scene.

## Playthrough

- On Play, the start door opens before the player crosses it.
- The player walks from the start tile to `5 , 2`.
- The start door closes after the intro.
- No object actions can be triggered during the intro.
- Selecting an object opens its action panel and highlights the selected object.
- Box/object movement obeys board occupancy and cannot pass through the player, blockers, closed doors, or out-of-bounds tiles.
- Opening the destination door allows the player to path to the destination tile.
- Closed destination door blocks the destination path.
- Success triggers after the player reaches the destination tile.
- Failure triggers when the action limit is reached away from the destination tile.
- Success and failure both show the flash and play their result sound.
- Pressing `R` reloads the level and replays the intro cleanly.

## Console

- Enter Play mode with no missing-reference warnings.
- Complete a success route with no warnings or errors.
- Complete a failure route with no warnings or errors.
- Press `R` and complete another route with no warnings or errors.

## Prefab Boundaries

- Reusable visual/audio/script defaults are applied to prefabs.
- Level-specific values stay in `Level01`, especially:
  - `maxInteractionCount`
  - start tile
  - start door
  - destination door
  - destination/goal tile
  - placed blocker/furniture layout
