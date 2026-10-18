using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The shop's run state: who owns what (Ownership), which app icons are hidden (IconGate) and what is in the post
/// (DeliveryScheduler). Lives on MANAGERS/Services. ShopItemBindings register with it from their Awake; because that can
/// run before this Awake, the static Register queues until the service exists.
/// Phase 2.1 only holds the state and checks the catalog; the Shop app, checkout and delivery wiring arrive in 2.2.
/// </summary>
public class ShopService : MonoBehaviour
{
    public static ShopService Instance { get; private set; }

    [SerializeField] private ItemCatalogSO catalog;

    public ItemCatalogSO Catalog => catalog;
    public Ownership Ownership { get; private set; }
    public IconGate IconGate { get; } = new IconGate();
    public DeliveryScheduler Delivery { get; } = new DeliveryScheduler();
    public ShopConfig Config { get; private set; }
    public IReadOnlyList<ShopItemBinding> Bindings => _bindings;

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
        ReportProblems();
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;
        Delivery.Tick(ClockService.Instance.GameDelta);     // shipping counts only while the clock runs
    }

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
