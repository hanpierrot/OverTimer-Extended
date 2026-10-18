# OVERTIME — Design & Implementation Spec

**Origin:** GMTK Game Jam — theme: **Countdown** · extended into a 4–5 person course project ("Extended")
**Team:** Ari · Han (+ Extended team)
**Engine:** Unity (2D, URP) · mouse only · single scene
**Platform:** WebGL, published on itch.io (custom template `Custom1080`). Windows `.exe` and mobile builds are possible targets; nothing in the design depends on them.
**Version:** 2.0 (2026-10-15)

---

## 0. How to use this document

This doc is context for both humans and AI agents. If you are an agent reading this:

- **§2 (Invariants) is law.** Do not violate those rules to make a task easier. If a request conflicts with an invariant, say so instead of silently working around it.
- **§5 (Economy) is the single source of truth for tuning.** Every economy value lives in a data asset (`GameConfig`, `BlockConfig`, `TicketSymbolConfig`, a `CardSO` asset, or a Billiards preset file), never in script logic. The *Where* column of each table says which asset holds a number. If you need a number that isn't in §5, add it to the right asset and add it to §5 — don't invent a literal.
- **§8 (Glossary)** defines project terminology. Use these exact names for classes, files, and variables so both humans and agents stay consistent.
- Anything marked **[LOCKED]** must not be redesigned.
- **Ask who you are working with before assigning, prioritising or estimating work.** File ownership is in §6.2.

---

## 1. Pitch

You are at a desk with **two minutes to live**. A clock counts down. Get it to **ten minutes** and you win.

Everything on the desk earns money. Money buys time. Between you and the clock: a tax agency, a ringing phone, a cat, a stack of lottery tickets that mostly pay nothing, a pool table that pays well if you can clear it, and a box of cards that can win the game for you — or end it.

**Genre:** single-screen resource/risk puzzle · **Run length:** 3–5 min · **Session:** instant restart

---

## 2. Invariants [LOCKED]

These are the load-bearing rules. Breaking any one of them breaks the game.

| # | Rule | Why |
|---|---|---|
| I1 | Clock starts at **120s**. The run is **won** at **≥600s** or by an alternate ending (§5.14). The run is **lost** at **≤0s** (unless the Final Countdown intercepts it once) or by an alternate ending (§5.14) | Core loop. The Clock path is the default; the alternate endings are additional and rare |
| I2 | The Clock counts down in **real time**. Only a ringing call or a beneficial call (§5.9) and timed card effects (§5.4, including the Time Scarab freeze) may change its tick rate, through `ClockService.TickMultiplier`. The player can never pause it. Time Scarab is a buff that slows or stops the Clock only; the game keeps running and the player can still act | The countdown must never feel safe |
| I3 | **Only the Meter converts money → time.** One conversion point. (The Egg Timer is a direct time source, not a conversion; Blackjack converts time → money) | Keeps the economy legible and tunable |
| I4 | **Nothing random may grant clock time directly** | Random gifts on the win condition kill tension and make balance impossible |
| I5 | **The player never acts while game time is frozen.** Scratching, wiring, Billiards and ordinary panels never pause the Clock. A beneficial call stops the Clock only while the Laptop is locked and desk input is blocked. Game time freezes (`Time.timeScale = 0`) only for modal information panels (the intro tutorial and the card inspector), for scripted cutscenes (the Final Countdown intro and the end-of-run sequences) and after the run has ended | Trivially exploitable — pause, mine safely, unpause |
| I6 | Exchange rate is **$1 = 1 second** (`exchangeRate`) | Every number in this doc assumes it |
| I7 | Debuffs **close a door**, never drain the clock directly. Every debuff ends by time or can be escaped by Pawning the card (the one exception is a triggered Final Countdown, §5.14) | Player must be able to route around punishment |
| I8 | **Only calls interrupt, one at a time**, with at least 12 s between calls (§5.9). Bills, mail and the doorbell are non-blocking notifications. The Cat is exempt | Overlapping nags read as bugs, not pressure |
| I9 | No physics simulation of the world and no character. The camera scrolls **vertically only** inside fixed bounds (`CameraVerticalScroller`, two hold-buttons); no drag-pan, no zoom, no rotation. `Physics2D` is used only for hit-testing and trigger overlaps (clicking, Pawn, Cat). Billiards runs its own deterministic vector-math simulation inside its app (§5.6) | Scope |
| I10 | All tuning values live in data assets, never in script logic | The last hours of a project are for tuning, not refactoring |

> **I1 detail — alternate endings.** The Clock stays the default route. Two kinds of alternate ending exist. Three **cards** end the run deterministically from placement state: **Set completion** (win), **Final Countdown** (win or loss), **Creepy Jester** (loss). Three **mechanics** end it from play: a **Lucky Nine** ticket of nine identical symbols, a **Mining** streak or cave-in, and a **Blackjack** seven-card hand or ruin streak. None of them converts money to time (I3) or grants clock time (I4). Each alternate ending is meant to occur in roughly 1–3% of runs, and every loss has a warning and a way out. Full spec: §5.14.

> **I9 detail — the Room.** The play space is a scrollable **room**: the desk is the main frame and further objects sit above/below it, reached by holding the scroll buttons. It is one scene with no loading. Anything that assumes a single unmoving frame (visibility checks, fixed layouts) must account for the scroll range.

---

## 3. Core loop

```
     ┌─────────────────────────────────────────┐
     │  CLOCK ticks down (1s per real second)  │
     └────────────────┬────────────────────────┘
                      │
        ┌─────────────▼─────────────┐
        │  Work the desk → MONEY    │
        │  Laptop · Tickets · Cards │
        │  Blackjack · Billiards    │
        │  Tasks                    │
        └─────────────┬─────────────┘
                      │
        ┌─────────────▼─────────────┐
        │  Feed the METER           │
        │  $1 → +1 second           │
        └─────────────┬─────────────┘
                      │
        ┌─────────────▼─────────────┐
        │  ITRS taxes your earnings │
        │  Phone / Cat interrupt    │
        └───────────────────────────┘
```

**The throughput target.** A 4-minute run burns 240s of clock and must add 480s. So the player needs ~**$720 in ~240 seconds ≈ $3/second sustained**.

**$3/sec is the baseline every task is priced against.** Below it = trap or comfort option. Above it = carries a cost (lockout, variance, or clock).

---

## 4. Desk objects

The play space is the scrollable room (I9). Everything is a clickable hotspot on one scene.

| Object | Role |
|---|---|
| **Alarm Clock** | The countdown. Diegetic — no HUD bar |
| **Meter** | Money → time. The only converter (§5.1) |
| **Laptop** | Hosts apps: **Mining** (§5.2), **Blackjack** (§5.5), **Billiards** (§5.6) |
| **Ticket Stack** ×2 | Two scratch-card tiers: Triple Match, Lucky Nine (§5.3) |
| **Card Box** | Card packs: buffs, debuffs, collectibles, win/lose cards (§5.4) |
| **Card Collection** | Book showing every card type collected, across runs (§5.4) |
| **Phone** | Smartphone: calls, Shop, Inbox, Taxes (§5.7–§5.9) |
| **Mail Tray ("Taxes")** | Paper copy of ITRS tax bills (§5.8) |
| **Door and Mailbox** | Parcels, tax letters and junk mail arrive here (§5.7) |
| **Egg Timer** | Direct clock source (§5.10) |
| **Printer (Pawn)** | Sell your own objects; Buyback through the Phone (§5.11) |
| **Cat** | Roaming chaos (§5.12) |
| **Click / Picture Click / Power Bank / Wire tasks** | Small money tasks (§5.13) |
| **Junk items** | Cheap things to Pawn (§5.11) |

The starting kit (§5.7) is part of the room from the start of every run. Every other object above is bought in the Shop (§5.7, §5.16).

---

## 5. Economy [SOURCE OF TRUTH]

> *Where* names the asset or component that holds a value. Numbers are tuning values and are expected to be adjusted.

### 5.1 Global

| Key | Value | Where |
|---|---|---|
| `startClock` | 120 s | `GameConfig` |
| `winClock` | 600 s | `GameConfig` |
| `loseClock` | 0 s | `GameConfig` |
| `startMoney` | $40 | `GameConfig` |
| `exchangeRate` | $1 = 1 s | `GameConfig` |
| `meterMinFeed` | $10 | `GameConfig` |
| Target run length | 210–300 s | design target |

**The Meter feeds your entire balance at once:** one click spends all current money and adds that many seconds (`current × exchangeRate`). It does nothing below `meterMinFeed`, and nothing while locked (§5.14, Final Countdown).

### 5.2 Laptop — Mining

Drag the pickaxe icon into the ore block and back out: that in-and-out cycle is one swing. Releasing the mouse while the pickaxe is still inside the block also counts as a swing, and the pickaxe always snaps back to its resting spot. Swings are rate-limited by `hitCooldown`.

A **vein** is a fixed walk through the ordered block list (Dirt → Wood → Stone → Coal → Copper → Iron → Quartz → Gold → Diamond → Ruby), of random length. Each block must be broken (HP = number of swings) before the next appears. Each block adds an **unconfirmed** payout by its position in the vein; **finishing the vein banks it**. **Closing the Mining app, or switching to another app, forfeits all unconfirmed ore.**

| Key | Value | Where |
|---|---|---|
| `veinLengthMin` / `veinLengthMax` | 6 / 10 blocks | `GameConfig` |
| `blockPayout[n]` | $1, 2, 4, 5, 7, 10, 12, 15, 17, 21 (by position) | `GameConfig` |
| `veinRespawn` | 1.0 s | `GameConfig` |
| Block HP (Dirt…Ruby) | 1, 2, 2, 3, 3, 4, 4, 5, 6, 7 | `BlockConfig` |
| `pickaxeDamage` | 1 per swing | `BlockConfig` |
| `hitCooldown` | 0.7 s | `PickaxeView` component |
| Vein value | $29 (length 6) … $94 (length 10) | derived |
| Throughput | **≈ $2.8–3.6/s** at maximum swing rate | derived |

