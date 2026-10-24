# AGENTS.md: Instructions for AI Agents

This file is the operating manual for AI coding agents (Claude Code, Codex, Cursor, Copilot, etc.) working in this Unity repository. **`PROJECT.md` is the project reference** (what the game is, how it is built, live numbers, known issues, roadmap). This file assumes you will read the parts of `PROJECT.md` you need; it tells you *how* to work here.

Claude Code also loads the rule files in [`.claude/rules/`](.claude/rules/). Other agents should read them before writing C# or touching Unity assets. Where they differ from this file, **this file describes the team standard**.

---

## 1. Ground rules

These rules are not optional. Unity projects break quietly: a lost GUID, a stray scene save or a stale serialized value doesn't fail to compile, it just ships a broken game. Some of these mistakes are expensive to undo in a two-person team sharing one scene.

**You MUST:**

- **Keep `PROJECT.md` true.** If your change makes any statement in it false (a value, a class, a rule, an issue, a decision, the roadmap), update it **in the same commit**, run its checklist (§22), and bump its snapshot line. Say in your final report which sections you changed, or why none needed it.
- Read `PROJECT.md` §1–§4 (pitch, invariants, conventions, ownership) before your first change, then only the sections for the area you touch.
- **Ask whether you are talking to Ari or Han** before assigning, prioritising or estimating work. They own different systems (`PROJECT.md` §4).
- Respect the invariants I1–I10 (`PROJECT.md` §2). If a request would break one, say so and stop. Don't work around it.
- **Prefer the live Editor over raw files.** When an Editor is connected (Unity MCP, or `unity status` with the Pipeline package), inspect and change scenes, prefabs and assets through it. Hand-edit `.unity` / `.prefab` / `.asset` YAML only when no Editor is reachable, and say so ("no live Editor, editing the file directly").
- Work on a branch, created from an up-to-date `main` (§6). Never commit to `main`.
- Before every commit: the project compiles with **no new Console errors or warnings**, and `git status` shows only the files you meant to change (§7).
- Move or rename assets **in the Editor**, or move the `.meta` file with them, so the GUID survives.
- Build new things as **prefabs**. Keep `SampleScene.unity` edits minimal, list every scene change in your report, and tell Han before touching the scene.
- State assumptions and uncertainties. Ask before acting when the request is ambiguous. Label every balance claim as arithmetic, a bot model, or an observed run (there is no playtest data yet).

**You MUST NOT, unless the user explicitly asks for that specific action in this conversation:**

- Push, open PRs, merge into `main`, delete branches, rebase shared branches or force-push.
- Change the Unity version (`ProjectSettings/ProjectVersion.txt`), add/remove/upgrade packages (`Packages/manifest.json`, `packages-lock.json`), or change Project Settings (Player, Quality, Input, Tags & Layers, Physics 2D, **Script Execution Order**, Editor settings).
- Change values in `Assets/Settings/Configs/GameConfig.asset`. Ari owns it: propose the diff (field, old → new, why) and let Ari apply or approve it. Adding a *new* field in code with a default equal to the intended value is fine.
- Create or edit card **assets** (`Assets/Resources/CardSO/*`: names, text, art, rarity, weight). Han owns them. Write the code and list the assets Han needs.
- Rename a `CardSO` asset, or rename/reorder an enum member. Saves and serialized ints depend on them (append only).
- Hand-edit GUIDs or `fileID`s, delete or regenerate `.meta` files, or delete scripts/assets that prefabs or the scene reference ("Missing Script" breaks silently).
- Save the scene with incidental changes (an open Laptop/Billiards panel, moved objects, toggled actives). Revert them before saving.
- Change Billiards physics values or table JSON without re-running **Verify Presets** and showing the result (stored solutions depend on them).
- Disable, skip or weaken a self-test, validator, `CanBePawned` guard or `ValidateBilliards` check to make something pass.
- Touch `.gitattributes`, `.gitignore`, Git LFS tracking, or run `git lfs migrate`.
- Commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `Build*/`, `*.csproj`/`*.sln` or other generated files.
- Change an invariant (I1–I10) or a decision marked LOCKED/final in `PROJECT.md` §18.
- Make a release build or upload a build.

