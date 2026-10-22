using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Shop app window, laid out like an inventory (the Backpack pieces of Premade Menus):
///   Left   - the shop's items as rows (name, price or status on the right); click one to select it (orange row).
///   Right  - Browse: an item card (picture box, name / price / status fields, description) with ADD / DROP and CART.
///            Cart: a receipt on a pink pane (the items, the total), two shipping tiles (STD / EXP, the chosen one is
///            orange), BACK and BUY. The orders on the way (with a live countdown) show on the receipt.
/// While the cart is open a click on a row takes that item out of the cart. It only reads ShopService.
/// </summary>
public class ShopScreen : MonoBehaviour
{
    [Header("Item list")]
    [SerializeField] private PhoneListView list;

    [Header("Browse card")]
    [SerializeField] private GameObject browseGroup;
    [SerializeField] private Image itemPicture;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text priceLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private Button addButton;
    [SerializeField] private TMP_Text addLabel;
    [SerializeField] private Button cartButton;
    [SerializeField] private TMP_Text cartLabel;

    [Header("Cart receipt")]
    [SerializeField] private GameObject cartGroup;
    [SerializeField] private TMP_Text receiptLabel;
    [SerializeField] private TMP_Text summaryLabel;
    [SerializeField] private Button standardTile;
    [SerializeField] private Image standardTileImage;
    [SerializeField] private Button expressTile;
    [SerializeField] private Image expressTileImage;
    [SerializeField] private Button openTile;
    [SerializeField] private Image openTileImage;
    [SerializeField] private Sprite tileNormal;
    [SerializeField] private Sprite tileSelected;
    [SerializeField] private Button backButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyLabel;

    [Header("Tabs (SHOP / PAWNED)")]
    [SerializeField] private Button shopTabButton;
    [SerializeField] private Image shopTabImage;
    [SerializeField] private Button pawnedTabButton;
    [SerializeField] private Image pawnedTabImage;
    [SerializeField] private TMP_Text pawnedTabLabel;

    private static readonly Color TabDim = new Color(0.65f, 0.65f, 0.65f, 1f);
    private static readonly Color TabSelectedText = new Color(0.16f, 0.16f, 0.35f, 1f);

    private bool _cartView;
    private bool _pawnedTab;       // the second tab: what the player pawned, with the live buyback price
    private bool _buybackView;      // the receipt for buying the selected pawned item back (reuses the cart receipt)
    private string _selectedId;
    private int _signature = -1;

    private readonly List<PhoneListRowData> _rows = new List<PhoneListRowData>();
    private readonly List<string> _rowIds = new List<string>();
    private readonly StringBuilder _text = new StringBuilder();

    private void Awake()
    {
        list.Clicked += HandleRowClicked;
        addButton.onClick.AddListener(() =>
        {
            var shop = ShopService.Instance;
            if (shop == null || _selectedId == null) return;
            if (_pawnedTab) { _buybackView = true; _signature = -1; return; }        // BUY BACK: on to the receipt
            if (shop.Cart.Contains(_selectedId)) shop.RemoveFromCart(_selectedId); else shop.AddToCart(_selectedId);
        });
        cartButton.onClick.AddListener(() => { _cartView = true; _signature = -1; });
        backButton.onClick.AddListener(() => { _cartView = false; _buybackView = false; _signature = -1; });
        if (shopTabButton != null) shopTabButton.onClick.AddListener(() => SelectTab(false));
        if (pawnedTabButton != null) pawnedTabButton.onClick.AddListener(() => SelectTab(true));
        standardTile.onClick.AddListener(() => { if (ShopService.Instance != null) ShopService.Instance.SetShipping(Shipping.Standard); });
        expressTile.onClick.AddListener(() => { if (ShopService.Instance != null) ShopService.Instance.SetShipping(Shipping.Express); });
        openTile.onClick.AddListener(() => { if (ShopService.Instance != null) ShopService.Instance.SetLeaveOpen(!ShopService.Instance.LeaveOpen); });
        buyButton.onClick.AddListener(() =>
        {
            var shop = ShopService.Instance;
            if (shop == null) return;
            if (_buybackView)
            {
                if (shop.TryBuyBack(_selectedId)) { _buybackView = false; _selectedId = null; _signature = -1; }
            }
            else if (shop.TryBuy()) { _cartView = false; _signature = -1; }
        });
    }