- Escalating payout is the drama: quitting at block 9 costs far more than quitting at block 2.
- **Progress is visible:** a status node per block of the current vein, plus *Unconfirmed* and *Received* totals.
- **No clock penalty on closing the app.** Losing the ore is enough (I7).
- Mining is the highest-risk number in the economy: if it dominates, lower `blockPayout` before touching anything else.
- **Alternate endings:** completing `miningStreakTarget` (3) veins of the maximum length (10 blocks) in a row without closing or leaving the app wins the run; a maximum-length vein can collapse and lose it (§5.14 D).
- **Implementation note:** `MiningApp` has no usable static instance and its GameObject only `Awake()`s the first time the Laptop opens it. Anything outside the Laptop that must affect Mining (a card) uses static pending-state fields applied when the next vein banks: `MiningApp.QueueNextVeinMultiplier(float)` and `MiningApp.QueueNextVeinSlip()`, plus the static event `MiningApp.OnNextVeinEffectResolved` (§7 hazards).

### 5.3 Ticket Stack — Scratch cards

Money → money, high variance, no lockout. Two tiers, each its own desk stack and its own pooled `ScratchTicket` instances (one tier per instance).

| Tier | Windows | Cost | Base reward |
|---|---|---|---|
| Triple Match | 3 | `tripleMatchCost` $25 | `tripleMatchBaseReward` $55 |
| Lucky Nine | 9 | `luckyNineCost` $55 | `luckyNineBaseReward` $26 |

**Flow.** Clicking a `TicketStack` charges the cost, rolls every window's symbol **at purchase**, and places a pooled ticket on the desk. Clicking the placed ticket opens its UI panel (generic `DeskObjectFocus`: click a desk object → open its assigned panel; no zoom, no pause). Input is blocked while the panel is open. **The Clock keeps running while the player scratches** — buying Lucky Nine at 0:20 remaining must still be a real bet.

**Reward formula — the same for every tier:**

```
reward = baseReward × (maxMatchCount − 1) × symbol.payoutMultiplier
```

`maxMatchCount` is the highest number of times any one symbol repeats among the **revealed** windows. `payoutMultiplier` lives on the matched symbol. Weights and multipliers are deliberately **inverse** (rarer symbol → higher multiplier). On a tie for `maxMatchCount`, the symbol with the higher multiplier wins.

| Symbol pool (`TicketSymbolConfig`) | Weights | Multipliers |
|---|---|---|
| Triple Match (3 symbols) | 75 / 20 / 5 | 0.25 / 0.6 / 2 |
| Lucky Nine (5 symbols) | 40 / 27 / 18 / 10 / 5 | 0.3 / 0.6 / 1.2 / 2.5 / 6 |

- **EV with every window revealed:** Triple Match $22.23 per $25 ticket (RTP 88.9%, 53% chance of a profit); Lucky Nine $47.76 per $55 ticket (RTP 86.8%, 26% chance of a profit). **Payout range:** Triple Match $0–$220 (three of the rarest); Lucky Nine $16–$1,248 (nine windows over five symbols can never pay $0; the maximum is nine of the rarest: 26 × 8 × 6).
- **Symbols are rolled once, at purchase.** Scratching is a pure reveal animation and never changes the outcome.
- **Reveal:** each window reveals itself when ≥ 60% of it is scratched (`revealThreshold`, `ScratchWindow` component). The ticket **resolves automatically** when every window is revealed. The live result label shows the reward from the windows revealed so far.
- **Cash Out** is always allowed: it resolves the ticket on the windows revealed so far (unrevealed windows pay nothing), reveals the rest, pays, then closes the panel and returns the ticket to its pool.
- **Scratch mechanic:** per-window CPU `Texture2D` alpha mask (circular windows, corner pixels excluded from progress, optional foil art texture).
- **Insider Tip** (card, §5.4) pre-reveals one random window of the next purchased ticket.
- **Lucky Nine special tickets.** At purchase, after the symbols are rolled, a second roll can turn a Lucky Nine ticket into a **jackpot** (`luckyNineJackpotChance`: all nine windows become the symbol with the highest `payoutMultiplier`) or a **curse** (`luckyNineCurseChance`: all nine become the symbol with the lowest). Natural odds are ≈ 1 in 3,800 for nine of the commonest symbol and effectively zero for nine of the rarest, so the chances are set explicitly. The ending fires only when **all nine windows are revealed by the player's own scratching** (the automatic resolve), never from Cash Out — a cursed ticket can be abandoned once the omen is visible. A jackpot or a curse is the only way a ticket ends the run (§5.14 D). Triple Match has no special tickets.
- The Lucky Nine stack can be Pawned for $24; the Triple Match stack cannot be Pawned (§5.11).

### 5.4 Card Box — cards

> Cards pay **rules, never money and never clock time directly** (I3, I4). The only cards that end the run are the win/lose cards in §5.14.

**Data model.** `CardSO` (ScriptableObject) is the base: `cardName`, `image`, `description`, `rarity` (`C`/`R`/`SR`/`UR`), `weight`, `pawnValue`, `hideUntilCollected`, `collectionRegisterAs`. A plain `CardSO` with no effect is a valid collectible. `EffectCardSO : CardSO` is abstract and adds `isDebuff`, `BlocksPawning`, `OnPlaced(CardItem)`, `OnRemoved(CardItem)` and `OnPawned(CardItem)`. `CardItem` is the one shared prefab that gets `Setup(CardSO)` — **pooled, fixed 7 instances, never instantiated or destroyed.**

**Rolling a pack.** A pack is 3 cards (`packSize` on `CardBox`). Each card rolls rarity first, then a specific card inside that rarity by `CardSO.weight`. Both rolls are weighted and independent.

| Key | Value | Where |
|---|---|---|
| `handCap` | **5** — counts every card currently Placed as a live `CardItem`, including ones already flipped face-down | `GameConfig` |
| `packCost` | $35 | `GameConfig` |
| Rarity weights `commonWeight` / `rareWeight` / `superRareWeight` / `ultrRareWeight` | 60 / 25 / 12 / 3 (rarities `C` / `R` / `SR` / `UR`) | `GameConfig` |
| `unrevealedCardSellValue` | $10 flat | `GameConfig` |
| `cardBoxPawnValue` | $48 | `GameConfig` |

**Pack reveal flow.** Buying a pack opens a panel showing a single pack image; clicking it shows a grid of 3 face-down cards that resolve **independently, in any order**. Per card:
- **Sell** is available the whole time: `unrevealedCardSellValue` while face-down, the card's `pawnValue` once revealed.
- Clicking the card art reveals it in place. Once revealed, **Place** always appears and is never gated by `handCap`; it is refused with a hint only when all 7 pooled `CardItem`s are in use. **Add to Collection** appears unless the card is a debuff or its Collection entry is already owned. **Sell** and **Add to Collection** are hidden for debuffs: a debuff can only be Placed.
- Clicking an already-revealed card opens the shared `CardPanel` (name, rarity, pawn value, description) — the same panel used to inspect any Placed card. The panel freezes game time while open (I5).
- The panel closes when all 3 cards are resolved.

**Card lifecycle.** A card does **not** disappear when its effect ends. `EffectCardSO.OnPlaced()` schedules its own end via `CardItem.ScheduleAutoRemove(duration)` or waits for a resolved-event; when it ends (`EndEffect()`), the card **flips face-down and stays on the desk, still counting against `handCap`, until the player Pawns it.** The first time a card type is spent to completion this way it is registered in the Collection. A card Pawned early does **not** earn Collection credit.

- **Placing never checks `handCap`; the cap gates buying.** A pack bought with 4 cards Placed can bring the hand to 7, and debuffs are forced on the player even with a full hand. A hand kept at 5 cannot buy a pack, so it dodges new debuffs.
- **`CardBox` refuses new purchases while the hand is full (≥ `handCap`).** The pool is exactly 7 `CardItem`s: 4 (last allowed purchase point) + 3 (pack size, worst case all forced debuffs). There is no path to instantiate a fresh `CardItem`, since each pooled instance holds scene-only UI references.
- **I7 compliance:** `EffectCardSO.OnRemoved()` fires exactly once however a card's active period ends — natural expiry or an early Pawn — behind one `_hasEnded` guard on `CardItem`. This is what makes Pawning a debuff early a real escape hatch.
- **Speed effects don't stack.** Every card or Phone ring that changes tick rate *writes* `ClockService.TickMultiplier` (the Phone sets 1.5 while ringing). When any of them ends it sets the multiplier back to 1. The last writer wins.
- **Card backs hint at rarity** so holding is an informed bet.

**Collection.** The Collection is a registry of card types ever obtained, saved across runs (§5.15). A card enters it by: ending its effect naturally; the *Add to Collection* button in a pack reveal; or being part of a completed win (§5.14). A card with `hideUntilCollected` is hidden in the Collection book until collected; a card with `collectionRegisterAs` is credited as the other card. The Collection book (`CardCollectionPanel`) shows the cards in alphabetical pages, uncollected ones as locked.

**Card roster.** The Billiards effect cards (Chalk Up, Trick Shot, Cue Slip) are specified in §5.6.

