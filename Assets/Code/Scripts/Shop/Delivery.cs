using System;
using System.Collections.Generic;

public sealed class Order
{
    public int id;
    public List<string> itemIds = new List<string>();   // physical items only (digital ones were delivered instantly)
    public bool leaveOpen;
    public bool junk;            // empty box: rings the bell, holds nothing
    public float secondsLeft;
    public bool arrived;
}

/// Shipping timers. Counts only while the clock runs (pass gameDelta). One scene instance per run.
/// Batch 2: Arrived -> items appear on the desk directly (no mailbox yet).
/// Batch 3: Arrived -> a box in the mailbox, unless leaveOpen (then straight to the desk, opened).
public sealed class DeliveryScheduler
{
    readonly List<Order> _inTransit = new List<Order>();
    int _nextId = 1;

    public IReadOnlyList<Order> InTransit => _inTransit;
    public event Action<Order> Arrived;      // doorbell + mailbox (Batch 3)

    public Order Ship(IEnumerable<string> items, float eta, bool leaveOpen)
    {
        var o = new Order { id = _nextId++, leaveOpen = leaveOpen, secondsLeft = eta };
        o.itemIds.AddRange(items);
        _inTransit.Add(o);
        return o;
    }

    public Order ShipJunk(float eta) => Add(new Order { id = _nextId++, junk = true, secondsLeft = eta });

    Order Add(Order o) { _inTransit.Add(o); return o; }

    public void Tick(float gameDelta)
    {
        if (gameDelta <= 0f) return;
        // collect first, then raise: a handler may Ship() again while we iterate
        List<Order> done = null;
        foreach (var o in _inTransit)
        {
            o.secondsLeft -= gameDelta;
            if (o.secondsLeft <= 0f) { o.arrived = true; (done ??= new List<Order>()).Add(o); }
        }
        if (done == null) return;
        foreach (var o in done) { _inTransit.Remove(o); Arrived?.Invoke(o); }
    }
}

/// Batch 4. Written by Pawn.cs BEFORE OnPawned() runs.
public sealed class PawnLedger
{
    public sealed class Entry { public string itemId; public int basis; public float pawnedAtGameTime; }
    readonly Dictionary<string, Entry> _byItem = new Dictionary<string, Entry>();

    public IEnumerable<Entry> All => _byItem.Values;

    public void Record(string itemId, int basis, float gameTime) =>
        _byItem[itemId] = new Entry { itemId = itemId, basis = basis, pawnedAtGameTime = gameTime };

    public bool TryGet(string itemId, out Entry e) => _byItem.TryGetValue(itemId, out e);

    public int Price(string itemId, float gameTimeNow) =>
        PawnMath.BuybackPrice(_byItem[itemId].basis, gameTimeNow - _byItem[itemId].pawnedAtGameTime);

    public void Remove(string itemId) => _byItem.Remove(itemId);
}