Read-only actions are always fine: `git status/diff/log/show`, reading files, Unity MCP inspection (find objects, read components, read the Console), `unity --version`, `unity editors`, `unity projects info`.

## 2. Machine setup (do this first)

The project is developed on **Windows** (repo at `A:\project game\OverTimer-Extended`). Commands below work in Git Bash or PowerShell.

1. **Unity 6000.3.14f1** with the **WebGL Build Support** module. Unity Hub, or the Unity CLI:
   ```bash
   unity editors --installed --format json          # is 6000.3.14f1 there?
   unity install 6000.3.14f1 --module webgl --yes --accept-eula
   unity projects info . --format json               # editorVersion must be 6000.3.14f1
   ```
2. **Git LFS** (images, audio, fonts, DLLs are LFS-tracked):
   ```bash
   git lfs install
   git lfs pull
   ```
   If a texture shows as a ~130-byte text file, LFS didn't pull.
3. **UnityYAMLMerge driver.** `.gitattributes` sends `*.unity`, `*.prefab`, `*.asset`, `*.anim`, `*.controller`, `*.playable` to `merge=unityyamlmerge`, but **git needs the driver configured per clone** or it silently falls back to a plain text merge (which corrupts scenes). Adjust the path to your Editor install:
   ```bash
   git config merge.unityyamlmerge.name "Unity SmartMerge"
   git config merge.unityyamlmerge.driver "\"C:/Program Files/Unity/Hub/Editor/6000.3.14f1/Editor/Data/Tools/UnityYAMLMerge.exe\" merge -p %O %B %A %A"
   git config merge.unityyamlmerge.recursive binary
   ```
   Verify with `git config --get merge.unityyamlmerge.driver`.
4. **Unity MCP** (`com.coplaydev.unity-mcp` v10 is already in the manifest). Open the project, open the MCP for Unity window from the Window menu, and connect your agent client. `.claude/settings.local.json` pre-allows `find_gameobjects`, `manage_scene`, `manage_components` and `refresh_unity`.
5. Optional: the **Unity CLI** (`unity`) for editor installs, `unity test` and `unity build`. Driving the Editor through `unity command` needs the `com.unity.pipeline` package, which this project does **not** have; use Unity MCP instead.
6. Open the project once in the Editor and wait for the import to finish before running anything.

## 3. The Editor connection

You usually **cannot open or click the Unity Editor yourself**. When a tool says no Editor is connected:

1. Check: the MCP tool list responds, or `unity status`.
2. If not, ask the user to open the project in Unity 6000.3.14f1 and start/connect the MCP bridge.
3. Wait for them to confirm, then retry. Don't fall back to YAML edits for scene/prefab work just because the Editor is closed; ask first.

Unity also pauses an unfocused Editor in some setups. For Play-mode checks, set `Application.runInBackground = true` for the session, or ask the user to keep the Editor focused.

## 4. How this project works (read before writing code)

There is **one scene** (`Assets/Design/Scenes/SampleScene.unity`) and no `DontDestroyOnLoad`. **Restart = reload that scene.** Everything else follows from that:

