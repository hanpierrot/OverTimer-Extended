---
paths:
  - "**/Cards/**"
  - "Assets/Resources/CardSO/**"
---

# Card System Rules

## Ownership

- Ari (or an agent) writes card **code**. **Han creates and edits card assets**: name, description text, art, rarity, weight, pawn value. Hand Han a spec instead of creating assets yourself.
- **Never rename a card asset.** The Collection is saved in PlayerPrefs by `CardSO.name` (comma-joined), so a rename loses unlocks and a comma corrupts the list.

## Effect contract

- `EffectCardSO.OnPlaced(card)` grants the effect, **owned by that `CardItem`** (not by the shared ScriptableObject: two copies of one card must not collide).
- `OnRemoved(card)` **always** withdraws it. It runs exactly once (guarded by `CardItem._hasEnded`) on natural end, early pawn or scene unload. An empty `OnRemoved` is a bug (Audit is the known one).
- Using the effect calls `card.EndEffect()`. Timed effects use `card.ScheduleAutoRemove(duration)` with a per-asset `[SerializeField] duration`.
- Effects that target a system that may be asleep (laptop apps, tickets) go through a static pending list on that system, cleared on `sceneLoaded`.
- Card effects never add money or clock directly (I3/I4). Debuffs close a door (hide an icon, raise a price, add noise), never drain the clock (I7).

## Lifecycle facts to respect

- 7 pooled `CardItem`s; never `Instantiate`. Pool math: `(handCap − 1) + packSize ≤ 7`.
- `handCap` (5) gates **buying a pack**, not placing. Spent cards flip face-down and keep their slot until pawned.
- Debuffs: after reveal they can only be Placed (no Collection, no Sell); before reveal they can be blind-sold for $10.
- Pawning early cancels the effect and gives **no** Collection credit.

## Testing

- Test with a throwaway `ScriptableObject.CreateInstance<YourCardSO>()`, place it through the cheat menu or `CardPackRevealPanel.TryPlaceCard`, then: let it expire, pawn it mid-effect, and Restart with it active. All three must leave no effect behind.
- Update `PROJECT.md` §12 (class table, card pool, notes).