| Card | Class | Rarity · weight · pawn | Effect |
|---|---|---|---|
| Fvck The Tax | `TaxExemptionCardSO` | SR · 18 · $28 | Next ITRS bill becomes $0 (ends when that bill resolves) |
| Lucky Ruby | `OvertimeCardSO` | C · 11 · $4 | Next vein pays ×2 |
| Ticket Hunter | `InsiderTipCardSO` | C · 10 · $4 | One window of the next ticket pre-revealed |
| Snail Speed | `ClockSpeedCardSO` | C · 11 · $2 | Tick ×0.75 for 15 s |
| Time Scarab | `ClockSpeedCardSO` | SR · 18 · $14 | Tick ×0 (clock frozen) for 20 s |
| Cable Eater | `CableEaterCardSO` | SR · 20 · $6 · debuff | Phone and Laptop both turned off for 30 s (calls stop; the Laptop and its apps are locked) |
| Tax Increase | `AuditCardSO` | SR · 10 · $28 · debuff | A bill lands immediately; if one is pending its amount doubles |
| Butter Finger | `ButterfingersCardSO` | C · 7 · $4 · debuff | Next vein pays $0 |
| Spam Robocall | `RobocallsCardSO` | R · 10 · $6 · debuff | Phone rings 2× as often for 30 s |
| Clock Up! | `ClockSpeedCardSO` | R · 10 · $6 · debuff | Tick ×1.5 for 20 s |
| No More Mining | `DisableAppIconCardSO` | R · 8 · $6 · debuff | Mining icon hidden 30 s |
| No More Gambling | `DisableAppIconCardSO` | C · 8 · $2 · debuff | Blackjack icon hidden 30 s |
| Not Valid 8-Ball | `DisableAppIconCardSO` | C · 8 · $2 · debuff | Billiards icon hidden 30 s |
| Close The Shop | `DisableAppIconCardSO` | R · 12 · $6 · debuff | Shop icon hidden 30 s |
| Ticket Up Sale | `TaskCostModifierCardSO` | SR · 10 · $6 · debuff | Ticket prices ×1.5 for 20 s |
| Rarest Card | `TaskCostModifierCardSO` | R · 10 · $6 · debuff | Card pack price ×1.5 for 20 s |
| Bet More Money | `TaskCostModifierCardSO` | C · 9 · $3 · debuff | Blackjack ante ×1.5 for 20 s |
| High Risk, High Reward | `TaskCostModifierCardSO` | C · 12 · $0 · debuff | Billiards charge ×1.5 for 20 s (the payout stays the same) |
| Mei – The Tester | `SetCompletionCardSO` | R · 8 · $5 | Set-completion anchor (§5.14) |
| Final Countdown | `FinalCountdownCardSO` | SR · 16 · $5 | Intercepts the first loss (§5.14) |
| Han – The Pierrot | `CreepyJesterCardSO` | C · 4 · $2 | Slowly turns into a loss (§5.14) |
| Ari – The Gambler | `CardSO` | C · 8 · $3 | Collectible / set piece |
| Han – The Jester | `CardSO` | C · 8 · $2 | Collectible / set piece |
| Sybil – The Caster | `CardSO` | C · 8 · $2 | Collectible / set piece |
| CtrlZ – The Boy Skull | `CardSO` | C · 8 · $2 | Collectible / set piece |
| Kamen Soldier DK | `CardSO` | SR · 19 · $35 | Collectible |
| The Forbidden Steve | `CardSO` | SR · 19 · $35 | Collectible |
| Blue-Eyes Fool Dragon | `CardSO` | UR · 50 · $75 | Collectible |
| Funky Balloon Magician | `CardSO` | UR · 50 · $75 | Collectible |

**Implementation notes.**
- `DisableAppIconCardSO` hides the target app's **desktop icon** on the Laptop home screen (`DesktopIconView.SetVisible`), not the app itself, because apps only `Awake()` the first time they open. A hidden-icon request made before the icon exists is remembered and applied when it appears.
- `ClockSpeedCardSO` writes `ClockService.TickMultiplier` directly (`float`).
- Cost modifiers go through `CostModifierService`, one multiplier per purchase target (`Ticket`, `CardPack`, `Blackjack`, `Billiards`).
- Effects that act on systems which may not be initialised yet use the static pending-state pattern (§7 hazards): `MiningApp.Queue…`, `ScratchTicket.QueuePreRevealWindow()` with `OnPreRevealDelivered`, and `BilliardsPendingEffects` (extra shots, trick-shot preview, cue slip) for the next Billiards table.

### 5.5 Blackjack — laptop app

**The only time → money converter in the game.** The ante is paid in clock seconds; the payout is money.

| Key | Value | Where |
|---|---|---|
| `blackjackAnte` | 20 s off the clock | `GameConfig` |
| `blackjackClockMargin` | 15 s — cannot deal below `ante + margin` | `GameConfig` |
| `blackjackWinPayout` | $100 (also paid when the dealer busts) | `GameConfig` |
| `blackjackPushPayout` | $25 | `GameConfig` |
| `blackjackNaturalPayout` | $130 | `GameConfig` |
| `blackjackStreakStep` / `blackjackStreakCap` | +10% per consecutive win / capped at +50% (Hot Table) | `GameConfig` |
| `blackjackRevealDelay` / `blackjackResultDelay` | 0.6 s / 1.2 s | `GameConfig` |
| `laptopSwitchTime` | 1.0 s | `GameConfig` |
| Rules | Fresh shuffled 52-card deck per hand · dealer stands on all 17s (soft included) · one hand per ante · no split, insurance or surrender | `BlackjackGame` |
| Double | Only on the first two cards; costs a **second ante**; one card then auto-stand; payout ×2 | |
| EV | ≈ +$30–35 per hand before tax (+15% with Hot Table); at 4–6 s per hand that is ≈ $5–9/s | derived |

- **The ante is `round(blackjackAnte × CostModifier(Blackjack))`**; the margin check and the Double label use the same figure. A refused deal shows a *Not enough time* hint.
- **Hot Table:** a win or dealer bust raises a streak by 1, a push keeps it, a loss or bust resets it; each payout is multiplied by `1 + min(streak × blackjackStreakStep, blackjackStreakCap)`. Money only. The streak also resets when the app is closed or switched.
- **Closing the app mid-hand forfeits the ante and the hand.**
- Self-balancing: attractive when you are rich in time, suicidal when you are not. A hand burns about 31 s of clock, so the app needs the clock above the margin.
- The Laptop **must not go fullscreen** — Mining, Blackjack and Billiards are high-throughput; if they cover the desk, the Phone, Cat and Mail Tray stop existing.
- `BlackjackApp` owns ante, payout, safety rails and UI; `BlackjackGame` owns cards and rules (pure logic, seeded from `RngService`).
- **Alternate endings:** holding `blackjackCharlieCards` (7) cards without busting wins the run; losing `blackjackRuinStreak` (3) doubled-down hands in a row loses it (§5.14 D).

### 5.6 Laptop — Billiards

A pool-puzzle app. The player is dealt a **pre-made table** (a puzzle), picks a **difficulty** and a **bet**, then pots the target balls **in a set order** within a limited number of shots. Harder means fewer shots and a bigger payout. Playing costs real clock time.

**Round flow.** `Racked → Aiming → Rolling → Judging → Aiming | Won | Lost → Racked`.

- **Racked:** the player sees the table, its ball queue, its par and the win payout for each difficulty at the chosen bet, before paying anything. A drawn table stays racked across Laptop close/reopen until it is played, so closing the Laptop is not a free reroll.
- **Aiming:** the bet has been paid; the player pulls back and releases to shoot.
- **Rolling:** the shot is out and the simulation runs until every ball stops.
- **Judging:** the referee rules on the shot; the table is re-spotted as needed. Shots left and queue left → Aiming; queue empty → Won; a fail or no shots left → Lost.
- **Won / Lost:** a result panel with a *next table* button; the next table is drawn at random.

**Round setup.**
1. The player chooses a **bet** with −/+ buttons (`billiardsBetMin` to `billiardsBetMax` in steps of `billiardsBetStep`), never above the table maximum and never above what they can afford.
2. The **charge** is `round(bet × CostModifier(Billiards))`, rounded like Ticket and Card Box prices, and is spent at once. The payout is always computed from the **base bet**, so a cost-raising card makes the table worse, never better.
3. The difficulty sets the **shots granted**: `par + billiardsParOffsets[difficulty]` (Easy +4, Medium +2, Hard +0), unless the preset gives its own `shots`. Cards can add extra shots.

**Rules (`BilliardsReferee`).** "Hit the balls in order" is two rules: the cue's **first contact** must be the current head of the queue (`billiardsStrictFirstContact`), and pots are judged **in the order they drop**, so a combination (ball 1 then ball 3 in one shot) counts both. Cushion hits are not contact, so bank shots are legal.

| Event | Result |
|---|---|
| Head potted cleanly | Popped; the next ball moves up |
| A later queue ball potted early | Re-spotted to its start position |
| **Final ball** potted early or on a foul | **Fail** |
| Final ball in the wrong called pocket | **Fail** if `billiardsFinalWrongPocketFails`, otherwise re-spotted |
| A `forbidden` ball potted, at any time | **Fail** |
| Scratch (cue potted), wrong first contact, or touching nothing | **Foul**: nothing potted on that shot counts, those balls are re-spotted, and the shot is used up |
| A blocker ball (not in the queue) potted | Stays down, no effect |
| Wrong pocket for a non-final called ball | Re-spotted, not a foul; other legal pots in the same shot still count |
| Queue empty | **Win** |
| Out of shots | **Fail** |

Called pockets are optional and set per ball in each preset. After a scratch the cue ball returns to its preset spot, or the nearest free spot.

**Difficulty, assist and payout.**

| | Shots | Aim assist | Target rate (expected NET $/s, losses included) | Assumed win rate |
|---|---|---|---|---|
| Easy | `par + 4` | **FullGhost:** dotted cue path, ghost ball, object-ball line (length ∝ cut) and cue deflection line (length ∝ 1 − cut); if a cushion comes first, the path to the cushion plus one short reflected segment | $1.5/s | 0.85 |
| Medium | `par + 2` | **ContactPoint:** path and ghost ball | $3/s (the baseline, §3) | 0.65 |
| Hard | `par` | **DirectionOnly:** a stub 1.5 ball diameters long | $4/s | 0.5 |

A table pays for the time it should take, so long tables pay more without per-table tuning. With bet `b`, win rate `p`, and `T` the expected seconds of play:

```
T      = (par + shotsGranted) / 2 × billiardsSecondsPerShot × preset.payoutScale
payout = b × (1 + billiardsTargetRate[d] × T / billiardsBetMax) / billiardsAssumedWinRate[d]     (gross, stake included)
```

| Example: a par-4 table | Shots | Win at $40 | Win at $10 |
|---|---|---|---|
| Easy | 8 | $121 | $30 |
| Medium | 6 | $223 | $56 |
| Hard | 4 | $304 | $76 |

**Money.** A win pays the gross payout as two credits: the stake back (the lesser of the charge and the payout) with reason `MoneyService.StakeRefundReason`, then the profit. Only the profit is income for ITRS and the run statistics (§5.8). A loss costs the charge. The clock keeps running throughout, and once the run has ended the app freezes: no input, no money changes, nothing moves.

**Leaving the app.**
- *Racked* or *Aiming*: the table stays exactly as it is (the Phone and the Mail Tray can only be handled from the desk, so a round must survive a short trip away).
- *Rolling* or *Judging*: the shot is out, so leaving forfeits the bet and drops the round; the next table shows "You left mid-shot. That bet is lost." There is no free reset after a miss.
- *Won* or *Lost*: the finished table is cleared and the next visit draws a new one.

**Aiming.** Press anywhere on the felt, drag away from where the ball should go, release to shoot (pull-back aim, drag only). `direction = cue − pointer`, converted to table space; `power = |pull| / maxPull` with `maxPull` ≈ 0.6 × the felt height; releasing with power below 0.05, or a right-click, cancels. A cue stick sits behind the cue ball along the aim line and pulls back as power grows; a power bar shows the pull. The assist is visible only while Aiming. Every shot, from the player or from a replay, is **quantized**: angle to 0.1° (wrapped to [−180°, 180°)), power to 0.01, so recorded solutions replay exactly.

**Table, mirror and coordinates.**

| Space | Range | Used by |
|---|---|---|
| **Preset** | 0–1 × 0–1 (stretched 2:1) | JSON only: `table = preset × (2, 1)` |
| **Table** | 0–2 × 0–1, y up, isotropic | Simulation, pockets, **all angles**, stored solutions |
| **Felt** | canvas units, (0,0) = bottom-left of the felt | Views and pointer input: `felt = mirror(table) × feltWidth / 2` |

Pocket centres in table space: BL (0,0), BM (1,0), BR (2,0), TL (0,1), TM (1,1), TR (2,1). Angles are always computed in table space, never in the stretched preset space. Each draw also rolls a **mirror** (none / X / Y / XY), turning every table into up to four. The mirror is applied only to what the player sees and does — drawing positions, aim input, assist lines and pocket labels (`FeltMapper`). The simulation always runs the preset as authored, so every stored solution is valid in all four orientations.

**Ball ids.** `cue` and `1`…`15`. Sprites come from the Billiard Kit (CC0, credit "Graphics: Luca Pixel"): 1–7 solids, 8 the 8-ball, 9–15 stripes, cue the white ball.

**Simulation (`BilliardsSim`).** Pure deterministic C# — no `MonoBehaviour`, `Time`, `UnityEngine.Random`, `Physics2D` or Rigidbody. Balls are iterated in index order and pairs checked `i < j`, so one input always gives one output. Fixed step `FixedDt = 1/120 s`, driven by `Time.deltaTime` with at most 8 steps per frame (a slow frame plays out slower on screen; the outcome depends only on the number of steps). Each step runs in this order:

1. **Move:** `pos += vel × dt` for every unpotted ball.
2. **Friction:** `speed = max(0, speed − rollingDecel × dt)`, keeping direction; below `stopSpeed` the ball stops.
3. **Ball–ball**, for every unpotted pair: push apart by half the overlap; if approaching, apply the impulse `J = (1 + ballRestitution) / 2 × vrel` along the line of centres. The cue's first touched ball is the shot's `firstContact`.
4. **Cushions and jaws:** push penetrating balls out; reflect the normal velocity component with `cushionRestitution`. Cushions are the felt edges split by the pocket mouths; each segment end is a **jaw point** collided as circle-against-point.
5. **Pockets:** a ball whose centre is within its pocket's capture radius is potted (velocity zeroed, removed from collisions, appended to the shot report in drop order). A potted cue sets `cueScratched`.
6. **Settle:** when no ball moves the shot ends. After `billiardsMaxShotSeconds` every velocity is zeroed.