| Piece | Lives in | Consequence |
|---|---|---|
| Services (`GameManager`, `ClockService`, `MoneyService`, `RngService`, …) | singletons on `MANAGERS/*` in the scene, `Instance` set in `Awake` | read other services' `.Instance` in **`Start`**, never in `Awake`/`OnEnable` |
| Tuning values | `GameConfig.asset` (read via `GameManager.Instance.GameConfig`) | no literals in logic (I10); the **asset is the truth**, C# defaults are stale for ~20 fields |
| Panels (Laptop, apps, card panels, Tax…) | prefab instances saved **inactive** under `Canvas` | their `Awake` doesn't run until first opened; use the static "pending state" pattern to reach them |
| Mutable statics | anywhere | survive a scene reload: **reset them on `sceneLoaded`** (pattern in `BilliardsPendingEffects`, `MiningApp`, `ScratchTicket`) |
| Pooled objects (`CardItem` ×7, tickets ×2 per tier) | the scene | never `Instantiate`/`Destroy`; pawnables deactivate in `OnPawned` |
| Randomness | `RngService.Instance.Random` (seedable `System.Random`) | never `UnityEngine.Random` |
| Desk input | `InputManager` raycasts `Physics2D.OverlapPoint`; panels block it with an `InputBlocker` | uGUI buttons are **not** gated by `IsInputEnabled` |
| Clock | `ClockService.AddSeconds/Spend` only; Meter is the only money → time path | `TickMultiplier` is a shared scalar today (known issue) |
| Asset identity | GUIDs in `.meta` files; card collection saved by `CardSO.name` | renames happen in the Editor; card asset names are permanent |

Code style rules and the reasons behind them: `PROJECT.md` §3 and `.claude/rules/unity-csharp.md`.

## 5. Repository map

```
Assets/Code/Scripts/
  Config/              GameConfig, BlockConfig, TicketSymbolConfig (in TicketSymbolPool.cs)
  Services/            singletons: GameManager, ClockService, MoneyService, Meter, RngService, ...
  Input/               InputManager glue: PointerReceiver, DeskObject, InputGate, InputBlocker
  Hint/                HintId, HintCatalog, TutorialPanel
  Annoyance/           Phone, ITRSService, Tax, TaxNotice, AnnoyanceManager, Cat/
  Desk/Items/          Cards/ (+CardData/, WinConditionCard/), Pawn/, Tasks/, Tickets/
  Desk/Laptop/         LaptopController + Mining/, Blackjack/, Billiards/ (+View/)
  Cutscene/, CheatMenu/, UI&Camera/ (+Summary/)
  Editor/Billiards/    authoring, solver, verifier, self-tests, balance report (Editor-only)
Assets/Design/Scenes/SampleScene.unity   the only scene
Assets/Design/Prefab/                    items, cards, panels
Assets/Settings/Configs/                 GameConfig.asset (Ari), BlockConfig, symbol configs
Assets/Resources/CardSO/                 card assets (Han); loaded by folder
Assets/Resources/BilliardsPresets/       Billiards tables (JSON, written by the Table Editor)
Assets/Plugins/Demigiant/                DOTween
Packages/manifest.json                   packages (don't change unasked)
ProjectSettings/                         don't change unasked
PROJECT.md  AGENTS.md  CLAUDE.md         reference, this manual, Claude entry point
.claude/rules/                           C# and Unity asset rules (path-scoped)
```

## 6. Recipes

### Change gameplay code

```bash
git switch main && git pull
git switch -c <owner>/<short-topic>          # e.g. ari/egg-timer-cooldown
# edit Assets/Code/Scripts/...
```

Then in the Editor: let it recompile, read the Console, run the relevant self-tests and a Play-mode smoke check (§7). Commit with the `PROJECT.md` update. Push/PR only when the user asks.

### Tune a number

1. Find the field in `PROJECT.md` §10. If it's already in `GameConfig`, **propose the change to Ari** (field, old → new, the arithmetic behind it, which throughput it moves).
2. If the value is a literal in code (I10 violation), move it to `GameConfig`: add the field under the right `[Header]`, default = current live value, read it in `Start` or at use time, then ask the user to select `GameConfig.asset` in the Inspector so the field serializes.
3. Update `PROJECT.md` §10 (and §14 if a rate changes). Also update the C# default to match the asset.

### Add a card effect

