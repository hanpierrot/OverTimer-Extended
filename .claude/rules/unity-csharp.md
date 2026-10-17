# Unity C# Rules (OverTimer Extended)

Always loaded. These rules come from real bugs in this project; each one says why.

## Lifecycle and init order

- Set your own `Instance` in `Awake`. Read **other** services' `.Instance` in `Start` (or at use time), never in `Awake` or `OnEnable`. Unity runs every `Awake` before any `Start`, but the order between different scripts' `Awake`/`OnEnable` is not guaranteed.
- Objects saved **inactive** (every Canvas panel, laptop apps, pools) don't `Awake` until first activated. To affect one before that, write a **static pending state** that it applies in its own `Awake`/`OnEnable` (see `DesktopIconView.SetVisible`, `MiningApp.QueueNextVeinMultiplier`, `BilliardsPendingEffects`).
- Don't add `[DefaultExecutionOrder]` or change Script Execution Order to fix an ordering bug without asking; fix the dependency instead.
- Singleton guard (house style):
  ```csharp
  void Awake()
  {
      if (Instance != null && Instance != this) { Destroy(gameObject); return; }
      Instance = this;
  }
  ```
  No `DontDestroyOnLoad`: Restart reloads the scene and every singleton is rebuilt.

## Static state

- Restart = scene reload. **Every mutable static must be reset on scene load**:
  ```csharp
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  static void Hook()
  {
      Reset();
      SceneManager.sceneLoaded -= OnLoaded;
      SceneManager.sceneLoaded += OnLoaded;
  }
  static void OnLoaded(Scene s, LoadSceneMode m) => Reset();
  ```
- Static events: subscribers unsubscribe in `OnDisable`/`OnDestroy`. Null the event in `Reset()` if nothing in the scene should survive.
- Documented exceptions only: `BilliardsRoundLog` (dev telemetry), `BilliardsDebug` (cheat switches).

## Null checks

- Unity objects: explicit `if (x != null)`. **Never `?.` or `??` on a `UnityEngine.Object`** (MonoBehaviour, ScriptableObject, GameObject, Component). Unity overrides `==` to detect destroyed objects; `?.` skips that and throws `MissingReferenceException` later.
- Plain C# objects (tweens, lists, delegates) may use `?.`.
- ~45 legacy `HintService.Instance?.` calls exist; convert them when you touch that line, don't sweep unasked.

## Randomness

- Use `RngService.Instance.Random` (`System.Random`, seedable). **Never `UnityEngine.Random`**: runs must be reproducible with a fixed seed and Billiards must stay deterministic.

## Tuning values (I10)

- No gameplay numbers as literals in logic. Add a `GameConfig` field under the right `[Header]`, read it via `GameManager.Instance.GameConfig` in `Start` or at use time.
- The C# default of a new field = the intended live value. The asset is the truth for existing fields (C# defaults are stale for ~20 of them; see `PROJECT.md` §10).
- Per-item data that belongs to an asset (card durations, symbol pools, block HP, Billiards tables) may live on that asset.

## Serialization

- Renaming a serialized field: `[FormerlySerializedAs("oldName")]`, always.
- Enums that are serialized or persisted are **append-only**: never reorder, rename or delete members (`CostModifierService.Target`, `EndReason` — persisted by name in run history —, `HintId`, `CardSO.Rarity`, `CatState`, Billiards enums).
- Prefer `[SerializeField] private` over `public` fields.
- File name = class name for every `MonoBehaviour`/`ScriptableObject`.

## Clock and money

- Clock only through `ClockService.AddSeconds(s, reason)` / `Spend(s, reason)`; money only through `MoneyService.Add(amount, reason)` / `TrySpend(amount, reason)`. Give a meaningful `reason`.
- Only the Meter converts money → time (I3). Nothing random grants clock time (I4). Debuffs never drain the clock (I7).
- Money that isn't income (refunds, stake returns) uses a non-income reason (today: `MoneyService.StakeRefundReason`) so ITRS and run stats ignore it.
- Don't write `ClockService.TickMultiplier` from new code; it is a shared scalar with a known overwrite bug. Wait for (or build) keyed speed modifiers.

## Game over and pausing

- Every new behaviour checks `GameManager.Instance.IsGameOver` and stops acting. Game over freezes `Time.timeScale`, but uGUI still receives clicks.
- A pause is legal only while all gameplay input is blocked (I5). Use `GameManager.PauseGame/ResumeGame` (refcounted) and always release what you take, including in `OnDisable`.
- Use scaled time for gameplay timers so they freeze with the clock; unscaled (`SetUpdate(true)`, `WaitForSecondsRealtime`) only for UI that must animate during a freeze.

## Input

- Desk objects: `DeskObject` + `PointerReceiver` on layer 3 `DeskObject` with a 2D collider. `SetInteractable(false, HintId)` gives the player a reason toast.
- A panel that should block desk input gets an `InputBlocker` component. uGUI buttons are not gated by `InputManager.IsInputEnabled`; guard them yourself if they must not act.

## Pooling and pawning

- Pooled objects (`CardItem` ×7, scratch tickets) are never `Instantiate`d or `Destroy`ed.
- `IPawnable.OnPawned()` deactivates (`SetActive(false)`) and releases anything the object holds (pauses, speed keys, `LaptopController.Disabled`, tweens). Never `Destroy()`.

## Events, coroutines, tweens

- Subscribe in `OnEnable`/`Start`, unsubscribe in `OnDisable`/`OnDestroy`. Subscribing in `OnEnable` to another service's event is an `Instance`-in-`OnEnable` read: do it in `Start` instead.
- No coroutines for the core economy tick; short local coroutines are fine. Remember that deactivating a GameObject stops its coroutines: restart them in `OnEnable` if needed.
- Kill your own DOTween tweens in `OnDisable`/`OnDestroy` (`DOTween.Kill(this)` or keep the handle).

## Pure logic

- Put rules and maths in plain C# classes (no `MonoBehaviour`, `Time`, `Physics2D`, `UnityEngine.Random`), driven by the MonoBehaviour and tested headless by an Editor self-test. Views only read state. Examples: `BlackjackGame`, `BilliardsSim`, `BilliardsReferee`.

## Logging

- `Debug.LogWarning` once for a missing reference; no per-frame logging. Dev telemetry behind `Debug.isDebugBuild`.
