using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The shop's run state and rules: who owns what (Ownership), which app icons are hidden (IconGate), the cart, and what
/// is in the post (DeliveryScheduler). Lives on MANAGERS/Services. ShopItemBindings register with it from their Awake;
/// because that can run before this Awake, the static Register queues until the service exists.
///
/// Buying (TryBuy): the money is taken at once (reason "shop"). A license arrives instantly as an Inbox message with an
/// Activate button; activating it shows the app's icon. A physical item ships (Standard / Express) and appears on the
/// desk when the order arrives. Shipping counts only while the clock runs.
/// </summary>
public class ShopService : MonoBehaviour
{
    public static ShopService Instance { get; private set; }

    [SerializeField] private ItemCatalogSO catalog;

    public ItemCatalogSO Catalog => catalog;
    public Ownership Ownership { get; private set; }
    public IconGate IconGate { get; } = new IconGate();
    public DeliveryScheduler Delivery { get; } = new DeliveryScheduler();
    public ShopCart Cart { get; } = new ShopCart();
    public ShopConfig Config { get; private set; }
    public Shipping Shipping { get; private set; } = Shipping.Standard;
    public IReadOnlyList<ShopItemBinding> Bindings => _bindings;
    /// <summary>Bumped on every change the Shop app shows (cart, shipping, ownership, orders), so the view refreshes only when needed.</summary>
    public int Version { get; private set; }

    private static readonly List<ShopItemBinding> Pending = new List<ShopItemBinding>();
    private readonly List<ShopItemBinding> _bindings = new List<ShopItemBinding>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Hook()
    {
        Pending.Clear();
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode) => Pending.Clear();

    public static void Register(ShopItemBinding binding)
    {
        if (Instance != null) Instance._bindings.Add(binding);
        else Pending.Add(binding);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (catalog == null)
        {
            Debug.LogWarning("[Shop] No ItemCatalog assigned on ShopService: the shop has no items.");
            Ownership = new Ownership(new List<ItemDef>());
        }
        else
        {
            try { Ownership = new Ownership(catalog.BuildDefs()); }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning("[Shop] " + e.Message);
                Ownership = new Ownership(new List<ItemDef>());
            }
        }