1. New class `XxxCardSO : EffectCardSO` in `Desk/Items/Cards/CardData/` with `[CreateAssetMenu]`. Its own `[SerializeField] duration` if timed.
2. `OnPlaced(card)` grants the effect (owned by `card`); `OnRemoved(card)` **always** withdraws it; consuming it calls `card.EndEffect()`. Timed: `card.ScheduleAutoRemove(duration)`. Queue effects for systems that may be asleep through a static pending list reset on `sceneLoaded`.
3. Debuffs set `_isDebuff` on the asset; they must close a door, never drain the clock (I7).
4. Test with a throwaway in-memory asset (`ScriptableObject.CreateInstance`) placed via the cheat menu or `CardPackRevealPanel.TryPlaceCard`. Pawn it mid-effect and confirm the effect ends.
5. Hand Han the asset spec (class, suggested name, rarity, weight, pawn value, description). Update `PROJECT.md` §12.

### Add a laptop app

Subclass `LaptopApp` (set `appName`), add the app prefab under the Laptop panel, add a `DesktopIconView` with the **same** `appName` string, add the app to `LaptopController.apps`, put an `InputBlocker` on it if it needs one, and handle `OnAppClosed` (what is forfeited when the player leaves). Keep money in `MoneyService`, clock in `ClockService`. Scene change → tell Han.

### Add a desk item (or make one pawnable)

Make it a prefab on layer **3 `DeskObject`** with a collider and a `PointerReceiver`. Tasks derive from `TaskBase`. Pawnable items implement `IPawnable` (`PawnValue` from `GameConfig`, `CanBePawned`, `OnPawned` = deactivate). Rule: an item is pawnable only if the run stays winnable without it (or it can be bought back). The shop plan replaces layer-based pawnability with an item catalog; check `PROJECT.md` §17 before building.

### Add or change a Billiards table

Use **Tools/OverTimer/Billiards/Table Editor** (record shots or Auto-solve; Export refuses unless solved and verified). Then **Verify Presets** and **Run Self-Tests**. Never hand-edit solutions in the JSON. After any physics constant change, re-verify every table and re-record knife-edge ones.

### Rename or move a script or asset

Do it in the Unity Project window (keeps the `.meta`/GUID). For a serialized field rename, add `[FormerlySerializedAs("old")]`. For a file rename outside the Editor, rename the `.meta` too and make sure the class name still matches the file name. Grep for string references (`appName`, `Resources.Load` paths, `nameof`).

### Edit the scene safely

1. Tell Han what you will change.
2. Close all panels, return the camera to its start, exit Play mode.
3. Make the change through the Editor (or MCP). Prefer changing a prefab over adding scene overrides.
4. Before saving, check the scene diff is only your change (`git diff --stat Assets/Design/Scenes/`).
5. List the scene changes in your report.

### Run tests

- Billiards: **Tools/OverTimer/Billiards/Run Self-Tests** (105 checks) and **Verify Presets** (every table, 30 jittered trials per shot). Run them through the MCP menu-item tool and read the Console summary.
- New features: add pure-C# checks as an Editor menu item `Tools/OverTimer/<Feature>/Run Self-Tests` (the Phone plan adds `PhoneShop`). Build test configs with `ScriptableObject.CreateInstance<GameConfig>()` and set every field you depend on.
- Unity Test Framework tests live in `Assets/Tests/EditMode` and `Assets/Tests/PlayMode` (asmdefs `OverTimer.Tests.*`, runtime code is the `OverTimer` assembly). Run them from Window > General > Test Runner or with `unity test --mode EditMode` / `PlayMode`. The menu self-tests above are separate and are not run by `unity test`. Tests must not pin balance numbers; read them from the assets or assert invariants.

### Make a build (only when asked)

WebGL, custom 1080p template, through the Editor's Build Profiles window. There is no build script (`-executeMethod`) yet. Never upload a build.

