using System.Collections.Generic;

/// The player's cart. Pure logic: Ownership decides what can be bought, Checkout prices it.
/// An item can be in the cart once, and only while it is buyable (sold, not owned, not already on the way).
public sealed class ShopCart
{
    readonly List<ItemDef> _items = new List<ItemDef>();

    public IReadOnlyList<ItemDef> Items => _items;
    public int Count => _items.Count;

    public bool Contains(string id)
    {
        foreach (var d in _items) if (d.id == id) return true;
        return false;
    }

    public bool TryAdd(ItemDef def, Ownership ownership)
    {
        if (def == null || Contains(def.id) || !ownership.CanBuy(def.id)) return false;
        _items.Add(def);
        return true;
    }

    public bool Remove(string id)
    {
        for (int i = 0; i < _items.Count; i++)
            if (_items[i].id == id) { _items.RemoveAt(i); return true; }
        return false;
    }

    public void Clear() => _items.Clear();

    /// No coupon (Ari, Q3). leaveOpen = "Leave it opened & ready": +fee, and the parcel skips the unboxing.
    public CheckoutQuote Quote(Shipping shipping, bool leaveOpen, ShopConfig config) => Checkout.Quote(_items, shipping, leaveOpen, false, config);
}