        _bindings.AddRange(Pending);
        Pending.Clear();
    }

    private void Start()
    {
        var g = GameManager.Instance.GameConfig;
        Config = new ShopConfig
        {
            standardFee = g.shopStandardFee, standardSeconds = g.shopStandardSeconds,
            expressFee = g.shopExpressFee, expressSeconds = g.shopExpressSeconds,
            leaveOpenFee = g.shopLeaveOpenFee, couponRate = g.shopCouponRate,
        };

        Ownership.Changed += HandleOwnershipChanged;
        Delivery.Arrived += HandleArrived;
        IconGate.VisibilityChanged += HandleIconVisibility;

        // Every license app starts hidden until the license is owned. (The Laptop icons listen through DesktopIconView,
        // the phone icons are polled by PhoneHome.)
        if (catalog != null)
            foreach (var item in catalog.items)
                if (item != null && item.kind == ItemKind.Digital && !string.IsNullOrEmpty(item.appName))
                    IconGate.SetLicensed(item.appName, Ownership.Has(item.id) && Ownership.IsOwned(item.id));

        ReportProblems();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;
        Delivery.Tick(ClockService.Instance.GameDelta);     // shipping counts only while the clock runs
    }

    // ---- Cart and checkout ----

    public bool AddToCart(string id)
    {
        if (!Ownership.Has(id) || !Cart.TryAdd(Ownership.Def(id), Ownership)) return false;
        Version++;
        return true;
    }

    public bool RemoveFromCart(string id)
    {
        if (!Cart.Remove(id)) return false;
        Version++;
        return true;
    }

    public void SetShipping(Shipping shipping)
    {
        if (Shipping == shipping) return;
        Shipping = shipping;
        Version++;
    }

    public CheckoutQuote Quote() => Cart.Quote(Shipping, Config);

    /// <summary>Buys the cart. False (nothing changes) when the cart is empty or the player cannot afford the total.</summary>
    public bool TryBuy()
    {
        if (Cart.Count == 0 || Config == null || GameManager.Instance.IsGameOver) return false;

        var quote = Quote();
        if (!MoneyService.Instance.TrySpend(quote.total, "shop"))
        {
            if (HintService.Instance != null) HintService.Instance.Show(HintId.NotEnoughMoney);
            return false;
        }

        var physical = new List<string>();
        var phone = PhoneController.Instance;
        foreach (var def in Cart.Items)
        {
            Ownership.MarkOrdered(def.id, quote.pricePaid[def.id]);
            if (def.kind == ItemKind.Digital)
            {
                if (phone != null)
                    phone.Inbox.Add(MessageKind.License, $"Your {def.displayName} license",
                        "Thank you for your order. Press Activate to unlock the app.", "license:" + def.id);
            }
            else physical.Add(def.id);
        }
        if (physical.Count > 0) Delivery.Ship(physical, quote.etaSeconds, false);

        Cart.Clear();
        Version++;
        return true;
    }

    /// <summary>The Inbox's Activate button on a license message.</summary>
    public bool ActivateLicense(string itemId)
    {
        if (!Ownership.Has(itemId) || Ownership.Def(itemId).kind != ItemKind.Digital) return false;
        if (Ownership.Of(itemId) != Ownership.Status.Ordered) return false;
        Ownership.MarkDelivered(itemId);     // HandleOwnershipChanged shows the app icon
        return true;
    }

    // ---- Reactions ----

    private void HandleOwnershipChanged(string id, Ownership.Status status)
    {
        Version++;
        if (status != Ownership.Status.Owned || catalog == null) return;
        var item = catalog.Find(id);
        if (item != null && item.kind == ItemKind.Digital && !string.IsNullOrEmpty(item.appName))
            IconGate.SetLicensed(item.appName, true);
    }

    private void HandleIconVisibility(string app, bool visible) => DesktopIconView.SetVisible(app, visible);

    private void HandleArrived(Order order)
    {
        Version++;
        if (order.junk) return;                                 // junk boxes arrive with Batch 3

        var phone = PhoneController.Instance;
        foreach (var id in order.itemIds)
        {
            Ownership.MarkDelivered(id);
            var binding = BindingFor(id);
            if (binding != null)
            {
                binding.gameObject.SetActive(true);
                binding.transform.DOPunchScale(Vector3.one * 0.25f, 0.45f, 6, 0.5f).SetUpdate(true).SetTarget(this);
            }
            if (phone != null && Ownership.Has(id))
                phone.Inbox.Add(MessageKind.Delivery, $"{Ownership.Def(id).displayName} delivered", "It is on your desk.");
        }
        if (HintService.Instance != null) HintService.Instance.ShowOnce(HintId.ShopPurchased);
    }

    // ---- Catalog lookups and checks ----

    /// <summary>The binding for a catalog id, or null.</summary>
    public ShopItemBinding BindingFor(string id)
    {
        foreach (var b in _bindings) if (b != null && b.ItemId == id) return b;
        return null;
    }

    /// <summary>Catalog problems (duplicate ids, a missing or double binding, a pawnable item off layer 3...). Empty when clean.</summary>
    public List<string> FindProblems()
    {
        var defs = new List<ItemDefCheck>();
        if (catalog != null)
        {
            foreach (var item in catalog.items)
            {
                if (item == null) continue;
                var check = new ItemDefCheck
                {
                    id = item.id, kind = item.kind, price = item.price, catalogValue = item.catalogValue,
                    pawnable = item.pawnable, needsBinding = item.sceneObject && item.kind == ItemKind.Physical,
                };
                var b = BindingFor(item.id);
                check.bindingLayer = b != null ? b.gameObject.layer : -1;
                defs.Add(check);
            }
        }
        var ids = new List<string>(_bindings.Count);
        foreach (var b in _bindings) if (b != null) ids.Add(b.ItemId);
        return ShopValidation.Check(defs, ids);
    }

    private void ReportProblems()
    {
        foreach (var p in FindProblems()) Debug.LogWarning("[Shop] " + p);
    }
}