## 7. Verifying your work

Your change is not done until all of these hold:

1. The project compiles; the Console shows **no new errors or warnings** after a recompile and after a Play-mode run.
2. Relevant self-tests pass (Billiards: all of them, and Verify Presets if physics, presets or rules were touched).
3. **Play-mode smoke check:** start a run, use the feature, trigger a win and a loss (cheat menu), press Restart, and confirm the feature works again in the second run (statics reset, no leftover state).
4. `git status` / `git diff --stat` show only intended files. No stray `.unity`/`.prefab` churn, no `Library/`, every new asset has its `.meta`, no `.meta` without its asset.
5. No new `UnityEngine.Random`, `?.` on Unity objects, `.Instance` in `Awake`/`OnEnable`, unreset statics, or tuning literals (see `.claude/rules/`).
6. `PROJECT.md` updated (checklist §22), and anything you verified from its §20 recorded.

## 8. Workflow

- **Branches:** one per feature or plan batch, from `main` after the previous one merged. Never stack feature branches. Merge `main` into your branch before finishing. Names seen: `ari/<topic>`, `Han/<Topic>`; the Phone plan writes `Ari/<Batch>` (confirm the case with Ari).
- **Commits:** small, focused, message says what and why. Styles in use: `Phase N: <summary>` (plan work), `landmine: <item>` (defect fixes), `chore: <summary>` (cleanup).
- **Plan checkpoints:** a plan phase ends with a report, then you wait for Ari's "approved". Report: done · files changed (prefab vs scene) · tests and results · deviations and why · problems found but not fixed (add to `PROJECT.md` §15) · defaults assumed · evidence (Console, screenshots) · questions · `PROJECT.md` sections updated.
- Don't fix unrelated bugs inside a feature change. Note them.

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `NullReferenceException` on `X.Instance` at startup | Read in `Awake`/`OnEnable`, or the object holding `X` is inactive. Move the read to `Start`; for sleeping panels use a pending static |
| Effect "does nothing" on a laptop app | The app's GameObject has never been opened, so `Awake` hasn't run. Use the pending-state pattern or go through `DesktopIconView`/`LaptopController` |
| `MissingReferenceException` after Restart or on stopping Play | A static or event still holds a destroyed object, or `?.` hid Unity's fake null. Reset statics on `sceneLoaded`, unsubscribe in `OnDestroy`, use `!= null` |
| State from the last run leaks into the next | A mutable static isn't reset on scene load (or Enter Play Mode Options skip domain reload) |
| "Missing (Mono Script)" on a prefab/scene object | Script renamed/moved without its `.meta`, or class name ≠ file name. Restore the `.meta` from git |
| A config value "won't change" in play | You edited the C# default; the asset holds the live value. Edit `GameConfig.asset` (Ari) |
| New `GameConfig` field reads 0 / default | The asset hasn't serialized it yet: select `GameConfig.asset` in the Inspector and save |
| Scene/prefab merge conflict full of `fileID` noise | UnityYAMLMerge driver not configured (§2.3). Don't hand-merge; stop and ask the user |
| Images/audio are tiny text files | Git LFS not pulled: `git lfs install && git lfs pull` |
| "Billiards unavailable" | `GameConfig.ValidateBilliards` failed after a physics change (pocket vs ball radius). Read the Console warning; revert or re-tune and re-verify |
| A table's stored solution misses | Physics changed since it was recorded, or runtime determinism differs (Editor vs WebGL). Re-run Verify Presets in the Editor |
| Clock speed wrong while a card is active | Known: `TickMultiplier` is last-writer-wins (Phone vs cards). See `PROJECT.md` §15 |
| Desk clicks ignored | `InputGate` blocked by an active `InputBlocker`, the pointer is over a UI raycast target, or the Cat is blocking that object |
| Agent tools say no Editor connected | Ask the user to open the project and connect MCP (§3) |
