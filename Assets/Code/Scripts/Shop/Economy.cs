using System;
using System.Collections.Generic;

public enum ItemKind { Physical, Digital }

/// One row of the catalog. In Unity this is a ScriptableObject (ItemDefSO); this plain
/// class is what the pure logic consumes so it can be tested without assets.
public sealed class ItemDef
{
    public string id;            // stable key: "power_bank", "license_blackjack" ... NEVER rename
    public string displayName;
    public ItemKind kind;
    public int price;            // shop price; 0 for starting items that are never sold
    public int catalogValue;     // pawn basis for starting items that ARE pawnable (Laptop: 150)
    public bool startsOwned;
    public bool pawnable;
    public bool sold;            // appears in the shop
}

public static class PawnMath
{
    public const float PawnRate = 0.8f;
    static readonly (float afterSeconds, float mult)[] Steps = { (0f, 1.10f), (30f, 1.15f), (60f, 1.20f), (90f, 1.25f) };

    /// What pawning pays. basis = price actually paid (after coupons), or catalogValue for a
    /// pawnable starting item. 0 = not pawnable.
    public static int PawnValue(ItemDef def, int pricePaid, bool owned)
    {
        if (!owned || !def.pawnable || def.kind == ItemKind.Digital) return 0;
        int basis = pricePaid > 0 ? pricePaid : (def.startsOwned ? def.catalogValue : 0);
        return (int)Math.Floor(basis * PawnRate);
    }

    public static float BuybackMultiplier(float secondsSincePawn)
    {
        float m = Steps[0].mult;
        foreach (var (after, mult) in Steps) if (secondsSincePawn >= after) m = mult;
        return m;
    }

    /// basis is the same value PawnValue used. Rounded UP so a pawn -> buyback round trip always loses.
    public static int BuybackPrice(int basis, float secondsSincePawn)
        => (int)Math.Ceiling(Math.Round(basis * BuybackMultiplier(secondsSincePawn), 4));

    public static float NextStepIn(float secondsSincePawn)
    {
        foreach (var (after, _) in Steps) if (after > secondsSincePawn) return after - secondsSincePawn;
        return -1f;    // already capped
    }
}

public enum Shipping { Standard, Express }

public sealed class CheckoutQuote
{
    public int subtotal, discount, shipping, leaveOpenFee, total;
    public float etaSeconds;                                   // 0 = digital-only order (instant)
    public readonly Dictionary<string, int> pricePaid = new Dictionary<string, int>();   // per item, after discount
    public bool hasPhysical;
}

public sealed class ShopConfig
{
    public int   standardFee = 0;   public float standardSeconds = 20f;
    public int   expressFee  = 8;   public float expressSeconds  = 5f;
    public int   leaveOpenFee = 3;
    public float couponRate = 0.2f;     // client-call coupon: 20% off the item subtotal of one order
}

public static class Checkout
{
    /// Fees are per ORDER, and only charged if the order has a physical item.
    /// The coupon discount is spread across items in proportion to price, rounded so the per-item
    /// prices add up exactly to subtotal - discount (pawn value uses these).
    public static CheckoutQuote Quote(IReadOnlyList<ItemDef> cart, Shipping ship, bool leaveOpen, bool useCoupon, ShopConfig c)
    {
        var q = new CheckoutQuote();
        foreach (var d in cart) { q.subtotal += d.price; if (d.kind == ItemKind.Physical) q.hasPhysical = true; }
        q.discount = useCoupon ? (int)Math.Floor(q.subtotal * c.couponRate) : 0;

        int remaining = q.subtotal - q.discount, allocated = 0;
        for (int i = 0; i < cart.Count; i++)
        {
            var d = cart[i];
            int paid = i == cart.Count - 1
                ? remaining - allocated
                : (q.subtotal == 0 ? 0 : (int)Math.Floor((double)d.price * remaining / q.subtotal));
            allocated += paid;
            q.pricePaid[d.id] = paid;
        }

        if (q.hasPhysical)
        {
            q.shipping = ship == Shipping.Express ? c.expressFee : c.standardFee;
            q.etaSeconds = ship == Shipping.Express ? c.expressSeconds : c.standardSeconds;
            q.leaveOpenFee = leaveOpen ? c.leaveOpenFee : 0;
        }
        q.total = q.subtotal - q.discount + q.shipping + q.leaveOpenFee;
        return q;
    }
}

/// Run-time ownership of catalog items. One instance per run (scene object), never static.
public sealed class Ownership
{
    public enum Status { NotOwned, Ordered, Owned, Pawned }
    sealed class Entry { public Status status; public int pricePaid; }

    readonly Dictionary<string, ItemDef> _defs = new Dictionary<string, ItemDef>();
    readonly Dictionary<string, Entry> _state = new Dictionary<string, Entry>();

    public event Action<string, Status> Changed;

    public Ownership(IEnumerable<ItemDef> catalog)
    {
        foreach (var d in catalog)
        {
            if (_defs.ContainsKey(d.id)) throw new ArgumentException($"duplicate item id '{d.id}'");
            _defs[d.id] = d;
            _state[d.id] = new Entry { status = d.startsOwned ? Status.Owned : Status.NotOwned };
        }
    }

