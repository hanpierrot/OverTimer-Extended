using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.5.4 và 4.5.5 của kế hoạch kiểm thử (Play Mode): Shop, cầm đồ theo catalog và mua lại, trên scene thật.
/// TC-SHP-1, 7, 8, 9, 10, 12, 13, 15 và TC-BUY-4 … 7. Phần logic thuần (báo giá, bậc giá mua lại) ở EditMode/ShopLogicTests.
/// Giá lấy từ ItemCatalog và phí giao từ ShopService.Config (không ghim số cân bằng; bảng giá đã có ở
/// PhoneShopSelfTests.ShopCatalogAsset). Hệ số mua lại ×1,05 / ×1,08 là quy tắc của DESIGN §5.11.
/// Mỗi khung hình = 0,02 s (UseFixedFrameRate(50)).
/// </summary>
public class ShopTests : GameSceneTest
{
    private const int Fps = 50;

    private static ShopService Shop => ShopService.Instance;
    private static int Money => MoneyService.Instance.Current;
    private static int Price(string id) => Shop.Ownership.Def(id).price;

    /// <summary>Tiền cầm đồ: 80% làm tròn xuống. Giá mua lại: basis × hệ số phần trăm, làm tròn lên (tính bằng số nguyên).</summary>
    private static int PawnPayout(int basis) => basis * 80 / 100;
    private static int Buyback(int basis, int percent) => (basis * percent + 99) / 100;

    private readonly List<HintId> _hints = new List<HintId>();
    private void RecordHint(HintId id) => _hints.Add(id);

    protected override void BeforeSceneLoad()
    {
        // Các gợi ý "chỉ hiện một lần" được nhớ trong PlayerPrefs (đã sao lưu, khôi phục sau test).
        foreach (HintId id in System.Enum.GetValues(typeof(HintId))) PlayerPrefs.DeleteKey($"HintSeen_{id}");
    }

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        TestHooks.SetClock(500f);       // các ca chờ giao hàng dài; Clock 120 s mặc định có thể hết giữa chừng
        _hints.Clear();
        HintService.HintShown += RecordHint;
        yield return null;

