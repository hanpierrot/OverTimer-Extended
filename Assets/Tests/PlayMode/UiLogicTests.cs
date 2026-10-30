using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Phần logic của các ca từng được đánh là kiểm thử thủ công (cột Mức = TC): TC-ALT-14, TC-SHP-14, TC-TKT-16, TC-FBK-7,
/// TC-CAM-6, TC-PHN-19. Test đọc trạng thái UI (đối tượng bật/tắt, chữ, vị trí khung) chứ không nhìn hình: màu sắc, hoạt ảnh,
/// làm mờ trên WebGL và cảm giác thao tác vẫn kiểm thủ công.
/// </summary>
public class UiLogicTests : GameSceneTest
{
    private const int Fps = 50;

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        TestHooks.SetClock(500f);
        TestHooks.Find<CatController>().gameObject.SetActive(false);   // Cat có thể ngồi chặn vật test đang dùng
        yield return null;
    }

    private static IEnumerator RealSeconds(float s)
    {
        float end = Time.realtimeSinceStartup + s;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    private static IEnumerator Restart()
    {
        var old = GameManager.Instance;
        GameManager.Instance.Restart();
        while (GameManager.Instance == null || GameManager.Instance == old) yield return null;
        yield return Frames(2);
    }

    private static PhoneHome Home => TestHooks.Find<PhoneHome>();

    private static IEnumerator OpenPhoneApp(string id)
    {
        DeviceLayer.Instance.OpenPhone();
        yield return Frames(25);
        Assert.IsFalse(PhoneController.Instance.IsRinging, "Không có cuộc gọi chen vào");
        Home.OpenApp(id);
        yield return Frames(2);
    }

    // ---------- TC-ALT-14 ----------

    [UnityTest]
    public IEnumerator TC_ALT_14_LichSuVanVoiKetThucMoi()
    {
        var reasons = new[] { EndReason.TicketJackpotWin, EndReason.TicketCurseLoss, EndReason.MotherLodeWin, EndReason.BlackjackCharlieWin };
        for (int i = 0; i < reasons.Length - 1; i++)
        {
            GameManager.Instance.GameOver(reasons[i]);
            yield return Restart();
        }

        // Ván thứ 4: bảng lịch sử nằm trong màn kết thúc (EndGamePanel), hiện sau cutscene kết thúc. Bỏ cutscene
        // (đường "không có cutscene" của GameManager.GameOver) để màn kết thúc hiện ngay.
        TestHooks.SetStaticAutoProperty<CutsceneController>("Instance", null);
        GameManager.Instance.GameOver(reasons[reasons.Length - 1]);
        yield return null;

        var history = RunStatsService.Instance.LoadHistory();
        CollectionAssert.AreEquivalent(reasons.Select(r => r.ToString()), history.Select(r => r.winCondition), "4 dòng, đúng tên EndReason");

        var panel = TestHooks.Find<RunHistoryPanel>();
        Assert.IsTrue(panel.gameObject.activeInHierarchy, "Màn kết thúc (có nút History) đang hiện");
        TestHooks.ClickButton(panel, "openButton");
        yield return null;
        var views = TestHooks.GetField<Transform>(panel, "entryContainer").GetComponentsInChildren<RunHistoryEntryView>(true);
        var rows = views.Select(v => TestHooks.GetField<TMP_Text>(v, "resultText").text).ToList();
        Assert.AreEqual(4, rows.Count, "Bảng có 4 dòng");
        foreach (var reason in reasons)
        {
            string result = RunRating.IsWin(reason) ? ">WIN<" : ">LOSE<";
            Assert.AreEqual(1, rows.Count(t => t.Contains(result) && t.Contains(RunRating.Title(reason))),
                $"{reason}: hiện {result.Trim('>', '<')} và tên “{RunRating.Title(reason)}”");
        }
        Assert.IsTrue(RunRating.IsWin(EndReason.TicketJackpotWin) && RunRating.IsWin(EndReason.MotherLodeWin)
                      && RunRating.IsWin(EndReason.BlackjackCharlieWin) && !RunRating.IsWin(EndReason.TicketCurseLoss),
            "3 thắng, Curse là thua");

        // Ván mới (nạp lại scene) vẫn đọc được lịch sử từ PlayerPrefs; tải lại trang WebGL thật kiểm thủ công.
        yield return Restart();
        Assert.AreEqual(4, RunStatsService.Instance.LoadHistory().Length, "Lịch sử còn sau khi nạp lại");
    }

    // ---------- TC-SHP-14 ----------

    [UnityTest]
    public IEnumerator TC_SHP_14_VatDaCamDoTrongShop()
    {
        var shop = ShopService.Instance;
        // Mua thật (GiveItem chỉ đánh dấu đã giao, giá gốc 0 nên cầm đồ trả 0 và bị từ chối).
        Assert.IsTrue(shop.AddToCart("power_bank"));
        TestHooks.SetMoney(shop.Quote().total);
        Assert.IsTrue(shop.TryBuy(), "Mua Power Bank");
        shop.Deliver(new[] { "power_bank" });
        yield return Frames(2);
        Pawning.PawnOnto(shop.BindingFor("power_bank"));
        Assert.AreEqual(Ownership.Status.Pawned, shop.Ownership.Of("power_bank"), "Power Bank đã bị cầm");
        string name = shop.Ownership.Def("power_bank").displayName;

        yield return OpenPhoneApp("shop");
        var screen = TestHooks.Find<ShopScreen>();
        Assert.IsTrue(screen.gameObject.activeInHierarchy, "Ứng dụng Shop đang mở");
        var rows = TestHooks.GetField<List<PhoneListRowData>>(screen, "_rows");
        var row = rows.Single(r => r.title == name);
        Assert.AreEqual("PAWNED", row.detail, "Tab SHOP: Power Bank hiện PAWNED");
        Assert.IsFalse(shop.AddToCart("power_bank"), "Không thêm vào giỏ được");
        Assert.IsFalse(shop.Cart.Contains("power_bank"));

        TestHooks.ClickButton(screen, "pawnedTabButton");
        yield return Frames(2);
        rows = TestHooks.GetField<List<PhoneListRowData>>(screen, "_rows");
        Assert.IsTrue(rows.Any(r => r.title == name), "Tab PAWNED có Power Bank");
    }

    // ---------- TC-TKT-16 ----------

    [Test]
    public void TC_TKT_16_BangThuongKhopAsset()
    {
        GiveItem("lucky_stack");
        var tickets = Object.FindObjectsByType<ScratchTicket>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var tripleCfg = TestHooks.GetField<TicketSymbolConfig>(tickets.First(t => t.TierKind == TicketTierKind.TripleMatch), "symbolConfig");
        var nineCfg = TestHooks.GetField<TicketSymbolConfig>(tickets.First(t => t.TierKind == TicketTierKind.LuckyNine), "symbolConfig");
        var tables = Object.FindObjectsByType<TicketPayoutTable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.IsTrue(tables.Any(t => TestHooks.GetField<TicketSymbolConfig>(t, "symbolConfig") == tripleCfg), "TriplePanel có bảng thưởng của Triple Match");
        Assert.IsTrue(tables.Any(t => TestHooks.GetField<TicketSymbolConfig>(t, "symbolConfig") == nineCfg), "NinePanel có bảng thưởng của Lucky Nine");

        foreach (var table in tables)
        {
            var cfg = TestHooks.GetField<TicketSymbolConfig>(table, "symbolConfig");
            Assert.IsTrue(cfg == tripleCfg || cfg == nineCfg, $"{table.name}: dùng đúng asset biểu tượng của một loại vé");
            TestHooks.Call(table, "Build");                         // panel lưu ẩn: Awake chưa chạy
            var pool = cfg.symbolPool;
            float total = pool.Sum(s => s.weight);
            string kind = cfg == nineCfg ? "Lucky Nine" : "Triple Match";

            for (int i = 0; i < pool.Length; i++)
            {
                var row = table.transform.Find($"Row{i}");
                Assert.IsNotNull(row, $"{kind}: dòng {i}");
                Assert.AreSame(pool[i].sprite, row.Find("Icon").GetComponent<Image>().sprite, $"{kind} dòng {i}: biểu tượng");
                float odds = float.Parse(row.Find("Odds").GetComponent<TMP_Text>().text.TrimEnd('%'), CultureInfo.CurrentCulture);
                Assert.AreEqual(pool[i].weight / total * 100f, odds, 0.051f, $"{kind} dòng {i}: tỷ lệ = trọng số / tổng");
                float pays = float.Parse(row.Find("Pays").GetComponent<TMP_Text>().text.TrimStart('x'), CultureInfo.CurrentCulture);
                Assert.AreEqual(pool[i].payoutMultiplier, pays, 0.0051f, $"{kind} dòng {i}: hệ số");
            }
            Assert.IsNull(table.transform.Find($"Row{pool.Length}"), $"{kind}: mỗi biểu tượng đúng một dòng");

            var win = table.transform.Find("WinText");
            if (cfg == nineCfg)
            {
                Assert.IsNotNull(win, "Lucky Nine có dòng thắng");
                Assert.AreEqual("= Win the game", win.Find("Win").GetComponent<TMP_Text>().text);
                var winRow = table.transform.Find("WinRow");
                Assert.AreEqual("9x", winRow.Find("Count").GetComponent<TMP_Text>().text);
                Assert.AreSame(pool[ScratchTicket.IndexOfHighestMultiplier(pool)].sprite, winRow.Find("Icon").GetComponent<Image>().sprite,
                    "Biểu tượng hiếm nhất");
            }
            else Assert.IsNull(win, "Triple Match không có dòng thắng");
        }
    }

    // ---------- TC-FBK-7 ----------

    [UnityTest]
    public IEnumerator TC_FBK_7_RewardPopup()
    {
        var popup = TestHooks.Find<MoneyPopup>();
        var pool = TestHooks.GetField<TMP_Text[]>(popup, "pool");
        var money = MoneyService.Instance;
        int cap = Mathf.Clamp(Config.rewardPopupMaxActive, 1, pool.Length);
        Assert.IsTrue(pool.All(t => !t.raycastTarget), "Popup không chặn nhấp chuột");

        void Clear() { foreach (var t in pool) t.gameObject.SetActive(false); }
        List<TMP_Text> Active() => pool.Where(t => t.gameObject.activeSelf).ToList();
        bool SameRgb(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

        // (a) Khoản thu tiền.
        Clear();
        money.Add(5, "click task");
        var a = Active();
        Assert.AreEqual(1, a.Count, "(a) một popup");
        Assert.AreEqual("+$5", a[0].text);
        Assert.IsTrue(SameRgb(Config.rewardPopupMoneyColor, a[0].color), "(a) màu tiền");

        // (b) Egg Timer cộng giờ.
        yield return RealSeconds(Config.rewardPopupStackSeconds + 0.05f);
        Clear();
        var egg = TestHooks.Find<EggTimerTask>().GetComponent<PointerReceiver>();
        egg.OnPressStart(Vector2.zero);
        for (int i = 0; i < Mathf.CeilToInt(Config.crankTime * Fps) + 1; i++) { yield return null; egg.OnPressUpdate(Vector2.zero); }
        egg.OnPressEnd(Vector2.zero);
        var time = Active().FirstOrDefault(t => t.text.EndsWith("s"));
        Assert.IsNotNull(time, "(b) có popup giờ");
        Assert.AreEqual($"+{Config.timePayout:0.#}s", time.text);
        Assert.IsTrue(SameRgb(Config.rewardPopupTimeColor, time.color), "(b) màu giờ");

        // (c) Tiền hoàn cược Billiards (lý do không phải thu nhập) và (f) chi tiền: không có popup.
        Clear();
        money.Add(20, MoneyService.StakeRefundReason);
        Assert.AreEqual(0, Active().Count, "(c) tiền hoàn cược không có popup");
        TestHooks.SetMoney(100);
        Assert.IsTrue(money.TrySpend(30, "meter"));
        Assert.AreEqual(0, Active().Count, "(f) chi tiền không có popup");

        // (d) Hai khoản trong 0,25 s: hai popup, cái sau cao hơn.
        yield return RealSeconds(Config.rewardPopupStackSeconds + 0.05f);
        Clear();
        money.Add(3, "a");
        money.Add(4, "b");
        var d = Active();
        Assert.AreEqual(2, d.Count, "(d) hai popup");
        var first = d.Single(t => t.text == "+$3");
        var second = d.Single(t => t.text == "+$4");
        Assert.Greater(second.rectTransform.position.y, first.rectTransform.position.y, "(d) xếp chồng lên trên");

        // (g) 9 khoản liên tiếp: tối đa 8 popup, khoản thứ 9 dùng lại popup cũ nhất.
        yield return RealSeconds(Config.rewardPopupStackSeconds + 0.05f);
        Clear();
        int oldest = TestHooks.GetField<int>(popup, "_next");
        for (int i = 1; i <= cap + 1; i++) money.Add(i, "g");
        Assert.AreEqual(cap, Active().Count, $"(g) tối đa {cap} popup");
        Assert.AreEqual($"+${cap + 1}", pool[oldest].text, "(g) popup cũ nhất được dùng lại");

        // (e) Sau GameOver: không hiện.
        Clear();
        GameManager.Instance.GameOver(EndReason.ClockLoss);
        money.Add(5, "late");
        Assert.AreEqual(0, Active().Count, "(e) sau GameOver không có popup");
    }

    // ---------- TC-CAM-6 ----------

    [UnityTest]
    public IEnumerator TC_CAM_6_BoCucDeviceLayer()
    {
        var layer = DeviceLayer.Instance;
        var laptopSlot = TestHooks.GetField<RectTransform>(layer, "laptopSlot");
        var phoneSlot = TestHooks.GetField<RectTransform>(layer, "phoneSlot");
        var blur = TestHooks.GetField<RawImage>(TestHooks.GetField<BlurSnapshot>(layer, "blur"), "target");
        float slide = TestHooks.GetField<float>(layer, "slideSeconds") + 0.15f;
        float X(RectTransform r) => r.anchoredPosition.x;
        float F(string field) => TestHooks.GetField<float>(layer, field);

        // Mở Laptop: nền mờ, Phone gắn bên phải.
        var laptop = Laptop;
        laptop.Disabled = false;
        laptop.gameObject.SetActive(true);
        yield return RealSeconds(slide);
        Assert.AreEqual(DeviceLayer.Mode.Laptop, layer.Current);
        Assert.IsTrue(blur.gameObject.activeInHierarchy, "Nền phía sau được làm mờ / phủ tối");
        Assert.IsTrue(phoneSlot.gameObject.activeSelf, "Phone gắn bên phải");
        Assert.AreEqual(F("phoneDockX"), X(phoneSlot), 0.5f);
        Assert.AreEqual(F("laptopDockX"), X(laptopSlot), 0.5f);

        // Nhấp Phone: Laptop trượt sang trái, Phone ra giữa.
        TestHooks.ClickButton(layer, "phoneBodyButton");
        Assert.AreEqual(DeviceLayer.Mode.Phone, layer.Current);
        yield return RealSeconds(slide);
        Assert.AreEqual(F("laptopHiddenX"), X(laptopSlot), 0.5f, "Laptop trượt khỏi màn hình bên trái");
        Assert.AreEqual(F("phoneActiveX"), X(phoneSlot), 0.5f, "Phone ra vị trí của màn chỉ có Phone");

        // Đóng Phone: cả hai trở về.
        layer.ClosePhone();
        yield return RealSeconds(slide);
        Assert.AreEqual(DeviceLayer.Mode.Laptop, layer.Current);
        Assert.AreEqual(F("laptopDockX"), X(laptopSlot), 0.5f);
        Assert.AreEqual(F("phoneDockX"), X(phoneSlot), 0.5f);

        // Mở Mining: Laptop ra giữa, Phone trượt khỏi màn hình rồi tắt.
        yield return OpenLaptopApp(TestHooks.Find<MiningApp>());
        yield return RealSeconds(slide);
        Assert.AreEqual(0f, X(laptopSlot), 0.5f, "Laptop ra giữa");
        Assert.IsFalse(phoneSlot.gameObject.activeSelf, "Phone đã trượt khỏi màn hình");

        // Đóng Mining: trở về bố cục ban đầu.
        laptop.CloseCurrentApp();
        yield return RealSeconds(slide);
        Assert.AreEqual(F("laptopDockX"), X(laptopSlot), 0.5f);
        Assert.IsTrue(phoneSlot.gameObject.activeSelf);
        Assert.AreEqual(F("phoneDockX"), X(phoneSlot), 0.5f);
    }

    // ---------- TC-PHN-19 ----------

    [UnityTest]
    public IEnumerator TC_PHN_19_NhanIncomingCallKhiDangOUngDungLaptop()
    {
        yield return OpenLaptopApp(TestHooks.Find<MiningApp>());
        var pill = TestHooks.Find<IncomingCallPill>();
        var visual = TestHooks.GetField<GameObject>(pill, "visual");
        Assert.IsFalse(visual.activeSelf, "Chưa reo: không có nhãn");

        var phone = PhoneController.Instance;
        phone.Calls.ForceRingNow();
        for (int i = 0; i < 10 && !phone.IsRinging; i++) yield return null;
        Assert.IsTrue(phone.IsRinging, "Phone đang reo");
        yield return null;
        Assert.IsTrue(visual.activeSelf, "Nhãn hiện khi đang ở ứng dụng Laptop");
        StringAssert.StartsWith("Incoming call", TestHooks.GetField<TMP_Text>(pill, "label").text);

        TestHooks.ClickButton(pill, "button");
        yield return Frames(2);
        Assert.AreEqual(DeviceLayer.Mode.Phone, DeviceLayer.Instance.Current, "Nhấp nhãn mở màn chỉ có Phone");
        Assert.IsFalse(visual.activeSelf, "Ở màn Phone thì nhãn ẩn");
        var callWindow = TestHooks.GetField<GameObject>(Home, "callWindow");
        Assert.IsTrue(callWindow.activeInHierarchy, "Cửa sổ cuộc gọi đang hiện");
        var labels = callWindow.GetComponentsInChildren<TMP_Text>().Select(t => t.text.ToUpperInvariant()).ToList();
        Assert.IsTrue(labels.Any(t => t.Contains("ACCEPT")), "Có nút ACCEPT");
        Assert.IsTrue(labels.Any(t => t.Contains("DECLINE")), "Có nút DECLINE");
    }
}
