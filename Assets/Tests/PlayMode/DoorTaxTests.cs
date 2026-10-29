using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.5.6 và 4.5.7 của kế hoạch kiểm thử (Play Mode): cửa (bưu kiện, thư thuế, thư rác) và hạn trả thuế trên scene thật.
/// TC-DOR-5 … 9, TC-TAX-4 … 6. Phần logic thuần (DoorDepot, LetterPile, JunkMailTimer, TaxDeadline) ở EditMode.
/// Số tiền, thời gian giao và mức phạt đọc từ GameConfig / ShopService.Config, không ghim số cân bằng.
/// Tiếng chuông cửa (AudioSource.PlayOneShot) không đọc lại được từ test: phần đó kiểm thủ công.
/// Mỗi khung hình = 0,02 s (UseFixedFrameRate(50)).
/// </summary>
public class DoorTaxTests : GameSceneTest
{
    private const int Fps = 50;

    private static ShopService Shop => ShopService.Instance;
    private static DoorController Door => DoorController.Instance;
    private static ITRSService Itrs => ITRSService.Instance;
    private static PhoneController Phone => PhoneController.Instance;
    private static float Clock => ClockService.Instance.RemainingTime;

    private readonly List<HintId> _hints = new List<HintId>();
    private readonly List<string> _hintTexts = new List<string>();
    private void RecordHint(HintId id) => _hints.Add(id);
    private int HintCount(HintId id) => _hints.Count(h => h == id);