    private void OnEnable() => _signature = -1;

    private void SelectTab(bool pawned)
    {
        if (_pawnedTab == pawned && !_cartView && !_buybackView) return;
        _pawnedTab = pawned;
        _cartView = false;
        _buybackView = false;
        _selectedId = null;
        _signature = -1;
    }

    private void Update()
    {
        var shop = ShopService.Instance;
        if (shop == null || shop.Config == null) return;

        int sig = shop.Version * 31 + (_cartView ? 1 : 0) + (_pawnedTab ? 2 : 0) + (_buybackView ? 4 : 0)
                  + (_selectedId != null ? _selectedId.GetHashCode() : 0) * 7 + OrderSeconds(shop) + (_pawnedTab ? PawnedSeconds(shop) : 0);
        if (sig == _signature) return;
        _signature = sig;

        UpdateTabs();
        bool receipt = _cartView || _buybackView;
        PhoneUi.SetActive(browseGroup, !receipt);
        PhoneUi.SetActive(cartGroup, receipt);
        if (_buybackView) ShowBuybackReceipt(shop);
        else if (_cartView) ShowCart(shop);
        else if (_pawnedTab) ShowPawned(shop);
        else ShowBrowse(shop);
    }

    private void UpdateTabs()
    {
        // The chosen tab is full colour, the other is dimmed (the tabs use the button sprite, not the 70x70 tile art).
        if (shopTabImage != null) shopTabImage.color = _pawnedTab ? TabDim : Color.white;
        if (pawnedTabImage != null) pawnedTabImage.color = _pawnedTab ? Color.white : TabDim;
        TintTabLabel(shopTabButton, !_pawnedTab);
        TintTabLabel(pawnedTabButton, _pawnedTab);
    }

    private static void TintTabLabel(Button tab, bool selected)
    {
        if (tab == null) return;
        var label = tab.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.color = selected ? TabSelectedText : Color.white;
    }

    // Whole seconds to the next price step of every pawned item, so the "goes up in" text redraws once a second.
    private static int PawnedSeconds(ShopService shop)
    {
        int n = 0;
        foreach (var e in shop.Ledger.All) n += Mathf.CeilToInt(shop.BuybackNextStepIn(e.itemId)) * 17 + shop.BuybackPrice(e.itemId);
        return n;
    }

    // Whole seconds left on the orders, so the countdown redraws once a second.
    private static int OrderSeconds(ShopService shop)
    {
        int n = 0;
        foreach (var o in shop.Delivery.InTransit) n += Mathf.CeilToInt(o.secondsLeft) * 13 + o.id;
        return n;
    }

    // ---- Browse ----

    private void ShowBrowse(ShopService shop)
    {
        _rows.Clear(); _rowIds.Clear();
        int selectedIndex = -1;
        foreach (var def in shop.Ownership.Defs)
        {
            if (!def.sold) continue;
            if (def.id == _selectedId) selectedIndex = _rows.Count;
            _rows.Add(new PhoneListRowData { title = def.displayName, detail = StatusOf(shop, def) });
            _rowIds.Add(def.id);
        }
        list.SetRows(_rows, selectedIndex);

        PhoneUi.SetActive(cartButton.gameObject, true);
        PhoneUi.SetText(cartLabel, shop.Cart.Count > 0 ? "CART " + shop.Cart.Count : "CART");

        if (selectedIndex < 0)
        {
            SetPicture(null);
            PhoneUi.SetText(nameLabel, "SHOP");
            PhoneUi.SetText(priceLabel, "");
            PhoneUi.SetText(statusLabel, "");
            _text.Clear().Append("Pick an item on the left.");
            AppendOrders(shop);
            PhoneUi.SetText(descriptionLabel, _text.ToString());
            PhoneUi.SetActive(addButton.gameObject, false);
            return;
        }

        var item = shop.Ownership.Def(_selectedId);
        var so = shop.Catalog != null ? shop.Catalog.Find(_selectedId) : null;
        SetPicture(so != null ? so.icon : null);
        PhoneUi.SetText(nameLabel, item.displayName);
        PhoneUi.SetText(priceLabel, "$" + item.price);
        PhoneUi.SetText(statusLabel, StatusText(shop, item));
        PhoneUi.SetText(descriptionLabel, (item.kind == ItemKind.Digital ? "License. " : "") + (so != null ? so.description : ""));

        bool inCart = shop.Cart.Contains(item.id);
        PhoneUi.SetActive(addButton.gameObject, true);
        PhoneUi.SetText(addLabel, inCart ? "DROP" : "ADD");
        addButton.interactable = inCart || shop.Ownership.CanBuy(item.id);
    }

