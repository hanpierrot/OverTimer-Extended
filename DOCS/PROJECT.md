# OverTimer Extended — Project Reference

> ## ⚠️ MAINTENANCE RULE (mandatory for every human and agent)
> **This file must stay true.** Any change to code, `GameConfig.asset` or other config assets, prefabs, the scene, or a design decision that makes a statement here false **must update this file in the same commit**. A commit that changes behaviour without updating `PROJECT.md` is incomplete and should not be merged.
> Before finishing any task, run the **update checklist in §22** and bump the snapshot line below.

| Snapshot | Value |
|---|---|
| Last verified against code | **2026-10-22**, static read of branch `Han/RefactorSystem` (based on `main` @ `ac381ad`) plus `DESIGN.md` v2.2; the Editor compiled the code and the scene/prefab edits were checked in the Editor Console only. Nothing was played through in Play mode, so every new mechanic is `[unverified]` at run time. Sections not touched by the 2026-10-22 sync keep their 2026-10-17/20 verification |
| Branch / commit | **`Han/RefactorSystem`** (uncommitted work on top of `main` @ `ac381ad`, 2026-10-22). `main` already has Batch 1 (Phone), Batch 2 (Shop), Batch 3 (Delivery/Door/Mail) and `ari/buyback`. This branch adds the four mechanic endings, the ticket rework, the cheat-menu rebuild, hints instead of service tutorials, the keyed cost modifiers, the Reward Popup and the scene clean-up (§21 changelog). Not yet committed or merged |
| Engine | Unity **6000.3.14f1**, URP 2D, single scene |
| Replaces | `GAME_STRUCTURE.md`, `CODE_GROUND_TRUTH.md`, `ITEMS_INVENTORY.md`, `BILLIARDS_BALANCE.md`, `BILLIARDS_DECISIONS.md` (see §21) |
| Design spec | `DOCS/DESIGN.md` **v2.2** (2026-10-22) is the design source of truth; this file records what the code does and where it differs (§17.3) |
| Related plan | `PHONE_SHOP_DELIVERY_IMPLEMENTATION.md` + `PHONE_DELIVERY_DECISIONS.md` (claude.ai project "Overtimer"); Batches 1–3 built (Batch 4 Buyback merged as `ari/buyback`) |

**Source-of-truth order:** *behaviour* = code and live assets → this file → plan docs. *Intent* = `DESIGN.md` (invariants and economy). When the code differs from `DESIGN.md`, that is either a bug (§15) or a design feature not built yet (§17.3): never record a deliberate design rule as a bug. When this file and the code disagree, the code wins: fix this file and say so in the commit.

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
- Genre: single-room resource/risk puzzle (three fixed camera views). Target run 210–300 s, instant restart.
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

These are **[LOCKED]** design rules, worded as in `DESIGN.md` v2.1 §2. Do not break one to make a task easier: if a request conflicts with an invariant, say so. Changes need an explicit owner decision recorded in §18.