    public IEnumerable<ItemDef> Defs => _defs.Values;
    public bool Has(string id) => id != null && _defs.ContainsKey(id);
    public ItemDef Def(string id) => _defs[id];
    public Status Of(string id) => _state[id].status;
    public int PricePaid(string id) => _state[id].pricePaid;
    public bool IsOwned(string id) => _state[id].status == Status.Owned;

    /// Only items that are sold, not owned and not already on the way.
    public bool CanBuy(string id) => _defs[id].sold && _state[id].status == Status.NotOwned;

    public void MarkOrdered(string id, int pricePaid) { var e = _state[id]; e.pricePaid = pricePaid; Set(id, Status.Ordered); }
    public void MarkDelivered(string id) => Set(id, Status.Owned);

    /// Returns what the Printer pays. Batch 2: pawned items stay Pawned (shown "Sold") until Batch 4.
    public int MarkPawned(string id)
    {
        int v = PawnMath.PawnValue(_defs[id], _state[id].pricePaid, IsOwned(id));
        if (v <= 0) return 0;
        Set(id, Status.Pawned);
        return v;
    }

    /// Batch 4 buyback: back to Ordered (it ships like a purchase); keeps the original basis.
    public void MarkBoughtBack(string id) => Set(id, Status.Ordered);

    void Set(string id, Status s) { _state[id].status = s; Changed?.Invoke(id, s); }
}

/// Why an app icon is hidden. Two independent reasons now exist (no license; DisableAppIcon card),
/// so a single bool would let a card's OnRemoved re-show an app the player never bought.
[Flags] public enum HideReason { None = 0, Unlicensed = 1, CardDisabled = 2 }

public sealed class IconGate
{
    // App names are compared ignoring case: the "Close The Shop" card says "Shop", the phone's icon id is "shop".
    readonly Dictionary<string, HideReason> _reasons = new Dictionary<string, HideReason>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, int> _cardHolds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);   // two "No More Mining" cards can overlap
    public event Action<string, bool> VisibilityChanged;

    public bool IsVisible(string app) => !_reasons.TryGetValue(app, out var r) || r == HideReason.None;

    public void SetLicensed(string app, bool licensed) => Apply(app, HideReason.Unlicensed, !licensed);

    public void CardHide(string app)
    {
        _cardHolds.TryGetValue(app, out int n); _cardHolds[app] = n + 1;
        Apply(app, HideReason.CardDisabled, true);
    }

    public void CardRelease(string app)
    {
        _cardHolds.TryGetValue(app, out int n); n = Math.Max(0, n - 1); _cardHolds[app] = n;
        if (n == 0) Apply(app, HideReason.CardDisabled, false);
    }

    void Apply(string app, HideReason flag, bool on)
    {
        bool before = IsVisible(app);
        _reasons.TryGetValue(app, out var r);
        _reasons[app] = on ? r | flag : r & ~flag;
        bool after = IsVisible(app);
        if (before != after) VisibilityChanged?.Invoke(app, after);
    }
}

/// Catalog sanity checks, shared by the self-tests and the scene-start check in ShopService.
/// bindings = the item id of every ShopItemBinding found in the scene (one entry per binding).
public static class ShopValidation
{
    public static List<string> Check(IReadOnlyList<ItemDefCheck> defs, IReadOnlyList<string> bindings)
    {
        var problems = new List<string>();
        var seen = new HashSet<string>();
        foreach (var d in defs)
        {
            if (string.IsNullOrEmpty(d.id)) { problems.Add("an item has an empty id"); continue; }
            if (!seen.Add(d.id)) problems.Add($"duplicate item id '{d.id}'");
            if (d.price < 0 || d.catalogValue < 0) problems.Add($"'{d.id}' has a negative price or value");
            if (d.kind == ItemKind.Digital && d.pawnable) problems.Add($"'{d.id}' is digital but pawnable");

            int count = 0;
            foreach (var b in bindings) if (b == d.id) count++;
            if (d.needsBinding && count != 1) problems.Add($"'{d.id}' needs exactly one ShopItemBinding in the scene (found {count})");
            if (!d.needsBinding && count > 0) problems.Add($"'{d.id}' is not a scene item but has a ShopItemBinding");
            if (d.pawnable && d.needsBinding && count == 1 && d.bindingLayer != 3) problems.Add($"'{d.id}' is pawnable but its object is on layer {d.bindingLayer}, not 3 (DeskObject)");
        }
        foreach (var b in bindings)
        {
            bool known = false;
            foreach (var d in defs) if (d.id == b) { known = true; break; }
            if (!known) problems.Add($"a ShopItemBinding uses unknown item id '{b}'");
        }
        return problems;
    }
}

/// What ShopValidation needs to know about one catalog row.
public sealed class ItemDefCheck
{
    public string id;
    public ItemKind kind;
    public int price, catalogValue;
    public bool pawnable;
    public bool needsBinding;    // a physical item that is an object in the scene
    public int bindingLayer;     // layer of that object (filled in by the caller; ignored when there is no binding)
}