        // Ván bắt đầu ở góc Room; hàng giao chỉ hiện ra khi người chơi nhìn bàn (DoorController.RevealOnDesk).
        Assert.IsTrue(CameraRig.Instance.GoTo(CameraView.Desk));
        while (CameraRig.Instance.IsMoving) yield return null;
    }

    [TearDown]
    public void StopRecording() => HintService.HintShown -= RecordHint;

    private static IEnumerator Seconds(float s) => Frames(Mathf.RoundToInt(s * Fps));

    /// <summary>Bỏ thời gian chờ lặp lại của gợi ý, để gợi ý thứ hai cùng loại vẫn hiện.</summary>
    private static void ForgetHintCooldown() =>
        TestHooks.GetField<Dictionary<HintId, float>>(HintService.Instance, "_lastShown").Clear();

    /// <summary>Mua qua giỏ hàng như người chơi (giao Standard, không để mở sẵn).</summary>
    private static void Buy(params string[] ids)
    {
        foreach (var id in ids) Assert.IsTrue(Shop.AddToCart(id), $"Không thêm được '{id}' vào giỏ");
        TestHooks.SetMoney(Shop.Quote().total);
        Assert.IsTrue(Shop.TryBuy(), "Mua thất bại");
    }

    /// <summary>Mua rồi nhận hàng ngay (bỏ qua chờ giao) và đợi vật hiện trên bàn.</summary>
    private static IEnumerator BuyAndReceive(params string[] ids)
    {
        Buy(ids);
        Shop.Deliver(ids);
        yield return Frames(2);
        foreach (var id in ids)
            Assert.IsTrue(Shop.BindingFor(id).gameObject.activeSelf, $"'{id}' phải hiện trên bàn sau khi nhận");
    }

    private static Parcel ParcelAtDoor()
    {
        var tower = DoorController.Instance.Depot.Tower;
        Assert.AreEqual(1, tower.Count, "Phải có đúng một bưu kiện ở cửa");
        return tower[0];
    }

    private static void OpenParcel(Parcel parcel)
    {
        var depot = DoorController.Instance.Depot;
        while (!depot.Click(parcel)) { }
    }

    private static IEnumerator WaitForArrival(float maxSeconds)
    {
        int frames = Mathf.RoundToInt(maxSeconds * Fps);
        for (int i = 0; i < frames && DoorController.Instance.Depot.Tower.Count == 0; i++) yield return null;
        Assert.AreEqual(1, DoorController.Instance.Depot.Tower.Count, $"Hàng chưa đến sau {maxSeconds} s");
    }

    private static bool IconHidden(string app)
    {
        var pending = (HashSet<string>)typeof(DesktopIconView)
            .GetField("s_pendingHidden", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .GetValue(null);
        return pending.Contains(app);
    }

    // ---------- TC-SHP-1 ----------

    [Test]
    public void TC_SHP_1_TrangThaiDauVan()
    {
        foreach (var id in new[] { "power_bank", "card_box", "lucky_stack" })
        {
            Assert.AreEqual(Ownership.Status.NotOwned, Shop.Ownership.Of(id), id);
            Assert.IsFalse(Shop.BindingFor(id).gameObject.activeSelf, $"{id} không có trên bàn đầu ván");
        }

        foreach (var app in new[] { "Blackjack", "Billiards" })
        {
            Assert.IsFalse(Shop.IconGate.IsVisible(app), $"Icon {app} ẩn khi chưa có license");
            Assert.IsTrue(IconHidden(app), $"DesktopIconView giữ {app} ở trạng thái ẩn");
        }

        var sold = Shop.Ownership.Defs.Where(d => d.sold).Select(d => d.id).OrderBy(x => x).ToArray();
        CollectionAssert.AreEqual(
            new[] { "card_box", "license_billiards", "license_blackjack", "lucky_stack", "power_bank" }, sold,
            "Shop bán đúng 5 mặt hàng");
        foreach (var id in sold) Assert.Greater(Price(id), 0, $"{id} có giá");
    }

    // ---------- TC-SHP-7 ----------

    [Test]
    public void TC_SHP_7_MuaKhiThieuVaDuTien()
    {
        Shop.AddToCart("power_bank");
        Shop.AddToCart("card_box");
        int total = Shop.Quote().total;
        Assert.AreEqual(Price("power_bank") + Price("card_box") + Shop.Config.standardFee, total);

        TestHooks.SetMoney(total - 1);
        Assert.IsFalse(Shop.TryBuy(), "(a) thiếu $1");
        Assert.AreEqual(total - 1, Money);
        Assert.Contains(HintId.NotEnoughMoney, _hints);
        Assert.AreEqual(2, Shop.Cart.Count, "Giỏ giữ nguyên");

        TestHooks.SetMoney(total);
        Assert.IsTrue(Shop.TryBuy(), "(b) vừa đủ tiền");
        Assert.AreEqual(0, Money);
        Assert.AreEqual(Ownership.Status.Ordered, Shop.Ownership.Of("power_bank"));
        Assert.AreEqual(Ownership.Status.Ordered, Shop.Ownership.Of("card_box"));
        Assert.AreEqual(0, Shop.Cart.Count, "Giỏ trống");
    }

    // ---------- TC-SHP-8 ----------

    [Test]
    public void TC_SHP_8_MuaVaKichHoatLicense()
    {
        Buy("license_blackjack");
        Assert.AreEqual(0, Money);

        var msg = PhoneController.Instance.Inbox.All.SingleOrDefault(m => m.actionId == "license:license_blackjack");
        Assert.IsNotNull(msg, "Inbox có thư license kèm nút Activate");
        Assert.AreEqual("Your Blackjack license", msg.title);
        Assert.AreEqual(1, _hints.Count(h => h == HintId.LicenseBought));
        Assert.AreEqual(Ownership.Status.Ordered, Shop.Ownership.Of("license_blackjack"));
        Assert.IsFalse(Shop.IconGate.IsVisible("Blackjack"), "Chưa Activate thì icon vẫn ẩn");

        // Nhấn Activate trong Inbox: gọi đúng hàm của nút (InboxScreen.RunAction) với thư đang chọn.
        var inbox = TestHooks.Find<InboxScreen>();
        TestHooks.SetField(inbox, "_selectedId", msg.id);
        TestHooks.Call(inbox, "RunAction");

        Assert.AreEqual(Ownership.Status.Owned, Shop.Ownership.Of("license_blackjack"));
        Assert.IsTrue(Shop.IconGate.IsVisible("Blackjack"));
        Assert.IsFalse(IconHidden("Blackjack"), "Icon Blackjack hiện trên Laptop");
        Assert.IsTrue(msg.actionDone);

        TestHooks.Call(inbox, "RunAction");
        Assert.AreEqual(Ownership.Status.Owned, Shop.Ownership.Of("license_blackjack"), "Activate lần 2 không có tác dụng");

        Buy("license_billiards");
        Assert.AreEqual(1, _hints.Count(h => h == HintId.LicenseBought), "Gợi ý chỉ hiện một lần");
    }

    // ---------- TC-SHP-9 ----------

    [UnityTest]
    public IEnumerator TC_SHP_9_ThoiGianGiaoChiChayKhiClockChay()
    {
        float eta = Shop.Config.standardSeconds;
        var holder = new object();
        int arrivedFrame = -1, frame = 0;
        System.Action<Order> onArrived = _ => { if (arrivedFrame < 0) arrivedFrame = frame; };
        Shop.Delivery.Arrived += onArrived;

        Buy("power_bank");
        for (; frame < 5 * Fps; frame++) yield return null;
        ClockService.Instance.Pause(holder);                    // cuộc gọi Client đang nói: Clock tạm dừng 10 s
        for (; frame < 15 * Fps; frame++) yield return null;
        ClockService.Instance.Resume(holder);
        Assert.AreEqual(-1, arrivedFrame, "Không đến trong lúc Clock tạm dừng");
        for (; frame < (eta + 15) * Fps && arrivedFrame < 0; frame++) yield return null;

        Shop.Delivery.Arrived -= onArrived;
        Assert.That(arrivedFrame / (float)Fps, Is.EqualTo(eta + 10f).Within(0.1f), "Đến sau 20 s Clock chạy + 10 s tạm dừng");
    }

    // ---------- TC-SHP-10 ----------

    [Test]
    public void TC_SHP_10_ShopSauKhiHetVan()
    {
        Shop.AddToCart("power_bank");
        TestHooks.SetMoney(100);
        GameManager.Instance.GameOver(EndReason.ClockLoss);

        Assert.IsFalse(Shop.TryBuy());
        Assert.AreEqual(100, Money);
        Assert.AreEqual(Ownership.Status.NotOwned, Shop.Ownership.Of("power_bank"));
    }

    // ---------- TC-SHP-12 ----------

    [UnityTest]
    public IEnumerator TC_SHP_12_GiaCamDoTheoCatalog()
    {
        yield return BuyAndReceive("power_bank", "card_box", "lucky_stack");

        // Hàng mua: 80% giá đã trả, làm tròn xuống (DESIGN §5.11). Laptop: 80% catalogValue.
        var payouts = new (string id, int basis)[]
        {
            ("power_bank", Price("power_bank")), ("card_box", Price("card_box")), ("lucky_stack", Price("lucky_stack")),
            ("laptop", Shop.Ownership.Def("laptop").catalogValue),
        };
        foreach (var (id, basis) in payouts)
        {
            TestHooks.SetMoney(0);
            Pawning.PawnOnto(Shop.BindingFor(id));
            Assert.AreEqual(PawnPayout(basis), Money, $"{id}: nhận 80% của ${basis}");
            Assert.IsFalse(Shop.BindingFor(id).gameObject.activeSelf, $"{id} biến khỏi bàn");
        }

        foreach (var id in new[] { "egg_timer", "bear", "picture", "triple_stack", "phone" })
        {
            var item = Shop.BindingFor(id);
            Assert.IsNotNull(item, $"Không có ShopItemBinding cho '{id}'");
            TestHooks.SetMoney(0);
            ForgetHintCooldown();
            _hints.Clear();

            Pawning.PawnOnto(item);

            Assert.AreEqual(0, Money, $"{id}: không nhận tiền");
            Assert.Contains(HintId.CannotPawn, _hints, $"{id}: phải hiện gợi ý CannotPawn");
            Assert.IsTrue(item.gameObject.activeSelf, $"{id} vẫn trên bàn");
        }
    }

    // ---------- TC-SHP-13 ----------

    [UnityTest]
    public IEnumerator TC_SHP_13a_PanelMoGoiDangMo_KhongCamDoCardBox()
    {
        yield return BuyAndReceive("card_box");
        TestHooks.SetMoney(100);
        Cards.ClickBox();
        Assert.IsTrue(TestHooks.GetField<GameObject>(Cards.RevealPanel, "panel").activeSelf, "Panel mở gói đang hiện");
        int money = Money;

        Pawning.PawnOnto(Shop.BindingFor("card_box"));

        Assert.AreEqual(money, Money);
        Assert.IsTrue(Shop.BindingFor("card_box").gameObject.activeSelf);
        Assert.AreEqual(Ownership.Status.Owned, Shop.Ownership.Of("card_box"));
    }

    [UnityTest]
    public IEnumerator TC_SHP_13b_VeLuckyNineTrenBan_KhongCamDoChongVe()
    {
        yield return BuyAndReceive("lucky_stack");
        var stack = Shop.BindingFor("lucky_stack");
        TestHooks.SetMoney(100);
        stack.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);   // mua một vé Lucky Nine
        int money = Money;
        Assert.Less(money, 100, "Đã mua vé");

        Pawning.PawnOnto(stack);

        Assert.AreEqual(money, Money);
        Assert.IsTrue(stack.gameObject.activeSelf);
        Assert.AreEqual(Ownership.Status.Owned, Shop.Ownership.Of("lucky_stack"));
    }

    // ---------- TC-SHP-15 ----------

    [UnityTest]
    public IEnumerator TC_SHP_15_GoiYLanDauCamDo()
    {
        yield return BuyAndReceive("power_bank");

        Pawning.PawnOnto(Shop.BindingFor("power_bank"));
        Assert.AreEqual(1, _hints.Count(h => h == HintId.FirstPawn), "Lần 1 hiện gợi ý");
        Assert.AreEqual("The Printer pays for what you drop on it. A pawned item is gone.", HintCatalog.Get(HintId.FirstPawn));

        ForgetHintCooldown();
        Pawning.PawnOnto(Shop.BindingFor("laptop"));
        Assert.AreEqual(1, _hints.Count(h => h == HintId.FirstPawn), "Lần 2 không hiện");
    }

    // ---------- TC-BUY-4 … 7 ----------

    [UnityTest]
    public IEnumerator TC_BUY_4_MuaLaiPowerBank()
    {
        yield return BuyAndReceive("power_bank");
        var item = Shop.BindingFor("power_bank");
        var task = item.GetComponent<PowerBankTask>();
        Vector3 home = item.HomePosition;
        TestHooks.SetField(task, "holdTimer", 1f);                  // đang sạc dở
        item.transform.position = Pawning.Tool.transform.position;  // thả lên Printer như khi kéo

        Pawning.PawnOnto(item);
        int price = Buyback(Price("power_bank"), 105);   // trong 30 s đầu: ×1,05 làm tròn lên
        Assert.AreEqual(price, Shop.BuybackPrice("power_bank"));

        TestHooks.SetMoney(price);
        Assert.IsTrue(Shop.TryBuyBack("power_bank"));
        Assert.AreEqual(0, Money);
        Assert.AreEqual(-1, Shop.BuybackPrice("power_bank"), "Không còn trong tab PAWNED");

        yield return WaitForArrival(Shop.Config.standardSeconds + 1f);
        OpenParcel(ParcelAtDoor());
        yield return Frames(2);

        Assert.IsTrue(item.gameObject.activeSelf, "Power Bank hiện lại");
        Assert.AreEqual(home, item.transform.position, "Về đúng chỗ ban đầu, không nằm trên Printer");
        Assert.AreEqual(0f, TestHooks.GetFieldInHierarchy<float>(task, "holdTimer"), "Tiến trình sạc về 0");
        Assert.AreEqual(Ownership.Status.Owned, Shop.Ownership.Of("power_bank"));
    }

    [UnityTest]
    public IEnumerator TC_BUY_5_MuaLaiLaptop()
    {
        Laptop.Disabled = false;                                    // Wire Task đã giải
        Shop.Ownership.MarkOrdered("license_blackjack", Price("license_blackjack"));
        Shop.ActivateLicense("license_blackjack");
        yield return OpenLaptopApp(TestHooks.Find<MiningApp>());

        var opener = TestHooks.Find<DeskLaptopOpener>();
        TestHooks.SetMoney(0);
        Pawning.PawnOnto(opener);
        Assert.IsFalse(opener.gameObject.activeSelf);

        TestHooks.SetMoney(Shop.QuoteBuyback("laptop").total);
        Assert.IsTrue(Shop.TryBuyBack("laptop"));
        yield return WaitForArrival(Shop.Config.standardSeconds + 1f);
        OpenParcel(ParcelAtDoor());
        yield return Frames(2);
        Assert.IsTrue(opener.gameObject.activeSelf, "Laptop hiện lại trên bàn");

        TestHooks.Call(opener, "OnClicked", Vector2.zero);
        yield return null;

        Assert.IsTrue(Laptop.gameObject.activeInHierarchy, "Mở được Laptop: vẫn có điện");
        Assert.IsFalse(Laptop.Disabled, "Không phải làm lại Wire Task");
        Assert.IsNull(TestHooks.GetField<LaptopApp>(Laptop, "_current"), "Laptop ở màn Home, không mở lại Mining");
        Assert.IsTrue(Shop.IconGate.IsVisible("Blackjack"), "Icon Blackjack vẫn còn");
        Assert.AreEqual(Ownership.Status.Owned, Shop.Ownership.Of("license_blackjack"));
    }

    [UnityTest]
    public IEnumerator TC_BUY_6_MuaLaiKhiThieuTien()
    {
        yield return BuyAndReceive("power_bank");
        Pawning.PawnOnto(Shop.BindingFor("power_bank"));
        int total = Shop.QuoteBuyback("power_bank").total;
        _hints.Clear();

        TestHooks.SetMoney(total - 1);
        Assert.IsFalse(Shop.TryBuyBack("power_bank"));

        Assert.AreEqual(total - 1, Money);
        Assert.Contains(HintId.NotEnoughMoney, _hints);
        Assert.AreEqual(Ownership.Status.Pawned, Shop.Ownership.Of("power_bank"), "Vẫn ở tab PAWNED");
        Assert.Greater(Shop.BuybackPrice("power_bank"), 0);
    }

    [UnityTest]
    public IEnumerator TC_BUY_7_GiaMuaLaiKhongTangKhiClockDung()
    {
        int basis = Shop.Ownership.Def("laptop").catalogValue;
        Pawning.PawnOnto(TestHooks.Find<DeskLaptopOpener>());
        int first = Buyback(basis, 105);
        Assert.AreEqual(first, Shop.BuybackPrice("laptop"));

        var holder = new object();
        ClockService.Instance.Pause(holder);                        // cuộc gọi Client 40 s thực
        yield return Seconds(40f);
        Assert.AreEqual(first, Shop.BuybackPrice("laptop"), "Clock dừng thì giá không tăng");

        ClockService.Instance.Resume(holder);
        yield return Seconds(31f);
        Assert.AreEqual(Buyback(basis, 108), Shop.BuybackPrice("laptop"), "Đối chứng: Clock chạy 30 s thì lên bậc ×1,08");
    }
}