| # | Rule (DESIGN v2.1) | Status in code (2026-10-20) |
|---|---|---|
| I1 | Clock starts at **120 s**; the run is won at **≥ 600 s** or by an alternate ending, lost at **≤ 0 s** (unless the Final Countdown intercepts it once) or by an alternate ending. Alternate endings: 3 cards (Set Completion, Final Countdown, Creepy Jester) + 3 mechanics (Lucky Nine ticket jackpot **or** curse, Mining Mother Lode streak, Blackjack five-to-seven-card Charlie), **10** `EndReason`s in all (the Blackjack ruin loss and the cave-in loss were cut) | Holds for the clock (`startClock 120`, `winClock 600`, `loseClock 0`). All **ten** endings are built (§7.2); the mechanic ones are `[unverified]` in Play mode. End-screen and intro-tutorial texts hard-code "10 minutes" / "2 minutes" |
| I2 | The Clock counts down in **real time**. Only a ringing or beneficial call and timed card effects (including the Time Scarab freeze) change its tick rate, through `ClockService.TickMultiplier`. The player can never pause it. Time Scarab is a buff that slows or stops the Clock only; the game keeps running and the player can still act | Holds. Tick rate = product of keyed modifiers (Phone ring ×1.5, speed cards). Time Scarab asset: `tickMultiplier 0`, 20 s [verified] |
| I3 | **Only the Meter converts money → time** (the Egg Timer is a direct time source; Blackjack converts time → money) | Holds. `AddSeconds` callers: Meter, `HoldTask` (Egg Timer), cheat. `Spend` callers: Blackjack ×2, ITRS unpaid-bill penalty, cheat |
| I4 | **Nothing random may grant clock time directly** | Holds |
| I5 | **The player never acts while game time is frozen.** Scratching, wiring, Billiards and ordinary panels never pause the Clock. A beneficial call stops the Clock only while the Laptop is locked and desk input is blocked. Game time freezes (`Time.timeScale = 0`) only for modal information panels (intro tutorial, card inspector), scripted cutscenes (Final Countdown intro, endings) and after the run has ended | The freezes match: only `CardPanel`/`TutorialPanel` call `PauseGame`. The Meter Feed button (uGUI) now returns early while `Time.timeScale == 0` (`Meter.HandleFeedClicked`, 2026-10-22). It is still not gated by `InputGate.IsBlocked`: panels with a full-screen raycast dim (e.g. `CardRevealPanel`'s Dim, 4000×4000) cover it instead (§9.2) |
| I6 | **$1 = 1 s** fixed | Holds (`exchangeRate 1`) |
| I7 | Debuffs **close a door**, never drain the clock directly. Every debuff ends by time or can be escaped by Pawning the card (only exception: a triggered Final Countdown) | Holds directly. Indirect drains by design: Robocalls (more rings), Audit (a bill you may ignore). Pawning an **active** debuff costs `debuffDismissFee` (the escape is paid, not free; DESIGN §5.4). The Audit handler leak (N5) is fixed |
| I8 | **Only calls interrupt, one at a time**, ≥ 12 s apart. Bills, mail and the doorbell are non-blocking notifications. The Cat is exempt | **Holds.** `CallScheduler` enforces one call / 12 s gap; bills are no longer an interruption (no new bill while one is open) |
| I9 | No physics simulation of the world and no character. The camera moves only between **three fixed viewpoints** (Room 14 / Desk 6 / Door 7 ortho size) by click-driven tweens; no scrolling, drag-pan, free zoom or rotation. `Physics2D` only for hit-testing and trigger overlaps. Billiards runs its own vector maths | **Holds** (`CameraRig`, §8.3). The old vertical scroller and Up/Down buttons are gone. The Door view has its door objects (`BackZone_Door_L/_R` camera zones, `DoorController`) |
| I10 | All tuning values live in **data assets** (`GameConfig`, `BlockConfig`, `TicketSymbolConfig`, `ItemCatalog`/`ItemDefSO`, `CardSO`, Billiards presets), never in script logic | **Partial:** `PickaxeView.hitCooldown` 0.7 s (sets Mining throughput), Cat speed/sleep, `ScratchWindow.revealThreshold`, `CardBox.packSize`, hint/tutorial/end texts are literals. Moved into `GameConfig` since 2026-10-20: Highlight Hover (`hoverGlow*`), Reward Popup (`rewardPopup*`), ending tuning, `debuffDismissFee`. Ticket symbol weights/multipliers live on the `TicketSymbolConfig` assets |

**Adopted from the old "proposed amendments" (now in DESIGN v2.1):** the I2/I5 wording above, the I8 wording, the I10 wording, the I9 three-viewpoint wording, and the pawn rule (an object is pawnable only if the run stays winnable without it: DESIGN §5.11 not-pawnable list). Ari's sign-off on the Time Scarab exception and the I9 rewrite is assumed from DESIGN v2.1 being committed to `main`; record it here if Ari says otherwise.

---

## 3. Coding conventions

These came from real bugs. Follow them in every change.

| Rule | Why |
|---|---|
| Read another service's `.Instance` in **`Start`**, never `Awake`/`OnEnable`, and never assume Awake order across scripts | A GameObject (or any ancestor) saved inactive defers `Awake` until first activation. Services behind closed panels are unreachable |
| For effects that target something that may not have woken yet, use a **static "pending state"** that the target applies in its own `Awake` (pattern: `MiningApp`, `ScratchTicket`, `BilliardsPendingEffects`, `DesktopIconView.s_pendingHidden`, `CheatFlags`) | Same as above |
| **Every mutable static must be reset on scene load** (`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` + `SceneManager.sceneLoaded`) | Restart = scene reload. Documented exceptions: `BilliardsRoundLog` (dev telemetry), `BilliardsDebug` (cheat switches) |
| Compare Unity objects with explicit **`!= null`**, never `?.` / `??` | `?.` misses Unity's fake-null on destroyed objects. ~45 legacy `Instance?.` sites remain (mostly `HintService`) — clean up when touched |
| **No `UnityEngine.Random`.** Use `RngService.Instance.Random` (seedable `System.Random`) | Reproducible bugs, deterministic Billiards. One cosmetic exception left: `Cutscene/ImageCycler.cs` |
| `[FormerlySerializedAs("old")]` on every renamed serialized field | Keeps asset values |
| **Enums are append-only** (`CostModifierService.Target`, `EndReason`, `HintId`, `Rarity`…) | Serialized as ints; `EndReason` is also persisted **by name** in run history |
| Asset names of `CardSO`s and future item ids are **permanent** | Collection save keys on `CardSO.name` |
| New `GameConfig` fields: add with a sensible default, then **touch them in the Inspector** so the asset serializes them | C# defaults are stale for ~20 fields; the asset is the live value |
| Card effects own their grant: `OnPlaced` adds, `OnRemoved` withdraws, using it calls `card.EndEffect()`; `CardItem._hasEnded` guarantees `OnRemoved` runs once. Shared scalars (`CostModifierService`, `SpeedModifiers`) are **keyed by the card** so two copies never overwrite each other | Pawning a card must be a real escape (I7) |
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

- Remote: `github.com/hanpierrot/OverTimer-Extended`. Branches seen: `main` (current), Han's `Han/MoreContent`, `Han/RefactorSystem`, `Han/RemakeCards`, `Han/RemakeTickets` (+ older `Han/*`), Ari's `ari/Phone-Revamp`, `ari/shop-system`, `ari/pool-table`, `ari/delivery` (all merged except possibly `ari/delivery`).
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
- Canvas: Screen Space Overlay, `CanvasScaler` Scale With Screen Size **960×540**, match width. Main Camera orthographic, driven by `CameraRig`: Room 14 (start), Desk 6, Door 7 (§8.3).
- Script execution order (from `.cs.meta`, per GAME_STRUCTURE 10-08): `GameManager −20 → InputManager −10 → MoneyService −5 → RngService −4 → CardCollectionManager −3 → RunStatsService −2 → others 0`. [re-verify]; `ClockService −1` via `[DefaultExecutionOrder(-1)]` on the class (Phone plan P3, owner-approved Q13).

### 5.5 Scene `SampleScene.unity`

Nothing is `DontDestroyOnLoad`; **Restart = reload this scene.**

| Root | Contents |
|---|---|
| `Background` | Desk, DeskLegs, Shadow, carpet |
| `CameraZones` | five instances of the `CameraZone` prefab (`DoorZone`, `BackZone_Door_L/R`, `DeskZone`, `BackZone_Desk`): `BoxCollider2D` + `PointerReceiver` + `CameraViewButton` + `ViewGate`; `CameraViewButton` requires `ViewGate` |
| `DeskObject` | prefab instances: PrinterPawn, Laptop (desk opener), EggTimer, Phone, PowerBank, ElectricSocket (wire puzzle), CardItems (7 pooled), Picture, Backpack, CardCollection, Cat, Bear, Tickets (2 Triple + 2 Nine pooled tickets) |
| `Main Camera` | orthographic (size set by `CameraRig`), AudioListener |
| `Global Light 2D` | |
| `MANAGERS` | `Managers` (GameManager, InputManager, CheatMenuToggle) · `Services` (ClockService, RngService, MoneyService, RunStatsService, CostModifierService, ITRSService, ShopService, **AudioService**) · `CardRelatedManager` (CardCollectionManager, CardHandManager, FinalCountdownTracker, SetCompletionTracker) · `ClickFeedback` (prefab `ClickFeedbackPool`: ClickFeedbackService + 10 pooled `ClickFeedbackVFX`) · `Audio` (one AudioSource, no clip; shared by the Door and the cutscenes) · `CameraRig` (Room / Desk / Door anchors) · `HoverOutline` (`HoverHighlighter`, shader glow) |
| `EventSystem` | `InputSystemUIInputModule` |
| `Canvas` | `Timer` (TimerText, MeterText, FeedButton, and the `Meter` component), `MoneyPopup` (pooled `+$N` texts), `DeviceLayer` (§13.1), panel prefabs: Laptop (UI), Mining App, Blackjack App, Billiards App, Tax, CardRevealPanel, CardPanel, CardCollectionPanel, NinePanel, TriplePanel, WirePanel, EndGamePanel, CheatMenuPanel, TutorialPanel, HintRoot |
| `CutsceneRoot`, `Cutscenes`, `ClockRoot` | cutscene canvas, `CutsceneController` + PlayableDirectors, clock-ending ticker |

Scene rules: don't save the scene with the Billiards app open (layout overrides leak); panel prefabs are saved inactive (their `Awake` waits for first open); never re-save `Mining App.prefab` by accident.

---

## 6. Code map and services

### 6.1 Folders (176 `.cs` files in all, 10 of them Editor-only)

| Folder | Files | Purpose |
|---|---|---|
| `Config/` | 3 | `GameConfig` (all tuning), `BlockConfig` (mining blocks), `TicketSymbolConfig` (declared in **`TicketSymbolPool.cs`**) |
| `Audio/` | 3 | `SfxId` (append-only enum of every sound effect), `AudioCatalogSO` (id → clips and settings; asset `Assets/Audio/AudioCatalog.asset`), `AudioService` |
| `Services/` (+`Clock/`) | 12 + 2 | GameManager, ClockService, MoneyService, Meter, RngService, RunStatsService, InputManager, HintService, ClickFeedbackService, CostModifierService, CardHandManager, CardCollectionManager; `PauseLock`, `SpeedModifiers` |
| `Input/` | 5 | `IPointerInputHandler`, `PointerReceiver`, `DeskObject` base, `InputGate` (static counter), `InputBlocker` |
| `Hint/` | 5 | `HintId`, `HintCatalog`, `TutorialPanel` (intro only); `HoverHighlighter` (Highlight Hover, shader glow), `MoneyPopup` (Reward Popup: money and Egg Timer time) |
| `Cutscene/` | 3 | `CutsceneController`, `TimerTicker`, `ImageCycler` |
| `CheatMenu/` | 3 | `CheatMenuController` (5 tabs), `CheatMenuToggle` (debug builds only), `CheatFlags` (static switches) |
| `UI&Camera/` | 6 | `CameraRig` (+`CameraView` enum), `CameraViewButton`, `ViewGate`, `DeviceLayer` (Laptop/Phone layout, lock), `BlurSnapshot`, `DeviceClock` |
| `Summary/` | 7 | `EndReason`, `RunRecord`, `RunRating` (+`IsMechanicWin`), `EndGameSummaryUI`, `RunHistoryPanel`, `RunHistoryEntryView`, `EndingFlourish` (coin rain on mechanic wins) |
| `Annoyance/` (+`Cat/`) | 3 + 2 | `ITRSService`, `TaxDeadline` (pure), `Tax` (paper-copy panel, opened from the tax letter at the door); `CatController`, `CatStateMachine` |
| `Shop/` | 13 | pure: `Door` (`Parcel`, `DoorDepot`), `ShopCart`, `Economy` (`ItemDef`, `PawnMath`, `Checkout`, `Ownership`, `IconGate` with `IsLicensed`, `ShopValidation`), `Delivery` (`DeliveryScheduler`, `PawnLedger`), `Mail` (`Letter`s); Unity: `ItemDefSO`, `ItemCatalogSO`, `ShopService` (on `MANAGERS/Services`), `ShopItemBinding`, `DoorController`, `ParcelView`, `LetterView`, `PoofEffect` |
| `Phone/` (+`Apps/`) | 6 + 10 | pure: `Calls` (`CallerRoller`, `PhoneConfig`, `CallRecord`, `CallDialogue`), `CallScheduler`, `PhonePower`, `Notifications` (`Inbox`, `BannerQueue`); `CallerProfileCatalog`; `PhoneController` (desk object, was `Phone`); views `PhoneHome`, `CallScreen`, `CallHistoryView`, `InboxScreen`, `ShopScreen`, `PhoneListView` + `PhoneListRow` (scrolling list), `TaxesScreen`, `PhoneIconView`, `IncomingCallPill` |
| `Desk/Items/Cards/` | 8 | `CardItem`, `CardBox`, `CardPanel`, `CardPackRevealPanel`, `CardRevealSlot`, `CardFlip`, `CardCollectionPanel/Slot` |
| `Desk/Items/Cards/CardData/` | 13 (+4) | `CardSO`, `EffectCardSO`, 11 effect classes; `WinConditionCard/{FinalCountdown,SetCompletion}/*` (a `SO` + a `Tracker` each) |
| `Desk/Items/Pawn/` | 4 | `Pawn` (the printer), `IPawnable`, `IRestorable`, `JunkItem` |
| `Desk/Items/Tasks/` (+`ExtendedTasks/`, `WireComponents/`) | 5 + 3 + 4 | `TaskBase`, `ClickTask`, `HoldTask`, `WireTask`, `DeskObjectFocus`; `EggTimerTask`, `PictureClickTask`, `PowerBankTask`; wire plug/socket/line/initializer |
| `Desk/Items/Tickets/` | 5 | `ScratchTicket`, `ScratchWindow`, `TicketStack`, `ScratchPanelView` (the ticket panel's UI references), `TicketPayoutTable` (the payout table beside the panel) |
| `Desk/Laptop/` | 7 | `LaptopController`, `LaptopApp`, `LaptopHome`, `LaptopLock`, `DesktopIconView`, `DeskLaptopOpener`, `CloseAppButton` |
| `Desk/Laptop/Mining/` | 2 | `MiningApp`, `PickaxeView` |
| `Desk/Laptop/Blackjack/` | 5 | `BlackjackApp`, `BlackjackGame` (pure C#), `CardDeckConfig`, `CardRowView`, `CardSlotView` |
| `Desk/Laptop/Billiards/` (+`View/`) | 14 + 9 | §13.4 |
| `Editor/` | 10 | `Billiards/` (7: table editor, solver, verifier, balance report, debug and round-log menus, preset JSON), `Fonts/BitmapFontBuilder`, `SaveData/PlayerPrefsTools`, `EditorAssetFinder` (finds an asset by type: the tools no longer hard-code asset paths). The two menu self-tests moved to `Assets/Tests/EditMode/SelfTests/` (2026-10-26) |

### 6.2 Singletons

All use `if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this;` in `Awake`. None survive a reload.

| Class | Owns | Key API |
|---|---|---|
| `GameManager` | config ref, game-over state, pause refcount, restart | `GameConfig`, `IsGameOver`, `LastEndReason`, `OnGameOver(EndReason)`, `PauseGame()/ResumeGame()` (refcounted, `timeScale`), `GameOver(reason)` (**ignored if already over**), `Restart()` |
| `ClockService` | `RemainingTime`, `GameDelta`/`GameTime`, `TickMultiplier` (read-only), final-countdown intercept; owns `Clock/PauseLock` + `Clock/SpeedModifiers` | `AddSeconds(s, reason)`, `Spend(s, reason)`, `Pause(holder)/Resume(holder)`, `SetSpeed(key, m)/ClearSpeed(key)`, `PauseCountdown()/ResumeCountdown()` (legacy wrappers around one holder), `ArmFinalCountdownIntercept(reset)`, `OnTimeChanged`, `OnCountdownFinished`, `OnFinalCountdownTriggered` |
| `MoneyService` | `Current` | `Add(int, reason)`, `TrySpend(int, reason) → bool` (never negative), `OnMoneyChanged(current, reason)`, `OnIncome(amount, reason)` (positive income only; not stake refunds; feeds the Reward Popup), `const StakeRefundReason = "stake refund"` |
| `Meter` | Feed button (subscribes in `Start`, unsubscribes in `OnDestroy`; ignores clicks while `Time.timeScale == 0`) | `SetLocked(bool)`, `IsLocked` |
| `RngService` | one `System.Random` | `Random`; serialized `useFixedSeed`, `seed` (scene: unseeded) |
| `RunStatsService` | run stats + history | `LastRecord`, `LoadHistory()` |
| `InputManager` | press/drag state machine | `IsInputEnabled`, `dragThreshold` 0.15, `PeekTarget()` (what a click would hit now; used by hover), view-aware `PickTarget` (honours `ViewGate`), vestigial `FocusRoot` |
| `CameraRig` | the three viewpoints | `CurrentView`, `IsMoving`, `GoTo(CameraView)`, `ViewChanged`; holds an `InputGate` block while moving |
| `HintService` | toast hints | `Show(id, args)` (per-id cooldown), `ShowAlways(id, args)` (ignores the cooldown; used for repeating teases), `ShowOnce(id, args)`, `ResetSeen()` |
| `ClickFeedbackService` | 10 pooled particle systems | `PlayAt(Vector2)` |
| `AudioService` | pool of 8 `AudioSource` voices, the `AudioCatalogSO` | static `AudioService.Play(SfxId, volumeScale)` (a no-op without the service); subscribes in `Start` to `MoneyService.OnIncome`, `CardHandManager.CardPlaced`, `CardRevealSlot.CardRevealed`, `CardCollectionManager.NewCardRegistered`, `GameManager.OnGameOver`, `DeviceLayer.ModeChanged`, `HintService.HintShown` |
| `CostModifierService` | price multiplier per target, **keyed** (the card is the key); the effective value is the product of all keys | `enum Target { Ticket, CardPack, Blackjack, Billiards }`, `GetMultiplier(target)`, `SetMultiplier(target, key, value)`, `ClearMultiplier(target, key)` |
| `CardHandManager` | 7-card pool, placed set | `HasRoom` (`< handCap`), `TryGetFreeCard`, `CardPlaced/CardRemoved` |
| `CardCollectionManager` | collected card types | `Register(card)`, `IsCollected`, `AllCards`, `NewCardRegistered` |
| `CardPanel` | card detail popup | `Show(CardSO, Sprite)` — **pauses the game while open** |
| `CutsceneController` | end/intro cutscenes | `PlayEnding(reason, onDone) → bool`, `PlayFinalCountdownIntro()`, `StopAll()` |
| `PhoneController` | `Power`, `Calls`, `Inbox`, `Banners`, `ActiveCall`, `CallHistory` (`CallRecord` list, newest first) | `Answer/Decline/Reply`, `PressPower`, `ForceOff/ReleaseForceOff(holder)`, `AddRobocalls/RemoveRobocalls(holder)` |
| `ITRSService` | tax bills (`TaxDeadline`) | `Pay()`, `ExtendCurrentBill(s)`, `SkipNextBill(owner)`, `ForceBillNow()`, `DoubleCurrentBill()`, `CurrentAmountDue`, `SecondsLeft`, `OnAssessmentIssued/Resolved`, `AssessmentChanged` |

Static helpers: `InputGate` (counted global input lock), `RunRating` (score maths), `CheatFlags` (dev-only "force the next X" switches; every flag is gated on `Debug.isDebugBuild` and reset on scene load).

---

## 7. Game flow, endings and persistence

### 7.1 Run lifecycle

1. Scene loads. `GameManager.Awake` unfreezes (`timeScale 1`). `ClockService.Start` sets 120 s and starts. `MoneyService.Start` sets $40.
2. First launch only: `TutorialPanel` (the intro, 5 click-through steps, skippable) opens and **pauses the game** until closed (`PlayerPrefs TutorialSeen`). Every other tutorial is a one-line hint (§8.2).
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
| `TicketJackpotWin` | a Lucky Nine ticket rolls the Jackpot (`luckyNineJackpotChance` 0.006) and is fully scratched | `EndingFlourish` coin rain | `ratingMechanicWin` 0.95 |
| `TicketCurseLoss` | a Lucky Nine ticket rolls the Curse (`luckyNineCurseChance` 0.01), is fully scratched and the `curseBeatSeconds` (1.2 s) beat runs | none (shake, red label) | `ratingMechanicLoss` 0 |
| `MotherLodeWin` | `miningStreakTarget` (3) consecutive length-10 veins banked | `EndingFlourish` coin rain | 0.95 |
| `BlackjackCharlieWin` | a hand reaches `blackjackCharlieCards` (7) cards without busting; rolled at the deal with `blackjackCharlieChance` (0.04) by rigging the first draws | `EndingFlourish` coin rain | 0.95 |

If a reason has no director, the end panel shows immediately; the three mechanic wins (`RunRating.IsMechanicWin`) play `EndingFlourish` instead. `GameManager.GameOver` now ignores a second call (re-entry guard), so a record is saved once. The Blackjack ruin loss and the Mining cave-in loss were cut (§18 A22).

### 7.3 Pause model (two separate mechanisms)

| Mechanism | Type | Used by |
|---|---|---|
| `GameManager.PauseGame/ResumeGame` | refcounted, sets `Time.timeScale = 0` | `CardPanel` (open/close), `TutorialPanel`; `ResumeGame` never unfreezes after game over |
| `ClockService.Pause/Resume(holder)` | **reference-counted** (`PauseLock`); `PauseCountdown/ResumeCountdown` share one legacy holder | `GameManager.GameOver` (own holder, never released), Final Countdown intro |

Per I5 these are the only legal freezes (plus cutscenes and game over). The Meter Feed rule that closes the uGUI gap is in §9.2.

Everything gameplay-side uses scaled time, so it freezes with `timeScale`. Cutscenes run `UnscaledGameTime`; hints and end panel use `SetUpdate(true)`.

### 7.4 Run summary and rating

- `RunStatsService` tracks peak clock (labelled "Best Time"), money earned (all positive deltas except stake refunds, including pawning), cards opened, new collection entries; saves a `RunRecord` JSON on game over into a 20-slot ring buffer.
- `EndGameSummaryUI` shows 4 stats. `RunHistoryPanel` lists history with 4 sort modes.
- `RunRating.Score` = `100 × (0.5·outcome + 0.3·survival + 0.2·money)` with `survival = win ? 1 : clamp01(peakClock / 600)` and `money = e / (e + 300)`. **Continuous 0–100, no grades/thresholds.** Shown in history rows only. Example: ClockWin with $300 earned = 90.

### 7.5 Persistence (`PlayerPrefs`)

| Key | Content |
|---|---|
| `TutorialSeen` | 1 after the intro tutorial closed |
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
- `InputGate` (static counter) + `InputBlocker` (component: `OnEnable → Add`, `OnDisable → Remove`). `GameManager` maps the gate to `InputManager.IsInputEnabled`. `InputBlocker` is on `CardPanel`, `TutorialPanel`, `CardRevealPanel`, `CardCollectionPanel`, the two ticket panels, `WirePanel`, `Tax`, the three Laptop apps and `Laptop`, `DeviceLayer`, `EndGamePanel`, `CheatMenuPanel`, `CutsceneRoot` [verified by prefab search]. `CameraRig` also holds a block while a view change tweens.
- **Views:** `PickTarget` skips colliders the current view does not allow (`ViewGate.IsAllowed`; default = Desk view only). In the Room view only objects with a `ViewGate` that lists Room (desk hotspot, door, Picture, Egg Timer per DESIGN §4.1) respond. **[verified 2026-10-29]** (prefab and scene read, and PlayMode `CameraClockTests.TC_CAM_4`): `ViewGate` sits on the `CameraZone` prefab (default Room: `DeskZone`, `DoorZone`; scene overrides make `BackZone_Desk` Desk-only and `BackZone_Door_L/R` Door-only), `EggTimer` and `Picture` (Room + Desk), `Letter` and `Parcel` (Door only); everything else has no `ViewGate` and is Desk-only.
- uGUI (Meter Feed, camera arrows, panel buttons) goes through the EventSystem and **is not gated by `IsInputEnabled`**.
- A press already in progress is not cancelled when input gets blocked or the game ends.

### 8.2 Hints and tutorial (exist in code; not in any older design doc)

- `HintId` has 28 values (the last three, `PackageArrived`, `ParcelOpen`, `JunkFlyer`, are new): `LaptopNoPower, CatBlocking, NotEnoughMoney, MeterMinFeed, MeterLocked, TooManyTickets, HandFull, NoRoomToPlace, NotEnoughTime, TaxCannotAfford, ClickCooling, PowerBankRecharging, CannotPawn, LaptopIconDoubleClick, MiningOpen, MiningCloseLosesOre, BlackjackOpen, BlackjackLeaveMidHand, WireOpen, PhoneFirstRing, MailTrayFirstBill, CatFirstBlock, ShopPurchased` plus the later ones (`PackageArrived`, `ParcelOpen`, `JunkFlyer`, `CharlieTease`, `TicketCashOut`, `TicketCursed`, `MotherLodeTease`, `PhoneFirstOpen`, `PausedCall`, `ShopFirstOpen`, `LicenseBought`, `FirstPawn`). `TicketOpen` and `EggTimerPlaced` were removed (no serialized field stores a `HintId`, so the numbers could shift safely).
- `HintCatalog.Get(id)` holds all texts as literals; some take `{0}` arguments (`MeterMinFeed`, `HandFull`, `NotEnoughTime`) — call those with an argument or `string.Format` throws.
- `HintService`: one toast label, hold 2.5 s, fade 0.5 s, per-id cooldown 3 s (unscaled), silent after game over. `ShowOnce` remembers across runs via `HintSeen_<id>`.
- **Service tutorials moved to hints (2026-10):** the nine `TutorialService.TryShow` calls (Phone, Shop, delivery, tax, pawn) now call `HintService.ShowOnce` with `PhoneFirstOpen`, `PhoneFirstRing`, `PausedCall`, `MailTrayFirstBill`, `ShopFirstOpen`, `LicenseBought`, `PackageArrived`, `ParcelOpen`, `FirstPawn`. `TutorialService`, `TutorialTracker`, `TutorialCatalogSO` and `Tutorials.asset` were deleted (the component was removed from `Services` in the scene), and `TutorialPanel` keeps only the intro.
- `TutorialPanel`: 5 hard-coded steps ("You have 2 minutes to live", "Get the clock to 10:00…", "$1 = 1 second"…), pauses via `PauseGame`. **Name collision:** the Phone plan proposes a new `TutorialPanel` — extend this one instead.

### 8.3 Camera (`CameraRig`)

Three fixed viewpoints, no scrolling (I9, DESIGN §4.1). The run starts in the **Room** view.

| View | Ortho size (`GameConfig`) | Reached by |
|---|---|---|
| Room | `roomCameraSize` 14 | start; a Back hotspot (`CameraViewButton` target Room) |
| Desk | `deskCameraSize` 6 | click the desk in the Room view |
| Door | `doorCameraSize` 7 | click the door in the Room view (the door objects exist since Batch 3) |

- `CameraRig.GoTo(view)` tweens position and size over `cameraFocusSeconds` (0.5 s, `Ease.InOutSine`), holds an `InputGate` block until done, then raises `ViewChanged`. Calls while moving or to the current view are ignored. It kills its tween in `OnDisable`.
- The clock keeps running during a move (I5).
- There are no scroll buttons and no on-screen Back control: Back is a hotspot outside the desk. `HoverHighlighter` skips Back (Room) buttons so they never glow.
- **Tutorial text** step 5 says "Click the desk to zoom in and work. Click the door when the bell rings."
- The camera zones (desk / door / back hotspots) are the `CameraZone` prefab (instances `BackZone_*`, `BackZone_Door_L/_R`); each carries its `CameraViewButton` and `ViewGate`.

### 8.3a Highlight Hover and Money Popup (Han, merged 2026-10-19)

Both are DESIGN features (§4.2, §5.17), built as a **simpler first pass**; differences from the spec are listed so nobody mistakes them for bugs:

| | DESIGN v2.1 | As built |
|---|---|---|
| Highlight Hover | glow along the sprite's alpha edge, shared outline material + `MaterialPropertyBlock`, fade in 0.1 s / out 0.15 s, width 3 px scaling with the camera, config `hoverGlow*` | **Matches the spec now.** `HoverHighlighter` draws up to `maxQuads` (8) quads per hovered object with the `SpriteOutlineGlow` shader (`Assets/Code/Shaders/`) and a `MaterialPropertyBlock`; colour, pixel width and fade times come from `GameConfig.hoverGlow*`; the hovered collider is picked every `checkInterval` (0.05 s) from `InputManager.PeekTarget()`. The old rectangular `LineRenderer` is gone |
| Respects interactable / input blocked / view | yes | interactable: yes; input blocked and view: yes through `PeekTarget` (null while input is disabled, honours `ViewGate`); the glow hides when the object deactivates (pawn) and when the game is over |
| Reward Popup | `+$N` **at the pointer**, money and Egg Timer `+Ns`, stacked, pooled (8), unscaled time, config `rewardPopup*` | Built: `MoneyPopup` listens to `MoneyService.OnIncome` (income only, never stake refunds or after game over) and `EggTimerTask.TimePaid`; one popup per reward at the pointer, stacked within `rewardPopupStackSeconds`, 8 pooled texts, unscaled time, scale-in pop, config `rewardPopup*` (serialized in `GameConfig.asset`) |
| Owner | Han | Han |

### 8.4 Cheat menu (debug builds only)

- Toggle key: F1 (F2 stops a cutscene). `CheatMenuToggle` disables itself in a release build (`Debug.isDebugBuild`), so the menu cannot open there; `CheatFlags` is gated the same way (closes the old "no guard" issue).
- `CheatMenuController` builds **five tabs** in code from the existing button grid (`AddCheat` / `AddCheatToggle`, `BuildTabStrip`): **Endings** (preview and force each of the 10 endings; next hand = Charlie; next Lucky Nine = Jackpot / Cursed), **Games** (open the viewed ticket with N windows scratched, free tickets, every vein = max, Mother Lode streak = N, next vein = Slip, bank vein now), **Economy** (clock near loss / near win, ±60 s, freeze clock, money +100 / +1000 / 0), **Annoyance** (ring phone, tax bill, junk flyer, empty package, Cat blocks Laptop / Phone, Cat leaves), **Dev** (hint next / previous / reset seen, history samples / clear, reset intro tutorial, FC intro, stop cutscene, restart, close panels, clear hand).
- One list sits outside the tabs: the **card list** (one button per card asset, `GiveCard`). The Billiards debug switches (overlay, mirror, table, verify presets, round log) are only in the Editor menu `Tools/OverTimer/Billiards/`; the in-game `BilliardsCheats` panel was removed.
- Panel prefab `CheatMenuPanel.prefab` (840×480, centred, `TabBar` + scroll lists); its scene overrides were removed.

### 8.5 Audio

Sound effects go through **`AudioService`** (2026-10-26, plan B: no mixer and no BGM yet). `AudioCatalogSO` maps every `SfxId` to its clips, volume, pitch jitter, `minInterval` (repeat guard) and `ignoreListenerPause`; an id with no clip is silent, not an error. Several clips in an entry are alternatives picked at random, using a private `System.Random` (never `RngService`, so audio cannot shift the seeded gameplay stream). `MoneyGain` has three tiers chosen by `moneySmallMax` / `moneyMediumMax` on the catalog.

| Sound | Hook |
|---|---|
| `Click`, `ClickRelease` | `InputManager` (press start / `EndPress`) |
| `MoneyGainSmall/Medium/Large` | `MoneyService.OnIncome` (never for a stake refund) |
| `CardPackBuy`, `CardPackOpen` | `CardBox.HandleClick` after paying, `CardPackRevealPanel.OnPackClicked` |
| `CardFlip`, `CardPlace`, `CardCollected` | `CardRevealSlot.CardRevealed`, `CardHandManager.CardPlaced`, `CardCollectionManager.NewCardRegistered` |
| `BlackjackDeal`, `BlackjackHit` | `BlackjackApp.OnDeal`, `OnHit` (only when a card was really drawn) |
| `PawnSell` | `Pawn.OnTriggerEnter2D`, both successful paths |
| `WirePlug` | `WireTask.HandleSocketDropped` (correct pair) |
| `UiToggle` | `DeviceLayer.ModeChanged` |
| `UiError`, `Tease` | `HintService.HintShown` (new static event; the hint id decides) |
| `WinStinger`, `LoseStinger` | `GameManager.OnGameOver` by `RunRating.IsWin` (`ignoreListenerPause`) |
| `BilliardsStart`, `BilliardsShot`, `BilliardsPocket` | `BilliardsApp.PlayRound`, `Shoot`, and next to `OnPocket` |
| `ParcelSlide` | `DoorController.HandleParcelAdded` |
| `CoinRain` | `EndingFlourish.Run` (its own `AudioSource`/`coinClip` fields were removed: they were never assigned) |

Still on their own `AudioSource`: the phone ring loop (`Phone.prefab`), the Pawn drag loop (`PrinterPawn.prefab`), the Jester laugh (`ImageCycler`), and the three `DoorController` fields `doorbellSound`, `boxHitSound`, `boxOpenSound`, now assigned in the scene (bell, `ball_collide_wall_1`, `cards-pack-open-2`: provisional). **No clip yet:** `UiError`, `LoseStinger`; no sound for Meter feed, Egg Timer time, Mining, tickets, tax, cat, camera or Billiards ball contacts (the sim raises no contact events; adding them touches `BilliardsSim` and needs Verify Presets). Clips are CC0 from Kenney.nl except the bell (Pixabay licence, check) and the Billiards pack; keep a credits list.

---

## 9. Clock, money, Meter and taxes

### 9.1 ClockService

- Tick: `GameDelta = IsRunning ? Time.deltaTime : 0`; `RemainingTime -= GameDelta * TickMultiplier` every frame while running. Gameplay timers use `GameDelta`, not `TickMultiplier`; `OnTimeChanged` fires every frame.
- At ≤ 0: if the Final Countdown intercept is armed, reset to its value (180 s) and raise `OnFinalCountdownTriggered`; else end the countdown (loss).
- `AddSeconds(s, reason)` ignores `s ≤ 0` and game over; no cap. `Spend(s, reason)` clamps at 0 and **ends the run directly, bypassing the Final Countdown intercept by design** (DESIGN §5.14 B). The `reason` strings are **discarded** — there is no balance log yet.
- **`TickMultiplier` is the product of keyed modifiers** (read-only, clamped 0–3). Writers: the phone (key = the phone, 1.5 while ringing) and `ClockSpeedCardSO` (key = the `CardItem`). Overlaps stack (Slow Hand 0.75 × ring 1.5 = 1.125). Never write it directly; use `SetSpeed/ClearSpeed`. The phone is never pawnable, so the old `Phone.OnPawned` gap no longer exists.

### 9.2 Money and Meter

- `MoneyService` is the only writer of money. `Add` ignores ≤ 0; `TrySpend` fails rather than going negative.
- **Meter Feed spends *all* current money** and adds `money × exchangeRate` seconds. Minimum feed $10 (hint `MeterMinFeed`). Blocked while `IsLocked` (set when the Final Countdown triggers, hint `MeterLocked`). It is the only money → time path.
- **Feed gate (built 2026-10-22):** `Meter.HandleFeedClicked` returns at once while `Time.timeScale == 0` (a modal panel such as `CardPanel` or the intro tutorial is open, or the run is over). It is **not** gated on `InputGate.IsBlocked`: ordinary panels that block the desk (Laptop, ticket panels) do not pause time and Feed is a legitimate action there. `CardRevealPanel` covers the Meter with its full-screen raycast Dim. A rejected Feed shows no hint (the modal panel is already on screen). `Meter` now subscribes in `Start` and unsubscribes in `OnDestroy`. Meter is Ari's file: Ari should review the diff.
- `MoneyService.StakeRefundReason` ("stake refund") marks money that is not income (Billiards bet returned on a win): excluded from ITRS and run stats. **Everything else counts as earned**, including pawning and card sales.

### 9.3 ITRS (taxes)

| Rule | Live value |
|---|---|
| Bill interval | every **45 s** of running clock (`GameDelta`), counted only while **no bill is open** |
| Amount | `max($20, round(20% × money earned since last bill))` |
| Deadline | each bill has a `TaxDeadline`: `taxPayWindow` **30 s of running clock** (stops during paused calls) |
| Pay | `TrySpend(due)` from the **Taxes phone app** or the **paper copy** (`Tax` panel, opened from the tax letter at the door); either resolves both. Pay is disabled when you can't afford it |
| Unpaid at 0 s | costs **due × 2 s** of clock, **once** (`ClockService.Spend`; bypasses the Final Countdown intercept by design). Closing the panel does nothing; the **Ignore button is gone** (Q1) |
| Gate | ITRS issues **no new bill while one is open** (P6 fixed) |
| Card hooks | Tax Exemption → `SkipNextBill` (next bill issued at $0, resolved like any other); Audit → `ForceBillNow` (ignored while a bill is open) or `DoubleCurrentBill` (also raises `AssessmentChanged`); ITRS call "payment plan" → `ExtendCurrentBill(itrsCallExtension)` |

- A new bill also posts an Inbox message "Tax bill $X" (action "Open Taxes") and a banner while the phone is usable.
- **Paper copy = the tax letter (Phase 3.2).** `taxLetterDelay` (10 s) after a bill is issued, `DoorController` drops a TAX `Letter` on the carpet in the Door view; clicking it opens the `Tax` panel; the letter disappears when the bill is settled. `Tax` reads `ITRSService` every frame while open (no event subscription: it starts inactive) and closes itself when the bill is gone. The old desk "Taxes" object (`TaxNotice`, `Taxes.prefab`) and its script were deleted.
- Effective tax is above 20% for low earners (a $1.2/s grinder pays ~36%). The Egg Timer earns no money, so it is never taxed.

---

## 10. GameConfig — live tuning values

`Assets/Settings/Configs/GameConfig.asset` holds **123 serialized fields** of the 160 declared in `GameConfig.cs` (checked 2026-10-22 by comparing the field list with the asset keys). The other **47** exist only as C# defaults until Ari touches the asset in the Inspector: Phone (`taxPayWindow`, call weights and timers, `deviceBlurEnabled`), Shop, Door/Mail, `eggTimerCooldown`, Camera, `ratingMechanicWin/Loss`, every Alternate Endings field and the four `hoverGlow*` fields. The Reward Popup fields and `debuffDismissFee` were serialized on 2026-10-22. The **asset is the truth**: C# defaults in `Config/GameConfig.cs` are stale for the rows marked ⚠ (a freshly created GameConfig would play the old, pre-hotfix game — e.g. Lucky Nine paying $0).

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
| | `debuffDismissFee` (new 2026-10-22) | $6 (pawning a debuff whose effect still runs costs this, pays nothing) | same |
| | `commonWeight` / `rareWeight` / `superRareWeight` / **`ultrRareWeight`** (typo) | 60 / 25 / 12 / 3 | same |
| Blackjack | `blackjackAnte` / `blackjackClockMargin` | **20 s / 15 s** ⚠ | 15 / 10 |
| | `blackjackWinPayout` / `PushPayout` / `NaturalPayout` | **100 / 25 / 130** ⚠ | 120 / 60 / 200 |
| | `blackjackStreakStep` / `blackjackStreakCap` (Hot Table) | 0.10 / 0.50 | same |
| | `blackjackRevealDelay` / `blackjackResultDelay` / `laptopSwitchTime` | 0.6 / 1.2 / 1.0 s | same |
| ITRS | `billInterval` / `billRate` / `billMinimum` / `shortfallPenalty` | 45 s / 0.20 / $20 / 2 s per $ | same |
| ITRS | `taxPayWindow` (new, C# default only until the asset is touched) | 30 s of running clock | same |
| Phone | `ringIntervalMin/Max` / `ringEffectMultiplier` / `maxRingDuration` | 30–60 s / 1.5 / 10 s | same |
| Door (Phase 3.1-3.2) | `doorParcelSlots` · `parcelClicksToOpen` · `taxLetterDelay` · `letterSlots` · `junkMailIntervalMin/Max` · `junkMailGrace` (new, C# defaults until the asset is touched) | 4 · 3 · 10 s · 8 · 60 / 120 s · 60 s | same |
| Shop (Phase 2.1) | `shopStandardFee/Seconds` · `shopExpressFee/Seconds` · `shopLeaveOpenFee` · `shopCouponRate` (new, C# defaults until the asset is touched) | $0 / 20 s · $8 / 5 s · $3 · 0.2 | same |
| Phone calls | `callWeightTelemarketer/Client/Unknown/ITRS` · `unknownSpamChance` · `telemarketerPitch` · `beneficialCallTimeout` · `clientGigMoney` · `callHangUpDelay` · `callGoodbyeSeconds` · `itrsCallExtension` · `phoneBootSeconds` · `bannerLifetime` (all new, C# defaults only until the asset is touched) | 25/25/40/10 · 0.6 · 4 s (game, hidden) · 8 s (real, hidden) · $22 · 5 s (real, hidden) · 2 s (real) · 15 s · 5 s · 4 s | same |
| Phone | `deviceBlurEnabled` (new, C# default only until `GameConfig.asset` is touched in the Inspector) | true | false = dark tint instead of blur (WebGL fallback) |
| Annoyance | `annoyanceMinGap` | 12 s | same |
| Egg Timer | `crankTime` / `timePayout` | **1 s** ⚠ / 3 s | 2 / 3 |
| | `eggTimerCooldown` (P18) | 0.75 s after each crank (C# default until the asset is touched) | 0.75 |
| Cat | `catSitDurationMin/Max` | **6 / 14 s** ⚠ | 3 / 8 |
| | `catBlockChance` / `catBlockDurationMin/Max` | 0.4 / **3 / 6 s** ⚠ | 0.4 / 4 / 8 |
| Click Task (Bear) | `clickTaskMinClicks/MaxClicks` / `LockDuration` / `MoneyReward` | 3–8 / 3 s / $5 | same |
| Picture | `pictureClickTask*` | 3–8 / 3 s / $5 | same |
| Power Bank | `powerBankCrankTime` / `MoneyReward` / `FullHoldDelay` / `Cooldown` / `RestTime` | 2 s / **$8** ⚠ / 1.5 / 5 / 2 s | reward 10 |
| Wire | `wireTaskMoneyReward` | $20 | same |
| Pawn values (**no longer used for catalog items** after Phase 2.3: the item catalog decides; they only still drive objects without a `ShopItemBinding`, i.e. nothing important) | `eggTimerPawnValue` / `powerBankPawnValue` / `clickTaskPawnValue` / `pictureClickTaskPawnValue` / `wireTaskPawnValue` | 150 / 45 / 40 / 40 / 15 | same |
| | `cardBoxPawnValue` / `nineStackPawnValue` / `laptopPawnValue` | **48 / 24 / 120** (legacy: no longer read, catalog items pay 80% of the price paid) | 180 / 100 / 300 |
| | `tripleStackPawnValue` (never paid: stack unpawnable) | 50 | same |
| | `junkSmall/Medium/LargePawnValue` / `unrevealedCardSellValue` | 5 / 15 / 30 / 10 | same |
| Billiards economy | `billiardsBetMin/Max/Step` | $10 / $40 / $5 | same |
| | `billiardsParOffsets` (Easy/Med/Hard) | [4, 2, 0] | same |
| | `billiardsTargetRate` / `billiardsAssumedWinRate` | [1.5, 3, 4] / [0.85, 0.65, 0.5] | same |
| | `billiardsSecondsPerShot` / `billiardsAssist` | 7 / [FullGhost, ContactPoint, DirectionOnly] | same |
| Billiards rules | `billiardsStrictFirstContact` / `billiardsFinalWrongPocketFails` | true / true | same |
| Billiards physics ⚠ re-run verifier after any change | `billiardsBallRadius` / `CornerPocketRadius` / `SidePocketRadius` | **0.0614 / 0.12 / 0.09** ⚠ | 0.0714 / 0.15 / 0.12 |
| | `MaxShotSpeed` / `RollingDecel` / `BallRestitution` / `CushionRestitution` / `StopSpeed` / `MaxShotSeconds` | 5 / 1.2 / 0.95 / 0.75 / 0.01 / 20 | same |
| Camera (new, Han) | `roomCameraSize` / `deskCameraSize` / `doorCameraSize` / `cameraFocusSeconds` | 14 / 6 / 7 / 0.5 s (C# defaults until the asset is touched) | same |
| Reward Popup (Han; replaced the old `moneyPopup*` fields) | `rewardPopupSeconds` / `RisePixels` / `MaxActive` / `StackSeconds` / `PopSeconds` | 0.9 s / 48 px / 8 / 0.25 s / 0.1 s (serialized) | same |
| | `rewardPopupMoneyColor` / `TimeColor` | green `#4CAF72` / blue `#4FC3F7` (serialized) | same |
| Highlight Hover | `hoverGlowColor` / `WidthPixels` / `FadeInSeconds` / `FadeOutSeconds` | `#FFD166` / 3 px / 0.1 s / 0.15 s (C# defaults until the asset is touched) | same |
| Alternate endings (DESIGN §5.14 D; all C# defaults until the asset is touched) | `luckyNineJackpotChance` / `luckyNineCurseChance` | 0.006 / 0.01 per ticket | same |
| | `luckyNineCurseOmenWindows` / `curseBeatSeconds` | 6 windows / 1.2 s | same |
| | `miningStreakTarget` / `miningFullVeinStreakChance` | 3 veins / 0.2 per vein | same |
| | `blackjackCharlieCards` / `blackjackCharlieChance` | 7 cards / 0.04 per hand | same |
| Hints | `hintHoldSeconds` / `hintFadeSeconds` / `hintRepeatCooldown` | 2.5 / 0.5 / 3 | same |
| Run rating | weights outcome / survival / money; `ratingMoneyHalfPoint` | 0.5 / 0.3 / 0.2; 300 | same |
| | outcome scores ClockWin / SetCompletion / FCWin / ClockLoss / FCLoss / Jester | 1 / 0.95 / 0.95 / 0 / 0.1 / 0 | same |
| | `ratingMechanicWin` / `ratingMechanicLoss` (Jackpot, Mother Lode, Charlie / Curse) | 0.95 / 0 (C# default until the asset is touched) | same |

Billiards validation (`GameConfig.ValidateBilliards`): `maxShotSpeed/120 < ballRadius` (0.042 < 0.0614 ✓), corner ≥ 1.9 × ball (0.12 ≥ 0.1167, 3% margin), side ≥ 1.4 × ball (0.09 ≥ 0.086, 5% margin). Failing it shows "Billiards unavailable".

Other tuning assets: `BlockConfig.asset` (block HP, §13.2), `TripleMatchSymbols.asset` / `LuckyNineSymbols.asset` (§11.3; weights and multipliers live there, not in `GameConfig`), each `CardSO` asset (durations/multipliers), Billiards preset JSONs.

---

## 11. Desk items

### 11.1 Inventory

| Item | Class | Mechanic | Reward (live) | Pawn |
|---|---|---|---|---|
| Egg Timer | `EggTimerTask : HoldTask` | hold 1 s (release resets), then rests `eggTimerCooldown` 0.75 s | **+3 s clock** (net ≈ +0.71 s/s) | **not pawnable** (starting kit, catalog row `egg_timer`) |
| Power Bank | `PowerBankTask : HoldTask` | hold 2 s cumulative | $8, then locked 8.5 s (1.5 + 5 + 2) | shop item ($25); pawns for 80% of the price paid ($20); layer 3 |
| Bear | `ClickTask` | click 3–8 times (rolled) | $5, then 3 s lock | **unpawnable** (`pawnable: 0` on prefab) |
| Picture | `PictureClickTask : ClickTask` | same, own config | $5, 3 s lock | **not pawnable** (starting kit) |
| Wire puzzle (ElectricSocket) | `WireTask` | drag 5 plugs to shuffled sockets | $20 once; **unlocks the Laptop** | **not pawnable** (`CanBePawned` false; part of the wall) |
| Laptop (desk opener) | `DeskLaptopOpener` | opens laptop panel unless `Disabled` | apps (§13) | **$108 ($135 catalog value × 80%), pawnable** by design (the only pawnable starting item; removes all apps until it is bought back from the Shop's PAWNED tab, $142-$156) |
| Backpack | `JunkItem` (Medium) | none | — | $15 |
| Card Box | `CardBox` | buy a 3-card pack; the pool skips cards whose license is not owned (`CardSO.IsAvailable`) | §12 | shop item ($40); pawns for $32 |
| Triple Match stack | `TicketStack` | sells $25 tickets | §11.3 | **unpawnable** (`CanBePawned => IsNine`) |
| Lucky Nine stack | `TicketStack` | sells $55 tickets | §11.3 | shop item ($30); pawns for $24 |
| Printer | `Pawn` | drag onto an item to pawn it | money | — |
| Phone | `PhoneController : DeskObject` | rings | — | **never** |
| Tax letter | `LetterView` (Door view) | paper copy of the bill; opens `Tax` | — | no |
| Cat | `CatController` | roams, blocks | — | no |
| Card Collection | binder UI | view only | — | no |
| Meter | uGUI | money → clock | — | — |

### 11.2 Tasks

- `TaskBase.CompleteTask()` → `ApplyReward()` → `OnTaskCompleted()`; tasks pay **money**, only the Egg Timer pays clock (documented I3 exception).
- `ClickTask`: required clicks rolled uniform 3..8 in `Start` and after each lock; clicks during the lock show `ClickCooling`.
- `HoldTask`: progress while held, completes at `CrankTime`; `EggTimerTask` adds `AddSeconds(timePayout, "hold task")`. **Landmine L1 is fixed (Phase 2.3):** after each crank `EggTimerTask` ignores input for `eggTimerCooldown` 0.75 s (via `HoldTask.CanStartHold`; the Power Bank shares `HoldTask` but has its own cooldown) → 3 s per 1.75 s cycle = **+0.71 s/s net**, untaxed. Arithmetic, not a playtest.
- `PowerBankTask` overrides the reward to money and does not reset progress on release.
- `WireTask`: sets `LaptopController.Disabled = true` in `Start`/`BeginPuzzle`, false on completion, **true again in `OnPawned`** (bug, §15). Completing it makes it non-interactive, but a Cat block ending re-enables it (`SetInteractable(true)`) and can reopen the puzzle.

### 11.3 Scratch tickets

- `TicketStack` (desk) holds a pool of **2 tickets per tier**; a third purchase shows `TooManyTickets`. Purchase charges `round(baseCost × CostModifier[Ticket])` and rolls every window's symbol **at purchase**. Scratching only reveals.
- **Panel wiring:** each `ScratchTicket` has one reference, `panelView`, to the `ScratchPanelView` on its panel's root (`TriplePanel` / `NinePanel`). The view holds the result label and the Cash Out button and collects the `ScratchWindow`s from its children, so adding a window needs no re-wiring (before: 36 scene overrides).
- Windows: **Triple Match 3**, **Lucky Nine 9**. A window reveals at 60% scratched (`ScratchWindow.revealThreshold`); the ticket is **paid only on Cash Out** (scratching every window just finishes the scratching). `ScratchTicket.Viewed` is the ticket whose panel is open; `RevealWindows(n)` is the dev hook the cheat menu uses.
- **Alternate endings on Lucky Nine (built 2026-10-22):** at purchase one banded roll (`RngService`) decides Jackpot (`luckyNineJackpotChance` 0.006), Curse (`luckyNineCurseChance` 0.01) or normal. A Jackpot ticket is pre-filled with nine of the highest-multiplier (rarest) symbol and, once all windows are scratched, calls `GameOver(TicketJackpotWin)` (no Cash Out needed). A Curse ticket is pre-filled with nine of the lowest-multiplier symbol, gives the hint `TicketCursed` when `luckyNineCurseOmenWindows` (6) windows are revealed, and when fully scratched runs the `CurseBeat` coroutine (shake, red label, Cash Out locked, `curseBeatSeconds` 1.2 s) and then calls `GameOver(TicketCurseLoss)`. Cashing out before every window is scratched forfeits the ending (the ticket pays what its revealed windows pay). `CheatFlags.NextLuckyNine` forces either roll in debug builds. Expected frequency per run is `[model]` only: 1.6% of Lucky Nine tickets end the run.
- **Reward as coded:** `round(Σ over every repeating symbol of baseReward × (count − 1) × multiplier)`, where `count` is per symbol among **revealed** windows only. Only the open ticket updates the result label (the two pooled tickets of a tier share one panel). Cash Out locks the reward computed from revealed windows; cashing out unscratched pays $0.

| Symbols | weights | multipliers |
|---|---|---|
| Triple Match (3) | 50 / 33 / 17 | 0.25 / 0.5 / 1.2 |
| Lucky Nine (5) | 30 / 25 / 20 / 15 / 10 | 0.15 / 0.25 / 0.5 / 0.8 / 1.2 |

| EV (exact enumeration, live values) | Triple Match | Lucky Nine |
|---|---|---|
| Cost / base reward | $25 / 55 | $55 / 26 |
| EV, all windows revealed | **$22.25** (RTP 89.0%) | **$48.62** (RTP 88.4%) |
| P(profit) | 46% | 32% |
| Min / max payout | $0 (17%) / $132 | **$16** (never $0: 9 windows over 5 symbols) / $250 |
| Optimal early stopping | no gain | no gain (more revealed windows can only add to the reward) |

Both tiers are negative-EV gambles, as intended. The values were rebalanced with the new reward formula; the old sets were 75/20/5 and 40/27/18/10/5, then 60/28/12 and 30/25/20/15/10 with other multipliers. Triple Match uses the 1st, 3rd and 5th symbol of Lucky Nine (same relative rarity), so a picture has the same rank in both tickets. Each scratch panel has a `TicketPayoutTable` (`TicketPayoutTable.cs`, on `NinePanel`/`TriplePanel`) that is built once from the symbol asset: one row per symbol (icon, odds, multiplier) plus, on Lucky Nine, the "9x rarest symbol = Win the game" row (pixel layout from `iconSize` / `fontSize`), so it cannot drift from the real weights. Tickets are free when `CheatFlags.FreeTicketsOn`.

### 11.4 Pawning

- Drag the **Printer** (`Pawn`, kinematic trigger) onto an item. It only sees colliders on **layer 3 `DeskObject`** (`pawnableLayers` = 8 in prefab and scene). It then needs `IPawnable` and `CanBePawned`; `CanBePawned` false or a catalog item that pays $0 shows hint `CannotPawn`. An object with no `IPawnable` (Phone, Meter, Clock, Cat) is ignored with no hint (N19). Pays `MoneyService.Add(PawnValue, "pawn")` then calls `OnPawned()`.
- **Catalog items (Phase 2.3):** any object with a `ShopItemBinding` is priced by `ShopService.Ownership.MarkPawned` = `floor(80% × price paid)`; Phone, Egg Timer, Bear, Picture, Triple stack and anything not owned pay 0 with the `CannotPawn` hint. A pawned item with an `IRestorable` component is recorded in `ShopService.Ledger` and can be bought back from the Shop's PAWNED tab (Batch 4, A21). Cards and junk keep their own `PawnValue`. **An active debuff card** (`CardItem.IsDebuffActive`: placed, not yet ended) has `PawnValue` 0; `Pawn` charges `debuffDismissFee` (`TrySpend(fee, "debuff dismiss")`, not income) and pawns it. If the player cannot pay, the pawn is refused: hint `DebuffDismissCost`, and the card is moved to a new spot of the placement area (`CardItem.Relocate()`, deferred to the drop if the card is being dragged). A debuff that has already ended pawns for its normal value. The old `GameConfig` pawn values no longer drive catalog items (§10).
- `IPawnable { int PawnValue; bool CanBePawned; void OnPawned(); }`. Implementors: `ClickTask` (+Picture), `HoldTask` (Egg Timer, Power Bank), `WireTask`, `DeskLaptopOpener`, `CardBox`, `TicketStack`, `JunkItem`, `CardItem`.
- **Every `OnPawned` deactivates (`SetActive(false)`); nothing is destroyed.** Catalog items also implement `IRestorable.OnRestored()` (Batch 4): called when a bought-back parcel is opened, while the object is hidden.
- Five prefabs still carry a stale serialized `pawnValue: 20` that no script reads.
- Pawn proceeds are taxed and count toward run stats.
- Pawn-everything total on a fresh run = Laptop $108 + Backpack $15 = $123 (+ shop items at 80%); see A15 in §18.

### 11.5 Phone (built in Batch 1)

- **Phone (rebuilt in Phase 1.3, `PhoneController` on the desk object `DeskObject/Phone`).** `CallScheduler`: a call every 30–60 s of running clock (×0.5 under Robocalls), one at a time, 12 s gap, ring times out at 10 s (missed). Caller ID weights 25/25/40/10 (Telemarketer / Client / Unknown / ITRS; **DESIGN §5.9 says 40/25/25/10, left as is for now, §17.5**; ITRS only while a bill is pending; Unknown is 60% telemarketer, 40% client; so with no bill 54% of calls are Unknown, and about 54% of all calls are really telemarketers). **While ringing:** clock ×1.5 (speed key = the phone), own `AudioSource` (no longer shared with the tax notice), desk phone shakes and shows the chat bubble, and an "Incoming call" pill shows at the top when a device hides the desk. **Answering:** clicking the desk phone only opens the phone-only view; the call is answered and declined there (declining is free). While ringing the screen shows only the caller ID (name + picture, which is the shown-as kind, so "Unknown" hides the truth) and **ACCEPT / DECLINE**: no details, no timers. **After ACCEPT the call is a short conversation** (`CallerProfileCatalog` scripts, edited by Ari/Han; `CallDialogue` is the pure line counter): DECLINE greys out for the whole call; each caller line is shown with the player's reply button ("Got you."); the **last line** is the payoff, its button reads "Hang up.", and a stalling player is hung up on after `callHangUpDelay` (5 real seconds, never shown). Client: clock **paused** from ACCEPT, Laptop locked, the phone can't be closed; on the last line +`clientGigMoney` ($22, reason `client`) goes to the player's money; a client who never answers the first line says "Hello? Are you there?" after `beneficialCallTimeout` (8 real seconds, hidden), shows it for `callGoodbyeSeconds` (2 real seconds) with the reply button greyed, then hangs up (no money). Telemarketer: clock keeps **running**; the first reply is greyed out for `telemarketerPitch` game seconds (hidden); no reward. ITRS: clock paused; on the last line the open bill gets `itrsCallExtension` (+15 s) more time; if the bill was already paid the ITRS agent says so and the +15 s is added to the **gap before the next bill** (45 s becomes 60 s; `ITRSService.GrantNextBillTime`; the 30 s pay window is unchanged). After hang-up the Call window returns to the call history. Every call (answered, declined, missed) is a `CallRecord` in the history; missed and declined calls also go to the Inbox. **Power:** side button, Off → Booting (5 s of running clock) → On; off means no rings (due calls are logged missed, never queued), no ×1.5, banners dropped. **Cable Eater** (`DoNotDisturbCardSO`) forces it off and kills the button for its duration; the phone stays off afterwards. Game over or restart releases every hold (verified).
- **Phone UI** (inside `DeviceLayer`, placeholder art from `Assets/Art/Phone UI/Smartphone.png`, body sprite `Smartphone_70`, icons `Smartphone_0/24/34`): home screen with Call, Inbox (unread badge) and Taxes icons; in the phone-only view each app opens in **its own window frame** (sprites from `Assets/Art/Phone UI/Premade Menus.png`, scaled ×2.5, pointer tail aimed at the phone) to the right of the phone: `CallWindow` teal (`Premade Menus_72`), `InboxWindow` magenta (`_74`), `TaxesWindow` yellow (`_75`); blue (`_73`) is kept for the Shop. `PhoneHome` shows one window at a time. **Every text under `PhoneSlot` uses `BoldPixels Pixel.asset`**, sized in multiples of 16 (the banners, the incoming-call pill and the Laptop clock are outside `PhoneSlot` and still use the old font). **Call app = `CallScreen`:** a headline, a `Body` vertical layout group (caller picture in a circle = ring + `Mask` + nested `Portrait` image, then the caller's name, then what the caller says; there is no timer label) and a `Buttons` horizontal layout group (`Accept` / `Decline`, which also hold the other call actions). Caller names and pictures come from **`Assets/Settings/Configs/CallerProfiles.asset`** (`CallerProfileCatalog`: lists per caller kind, each with a `displayName` and a square `portrait`; one entry is picked at random per call and stored in `CallInfo.profileSeed`, so the ringing and answered screens show the same face; an empty portrait shows the blank circle). **Inbox and call history share one list + reading-pane layout** drawn from the Premade Menus Mail pieces (row `_126` / selected row `_127`, scroll track `_52`, handle dot `_123`, pane `_53`; all positions are the frame's native pixels x2.5): `PhoneListView` (a ScrollRect with a hand-synced `Scrollbar` so the handle stays a fixed dot, rows cloned from a template, click highlights a row) + `PhoneListRow`. **Inbox** = `InboxScreen`: every message, newest first, an icon (envelope; alert icon for tax bills and missed calls) and an unread dot per row, click to read (marks read), "FROM:" shows the sender kind, an "Open" action for `app:taxes`, license actions shown but inert until Batch 2. **Call history** = `CallHistoryView` (the `History` object under `CallWindow`, shown by `CallScreen` only when no call rings or is active): one row per call (icon for answered vs missed/declined, newest first, 30 kept), the right pane shows the outcome, the result (e.g. +$22) and the transcript. (Other apps: **Taxes** = `TaxesScreen`: bill, seconds left, Pay; the Taxes icon shows a badge while a bill is open). **Banners** = `BannerQueue` logic (the `BannerStack` view was deleted; the banners are not shown. Originally top-right of the screen, max 3, `bannerLifetime` real seconds, only while the phone is usable; a phone-top variant waits for final art).
- Shop app (Phase 2.2, built): see below. The Shop icon and the Shop window title icon are placeholders (the Taxes icon tinted blue, an envelope) until Han draws them. Icon layout and status bar are placeholders until Ari arranges them.

### 11.6 Cat

- `CatStateMachine` states **Wandering → SittingIdle → Wandering**; while Wandering, touching an interactable rolls `catBlockChance` 0.4 → **SittingBlocking** for 3–6 s (`SetInteractable(false, CatBlocking)`); **Dragged** while the player drags it. Sit 6–14 s (Idle/Lying families, nested Sleep 3–6 s). Moves by transform, 1.5 u/s.
- Not routed through `AnnoyanceManager` by decision D8. It can block anything with a `PointerReceiver`, including the Phone and the Printer.

---

## 12. Cards

### 12.1 Data model

- `CardSO` (`Create > Overtime > Cards > Normal`): `cardName, image, description, rarity, weight, pawnValue, hideUntilCollected, collectionRegisterAs`, plus the virtual `IsAvailable` (default true; `CardBox.RollCard` skips cards that return false). A plain `CardSO` (no effect) is a valid collectible.
- `Rarity { C, R, SR, UR }` (weights 60/25/12/3). Older docs say Common/Uncommon/Rare/Legendary — **"Rare" means a different tier there**; use the code names.
- `EffectCardSO` (abstract): `_isDebuff` (`[FormerlySerializedAs("isDebuff")]`), `BlocksPawning` (false by default), `OnPlaced(CardItem)`, `OnRemoved(CardItem)`, `OnPawned`. Each timed subclass declares its own `duration`.

| Class | Fields (defaults) | Effect | Ends when |
|---|---|---|---|
| `ClockSpeedCardSO` | duration 15, tickMultiplier 0.75 | `SetSpeed(card, x)` / `ClearSpeed(card)` | timer |
| `CableEaterCardSo` (Cable Eater; replaced `DoNotDisturbCardSO`) | duration 20 | `PhoneController.ForceOff(card)` + `LaptopLock.Add(card)`: the phone is forced off with a dead power button **and the Laptop is locked**; released in `OnRemoved`. Asset: debuff, SR, weight 20, pawn $10 [verified] | timer |
| `RobocallsCardSO` | duration 30 | `PhoneController.AddRobocalls(card)` / `RemoveRobocalls(card)`: call interval ×0.5 | timer |
| `DisableAppIconCardSO` | duration 15, targetAppName | hides an icon (open app keeps running). `IsAvailable` = `IconGate.IsLicensed(targetAppName)`: not rolled until the player owns that app | timer |
| `TaskCostModifierCardSO` | target, ×1.5, 20 s | `CostModifierService.SetMultiplier(target, card, x)` (keyed by the card, so two copies multiply instead of overwriting). `IsAvailable` is false for the Blackjack / Billiards targets until that license is owned | timer |
| `AuditCardSO` | — | force a bill or double the open one | bill resolved; `OnRemoved` detaches its handler (N5 fixed) |
| `TaxExemptionCardSO` | — | `SkipNextBill(card)` / withdraw | exempt bill resolved |
| `OvertimeCardSO` | ×2 | next mining vein multiplier | vein banked |
| `ButterfingersCardSO` | — | next vein pays 0 | vein banked |
| `InsiderTipCardSO` | — | pre-reveal one window on next ticket | ticket opened |
| `CreepyJesterCardSO` | artPool, interval 20 s | swaps art each tick; last art → `PlayerPrefs.DeleteAll()` + `GameOver(CreepyJesterLoss)` | never by itself |
| `FinalCountdownCardSO`, `SetCompletionCardSO` (+ trackers) | — | §12.5 | — |

### 12.2 Lifecycle

1. **Buy a pack** at the Card Box: needs `CardHandManager.HasRoom` (placed < `handCap` 5) else hint `HandFull`; costs `round(35 × CostModifier[CardPack])`.
2. **Roll** 3 cards independently (duplicates possible): rarity by weight, then a card of that rarity by `CardSO.weight` (`RngService`). Cards with `IsAvailable == false` (Blackjack/Billiards cards before their license) are skipped. Empty rarity → whole pool. All-zero weights → always the first card.
3. **Reveal panel:** pack image, then 3 face-down slots resolved in any order. Before reveal: **Sell $10** (works on debuffs too). After reveal: **Place** always; **Collection** and **Sell (pawnValue)** hidden for debuffs.
4. **Place** → a pooled `CardItem` (7 fixed instances in the scene, never instantiated) → `Setup(card)` → `OnPlaced`. `TryPlaceCard` checks only the pool, **not the hand cap**, so a pack bought at 4 placed can reach 7.
4a. **Pawning an active debuff** (`CardItem.IsDebuffActive`): the Printer charges `debuffDismissFee` ($6, `GameConfig`) and pays nothing; without the money it shows `DebuffDismissCost` and `CardItem.Relocate()` moves the card to another placement spot (after the drop if it is held). An ended card pays `pawnValue` as before.
5. **Effect ends** (`EndEffect`, guarded by `_hasEnded`): `OnRemoved`, card **flips face-down and stays**, still using a hand slot, and its type is registered in the Collection. Clicking a face-up card opens `CardPanel` (pauses the game).
6. **Pawn** the card (only way to clear a slot): releases the slot, ends the effect early if still running (**no collection credit**), pays full `pawnValue` even for spent cards, returns to pool.
7. Scene reload: `CardItem.OnDestroy` runs `OnRemoved` once if needed; every effect static is cleared on `sceneLoaded`.

`Collection` is a registry ("ever completed this type"), not storage. `CardCollectionPanel` shows a paged binder; `hideUntilCollected` cards stay hidden until collected.

### 12.3 Card pool (29 assets in `Resources/CardSO`; Cable Eater, Time Scarab and Clock Up re-read on 2026-10-20, the rest per the 2026-10-17 audit and DESIGN §5.4)

| Card (display name) | Asset | Rarity / weight / pawn | Effect |
|---|---|---|---|
| Snail Speed | SnailSpeed | C / 11 / 2 | clock ×0.75, timed |
| Clock Up! | ClockUp | R / 10 / 6 · debuff [verified] | clock ×1.5, 20 s |
| Time Scarab | TimeScarab | SR / 18 / 14 · buff [verified] | clock ×0 for 20 s; the player can still act (allowed by I2 v2.1) |
| **Cable Eater** | CableEater | SR / 20 / 10 · debuff [verified] | phone forced off **and Laptop locked** for 20 s |
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
| Close The Shop | DisableShop | R / 12 / 6 [verified] | debuff, hides the phone's "Shop" icon through `IconGate` (case-insensitive, so the asset's "Shop" matches icon id "shop"); closes the Shop window if it is open |
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

**The three Billiards cards (Chalk Up, Trick Shot, Cue Slip) were removed** (Han, `b768787`): their classes and assets are gone. `BilliardsPendingEffects` and its hooks in `BilliardsApp` / `CueAimController` / the self-tests remain with nothing queuing into them (orphan code; clean up when Ari next touches Billiards).

### 12.4 Card notes

- Debuffs bypass the cap only in the sense that placement never checks the cap; the cap gates **buying**. Keeping 5 cards on the desk dodges new debuffs (accepted trade-off).
- Card Box pawns for $32 (80% of its $40 catalog price) against a $35 pack, so pawning a box always loses money; spent cards pawn for full value; an **active debuff** costs `debuffDismissFee` ($6) to pawn instead (§11.4).
- The licensed-app cards (No More Gambling, Not Valid 8-Ball, Bet More Money, High Risk High Reward, plus any `DisableAppIcon` card aimed at a licensed app) appear in packs only after the matching license is bought, so a pack never rolls a card that does nothing.
- `CardItem.EndEffect` registers `_data`, while reveal-to-Collection registers `CollectionTarget` — inconsistent for alias cards (Jester → Han).

### 12.5 Win/lose cards (`WinConditionCard/`, described from GAME_STRUCTURE — not re-read in the 2026-10-22 sync; the folder now holds `FinalCountdown/{FinalCountdownCardSO,FinalCountdownTracker}` and `SetCompletion/{SetCompletionCardSO,SetCompletionTracker}`)

- **Final Countdown** (`FinalCountdownTracker`: None → Armed → Triggered). First placement arms `ClockService.ArmFinalCountdownIntercept(180)` (Meter still works). When the clock hits 0 it resets to 180 s, **locks the Meter for the rest of the run** (`HandleTriggered`, as DESIGN §5.14 B says; the 2026-10-17 text had this backwards) and plays the intro (clock paused during it). A second card placed while **Armed = loss**, while **Triggered = win**. Pawning the armed card is an escape that costs `debuffDismissFee` (an Armed card counts as a running debuff: `FinalCountdownCardSO.isDebuff`; pays nothing, refused without the money). `ClockService.Spend` (ITRS unpaid-bill penalty, Blackjack ante) bypasses the intercept **by design** (DESIGN §5.14 B) and ends the run as `ClockLoss` whatever the phase.
- **Set Completion / "MASCH"** (anchor **Mei**): wins when Mei plus one card of each group is on the desk: {Han or Creepy Jester}, {Ari}, {Sybil}, {CtrlZ} — exactly 5 cards = the whole hand at `handCap` 5.
- **Creepy Jester**: §12.1. `artPool` length 0 or 1 throws on the first tick.

---

## 13. Laptop and apps

### 13.1 Shell

- **`DeviceLayer`** (prefab `Assets/Design/Prefab/Panels/DeviceLayer.prefab`, scene: `Canvas/DeviceLayer`, between `Timer` and `EndGamePanel`): parts `Blur`, `LaptopSlot` (`AppsHost` holds the `Laptop` panel and the Mining, Billiards, Blackjack apps; `LockCover`; `LaptopClock`), `PhoneSlot` (placeholder phone, `InputBlocker`, clock, close button). Modes `None` / `Laptop` / `Phone`. Laptop home: slot at x −60 with the phone docked on the right edge (x 360); tapping the phone slides the Laptop to x −1000 and shows the phone at centre (0.25 s, unscaled DOTween). Placeholder phone is 195 × 330 (dock x 372, hide offset 260). While a Laptop **app** is open the Laptop slides to the centre (x 0) and the docked phone slides off the right edge and hides; both slide back when the app closes. `PickaxeView` keeps its home position in **local** space so the slide cannot strand it. Blur: one `ScreenCapture` per first open, 1/8 RenderTexture, raycast target on (blocks Meter Feed); falls back to tint. `LockLaptop(holder)/UnlockLaptop(holder)` cover the Laptop and set `LaptopController.IsLocked`, which freezes `PickaxeView` hits and a rolling Billiards shot. `LaptopController` reports open/close and app open/close to it. Verified in Editor Play mode by stepping frames (states, slides, lock); the blur image itself needs a focused Game view or a WebGL build (Ari).
- `DeskLaptopOpener` (desk sprite): opens the panel unless `LaptopController.Disabled` (hint `LaptopNoPower`). Pawnable for $108 (catalog value $135, A21).
- `LaptopController`: one app at a time. `RequestOpen(app)` runs a switch coroutine: overlay → close current (`OnAppClosed`) → wait `laptopSwitchTime` **1 s (also on the first open from home)** → open. `CloseCurrentApp` is free and cancels a pending switch. `Disabled` is only written by `WireTask`.
- `LaptopHome` + `DesktopIconView`: icons need a **double click**; apps are matched by string (`LaptopApp.AppName == DesktopIconView.appName`). `DesktopIconView.SetVisible(appName, bool)` works before the icon wakes (static `s_pendingHidden`, a set — not counted, so two overlapping hide cards end at the first expiry).
- `CloseAppButton`: closes the app or the whole laptop.
- Leaving an app runs `OnAppClosed`: Mining forfeits unbanked ore, Blackjack forfeits the hand and resets Hot Table, Billiards forfeits only a rolling shot.

### 13.2 Mining (`MiningApp`, `PickaxeView`)

- **Input:** hold the pickaxe, drag it into the ore and back out = one swing; `hitCooldown` **0.7 s** (literal on `PickaxeView`, not GameConfig). No hold mechanic any more (landmine L8, design question open).
- **Vein:** length uniform **6–10**, shown up front by status nodes (L7 fixed). Blocks always in order: Dirt 1 HP, Wood 2, Stone 2, Coal 3, Copper 3, Iron 4, Quartz 4, Gold 5, Diamond 6, Ruby 7. Payout per block from `blockPayout`; vein totals L6 $29 / L7 $41 / L8 $56 / L9 $73 / L10 $94 (mean **$58.6, 25 hits**).
- **Banking:** paid only when the vein completes; closing/switching mid-vein zeroes the unbanked ore (but block damage persists, so reopening resumes the vein with nothing banked). Respawn 1 s.
- Overtime (×2) and Butterfingers (slip → 0) queue through statics (`QueueNextVeinMultiplier` / `QueueNextVeinSlip`, each with the owning card), reset on scene load. "Received" label ignores the Overtime multiplier.
- **Mother Lode (ending, built 2026-10-22):** `_fullVeinStreak` counts consecutive banked veins of maximum length (`MaxVeinLength` = min of `veinLengthMax`, the block list and the status nodes). While the streak is above 0, the next vein is maximum with `miningFullVeinStreakChance` (0.2; this **replaces** the normal 1-in-5, it does not add) and otherwise 6–9. Banking the `miningStreakTarget`-th (3) full vein calls `GameOver(MotherLodeWin)`. A slip (Butterfingers) or a shorter vein sets the streak to 0, and so does closing the app (`OnAppClosed`). Reaching `target − 1` shows the hint `MotherLodeTease`. No progress label (decision A22). Dev hooks: `CheatFlags.PendingMiningStreak`, `ForceFullVein`, `MiningApp.DebugFinishVein()`. `BankVein()` returns whether the run was won; `CompleteVein()` finishes the vein.
- **Throughput ceiling $3.17/s** (perfect 0.7 s rhythm), ~$2.25/s at 1 swing/s; $2.53/s after 20% tax.

### 13.3 Blackjack (`BlackjackApp`, `BlackjackGame`)

- Rules: fresh 52-card deck per hand (seeded shuffle), dealer stands on all 17s, double on first two cards only, no split/insurance/surrender, **no dealer peek**. Naturals resolve on deal.
- Ante **20 s of clock** × `CostModifier[Blackjack]`; deal/double need clock ≥ ante + **15 s** margin (35 s). Double spends a second ante.
- Payouts: win/dealer bust **$100**, push **$25**, natural **$130** (× 2 if doubled).
- **Hot Table:** payout × (1 + min(streak × 0.10, 0.50)); streak +1 per win, kept on push, reset on loss/bust and **on closing or switching the app**. Money only.
- **Pay before reveal (N14 fixed 2026-10-22):** `Resolve()` settles the hand and credits the payout (`MoneyService.Add`, `_totalReceived`) **before** the `blackjackRevealDelay` wait, so closing the app during the reveal no longer forfeits a won hand.
- **Charlie (ending, built 2026-10-22):** at each deal `BlackjackApp.OnDeal` rolls `CheatFlags.ConsumeCharlie() || NextDouble() < blackjackCharlieChance` (0.04). On a hit `BlackjackGame.Deal(charlieCards)` rigs the player's draws through a `_rigged` queue of low cards (2s and 3s), so a patient player can reach 7 cards. `HasCharlie(n)`; reaching `blackjackCharlieCards` without busting calls `GameOver(BlackjackCharlieWin)` immediately. Doubling down clears the rig. From the 4th card on, every Hit shows the hint `CharlieTease` (`ShowAlways`, bypasses the cooldown). Normal hands and EV are unchanged (the 4% rigged hands only help the player).
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

**Balance status:** model numbers only (no real playtest rounds). Typical player at $40: Easy +$1.6/s, Medium +$4.3/s, Hard +$4.4/s; choosing difficulty after seeing the table adds ~$1/s. BILLIARDS_BALANCE's recommendation (0.6/1.8/2.3) is **overridden**: retune so Easy/Med/Hard ≈ 0.6×/1.0×/1.3× Mining ≈ **1.9/3.2/4.1 $/s** after measuring real rounds (pending). Per-table `winRates` in preset JSON proposed, not built. Billiards is now behind a license (`license_billiards.asset`; Blackjack: `license_blackjack.asset`), bought in the Shop app.

---

## 14. Economy summary

| Source | Rate (pre-tax unless noted) | Notes |
|---|---|---|
| Egg Timer | **+0.71 s/s net** (ideal), untaxed | 0.75 s cooldown after each 1 s crank (L1 fixed) |
| Bear / Picture | ≤ $1.67/s each (≈ $1.0–1.2/s real) | independent locks, can alternate |
| Power Bank | $0.76/s | |
| Wire puzzle | $20 once | gates laptop |
| Mining | ≤ $3.17/s ($2.53 after tax) | free, no capital |
| Blackjack | ~$5–9/s | needs clock ≥ 35 s, burns ~31 s per hand |
| Billiards | ~$1.6 / $4.3 / $4.4 per s (E/M/H, model) | needs ≤ $40 capital |
| Triple Match | −$2.75 per ticket (RTP 89.0%) | 46% chance of profit |
| Lucky Nine | −$6.38 per ticket (RTP 88.4%; stopping early never pays more) | 32% chance of profit; 1.6% of tickets are a Jackpot or a Curse (ending) |
| Phone ring | costs ≈ 0.75–5 s of clock each | ~9% extra drain if always ignored |
| ITRS | 20% of gross income, min $20 / 45 s | ignore = 2 s per $ |

Baseline to beat: **$3/s**. Any future paid earner (licenses in the shop plan) must beat free Mining or do something Mining can't.

---

## 15. Known issues (open)

Only genuine defects live here. Behaviour that matches `DESIGN.md` is not a bug (see §17.3 for designed-but-unbuilt features and §18 for deliberate choices). Ranked by impact. IDs: `L#` landmine pass (2026-08-31), `P#` Phone-plan problem list, `N#` audits (2026-10-17, 2026-10-22). When you fix one, move it to §16 in the same commit.

| # | Sev | Issue | Where | Fix / owner |
|---|---|---|---|---|
| — | Med | Blackjack ≈ 2–3× Mining after the hotfix | `GameConfig` Blackjack | playtest, then tune `blackjackWinPayout` / ante / cycle time |
| — | Med | Billiards payouts not retuned (targets still 1.5/3/4) | `billiardsTargetRate` | measure 20 real rounds → 1.9/3.2/4.1 $/s |
| N2 | Low | Consumers cache `MoneyService.Current` in their `Start` (`Meter` now subscribes in `Start`, so it is safe) | `ITRSService`, `RunStatsService` | safe only with the execution order in §5.4; move to lazy init |
| N3 | Med | Pawning, card sales (and future buyback refunds) count as income for ITRS and run stats | `ITRSService`, `RunStatsService` | non-income reason class before the shop/buyback |
| — | Med | `PointerReceiver.SetInteractable` is a bool: a Cat block ending re-enables a solved wire puzzle or a cooling Power Bank | `PointerReceiver` | refcount / reason set |
| L10 | Low | `ultrRareWeight` typo; docs used other rarity names | `GameConfig.cs` | rename with `[FormerlySerializedAs]` |
| L12a | Low | Vestigial `InputManager.FocusRoot` | `InputManager` | delete |
| — | Low | `TicketSymbolConfig` lives in `TicketSymbolPool.cs` | `Config/` | rename file (keep `.meta`) |
| — | Low | `CreepyJesterCardSO` `artPool` index is unguarded (length 0 or 1 throws on the first tick). The `PlayerPrefs.DeleteAll()` wipe is **deliberate** (DESIGN §5.14 C), not a bug | `CreepyJesterCardSO` | guard the index |
| N10 | Low | `AddSeconds/Spend` discard `reason` (no clock balance log) | `ClockService` | `OnClockChanged(delta, reason)` event |
| — | Low | Collection saved by asset name, comma-joined | `CardCollectionManager` | never rename card assets; avoid commas |
| — | Med | **47 of 160 `GameConfig` fields are not serialized in the asset** (§10): they run on C# defaults, which are the intended values today but are invisible to the Inspector and to anyone editing the asset | `GameConfig.asset` | Ari: select the asset in the Inspector and save |
| — | Low | Stale C# defaults on ~20 older GameConfig fields (§10) | `GameConfig.cs` | update defaults to the live values |
| N16 | Low | Phone call weights in code/asset are 25/25/40/10 (Telemarketer / Client / Unknown / ITRS) while DESIGN §5.9 says 40/25/25/10 | `GameConfig` call weights | decision pending (§17.5) |
| N17 | Low | `CardPanel` shows the card's normal `pawnValue` even for an active debuff, whose real pawn result is a $6 fee | `CardPanel` | show the fee for an active debuff |
| N18 | Low | The scene has more than one cutscene root (audit item A4); not yet merged into one | `SampleScene.unity` | Editor step (A4), Han |
| N19 | Low | Pawning the Phone (and other protected objects with no `IPawnable`: Meter, Clock, Cat) does nothing and shows no hint; DESIGN §5.11 says trying to Pawn a protected object shows a hint. Found by PlayMode `ShopTests.TC_SHP_12b_CamDoPhone_BiTuChoiCoGoiY` (fails until fixed) | `Pawn.OnTriggerEnter2D` | show `CannotPawn` when the object on layer 3 has no `IPawnable` |
| — | Info | Orphan code: `BilliardsPendingEffects` and its hooks (the three Billiards cards were removed). Dead code: `BlackjackApp.Banned`, `BlackjackGame.Payout`, unused event `ScratchTicket.OnPreRevealDelivered`; stale `pawnValue: 20` in 5 prefabs | various | clean when touched |
| — | Info | ~45 `Instance?.` uses against the `!= null` convention | mostly `HintService` | clean when touched |

---

## 16. Fixed issues ledger

| ID | Was | Fixed by |
|---|---|---|
| I5 | Meter Feed worked while the CardPanel froze game time (the Meter sits above most panels) | `Meter.HandleFeedClicked` returns when `Time.timeScale == 0` (2026-10); the Laptop, Phone, intro and end panels already cover it |
| N4 | `CostModifierService` overwrite: two cost cards on one target, first expiry reset both | keyed multipliers, one per card, multiplied together (`SetMultiplier(target, card, value)` / `ClearMultiplier`) (2026-10) |
| N14 | Blackjack forfeited a won hand closed during the reveal | `BlackjackApp.Resolve` pays before the animation (2026-10) |
| N6 | Cat can block the Phone and the tax notice | Closed as not a bug (2026-10): the Cat blocking the Phone is intended, and the tax notice is part of the Phone now |
| L2 | Lucky Nine always profitable (EV $203 on $55) | `luckyNineBaseReward` 110 → 26 (hotfix 2026-10-17). Note: still can't pay $0 (min $16) |
| T1 | Lucky Nine paid by the symbol with the highest count, so stopping early paid more; ticket auto-paid on the last window; the label of a shared panel could show another ticket's reward | Reward is now the sum of every repeating symbol; payout only on Cash Out; only the open ticket updates the label (2026-10) |
| L3 | Phone pawnable | `Phone` no longer implements `IPawnable` |
| L6 | `handCap` 3 in asset | asset 5; `HasRoom` reads config |
| L7 | Vein length invisible | status nodes show the length |
| L9 | All pawn values $20 | per-item pawn fields in `GameConfig` |
| L11 | Fields missing from asset | asset fully serialized |
| L12 (part) | `hitDropChance` stale key; Egg Timer bypassed `AddSeconds` | removed; `HoldTask` uses `AddSeconds` |
| GS | Laptop apps had null service refs | apps use `.Instance` (2026-10-12) |
| GS | `LaptopController._switching` soft-lock | `CancelSwitch` (2026-10-12) |
| GS | Pawn-after-place exploit (Overtime, Butterfingers, InsiderTip, TaxExemption) | card owns its effect; `OnRemoved` withdraws (Billiards Phase 8) |
| N15 | `TaxNotice.cs` orphan (its `Taxes.prefab` was deleted) | script and `.meta` deleted 2026-10-26, with the unused `BilliardsCheats`, `BannerStack` and 20 dead assets |
| N5 | Audit card: `OnRemoved` empty, so after pawning its handler ended a different pooled card | `AuditCardSO` keeps one handler per `CardItem` and `OnRemoved` detaches it (2026-10-22) |
| — | `GameOver` had no re-entry guard | `GameManager.GameOver` returns if `IsGameOver` (2026-10-22) |
| — | `CardItem` timed `Invoke` not cancelled on pawn or reuse | `CancelInvoke(EndEffect)` in `Setup` and `OnPawned` (2026-10-22) |
| — | Cheat menu shipped in builds | `CheatMenuToggle` disables itself unless `Debug.isDebugBuild`; `CheatFlags` is gated the same way (2026-10-22) |
| — | `HoverHighlighter` was a rectangle with serialized values, no `hoverGlow*` | shader glow + `GameConfig.hoverGlow*` (Han) |
| — | Tutorial step 5 told the player to click a door that did not exist | the door exists since Batch 3 |
| — | `Meter` subscribed in `OnEnable` and never unsubscribed; `InputManager` leaked its input actions | `Meter` subscribes in `Start`, unsubscribes in `OnDestroy`; `InputManager.OnDestroy` disposes the actions (2026-10-22) |
| GS | Mining/Ticket statics survived restart | reset on `sceneLoaded` |
| GS | Mining vein respawn soft-lock | `OnEnable` restarts a finished vein |
| GS | ClickTask first click always completed | required clicks rolled in `Start` |
| GS | Card weights all 0 | weights set (3 assets verified, rest per ITEMS_INVENTORY) |
| GS | `CostModifier` couldn't reach Blackjack/Billiards | `Target { Ticket, CardPack, Blackjack, Billiards }` |
| P11 | Pawned objects `Destroy()`ed | every `OnPawned` deactivates |
| P6 | ITRS issued a new bill every 45 s over an unpaid one | no new bill while one is open (Phase 1.4; tested) |
| — | Tax panel showed a stale amount after Audit doubled the bill | `Tax` reads the live amount; `AssessmentChanged` fires (Phase 1.4) |
| P7 | Phone and TaxNotice shared one AudioSource | the phone has its own `AudioSource` (Phase 1.3, scene edit on `DeskObject/Phone`) |
| I8 / P5 | `AnnoyanceManager` absent from the scene, so nothing enforced one call at a time | `CallScheduler` (tested: one at a time, 12 s gap); class deleted (Phase 1.3) |
| L5 / P1 | `TickMultiplier` last-write-wins (Phone vs clock cards) | keyed `SpeedModifiers` in `ClockService` (`SetSpeed/ClearSpeed`); `TickMultiplier` is read-only (Phase 1.1, `ari/Phone-Revamp`; self-tests + Play check, Final Countdown intro not yet Play-tested) |
| L1 | Egg Timer +2 s/s net, no limit | `eggTimerCooldown` 0.75 s in `EggTimerTask` (Phase 2.3) → +0.71 s/s |
| L4b | Pawning the wire puzzle disabled the Laptop forever | `WireTask.CanBePawned` is false (Phase 2.3) |
| P8 | Icon hiding was a set, not counted | `IconGate` with per-app card hold counts and `HideReason` flags (Phase 2.2) |
| P9 | "Close The Shop" could not hide the Shop icon | `DisableShop` goes through `IconGate` (Phase 2.2) |
| — | Camera kept scrolling if a held arrow was deactivated | the vertical scroller and `HoldButton` were removed with the three-viewpoint `CameraRig` |
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
| Ari, 2026-10-12 → 16 | laptop fixes; **Billiards Phases 1–9** with editor tools, 10 tables, 105 self-tests (the 3 Billiards card classes were removed on 10-19) |
| 2026-10-17 | Balance hotfix (Batch 0) |
| 2026-10-18 → 19 | Phone/Shop Batches 1–2 (Ari); three-viewpoint `CameraRig`, Highlight Hover, Money Popup, DESIGN v2.1 (Han) |
| 2026-10-20 → 21 | Batch 3 Delivery (door, parcels, letters) and Batch 4 buyback (Ari) |
| 2026-10-22 (`Han/RefactorSystem`) | four mechanic endings (Charlie, Jackpot, Curse, Mother Lode), ticket reward rework + payout table, Reward Popup, cheat menu rebuilt (5 tabs + `CheatFlags`), service tutorials replaced by hints, keyed cost modifiers, license-gated card pool and debuff dismiss fee, Meter timeScale gate, `CardRevealPanel` rework, scene clean-up (camera zones, prefabs for repeated pieces), DESIGN v2.2 |

### 17.2 In flight / waiting on a person

- **Billiards sign-off (Ari):** 20 real rounds (> 1 player), measure seconds per shot, retune to 1.9/3.2/4.1 $/s, re-run the Balance Report, decide `straight_test` and per-table `winRates`, WebGL determinism check (postponed).
- **Phone/Shop plan Batches 1–4: built and merged to `main`.** Still owed: Ari touches `GameConfig.asset` so the 47 unserialized fields serialize (§10); WebGL blur check (Ari); Han's art for the phone UI (final frame, icons, status bar, Shop icon).
- **`Han/RefactorSystem` branch:** uncommitted. Needs a Play-mode pass of every new mechanic (the four endings, Reward Popup, cheat menu tabs, debuff dismiss) and Ari's review of `Meter.cs`, `GameConfig.cs` and `Economy.cs`, then a commit and merge.
- **Batch 5: playtest** (≥ 10 full runs); decide win target and balance numbers marked `[model]` (§17.5).

### 17.3 Planned: Phone, Shop, Delivery, Buyback

Full spec: `PHONE_SHOP_DELIVERY_IMPLEMENTATION.md` (phases, checkpoints, pure-C# appendices A1–A9, new GameConfig fields) and `PHONE_DELIVERY_DECISIONS.md` (decision log). One branch per batch, each from `main` after the previous merge.

| Batch | Branch | Contents |
|---|---|---|
| 0 | `Ari/PawnHotfix` | balance hotfix — **applied** |
| 1 | `Ari/PhoneShell` (**built on `ari/Phone-Revamp`**; layout changed per A3/A6/A8: docked phone, no side-by-side, DeviceLayer inside the main Canvas) | `PauseLock` (counted clock pause), `SpeedModifiers`, `ClockService.GameDelta/GameTime`; device layer (blurred desk, phone + laptop side by side, small device clocks, Meter only on desk, laptop locked during beneficial calls); smartphone: power button (5 s boot, clock running), caller ID calls (Telemarketer 40 / Client 25 / Unknown 25 / ITRS 10), call queue with 12 s gap, banners (max 3, 4 s), Inbox, Taxes app with 30 s deadline; delete `AnnoyanceManager`; Cable Eater becomes a debuff (phone forced off 30 s) |
| 2 | `Ari/Shop` (**built and merged**) | item catalog ScriptableObject (replaces layer-based pawnability), Shop app with cart/checkout, licenses (Blackjack $60, Billiards $60, via Inbox "Activate", not pawnable), catalog prices (Power Bank $25, Card Box $40, Lucky Nine stack $30), pawn = floor(80% × price paid), `IconGate`, **Egg Timer 0.75 s cooldown** |
| 3 | `Ari/Delivery` | **Door object and Door view** (DESIGN §4.1; the plan's "mailbox higher up" scroll is superseded by the three viewpoints), doorbell, unboxing, shipping (Standard free 20 s / Express $8 5 s / "leave it opened & ready" +$3), tax letters, junk mail, tutorial panels (extend the existing `TutorialPanel`/`HintService`) |
| 4 | `ari/buyback` | `IRestorable`, pawn ledger written before `OnPawned`, buyback ×1.05 → ×1.15 of full price over 90 s game time (A21; the Buyback card was dropped, A5) |
| 5 | playtest | ≥ 10 full runs; decide win target / run length (I1 amendment if needed) |

**Phase 3.3 built `TutorialTracker`, `TutorialCatalogSO` + `Tutorials.asset`, `TutorialService`, `TutorialPanel.Present` (all removed 2026-10: the guidance became hints). Batch 3 done.** **Phase 3.2 built: `LetterPile` / `JunkMailTimer` (pure, `Shop/Mail.cs`), `LetterView` pool (prefab `Shop/Letter`), tax letter + junk mail in `DoorController`, `HintId.JunkFlyer`.** **Phase 3.1 built (`ari/delivery`): `DoorController` (scene object `Door`: `DoorZone` collider, `BackZone_Door`, `ParcelTowerBase`), `ParcelView` pool (prefab `Shop/Parcel`), `PoofEffect` (prefab `Shop/Poof`), `DoorDepot` (pure), `ShopService.Deliver/LeaveOpen`, OPEN tile in the cart, `HintId.PackageArrived/ParcelOpen`. Tax and junk letters: Phase 3.2.** **Phase 2.3 built: catalog pawning in `Pawn.cs`, Egg Timer 0.75 s cooldown, redesigned inventory-style Shop window.** **Phase 2.2 built: Shop app on the phone (`ShopScreen`: item list with prices/status, ADD/DROP, CART with Standard/Express and BUY, on-the-way orders with a live countdown), `ShopService.TryBuy/ActivateLicense`, Inbox Activate button, desk delivery, licenses hide Blackjack and Billiards until activated, `IconGate` wired to Laptop icons (`DesktopIconView`), phone icons (`PhoneHome` polls) and the "No More X" / "Close The Shop" cards (`DisableAppIconCardSO`), `LaptopController.RequestOpen` refuses hidden apps.**

**Phase 2.1 built (`ari/shop-system`): item catalog (`Assets/Settings/Configs/ItemCatalog.asset` + 14 `Items/*.asset`: laptop, phone, printer, meter, collection, egg_timer, bear, picture, triple_stack, power_bank $25, card_box $40, lucky_stack $30, license_blackjack $60, license_billiards $60), `ShopService`, `ShopItemBinding` on Laptop, Phone, Egg Timer, Bear, Picture, Power Bank, Triple stack, Nine stack, Card Box. A fresh run hides the Power Bank, Card Box and Lucky Nine stack; nothing can buy them yet (Phase 2.2). Pawning still uses the old `IPawnable` values until Phase 2.3.**

Starting kit (decided): Laptop (+ Mining), wire puzzle, Egg Timer, Bear, Picture, Triple Match stack, Phone, Printer, Meter, Collection. Starting items are unpawnable except the Laptop; the Phone never.

Branch names: Batch 1 used `ari/Phone-Revamp` (made by Ari). Pick the names for Batches 2–4 before starting them.

### 17.3a Designed in `DESIGN.md`, status of each (v2.2)

| Feature | DESIGN § | State in code | Owner |
|---|---|---|---|
| Four mechanic endings: `TicketJackpotWin`/`TicketCurseLoss` (Lucky Nine jackpot/curse tickets), `MotherLodeWin` (Mining streak) and `BlackjackCharlieWin` (7 cards) and their `GameConfig` keys (`luckyNineJackpotChance` 0.6%, `luckyNineCurseChance` 1%, `luckyNineCurseOmenWindows` 6, `curseBeatSeconds` 1.2, `miningStreakTarget` 3, `miningFullVeinStreakChance` 0.2, `blackjackCharlieCards` 7, `blackjackCharlieChance` 4%), ending texts, rating outcome values (0.95 wins / 0 losses). `EndReason` is append-only. **Built:** `BlackjackCharlieWin`, `TicketJackpotWin`, `TicketCurseLoss`, `MotherLodeWin` (`CaveInLoss` and `BlackjackRuinLoss` were cut). The CheatMenu forces each one (§8.4). Moved to "built", kept here so the config keys are easy to find | 5.3, 5.5, 5.14 D, 5.15 | built; `EndReason` has 10 values; `[unverified]` in Play mode | Han |
| Door object, Door view content, doorbell, parcels, unboxing, empty-box junk item, paper bills and junk mail at the door | 4.1, 5.7 | built (Batch 3) | Ari |
| Buyback (`IRestorable`, pawn ledger) | 5.11 | none | built (Batch 4) |
| "Leave it opened & ready" shipping option | 5.7 | built (`ShopService.LeaveOpen`, `shopLeaveOpenFee`) | Ari |
| Highlight Hover as a sprite-edge glow with fade and `hoverGlow*` config | 4.2 | built (§8.3a); the four `hoverGlow*` fields are C# defaults until the asset is touched | Han |
| Tutorials for calls, Inbox, Taxes, shipping, pawn | 5.16 | built as one-line hints (§8.2); the intro panel is the only panel | Ari |
| `billDeadline` as a `GameConfig` key | 5.8 | built as `taxPayWindow` | naming only |

**Naming differences (DESIGN → code), not bugs:** `billDeadline` → `taxPayWindow`; `shopExpressCost` / `shopReadyCost` → `shopExpressFee` / `shopLeaveOpenFee`; `ClockService.CurrentSeconds` → `RemainingTime`; Cable Eater class `CableEaterCardSo`. **Value differences: one open (phone call weights, N16); the Card Box price $40 and pawn $32 are the same in DESIGN and code.

### 17.4 Backlog / ideas (not scheduled)

Coffee Mug, Dish Rack, achievements, settings menu, daily challenge (fixed seed), localization VI/EN, New Game+, roguelite/prestige run structure (exploring only), card-collection reward or win (needs I1 amendment), Lucky Nine jackpot win (needs I1 amendment), Billiards click-and-slider aim, sound clips, real ball/cue art. Cut: bank/loans, porch pirate, courier calls.

### 17.5 Open questions

| Question | Status |
|---|---|
| Win target 600 s vs shorter runs | after playtests (Batch 5) |
| Approve first-pass shop prices for playtesting | open |
| Phone call weights: keep 25/25/40/10 in code or move to DESIGN's 40/25/25/10 (N16) | open, left as is on 2026-10-22 |
| Balance of the new endings (4% Charlie, 1.6% ticket ending, Mother Lode 0.2 streak) is arithmetic only | after playtests |
| `dragThreshold` feel | untuned |
| Ari's sign-off on the DESIGN v2.1 invariant rewrites (I2 Time Scarab, I5, I9) | assumed from the merge, confirm |

**Answered by DESIGN v2.1 (removed from this list on 2026-10-20):** Mining stays drag/hit and the Phone's job is calls, Shop, Inbox and Taxes (L8); Time Scarab freezing the clock while the player can act is allowed (I2); the Laptop is the one pawnable starting item and Buyback is the safety net; the Egg Timer is not pawnable; Phone plan Q1–Q9 (see A1–A11); the Creepy Jester `PlayerPrefs.DeleteAll()` wipe is deliberate.

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
| D7 | 08-31 | I9 amended: vertical camera scroll | **superseded 10-19** by the three fixed viewpoints (I9 in DESIGN v2.1; `CameraRig`) |
| D8 | 08-31 | Cat exempt from `AnnoyanceManager` | final |
| D9 | 08-31 | `WireTask` replaces `DragTask` | final |
| D10 | 08-31 | `CostModifierService` per target | final |
| L2 | 10-17 | Lucky Nine fixed by base reward 26 (not `minMatchToPay`) | applied |
| L3 / P12 | 10-17 | Phone never pawnable; Laptop pawnable ($150 catalog → $120; changed to $135 → $108 in A21) | applied |
| B1–B5 | 10-12 | Billiards = ordered-queue puzzles; strict rules; view-only mirror; deterministic sim; append-only enums | built |
| B6 | 10-13 | Leaving while Aiming keeps the bet; only a rolling shot forfeits | built |
| B7 | 10-14 | Tax counts Billiards profit only (stake refund reason) | built |
| B10 | 10-14 | EV-based payout formula | built; values pending |
| B13 | 10-14/15 | Physics: ball 0.0614, corner 0.12, side 0.09; validator ratios; re-verify after changes | final |
| B21 | 10-16 | Exact touch-instant ball collisions | final |
| B25–B26 | 10-16 | Billiards card classes; pending-effects contract applied to all card queues | built (assets pending) |
| P1–P3 | 10-17 | Calls with caller ID, weights 40/25/25/10; beneficial calls stop the clock; declining is free; one call at a time, 12 s gap | built (Batch 1) |
| P5 | 10-17 | Power button, 5 s boot (tune gig value first, then boot time, then cut) | built (Batch 1) |
| P6 | 10-17 | Cable Eater → debuff (phone forced off 30 s); Do Not Disturb retired; asset name kept | built (Batch 1) |
| P7–P9 | 10-17 | Apps: Shop, Call, Inbox, Taxes; bills in app + mail, pay one cancels other, penalty once after 30 s; only calls interrupt | built (Batch 1) |
| P10 | 10-17 | Device layer: blur, side by side, Meter only on desk | built (Batch 1) |
| P11–P14 | 10-17 | Licenses digital and permanent; pawn 80%; starting items unpawnable except Laptop; buyback 110 → 125% (changed to 105 → 115% of full price, A21); first-pass prices | built (Batches 2 and 4) |
| P15–P16 | 10-17 | Shipping options; door + mailbox via scroll; junk mail | Batch 3, with the Door view (DESIGN §4.1) replacing the scrolled mailbox |
| P18 | 10-17 | Egg Timer 0.75 s cooldown | built (Phase 2.3): `eggTimerCooldown` 0.75 s in `GameConfig` (code default until the asset is touched), only in `EggTimerTask` via `HoldTask.CanStartHold` |
| P19–P21 | 10-17 | Blackjack 100/25/130 + Hot Table; Lucky Nine 26 | applied |
| P22 | 10-17 | Billiards: don't adopt 0.6/1.8/2.3; target 1.9/3.2/4.1 $/s after measuring | pending |
| P24 | 10-17 | Guided tutorial, one panel per new mechanic | **superseded 10-22 by one-line hints (A24)** |
| P29 | 10-17 | Remove `AnnoyanceManager`; `CallScheduler` replaces it | built (Batch 1) |
| P30 | 10-17 | Batch order 0 → 4, one branch each from `main` | final |
| A1 | 10-18 | Phone plan Checkpoint 0 answers (Ari): Q1 remove tax Ignore; Q2 full catalog, no refresh; Q5 tutorials pause the clock; Q6 junk mail 60–120 s, none in first 60 s; Q9 Han draws art, placeholders meanwhile; Q10 Robocalls halve the call interval; Q13 `ClockService` execution order −1; Q14 Egg Timer stays pawnable ($150) until Batch 2 | decided |
| A2 | 10-18 | **Q3:** client call pays **+$22 only**; no coupon option (no sale-tag art). Coupon code in `Checkout` stays tested but unwired, no `GrantCoupon` | decided |
| A3 | 10-18 | **Q4:** device layout replaces "side by side": Laptop open shows the phone docked on its right; tapping the phone tweens the Laptop out to the left and leaves only the phone (home screen, like the reference UI set). Background blur stays while any device is open. A paused call therefore never shows the Laptop | decided; details at Checkpoint 1.2 |
| A4 | 10-18 | **Q7:** unboxing = 3 box clicks with shake and broken-crate SFX (no knife drag) | decided |
| A5 | 10-18 | **Q8:** no buyback card; task 4.2.5 dropped (no new card assets) | decided |
| A6 | 10-18 | **Q11 superseded:** `DeviceLayer` lives **inside the main Canvas** (not a separate canvas), so hints, tutorials and the end screen stay above devices and `CutsceneRoot`/`ClockRoot` order is unchanged | built (Phase 1.2) |
| A9 | 10-18 | Phase 1.3 choices: the desk phone only opens the phone UI (calls are answered/declined there); the incoming-call pill also shows over a Laptop app and opens the phone-only view; the phone can't be closed during a paused call; ITRS "payment plan" waits for Phase 1.4 (it needs `TaxDeadline`); `CallScheduler.IntervalScale` was added to the plan's class for Robocalls (Q10) | decided (the pill-over-app rule overrides A8's "can't reach the phone from an app" only for a ringing call; tell me if you disagree) |
| A11 | 10-18 | Ari's answers on A10: unknown-caller share raised (weights 25/25/40/10 instead of 40/25/25/10); "money to the meter" = $22 to the money counter; a client who never answers says "Hello? Are you there?" and hangs up; the Taxes countdown stays; an ITRS call after the bill was paid adds its +15 s to the gap between bills (45 s becomes 60 s; Ari clarified: gap 45 s, pay window 30 s, the +15 goes into the gap) | built |
| A21 | 10-21 | Batch 4 (Ari): **buyback** is priced from the item's **full price** (price paid, or `catalogValue` for the Laptop), not the pawn payout, with gentler steps **×1.05 / 1.08 / 1.12 / 1.15** at 0 / 30 / 60 / 90 s of game time (plan was 1.10-1.25; Ari lowered it twice, first to 1.12-1.18): **the Laptop's catalog value is now $135** (was $150; Ari, "we will see how the case goes"; `laptop.asset`), so it pays $108 and costs $142 / 146 / 152 / 156 to buy back, Power Bank 27-29, Card Box 42-46, Lucky Nine 32-35; the round trip always loses (self-tested for $1-$500). It **ships like a purchase** (Standard/Express and Leave it opened, fees paid on top; one parcel per buyback) and the item is `OnRestored()` and put back at its start-of-run position (`ShopItemBinding.HomePosition`; otherwise it would reappear on the printer and be pawned again) when the parcel is opened. The Shop app has two tabs, SHOP and PAWNED (live price, "Goes up in Xs", BUY BACK opens a receipt with the shipping tiles); a pawned item reads "PAWNED" in the SHOP tab. No Laptop guard (the player must know the risk) and no buyback tutorial. Guards: Card Box refuses pawn while its reveal panel is open, Lucky Nine stack while a ticket is out. `IRestorable` is on `ClickTask`, `HoldTask` (Egg Timer cooldown and Power Bank charge reset), `DeskLaptopOpener` (closes the panel; licenses and the solved wire puzzle untouched), `CardBox`, `TicketStack` | built (Phases 4.1-4.2) |
| A18 | 10-20 | Phase 3.3: **first-time tutorial panels**. `TutorialService` (on `MANAGERS/Services`) shows a panel once per id (PlayerPrefs `Tutorial_Seen_<id>`), one at a time, **pausing the clock** while it is open (Q5); it reuses the intro `TutorialPanel` (`Present(steps, onClosed)`; the intro keeps its own flag and its hard freeze). Texts live in `Assets/Settings/Configs/Tutorials.asset` (**placeholders, Ari writes the final copy**). Ids and triggers: `phone_first_open` (Phone opened), `first_call` (first ring), `first_paused_call` (first client/ITRS call), `first_tax` (first bill), `shop_first_open` (Shop app opened), `first_license` (first license bought), `first_delivery` (first parcel at the door), `door_first_package` (Door view entered with a parcel; the plan called it `mailbox_first_package`, renamed because the mailbox became the door), `first_pawn` (first pawn). `first_buyback` waits for Batch 4. Cheat menu: "Reset tutorials". The Creepy Jester wipe re-enabling them is accepted (P18) | built (Phase 3.3), **replaced on 10-22 by hints (A24)** |
| A17 | 10-20 | Phase 3.2: **tax letter** lands on the carpet `taxLetterDelay` (10) game seconds after a bill is issued (red stamp; click it to open the Tax paper panel; recalled in a puff when the bill is settled anywhere, and never shown if it is settled first); **junk mail** every 60-120 game seconds, none in the first 60 (blue stamp; click shows a placeholder joke flyer and bins the letter); both ring the doorbell; the pile holds `letterSlots` (8) and a new letter pushes the oldest junk off (a tax letter is never pushed off); letters scatter randomly (`RngService`) around `LetterCenter` on the right of the carpet. The desk clipboard (`TaxNotice`, object "Taxes") was saved inactive at the time; on 10-22 its prefab was deleted (N15). Junk flyer jokes are placeholders for Ari to rewrite (`DoorController.junkLines`) | built (Phase 3.2) |
| A16 | 10-20 | Phase 3.1 (Ari): taxes will reach the player two ways, the phone (Taxes app + Inbox) and a **tax letter delivered at the door** (replaces the desk clipboard / Mail Tray, which is skipped for now; built in Phase 3.2). The door is already in the background art: only a collider (`DoorZone`) was added. **Parcels** stand in a **tower** on the carpet, left of centre; **letters** (tax and junk, Phase 3.2) are scattered on the right. A parcel opens after `parcelClicksToOpen` (3) clicks (shake + sound hooks), the box returns to a pool of `doorParcelSlots` (4) and is recycled; a fifth parcel waits for a free spot. Pawning the empty box comes later with the art. Items appear on the desk in a smoke **poof** (placeholder circle until Ari finds the particle asset) when the player next looks at the Desk view. "Leave it opened & ready" (+$3) skips the door and tower. `DESIGN.md` updated: Card Box $40, config names follow the code (`shopStandardSeconds`, `shopExpressFee`, `shopLeaveOpenFee`, `taxPayWindow`), tax letter at the door | built (Phase 3.1) |
| A15 | 10-19 | Phase 2.3: **catalog pawning** — `Pawn.cs` asks `ShopService.Ownership.MarkPawned` for any object with a `ShopItemBinding` (pays `floor(80% x price paid)`; Laptop $120; Power Bank $20 / Card Box $32 / Lucky Nine $24 when bought at full price; 0 and the "can't pawn" hint for Phone, Egg Timer, Bear, Picture, Triple stack, anything not owned); a pawned item stays "Sold" in the shop until Batch 4. **`WireTask.CanBePawned` is now false** (the wire puzzle is part of the wall; was $15). Cards and junk keep their own `PawnValue`. **Shop UI redesigned** (Ari): an inventory-style window from the Backpack pieces (yellow/orange rows, item card with picture box, cart receipt on a pink pane, STD/EXP shipping tiles) instead of the Inbox layout. **Exploit check:** pawn everything pawnable in a fresh run = Laptop $120 + Backpack $15 -> $175 money -> about 295 s of clock (win needs 600) | built (Phase 2.3) |
| A14 | 10-19 | Phase 2.2 choices: no coupon in the Shop (A2); catalog display names shortened to fit a list row (Lucky Nine, Blackjack, Billiards); a license arrives as an Inbox message with an Activate button (status Ordered until activated, then Owned and the Laptop icon appears); a physical order ships Standard ($0, 20 s) or Express ($8, 5 s) and appears on the desk with a punch-scale and an Inbox "delivered" note (banners are gone); `IconGate` names compare case-insensitively; `DesktopIconView` statics are reset on scene load; the old `DesktopIconView.SetVisible` is now driven only by `ShopService` | built (Phase 2.2) |
| A13 | 10-18 | Phase 2.1 answers (Ari): the Power Bank moves to layer 3 so it can be pawned; `ShopItemBinding` marks only items that are their own desk object (the wire puzzle is part of the wall, so `wire_puzzle` is not in the catalog; Printer, Meter and Collection are catalog rows without a binding); **Card Box shop price $40** (plan said $60: pawn $32, buyback $44/46/48/50; pack stays $35, three unused cards still sell for $30, so no change to `unrevealedCardSellValue`); new `GameConfig` shop fields (fees, times, coupon rate; the 80% pawn rate stays the constant `PawnMath.PawnRate`). A binding means "this object is catalog item X": it hides until owned and is how the Pawn finds the catalog price. Whether an item can be pawned or bought back comes only from its catalog row | built (Phase 2.1) |
| A12 | 10-18 | Phone banners (top-right notifications) removed from the prefab (Ari: the ring and ping sounds are enough); `BannerQueue` stays (logic), the unused `BannerStack` view was deleted on 2026-10-26. Home-icon badges enlarged (26 px) and centred on the icon's top-right corner. App windows, the scroll handle and other transient UI are saved inactive and shown on demand; the list's scroll dot only appears while rows overflow (`PhoneListView`). Ari's window title bars (`Title` > `Icon` + `Title Text`) and the status bar (clock, Wi-Fi, battery) use horizontal layout groups with `LayoutElement` widths (a group that does not control child width collapsed them to 0 at runtime) | built |
| A10 | 10-18 | **Calls tell the player nothing before pickup** (Ari): no timers, no money amounts, no "+15 s" on any button; ACCEPT / DECLINE only. After ACCEPT the call is a scripted conversation with a reply button per line, DECLINE greyed out, the payoff on the last line ("Hang up." button), a hidden 5 s stall hang-up, and the call history afterwards. The Taxes app still shows its 30 s countdown (kept: it is the bill's deadline, not a call estimate) | built; Ari to confirm the Taxes timer, the ITRS/telemarketer scripts and the hidden 8 s client timeout |
| A8 | 10-18 | Docked phone **overlaps** the Laptop's right edge (no Laptop scaling). A Laptop app window (Mining/Blackjack/Billiards) is centred and covers the phone, so the phone cannot be reached from inside a laptop app. Phone apps (later) open as a centred window beside the small phone, like Ari's reference UI (phone left, big app window right) | decided; phone apps in Phase 1.3. **Open for 1.3:** may the incoming-call pill open the phone while a laptop app is open? |
| A7 | 10-18 | **Q12:** Ari runs the WebGL build for the blur test; agents implement blur with tint fallback | decided |
| A19 | 10-20 | **Meter Feed gate (option A, Han):** Feed does nothing while `InputGate.IsBlocked` or `Time.timeScale == 0`, closing the I5 gap left by `CardPanel` not covering the button. Written into §9.2 first; **built 10-22 as a `timeScale == 0` check only** (the `IsBlocked` half was dropped: ordinary panels do not pause time, so Feed stays legal there; see A27) | built (partly) |
| A20 | 10-20 | **PROJECT.md synced to DESIGN v2.1:** I1/I2/I5/I8/I9/I10 take the DESIGN wording (§2). Deliberate design, no longer listed as bugs: `ClockService.Spend` ignores the Final Countdown intercept (DESIGN §5.14 B; was P15); a card's `OnPlaced` never checks `handCap`, only buying does (§5.4; was N7); the Creepy Jester wipes all saved data (§5.14 C; was P18); the Laptop is pawnable until Buyback (§5.11; was L4a); modal panels may freeze `timeScale` (I5). Fixed and moved to §16: L1, L4b, P8, P9. Still bugs at the time: N5 (Audit handler leak, fixed 10-22) and the Meter gap (fixed 10-22) | applied |
| A22 | 10-22 | **Alternate endings, Han.** Ten `EndReason`s: the six old ones plus `TicketJackpotWin`, `TicketCurseLoss`, `MotherLodeWin`, `BlackjackCharlieWin`. **Cut:** `BlackjackRuinLoss` (pawn everything and still cannot pay) and `CaveInLoss` (the mining loss), replaced by nothing. Mechanic wins rate 0.95, the curse 0 (`ratingMechanicWin/Loss`) and play `EndingFlourish`. Lucky Nine: one banded roll at purchase (jackpot 0.6%, curse 1%), the ending fires when the last window is scratched, and cashing out early forfeits it; the curse gives a hint at 6 windows and a 1.2 s shake beat. Mining: streak of 3 full veins, boost 0.2 replaces the 1-in-5, a slip or a short vein or leaving the app breaks it, the hint shows at 2 of 3 and no progress label. Blackjack: 4% of hands are rigged with 2s and 3s toward a 7-card Charlie; the hint `CharlieTease` shows from the 4th card on every hit. Chances are `[model]`. `GameManager.GameOver` ignores a second call | built, `[unverified]` in Play mode |
| A23 | 10-22 | **Ticket economy.** Reward = sum over repeating symbols of `baseReward × (count − 1) × multiplier`, paid only on Cash Out. Symbol sets: Triple Match weights 50/33/17, multipliers 0.25/0.5/1.2; Lucky Nine 30/25/20/15/10 with 0.15/0.25/0.5/0.8/1.2 (RTP 89.0% / 88.4%, P(profit) 46% / 32%, exact enumeration). A payout table beside each panel (`TicketPayoutTable`) is built from the symbol asset. Both ticket UIs read through `ScratchPanelView`. Hints `TicketCashOut` (on opening a panel) and `TicketCursed` | built |
| A24 | 10-22 | **Tutorials become hints.** `TutorialService`, `TutorialTracker`, `TutorialCatalogSO` and `Tutorials.asset` deleted; the nine service tutorials are now `HintService.ShowOnce` calls (`PhoneFirstOpen`, `PhoneFirstRing`, `PausedCall`, `MailTrayFirstBill`, `ShopFirstOpen`, `LicenseBought`, `PackageArrived`, `ParcelOpen`, `FirstPawn`); `TutorialPanel` keeps only the intro. Hints no longer pause the clock (this replaces Q5/A18). `HintId` is append-only; `TicketOpen` and `EggTimerPlaced` were removed because nothing serializes a `HintId`. `HintService.ShowAlways` bypasses the repeat cooldown for teases | built |
| A25 | 10-22 | **Cheat menu rebuilt and gated.** Five tabs built in code (Endings, Games, Economy, Annoyance, Dev) over the existing grid; `CheatFlags` holds the "force the next X" switches; the toggle and the flags work only when `Debug.isDebugBuild`. Reward Popup moved to `GameConfig.rewardPopup*` (replacing `moneyPopup*`), Highlight Hover to `GameConfig.hoverGlow*` | built |
| A26 | 10-22 | **Cards and debuffs.** `CardSO.IsAvailable` keeps licensed-app cards out of packs until the license is owned. `CostModifierService` is keyed per card and multiplies (N4). Pawning a debuff whose effect still runs costs `debuffDismissFee` $6 and pays nothing; if the player cannot pay, the card is refused with a hint and moved elsewhere in the placement area. Card Box price/pawn $40/$32: docs follow the code | built |
| A27 | 10-22 | **Safety fixes.** `Meter` ignores Feed while `timeScale == 0`; Blackjack pays before the reveal delay (N14); `CardItem` cancels its timed `Invoke`; Audit detaches its handler (N5); `InputManager` disposes its actions. Phone call weights stay 25/25/40/10 in code although DESIGN §5.9 says 40/25/25/10 (N16, open). N6 closed: the Cat may block the Phone | built |
| A29 | 10-26 | **Audio, plan B.** `AudioService` + `AudioCatalogSO` + `SfxId` (append-only) with 24 ids; hooks through existing events where they exist, one `AudioService.Play` line elsewhere; `HintService.HintShown` event added; `EndingFlourish` now uses the service. Mixer, BGM, volume settings and the remaining SFX are open (see §8.5). Clips are provisional until the team listens | built, `[unverified]` in Play mode |
| A28 | 10-22 | **Scene clean-up (Han).** Repeated pieces became prefabs (`CameraZone`, `ClickFeedbackPool`, `NinePanel` / `TriplePanel` with their views); services moved under `MANAGERS` (Audio, CameraRig, HoverOutline); `Meter` sits on the Timer object; `CardRevealPanel` was rebuilt (Dim, Plate, PackView) and sits right after Timer in the Canvas so its Dim covers the Meter. Merging the cutscene roots (A4) and prefab variants for the ticket panels (A3 steps 2-4) were left undone | built (partly) |

---

## 19. Tools and testing

| Tool | Where | Use |
|---|---|---|
| Phone UI pixel fonts | `Tools/OverTimer/Fonts/Build Phone UI Fonts` (`Editor/Fonts/BitmapFontBuilder.cs`) | rebuilds `Assets/Design/Fonts/PhoneBig.ttf` + `PhoneBig Pixel.asset` and the `PhoneSmall` pair from `Assets/Art/Phone UI/Font Big.png` / `Font Small.png` (16 px cells, one 16-row band per row of characters, baseline = band bottom). Static raster TMP assets, point filtered, 10 px per em: **use TMP font sizes in multiples of 10** (Big caps are 7 px, Small caps 5 px). Not on the sheets: `$` and `:` (synthesised), `< > & ~ ^` and Vietnamese letters (TMP falls back to another font). Not applied to any UI yet. The sheet license forbids redistribution, so keep the repo private |
| BoldPixels font | `Tools/OverTimer/Fonts/Build BoldPixels Font` | builds `Assets/Design/Fonts/BoldPixels Pixel.asset` (static raster, point filtered, sampled at 16 pt = 1 design pixel per atlas pixel; **use TMP sizes in multiples of 16**, caps are 8 px) from `BoldPixels.ttf`: 415 glyphs incl. `$ % & < > ~`, Latin-1/Extended-A and Vietnamese. Not applied to any UI yet |
| Billiards Table Editor | `Tools/OverTimer/Billiards/Table Editor` | author tables, record shots through the real sim, auto-solve, export (refuses unless solved + verified) |
| Solver | button in the Table Editor | brute force 0.5° × 6 powers, iterative deepening, robustness ranking |
| Verify Presets | `Tools/OverTimer/Billiards/Verify Presets` (also cheat menu in builds) | replay every stored solution + 30 jittered trials per shot; knife-edge < 70% |
| Self-tests | `Tools/OverTimer/Billiards/Run Self-Tests` | 105 EditMode checks (sim, referee, payout, pending effects, config, tables) |
| Balance Report | `Tools/OverTimer/Billiards/Balance Report` | noisy-bot model (~6 min); **overwrites `BILLIARDS_BALANCE.md`** — paste the summary into §13.4 afterwards |
| Round log | `Tools/OverTimer/Billiards/Round Log Summary`, `Reveal Round Log File` | dev-only CSV telemetry |
| Debug menu | `Tools/OverTimer/Billiards/Toggle Debug Overlay`, `Mirror/*` | same switches as the cheat menu |
| Preset formatter | `Tools/OverTimer/Billiards/Reformat preset files` | rewrite JSON compactly |
| Cheat menu | in game, debug builds only (§8.4); F1 | five tabs: endings, games, economy, annoyance, dev; card list; Billiards switches |
| PhoneShop self-tests | `Tools/OverTimer/PhoneShop/Run Self-Tests` (`Assets/Tests/EditMode/SelfTests/PhoneShopSelfTests.cs`; `SelfTestRunnerTests` runs it, and the Billiards self-tests, in the Test Runner) | Phone, Shop, Delivery, Buyback pure logic. The tutorial checks were removed with `TutorialService`. |
| Unity Test Framework | `Assets/Tests/EditMode` (assembly `OverTimer.Tests.EditMode`) and `Assets/Tests/PlayMode` (`OverTimer.Tests.PlayMode`); Window > General > Test Runner, or `unity test --mode EditMode/PlayMode` | EditMode: pure rules (Blackjack incl. the Charlie rig, Billiards, `MoneyService`, keyed `CostModifierService`, `RunRating`/`EndReason`, `GameConfigSanityTests` = invariants of `GameConfig` and the ticket symbol assets; per-case tests of the pure Phone/Shop/Delivery logic named after the test plan `Docs/KeHoachKiemThu_OverTimer.md`: `ClockCoreTests`, `TaxDeadlineTests`, `PhoneLogicTests`, `ShopLogicTests`, `DoorMailTests`). PlayMode: the real `SampleScene` (clock, Meter, cards, pawn and debuff dismiss fee, tickets incl. Jackpot/Curse, Mother Lode, Charlie, hints, phone/ITRS; `ShopTests` = Shop purchase, license, clock-gated delivery, catalog pawn values and buyback; `DoorTaxTests` = parcels, tax letter, junk mail, Restart, tax penalty and its pause; `PhoneCallTests` = Client/Telemarketer/ITRS calls, decline, no-reply and auto hang-up, call history cap, Restart mid-call; `CameraClockTests` = clock pause and stacked speeds, the three camera views and `ViewGate`, keyed Laptop locks and apps freezing under them; `TicketEggTests` = ticket payout formula, Cash Out on revealed cells only, the 2-ticket pool, exact RTP vs a seeded simulation, the Egg Timer cooldown edge; `CardRuleTests` = Final Countdown pawn by phase, stacked pack-price multipliers, two No More Mining cards, Close the Shop, Cable Eater, Robocalls interval, the pack-reveal dim over the Meter; all per test-plan case). The Cat may sit on any desk object a test is using (its `PointerReceiver` goes non-interactable); fixtures that click desk objects repeatedly switch the Cat off. Delivered items appear through `DOVirtual.DelayedCall`, which DOTween times in **real** time, and `PhoneController`'s real-time call timers use `Time.unscaledDeltaTime`; neither is fixed by `captureFramerate`, so a test waits on `Time.realtimeSinceStartup` / `Time.unscaledTime`, not on a frame count. `GameSceneTest.GiveItem(id)` puts a Shop item on the desk (Power Bank, Card Box and Lucky Nine stack start hidden). Tests read balance numbers from the assets and do not pin them; keep it that way. **Run in Player (WebGL):** a PlayMode fixture with `[TestCase]` / parameterized tests fails to load there (`DllNotFoundException: libc`), so PlayMode tests are written without parameters; results go to `%LocalAppData%Low/HanAri/OverTimer/TestResults.xml`. `GameSceneTest.RestoreState` resets `Time.timeScale` and `AudioListener.pause` after every test, because the Player's result sender waits on game time and a last test that ends in `GameOver` (timeScale 0) would leave "Wait For Player Run" hanging |
| Unity MCP | `com.coplaydev.unity-mcp` v10 | agents inspect/edit the scene and run Play mode. Its `run_tests` in PlayMode turns on *Enter Play Mode Options: disable domain reload* for the run and restores it afterwards; if the MCP session drops mid-run, `ProjectSettings/EditorSettings.asset` is left with `m_EnterPlayModeOptions: 1`: revert it, don't commit it |

Testing approach:

- Put logic in pure C# and test it headless: new rules get an EditMode test under `Assets/Tests/EditMode` (the Editor menu self-tests `Tools/OverTimer/<Feature>/Run Self-Tests` still exist for Billiards and PhoneShop). Do not pin config values in a test; assert invariants (`GameConfigSanityTests`) or read the value from the asset. Build test configs with `ScriptableObject.CreateInstance<GameConfig>()` and set every field you rely on (defaults are stale).
- Outside Unity, the pure cores can be compiled with `dotnet` 8 against small Unity stubs (used for the Phone plan's CallScheduler checks and Billiards cross-checks). Physics results differ between Editor and .NET, so verify Billiards only in the Editor.
- Play-mode checks via Unity MCP: simulated pointer events, `Application.runInBackground = true` (an unfocused Editor may pause), throwaway in-memory card assets to test placing.
- Every plan phase ends with a **checkpoint report**: done, files changed (prefab vs scene), tests and results, deviations, problems, assumed defaults, evidence, questions. Then wait for Ari's "approved".
- No real playtest data exists yet. Every balance number here is arithmetic or a bot model.

---

## 20. Verification gaps (check in the Editor)

This file was written from a partial snapshot (no Play mode, ~8 of ~45 prefabs, few card assets, 1 of 10 Billiards tables, `WinConditionCard/` not re-read); the 2026-10-20 sync re-read the camera, hover, popup, pause and Audit code and the Cable Eater, Time Scarab and Clock Up assets; the 2026-10-22 sync re-read every file touched by the `Han/RefactorSystem` work. Whoever checks an item below: record the answer in the right section and delete the row.

| Check | Why it matters |
|---|---|
| **Play-mode pass of everything built on 2026-10-22:** the four endings (use the cheat tab Endings), Charlie rig, Mother Lode streak, Curse beat, Reward Popup positions, license-gated card pool, debuff dismiss (pay / refuse / relocate), CardRevealPanel Dim over the Meter, Restart after each | all new code was compiled and read, never played |
| Does the scene still have more than one cutscene root, and are all cutscene Timelines mapped to their `EndReason`? | A4 / N18 |
| Script execution order still as in §5.4 | Meter/ITRS/RunStats start-money order (N2) |
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

`DESIGN.md` **v2.2 now lives in the repo** (`DOCS/DESIGN.md`) and is the design source of truth. Other design and plan docs kept in the claude.ai project "Overtimer" (not in the repo): `OVERTIME_EXTENDED_HANDOFF.md` (D1–D10, Vietnamese), `OVERTIME_LANDMINE_PASS.md` (L1–L12 and fix briefs), `OVERTIME_UML.md`, `BILLIARDS_IMPLEMENTATION.md` (superseded by the code + §13.4), `PHONE_DELIVERY_DECISIONS.md`, `PHONE_SHOP_DELIVERY_IMPLEMENTATION.md` (**active plan**), `OVERTIME_UPDATE_SUMMARY.md` (EN/VI summary for Han).

Known stale spots in the plan docs (fix when the batch starts): they say `GameManager.inputBlockers` (now `InputBlocker` + `InputGate`), propose a new `TutorialPanel` and `TutorialService` (the service was built then deleted; `TutorialPanel` is the intro only), list Blackjack 40/20/50 as "today" (hotfix applied), and `PHONE_DELIVERY_DECISIONS` §2.4 still shows the 1.0 s Egg Timer proposal (locked value is 0.75 s). The Phone plan's P11 "replace `Destroy()`" is already done.

### Changelog of this file

| Date | Change |
|---|---|
| 2026-10-30 | §19: PlayMode `CardRuleTests` (9 tests, cases TC-CRD-14/15/17/19–22), all pass. No game code changed. |
| 2026-10-29 | §19: PlayMode `TicketEggTests` (7 tests, cases TC-TKT-11–14/17, TC-TSK-7), all pass; note on the Cat blocking test targets. No game code changed. |
| 2026-10-29 | §19: PlayMode `CameraClockTests` (9 tests, cases TC-CLC-7/8, TC-CAM-1–4/7/8), all pass. §8 (views): the `ViewGate` list verified from the prefabs/scene and in Play mode; §20 row removed. No game code changed. |
| 2026-10-29 | §19: PlayMode `PhoneCallTests` (9 tests, cases TC-PHN-10–16, TC-PHN-20), all pass; real-time note extended to the Phone call timers. No game code changed. |
| 2026-10-29 | §19: PlayMode `DoorTaxTests` (9 tests, cases TC-DOR-5–9, TC-TAX-4–6), all pass; note on DOTween real-time delays under `captureFramerate`. No game code changed. |
| 2026-10-29 | §19: PlayMode `ShopTests` (14 tests, cases TC-SHP-1/7–10/12/13/15, TC-BUY-4–7); 13 pass, `TC_SHP_12b` fails on new known issue N19 (Phone pawn shows no hint); §11 Printer rule corrected; §19 Unity MCP note on the Enter Play Mode Options side effect. No game code changed. |
| 2026-10-29 | §19: EditMode per-case tests for the pure Phone/Shop/Delivery logic (36 tests, `ClockCoreTests`, `TaxDeadlineTests`, `PhoneLogicTests`, `ShopLogicTests`, `DoorMailTests`), traced to `Docs/KeHoachKiemThu_OverTimer.md`. All 121 EditMode tests pass in the Editor. No game code changed. |
| 2026-10-26 | Orphans removed: scripts `TaxNotice`, `BilliardsCheats`, `BannerStack`; the " 1" duplicates of the volume profile, URP global settings and input actions; fonts `M 8pt` and `quaver` (ttf + SDF); seven unused Billiard Kit sprites, four unused Phone UI sheets, `TaxBill.png`, `Placeholder_Square.png`. §6.1, §8.4, §15 N15 → §16. |
| 2026-10-26 | Editor tools: paths fixed after the folder reorganisation (GameConfig, ItemCatalog and the font sheets/output are found by type or by their new folders); `BilliardsSelfTests` and `PhoneShopSelfTests` moved to `Assets/Tests/EditMode/SelfTests/` with Test Runner wrappers (`SelfTestRunnerTests`). §6.1, §19. |
| 2026-10-26 | Audio plan B: §5.5, §6.1, §6.2, §8.5, §18 A29. New folder `Scripts/Audio/`, asset `Assets/Audio/AudioCatalog.asset`, `AudioService` on `MANAGERS/Services` (scene edit), tests `AudioCatalogTests` / `AudioTests`. |
| 2026-10-22 | Synced to `DESIGN.md` v2.2 and branch `Han/RefactorSystem` (on `main` @ `ac381ad`): header, §2 (I1, I5, I7, I9, I10), §3, §6 (folders and singletons), §7.1/§7.2 (ten endings), §8.2-§8.4 (hints, camera, hover and popup, cheat menu), §9 (Meter gate, tax letter), §10 (47 unserialized fields, new config rows), §11 (tickets, pawning debuffs, call weights), §12 (`IsAvailable`, keyed modifiers, Audit), §13.2-§13.4 (Mother Lode, Charlie, pay-before-reveal, licenses), §14, §15 (N5 and five more fixed; N15-N18 added), §16, §17, §18 A22-A28, §19, §20. |
| 2026-10-20 | Synced to `DESIGN.md` v2.1 and `main` @ `4d940c0`: header, §2 (invariants reworded, proposed amendments adopted), §5, §6, §7.3, §8.1/§8.3/§8.3a (CameraRig, hover, popup), §9.2 (Meter Feed gate rule), §10, §11, §12 (Cable Eater, Billiards cards removed), §14, §15 (by-design items removed, I5/Meter and Jester guard reworded), §16, §17 (Batch 2 merged, new §17.3a), §18 A19–A20, §20; duplicate A13–A15 rows and changelog rows removed. |
| 2026-10-17 | Created from a six-agent static audit of `ari/pool-table` @ `e724c68` plus all project docs. |
| 2026-10-20 | §5, §18 A18: Phase 3.3 tutorial service, catalog, triggers, cheat reset. |
| 2026-10-20 | §10, §18 A17: Phase 3.2 tax letter, junk mail, letters pile, desk clipboard skipped. |
| 2026-10-20 | §5, §10, §18 A16: Phase 3.1 door collider, parcel tower, unboxing, poof, leave-open, hint ids. |
| 2026-10-19 | §10, §18 A15 (P18): Phase 2.3 catalog pawning, Egg Timer cooldown, wire puzzle unpawnable, redesigned Shop UI. |
| 2026-10-19 | §11 Shop app, §12, §15 P9, §18 A14: Phase 2.2 Shop app, checkout, licenses, desk delivery, `IconGate` wiring. |
| 2026-10-18 | §5 map, §10, §17, §18 A13: Phase 2.1 item catalog, `ShopService`, bindings, Card Box $40. |
| 2026-10-18 | §11, §18 A12: banners removed from the prefab, bigger corner badges, ITRS extra time goes to the bill gap. |
| 2026-10-18 | §6, §9, §10, §11, §18 A11: Inbox and call history as scrolling list + reading pane, caller weights 25/25/40/10, client goodbye line, ITRS time carried to the next bill, `CallRecord` history, `callGoodbyeSeconds`. |
| 2026-10-18 | §10, §11, §18 A10: scripted call conversations, no pre-pickup info, `callHangUpDelay`. |
| 2026-10-18 | §11: phone UI restyle (BoldPixels in `PhoneSlot`, one window frame per app, `CallScreen` layout groups, `CallerProfileCatalog`). |
| 2026-10-18 | §19: Phone UI pixel font builder and the `PhoneBig` / `PhoneSmall` fonts (not yet used by any UI). |
| 2026-10-18 | Phone plan Phase 1.5: Cable Eater verified (mid-call, expiry, early pawn), restart verified; roadmap §17 updated. Batch 1 done. |
| 2026-10-18 | Phone plan Phase 1.4: `TaxDeadline`, ITRS rules, Taxes/Inbox apps, banners, paper-copy `Tax`/`TaxNotice` in §6, §9, §10, §11, §15/§16. |
| 2026-10-18 | Phone plan Phase 1.3: `PhoneController`, call/power/Inbox classes and phone views in §6, §10, §11, §12, §15/§16, I8 in §2; `AnnoyanceManager` removed. |
| 2026-10-18 | Phone plan Phase 1.2: `DeviceLayer` prefab + scene reparent in §6, §10, §13, decisions A6/A8 in §18. |
| 2026-10-18 | Phone plan Phase 1.1: clock core (`PauseLock`, `SpeedModifiers`, `GameDelta`) in §6, §7, §9, §12, §15/§16; Checkpoint 0 answers A1–A7 in §18. |
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
| Annoyance | An interruption gated by the 12 s rule: calls only (`CallScheduler`); bills, mail and the doorbell don't interrupt (I8) |
| Hand / Placed / Flip | cards on the desk (cap 5) / put on the desk as a `CardItem` / effect ended: face-down, still in a slot until pawned |
| Pack | 3 cards from the Card Box, revealed independently |
| Collection | registry of card types ever completed (saved by asset name) |
| Rarity | C / R / SR / UR (60/25/12/3) |
| Vein / Bank / Drop | Mining run of 6–10 blocks / paid on completion / unbanked ore lost on close |
| Hot Table | Blackjack streak bonus +10% per consecutive win, cap +50% |
| Pawn / Printer | sell an item by dragging the Printer onto it |
| Buyback | re-buy a pawned item from the Shop's PAWNED tab at 105–115% of its full price (A21) |
| Starting kit / Catalog / License | shop terms: items present every run / `ItemCatalog` asset with ids, prices, pawnability / permanent digital unlock of Blackjack or Billiards (built, Batch 2) |
| Room / Desk / Door view | the three fixed camera viewpoints (Room 14, Desk 6, Door 7 ortho size; I9, §8.3) |
| Wire Task | plug-to-socket puzzle that powers the Laptop |
| Billiards: table, queue, par, racked, foul, re-spot, called pocket, blocker, forbidden ball, mirror, quantized shot, verifier, knife-edge, stake refund | §13.4 |
| Pending effect / Grant | a queued card effect owned by its card; withdrawn when the card leaves; cleared on scene load |
| InputGate / InputBlocker | counted global input lock / component that holds it while enabled |
| PauseLock / SpeedModifiers / GameDelta | counted clock pause / keyed clock speed contributors / clock-aware delta time (built, Phase 1.1) |
| Batch / Phase / Checkpoint | plan units: one branch / a task group / stop-and-report gate |
| Landmine (L#) | item from the 2026-08-31 defect pass |
| MASCH | the Set Completion win |
| Mechanic ending | a win or loss caused by a mini-game, not by the clock or a card: Lucky Nine jackpot / curse, Mother Lode, Blackjack Charlie (§7.2) |
| Mother Lode | three maximum-length Mining veins banked in a row |
| Charlie | a Blackjack hand of 7 cards without busting |
| Dismiss fee | `debuffDismissFee` ($6): the price of pawning a debuff whose effect still runs |
| `CheatFlags` | dev-only static switches that force the next ticket / hand / vein; inactive in release builds |
| License-gated card | a card with `IsAvailable == false` until the matching app license is owned; not rolled from packs |