    protected override void BeforeSceneLoad()
    {
        // Các gợi ý "chỉ hiện một lần" được nhớ trong PlayerPrefs (đã sao lưu, khôi phục sau test).
        foreach (HintId id in System.Enum.GetValues(typeof(HintId))) PlayerPrefs.DeleteKey($"HintSeen_{id}");
    }

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        TestHooks.SetClock(500f);
        _hints.Clear();
        HintService.HintShown += RecordHint;
        yield return null;
    }

    [TearDown]
    public void StopRecording() => HintService.HintShown -= RecordHint;

    private static IEnumerator Seconds(float s) => Frames(Mathf.RoundToInt(s * Fps));

    private static IEnumerator GoTo(CameraView view)
    {
        if (CameraRig.Instance.CurrentView != view) Assert.IsTrue(CameraRig.Instance.GoTo(view), $"Không chuyển được sang góc {view}");
        while (CameraRig.Instance.IsMoving) yield return null;
        yield return null;
    }

    /// <summary>Tắt Phone như khi bấm nút nguồn, để không có cuộc gọi nào tạm dừng Clock giữa ca.</summary>
    private void PhoneOff() => Phone.ForceOff(this);

    private static void Buy(string id, bool leaveOpen = false)
    {
        Shop.SetLeaveOpen(leaveOpen);
        Assert.IsTrue(Shop.AddToCart(id), $"Không thêm được '{id}' vào giỏ");
        TestHooks.SetMoney(Shop.Quote().total);
        Assert.IsTrue(Shop.TryBuy(), "Mua thất bại");
    }

    private static IEnumerator WaitForArrival()
    {
        bool arrived = false;
        System.Action<Order> onArrived = _ => arrived = true;
        Shop.Delivery.Arrived += onArrived;
        int frames = Mathf.RoundToInt((Shop.Config.standardSeconds + 2f) * Fps);
        for (int i = 0; i < frames && !arrived; i++) yield return null;
        Shop.Delivery.Arrived -= onArrived;
        Assert.IsTrue(arrived, "Hàng chưa đến");
        yield return null;
    }

    /// <summary>
    /// Vật giao xong hiện ra qua DOVirtual.DelayedCall, mà DOTween mặc định đếm theo giờ thực. Với captureFramerate các khung
    /// hình chạy nhanh hơn giờ thực, nên phải chờ theo Time.realtimeSinceStartup chứ không theo số khung.
    /// </summary>
    private static IEnumerator WaitUntilShown(Component item, float realSeconds = 3f)
    {
        float deadline = Time.realtimeSinceStartup + realSeconds;
        while (!item.gameObject.activeSelf && Time.realtimeSinceStartup < deadline) yield return null;
    }

    private static ParcelView ViewOf(Parcel parcel) =>
        TestHooks.GetField<List<ParcelView>>(Door, "_pool").Find(v => v.Data == parcel);

    private static LetterView ViewOf(Letter letter) =>
        TestHooks.GetField<List<LetterView>>(Door, "_letterPool").Find(v => v.Data == letter);

    /// <summary>Nhấp chuột lên một vật ở cửa (DeskObject.OnClicked, cùng đường InputManager gọi).</summary>
    private static void Click(DeskObject obj) => TestHooks.Call(obj, "OnClicked", Vector2.zero);

    private static Tax TaxPanel => TestHooks.GetField<Tax>(Door, "taxPanel");

    private static TaxDeadline Deadline => TestHooks.GetField<TaxDeadline>(Itrs, "_deadline");

    /// <summary>Phát hóa đơn ngay (bỏ qua chờ billInterval), với số tiền kiếm được cho trước.</summary>
    private static int IssueBill(int earned = 150)
    {
        TestHooks.SetField(Itrs, "earnedSinceLastBill", earned);
        Itrs.ForceBillNow();
        Assert.IsTrue(Itrs.HasPendingAssessment, "Hóa đơn phải được phát");
        return Itrs.CurrentAmountDue;
    }

    // ---------- TC-DOR-5 ----------

    [UnityTest]
    public IEnumerator TC_DOR_5_HangDenCua_MoBuuKien_HienTrenBan()
    {
        PhoneOff();
        yield return GoTo(CameraView.Desk);
        Buy("power_bank");

        yield return WaitForArrival();

        Assert.AreEqual(1, Door.Depot.Tower.Count, "Bưu kiện nằm ở tháp cạnh cửa");
        var parcel = Door.Depot.Tower[0];
        CollectionAssert.AreEqual(new[] { "power_bank" }, parcel.itemIds);
        Assert.AreEqual(1, HintCount(HintId.PackageArrived), "Gợi ý PackageArrived một lần");
        Assert.IsFalse(Shop.BindingFor("power_bank").gameObject.activeSelf, "Chưa mở hộp thì chưa có trên bàn");

        yield return GoTo(CameraView.Door);
        Assert.AreEqual(1, HintCount(HintId.ParcelOpen), "Gợi ý ParcelOpen khi nhìn cửa có bưu kiện");

        var view = ViewOf(parcel);
        Assert.IsNotNull(view, "Bưu kiện phải có hình ở cửa");
        for (int i = 0; i < Config.parcelClicksToOpen - 1; i++) Click(view);
        Assert.AreEqual(1, Door.Depot.Tower.Count, $"Chưa mở sau {Config.parcelClicksToOpen - 1} lần nhấp");
        Click(view);
        Assert.AreEqual(0, Door.Depot.Tower.Count, $"Mở sau {Config.parcelClicksToOpen} lần nhấp");

        string title = $"{Shop.Ownership.Def("power_bank").displayName} delivered";
        Assert.IsTrue(Phone.Inbox.All.Any(m => m.title == title), $"Inbox có “{title}”");
        yield return Frames(5);
        Assert.IsFalse(Shop.BindingFor("power_bank").gameObject.activeSelf, "Đang nhìn cửa: vật chờ tới khi về Desk");

        yield return GoTo(CameraView.Desk);
        yield return WaitUntilShown(Shop.BindingFor("power_bank"));
        Assert.IsTrue(Shop.BindingFor("power_bank").gameObject.activeSelf, "Power Bank hiện trên bàn khi về Desk");
        Assert.AreEqual(1, HintCount(HintId.ShopPurchased), "Gợi ý “Your item is on the desk.” một lần");

        // Đơn thứ hai: các gợi ý chỉ hiện một lần không hiện lại.
        Buy("card_box");
        yield return WaitForArrival();
        yield return GoTo(CameraView.Door);
        Assert.AreEqual(1, HintCount(HintId.PackageArrived));
        Assert.AreEqual(1, HintCount(HintId.ParcelOpen));
    }

    // ---------- TC-DOR-6 ----------

    [UnityTest]
    public IEnumerator TC_DOR_6_DeMoSan_LenThangBan()
    {
        PhoneOff();
        yield return GoTo(CameraView.Desk);
        Buy("power_bank", leaveOpen: true);

        yield return WaitForArrival();
        yield return WaitUntilShown(Shop.BindingFor("power_bank"));

        Assert.AreEqual(0, Door.Depot.Tower.Count, "Không có bưu kiện ở cửa");
        Assert.AreEqual(0, HintCount(HintId.PackageArrived), "Không hiện gợi ý PackageArrived");
        Assert.IsTrue(Shop.BindingFor("power_bank").gameObject.activeSelf, "Power Bank lên thẳng bàn");
    }

    // ---------- TC-DOR-7 ----------

    [UnityTest]
    public IEnumerator TC_DOR_7a_ThuThue_DenSau10Giay_NhapMoPanelTax()
    {
        PhoneOff();
        IssueBill();
        Assert.IsNull(Door.Letters.FirstOf(LetterKind.Tax), "Thư chưa đến ngay khi phát hóa đơn");

        yield return Seconds(Config.taxLetterDelay - 0.5f);
        Assert.IsNull(Door.Letters.FirstOf(LetterKind.Tax), "Thư chưa đến trước taxLetterDelay");
        yield return Seconds(1f);

        var letter = Door.Letters.FirstOf(LetterKind.Tax);
        Assert.IsNotNull(letter, $"Thư thuế đến sau {Config.taxLetterDelay} s");
        var view = ViewOf(letter);
        Assert.IsNotNull(view, "Thư thuế nằm trên thảm");
        Assert.IsTrue(view.gameObject.activeSelf);
        Assert.AreEqual(1, HintCount(HintId.MailTrayFirstBill), "Gợi ý thư thuế đầu tiên");

        Click(view);
        yield return null;
        Assert.IsTrue(TaxPanel.gameObject.activeSelf, "Nhấp thư mở panel Tax");
    }

    [UnityTest]
    public IEnumerator TC_DOR_7b_TraTruocKhiThuDen_ThuKhongXuatHien()
    {
        PhoneOff();
        int amount = IssueBill();
        yield return Seconds(5f);
        TestHooks.SetMoney(amount);
        Assert.IsTrue(Itrs.Pay(), "Trả hóa đơn");

        yield return Seconds(Config.taxLetterDelay + 2f);

        Assert.IsNull(Door.Letters.FirstOf(LetterKind.Tax), "Thư thuế không bao giờ xuất hiện");
    }

    // ---------- TC-DOR-8 ----------

    [UnityTest]
    public IEnumerator TC_DOR_8_ThuRac_NhapHienToRoiVaBoDi()
    {
        PhoneOff();
        // Thời gian chờ thư rác (60 … 120 s, không trước 60 s đầu ván) đã kiểm ở EditMode; ở đây cho thư đến sau 1 s.
        TestHooks.SetField(TestHooks.GetField<JunkMailTimer>(Door, "_junkTimer"), "_left", 1f);
        yield return Seconds(1.5f);

        var letter = Door.Letters.FirstOf(LetterKind.Junk);
        Assert.IsNotNull(letter, "Có thư rác ở cửa");
        Assert.IsFalse(string.IsNullOrEmpty(letter.text), "Thư rác có câu đùa");
        var view = ViewOf(letter);
        Assert.IsNotNull(view);

        Click(view);
        yield return null;

        Assert.AreEqual(1, HintCount(HintId.JunkFlyer), "Hiện tờ rơi (gợi ý JunkFlyer)");
        Assert.IsNull(Door.Letters.FirstOf(LetterKind.Junk), "Thư rác bị bỏ đi");
        Assert.IsFalse(TaxPanel.gameObject.activeSelf, "Thư rác không mở panel Tax");
    }

    // ---------- TC-DOR-9 ----------

    [UnityTest]
    public IEnumerator TC_DOR_9_Restart_XoaThuBuuKienDonHang()
    {
        PhoneOff();
        Buy("power_bank");
        yield return WaitForArrival();
        Buy("card_box");                                   // đơn thứ hai còn đang giao
        Door.DropJunk();
        Assert.AreEqual(1, Door.Depot.Tower.Count);
        Assert.AreEqual(1, Shop.Delivery.InTransit.Count(o => !o.arrived));
        Assert.AreEqual(1, Door.Letters.Letters.Count);

        var oldDoor = Door;
        GameManager.Instance.Restart();
        while (Door == null || Door == oldDoor) yield return null;
        yield return null;                                 // chờ mọi Start() của ván mới

        Assert.AreEqual(0, Door.Depot.Tower.Count, "Cửa trống");
        Assert.AreEqual(0, Door.Letters.Letters.Count, "Không còn thư");
        Assert.AreEqual(0, Shop.Delivery.InTransit.Count, "Không còn đơn đang giao");
        foreach (var id in new[] { "power_bank", "card_box" })
        {
            Assert.AreEqual(Ownership.Status.NotOwned, Shop.Ownership.Of(id), $"{id} như đầu ván");
            Assert.IsFalse(Shop.BindingFor(id).gameObject.activeSelf, $"{id} không có trên bàn");
        }
        Assert.AreEqual(0, Shop.Quote().subtotal, "Giỏ hàng trống");
        Assert.IsFalse(Shop.LeaveOpen);
    }

    // ---------- TC-TAX-4 ----------

    [UnityTest]
    public IEnumerator TC_TAX_4_KhongTra_PhatMotLan_RoiDemHoaDonKe()
    {
        PhoneOff();
        TestHooks.SetClock(100f);
        int amount = IssueBill();
        float penalty = amount * Config.shortfallPenalty;
        float expected = 100f - Config.taxPayWindow - penalty;
        Assume.That(expected, Is.GreaterThan(5f), "Với GameConfig hiện tại người chơi thua ngay; ca này cần Clock còn dương");

        bool resolved = false;
        System.Action onResolved = () => resolved = true;
        Itrs.OnAssessmentResolved += onResolved;
        int frames = 0;
        while (!resolved && frames < (Config.taxPayWindow + 1f) * Fps) { yield return null; frames++; }
        Itrs.OnAssessmentResolved -= onResolved;

        Assert.IsTrue(resolved, "Hóa đơn hết hạn");
        Assert.That(frames / (float)Fps, Is.EqualTo(Config.taxPayWindow).Within(0.05f), "Hết hạn sau đúng taxPayWindow giây Clock chạy");
        Assert.That(Clock, Is.EqualTo(expected).Within(0.1f), $"Clock = 100 − {Config.taxPayWindow} − {amount} × {Config.shortfallPenalty}");

        float afterPenalty = Clock;
        yield return Seconds(2f);
        Assert.That(Clock, Is.EqualTo(afterPenalty - 2f).Within(0.05f), "Chỉ bị phạt một lần");

        // Hóa đơn kế: billInterval giây Clock chạy sau khi hóa đơn cũ kết thúc.
        TestHooks.SetClock(500f);
        int wait = 0;
        while (!Itrs.HasPendingAssessment && wait < (Config.billInterval + 2f) * Fps) { yield return null; wait++; }
        Assert.IsTrue(Itrs.HasPendingAssessment, "Có hóa đơn kế");
        Assert.That(2f + wait / (float)Fps, Is.EqualTo(Config.billInterval).Within(0.1f), "Hóa đơn kế sau billInterval giây");
    }

    // ---------- TC-TAX-5 ----------

    [UnityTest]
    public IEnumerator TC_TAX_5_PhatThueBoQuaFinalCountdown()
    {
        PhoneOff();
        Cards.Place(Cards.Asset("FinalCountdown"));
        Assert.AreEqual(FinalCountdownTracker.Phase.Armed, FinalCountdownTracker.Instance.CurrentPhase);
        TestHooks.SetClock(50f);
        int amount = IssueBill();
        Assume.That(amount * Config.shortfallPenalty, Is.GreaterThanOrEqualTo(50f), "Ca này cần mức phạt ≥ Clock còn lại");
        TestHooks.SetAutoProperty(Deadline, "SecondsLeft", 1f);

        yield return Seconds(1.5f);

        Assert.AreEqual(0f, Clock, "Clock về 0");
        Assert.IsTrue(GameManager.Instance.IsGameOver, "Final Countdown không chặn khoản phạt");
        Assert.AreEqual(EndReason.ClockLoss, GameManager.Instance.LastEndReason);
        Assert.AreNotEqual(FinalCountdownTracker.Phase.Triggered, FinalCountdownTracker.Instance.CurrentPhase);
    }

    // ---------- TC-TAX-6 ----------

    [UnityTest]
    public IEnumerator TC_TAX_6_HanTraDungKhiCuocGoiTamDungClock()
    {
        IssueBill();
        // Cuộc gọi kế chắc chắn là Client (chỉ Client và ITRS tạm dừng Clock; ITRS thì gia hạn hóa đơn).
        var cfg = Phone.Config;
        cfg.weightTelemarketer = 0; cfg.weightUnknown = 0; cfg.weightITRS = 0; cfg.weightClient = 1;
        Phone.Calls.ForceRingNow();
        for (int i = 0; i < 10 && !Phone.IsRinging; i++) yield return null;
        Assert.IsTrue(Phone.IsRinging, "Phone phải reo");

        Phone.Answer();
        Assert.IsNotNull(Phone.ActiveCall);
        Assert.AreEqual(CallTruth.Client, Phone.ActiveCall.truth);
        // Khách tự cúp máy nếu người chơi không trả lời sau beneficialCallTimeout giây thực; giữ máy đủ 15 s.
        TestHooks.SetField(Phone, "_firstLineLeft", 16f);
        TestHooks.SetAutoProperty(Deadline, "SecondsLeft", 20f);
        float clockBefore = Clock;

        yield return Seconds(15f);

        Assert.IsNotNull(Phone.ActiveCall, "Cuộc gọi vẫn đang diễn ra");
        Assert.AreEqual(20f, Itrs.SecondsLeft, "Hạn trả không trôi trong lúc cuộc gọi tạm dừng Clock");
        Assert.AreEqual(clockBefore, Clock, "Clock tạm dừng");
        Assert.IsTrue(Itrs.HasPendingAssessment);
    }
}