**Re-spot:** the preset spot if free (no unpotted ball within `2r + ε`, outside every pocket radius, inside the felt by at least `r`); otherwise the first valid spot searching rings `k = 1…12`, each with `8k` candidates at radius `k × 2.2r`, starting at angle 0 counter-clockwise. The search is deterministic. `StateHash()` (FNV-1a 64 over the bits of every ball's position, velocity and potted flag) and `Clone()` (a deep copy) support determinism tests and previews.

| Key | Value | Where |
|---|---|---|
| `billiardsBetMin` / `billiardsBetMax` / `billiardsBetStep` | $10 / $40 / $5 | `GameConfig` |
| `billiardsParOffsets` | 4 / 2 / 0 (Easy / Medium / Hard) | `GameConfig` |
| `billiardsTargetRate` | 1.5 / 3 / 4 | `GameConfig` |
| `billiardsAssumedWinRate` | 0.85 / 0.65 / 0.5 | `GameConfig` |
| `billiardsSecondsPerShot` | 7 s | `GameConfig` |
| `billiardsAssist` | FullGhost / ContactPoint / DirectionOnly | `GameConfig` |
| `billiardsStrictFirstContact` · `billiardsFinalWrongPocketFails` | on · on | `GameConfig` |
| `billiardsBallRadius` | 0.0614 table units (≈ 36 px on a 588-unit felt) | `GameConfig` |
| `billiardsCornerPocketRadius` / `billiardsSidePocketRadius` | 0.12 / 0.09 (≥ 1.9 × and ≥ 1.4 × the ball radius) | `GameConfig` |
| `billiardsMaxShotSpeed` | 5 table units/s at power 1 (`/120` must stay below the ball radius, or balls tunnel through cushions) | `GameConfig` |
| `billiardsRollingDecel` | 1.2 table units/s² | `GameConfig` |
| `billiardsBallRestitution` / `billiardsCushionRestitution` | 0.95 / 0.75 | `GameConfig` |
| `billiardsStopSpeed` | 0.01 | `GameConfig` |
| `billiardsMaxShotSeconds` | 20 s | `GameConfig` |

`GameConfig.ValidateBilliards()` is checked when the app first opens and rejects wrong-sized arrays, invalid bets and physics values that break the relations above; the app then reports "Billiards unavailable" instead of running with them. **Changing any physics value invalidates every stored solution** and requires re-running the preset verifier.

**Presets.** One JSON file per table in `Resources/BilliardsPresets`, loaded once, sorted by `id` (the same seed gives the same table). A table is drawn at random, never the one just played.

| Field | Meaning |
|---|---|
| `id`, `name` | Identity and display name |
| `par` | Shots of the known solution |
| `shots` | Optional `[easy, medium, hard]` override; 0 or missing = derive from `par` |
| `payoutScale` | Optional multiplier on the time term of the payout (default 1) |
| `balls` | `{id, x, y}` in preset space; exactly one ball is `cue` |
| `order` | Potting queue; the last entry is the final ball |
| `forbidden` | Optional balls that lose the table when potted |
| `called` | Optional `{ball, pocket}`: the pocket a ball must drop in |
| `solution` | The known solution: `{angle, power}` shots, quantized, in table space |

Validation: shots granted never increase with difficulty; Hard grants at least `par`; `solution` has `par` shots; ids are unique; positions lie on the felt; `order`, `forbidden` and `called` name existing balls and valid pockets.

**Table set.** Ten tables (including a test table that stays in the random pool) cover a straight pot, a cut, a bank shot, a combination, a blocker in the way, a called pocket, an 8-ball trap (the final ball sitting near a pocket) and a long table (par ≥ 5).

**Verification and authoring.**
- **Verifier** (`BilliardsPresetVerifier`): for each preset, replays the solution on Hard with no mirror; it passes when the result is a win on exactly the last solution shot, and a second replay gives an identical `StateHash`. Run from the Editor menu and from the cheat menu in a WebGL build, because Editor (Mono) and WebGL (IL2CPP) float maths can differ in the last bits and multi-ball shots amplify that.
- **Robustness:** 30 seeded replays per solution with each shot's angle jittered by ±0.3° and power by ±0.02 before quantizing. A pass rate under 70% marks the table knife-edge: players cannot aim to 0.1°, so such a table is unfair even if solvable.
- **Authoring tools** (Editor only): a layout edit mode (drag balls on a 0.005 grid, add/remove balls, set `order`, `forbidden`, `called`) and a record-and-export mode — with the mirror forced to none, every quantized shot of a won table is appended and exported as a preset with `par` = shots used.
- Self-tests cover rules, mirror, coordinate conversion, payout, preset loading, sim scenarios (straight pot, head-on, cushion reflection, scratch, 200 seeded shots without escapes or overlaps, clone and frame-loop determinism, zero-friction cap) and config validation.

**Cards.** `BilliardsPendingEffects` holds the effects waiting for the next round or shot (static, because the app may not have `Awake`d when a card is placed; reset on every scene load). Three effect cards use it, plus the two cards in the roster (§5.4):

| Card | Type | Effect |
|---|---|---|
| Chalk Up | Buff | +1 extra shot on the next table; ends when the round that took it starts |
| Trick Shot | Buff | On the next shot, while aiming, shows where every ball would end up (a `Sim.Clone()` run up to 10 times a second); ends when the shot fires |
| Cue Slip | Debuff | The next shot's power is jittered by ±15% (via `RngService`, before quantizing, so the simulation still gets a deterministic input); ends when the shot fires |

`OnRemoved` clears a pending effect that has not been used, so Pawning an unplayed card is a real escape (I7) and cannot keep the effect.

**UI and art.**
- **Window:** 668 × 474 canvas units on the laptop screen, cleared of the clock/money HUD (top left) and the camera buttons (right edge). The title bar holds the table name and difficulty on the left and the ball queue on the right.
- **Table:** the felt is **588 × 294 canvas units**, exactly 2:1, anchored and pivoted bottom-left, inside a nested Canvas (with its own `GraphicRaycaster`) so rolling balls never rebuild the main Canvas. Wooden rails (21 units) surround it, with a cushion shadow inside each edge; the six pocket holes are drawn from the same capture radii the simulation uses, so the visible hole and the capture area agree to the nearest art pixel. Table art is 3 canvas units per art pixel; balls are drawn 36 px wide from 42 px sprites. If the table art is redrawn, the felt rectangle and the capture radii stay fixed.
- **Balls:** one image per ball, no raycast, drawn at the simulation position through `FeltMapper`, snapped to the art pixel for drawing only. Ball size is `round(billiardsBallRadius × feltWidth)`. Potting is a visual-only 0.15 s tween into the pocket centre while scaling to 0; a re-spot punch-scales the ball back.
- **Ball queue** (`BallQueueView`): a strip of ball icons from the referee's queue. The head ball is enlarged with a pulsing halo, the final ball has a distinct frame, a ball with a called pocket shows a badge with the **mirrored** pocket label, and the head ball's called pocket gets a ring on the table. A popped icon slides out and fades (0.25 s) while the rest slide left; a re-spotted ball shakes its icon.
- **HUD:** shots left (large), the bet, the potential payout and the difficulty. While racked, the bet buttons and the three difficulty buttons (each with its shot count and win amount) take the same slot as the playing bar, so switching never moves the table.
- **Result panel:** on a win the net profit and the gross payout; on a loss the reason (e.g. "Out of shots", "8 dropped too early").
- **Status line:** a foul toast built from the shot report — "Foul: scratch", "Foul: touched nothing", "Foul: hit 5 first (needed 1)" — plus "Potted …", "Wrong pocket: … back on the table" and "No pot".
- **Sound hooks:** `OnPocket`, `OnWin`, `OnFail`, `OnFoul`.
- **Integration:** the app is registered in `LaptopController.apps`, has its own desktop icon (`appName` "Billiards", the same string the Not Valid 8-Ball card hides) and is an input blocker while open (§6.1). Services are read through `.Instance` on use, never through serialized references; DOTween tweens are killed when the app closes.

### 5.7 Phone — Shop and Delivery

The Shop is an app on the Phone (§5.9). Everything the player owns that is not in the **starting kit** is bought here. **Starting kit** (present at the start of every run): Laptop (with Mining), Wire Task, Egg Timer, Bear (Click Task), Picture, Triple Match stack, Phone, Printer (Pawn), Meter, Alarm Clock, Card Collection.

**Catalog.** An item catalog asset gives every item a permanent id, a price and whether it can be Pawned (it replaces any layer-based rule). The Shop shows the catalog, with a cart and a checkout.

| Item | Price | Notes |
|---|---|---|
| Power Bank | $25 | |
| Card Box | $60 | Packs are still bought repeatedly from the Card Box on the desk (§5.4) |
| Lucky Nine stack | $30 | |
| Blackjack license | $60 | Digital and permanent; activated from the Inbox; cannot be Pawned |
| Billiards license | $60 | Digital and permanent; activated from the Inbox; cannot be Pawned |

Blackjack and Billiards cannot be opened until their license is activated.

**Delivery.** A bought physical item arrives as a parcel; a license arrives in the Inbox. Each purchase picks a shipping option:

| Option | Cost | Wait |
|---|---|---|
| Standard | free | `shopStandardShipSeconds` 20 s |
| Express | `shopExpressCost` $8 | `shopExpressSeconds` 5 s |
| Leave it opened & ready | `shopReadyCost` +$3 | skips the unboxing |

A parcel is announced by a doorbell. The **door** sits beside the desk and the **mailbox** higher up the room (reached by scrolling the camera). The player unboxes the parcel, and the item appears in its place on the desk. The mailbox also receives paper tax bills (§5.8) and junk mail.

Neither shipping nor buying converts money to time (I3/I4 untouched). The Close The Shop card (§5.4) hides the Shop icon for 30 s.

### 5.8 ITRS — Internal Time Revenue Service

| Key | Value | Where |
|---|---|---|
| `billInterval` | 45 s | `GameConfig` |
| `billRate` | 20% of money **earned since the last bill** | `GameConfig` |
| `billMinimum` | $20 | `GameConfig` |
| `shortfallPenalty` | 2 s off the clock per $1 | `GameConfig` |
| `billDeadline` | 30 s to pay | `GameConfig` |

Scaling with **earnings, not elapsed time** punishes hoarding and gives a difficulty curve for one line of code. **Only profit is income:** a stake returned to the player (the `MoneyService.StakeRefundReason` credit paid on a Billiards win, §5.6) is not counted as earnings, either for the bill or for the run statistics.

**Flow.** Every `billInterval` seconds of running clock, `ITRSService` issues a bill, `max(billMinimum, earned × billRate)`, unless one is already pending (ITRS never issues a second bill while one is pending). The bill reaches the player two ways at once: a notification and a record in the **Taxes** app on the Phone, and a paper copy at the Mail Tray ("Taxes" desk object), which nags with a repeating sound every 2 s until opened. Paying through either one cancels the other.
- **Pay** spends the full amount; it is refused with a hint if the player cannot afford it.
- **No Ignore button.** If the bill is not paid within `billDeadline`, the penalty applies once: `amountDue × shortfallPenalty` seconds come off the clock. Closing a panel is not ignoring.

Bills, mail and the doorbell are non-blocking notifications; only phone calls interrupt (I8). A $0 bill (Tax Exemption) still has to be resolved.

### 5.9 Phone

The Phone is a smartphone with a **power button**: it boots in 5 s (the clock keeps running) and nothing on it works while it is off. Its apps are **Shop** (§5.7), **Call**, **Inbox** and **Taxes** (§5.8). The Phone can never be Pawned.

**Calls.** A call arrives with a caller ID: Telemarketer 40%, Client 25%, Unknown 25%, ITRS 10%. Only one call is active at a time, with a minimum gap of `annoyanceMinGap` between any two (a refused call is retried shortly, never dropped); a queue holds the next one. Declining a call is free. A call that is not answered stops by itself after `maxRingDuration`.
- While a call rings, the Clock ticks at `ringEffectMultiplier`.
- **Beneficial calls** (a Client's gig) stop the Clock and lock the Laptop while they last.
- Notifications appear as banners (at most 3, 4 s each) and are kept in the Inbox.

| Key | Value | Where |
|---|---|---|
| `ringIntervalMin` / `ringIntervalMax` | 30–60 s (random) | `GameConfig` |
| `ringEffectMultiplier` | Clock ticks at **1.5×** while a call rings | `GameConfig` |
| `answerTime` | 1.5 s after the click | `GameConfig` |
| `maxRingDuration` | 10 s, then auto-stops | `GameConfig` |
| `annoyanceMinGap` | 12 s between calls | `GameConfig` |

**Device layer.** Opening the Phone or the Laptop blurs the desk; the two devices sit side by side, each with a small clock. The Meter works only from the desk.

**Call scheduler [LOCKED]** — one scheduler (`CallScheduler`) owns the calls: one active call, minimum 12 s gap, never dropped.

> **Cat is exempt.** Its block is resolved by a single drag (pick the cat up, drop it elsewhere), so it carries no "two nags stacking" risk. If the Cat's interaction ever becomes a timed nag, it joins the call scheduler.

Cards interact with the Phone: Cable Eater turns the Phone and the Laptop off (§5.4); Spam Robocall halves the call interval (`StartRobocalls` / `StopRobocalls`).

The Phone's real job is making Mining scary. Without it, the laptop has no tension.

### 5.10 Egg Timer

| Key | Value | Where |
|---|---|---|
| `crankTime` | 1 s hold | `GameConfig` |
| `timePayout` | +3 s of clock | `GameConfig` |
| `eggTimerCooldown` | 0.75 s after each crank, in which it refuses input | `GameConfig` |
| Net | **≈ +0.71 s per second** (3 s per 1.75 s cycle, minus the second spent) | derived |

Hold the mouse on it for `crankTime` and it adds `timePayout` seconds directly to the Clock (a direct time source — see I3), then rests for `eggTimerCooldown`. Release and press again to repeat; progress resets on release. The cooldown belongs to the Egg Timer only (the Power Bank shares its hold logic but has its own cooldown). It is in the starting kit and cannot be Pawned.

### 5.11 Pawn and Buyback

Drag the **Printer** (the Pawn object) onto a pawnable object: it pays out immediately and the object leaves the room. There is no confirmation dialog.

**Payout.** An item bought in the Shop pays `floor(80% × price paid)`. Other pawnables pay a fixed value.

| Pawnable | Value | Where |
|---|---|---|
| Laptop | $120 | `laptopPawnValue` |
| Card Box | $48 (80% of $60) | catalog |
| Lucky Nine stack | $24 (80% of $30) | catalog |
| Power Bank | $20 (80% of $25) | catalog |
| Junk (small / medium / large) | $5 / $15 / $30 | `junkSmallPawnValue` / `junkMediumPawnValue` / `junkLargePawnValue` |
| A card on the desk | the card's `pawnValue` | `CardSO` |

**Not pawnable:** the starting kit except the Laptop (Egg Timer, Bear, Picture, Triple Match stack, Wire Task, Printer, Collection), licenses, Meter, Clock, Phone, Mail Tray, Cat, and a **triggered Final Countdown card** (§5.14). Pawning the Phone would delete an entire pressure system. Trying to Pawn a protected object shows a hint.

**Pawnable interface.** `IPawnable` exposes `PawnValue`, `CanBePawned` and `OnPawned()`. After paying out, `Pawn` calls `OnPawned()`. Most objects deactivate themselves there; `CardItem` returns to its pool instead.

**Buyback.** Buyback is a function of the Phone. Before `OnPawned()` runs, a pawn ledger records the item (any `IRestorable` item). The player can buy a pawned item back through the Phone: the price starts at ×1.10 of the pawn payout and rises to ×1.25 over 90 s of game time. The item returns to its place. Licenses are never in the ledger.

### 5.12 Cat

A small roaming state machine (`CatController` / `CatStateMachine`):

- `Wandering` — walks to a random point inside a roam area, then → `SittingIdle`.
- `SittingIdle` — plays an idle or a lying clip family and randomly switches between clips of that family; a Sleep clip locks the cat in place for 3–6 s. After the sit duration → `Wandering`.
- `SittingBlocking` — entered when the Cat, while `Wandering`, touches an interactable desk object and wins the `catBlockChance` roll. It disables that object for a random duration (clicks on it show the *cat is in the way* hint), then re-enables it and → `Wandering`.
- `Dragged` — the instant the player starts dragging the cat; returns to `Wandering` on release.

| Key | Value | Where |
|---|---|---|
| `catSitDurationMin` / `catSitDurationMax` | 6–14 s | `GameConfig` |
| `catBlockChance` | 0.4 | `GameConfig` |
| `catBlockDurationMin` / `catBlockDurationMax` | 3–6 s | `GameConfig` |
| Walk speed · sleep lock · clip-switch chance | 1.5 · 3–6 s · 0.3 per 1 s check | `CatController` component |

**Removal is a drag, not a click count.** No `shooCost`, no click-counting. Blocking uses `PointerReceiver.SetInteractable(false, HintId.CatBlocking)` on the target. Pure charm mechanic.

### 5.13 Tasks

Small repeatable desk tasks built on `TaskBase` (`CompleteTask()` → `ApplyReward()` → `OnTaskCompleted()`). Tasks pay **money**, never clock time (the Egg Timer, §5.10, is the exception).

| Task | Mechanic | Reward | Values | Where |
|---|---|---|---|---|
| **Click Task** (`ClickTask`) | Click a random 3–8 times; greys out for 3 s after completion (clicks show a *cooling down* hint), then re-rolls | $5 | `clickTaskMinClicks` 3 · `clickTaskMaxClicks` 8 · `clickTaskLockDuration` 3 s · `clickTaskMoneyReward` 5 | `GameConfig` |
| **Picture Click Task** (`PictureClickTask`) | Same as Click Task with a reacting picture sprite | $5 | `pictureClickTask…` (same values) | `GameConfig` |
| **Power Bank** (`PowerBankTask`) | Hold 2 s to fill (progress is kept on release); after the full-hold delay it drains, rests, then is usable again (clicks meanwhile show a *recharging* hint) | $8 | `powerBankCrankTime` 2 s · `powerBankMoneyReward` 8 · `powerBankFullHoldDelay` 1.5 s · `powerBankCooldown` 5 s · `powerBankRestTime` 2 s | `GameConfig` |
| **Wire Task** (`WireTask`) | Plug-to-socket puzzle (below) | $20 | `wireTaskMoneyReward` 20 | `GameConfig` |

**Wire Task.** Five colour-coded plugs and sockets (`WirePlug`, `WireSocket`, `WireLineView`, `WirePanelInitializer`), sockets shuffled every time the puzzle begins using `RngService`. The player drags each plug to its matching socket: a wrong drop is rejected, a right one draws a connecting line and locks the pair. Completing every pair pays the reward, switches the puzzle off, and **unlocks the Laptop** — the Laptop is locked from the start of the run until the Wire Task is solved (`LaptopController.Disabled`; clicking the Laptop meanwhile shows the *no power* hint). Pawning the Wire Task before solving it leaves the Laptop locked.

### 5.14 Win & lose conditions

Besides the normal Clock check, three cards (A–C) and three mechanics (D) can end the run. Every ending is an `EndReason`:

| `EndReason` | Meaning | Trigger |
|---|---|---|
| `ClockWin` | Win | Clock ≥ `winClock` (600 s) |
| `SetCompletionWin` | Win | Set completed on the desk |
| `FinalCountdownWin` | Win | Second Final Countdown placed after the first triggered |
| `ClockLoss` | Loss | Clock reaches 0 and nothing intercepts it |
| `FinalCountdownLoss` | Loss | Second Final Countdown placed before the first triggered |
| `CreepyJesterLoss` | Loss | Creepy Jester finishes its last stage |
| `TicketJackpotWin` | Win | Lucky Nine: all nine windows revealed as the rarest symbol |
| `TicketCurseLoss` | Loss | Lucky Nine: all nine windows revealed as the commonest symbol |
| `MotherLodeWin` | Win | Three maximum-length veins banked in a row |
| `CaveInLoss` | Loss | A cursed maximum-length vein is banked |
| `BlackjackCharlieWin` | Win | Seven cards held without busting |
| `BlackjackRuinLoss` | Loss | Three doubled-down hands lost in a row |

**A. Set completion (`SetCompletionCardSO` + `SetCompletionTracker`).** The anchor card is **Mei – The Tester**. Its pieces are four groups: **Han** (Han – The Jester *or* Han – The Pierrot), **Ari – The Gambler**, **Sybil – The Caster**, **CtrlZ – The Boy Skull**. While the anchor is Placed, the tracker counts Placed cards (`CardHandManager`'s live Placed set — **not** the Collection, which means "ever obtained"). The moment every group is satisfied, the run is won. The check re-runs on every place and remove. On a win the anchor and the satisfying pieces are registered in the Collection. Removing the anchor stops tracking.

**B. Final Countdown (`FinalCountdownCardSO` + `FinalCountdownTracker`).** Phases: `None` → `Armed` → `Triggered`.

| State when a Final Countdown card is Placed | Result |
|---|---|
| `None` | **Armed.** `ClockService` is told to intercept the next time the Clock reaches 0 |
| `Armed` (a second copy) | Instant **loss** (`FinalCountdownLoss`) |
| `Triggered` (a second copy) | Instant **win** (`FinalCountdownWin`); the card is registered in the Collection |

- **Trigger.** When the Clock counts down to 0 while `Armed`, the intercept is used up: the Clock is set to `resetSeconds` (180 s), the phase becomes `Triggered`, the **Meter is locked for the rest of the run**, and the Final Countdown intro cutscene plays while the Clock is paused.
- **After the trigger** the Clock runs normally from 180 s. If it reaches 0 again the run is lost (`ClockLoss`). Without the Meter, time comes only from the Egg Timer and card effects.
- **Only the Clock counting down to 0 by itself is intercepted.** Time removed directly (the ITRS deadline penalty, Blackjack ante) that takes the Clock to 0 ends the run as `ClockLoss` immediately, whatever the phase.
- While `Armed`, the card can be Pawned (it disarms, and the intercept is removed). Once `Triggered` it **cannot be Pawned** — the one card with no I7 escape hatch.
- A Final Countdown card behaves as a **debuff** once the phase is not `None`: it can only be Placed (no Sell, no Collection) and it ignores `handCap`.
- A second copy "drawn" means **Placed**, like every other card effect.

| Key | Value | Where |
|---|---|---|
| `resetSeconds` | 180 s | `FinalCountdown` `CardSO` asset |
| Rarity · weight | SR · 16 | `CardSO` |

**C. Creepy Jester (`CreepyJesterCardSO`, "Han – The Pierrot").** A loss card disguised as a collectible: hidden in the Collection book until collected. When Placed it counts as the Han piece of the set (A) and starts changing its art every `intervalSeconds` (20 s) through its art pool. **When it reaches the last art it deletes all saved data (`PlayerPrefs.DeleteAll()` — Collection, run history, tutorial and hint flags) and ends the run as `CreepyJesterLoss`.** Pawning it stops the threat; if its art has already progressed it is credited to the Collection. This wipe is deliberate: it is the card's horror gimmick.

| Card | Rarity · weight |
|---|---|
| Mei – The Tester | R · 8 |
| Final Countdown | SR · 16 |
| Han – The Pierrot | C · 4 |

**D. Mechanic endings.** All numbers live in `GameConfig`. Each ending is checked where the triggering action resolves, and calls `GameManager.GameOver(EndReason)`.

| Mechanic | Ending | Rule | Values |
|---|---|---|---|
| Lucky Nine | `TicketJackpotWin` | A jackpot ticket (§5.3) has all nine windows revealed by scratching | `luckyNineJackpotChance` 0.2% per ticket |
| Lucky Nine | `TicketCurseLoss` | A cursed ticket (§5.3) has all nine windows revealed by scratching. Stopping early with Cash Out avoids it | `luckyNineCurseChance` 0.5% per ticket |
| Mining | `MotherLodeWin` | `miningStreakTarget` consecutive veins of length 10, banked without closing or leaving the Mining app. Any shorter vein, or leaving the app, resets the streak | `miningStreakTarget` 3. After a full vein, the next vein is length 10 with `miningFullVeinStreakChance` (0.3) instead of the normal 1 in 5 |
| Mining | `CaveInLoss` | A length-10 vein may be cursed when it starts. From block 8 of a cursed vein the app shows a warning ("The ceiling is creaking."); banking it ends the run. Closing the app (forfeiting the unconfirmed ore) avoids it | `caveInChance` 3% per length-10 vein |
| Blackjack | `BlackjackCharlieWin` | The player holds `blackjackCharlieCards` cards without busting | `blackjackCharlieCards` 7 |
| Blackjack | `BlackjackRuinLoss` | `blackjackRuinStreak` doubled-down hands lost in a row. A doubled-down win or push resets the count; hands that are not doubled do not count | `blackjackRuinStreak` 3 |

These endings are meant for roughly 1–3% of runs each: a run buys about five Lucky Nine tickets, banks about eight veins and plays a handful of hands.

**End of run.** `GameManager.GameOver(EndReason)` disables input, pauses the Clock, freezes game time, pauses audio, and fires `OnGameOver`. `CutsceneController` then plays that reason's Timeline ending (clock endings play a ticker that counts the last seconds; a reason without a Timeline goes straight to the panel), after which the end panel fades in (1 s delay, 0.6 s fade) with the ending text:

| `EndReason` | Ending text |
|---|---|
| `ClockWin` | "The Timer reaches 10 minutes." |
| `SetCompletionWin` | "The MASCH make you win!" |
| `FinalCountdownWin` | "You beat the Final Countdown!" |
| `ClockLoss` | "Time out." |
| `FinalCountdownLoss` | "The countdown betrayed you." |
| `CreepyJesterLoss` | "The Pierrot found you..." |
| `TicketJackpotWin` | "Nine of a kind. The ticket was real." |
| `TicketCurseLoss` | "The ticket was cursed." |
| `MotherLodeWin` | "You struck the mother lode." |
| `CaveInLoss` | "The mine collapsed." |
| `BlackjackCharlieWin` | "Seven cards, no bust. The dealer folds." |
| `BlackjackRuinLoss` | "The house always wins." |

### 5.15 Run Save, Summary & Rating

The project's persistence uses `PlayerPrefs` (on WebGL it is stored in the browser's IndexedDB). `GameManager.Restart()` reloads the scene; saved data survives it.

**A. Per-run history — circular buffer, 20 slots.** On every `GameOver`, `RunStatsService` finalizes one `RunRecord` (serialized with `JsonUtility`) into slot `RunHistory_<n>`, advances `RunHistory_NextSlot` (wraps 19 → 0) and increments the lifetime counter `RunHistory_Total`. Past 20 runs the oldest slot is overwritten.

| Field | Source |
|---|---|
| `bestTime` | Highest Clock value seen via `ClockService.OnTimeChanged` during the run |
| `winCondition` | The `EndReason` name (§5.14) |
| `totalMoneyEarned` | Sum of positive deltas of `MoneyService.OnMoneyChanged`, excluding stake refunds (the service diffs against its last seen `Current`) |
| `cardsOpened` | Count of reveals (`CardRevealSlot.CardRevealed`) |
| `newCardsToCollection` | Count of `CardCollectionManager.NewCardRegistered` events |
| `runNumber` | `RunHistory_Total` after this run |

**B. Lifetime Collection.** `CardCollectionManager` loads the key `CardCollection_UnlockedIds` (comma-joined card asset names) at start and re-saves it whenever `Register()` adds a new card. "Collection" therefore means *ever obtained, on this device, across all runs*.

**C. Run score.** Each record has a score from 0 to 100 (`RunRating.Score`):

```
score = 100 × (wO × outcome + wS × survival + wM × money) / (wO + wS + wM)
survival = 1 for a win, else clamp(bestTime / winClock)
money    = earned / (earned + ratingMoneyHalfPoint)
```

| Key | Value | Where |
|---|---|---|
| `ratingOutcomeWeight` / `ratingSurvivalWeight` / `ratingMoneyWeight` | 0.5 / 0.3 / 0.2 | `GameConfig` |
| `ratingMoneyHalfPoint` | $300 | `GameConfig` |
| Outcome value | `ClockWin` 1 · `SetCompletionWin` 0.95 · `FinalCountdownWin` 0.95 · `ClockLoss` 0 · `FinalCountdownLoss` 0.1 · `CreepyJesterLoss` 0 · mechanic wins 0.95 · mechanic losses 0 | `GameConfig` |

A run is a *win* for the history viewer when its `EndReason` is any of the six win endings.

**UI.**
- **End panel** — the ending text (§5.14) plus *Best Time*, *Money Earned*, *Cards Opened* and *New Cards* (`EndGameSummaryUI`).
- **Run history viewer** (`RunHistoryPanel`) — opened from a button. One row per stored run: run number, WIN/LOSE with the ending title (e.g. "Survived 10:00", "Set Complete", "Time Out"), best time, money, cards opened, new cards and score; the run just finished is highlighted. A sort button cycles *Best Score*, *Longest Run*, *Most Money* and *Fastest End*. An empty history shows an empty label.

### 5.16 Onboarding & Hints

Players learn the basics from a short intro panel, and everything else from **hints**: small tooltips shown when an action fails or the first time a mechanic appears. The surprises (cards, win/lose cards, Final Countdown, Creepy Jester) are deliberately never explained up front.

**Hint rules.**
- One hint on screen at a time (`HintService`). The same `HintId` is not repeated within `hintRepeatCooldown`.
- A hint states the *problem* and the *next step*, never an unrevealed outcome.
- No hover is required, so hints work with touch input.
- Failed clicks raise `ClickRejected(HintId)` on `PointerReceiver` (and the static `AnyClickRejected`, which `HintService` listens to). Anything that calls `SetInteractable(false, HintId)` (Cat, cooldowns) supplies the reason. Other failures call `HintService.Show` directly.
- `ShowOnce` hints show the first time ever on this device (flag `HintSeen_<id>` in `PlayerPrefs`).
- Hints are not shown after the run has ended.

| Key | Value | Where |
|---|---|---|
| `hintHoldSeconds` | 2.5 s | `GameConfig` |
| `hintFadeSeconds` | 0.5 s | `GameConfig` |
| `hintRepeatCooldown` | 3 s | `GameConfig` |

**Intro panel** (`TutorialPanel`): shown once ever (flag `TutorialSeen`); it freezes game time (I5); click anywhere to advance, a Skip button closes it. Texts:

| # | Text |
|---|---|
| 1 | **You have 2 minutes to live.** Get the clock to **10:00** before it hits **0:00**. |
| 2 | **Click** things to use them. **Drag** things to move them. If something doesn't respond, it may be blocked, so look for a hint. |
| 3 | Everything on the desk can earn **money**. Money alone doesn't save you. |
| 4 | Click the **Meter** to turn money into time: **$1 = 1 second.** It spends all your money at once. |
| 5 | Hold the **arrow buttons** to look around the room. Now try your luck. |

**Hints when a click is rejected or an action fails** (`HintCatalog`):

| `HintId` | Reason | Hint |
|---|---|---|
| `LaptopNoPower` | Wire Task not solved | The laptop has no power. Check the wiring. |
| `CatBlocking` | Cat is sitting on the object | The cat is in the way. Pick it up and move it. |
| `NotEnoughMoney` | Cannot afford a ticket, pack or Shop item | Not enough money. |
| `MeterMinFeed` | Balance below `meterMinFeed` | The Meter needs at least $10. |
| `MeterLocked` | Meter locked | The Meter is locked. |
| `TooManyTickets` | No free ticket in the pool | Too many tickets on the desk. Cash one out first. |
| `HandFull` | Hand full when buying a pack | Your hand is full (5). Pawn a card to make room. |
| `NoRoomToPlace` | No free card slot to Place a revealed card | No room to place it. Pawn a card first. |
| `NotEnoughTime` | Clock below ante + margin | Not enough time to play. You need 35 s on the clock. |
| `TaxCannotAfford` | Cannot afford the bill | Not enough money to pay. Missing the deadline costs clock time. |
| `ClickCooling` | Click Task cooling down | Still cooling down. |
| `PowerBankRecharging` | Power Bank recharging | Recharging... |
| `CannotPawn` | Object cannot be pawned | This can't be pawned. |

**Hints on first use or risk:**

| `HintId` | When | Hint |
|---|---|---|
| `LaptopIconDoubleClick` | First single click on an icon | Double-click an icon to open it. |
| `MiningOpen` | First time Mining opens | Drag the pickaxe into the block and back out to swing. |
| `MiningCloseLosesOre` | Closing with unconfirmed ore | Closing now loses your unconfirmed ore. Finish the vein to bank it. |
| `BlackjackOpen` | First time Blackjack opens | Betting costs clock time, wins pay money. |
| `BlackjackLeaveMidHand` | First deal | Leaving now forfeits your ante and hand. |
| `TicketOpen` | First time a ticket opens | Scratch with the mouse. Cash Out to collect. |
| `WireOpen` | First time the Wire Task opens | Drag each plug to the socket of the same colour. |
| `PhoneFirstRing` | First ring | The phone is ringing! Click it. The clock runs faster until it stops. |
| `MailTrayFirstBill` | First bill | A tax bill arrived. Click the mail tray. |
| `CatFirstBlock` | First time the Cat blocks an object | Drag the cat to move it. |
| `EggTimerPlaced` | First time the Egg Timer is placed | Hold to wind it. Release and hold again to repeat. |
| `ShopPurchased` | After a purchase | Your item is on the desk. |

**Shop catalogue.** The Shop (§5.7) sells everything outside the starting kit. Each entry has a one-line description of what the item does and how to use it; it never states a cost the player is meant to discover. Prices are separate fields.

| Item | Description |
|---|---|
| Power Bank | Hold to charge it for money. It needs time to recharge afterwards. |
| Card Box | Buy a pack of 3 cards. Cards can help or hurt. Place them on the desk to use them. |
| Lucky Nine stack | Buy a scratch ticket with 9 windows. Costs more, can pay a lot more. |
| Blackjack license | Unlocks the Blackjack app on the Laptop. Activate it from your Inbox. |
| Billiards license | Unlocks the Billiards app on the Laptop. Activate it from your Inbox. |

**Tutorial panels.** Each new mechanic introduced by the Phone and the Shop (calls, the Inbox, the Taxes app, shipping and delivery, Buyback) gets one short panel, built on the same `TutorialPanel` and `HintService` as the intro.

---

## 6. Interfaces & ownership

### 6.1 Interfaces

The public surface other systems rely on:

| Member | Purpose |
|---|---|
| `MoneyService.Add(int amount, string reason)` | Award money. `reason` feeds the balance log |
| `MoneyService.TrySpend(int amount, string reason) → bool` | Returns false if unaffordable. **Never** lets a caller go negative |
| `MoneyService.Current` · `OnMoneyChanged(int current, string reason)` | Read-only balance · change event (carries the new balance, not the delta) |
| `MoneyService.StakeRefundReason` | Reason for money that is only a returned stake; not income |
| `ClockService.AddSeconds(float s, string reason)` | Meter, Egg Timer and developer tools only |
| `ClockService.Spend(float s, string reason)` | Blackjack ante, ITRS penalties |
| `ClockService.CurrentSeconds` · `OnTimeChanged` · `OnCountdownFinished` | Read-only value · events |
| `ClockService.TickMultiplier` | Phone and speed cards write this |
| `ClockService.ArmFinalCountdownIntercept(float)` · `OnFinalCountdownTriggered` | Final Countdown only |
| `GameManager.GameOver(EndReason)` · `OnGameOver` · `IsGameOver` | The one way a run ends |
| `GameManager.PauseGame()` / `ResumeGame()` | Counted freeze of game time for modal panels |
| `InputGate` · `InputBlocker` | Every enabled `InputBlocker` increments `InputGate`; while the count is above zero, world input and the camera scroll buttons are disabled |
| `PointerReceiver.SetInteractable(bool, HintId? reason)` · `ClickRejected` | One mechanism for debuffs, the Cat and cooldowns; the reason becomes the hint |
| `HintService.Show / ShowOnce(HintId, args)` | Show a hint (§5.16) |
| `IPawnable` | `PawnValue`, `CanBePawned`, `OnPawned()` |
| `IRestorable` · pawn ledger | An item that Buyback can bring back; the ledger records it before `OnPawned()` runs |
| `CallScheduler` | One active call, minimum 12 s gap, queue of the next |
| Item catalog | Ids, prices and pawnability of every purchasable item |
| `RngService.Random` | Shared seeded `System.Random`. Never use `UnityEngine.Random` |
| `CostModifierService.GetMultiplier / SetMultiplier(Target)` | Price multipliers per target: `Ticket`, `CardPack`, `Blackjack`, `Billiards` |
| `GameConfig` | All economy tuning values (I10) |

### 6.2 File ownership — do not cross

| Path | Owner |
|---|---|
| `Scripts/Services/`, `Scripts/Config/` | Ari |
| `Scripts/Desk/Laptop/` (Mining, Blackjack, Billiards), `Scripts/Annoyance/` (except Cat) | Ari |
| `Scripts/Desk/Items/`, `Scripts/Annoyance/Cat/`, `Scripts/Hint/` | Han |
| `Art/`, `Audio/` | Han |
| `GameConfig.asset` | **Ari only** |

**Only Ari edits `GameConfig.asset`.** Han requests values in chat and Ari changes them — Unity `.asset` merge conflicts are unrecoverable at the end of a sprint. Asset serialization is **Force Text**, with `*.unity` and `*.prefab` marked `merge=unityyamlmerge`.

---

## 7. Unity implementation notes

- **Scene:** one. Orthographic camera that scrolls vertically within `[minY, maxY]` (I9). No scene loading — restart reloads the scene.
- **Platform:** WebGL build with the `Custom1080` template, published on itch.io. `PlayerPrefs` persists in the browser (IndexedDB).
- **Input:** mouse only. `InputManager` distinguishes click from drag with `dragThreshold` (0.15 world units): a click resolves on release without a drag, a drag fires `DragStart/Update/End`. Press events (`PressStart/Update/End`) serve hold-style objects. Panels mark themselves as input blockers with an `InputBlocker` component (§6.1).
- **Hotspots:** every interactable has a `PointerReceiver`; `DeskObject` is the base class for `ClickDown` / drag handling. Debuffs, the Cat and cooldowns gate a hotspot with `PointerReceiver.SetInteractable(bool, HintId?)` — one mechanism for all of them.
- **Clock:** one `ClockService` owns `RemainingTime` and `TickMultiplier`. Nothing else writes the Clock except the Meter, the Egg Timer, explicit penalties (`Spend`) and the Final Countdown reset (§5.14).
- **Cutscenes:** `CutsceneController` plays Unity Timeline (`PlayableDirector`, unscaled game time) for each `EndReason`, for the Final Countdown intro, and a ticker for clock endings.
- **Laptop:** `LaptopController` shows one app at a time; switching costs `laptopSwitchTime`. Closing the laptop or going home during a switch cancels the switch and returns to the home screen.
- **Scratch masking:** per-window CPU `Texture2D` alpha mask (§5.3).
- **Randomness:** single seeded `System.Random` in `RngService` (optional fixed seed for reproducible runs).
- **Update loop:** each service ticks in its own `Update` (`ClockService`, `ITRSService`, `Phone`). Short-lived coroutines are fine for local sequencing (Blackjack reveal delay, Mining respawn, Cat clips).
- **Libraries:** DOTween (card flips, hint fades, end panel fade), Unity Input System, TextMeshPro, Timeline.
- **Billiards tests:** Editor self-tests and a preset verifier menu replay every stored solution headless (§5.6).
- **Dev tools:** a cheat menu (F1 toggles it, F2 stops a cutscene) can give money and time, give any card, and preview or trigger every ending. It is a development tool, not a game feature, and is excluded from release builds.

**Two hazards, standing conventions —**

- **A GameObject (or any ancestor, including a whole closed panel) that starts inactive defers every attached script's `Awake()` until first activation.** Anything that reads another service's `.Instance` can silently no-op if that service hasn't `Awake()`d yet (`MiningApp` and `DesktopIconView` behind the closed Laptop, `ScratchWindow` populated before its `Awake()`). Services read other services in `Start()`, not `Awake()`; apps read services through `.Instance` on use, never through serialized references. Where an `Instance` genuinely might not exist yet, use a **static pending-state field** applied when the real `Awake()` finally runs (`MiningApp.Queue…`, `ScratchTicket.QueuePreRevealWindow()`, `DesktopIconView.SetVisible`, `BilliardsPendingEffects`). Static state is reset on every scene load, because `Restart()` is a scene reload.
- **`?.` on a `UnityEngine.Object` does not detect Unity's "fake null"** for an object that was `Destroy()`ed but is still referenced (a static field, an event subscriber). Compare explicitly with `!= null` for any Unity object that may be destroyed at an uncertain time.

---

## 8. Glossary

Use these exact terms in code, commits, and agent prompts.

| Term | Meaning |
|---|---|
| **Clock** | The countdown. Win at 600 s, lose at 0 s |
| **Meter** | The only money → time converter; feeds the whole balance at once |
| **Vein** | One random-length (6–10 block) mining sequence |
| **Bank** | Complete a vein and receive its payout |
| **Unconfirmed** | Mining ore not yet banked; lost if the app is closed or switched |
| **Hand** | The player's Placed cards. Cap is **5**; counts cards sitting on the desk as a live `CardItem`, including ones already flipped face-down |
| **Placed** | A card put on the desk as a live `CardItem`; counts against the Hand for as long as it exists there |
| **Flip** | A card's effect ending — it turns face-down and stays on the desk until Pawned |
| **Pack** | 3 cards revealed together (independently resolvable) when a Card Box pack is opened |
| **Rarity** | Card class `C` / `R` / `SR` / `UR`; determines pack drop weight |
| **Collection** | Registry of card types ever obtained, saved across runs; not a physical place |
| **Bill** | An ITRS tax charge |
| **Call** | The only interruption: a phone call with a caller ID, scheduled by `CallScheduler` |
| **Smartphone / Inbox / Taxes app** | The Phone with its power button and apps; the Inbox keeps notifications and license activations; the Taxes app shows bills |
| **Starting kit** | The items present at the start of every run; everything else is bought in the Shop |
| **Catalog / License** | The item catalog asset with ids and prices / a permanent digital unlock for the Blackjack or Billiards app |
| **Parcel / Door / Mailbox** | A delivered purchase / where parcels arrive / where paper bills and mail arrive, higher up the room |
| **Buyback** | Buying a pawned item back through the Phone at ×1.10 rising to ×1.25 of its pawn payout |
| **Baseline** | $3/second — the throughput a task is judged against |
| **Throughput** | Money per real second a task generates |
| **Room** | The scrollable play space beyond the original desk frame (I9) |
| **Wire Task** | The plug-to-socket puzzle that unlocks the Laptop (§5.13) |
| **Table / Preset** | A Billiards layout: balls, potting order, optional called pockets and forbidden balls (§5.6) |
| **Ghost ball** | The Billiards assist marker showing where the cue ball will be when it first touches another ball |
| **Knife-edge** | A Billiards table whose stored solution passes under 70% of jittered replays, so players cannot realistically repeat it |
| **Queue** | The ordered list of balls still to pot on a Billiards table; the first is the head, the last the final ball |
| **Foul** | A Billiards shot where the cue ball scratched, touched nothing or touched the wrong ball first |
| **Respot** | Putting a ball back on the table after an illegal pot |
| **Charge / Payout** | What a Billiards round costs (bet × cost modifier) / the gross amount a win pays, stake included |
| **Stake refund** | The part of a payout that is the player's own bet coming back; untaxed and not counted as earnings |
| **Set completion** | Win by Placing Mei – The Tester plus all four piece groups at once (§5.14) |
| **Final Countdown** | Card that intercepts the first Clock-at-0 loss once; a second copy wins (after the trigger) or loses (before it) |
| **Armed / Triggered** | Final Countdown phases: waiting for the Clock to reach 0 / the intercept has been used |
| **Creepy Jester** | The "Han – The Pierrot" loss card that wipes saved data when it finishes |
| **Run Record** | One saved run's summary, written to one of 20 circular slots |
| **EndReason** | The enum of the twelve ways a run can end (§5.14) |
| **Omen** | The visible warning before a mechanic loss (a ticket of repeated common symbols, the creaking mine) that lets the player back out |
| **Hint** | A short tooltip shown when a click is rejected or a mechanic first appears (§5.16) |
