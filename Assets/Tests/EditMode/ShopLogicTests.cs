using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Mục 4.5.4, 4.5.5 và TC-CRD-18 của kế hoạch kiểm thử: logic thuần của Shop (Checkout, ShopCart, Ownership,
/// ShopValidation, PawnMath, IconGate).
/// Giá và phí lấy theo DESIGN §5.7, §5.11 và dựng tay trong test, nên test kiểm công thức chứ không ghim giá trị asset;
/// việc asset khớp bảng giá đã có ở PhoneShopSelfTests.ShopCatalogAsset.
/// </summary>
public class ShopLogicTests
{
    private static readonly ItemDef PowerBank = new ItemDef { id = "power_bank", kind = ItemKind.Physical, price = 25, pawnable = true, sold = true };
    private static readonly ItemDef CardBox = new ItemDef { id = "card_box", kind = ItemKind.Physical, price = 40, pawnable = true, sold = true };
    private static readonly ItemDef LicenseBlackjack = new ItemDef { id = "license_blackjack", kind = ItemKind.Digital, price = 60, sold = true };
    private static readonly ItemDef LicenseBilliards = new ItemDef { id = "license_billiards", kind = ItemKind.Digital, price = 60, sold = true };
    private static readonly ItemDef EggTimer = new ItemDef { id = "egg_timer", kind = ItemKind.Physical, startsOwned = true };

    private static ShopConfig Config() => new ShopConfig
    {
        standardFee = 0, standardSeconds = 20f,
        expressFee = 8, expressSeconds = 5f,
        leaveOpenFee = 3,
    };

    private static CheckoutQuote Quote(Shipping ship, bool leaveOpen, params ItemDef[] cart)
        => Checkout.Quote(cart, ship, leaveOpen, false, Config());

    // ---------- Báo giá ----------

    [Test]
    public void TC_SHP_2_BaoGiaStandard()
    {
        var q = Quote(Shipping.Standard, false, PowerBank, CardBox);

        Assert.AreEqual(65, q.subtotal);
        Assert.AreEqual(0, q.shipping);
        Assert.AreEqual(65, q.total);
        Assert.AreEqual(20f, q.etaSeconds);
        Assert.AreEqual(25, q.pricePaid["power_bank"]);
        Assert.AreEqual(40, q.pricePaid["card_box"]);
    }

    [Test]
    public void TC_SHP_3_BaoGiaExpressVaDeMoSan()
    {
        var q = Quote(Shipping.Express, true, PowerBank, CardBox);

        Assert.AreEqual(76, q.total, "65 + 8 phí Express + 3 để mở sẵn");
        Assert.AreEqual(5f, q.etaSeconds);
    }

    [Test]
    public void TC_SHP_4_DonChiCoLicense_KhongTinhPhiGiao()
    {
        var q = Quote(Shipping.Express, true, LicenseBlackjack);

        Assert.AreEqual(60, q.total);
        Assert.AreEqual(0, q.shipping);
        Assert.AreEqual(0, q.leaveOpenFee);
        Assert.AreEqual(0f, q.etaSeconds, "Hàng số đến ngay");
    }

    [Test]
    public void TC_SHP_5_DonTronLicenseVaHangVatLy()
    {
        var q = Quote(Shipping.Express, false, LicenseBilliards, PowerBank);

        Assert.AreEqual(93, q.total, "60 + 25 + 8");
        Assert.AreEqual(5f, q.etaSeconds);
    }

    [Test]
    public void TC_SHP_6_QuyTacThemVaoGio()
    {
        var own = new Ownership(new[] { PowerBank, CardBox, EggTimer });
        var cart = new ShopCart();
        own.MarkOrdered("card_box", 40);

        Assert.IsTrue(cart.TryAdd(PowerBank, own));
        Assert.IsFalse(cart.TryAdd(PowerBank, own), "Một mặt hàng chỉ vào giỏ một lần");
        Assert.IsFalse(cart.TryAdd(EggTimer, own), "Egg Timer không bán");
        Assert.IsFalse(cart.TryAdd(CardBox, own), "Card Box đang trên đường giao");
        Assert.AreEqual(1, cart.Count);
    }

    // ---------- Kiểm tra catalog ----------

    private static List<ItemDefCheck> CleanCatalog() => new List<ItemDefCheck>
    {
        new ItemDefCheck { id = "power_bank", kind = ItemKind.Physical, price = 25, pawnable = true, needsBinding = true, bindingLayer = 3 },
        new ItemDefCheck { id = "bear", kind = ItemKind.Physical, needsBinding = true, bindingLayer = 3 },
        new ItemDefCheck { id = "license_blackjack", kind = ItemKind.Digital, price = 60 },
    };

    private static readonly string[] CleanBindings = { "power_bank", "bear" };

    [Test]
    public void TC_SHP_11_KiemTraCatalog_MoiLoiBaoDungMotLan()
    {
        Assert.IsEmpty(ShopValidation.Check(CleanCatalog(), CleanBindings), "Catalog sạch không có lỗi");

        var dup = CleanCatalog();
        dup.Add(new ItemDefCheck { id = "bear", kind = ItemKind.Physical, needsBinding = true, bindingLayer = 3 });
        Assert.AreEqual(1, ShopValidation.Check(dup, CleanBindings).Count, "(a) trùng id");

        var digital = CleanCatalog();
        digital[2].pawnable = true;
        Assert.AreEqual(1, ShopValidation.Check(digital, CleanBindings).Count, "(b) hàng số cầm đồ được");

        var layer = CleanCatalog();
        layer[0].bindingLayer = 0;
        Assert.AreEqual(1, ShopValidation.Check(layer, CleanBindings).Count, "(c) hàng cầm được không nằm layer 3");

        Assert.AreEqual(1, ShopValidation.Check(CleanCatalog(), new[] { "power_bank", "bear", "lạ" }).Count, "(d) binding dùng id lạ");
    }

    // ---------- Mua lại (Buyback) ----------

    [Test]
    public void TC_BUY_1_BacGiaMuaLaiLaptop()
    {
        const int basis = 135;
        var expected = new (float t, int price)[]
        {
            (0f, 142), (29.9f, 142), (30f, 146), (59.9f, 146), (60f, 152), (90f, 156), (500f, 156),
        };

        foreach (var (t, price) in expected)
            Assert.AreEqual(price, PawnMath.BuybackPrice(basis, t), $"t = {t} s");
    }

    [Test]
    public void TC_BUY_2_MuaLaiLuonLo()
    {
        for (int basis = 1; basis <= 500; basis++)
        foreach (float t in new[] { 0f, 30f, 60f, 90f })
        {
            int pawn = (int)System.Math.Floor(basis * 0.8);
            Assert.Greater(PawnMath.BuybackPrice(basis, t), pawn, $"basis {basis}, t = {t} s");
        }
    }

    [Test]
    public void TC_BUY_3_ThoiGianDenBacGiaKe()
    {
        Assert.AreEqual(20f, PawnMath.NextStepIn(10f), 1e-5f);
        Assert.AreEqual(30f, PawnMath.NextStepIn(30f), 1e-5f);
        Assert.AreEqual(-1f, PawnMath.NextStepIn(95f), "Đã ở bậc cao nhất");
    }

    // ---------- IconGate ----------

    [Test]
    public void TC_CRD_18_HetTheAn_KhongMoUngDungChuaMua()
    {
        var gate = new IconGate();
        gate.SetLicensed("Blackjack", false);

        gate.CardHide("Blackjack");
        gate.CardRelease("Blackjack");

        Assert.IsFalse(gate.IsVisible("Blackjack"), "Thẻ hết không được làm hiện app chưa có license");
        Assert.IsFalse(gate.IsLicensed("Blackjack"));
    }
}
