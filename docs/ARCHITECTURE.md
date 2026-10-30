# Build Mania architecture

Build Mania is a Unity 6 + Meta XR (v207) game for Meta Quest and Meta VR Glasses. The tagline is *Build It. Play It.*: the player builds original brick models and then plays with them. This guide explains how the app is put together and where each future feature goes. The phase numbers and module names come from the project plan (v1.1).

## 1. The player journey: app steps

The app runs as a sequence of **steps** inside one scene. Passthrough and the build area stay put while the player moves between steps.

```
            ┌──────────── Garage ────────────┐
            │ pick a model · free build ·    │
            │ open a saved creation          │
            └──────┬───────────────┬─────────┘
                   ▼               ▼
            Guided Build ──► Free Build ──► Play
            (step by step,  (customise,    (Phase 3:
             saved per step) save creation)  drive it)
```

| Step | Class | What it does | Plan phase |
|---|---|---|---|
| Garage | `App/Steps/GarageStep.cs` | Lists catalog models (Build / Continue / built), Free build, and saved creations | 4 (grows into the garage and gallery) |
| Guided Build | `App/Steps/GuidedBuildStep.cs` | The tray shows only the current step's bricks in model colours, hint ghosts show where they go, only correct spots snap, and progress saves after every step | 2 |
| Free Build | `App/Steps/FreeBuildStep.cs` | Whole tray, any colour; customise a finished model; save a creation | 1 / MVP |
| Play | `App/Steps/PlayStep.cs` | Placeholder that keeps the build; Phase 3 replaces it | 3 |

`App/AppFlow.cs` creates the services and steps and moves between them with `GoTo<TStep>()`. Steps share an `AppContext`, which holds the build area, catalogs, saves, menu, playtest log and a `Session` that carries choices from step to step.

## 2. Modules (one assembly each)

Lower layers never reference higher ones.

| Folder (`Assets/BuildMania/Scripts/`) | Assembly | Responsibility (plan §6) |
|---|---|---|
| `BrickEngine` | `BuildMania.BrickEngine` | Grid standard (`BrickGrid`), brick definitions, connectors, placement and detach rules (`BrickAssembly`), snapping, undo/redo, scene view |
| `BuildingEngine` | `BuildMania.BuildingEngine` | Model format (`ModelDefinition`, JSON), instruction sequencing (`InstructionPlan`), validation (`GuidedBuild`), save state (`ISaveStore`) |
| `PlayEngine` | `BuildMania.PlayEngine` | Phase 3: finished build → playable vehicle and physics (empty for now) |
| `Content` | `BuildMania.Content` | Model catalog (`ModelCatalog`); later thumbnails, packs, prices, unlocks, Addressables |
| `Interaction` | `BuildMania.Interaction` | Device-independent building: `BrickHand` (grab/snap/detach/carry), `BuildArea` (tray, hints, swatches), touch buttons |
| `XR` | `BuildMania.XR` | Meta specifics: `MetaHandInput` (Touch controllers + hand tracking), passthrough fallback |
| `App` | `BuildMania.App` | Player journey: `AppFlow`, steps, in-world menu, playtest log |

Editor tooling lives in `Assets/BuildMania/Editor`, and tests in `Tests/EditMode` and `Tests/PlayMode`.

**Input rule:** every interaction must work with controllers *and* bare hands (grab = grip or pinch, buttons = touch). It must not depend on hover, because Meta VR Glasses have no controllers in the box and never fire hover. Keep content compact and central, since the glasses have a narrower field of view.

## 3. How to grow the app

**Add a step** (for example Tutorial, Store or Gallery):
1. Create `App/Steps/MyStep.cs : AppStep`.
2. In `Enter()`, set up the area: `ShowTray`/`HideTray`, `PlacementFilter`, `ShowHints`, `Menu.SetTitle`, `Menu.ShowButtons`. In `Exit()`, undo what you set up.
3. Register it in `AppFlow.Start()` and reach it with `Context.Flow.GoTo<MyStep>()`.

**Add a guided model** (plan §5):
1. Build it in Free Build. In the editor's Play mode (for example over Quest Link), use **Build Mania → Export Current Build as Model**. This writes `Data/Models/<id>.json` and adds it to `Data/ModelCatalog.asset`.
2. Edit the `step` numbers in the JSON to group bricks into instruction steps. Each brick starts as its own step.
3. `ContentTests` checks that every catalog model loads and builds completely.

**Add a brick type:**
1. Add a `BrickSpec` to `BrickEngine/StarterBrickSet.cs` with its size, connectors and shape. Use Blender for special shapes later.
2. Run **Build Mania → Generate Scenes**. It regenerates the meshes, prefabs, definitions and catalog, then add a tray slot in `Editor/BuildAreaGenerator.cs`.
3. Ids are stored in model files, so never rename a published id.

**Add a visual theme** (plan: realistic plastic and playful): create a second `BrickPalette` with the same colour order, then swap it on the `BuildArea` and `BrickAssemblyView`. Models store colour indices, not materials.

**Add model packs** (plan §7): give each pack its own `ModelCatalog` (and Addressables group). The Garage would then merge catalogs, with unlock and purchase state checked through the Meta Platform SDK before a pack's models are offered.

## 4. Roadmap by phase

| Phase | Plan target | Status | Where the work goes |
|---|---|---|---|
| 0 Setup | Unity + Quest pipeline | Unity 6, Meta SDK v207, URP for Quest, Git and LFS done. **Android Build Support not installed yet**, so no headset build | Project settings, `Editor/QuestRenderSetup.cs` |
| 1 Brick prototype | 5 bricks + wheel, snapping, undo | Done and tested; needs headset feel-testing | `BrickEngine`, `Interaction`, `XR` |
| 2 Guided car | Instructions load, steps complete and save | Structure done with the Starter Car. **Next:** Velocity GT reference sketch → build → export → group steps | `BuildingEngine`, `GuidedBuildStep`, `Data/Models` |
| 3 Play mode | Build → drive | Slot only (`PlayStep`) | `PlayEngine`: merge bricks into one mesh, wheel colliders, track scene content; `PlayStep` hands over the assembly |
| 4 Experience | Garage, themes, MR tabletop | MR passthrough, carry-anywhere build area and a basic Garage done | `GarageStep` (gallery, 3D previews), second `BrickPalette`, Scene/MRUK surface snapping in `XR` |
| 5 Test & release | Playtests, store package | Local playtest log in place (`persistentDataPath/BuildMania/playtest-log.jsonl`) | `App/PlaytestLog.cs`, privacy policy and store assets |

## 5. Generated content and checks

- **Build Mania → Generate Scenes** rebuilds bricks, materials, the `BuildArea` and `MenuButton` prefabs, the model catalog entry, `MRBuild` and `VRWorkshop`, and the Meta project settings. It is safe to re-run.
- **Build Mania → Import Text Resources** imports TextMesh Pro's fonts once; menus need them.
- Tests run in Unity's Test Runner, or headless with `-runTests -testPlatform EditMode` or `PlayMode`. Play-mode tests drive the real prefab with a simulated hand.
- Saves live in `Application.persistentDataPath/BuildMania/` (`progress/`, `creations/`) and stay on the device.
