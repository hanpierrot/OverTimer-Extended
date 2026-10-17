# OverTimer Extended — Project Reference

> ## ⚠️ MAINTENANCE RULE (mandatory for every human and agent)
> **This file must stay true.** Any change to code, `GameConfig.asset` or other config assets, prefabs, the scene, or a design decision that makes a statement here false **must update this file in the same commit**. A commit that changes behaviour without updating `PROJECT.md` is incomplete and should not be merged.
> Before finishing any task, run the **update checklist in §22** and bump the snapshot line below.

| Snapshot | Value |
|---|---|
| Last verified against code | **2026-10-17**, static read of the repo (nothing run in Play mode) |
| Branch / commit | `ari/pool-table` @ `e724c68` (fast-forward merge of `main`) |
| Engine | Unity **6000.3.14f1**, URP 2D, single scene |
| Replaces | `GAME_STRUCTURE.md`, `CODE_GROUND_TRUTH.md`, `ITEMS_INVENTORY.md`, `BILLIARDS_BALANCE.md`, `BILLIARDS_DECISIONS.md` (see §21) |
| Related plan (not built) | `PHONE_SHOP_DELIVERY_IMPLEMENTATION.md` + `PHONE_DELIVERY_DECISIONS.md` (claude.ai project "Overtimer") |

**Source-of-truth order:** code and live assets → this file → design/plan docs. When this file and the code disagree, the code wins: fix this file and say so in the commit.

**Tags used below:** `[verified]` read directly from code/asset · `[unverified]` could not be confirmed from the files available (see §20 for the Editor checklist) · `[planned]` decided but not built · **OPEN** known defect or undecided question.

Paths are relative to `Assets/Code/Scripts/` unless they start with `Assets/`, `ProjectSettings/` or `Packages/`. Line numbers drift, so search by symbol name first.

---

## Contents