    private void SetPicture(Sprite sprite)
    {
        if (itemPicture == null) return;
        itemPicture.sprite = sprite;
        itemPicture.enabled = sprite != null;
    }

    // ---- Cart ----

    private void ShowCart(ShopService shop)
    {
        _rows.Clear(); _rowIds.Clear();
        foreach (var def in shop.Cart.Items)
        {
            _rows.Add(new PhoneListRowData { title = def.displayName, detail = "$" + def.price });
            _rowIds.Add(def.id);
        }
        list.SetRows(_rows, -1);

        var quote = shop.Quote();
        bool any = shop.Cart.Count > 0;

        _text.Clear();
        if (!any) _text.Append("Your cart is empty.");
        else
        {
            foreach (var def in shop.Cart.Items) _text.Append(def.displayName).Append("  $").Append(def.price).Append('\n');
            if (quote.hasPhysical)
                _text.Append(shop.Shipping == Shipping.Express ? "Express $" : "Standard $").Append(quote.shipping)
                     .Append("  ").Append(Mathf.CeilToInt(quote.etaSeconds)).Append('s');
            if (shop.LeaveOpen && quote.hasPhysical) _text.Append("\nOpened $").Append(quote.leaveOpenFee);
            _text.Append("\nTOTAL $").Append(quote.total);
        }
        AppendOrders(shop);
        PhoneUi.SetText(receiptLabel, _text.ToString());

        SetShippingTiles(shop, quote.hasPhysical);

        PhoneUi.SetActive(buyButton.gameObject, any);
        PhoneUi.SetText(buyLabel, "BUY $" + quote.total);
    }

    private void SetShippingTiles(ShopService shop, bool visible)
    {
        bool express = shop.Shipping == Shipping.Express;
        standardTileImage.sprite = express ? tileNormal : tileSelected;
        expressTileImage.sprite = express ? tileSelected : tileNormal;
        PhoneUi.SetActive(standardTile.gameObject, visible);
        PhoneUi.SetActive(expressTile.gameObject, visible);
        openTileImage.sprite = shop.LeaveOpen ? tileSelected : tileNormal;
        PhoneUi.SetActive(openTile.gameObject, visible);

        if (summaryLabel != null) PhoneUi.SetText(summaryLabel, "");      // the total is on the receipt now (three tiles fill the row)
    }

    // ---- Pawned tab ----

    private void ShowPawned(ShopService shop)
    {
        _rows.Clear(); _rowIds.Clear();
        int selectedIndex = -1;
        foreach (var e in shop.Ledger.All)
        {
            if (e.itemId == _selectedId) selectedIndex = _rows.Count;
            _rows.Add(new PhoneListRowData { title = shop.Ownership.Def(e.itemId).displayName, detail = "$" + shop.BuybackPrice(e.itemId) });
            _rowIds.Add(e.itemId);
        }
        list.SetRows(_rows, selectedIndex);

        PhoneUi.SetActive(cartButton.gameObject, false);       // buybacks are bought one at a time, from their own receipt
        if (selectedIndex < 0)
        {
            SetPicture(null);
            PhoneUi.SetText(nameLabel, "PAWNED");
            PhoneUi.SetText(priceLabel, "");
            PhoneUi.SetText(statusLabel, "");
            _text.Clear().Append(_rows.Count == 0 ? "Nothing pawned." : "Pick an item on the left.");
            AppendOrders(shop);
            PhoneUi.SetText(descriptionLabel, _text.ToString());
            PhoneUi.SetActive(addButton.gameObject, false);
            return;
        }

        var item = shop.Ownership.Def(_selectedId);
        var so = shop.Catalog != null ? shop.Catalog.Find(_selectedId) : null;
        SetPicture(so != null ? so.icon : null);
        PhoneUi.SetText(nameLabel, item.displayName);
        PhoneUi.SetText(priceLabel, "$" + shop.BuybackPrice(_selectedId));
        float next = shop.BuybackNextStepIn(_selectedId);
        PhoneUi.SetText(statusLabel, next < 0f ? "Price is final" : "Goes up in " + Mathf.CeilToInt(next) + "s");
        PhoneUi.SetText(descriptionLabel, so != null ? so.description : "");
        PhoneUi.SetActive(addButton.gameObject, true);
        PhoneUi.SetText(addLabel, "BUY BACK");
        addButton.interactable = true;
    }

