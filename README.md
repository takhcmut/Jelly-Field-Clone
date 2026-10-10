# Jelly Field Clone MVP

This project contains a provisional, playable three-level Unity MVP inspired by Jelly Field. It uses procedural cube regions and no imported gameplay assets.

## Run it

1. Open the project in Unity `6000.3.22f1`.
2. Open `Assets/Scenes/SampleScene.unity` and press Play, or use `Tools > JellyField > Build Demo Scene` to create and open `Assets/Scenes/MainScene.unity`.
3. Drag a spawn jelly into any empty valid board cell. Mouse input works in the Editor; touch input works on Android.

The runtime bootstraps itself into an empty scene, so the sample scene is immediately playable. The editor command creates a minimal reproducible scene with a camera and game root without duplicating objects.

Both input backends are enabled because the MVP uses Unity's built-in mouse/touch API for the shared drag path. Unity may request an Editor restart after changing this setting.

## Scene setup

There are two supported setups:

1. Fastest: open `Assets/Scenes/SampleScene.unity` and press Play. The runtime bootstrap creates the camera, board, spawn area, and UI automatically.
2. Recommended: choose `Tools > JellyField > Build Demo Scene`. This creates the level assets and `Assets/Scenes/MainScene.unity` with a configured `GameController`, `Main Camera`, `Board`, `Grid`, and `Spawn Area`. Open that scene and press Play.

You can also choose `Tools > JellyField > Create Sample Level Assets` when you only want to create or refresh the editable level assets.

In the recommended scene, select `GameController` in the Hierarchy. Its Inspector has:

- `Level Assets`: the ordered level list. The order controls Next Level.
- `Use Runtime Fallback Samples`: leave enabled while prototyping; disable it once all level assets are assigned.
- `Game Camera`: the scene camera.
- `Board Root`: parent for board block views.
- `Grid Root`: parent for visible valid-cell backgrounds and borders.
- `Spawn Root`: parent for spawn block views.
- `Jelly Region Prefab`: optional visual prefab for one jelly region.

The runtime creates only the Canvas UI because it is gameplay-owned and does not need scene references. Do not add a second `GameController`; the bootstrap prevents duplicates.

The grid is visual only. Every valid coordinate gets a bordered cell; empty cells use a lighter fill, while occupied cells use a darker fill. Missing coordinates are not created, so irregular board layouts remain visibly and logically empty outside the valid-cell set.

## Using an external jelly prefab

The assigned prefab must represent one region, not a complete two/four-region composite. The runtime creates one prefab instance for each logical region and positions/scales it from the normalized `Rect` in `JellyRegionData`.

Recommended prefab setup:

1. Import the model or prefab under `Assets/Art/Jelly`.
2. Make the root pivot centered on the visual mesh.
3. Make one prefab instance approximately one Unity unit wide and high.
4. Remove gameplay scripts and unnecessary colliders from the visual prefab. Dragging uses screen-to-board coordinates, not physics.
5. Assign the prefab to `GameController > Jelly Region Prefab`.
6. Press Play and verify that one, two, three, and four-region blocks keep the expected cell layout.

The current view assigns a cached color material to every renderer under the prefab. This is intentional for the sample color system; if the imported asset must keep its own textured material, change that assignment in `JellyBlockView.Build()` to a `MaterialPropertyBlock` tint or remove the color override.

The existing effects are transform-based and therefore work with the external mesh automatically:

- `SetSelected(true)`: selected spawn jiggle.
- `SetDragging(true)`: squash/stretch while dragging.
- `PlayPop()`: short squash/pop during a cascade.

For richer bounce, wobble, or shatter effects, add an animation component to the external prefab and call it from these three methods in `JellyBlockView`. Keep the logical removal in `JellyFieldMatchResolver` and `JellyFieldBoard`; visual animation should never decide whether a region matches or is destroyed.

## Code layout

- `JellyFieldTypes.cs`: enums and serializable logical data (`LevelData`, `JellyBlockData`, regions, objectives).
- `JellyFieldSamples.cs`: provisional three-level content. Edit this file when changing coordinates, starting blocks, objectives, or spawn sequences.
- `Runtime/Data/GameData.cs`: logical data types and geometry definitions.
- `Runtime/Data/LevelDefinitionAsset.cs`: editable `ScriptableObject` wrapper for one level.
- `Runtime/Data/SampleLevels.cs`: provisional fallback level factory.
- `Runtime/Board/BoardState.cs`: valid cells, occupancy, placement, and region removal.
- `Runtime/Board/MatchResolver.cs`: deterministic boundary matching and unique removal collection.
- `Runtime/Gameplay/GameplayServices.cs`: spawn and objective services.
- `Runtime/Gameplay/GameController.cs`: composition root, level lifecycle, input, camera framing, and cascade orchestration.
- `Runtime/Presentation/GameHud.cs`: Canvas/UI construction and presentation updates.
- `Runtime/Presentation/BoardGridView.cs`: visible valid-cell grid.
- `Runtime/Presentation/JellyBlockView.cs`: procedural or prefab-based rendering and jiggle/pop animations.
- `Editor/SceneBuilder.cs`: one-click scene and level asset creation menu.

The important rule is that matching lives in `MatchResolver` and reads `JellyRegionData.rect`; `JellyBlockView` only renders that same geometry. Do not implement matching by checking renderer or collider overlap.

To edit a level, select its asset under `Assets/Data/JellyField/Levels` and edit `Level`, `Valid Cells`, `Initial Cells`, `Objectives`, `Spawn Slot Count`, `Initial Spawns`, and `Spawn Sequence` in the Inspector. To add a new level asset, use `Create > Jelly Field > Level Data`, fill the fields, then add the asset to `GameController.Level Assets` in the desired order.

`Runtime/Data/SampleLevels.cs` is only provisional fallback/content generation. Once assets are assigned to the scene, gameplay no longer depends on changing that code file.

## Gameplay assumptions

The three level layouts, colors, objectives, and spawn sequences are clearly provisional sample data. They contain exactly 11, 12, and 13 valid cells and can be replaced in `SampleLevels` without changing the matching algorithm.

Matching is logical: only same-colored regions in different adjacent cells match, and a match requires positive shared boundary length. All matches in a wave are collected before removal. Recomposition and repeated waves continue until the board is stable. Objective counters count individual destroyed regions, not cells.

The current MVP uses a fixed orthographic 3D board, procedural materials, finite selection/drag deformation, simultaneous cascade resolution, retry, level progression, and final completion state. No ads, network access, shops, or proprietary assets are included.

## Android build

Set the Android platform in Build Settings, add `Assets/Scenes/MainScene.unity` (or `SampleScene.unity`) to Scenes In Build, and use portrait orientation. The project settings are already configured for portrait by default. Select an Android SDK/NDK installation in Unity Hub, then use `Build` or `Build And Run`.

An Android build was not run from this workspace; validate it in the installed Unity Editor with the target SDK configured.