1. [Pitch and core loop](#1-pitch-and-core-loop)
2. [Invariants I1–I10](#2-invariants-i1i10)
3. [Coding conventions](#3-coding-conventions)
4. [Team, ownership and workflow](#4-team-ownership-and-workflow)
5. [Project setup](#5-project-setup)
6. [Code map and services](#6-code-map-and-services)
7. [Game flow, endings and persistence](#7-game-flow-endings-and-persistence)
8. [Input, UI, hints and camera](#8-input-ui-hints-and-camera)
9. [Clock, money, Meter and taxes](#9-clock-money-meter-and-taxes)
10. [GameConfig — live tuning values](#10-gameconfig--live-tuning-values)
11. [Desk items](#11-desk-items)
12. [Cards](#12-cards)
13. [Laptop and apps](#13-laptop-and-apps)
14. [Economy summary](#14-economy-summary)
15. [Known issues (open)](#15-known-issues-open)
16. [Fixed issues ledger](#16-fixed-issues-ledger)
17. [Roadmap](#17-roadmap)
18. [Decision log](#18-decision-log)
19. [Tools and testing](#19-tools-and-testing)
20. [Verification gaps (check in the Editor)](#20-verification-gaps-check-in-the-editor)
21. [Document history](#21-document-history)
22. [Update checklist](#22-update-checklist)
23. [Glossary](#23-glossary)

---

## 1. Pitch and core loop

You are at a desk with **two minutes to live**. A clock counts down from **120 s**. Get it to **600 s (10:00)** and you win; hit **0** and you lose. Everything on the desk earns **money**, and the **Meter** turns money into clock time at **$1 = 1 s**. Between you and the clock: a tax agency (ITRS), a ringing phone, a cat, scratch tickets that mostly lose, and a pack of cards that can help or hurt.

- Origin: GMTK Game Jam (theme "Countdown"), extended into a course project ("Extended").
- Genre: single-screen (vertically scrolling) resource/risk puzzle. Target run 210–300 s, instant restart.
- **Throughput baseline:** a 4-minute run burns 240 s and must add 480 s, so the player needs about **$3/s sustained**. Every earner is priced against this. Below it is a trap or a comfort option; above it must carry a cost (lockout, variance, or clock).

```text
CLOCK ticks down (1 s per real second, x TickMultiplier)
   │
Work the desk → MONEY   (tasks, tickets, cards, laptop apps, pawning)
   │
Feed the METER: all money → clock at $1 = 1 s (min $10)
   │
ITRS taxes your earnings · Phone speeds the clock · Cat blocks objects · Cards add rules
```

---

## 2. Invariants I1–I10

These are **[LOCKED]** design rules (DESIGN.md v0.6 §2). Do not break one to make a task easier: if a request conflicts with an invariant, say so. Changes need an explicit owner decision recorded in §18.

| # | Rule | Approved amendments / exceptions | Status in code (2026-10-17) |
|---|---|---|---|
| I1 | Clock starts at **120 s**, win at **≥ 600 s**, lose at **≤ 0 s** | Card endings exist (Final Countdown, Set Completion, Creepy Jester). Changing 120/600 needs a written amendment after playtests | Holds (`startClock 120`, `winClock 600`, `loseClock 0`). End-screen and tutorial texts hard-code "10 minutes" / "2 minutes" |
| I2 | Clock ticks **down in real time, always** | Final Countdown intro pauses the clock | **Grey:** modal `CardPanel`/`TutorialPanel` freeze `Time.timeScale`; Time Scarab card is ×0 (asset, unverified). Planned beneficial calls also stop the clock. Amendment needed (see "Proposed" below) |
| I3 | **Only the Meter converts money → time** | Documented exceptions: Egg Timer adds clock; Blackjack ante/double spends clock; ITRS shortfall spends clock | Holds. `AddSeconds` callers: Meter, `HoldTask` (Egg Timer), cheat. `Spend` callers: Blackjack ×2, ITRS Ignore, cheat |
| I4 | **Nothing random grants clock time directly** | — | Holds |
| I5 | **No global pause that lets the player still act** | Pause allowed only while gameplay input is blocked (modal UI, cutscene, game over) | **At risk:** `GameManager.PauseGame` sets `timeScale = 0` for `CardPanel` and `TutorialPanel`; uGUI buttons (Meter Feed, camera arrows) may still be clickable if the panel does not cover them [unverified] |
| I6 | **$1 = 1 s** fixed | — | Holds (`exchangeRate 1`) |
| I7 | Debuffs **close a door**, never drain the clock | Pawning a debuff must really end it (`OnRemoved` exactly once) | Holds directly. Indirect drains by design: Robocalls (more rings), Audit (a bill you may ignore) |
| I8 | **One annoyance at a time**, ≥ 12 s gap (`AnnoyanceManager`) | **D8: Cat is exempt** (drag-away is instant) | **Not enforced:** `AnnoyanceManager` class exists but GAME_STRUCTURE (10-08) found it in no scene/prefab [re-verify]; Audit's `ForceBillNow` bypasses it. Plan replaces it with `CallScheduler` |
| I9 | No physics, no character. Camera **scrolls vertically only** inside a fixed room | **D7 (2026-08-31, owner-confirmed):** two hold buttons, clamped `[minY, maxY]`, no pan/zoom/drag | Holds. Billiards uses its own vector maths; Pawn uses a kinematic trigger only for detection |
| I10 | All tuning values live in `GameConfig`, never in code | Ticket symbol pools (`TicketSymbolConfig` assets), `BlockConfig`, card durations on each `CardSO`, Billiards presets (JSON) | **Partial:** `PickaxeView.hitCooldown` 0.7 s (sets Mining throughput), `TaxNotice.nagInterval`, Cat speed/sleep, `ScratchWindow.revealThreshold`, `CardBox.packSize`, hint/tutorial/end texts are literals |

**Proposed amendments, waiting on Ari (do not apply silently):**

- **I2/I5:** "The clock may stop only while all gameplay input is blocked: Final Countdown intro, modal panels, beneficial phone calls, game over." Time Scarab (×0 while the player can still act) then needs either an exception or a change.
- **I8:** "Only calls interrupt; at most one call at a time with ≥ 12 s between calls. Bills, mail and the doorbell are non-blocking notifications." (Phone plan.)
- **I10:** "…in `GameConfig` or a data asset it references (item catalog, symbol configs, presets, card assets); never as literals in logic."
- Pawn rule (from the landmine pass, not yet adopted): **an object may be pawnable only if the run stays winnable without it** (or it can be bought back).

---

## 3. Coding conventions

These came from real bugs. Follow them in every change.

| Rule | Why |
|---|---|
| Read another service's `.Instance` in **`Start`**, never `Awake`/`OnEnable`, and never assume Awake order across scripts | A GameObject (or any ancestor) saved inactive defers `Awake` until first activation. Services behind closed panels are unreachable |
| For effects that target something that may not have woken yet, use a **static "pending state"** that the target applies in its own `Awake` (pattern: `MiningApp`, `ScratchTicket`, `BilliardsPendingEffects`, `DesktopIconView.s_pendingHidden`) | Same as above |
| **Every mutable static must be reset on scene load** (`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` + `SceneManager.sceneLoaded`) | Restart = scene reload. Documented exceptions: `BilliardsRoundLog` (dev telemetry), `BilliardsDebug` (cheat switches) |
| Compare Unity objects with explicit **`!= null`**, never `?.` / `??` | `?.` misses Unity's fake-null on destroyed objects. ~45 legacy `Instance?.` sites remain (mostly `HintService`) — clean up when touched |
| **No `UnityEngine.Random`.** Use `RngService.Instance.Random` (seedable `System.Random`) | Reproducible bugs, deterministic Billiards. One cosmetic exception left: `Cutscene/ImageCycler.cs` |
| `[FormerlySerializedAs("old")]` on every renamed serialized field | Keeps asset values |
| **Enums are append-only** (`CostModifierService.Target`, `EndReason`, `HintId`, `Rarity`…) | Serialized as ints; `EndReason` is also persisted **by name** in run history |
| Asset names of `CardSO`s and future item ids are **permanent** | Collection save keys on `CardSO.name` |
| New `GameConfig` fields: add with a sensible default, then **touch them in the Inspector** so the asset serializes them | C# defaults are stale for ~20 fields; the asset is the live value |
| Card effects own their grant: `OnPlaced` adds, `OnRemoved` withdraws, using it calls `card.EndEffect()`; `CardItem._hasEnded` guarantees `OnRemoved` runs once | Pawning a card must be a real escape (I7) |
| Pure logic in plain C# classes (no `MonoBehaviour`, `Time`, `Physics2D`), views only read state | Testable headless (Billiards sim, Blackjack rules) |
| No coroutines for the core economy tick; short local coroutines are fine | Debuggability |
| Kill your DOTween tweens on close/disable; UI tweens that must run during freeze use `SetUpdate(true)` | No global `DOTween.KillAll` on reload |
| Every new behaviour checks `GameManager.Instance.IsGameOver` and stops | Game over freezes `timeScale` but Unity UI still clicks |
| Put an **`InputBlocker`** component on any panel that should block desk input | `GameManager.inputBlockers` no longer exists |
| Build new things as **prefabs**; keep `SampleScene.unity` edits minimal and announce them to Han | Single shared scene = merge conflicts |

---

## 4. Team, ownership and workflow

| Person | Owns | Notes |
|---|---|---|
| **Ari** | Systems: `GameConfig` (asset edits go through Ari), Services (Clock, Money, Meter, Rng, ITRS, Phone logic), Laptop + all apps (Mining, Blackjack, **Billiards**), pawn system, restart flow, builds, plan docs | Wrote Billiards (Phases 1–9) and the Phone/Shop plan |
| **Han** | Art and desk content: scratch tickets, card box, **card assets** (text, art, rarity, weight), egg timer, cat, SFX, cutscenes, win/lose cards, summary/run history | Most commits on `main`; owns `Resources/CardSO/*` and `Cards.png` |

- **Agents: ask whether you are talking to Ari or Han** before assigning, prioritising or estimating work.
- Ownership is "owner reviews", not "owner only": Ari has fixed Han's card code (Billiards Phase 8) and Han's refactor added fields to `GameConfig`. Tell the owner when you touch their area.
- Ari writes code for new cards; **Han creates the card assets**.
- Language: the team is Vietnamese-speaking. Docs are English; summaries for Han may be bilingual EN/VI.
- Workflow (detail in `AGENTS.md`): one branch per batch from `main`, merge `main` back in before finishing, never stack feature branches; plan phases end with a **CHECKPOINT** report and wait for Ari's "approved"; coordinate scene edits with Han first.

---

## 5. Project setup

### 5.1 Engine and packages (`Packages/manifest.json`)

| Package | Version | Use |
|---|---|---|
| Unity | 6000.3.14f1 (`d68c3f99a318`) | |
| `com.unity.render-pipelines.universal` | 17.3.0 | URP 2D |
| `com.unity.inputsystem` | 1.19.0 | **mouse only** (`<Mouse>/position`, `<Mouse>/leftButton`) |
| `com.unity.timeline` | 1.8.12 | end cutscenes |
| `com.unity.ugui` | 2.0.0 | all UI (TextMeshPro) |
| `com.coplaydev.unity-mcp` | git `#v10.0.0` | Unity MCP for agents |
| `com.unity.ai.assistant` / `ai.inference` | 2.18.0-pre.2 / 2.6.1 | Unity AI |
| 2D set | animation 13.0.4, aseprite 3.0.1, psdimporter 12.0.1, spriteshape 13.0.0, tilemap 1.0.0 (+extras 6.0.1) | |
| Others | test-framework 1.6.0, recorder 5.1.7, visualscripting 1.9.11, collab-proxy 2.12.4, IDE packages | |
| **DOTween** | plugin under `Assets/Plugins/Demigiant` (not a package) | card flip, hints, end panel |

Target platform: **WebGL** (custom 1080p template). No `.asmdef` files: runtime code is `Assembly-CSharp`, `Editor/` folders are `Assembly-CSharp-Editor`.

### 5.2 Repository

- Remote: `github.com/hanpierrot/OverTimer-Extended`. Branches seen: `main`, Han's `Han/RefactorSystem`, `Han/RemakeCards`, `Han/RemakeTickets` (+ older `Han/*`), Ari's `ari/pool-table` (current).
- **Git LFS** for images, audio, video, fonts, dll/so/exe, archives, pdf. `*.unity/*.prefab/*.asset/*.anim/*.controller/*.playable` use `merge=unityyamlmerge`, `eol=lf`. Asset serialization: Force Text.
- `.gitignore` adds `.idea`, `Docs`, and **`CODE_GROUND_TRUTH.md`, `GAME_STRUCTURE.md`, `ITEMS_INVENTORY.md`** (those three were local-only). `PROJECT.md`, `AGENTS.md` and `CLAUDE.md` are meant to be committed.
- **The `unityyamlmerge` driver is not defined in the working clone's `.git/config`.** Unless it is set in a global gitconfig (check `git config --get merge.unityyamlmerge.driver`), the `.gitattributes` rule silently falls back to a text merge. Setup commands: `AGENTS.md` §2.
- Agent instructions: `AGENTS.md` (all agents), `CLAUDE.md` (Claude; imports `AGENTS.md`), `.claude/rules/*.md` (Unity C#, scenes/prefabs, config assets, cards, Billiards; path-scoped).
- `.claude/settings.local.json` allows UnityMCP `find_gameobjects`, `manage_scene`, `manage_components`, `refresh_unity` and `Bash(git diff *)`.
- Commit styles: Ari `Phase N: <summary>`; Han sentence style; fix passes `landmine: <item>` / `chore: …`. Recent history: Billiards Phases 1–7 (10-13 → 10-15), "Finish implementing the billiards table." (10-16), "Remove all the images." (10-16), merges of `main` (10-16, 10-17).

### 5.3 Assets layout

| Path | Contents |
|---|---|
| `Assets/Code/Scripts/` | all C# (§6) |
| `Assets/Design/Scenes/SampleScene.unity` | **the only scene** (build index 0) |
| `Assets/Design/Prefab/**` | items (Laptop, Phone, EggTimer, PrinterPawn, Bear…), cards (CardBox, stacks), panels |
| `Assets/Settings/Configs/` | `GameConfig.asset`, `BlockConfig.asset`, `LuckyNineSymbols.asset`, `TripleMatchSymbols.asset` |
| `Assets/Resources/CardSO/` | every `CardSO` asset (loaded by folder with `Resources.LoadAll`) |
| `Assets/Resources/BilliardsPresets/*.json` | 10 Billiards tables |
| Timelines | `FinalCountdownIntro/Lose/Win`, `Jester`, `SetCompletion` |

### 5.4 Project settings

- Tags: none. Layers: 0 Default, 1 TransparentFX, 2 Ignore Raycast, **3 DeskObject** (pawnable mask), 4 Water, 5 UI. Sorting layers: `Default` only (order by `sortingOrder`).
- Canvas: Screen Space Overlay, `CanvasScaler` Scale With Screen Size **960×540**, match width. Main Camera orthographic size 6.
- Script execution order (from `.cs.meta`, per GAME_STRUCTURE 10-08): `GameManager −20 → InputManager −10 → MoneyService −5 → RngService −4 → CardCollectionManager −3 → RunStatsService −2 → others 0`. [re-verify]; the Phone plan wants `ClockService −1`.

### 5.5 Scene `SampleScene.unity`

Nothing is `DontDestroyOnLoad`; **Restart = reload this scene.**

| Root | Contents |
|---|---|
| `Background` | `CameraVerticalScroller`; Desk, DeskLegs, Shadow, `VFX` (10 pooled `ClickFeedbackVFX`) |
| `DeskObject` | prefab instances: PrinterPawn, Laptop (desk opener), EggTimer, Taxes, Phone, PowerBank, ElectricSocket (wire puzzle), CardItems (7 pooled), Picture, Backpack, CardCollection, Cat, Bear, Tickets (2 Triple + 2 Nine pooled tickets) |
| `Main Camera` | ortho 6, AudioListener |
| `Global Light 2D` | |
| `MANAGERS` | `Managers` (GameManager, InputManager, CheatMenuToggle) · `Meter` · `Services` (ClockService, RngService, ClickFeedbackService, MoneyService, RunStatsService, CostModifierService, ITRSService/AnnoyanceManager [which ones: unverified]) · `CardRelatedManager` (CardCollectionManager, CardHandManager, FinalCountdownTracker, SetCompletionTracker) |
| `EventSystem` | `InputSystemUIInputModule` |
| `Canvas` | `Timer` (TimerText, MeterText, FeedButton), `MoveButtonPanel` (Up/Down `HoldButton`), panel prefabs: Laptop (UI), Mining App, Blackjack App, Billiards App, Tax, CardRevealPanel, CardPanel, CardCollectionPanel, NinePanel, TriplePanel, WirePanel, EndGamePanel, CheatMenuPanel, TutorialPanel, HintRoot |
| `Audio` | one AudioSource, no clip (silent) |
| `CutsceneRoot`, `Cutscenes`, `ClockRoot` | cutscene canvas, `CutsceneController` + PlayableDirectors, clock-ending ticker |

Scene rules: don't save the scene with the Billiards app open (layout overrides leak); panel prefabs are saved inactive (their `Awake` waits for first open); never re-save `Mining App.prefab` by accident.

---

## 6. Code map and services

### 6.1 Folders (130 `.cs` files + 4 unstaged in `WinConditionCard/`)

| Folder | Files | Purpose |
|---|---|---|
| `Config/` | 3 | `GameConfig` (all tuning), `BlockConfig` (mining blocks), `TicketSymbolConfig` (declared in **`TicketSymbolPool.cs`**) |
| `Services/` | 12 | GameManager, ClockService, MoneyService, Meter, RngService, RunStatsService, InputManager, HintService, ClickFeedbackService, CostModifierService, CardHandManager, CardCollectionManager |
| `Input/` | 5 | `IPointerInputHandler`, `PointerReceiver`, `DeskObject` base, `InputGate` (static counter), `InputBlocker` |
| `Hint/` | 3 | `HintId`, `HintCatalog`, `TutorialPanel` |
| `Cutscene/` | 3 | `CutsceneController`, `TimerTicker`, `ImageCycler` |
| `CheatMenu/` | 3 | `CheatMenuController`, `CheatMenuToggle`, `BilliardsCheats` |
| `UI&Camera/` (+`Summary/`) | 2 + 6 | `CameraVerticalScroller`, `HoldButton`; `EndReason`, `RunRecord`, `RunRating`, `EndGameSummaryUI`, `RunHistoryPanel`, `RunHistoryEntryView` |
| `Annoyance/` (+`Cat/`) | 5 + 2 | `AnnoyanceManager`, `Phone`, `ITRSService`, `Tax` (panel), `TaxNotice` (desk); `CatController`, `CatStateMachine` |
| `Desk/Items/Cards/` | 8 | `CardItem`, `CardBox`, `CardPanel`, `CardPackRevealPanel`, `CardRevealSlot`, `CardFlip`, `CardCollectionPanel/Slot` |
| `Desk/Items/Cards/CardData/` | 16 (+4) | `CardSO`, `EffectCardSO`, 14 effect classes; `WinConditionCard/{FinalCountdown,SetCompletion}/*` |
| `Desk/Items/Pawn/` | 3 | `Pawn` (the printer), `IPawnable`, `JunkItem` |
| `Desk/Items/Tasks/` (+`ExtendedTasks/`, `WireComponents/`) | 5 + 3 + 4 | `TaskBase`, `ClickTask`, `HoldTask`, `WireTask`, `DeskObjectFocus`; `EggTimerTask`, `PictureClickTask`, `PowerBankTask`; wire plug/socket/line/initializer |
| `Desk/Items/Tickets/` | 3 | `ScratchTicket`, `ScratchWindow`, `TicketStack` |
| `Desk/Laptop/` | 6 | `LaptopController`, `LaptopApp`, `LaptopHome`, `DesktopIconView`, `DeskLaptopOpener`, `CloseAppButton` |
| `Desk/Laptop/Mining/` | 2 | `MiningApp`, `PickaxeView` |
| `Desk/Laptop/Blackjack/` | 5 | `BlackjackApp`, `BlackjackGame` (pure C#), `CardDeckConfig`, `CardRowView`, `CardSlotView` |
| `Desk/Laptop/Billiards/` (+`View/`) | 14 + 9 | §13.4 |
| `Editor/Billiards/` | 8 | authoring, solver, verifier, self-tests, balance report |

### 6.2 Singletons

All use `if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this;` in `Awake`. None survive a reload.

| Class | Owns | Key API |
|---|---|---|
| `GameManager` | config ref, game-over state, pause refcount, restart | `GameConfig`, `IsGameOver`, `LastEndReason`, `OnGameOver(EndReason)`, `PauseGame()/ResumeGame()` (refcounted, `timeScale`), `GameOver(reason)`, `Restart()` |
| `ClockService` | `RemainingTime`, `TickMultiplier`, final-countdown intercept | `AddSeconds(s, reason)`, `Spend(s, reason)`, `PauseCountdown()/ResumeCountdown()` (plain bool), `ArmFinalCountdownIntercept(reset)`, `OnTimeChanged`, `OnCountdownFinished`, `OnFinalCountdownTriggered` |
| `MoneyService` | `Current` | `Add(int, reason)`, `TrySpend(int, reason) → bool` (never negative), `OnMoneyChanged(current, reason)`, `const StakeRefundReason = "stake refund"` |
| `Meter` | Feed button | `SetLocked(bool)`, `IsLocked` |
| `RngService` | one `System.Random` | `Random`; serialized `useFixedSeed`, `seed` (scene: unseeded) |
| `RunStatsService` | run stats + history | `LastRecord`, `LoadHistory()` |
| `InputManager` | press/drag state machine | `IsInputEnabled`, `dragThreshold` 0.15, vestigial `FocusRoot` |
| `HintService` | toast hints | `Show(id, args)`, `ShowOnce(id, args)` |
| `ClickFeedbackService` | 10 pooled particle systems | `PlayAt(Vector2)` |
| `CostModifierService` | price multiplier per target | `enum Target { Ticket, CardPack, Blackjack, Billiards }`, `GetMultiplier`, `SetMultiplier` (overwrite) |
| `CardHandManager` | 7-card pool, placed set | `HasRoom` (`< handCap`), `TryGetFreeCard`, `CardPlaced/CardRemoved` |
| `CardCollectionManager` | collected card types | `Register(card)`, `IsCollected`, `AllCards`, `NewCardRegistered` |
| `CardPanel` | card detail popup | `Show(CardSO, Sprite)` — **pauses the game while open** |
| `CutsceneController` | end/intro cutscenes | `PlayEnding(reason, onDone) → bool`, `PlayFinalCountdownIntro()`, `StopAll()` |
| `Phone` | ring timer | `IsRinging`, `Silence(d)`, `Unsilence()`, `StartRobocalls(d)`, `StopRobocalls()` |
| `AnnoyanceManager` | busy flag + gap | `CanStart`, `TryBegin(reason)`, `End(reason)` |
| `ITRSService` | tax bills | `Pay()`, `Ignore()`, `SkipNextBill(owner)`, `ForceBillNow()`, `DoubleCurrentBill()`, `OnAssessmentIssued/Resolved` |

Static helpers: `InputGate` (counted global input lock), `RunRating` (score maths).

---

## 7. Game flow, endings and persistence

### 7.1 Run lifecycle

1. Scene loads. `GameManager.Awake` unfreezes (`timeScale 1`). `ClockService.Start` sets 120 s and starts. `MoneyService.Start` sets $40.
2. First launch only: `TutorialPanel` (5 click-through steps, skippable) opens and **pauses the game** until closed (`PlayerPrefs TutorialSeen`).
3. The **Laptop starts disabled** until the wire puzzle (ElectricSocket) is solved.
4. **Win:** any `OnTimeChanged` with clock ≥ `winClock` → `GameOver(ClockWin)`. **Lose:** clock reaches 0 (`EndCountdown`) → `GameOver(ClockLoss)`.
5. `GameOver(reason)`: sets `IsGameOver`, disables input, pauses the clock, **freezes `timeScale = 0` and `AudioListener.pause`**, fires `OnGameOver` (run record saved here), then plays the ending cutscene if one exists, then shows `EndGamePanel` (fade, DOTween unscaled).
6. **Restart** button → `GameManager.Restart()` → reload scene. No pause menu, no main menu, no quit.

### 7.2 Endings (`EndReason`, persisted by name — append only)

| EndReason | Trigger | Cutscene | Rating outcome |
|---|---|---|---|
| `ClockWin` | clock ≥ 600 | code ticker: clock text counts 3 s toward 600 | 1.0 |
| `SetCompletionWin` | Set Completion card set on desk | `SetCompletionTimeline` (6 s) | 0.95 |
| `FinalCountdownWin` | 2nd card placed after Final Countdown triggered | `FinalCountdownWinTimeline` (4.23 s) | 0.95 |
| `ClockLoss` | clock 0 | code ticker toward 0 | 0 |
| `FinalCountdownLoss` | 2nd card placed while Final Countdown armed | director may be empty [unverified] | 0.1 |
| `CreepyJesterLoss` | Creepy Jester finishes its art cycle | none mapped [unverified] | 0 |

If a reason has no director, the end panel shows immediately. `GameOver` has **no re-entry guard** (a second call saves a second record).

### 7.3 Pause model (two separate mechanisms)

| Mechanism | Type | Used by |
|---|---|---|
| `GameManager.PauseGame/ResumeGame` | refcounted, sets `Time.timeScale = 0` | `CardPanel` (open/close), `TutorialPanel`; `ResumeGame` never unfreezes after game over |
| `ClockService.PauseCountdown/ResumeCountdown` | **plain bool, not counted** | `GameOver`, Final Countdown intro |

Everything gameplay-side uses scaled time, so it freezes with `timeScale`. Cutscenes run `UnscaledGameTime`; hints and end panel use `SetUpdate(true)`.

### 7.4 Run summary and rating

- `RunStatsService` tracks peak clock (labelled "Best Time"), money earned (all positive deltas except stake refunds, including pawning), cards opened, new collection entries; saves a `RunRecord` JSON on game over into a 20-slot ring buffer.
- `EndGameSummaryUI` shows 4 stats. `RunHistoryPanel` lists history with 4 sort modes.
- `RunRating.Score` = `100 × (0.5·outcome + 0.3·survival + 0.2·money)` with `survival = win ? 1 : clamp01(peakClock / 600)` and `money = e / (e + 300)`. **Continuous 0–100, no grades/thresholds.** Shown in history rows only. Example: ClockWin with $300 earned = 90.

### 7.5 Persistence (`PlayerPrefs`)

| Key | Content |
|---|---|
| `TutorialSeen` | 1 after tutorial closed |
| `HintSeen_<HintId>` | 1 per first-time hint shown |
| `CardCollection_UnlockedIds` | comma-joined **`CardSO` asset names** (a comma or rename breaks it) |
| `RunHistory_Total`, `RunHistory_NextSlot`, `RunHistory_0..19` | lifetime counter, ring head, `RunRecord` JSON |

- **`CreepyJesterCardSO` calls `PlayerPrefs.DeleteAll()`** right before its loss: wipes tutorial flag, hints, collection and run history. Team accepts "tutorials come back" as a joke; the collection/history wipe is still to confirm (§15).
- Dev/Editor builds only: `billiards_rounds.csv` in `Application.persistentDataPath`.
- No mid-run save.

---

## 8. Input, UI, hints and camera

### 8.1 Desk input

- `InputManager` (new Input System, mouse only). On press: ignored if input disabled or pointer over **any** UI raycast target; else `Physics2D.OverlapPoint(worldPos, interactableLayers)` (scene mask = Everything) and the first collider's `IPointerInputHandler`.
- Press becomes a drag after **0.15 world units** (`dragThreshold`, untuned). Release without drag = click (`OnClickDown` fires on **release**, plus click VFX).
- `PointerReceiver`: events ClickDown, DragStart/Update/End, PressStart/Update/End. `SetInteractable(bool, HintId?)` is a **plain bool, not refcounted** (a Cat block ending can re-enable something another system disabled). A rejected click raises a hint toast.
- `InputGate` (static counter) + `InputBlocker` (component: `OnEnable → Add`, `OnDisable → Remove`). `GameManager` maps the gate to `InputManager.IsInputEnabled` and the camera buttons. No `InputBlocker` was found in the staged scene/prefabs; the panel prefabs that should carry them were not inspected [unverified].
- uGUI (Meter Feed, camera arrows, panel buttons) goes through the EventSystem and **is not gated by `IsInputEnabled`**.
- A press already in progress is not cancelled when input gets blocked or the game ends.

### 8.2 Hints and tutorial (exist in code; not in any older design doc)

- `HintId` has 25 values: `LaptopNoPower, CatBlocking, NotEnoughMoney, MeterMinFeed, MeterLocked, TooManyTickets, HandFull, NoRoomToPlace, NotEnoughTime, TaxCannotAfford, ClickCooling, PowerBankRecharging, CannotPawn, LaptopIconDoubleClick, MiningOpen, MiningCloseLosesOre, BlackjackOpen, BlackjackLeaveMidHand, TicketOpen, WireOpen, PhoneFirstRing, MailTrayFirstBill, CatFirstBlock, EggTimerPlaced, ShopPurchased` (append only). `EggTimerPlaced` and `ShopPurchased` are unused yet.
- `HintCatalog.Get(id)` holds all texts as literals; some take `{0}` arguments (`MeterMinFeed`, `HandFull`, `NotEnoughTime`) — call those with an argument or `string.Format` throws.
- `HintService`: one toast label, hold 2.5 s, fade 0.5 s, per-id cooldown 3 s (unscaled), silent after game over. `ShowOnce` remembers across runs via `HintSeen_<id>`.
- `TutorialPanel`: 5 hard-coded steps ("You have 2 minutes to live", "Get the clock to 10:00…", "$1 = 1 second"…), pauses via `PauseGame`. **Name collision:** the Phone plan proposes a new `TutorialPanel` — extend this one instead.

### 8.3 Camera

`CameraVerticalScroller`: Y only, 5 units/s, clamped **minY −5 … maxY 6** (scene values). Up/Down `HoldButton`s hide at the limits and when input is blocked. Known glitch: if a held button gets deactivated, `_direction` can stay set and the camera keeps scrolling until the other button is pressed.

### 8.4 Cheat menu (ships in builds — no `#if` guard)

- Toggle key: script default F1, **scene override = value 10** (most likely Backslash `\`) [unverified]; F2 stops a cutscene.
- Buttons: give money, give time (negative amounts silently do nothing), place any card, preview / force every ending, FC intro, stop cutscene; Billiards: debug overlay, mirror cycle, table cycle, verify presets, round log (dev builds).

### 8.5 Audio

No audio manager or mixer. Per-object `AudioSource`s: Phone ring loop, Pawn loop while dragging, TaxNotice nag (`PlayOneShot`), Jester laugh (`ignoreListenerPause`). **Phone and TaxNotice share one AudioSource**, so stopping the ring cuts the tax nag. Billiards exposes sound hooks with no clips.

---

## 9. Clock, money, Meter and taxes

### 9.1 ClockService

- Tick: `RemainingTime -= Time.deltaTime * TickMultiplier` every frame while running; `OnTimeChanged` fires every frame.
- At ≤ 0: if the Final Countdown intercept is armed, reset to its value (180 s) and raise `OnFinalCountdownTriggered`; else end the countdown (loss).
- `AddSeconds(s, reason)` ignores `s ≤ 0` and game over; no cap. `Spend(s, reason)` clamps at 0 and **ends the run directly, bypassing the Final Countdown intercept**. The `reason` strings are **discarded** — there is no balance log yet.
- **`TickMultiplier` is one scalar, last writer wins.** Writers: `Phone` (1.5 on ring start, 1.0 on stop) and `ClockSpeedCardSO` (its value on place, 1.0 on remove). Overlaps cancel each other (e.g. Slow Hand placed → ring → ring ends → back to 1.0 while the card still shows). Planned fix: keyed `SpeedModifiers` (Phone plan Batch 1).

### 9.2 Money and Meter

- `MoneyService` is the only writer of money. `Add` ignores ≤ 0; `TrySpend` fails rather than going negative.
- **Meter Feed spends *all* current money** and adds `money × exchangeRate` seconds. Minimum feed $10 (hint `MeterMinFeed`). Blocked while `IsLocked` (Final Countdown armed, hint `MeterLocked`). It is the only money → time path.
- `MoneyService.StakeRefundReason` ("stake refund") marks money that is not income (Billiards bet returned on a win): excluded from ITRS and run stats. **Everything else counts as earned**, including pawning and card sales.

### 9.3 ITRS (taxes)

| Rule | Live value |
|---|---|
| Bill interval | every **45 s** of running clock |
| Amount | `max($20, round(20% × money earned since last bill))` |
| Pay | `TrySpend(due)`; Pay button disabled when you can't afford it |
| **Ignore, or close the panel** | costs **due × 2 s** of clock (minimum 40 s; at < 40 s clock this kills the run) |
| Gate | uses `AnnoyanceManager.TryBegin` (blocks a 2nd bill while one is pending) — **only works if the manager is in the scene** |
| Card hooks | Tax Exemption → `SkipNextBill` (next bill issued at $0); Audit/Tax Increase → `ForceBillNow` (bypasses the gate) or `DoubleCurrentBill` |

- `TaxNotice` (desk "Taxes" object) nags every 2 s until clicked, then opens the `Tax` panel.
- Effective tax is above 20% for low earners (a $1.2/s grinder pays ~36%). The Egg Timer earns no money, so it is never taxed.
- If `DoubleCurrentBill` fires while the panel is open, the panel shows the old amount but `Pay` charges the doubled one.
- Planned (Phone plan): bill shown in a Taxes app + paper copy, 30 s deadline then penalty once, close ≠ ignore, Ignore button removed, ITRS itself refuses a second bill while one is pending.

---

## 10. GameConfig — live tuning values

`Assets/Settings/Configs/GameConfig.asset` holds **106 fields, all serialized, no stale keys**. The **asset is the truth**: C# defaults in `Config/GameConfig.cs` are stale for the rows marked ⚠ (a freshly created GameConfig would play the old, pre-hotfix game — e.g. Lucky Nine paying $0).

| Group | Field | Live | C# default |
|---|---|---|---|
| Global | `startClock` / `winClock` / `loseClock` | 120 / 600 / 0 | same |
| | `startMoney` / `exchangeRate` / `meterMinFeed` | 40 / 1 / 10 | same |
| Mining | `blockPayout` | **[1, 2, 4, 5, 7, 10, 12, 15, 17, 21]** ⚠ | [1,3,4,5,6,8,9,10,12,13] |
| | `veinLengthMin` / `veinLengthMax` | 6 / 10 | same |
| | `veinRespawn` | **1.0** ⚠ | 1.5 |
| Tickets | `tripleMatchCost` / `tripleMatchBaseReward` | 25 / 55 | same |
| | `luckyNineCost` / `luckyNineBaseReward` | **55 / 26** ⚠ | 80 / 0 |
| Card Box | `handCap` / `packCost` | 5 / 35 | same |
| | `commonWeight` / `rareWeight` / `superRareWeight` / **`ultrRareWeight`** (typo) | 60 / 25 / 12 / 3 | same |
| Blackjack | `blackjackAnte` / `blackjackClockMargin` | **20 s / 15 s** ⚠ | 15 / 10 |
| | `blackjackWinPayout` / `PushPayout` / `NaturalPayout` | **100 / 25 / 130** ⚠ | 120 / 60 / 200 |
| | `blackjackStreakStep` / `blackjackStreakCap` (Hot Table) | 0.10 / 0.50 | same |
| | `blackjackRevealDelay` / `blackjackResultDelay` / `laptopSwitchTime` | 0.6 / 1.2 / 1.0 s | same |
| ITRS | `billInterval` / `billRate` / `billMinimum` / `shortfallPenalty` | 45 s / 0.20 / $20 / 2 s per $ | same |
| Phone | `ringIntervalMin/Max` / `ringEffectMultiplier` / `answerTime` / `maxRingDuration` | 30–60 s / 1.5 / 1.5 s / 10 s | same |
| Annoyance | `annoyanceMinGap` | 12 s | same |
| Egg Timer | `crankTime` / `timePayout` | **1 s** ⚠ / 3 s | 2 / 3 |
| Cat | `catSitDurationMin/Max` | **6 / 14 s** ⚠ | 3 / 8 |
| | `catBlockChance` / `catBlockDurationMin/Max` | 0.4 / **3 / 6 s** ⚠ | 0.4 / 4 / 8 |
| Click Task (Bear) | `clickTaskMinClicks/MaxClicks` / `LockDuration` / `MoneyReward` | 3–8 / 3 s / $5 | same |
| Picture | `pictureClickTask*` | 3–8 / 3 s / $5 | same |
| Power Bank | `powerBankCrankTime` / `MoneyReward` / `FullHoldDelay` / `Cooldown` / `RestTime` | 2 s / **$8** ⚠ / 1.5 / 5 / 2 s | reward 10 |
| Wire | `wireTaskMoneyReward` | $20 | same |
| Pawn values | `eggTimerPawnValue` / `powerBankPawnValue` / `clickTaskPawnValue` / `pictureClickTaskPawnValue` / `wireTaskPawnValue` | 150 / 45 / 40 / 40 / 15 | same |
| | `cardBoxPawnValue` / `nineStackPawnValue` / `laptopPawnValue` | **48 / 24 / 120** ⚠ | 180 / 100 / 300 |
| | `tripleStackPawnValue` (never paid: stack unpawnable) | 50 | same |
| | `junkSmall/Medium/LargePawnValue` / `unrevealedCardSellValue` | 5 / 15 / 30 / 10 | same |
| Billiards economy | `billiardsBetMin/Max/Step` | $10 / $40 / $5 | same |
| | `billiardsParOffsets` (Easy/Med/Hard) | [4, 2, 0] | same |
| | `billiardsTargetRate` / `billiardsAssumedWinRate` | [1.5, 3, 4] / [0.85, 0.65, 0.5] | same |
| | `billiardsSecondsPerShot` / `billiardsAssist` | 7 / [FullGhost, ContactPoint, DirectionOnly] | same |
| Billiards rules | `billiardsStrictFirstContact` / `billiardsFinalWrongPocketFails` | true / true | same |
| Billiards physics ⚠ re-run verifier after any change | `billiardsBallRadius` / `CornerPocketRadius` / `SidePocketRadius` | **0.0614 / 0.12 / 0.09** ⚠ | 0.0714 / 0.15 / 0.12 |
| | `MaxShotSpeed` / `RollingDecel` / `BallRestitution` / `CushionRestitution` / `StopSpeed` / `MaxShotSeconds` | 5 / 1.2 / 0.95 / 0.75 / 0.01 / 20 | same |
| Hints | `hintHoldSeconds` / `hintFadeSeconds` / `hintRepeatCooldown` | 2.5 / 0.5 / 3 | same |
| Run rating | weights outcome / survival / money; `ratingMoneyHalfPoint` | 0.5 / 0.3 / 0.2; 300 | same |
| | outcome scores ClockWin / SetCompletion / FCWin / ClockLoss / FCLoss / Jester | 1 / 0.95 / 0.95 / 0 / 0.1 / 0 | same |

Billiards validation (`GameConfig.ValidateBilliards`): `maxShotSpeed/120 < ballRadius` (0.042 < 0.0614 ✓), corner ≥ 1.9 × ball (0.12 ≥ 0.1167, 3% margin), side ≥ 1.4 × ball (0.09 ≥ 0.086, 5% margin). Failing it shows "Billiards unavailable".

Other tuning assets: `BlockConfig.asset` (block HP, §13.2), `TripleMatchSymbols.asset` / `LuckyNineSymbols.asset` (§11.3), each `CardSO` asset (durations/multipliers), Billiards preset JSONs.

---

## 11. Desk items

### 11.1 Inventory

| Item | Class | Mechanic | Reward (live) | Pawn |
|---|---|---|---|---|
| Egg Timer | `EggTimerTask : HoldTask` | hold 1 s (release resets) | **+3 s clock**, no cooldown | $150, **pawnable** per prefab (layer 3) — conflicts with ITEMS_INVENTORY "layer 0" [verify] |
| Power Bank | `PowerBankTask : HoldTask` | hold 2 s cumulative | $8, then locked 8.5 s (1.5 + 5 + 2) | $45, layer [unverified] |
| Bear | `ClickTask` | click 3–8 times (rolled) | $5, then 3 s lock | **unpawnable** (`pawnable: 0` on prefab) |
| Picture | `PictureClickTask : ClickTask` | same, own config | $5, 3 s lock | $40, layer [unverified] |
| Wire puzzle (ElectricSocket) | `WireTask` | drag 5 plugs to shuffled sockets | $20 once; **unlocks the Laptop** | $15 — **pawning it re-locks the Laptop forever** |
| Laptop (desk opener) | `DeskLaptopOpener` | opens laptop panel unless `Disabled` | apps (§13) | **$120, pawnable** (removes all three apps) |
| Backpack | `JunkItem` (Medium) | none | — | $15 |
| Card Box | `CardBox` | buy a 3-card pack | §12 | $48 |
| Triple Match stack | `TicketStack` | sells $25 tickets | §11.3 | **unpawnable** (`CanBePawned => IsNine`) |
| Lucky Nine stack | `TicketStack` | sells $55 tickets | §11.3 | $24 |
| Printer | `Pawn` | drag onto an item to pawn it | money | — |
| Phone | `Phone : DeskObject` | rings | — | **never** (no `IPawnable`; orphan `OnPawned()` is dead code) |
| Taxes | `TaxNotice` | bill notice | — | no |
| Cat | `CatController` | roams, blocks | — | no |
| Card Collection | binder UI | view only | — | no |
| Meter | uGUI | money → clock | — | — |

### 11.2 Tasks

- `TaskBase.CompleteTask()` → `ApplyReward()` → `OnTaskCompleted()`; tasks pay **money**, only the Egg Timer pays clock (documented I3 exception).
- `ClickTask`: required clicks rolled uniform 3..8 in `Start` and after each lock; clicks during the lock show `ClickCooling`.
- `HoldTask`: progress while held, completes at `CrankTime`; `EggTimerTask` adds `AddSeconds(timePayout, "hold task")`. **Landmine L1 is still live:** 1 s hold → +3 s = **+2 s/s net**, untaxed; holding alone wins in ~240 s. Locked fix: **0.75 s cooldown after each crank, in `EggTimerTask` only** (Power Bank shares `HoldTask`) → +0.71 s/s net [planned, Batch 2].
- `PowerBankTask` overrides the reward to money and does not reset progress on release.
- `WireTask`: sets `LaptopController.Disabled = true` in `Start`/`BeginPuzzle`, false on completion, **true again in `OnPawned`** (bug, §15). Completing it makes it non-interactive, but a Cat block ending re-enables it (`SetInteractable(true)`) and can reopen the puzzle.

### 11.3 Scratch tickets

- `TicketStack` (desk) holds a pool of **2 tickets per tier**; a third purchase shows `TooManyTickets`. Purchase charges `round(baseCost × CostModifier[Ticket])` and rolls every window's symbol **at purchase**. Scratching only reveals.
- Windows: **Triple Match 3**, **Lucky Nine 9**. A window reveals at 60% scratched (`ScratchWindow.revealThreshold`); the ticket resolves when all are revealed or on close (Cash Out).
- **Reward as coded:** `round(baseReward × (maxMatch − 1) × multiplier[bestSymbol])`, where `maxMatch` counts **revealed** windows only and `bestSymbol` is the symbol with the **highest count** (ties → higher multiplier). It is *not* the best-paying symbol. Closing early locks the reward computed from revealed windows; closing unscratched pays $0.

| Symbols | weights | multipliers |
|---|---|---|
| Triple Match (3) | 75 / 20 / 5 | 0.25 / 0.6 / 2 |
| Lucky Nine (5) | 40 / 27 / 18 / 10 / 5 | 0.3 / 0.6 / 1.2 / 2.5 / 6 |

| EV (exact enumeration, live values) | Triple Match | Lucky Nine |
|---|---|---|
| Cost / base reward | $25 / 55 | $55 / 26 |
| EV, all windows revealed | **$22.23** (RTP 88.9%) | **$47.76** (RTP 86.8%) |
| P(profit) | 53% | 26% |
| Min / max payout | $0 (4.5%) / $220 | **$16** (never $0: 9 windows over 5 symbols) / $1,248 |
| Optimal early stopping | no gain | **$52.13** (stop on a high pair before low triples bury it) |

Both tiers are negative-EV gambles, as intended after the hotfix (base 110 → 26). The "stop early" quirk is discoverable because the panel shows a live total — decide whether to keep it (§15).

### 11.4 Pawning

- Drag the **Printer** (`Pawn`, kinematic trigger) onto an item. It only sees colliders on **layer 3 `DeskObject`** (`pawnableLayers` = 8 in prefab and scene). It then needs `IPawnable` and `CanBePawned`; else hint `CannotPawn`. Pays `MoneyService.Add(PawnValue, "pawn")` then calls `OnPawned()`.
- `IPawnable { int PawnValue; bool CanBePawned; void OnPawned(); }`. Implementors: `ClickTask` (+Picture), `HoldTask` (Egg Timer, Power Bank), `WireTask`, `DeskLaptopOpener`, `CardBox`, `TicketStack`, `JunkItem`, `CardItem`.
- **Every `OnPawned` deactivates (`SetActive(false)`); nothing is destroyed.** (The Phone plan's "replace Destroy()" step is already done; buyback still needs `IRestorable` + a ledger.)
- Five prefabs still carry a stale serialized `pawnValue: 20` that no script reads.
- Pawn proceeds are taxed and count toward run stats.
- Pawn-everything total today ≈ $457 (Egg 150, Power Bank 45, Picture 40, Wire 15, Laptop 120, Card Box 48, Nine stack 24, Backpack 15), if the unverified layers are 3.

### 11.5 Phone (current, pre-rework)

- Rings every uniform 30–60 s (×0.5 interval under Robocalls). While ringing: `TickMultiplier = 1.5`. Clicking answers; the ring stops 1.5 s later. Unanswered rings stop after 10 s.
- Cost: ignore ≈ 5 s of clock; answer at once ≈ 0.75 s. Gated by `AnnoyanceManager` (if present).
- `Silence(d)` (Cable Eater today) stops a ring and blocks new ones; `Unsilence()` ends early.

### 11.6 Cat

- `CatStateMachine` states **Wandering → SittingIdle → Wandering**; while Wandering, touching an interactable rolls `catBlockChance` 0.4 → **SittingBlocking** for 3–6 s (`SetInteractable(false, CatBlocking)`); **Dragged** while the player drags it. Sit 6–14 s (Idle/Lying families, nested Sleep 3–6 s). Moves by transform, 1.5 u/s.
- Not routed through `AnnoyanceManager` by decision D8. It can block anything with a `PointerReceiver`, including the Phone and the Printer.

---

## 12. Cards

### 12.1 Data model

- `CardSO` (`Create > Overtime > Cards > Normal`): `cardName, image, description, rarity, weight, pawnValue, hideUntilCollected, collectionRegisterAs`. A plain `CardSO` (no effect) is a valid collectible.
- `Rarity { C, R, SR, UR }` (weights 60/25/12/3). Older docs say Common/Uncommon/Rare/Legendary — **"Rare" means a different tier there**; use the code names.
- `EffectCardSO` (abstract): `_isDebuff` (`[FormerlySerializedAs("isDebuff")]`), `BlocksPawning` (false by default), `OnPlaced(CardItem)`, `OnRemoved(CardItem)`, `OnPawned`. Each timed subclass declares its own `duration`.

| Class | Fields (defaults) | Effect | Ends when |
|---|---|---|---|
| `ClockSpeedCardSO` | duration 15, tickMultiplier 0.75 | writes `TickMultiplier` | timer |
| `DoNotDisturbCardSO` | duration 30 | `Phone.Silence` / `Unsilence` | timer |
| `RobocallsCardSO` | duration 30 | ring interval ×0.5 | timer |
| `DisableAppIconCardSO` | duration 15, targetAppName | hides a laptop desktop icon (open app keeps running) | timer |
| `TaskCostModifierCardSO` | target, ×1.5, 20 s | `CostModifierService.SetMultiplier` (overwrite, no stacking) | timer |
| `AuditCardSO` | — | force a bill or double the open one | bill resolved — **`OnRemoved` is empty (bug)** |
| `TaxExemptionCardSO` | — | `SkipNextBill(card)` / withdraw | exempt bill resolved |
| `OvertimeCardSO` | ×2 | next mining vein multiplier | vein banked |
| `ButterfingersCardSO` | — | next vein pays 0 | vein banked |
| `InsiderTipCardSO` | — | pre-reveal one window on next ticket | ticket opened |
| `BilliardsExtraShotCardSO` (Chalk Up) | extraShots 1 | +shots next round | round starts |
| `BilliardsTrickShotCardSO` | — | end-position preview for next shot | shot fired |
| `BilliardsCueSlipCardSO` (debuff) | powerJitter 0.15 | random power ± on next shot (RngService) | shot fired |
| `CreepyJesterCardSO` | artPool, interval 20 s | swaps art each tick; last art → `PlayerPrefs.DeleteAll()` + `GameOver(CreepyJesterLoss)` | never by itself |
| `FinalCountdownCardSO`, `SetCompletionCardSO` (+ trackers) | — | §12.5 | — |

### 12.2 Lifecycle

1. **Buy a pack** at the Card Box: needs `CardHandManager.HasRoom` (placed < `handCap` 5) else hint `HandFull`; costs `round(35 × CostModifier[CardPack])`.
2. **Roll** 3 cards independently (duplicates possible): rarity by weight, then a card of that rarity by `CardSO.weight` (`RngService`). Empty rarity → whole pool. All-zero weights → always the first card.
3. **Reveal panel:** pack image, then 3 face-down slots resolved in any order. Before reveal: **Sell $10** (works on debuffs too). After reveal: **Place** always; **Collection** and **Sell (pawnValue)** hidden for debuffs.
4. **Place** → a pooled `CardItem` (7 fixed instances in the scene, never instantiated) → `Setup(card)` → `OnPlaced`. `TryPlaceCard` checks only the pool, **not the hand cap**, so a pack bought at 4 placed can reach 7.
5. **Effect ends** (`EndEffect`, guarded by `_hasEnded`): `OnRemoved`, card **flips face-down and stays**, still using a hand slot, and its type is registered in the Collection. Clicking a face-up card opens `CardPanel` (pauses the game).
6. **Pawn** the card (only way to clear a slot): releases the slot, ends the effect early if still running (**no collection credit**), pays full `pawnValue` even for spent cards, returns to pool.
7. Scene reload: `CardItem.OnDestroy` runs `OnRemoved` once if needed; every effect static is cleared on `sceneLoaded`.

`Collection` is a registry ("ever completed this type"), not storage. `CardCollectionPanel` shows a paged binder; `hideUntilCollected` cards stay hidden until collected.

### 12.3 Card pool (29 assets in `Resources/CardSO`; only 3 were readable in this snapshot)

| Card (display name) | Asset | Rarity / weight / pawn | Effect |
|---|---|---|---|
| Snail Speed | SnailSpeed | C / 11 / 2 | clock ×0.75, timed |
| Clock Up! | ClockUp | R / 10 / 6 | clock ×1.5, timed |
| Time Scarab | TimeScarab | SR / 18 / 14 | clock ×0 (stops), timed — I2/I5 question |
| **Cable Eater** | CableEater | R / 20 / 6 [verified] | today: buff, silences phone 30 s. **Planned: debuff, forces phone off 30 s** |
| Spam Robocall | Robocalls | R / 10 / 6 | debuff, rings ×2 |
| Fvck The Tax | TaxExemption | SR / 18 / 28 | skip next bill |
| Tax Increase | Audit | SR / 10 / 28 | debuff, force/double a bill |
| Lucky Ruby | LuckyRuby | C / 11 / 4 | next vein ×2 (Overtime) |
| Butter Finger | Butterfingers | C / 7 / 4 | debuff, next vein slips |
| Ticket Hunter | InsiderTip | C / 10 / 4 | one window pre-revealed |
| Ticket Up Sale | TicketCostModifier | SR / 10 / 6 | tickets ×1.5, 20 s |
| Rarest Card | CardCostModifier | R / 10 / 6 | pack cost up, 20 s |
| Bet More Money | BlackjackCostModifier | C / 9 / 3 | Blackjack ante up (target to verify) |
| High Risk, High Reward | BilliardsCostModifier | C / 12 / 0 | Billiards bet charge up (`Target.Billiards`) |
| No More Mining | DisableMining | R / 8 / 6 | hide Mining icon |
| No More Gambling | `DisableBlackjack'` (apostrophe in asset name) | C / 8 / 2 | hide Blackjack icon |
| Not Valid 8-Ball | DisableBilliards | C / 8 / 2 [verified] | debuff, hide Billiards icon 30 s |
| Close The Shop | DisableShop | R / 12 / 6 [verified] | debuff, hides "Shop" icon — **no-op until the shop exists** |
| Final Countdown | FinalCountdown | SR / 16 / 5 | §12.5 |
| Mei – The Tester | Mei | R / 8 / 5 | Set Completion anchor |
| Han – The Jester | Han | C / 8 / 2 | Set Completion piece |
| Han – The Pierrot | CreepyJester | C / 4 / 2 | hidden; registers as Han; 60 s → wipe + loss |
| Ari – The Gambler | Ari | C / 8 / 3 | Set Completion piece |
| Sybil – The Caster | Sybil | C / 8 / 2 | Set Completion piece |
| CtrlZ – The Boy Skull | CtrlZ | C / 8 / 2 | Set Completion piece |
| Kamen Soldier DK | DeeKay | SR / 19 / 35 | collectible |
| The Forbidden Steve | Steve | SR / 19 / 35 | collectible |
| Blue-Eyes Fool Dragon | BEFDragon | UR / 50 / 75 | collectible |
| Funky Balloon Magician | FBMagician | UR / 50 / 75 | collectible |

Billiards cards (Chalk Up, Trick Shot, Cue Slip) have **code but no assets** — Han creates them (suggested C / R / C-debuff with low pawn value).

### 12.4 Card notes

- Debuffs bypass the cap only in the sense that placement never checks the cap; the cap gates **buying**. Keeping 5 cards on the desk dodges new debuffs (accepted trade-off).
- Card Box pawns for $48 against a $35 pack; spent cards pawn for full value. Cash-flow positive by design? Revisit in the shop pass.
- `CardItem.EndEffect` registers `_data`, while reveal-to-Collection registers `CollectionTarget` — inconsistent for alias cards (Jester → Han).

### 12.5 Win/lose cards (`WinConditionCard/`, described from GAME_STRUCTURE — these files were not readable in this snapshot)

- **Final Countdown** (`FinalCountdownTracker`: None → Armed → Triggered). First placement arms `ClockService.ArmFinalCountdownIntercept(180)` and **locks the Meter**. When the clock hits 0 it resets to 180 s, unlocks the Meter and plays the intro (clock paused during it). A second card placed while **Armed = loss**, while **Triggered = win**. Pawning the armed card is a free escape. `ClockService.Spend` (ITRS ignore, Blackjack ante) bypasses the intercept and can still kill an armed run.
- **Set Completion / "MASCH"** (anchor **Mei**): wins when Mei plus one card of each group is on the desk: {Han or Creepy Jester}, {Ari}, {Sybil}, {CtrlZ} — exactly 5 cards = the whole hand at `handCap` 5.
- **Creepy Jester**: §12.1. `artPool` length 0 or 1 throws on the first tick.

---

## 13. Laptop and apps

### 13.1 Shell

- `DeskLaptopOpener` (desk sprite): opens the panel unless `LaptopController.Disabled` (hint `LaptopNoPower`). Pawnable for $120.
- `LaptopController`: one app at a time. `RequestOpen(app)` runs a switch coroutine: overlay → close current (`OnAppClosed`) → wait `laptopSwitchTime` **1 s (also on the first open from home)** → open. `CloseCurrentApp` is free and cancels a pending switch. `Disabled` is only written by `WireTask`.
- `LaptopHome` + `DesktopIconView`: icons need a **double click**; apps are matched by string (`LaptopApp.AppName == DesktopIconView.appName`). `DesktopIconView.SetVisible(appName, bool)` works before the icon wakes (static `s_pendingHidden`, a set — not counted, so two overlapping hide cards end at the first expiry).
- `CloseAppButton`: closes the app or the whole laptop.
- Leaving an app runs `OnAppClosed`: Mining forfeits unbanked ore, Blackjack forfeits the hand and resets Hot Table, Billiards forfeits only a rolling shot.

### 13.2 Mining (`MiningApp`, `PickaxeView`)

- **Input:** hold the pickaxe, drag it into the ore and back out = one swing; `hitCooldown` **0.7 s** (literal on `PickaxeView`, not GameConfig). No hold mechanic any more (landmine L8, design question open).
- **Vein:** length uniform **6–10**, shown up front by status nodes (L7 fixed). Blocks always in order: Dirt 1 HP, Wood 2, Stone 2, Coal 3, Copper 3, Iron 4, Quartz 4, Gold 5, Diamond 6, Ruby 7. Payout per block from `blockPayout`; vein totals L6 $29 / L7 $41 / L8 $56 / L9 $73 / L10 $94 (mean **$58.6, 25 hits**).
- **Banking:** paid only when the vein completes; closing/switching mid-vein zeroes the unbanked ore (but block damage persists, so reopening resumes the vein with nothing banked). Respawn 1 s.
- Overtime (×2) and Butterfingers (slip → 0) queue through statics, reset on scene load. "Received" label ignores the Overtime multiplier.
- **Throughput ceiling $3.17/s** (perfect 0.7 s rhythm), ~$2.25/s at 1 swing/s; $2.53/s after 20% tax.

### 13.3 Blackjack (`BlackjackApp`, `BlackjackGame`)

- Rules: fresh 52-card deck per hand (seeded shuffle), dealer stands on all 17s, double on first two cards only, no split/insurance/surrender, **no dealer peek**. Naturals resolve on deal.
- Ante **20 s of clock** × `CostModifier[Blackjack]`; deal/double need clock ≥ ante + **15 s** margin (35 s). Double spends a second ante.
- Payouts: win/dealer bust **$100**, push **$25**, natural **$130** (× 2 if doubled).
- **Hot Table:** payout × (1 + min(streak × 0.10, 0.50)); streak +1 per win, kept on push, reset on loss/bust and **on closing or switching the app**. Money only.
- Closing during the 0.6 s reveal forfeits even a won hand (payout is credited after the delay).
- EV: about **+$30–35 per hand** pre-tax (basic vs money-optimal play), +15% with Hot Table; at 4–6 s per hand that is **~$5–9/s**, roughly 2–3× Mining. HANDOFF D6 "Blackjack vs Mining" is **not closed** by the hotfix; the plan treats it as needing playtest.
- Dead code: `BlackjackApp.Banned` is never set; `BlackjackGame.Payout` is unused.

### 13.4 Billiards (built 2026-10-13 → 16 by Ari)

**Concept:** pre-made table puzzles. Pick a bet ($10–40) and a difficulty; pot the **ball queue in order** within limited shots. Easy = par + 4 shots (full ghost-ball assist), Medium = par + 2 (contact point), Hard = par (direction only).

| File | Role |
|---|---|
| `BilliardsApp` | `LaptopApp` + state machine Racked / Aiming / Rolling / Won / Lost; bet, charge, payout, refund; fixed-step driver (≤ 8 steps/frame) |
| `BilliardsSim` | deterministic pure-C# physics, fixed step **1/120 s**; `StateHash()` (FNV-1a) |
| `BilliardsReferee` | rules → `Verdict` |
| `BilliardsRound` | one table being played (also used headless) |
| `BilliardsPayout` | EV payout formula |
| `BilliardsPreset` (+`Library`, `Verifier`, validator) | table data, JSON loading, replay + robustness check |
| `BilliardsPendingEffects` | static card grants, reset on scene load |
| `BilliardsRoundLog` / `BilliardsDebug` | dev telemetry CSV / debug switches (both survive restart on purpose) |
| `FeltMapper`, `TableMirror`, `BallSprites` | coordinate mapping, view-only mirror, sprite names |
| `View/*` | `BallView`, `BallQueueView`, `BallSpriteSet`, `AimAssist`/`AimPredictor`, `CueAimController`, `PowerBarView`, `TableView`, `BilliardsArt`, `BilliardsDebugOverlay` |

**Physics:** table space 2 × 1 (preset coords 0..1 stretched ×2; **angles only in table space**). Step: move → rolling friction (linear decel 1.2, stop below 0.01) → ball-ball (≤ 4 passes, exact touch instant, equal masses, restitution 0.95, no spin) → cushions (normal-only restitution 0.75, so `tan out = 0.75 tan in`) and jaw points → ≤ 3 separation passes → pockets (centre inside radius) → settle or 20 s cap. Re-spot searches 12 rings around the spot. **Determinism holds within one runtime only** (Editor vs .NET 8 differ); WebGL verification has not been run.

**Rules:** foul = scratch, no contact, or (strict) first contact ≠ head ball. Legal pot = no foul, head ball, called pocket if any (combos in drop order count). Illegal non-final pot → re-spot. Final ball potted early/foul/wrong pocket → fail. Forbidden ball potted → fail. Out of shots → fail. Blockers (not in queue) may be potted harmlessly.

**Money:** charge `round(bet × CostModifier[Billiards])`; payout from the **base** bet. On a win: stake back with reason "stake refund" (untaxed) + profit (taxed). Leaving while Racked/Aiming keeps the bet; leaving while Rolling forfeits. Pawning the laptop mid-round loses the stake.
`payout = round(bet × (1 + targetRate[d] × T / betMax) / assumedWinRate[d])`, `T = (par + shotsGranted)/2 × 7 s`. Multiples (E/M/H): par 1 → 2.10/3.15/3.40; par 2 → 2.41/3.96/4.80; par 4 → 3.03/5.58/7.60; par 5 → 3.34/6.38/9.00.

**Tables** (`Resources/BilliardsPresets/*.json`; 1 of 10 read here, rest per BILLIARDS_DECISIONS):

| id | Name | Tests | par | shots E/M/H | worst-shot robustness |
|---|---|---|---|---|---|
| straight_pot_01 | Straight Shot | straight pots | 2 | 6/4/2 | 97% |
| cut_shot_01 | Cut It Fine | cuts | 2 | 6/4/2 | 97% |
| bank_shot_01 | Off the Rail | bank required | 1 | 5/3/1 | 93% |
| combo_01 | Two for One | combo | 2 | 6/4/2 | 100% |
| blocker_01 | Dodge the Blocker | blocker on line | 2 | 6/4/2 | 80% |
| called_pocket_01 | Call Your Pocket | called pockets | 2 | 6/4/2 | 100% |
| eight_trap_01 | Eight Ball Trap | final ball trap | 2 | 6/4/2 | 97% |
| long_five_01 | Long Table | 5 balls, blocker, called final | 5 | 9/7/5 | 93% |
| four_in_order_01 | Four in Order | queue 1,3,5,8; 8 called TR | 4 | 8/6/4 | 97% |
| straight_test | test table | self-tests; **still in the random pool** (Ari's call) | 1 | 5/3/1 | 100% |

**Aim:** press on the felt, drag away from the shot, release; power = pull / (0.6 × felt height); < 5% power or right-click cancels. Every shot is quantized (0.1°, 0.01 power) so replays are exact. Mirror (none/X/Y/XY) is view-only, so one stored solution covers all four. Felt 588×294 canvas units, window 668×474; balls drawn 36 px from 42 px sprites.

**Balance status:** model numbers only (no real playtest rounds). Typical player at $40: Easy +$1.6/s, Medium +$4.3/s, Hard +$4.4/s; choosing difficulty after seeing the table adds ~$1/s. BILLIARDS_BALANCE's recommendation (0.6/1.8/2.3) is **overridden**: retune so Easy/Med/Hard ≈ 0.6×/1.0×/1.3× Mining ≈ **1.9/3.2/4.1 $/s** after measuring real rounds (pending). Per-table `winRates` in preset JSON proposed, not built. **No license gate exists yet** (Billiards is free today; licenses come with the shop).

---

## 14. Economy summary

| Source | Rate (pre-tax unless noted) | Notes |
|---|---|---|
| Egg Timer | **+2.0 s/s net** (ideal), untaxed | exploit L1; planned cooldown → +0.71 s/s |
| Bear / Picture | ≤ $1.67/s each (≈ $1.0–1.2/s real) | independent locks, can alternate |
| Power Bank | $0.76/s | |
| Wire puzzle | $20 once | gates laptop |
| Mining | ≤ $3.17/s ($2.53 after tax) | free, no capital |
| Blackjack | ~$5–9/s | needs clock ≥ 35 s, burns ~31 s per hand |
| Billiards | ~$1.6 / $4.3 / $4.4 per s (E/M/H, model) | needs ≤ $40 capital |
| Triple Match | −$2.77 per ticket | 53% chance of profit |
| Lucky Nine | −$7.24 per ticket (−$2.87 stopping early) | 26% chance of profit |
| Phone ring | costs ≈ 0.75–5 s of clock each | ~9% extra drain if always ignored |
| ITRS | 20% of gross income, min $20 / 45 s | ignore = 2 s per $ |

Baseline to beat: **$3/s**. Any future paid earner (licenses in the shop plan) must beat free Mining or do something Mining can't.

---

## 15. Known issues (open)

Ranked by impact. IDs: `L#` landmine pass (2026-08-31), `P#` Phone-plan problem list, `N#` 2026-10-17 audit. When you fix one, move it to §16 in the same commit.

| # | Sev | Issue | Where | Fix / owner |
|---|---|---|---|---|
| L1 | **High** | Egg Timer +2 s/s net, untaxed, no limit — holding it alone wins in ~240 s | `crankTime 1`, `EggTimerTask` | 0.75 s cooldown in `EggTimerTask` (Batch 2) |
| L4b | **High** | Pawning the wire puzzle sets `LaptopController.Disabled = true` forever (all apps dead) | `WireTask.OnPawned` | make it unpawnable or set `false`; plan makes it unpawnable |
| L4a | High | Laptop pawnable ($120) with no buyback yet → unwinnable-but-running run | `DeskLaptopOpener.CanBePawned` | intended once Buyback ships (Batch 4); until then risky |
| L5 / P1 | High | `TickMultiplier` last-write-wins (Phone vs clock cards) | `ClockService`, `Phone`, `ClockSpeedCardSO` | keyed `SpeedModifiers` (Batch 1) |
| I8 / P5 | Med | `AnnoyanceManager` probably absent from the scene → phone/bill overlap and bills overwrite each other; Audit bypasses it anyway | `AnnoyanceManager`, `ITRSService.ForceBillNow` | verify in Editor; plan replaces with `CallScheduler` and moves the pending-bill guard into ITRS |
| N5 | Med | Audit card: `OnRemoved` empty → after pawning, its handler ends a *different* pooled card later | `AuditCardSO` | unsubscribe in `OnRemoved` |
| P15 | Med | `ClockService.Spend` bypasses the Final Countdown intercept | `ClockService.Spend` | route through the same check as the tick |
| I5 | Med | `CardPanel`/`TutorialPanel` freeze `timeScale`; uGUI (Meter Feed) may still act | `GameManager.PauseGame` | verify panel raycast coverage; plan's `PauseLock` must coexist |
| — | Med | Blackjack ≈ 2–3× Mining after the hotfix | `GameConfig` Blackjack | playtest, then tune `blackjackWinPayout` / ante / cycle time |
| — | Med | Billiards payouts not retuned (targets still 1.5/3/4) | `billiardsTargetRate` | measure 20 real rounds → 1.9/3.2/4.1 $/s |
| N2 | Med | Consumers cache `MoneyService.Current` in their `Start`; `Meter` subscribes in `OnEnable` | `Meter`, `ITRSService`, `RunStatsService` | safe only with the execution order in §5.4; move to `Start`/lazy init |
| N3 | Med | Pawning, card sales (and future buyback refunds) count as income for ITRS and run stats | `ITRSService`, `RunStatsService` | non-income reason class before the shop/buyback |
| N4 | Med | `CostModifierService` overwrite: two cost cards on one target, first expiry resets both | `CostModifierService` | keyed multipliers |
| N6 | Med | Cat can block the Phone (unanswerable ring) and anything else | `CatController` | exclude Phone/TaxNotice or gate |
| N7 | Med | Hand cap only gates buying; a pack can overfill to 7 | `CardPackRevealPanel.TryPlaceCard` | check per placement or reserve slots |
| — | Med | `PointerReceiver.SetInteractable` is a bool: a Cat block ending re-enables a solved wire puzzle or a cooling Power Bank | `PointerReceiver` | refcount / reason set |
| P7 | Low | Phone and TaxNotice share one AudioSource | scene | give each its own source |
| P8 | Low | Icon hiding is a set, not counted (overlapping hide cards) | `DesktopIconView` | plan's `IconGate` |
| P9 | Low | "Close The Shop" card targets a non-existent icon | `DisableShop.asset` | Shop app (Batch 2) |
| L8 | Design | Mining is drag/hit, not hold — the Phone lost its original job | `MiningApp` | Ari's call (hold vs re-found phone role); plan re-founds the phone as calls/shop |
| L10 | Low | `ultrRareWeight` typo; docs used other rarity names | `GameConfig.cs` | rename with `[FormerlySerializedAs]` |
| L12a | Low | Vestigial `InputManager.FocusRoot` | `InputManager` | delete |
| — | Low | `TicketSymbolConfig` lives in `TicketSymbolPool.cs` | `Config/` | rename file (keep `.meta`) |
| — | Low | Lucky Nine pays more if you stop early (best-count rule) | `ScratchTicket.ComputeCurrentReward` | decide: keep as skill, or pay the best symbol |
| — | Low | `GameOver` has no re-entry guard | `GameManager.GameOver` | early return |
| P18 | Low | Creepy Jester `PlayerPrefs.DeleteAll()` wipes collection and run history too; `artPool` index unguarded | `CreepyJesterCardSO` | confirm scope with Han; guard index |
| — | Low | Cheat menu ships in builds; negative money/time cheats do nothing | `CheatMenu*` | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` |
| N10 | Low | `AddSeconds/Spend` discard `reason` (no clock balance log) | `ClockService` | `OnClockChanged(delta, reason)` event |
| N14 | Low | Blackjack forfeits a won hand closed during the reveal | `BlackjackApp.Resolve` | credit before animating |
| — | Low | Tax panel shows a stale amount after Audit doubles the open bill | `Tax` | refresh on `OnAssessmentIssued` |
| — | Low | Camera keeps scrolling if a held arrow is deactivated | `CameraVerticalScroller` | clear `_direction` on disable |
| — | Low | Collection saved by asset name, comma-joined | `CardCollectionManager` | never rename card assets; avoid commas |
| — | Low | `CardItem` timed `Invoke` not cancelled on pawn/reuse [unverified risk] | `CardItem` | `CancelInvoke()` in `Setup`/`OnPawned` |
| — | Low | Stale C# defaults on ~20 GameConfig fields (§10) | `GameConfig.cs` | update defaults to the live values |
| — | Info | Dead code: `Phone.OnPawned`, `TaxNotice.OnPawned`, `BlackjackApp.Banned`, `BlackjackGame.Payout`, unused events `MiningApp.OnNextVeinEffectResolved`, `ScratchTicket.OnPreRevealDelivered`; stale `pawnValue: 20` in 5 prefabs | various | clean when touched |
| — | Info | ~45 `Instance?.` uses against the `!= null` convention | mostly `HintService` | clean when touched |

---

## 16. Fixed issues ledger

| ID | Was | Fixed by |
|---|---|---|
| L2 | Lucky Nine always profitable (EV $203 on $55) | `luckyNineBaseReward` 110 → 26 (hotfix 2026-10-17). Note: still can't pay $0 (min $16) |
| L3 | Phone pawnable | `Phone` no longer implements `IPawnable` |
| L6 | `handCap` 3 in asset | asset 5; `HasRoom` reads config |
| L7 | Vein length invisible | status nodes show the length |
| L9 | All pawn values $20 | per-item pawn fields in `GameConfig` |
| L11 | Fields missing from asset | asset fully serialized |
| L12 (part) | `hitDropChance` stale key; Egg Timer bypassed `AddSeconds` | removed; `HoldTask` uses `AddSeconds` |
| GS | Laptop apps had null service refs | apps use `.Instance` (2026-10-12) |
| GS | `LaptopController._switching` soft-lock | `CancelSwitch` (2026-10-12) |
| GS | Pawn-after-place exploit (Overtime, Butterfingers, InsiderTip, TaxExemption) | card owns its effect; `OnRemoved` withdraws (Billiards Phase 8) — **except Audit** |
| GS | Mining/Ticket statics survived restart | reset on `sceneLoaded` |
| GS | Mining vein respawn soft-lock | `OnEnable` restarts a finished vein |
| GS | ClickTask first click always completed | required clicks rolled in `Start` |
| GS | Card weights all 0 | weights set (3 assets verified, rest per ITEMS_INVENTORY) |
| GS | `CostModifier` couldn't reach Blackjack/Billiards | `Target { Ticket, CardPack, Blackjack, Billiards }` |
| P11 | Pawned objects `Destroy()`ed | every `OnPawned` deactivates |
| Hotfix | Blackjack lost money (40/20/50) | 100/25/130 + Hot Table; pawn values Laptop 120, Card Box 48, Nine stack 24; Triple stack and Bear unpawnable |

---

## 17. Roadmap

### 17.1 Built

| When | What |
|---|---|
| Jam (Jul 2026) | Clock, Meter, money, Mining, Blackjack, ITRS, phone ring, Egg Timer, Pawn, first Cat and tickets |
| Extended S1 (2026-08-21/22) | Ticket Stack rebuild, card system rebuild, click/drag input, `DeskObjectFocus`, `IPawnable.OnPawned` |
| Extended S2 (2026-08-31) | 7 more card effects, Cat state machine, `WireTask`, vertical camera, `CostModifierService`, collection panel |
| Han, to 2026-10-08 | win/lose cards and Timeline cutscenes, run summary + history, DOTween card flip, cheat menu, click VFX, 29 card assets, Power Bank / Picture, hints + tutorial, run rating |
| Ari, 2026-10-12 → 16 | laptop fixes; **Billiards Phases 1–9** with editor tools, 10 tables, 105 self-tests, 3 card classes |
| 2026-10-17 | Balance hotfix (Batch 0) |

### 17.2 In flight / waiting on a person

- **Billiards sign-off (Ari):** 20 real rounds (> 1 player), measure seconds per shot, retune to 1.9/3.2/4.1 $/s, re-run the Balance Report, decide `straight_test` and per-table `winRates`, WebGL determinism check (postponed).
- **Billiards card assets (Han):** Chalk Up, Trick Shot, Cue Slip.
- **Merge `ari/pool-table` → `main`.**
- **Phone/Shop plan Phase 0:** Q1–Q9 open with defaults; Han's art plan and scene coordination.

### 17.3 Planned: Phone, Shop, Delivery, Buyback

Full spec: `PHONE_SHOP_DELIVERY_IMPLEMENTATION.md` (phases, checkpoints, pure-C# appendices A1–A9, new GameConfig fields) and `PHONE_DELIVERY_DECISIONS.md` (decision log). One branch per batch, each from `main` after the previous merge.

| Batch | Branch | Contents |
|---|---|---|
| 0 | `Ari/PawnHotfix` | balance hotfix — **applied** |
| 1 | `Ari/PhoneShell` | `PauseLock` (counted clock pause), `SpeedModifiers`, `ClockService.GameDelta/GameTime`; device layer (blurred desk, phone + laptop side by side, small device clocks, Meter only on desk, laptop locked during beneficial calls); smartphone: power button (5 s boot, clock running), caller ID calls (Telemarketer 40 / Client 25 / Unknown 25 / ITRS 10), call queue with 12 s gap, banners (max 3, 4 s), Inbox, Taxes app with 30 s deadline; delete `AnnoyanceManager`; Cable Eater becomes a debuff (phone forced off 30 s) |
| 2 | `Ari/Shop` | item catalog ScriptableObject (replaces layer-based pawnability), Shop app with cart/checkout, licenses (Blackjack $60, Billiards $60, via Inbox "Activate", not pawnable), catalog prices (Power Bank $25, Card Box $60, Lucky Nine stack $30), pawn = floor(80% × price paid), `IconGate`, **Egg Timer 0.75 s cooldown** |
| 3 | `Ari/Delivery` | door beside the desk, mailbox higher up (camera scroll), doorbell, unboxing, shipping (Standard free 20 s / Express $8 5 s / "leave it opened & ready" +$3), tax letters, junk mail, tutorial panels (extend the existing `TutorialPanel`/`HintService`) |
| 4 | `Ari/Buyback` | `IRestorable`, pawn ledger written before `OnPawned`, buyback ×1.10 → ×1.25 over 90 s game time, Buyback card |
| 5 | playtest | ≥ 10 full runs; decide win target / run length (I1 amendment if needed) |

Starting kit (decided): Laptop (+ Mining), wire puzzle, Egg Timer, Bear, Picture, Triple Match stack, Phone, Printer, Meter, Collection. Starting items are unpawnable except the Laptop; the Phone never.

Branch-name case: plan says `Ari/…`, the real branch is `ari/pool-table`. Pick one before Batch 1.

### 17.4 Backlog / ideas (not scheduled)

Coffee Mug, Dish Rack, achievements, settings menu, daily challenge (fixed seed), localization VI/EN, New Game+, roguelite/prestige run structure (exploring only), card-collection reward or win (needs I1 amendment), Lucky Nine jackpot win (needs I1 amendment), Billiards click-and-slider aim, sound clips, real ball/cue art. Cut: bank/loans, porch pirate, courier calls.

### 17.5 Open questions

| Question | Status |
|---|---|
| Win target 600 s vs shorter runs | after playtests |
| Mining: restore hold, or re-found the Phone's role (L8) | Ari, unanswered |
| Time Scarab (×0) vs I2/I5 | needs owner decision |
| Laptop pawnable before Buyback exists | decided pawnable; interim risk accepted? |
| Egg Timer pawnable? (prefab layer 3 vs "layer 0" in old inventory) | verify in Editor |
| Phone plan Q1–Q9 (Ignore button removal, catalog display, client gig $22 or 20% coupon, device blur, tutorial pausing, junk mail timing, unboxing, Buyback card, art) | defaults apply until answered |
| Approve first-pass shop prices for playtesting | open |
| Lucky Nine "stop early" behaviour | open |
| Jester wipe scope | confirm with Han |
| `dragThreshold` feel | untuned |

---

## 18. Decision log

Condensed; newest last. Full rationale lives in the plan docs.

| ID | Date | Decision | Status |
|---|---|---|---|
| J1–J3 | jam | Blackjack is a laptop app; Dead Battery split into per-app disable cards; `laptopSwitchTime`, clock margin | final |
| J4–J5 | jam | Ari owns the laptop/systems/GameConfig; Han owns tickets, cards, desk content, art | final (owner-reviews) |
| J8 | jam | ITRS scales with earnings, not time | final |
| J13–J15 | jam | CPU texture scratch mask; no coroutines in the economy tick; one seeded RNG | final |
| D1 | 08-22 | Cards never grant money/time directly | final |
| D2 | 08-22 | `ThrowTask` cut → Billiards (vector maths, no physics engine) | built |
| D4 | 08-22 | Ticket rebuild: symbols at purchase, unified formula, clock never pauses while scratching | final |
| E3–E8 | 08-22 | Pack of 3 resolved independently; debuffs Place-only; flip-and-linger lifecycle; Collection = registry; early pawn = no credit; `handCap` 5; pool 7; `OnRemoved` exactly once | final |
| E9 | 08-22 | Click vs drag via `dragThreshold` | final (feel untuned) |
| D7 | 08-31 | I9 amended: vertical camera scroll | final |
| D8 | 08-31 | Cat exempt from `AnnoyanceManager` | final |
| D9 | 08-31 | `WireTask` replaces `DragTask` | final |
| D10 | 08-31 | `CostModifierService` per target | final |
| L2 | 10-17 | Lucky Nine fixed by base reward 26 (not `minMatchToPay`) | applied |
| L3 / P12 | 10-17 | Phone never pawnable; Laptop pawnable ($150 catalog → $120) | applied |
| B1–B5 | 10-12 | Billiards = ordered-queue puzzles; strict rules; view-only mirror; deterministic sim; append-only enums | built |
| B6 | 10-13 | Leaving while Aiming keeps the bet; only a rolling shot forfeits | built |
| B7 | 10-14 | Tax counts Billiards profit only (stake refund reason) | built |
| B10 | 10-14 | EV-based payout formula | built; values pending |
| B13 | 10-14/15 | Physics: ball 0.0614, corner 0.12, side 0.09; validator ratios; re-verify after changes | final |
| B21 | 10-16 | Exact touch-instant ball collisions | final |
| B25–B26 | 10-16 | Billiards card classes; pending-effects contract applied to all card queues | built (assets pending) |
| P1–P3 | 10-17 | Calls with caller ID, weights 40/25/25/10; beneficial calls stop the clock; declining is free; one call at a time, 12 s gap | planned |
| P5 | 10-17 | Power button, 5 s boot (tune gig value first, then boot time, then cut) | planned |
| P6 | 10-17 | Cable Eater → debuff (phone forced off 30 s); Do Not Disturb retired; asset name kept | planned |
| P7–P9 | 10-17 | Apps: Shop, Call, Inbox, Taxes; bills in app + mail, pay one cancels other, penalty once after 30 s; only calls interrupt | planned |
| P10 | 10-17 | Device layer: blur, side by side, Meter only on desk | planned |
| P11–P14 | 10-17 | Licenses digital and permanent; pawn 80%; starting items unpawnable except Laptop; buyback 110 → 125%; first-pass prices | planned |
| P15–P16 | 10-17 | Shipping options; door + mailbox via scroll; junk mail | planned |
| P18 | 10-17 | Egg Timer 0.75 s cooldown | planned (Batch 2) |
| P19–P21 | 10-17 | Blackjack 100/25/130 + Hot Table; Lucky Nine 26 | applied |
| P22 | 10-17 | Billiards: don't adopt 0.6/1.8/2.3; target 1.9/3.2/4.1 $/s after measuring | pending |
| P24 | 10-17 | Guided tutorial, one panel per new mechanic | planned (reuse existing hint/tutorial code) |
| P29 | 10-17 | Remove `AnnoyanceManager`; `CallScheduler` replaces it | planned |
| P30 | 10-17 | Batch order 0 → 4, one branch each from `main` | final |

---

## 19. Tools and testing

| Tool | Where | Use |
|---|---|---|
| Billiards Table Editor | `Tools/OverTimer/Billiards/Table Editor` | author tables, record shots through the real sim, auto-solve, export (refuses unless solved + verified) |
| Solver | button in the Table Editor | brute force 0.5° × 6 powers, iterative deepening, robustness ranking |
| Verify Presets | `Tools/OverTimer/Billiards/Verify Presets` (also cheat menu in builds) | replay every stored solution + 30 jittered trials per shot; knife-edge < 70% |
| Self-tests | `Tools/OverTimer/Billiards/Run Self-Tests` | 105 EditMode checks (sim, referee, payout, pending effects, config, tables) |
| Balance Report | `Tools/OverTimer/Billiards/Balance Report` | noisy-bot model (~6 min); **overwrites `BILLIARDS_BALANCE.md`** — paste the summary into §13.4 afterwards |
| Round log | `Tools/OverTimer/Billiards/Round Log Summary`, `Reveal Round Log File` | dev-only CSV telemetry |
| Debug menu | `Tools/OverTimer/Billiards/Toggle Debug Overlay`, `Mirror/*` | same switches as the cheat menu |
| Preset formatter | `Tools/OverTimer/Billiards/Reformat preset files` | rewrite JSON compactly |
| Cheat menu | in game (§8.4) | money, time, cards, endings, Billiards switches |
| Unity MCP | `com.coplaydev.unity-mcp` v10 | agents inspect/edit the scene and run Play mode |

Testing approach:

- Put logic in pure C# and test it headless (`Tools/OverTimer/<Feature>/Run Self-Tests`; the Phone plan adds `Tools/OverTimer/PhoneShop/Run Self-Tests`). Build test configs with `ScriptableObject.CreateInstance<GameConfig>()` and set every field you rely on (defaults are stale).
- Outside Unity, the pure cores can be compiled with `dotnet` 8 against small Unity stubs (used for the Phone plan's CallScheduler checks and Billiards cross-checks). Physics results differ between Editor and .NET, so verify Billiards only in the Editor.
- Play-mode checks via Unity MCP: simulated pointer events, `Application.runInBackground = true` (an unfocused Editor may pause), throwaway in-memory card assets to test placing.
- Every plan phase ends with a **checkpoint report**: done, files changed (prefab vs scene), tests and results, deviations, problems, assumed defaults, evidence, questions. Then wait for Ari's "approved".
- No real playtest data exists yet. Every balance number here is arithmetic or a bot model.

---

## 20. Verification gaps (check in the Editor)

This file was written from a partial snapshot (no `.meta` files, no Play mode, ~8 of ~45 prefabs, 3 of 29 card assets, 1 of 10 Billiards tables, `WinConditionCard/` not readable). Whoever checks an item below: record the answer in the right section and delete the row.

| Check | Why it matters |
|---|---|
| Is `AnnoyanceManager` on a scene object? Is `ITRSService` on `MANAGERS/Services`? | I8, bill overwrite (P5/P6) |
| Egg Timer's layer in the scene (3 or 0) — can the Printer pawn it? | hotfix completeness; "starting items unpawnable" |
| Layers of Power Bank, Picture, ElectricSocket, Backpack | pawnability |
| Do panel prefabs carry `InputBlocker`? Does `CardPanel`/`TutorialPanel` cover the Meter Feed button while paused? | I5 |
| Script execution order still as in §5.4 | Meter/ITRS/RunStats start-money order (N2) |
| Cheat toggle key (scene value 10 = Backslash?) | team knowledge |
| `PickaxeView.hitCooldown` override in the Laptop prefab | Mining throughput |
| Which end reasons have a Timeline asset; does FC intro resume if its director is empty? | soft-lock risk |
| `CutsceneRoot` saved active? | covers the game at start? |
| All 29 card assets: weights, durations, `isDebuff`, Time Scarab = 0? Bet More Money target? | economy, I2/I5 |
| Tier/symbol config assigned on each ticket in the `Tickets` prefab | ticket EV |
| `straight_test` shipping in the random pool | Billiards variety |
| Billiards solutions in a WebGL dev build | determinism |
| `WinConditionCard/*` code matches §12.5 | win/lose rules |

---

## 21. Document history

This file **supersedes** these repo docs (safe to delete once this is merged):

| Old doc | Date | What happened to its content |
|---|---|---|
| `GAME_STRUCTURE.md` | 2026-10-08 (`main` @ `10bb783`) | merged into §5–§13, §15–§16; stale items (handCap 3, card weights 0, no Billiards, `inputBlockers`, `AddTime`) corrected |
| `CODE_GROUND_TRUTH.md` | 2026-08-31 | GameConfig map → §10, API → §6, findings → §15/§16; stale values dropped |
| `ITEMS_INVENTORY.md` | 2026-10-17 | inventory → §11.1, card pool → §12.3; pawn values updated to the hotfix |
| `BILLIARDS_BALANCE.md` | 2026-10-16 (auto-generated) | summary in §13.4; the tool regenerates this file, so it may reappear |
| `BILLIARDS_DECISIONS.md` | 2026-10-13 → 16 | rules, physics, tools and decisions → §13.4, §18, §19 |

Design and plan docs kept in the claude.ai project "Overtimer" (not in the repo): `DESIGN.md` v0.6 (base spec; numbers in its §5 are stale — use §10 here), `OVERTIME_EXTENDED_HANDOFF.md` (D1–D10, Vietnamese), `OVERTIME_LANDMINE_PASS.md` (L1–L12 and fix briefs), `OVERTIME_UML.md`, `BILLIARDS_IMPLEMENTATION.md` (superseded by the code + §13.4), `PHONE_DELIVERY_DECISIONS.md`, `PHONE_SHOP_DELIVERY_IMPLEMENTATION.md` (**active plan**), `OVERTIME_UPDATE_SUMMARY.md` (EN/VI summary for Han).

Known stale spots in the plan docs (fix when the batch starts): they say `GameManager.inputBlockers` (now `InputBlocker` + `InputGate`), propose a new `TutorialPanel` (one exists), list Blackjack 40/20/50 as "today" (hotfix applied), and `PHONE_DELIVERY_DECISIONS` §2.4 still shows the 1.0 s Egg Timer proposal (locked value is 0.75 s). The Phone plan's P11 "replace `Destroy()`" is already done.

### Changelog of this file

| Date | Change |
|---|---|
| 2026-10-17 | Created from a six-agent static audit of `ari/pool-table` @ `e724c68` plus all project docs. |
| 2026-10-17 | §5.2: noted the missing `unityyamlmerge` driver and the new agent files (`AGENTS.md`/`CLAUDE.md` rebuilt on Ari's template, `.claude/rules/` added). |

---

## 22. Update checklist

Run this before every commit. If a box applies, update that section **in the same commit** and add a changelog row (§21) for anything bigger than a value tweak.

- [ ] Changed a `GameConfig` value or added a field → §10 (and §14 if a rate moved). Update the C# default too.
- [ ] Added/renamed/removed a script, service or folder → §6.
- [ ] Changed scene roots, prefabs, layers, packages or settings → §5.
- [ ] Changed flow, endings, pausing or persistence keys → §7.
- [ ] Changed input, hints, tutorial, camera or cheats → §8.
- [ ] Changed clock, money, Meter or ITRS behaviour → §9.
- [ ] Changed a desk item, task, ticket, pawn or the Cat/Phone → §11.
- [ ] Added/changed a card class or asset → §12.
- [ ] Changed the Laptop or an app → §13.
- [ ] Fixed a known issue → move it from §15 to §16. Found a new one → add it to §15.
- [ ] Made or changed a design decision → §18 (and §2 if it touches an invariant — owner sign-off required).
- [ ] Finished or started roadmap work → §17.
- [ ] Verified something from §20 → record it and delete the row.
- [ ] Bump the snapshot table at the top (date, branch, commit).

---

## 23. Glossary

| Term | Meaning |
|---|---|
| Clock | The countdown (`ClockService.RemainingTime`): 120 s start, win ≥ 600, lose 0 |
| Meter | The only money → time converter; Feed spends all money ≥ $10 at $1 = 1 s |
| Baseline / Throughput | $3/s target / money per real second an activity yields |
| ITRS / Bill / Assessment | Internal Time Revenue Service; a tax charge every 45 s |
| Annoyance | An interruption gated by the 12 s rule (today `AnnoyanceManager`; planned: calls only) |
| Hand / Placed / Flip | cards on the desk (cap 5) / put on the desk as a `CardItem` / effect ended: face-down, still in a slot until pawned |
| Pack | 3 cards from the Card Box, revealed independently |
| Collection | registry of card types ever completed (saved by asset name) |
| Rarity | C / R / SR / UR (60/25/12/3) |
| Vein / Bank / Drop | Mining run of 6–10 blocks / paid on completion / unbanked ore lost on close |
| Hot Table | Blackjack streak bonus +10% per consecutive win, cap +50% |
| Pawn / Printer | sell an item by dragging the Printer onto it |
| Buyback | planned: re-buy a pawned item at 110–125% |
| Starting kit / Catalog / License | planned shop terms (§17.3) |
| Room | the vertically scrolling play space (I9 amendment) |
| Wire Task | plug-to-socket puzzle that powers the Laptop |
| Billiards: table, queue, par, racked, foul, re-spot, called pocket, blocker, forbidden ball, mirror, quantized shot, verifier, knife-edge, stake refund | §13.4 |
| Pending effect / Grant | a queued card effect owned by its card; withdrawn when the card leaves; cleared on scene load |
| InputGate / InputBlocker | counted global input lock / component that holds it while enabled |
| PauseLock / SpeedModifiers / GameDelta | planned counted clock pause / keyed clock speed contributors / clock-aware delta time |
| Batch / Phase / Checkpoint | plan units: one branch / a task group / stop-and-report gate |
| Landmine (L#) | item from the 2026-08-31 defect pass |
| MASCH | the Set Completion win |