    private void ShowBuybackReceipt(ShopService shop)
    {
        var quote = shop.QuoteBuyback(_selectedId);
        _rows.Clear(); _rowIds.Clear();
        if (quote == null) { _buybackView = false; _signature = -1; return; }      // no longer pawned: back to the list

        var def = shop.Ownership.Def(_selectedId);
        _rows.Add(new PhoneListRowData { title = def.displayName, detail = "$" + quote.subtotal });
        _rowIds.Add(def.id);
        list.SetRows(_rows, -1);

        _text.Clear().Append(def.displayName).Append(" (buy back)  $").Append(quote.subtotal).Append('\n')
             .Append(shop.Shipping == Shipping.Express ? "Express $" : "Standard $").Append(quote.shipping)
             .Append("  ").Append(Mathf.CeilToInt(quote.etaSeconds)).Append('s');
        if (shop.LeaveOpen) _text.Append("\nOpened $").Append(quote.leaveOpenFee);
        float next = shop.BuybackNextStepIn(_selectedId);
        if (next >= 0f) _text.Append("\nPrice goes up in ").Append(Mathf.CeilToInt(next)).Append('s');
        _text.Append("\nTOTAL $").Append(quote.total);
        AppendOrders(shop);
        PhoneUi.SetText(receiptLabel, _text.ToString());

        SetShippingTiles(shop, true);
        PhoneUi.SetActive(buyButton.gameObject, true);
        PhoneUi.SetText(buyLabel, "BUY $" + quote.total);
    }

    private void AppendOrders(ShopService shop)
    {
        bool header = false;
        foreach (var o in shop.Delivery.InTransit)
        {
            if (o.junk) continue;
            if (!header) { _text.Append("\n\nON THE WAY"); header = true; }
            _text.Append('\n');
            for (int i = 0; i < o.itemIds.Count; i++)
            {
                if (i > 0) _text.Append(", ");
                _text.Append(shop.Ownership.Has(o.itemIds[i]) ? shop.Ownership.Def(o.itemIds[i]).displayName : o.itemIds[i]);
            }
            _text.Append("  ").Append(Mathf.CeilToInt(o.secondsLeft)).Append('s');
        }
    }

    private static string StatusOf(ShopService shop, ItemDef def)
    {
        if (shop.Cart.Contains(def.id)) return "IN CART";
        switch (shop.Ownership.Of(def.id))
        {
            case Ownership.Status.Owned: return "OWNED";
            case Ownership.Status.Ordered: return "ON WAY";
            case Ownership.Status.Pawned: return shop.Ledger.TryGet(def.id, out _) ? "PAWNED" : "SOLD";
            default: return "$" + def.price;
        }
    }

    private static string StatusText(ShopService shop, ItemDef def)
    {
        if (shop.Cart.Contains(def.id)) return "In cart";
        switch (shop.Ownership.Of(def.id))
        {
            case Ownership.Status.Owned: return "Owned";
            case Ownership.Status.Ordered: return def.kind == ItemKind.Digital ? "Activate it" : "On the way";
            case Ownership.Status.Pawned: return shop.Ledger.TryGet(def.id, out _) ? "Pawned (see PAWNED)" : "Sold";
            default: return "For sale";
        }
    }

    private void HandleRowClicked(int index)
    {
        var shop = ShopService.Instance;
        if (shop == null || index < 0 || index >= _rowIds.Count) return;
        if (_buybackView) return;
        if (_cartView) shop.RemoveFromCart(_rowIds[index]);
        else _selectedId = _rowIds[index];
        _signature = -1;
    }
}
