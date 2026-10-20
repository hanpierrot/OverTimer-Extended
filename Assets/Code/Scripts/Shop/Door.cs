using System;
using System.Collections.Generic;

/// A parcel standing at the door: the physical items of one order, waiting to be unboxed.
public sealed class Parcel
{
    public int orderId;
    public List<string> itemIds = new List<string>();
    public int clicksLeft;      // box clicks until it springs open
}

/// What is at the door: a tower of parcels (at most `capacity`; later arrivals wait their turn, nothing is lost) and
/// the unboxing counter. Pure logic, one instance per run (owned by DoorController).
public sealed class DoorDepot
{
    readonly int _capacity, _clicksToOpen;
    readonly List<Parcel> _tower = new List<Parcel>();
    readonly Queue<Parcel> _waiting = new Queue<Parcel>();

    public DoorDepot(int capacity, int clicksToOpen)
    {
        _capacity = Math.Max(1, capacity);
        _clicksToOpen = Math.Max(1, clicksToOpen);
    }

    /// Bottom first.
    public IReadOnlyList<Parcel> Tower => _tower;
    public int Waiting => _waiting.Count;
    public int Capacity => _capacity;

    /// Position in the tower (0 = bottom), or -1.
    public int IndexOf(Parcel parcel) => _tower.IndexOf(parcel);

    public event Action<Parcel> Added;      // a parcel joined the tower
    public event Action<Parcel> Opened;     // a parcel sprang open and left the tower

    /// Puts the order's physical items at the door. Returns the parcel (it may be waiting for a free spot).
    public Parcel Receive(int orderId, IEnumerable<string> itemIds)
    {
        var p = new Parcel { orderId = orderId, clicksLeft = _clicksToOpen };
        p.itemIds.AddRange(itemIds);
        if (_tower.Count < _capacity) Place(p); else _waiting.Enqueue(p);
        return p;
    }

    /// One click on a parcel. True when this click opened it.
    public bool Click(Parcel parcel)
    {
        if (parcel == null || !_tower.Contains(parcel)) return false;
        parcel.clicksLeft--;
        if (parcel.clicksLeft > 0) return false;

        _tower.Remove(parcel);
        Opened?.Invoke(parcel);
        if (_waiting.Count > 0) Place(_waiting.Dequeue());      // the box is recycled for the next delivery
        return true;
    }

    void Place(Parcel p) { _tower.Add(p); Added?.Invoke(p); }
}
