using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Mục 4.4.4 của kế hoạch kiểm thử (Play Mode): TC-CRD-14, 15, 17, 19, 20, 21, 22, trên scene thật với asset thẻ thật.
/// Đặt thẻ = CardPackRevealPanel.TryPlaceCard (nút Place); Pawn = Pawn.OnTriggerEnter2D (xem Pawning).
/// Thời lượng, hệ số, phí đọc từ asset thẻ và GameConfig.
/// Mỗi khung hình = 0,02 s (UseFixedFrameRate(50)).
/// </summary>
public class CardRuleTests : GameSceneTest
{
    private const int Fps = 50;

    private static int Money => MoneyService.Instance.Current;
    private static FinalCountdownTracker Tracker => FinalCountdownTracker.Instance;
    private static PhoneController Phone => PhoneController.Instance;

    private readonly List<HintId> _hints = new List<HintId>();
    private void RecordHint(HintId id) => _hints.Add(id);

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        TestHooks.SetClock(500f);
        TestHooks.Find<CatController>().gameObject.SetActive(false);   // Cat có thể ngồi chặn Card Box giữa ca
        _hints.Clear();
        HintService.HintShown += RecordHint;
        yield return null;
    }

    [TearDown]
    public void StopRecording() => HintService.HintShown -= RecordHint;

    private static IEnumerator Seconds(float s) => Frames(Mathf.RoundToInt(s * Fps));
    private static T Field<T>(string card, string field) => TestHooks.GetField<T>(Cards.Asset(card), field);

    // ---------- TC-CRD-14 ----------

    [Test]
    public void TC_CRD_14a_PawnFinalCountdownArmed_TraPhi_Disarm()
    {
        var card = Cards.Place(Cards.Asset("FinalCountdown"));
        Assert.AreEqual(FinalCountdownTracker.Phase.Armed, Tracker.CurrentPhase);
        int fee = Config.debuffDismissFee;
        TestHooks.SetMoney(fee + 4);

        Pawning.PawnOnto(card);

        Assert.AreEqual(4, Money, $"Trả phí ${fee}, không nhận tiền cầm đồ");
        Assert.AreEqual(FinalCountdownTracker.Phase.None, Tracker.CurrentPhase, "Disarm");
        Assert.IsFalse(ClockService.Instance.HasFinalCountdownIntercept, "Clock không còn được chặn");
    }

    [Test]
    public void TC_CRD_14b_PawnFinalCountdownArmed_ThieuPhi_TuChoiVaDoiCho()
    {
        var card = Cards.Place(Cards.Asset("FinalCountdown"));
        int fee = Config.debuffDismissFee;
        TestHooks.SetMoney(fee - 1);
        card.transform.position = new Vector3(123f, 45f, card.transform.position.z);   // giả lập thẻ đang nằm trên Printer

        Pawning.PawnOnto(card);

        Assert.AreEqual(fee - 1, Money, "Từ chối: tiền không đổi");
        Assert.AreEqual(FinalCountdownTracker.Phase.Armed, Tracker.CurrentPhase, "Vẫn Armed");
        Assert.IsTrue(card.gameObject.activeSelf, "Thẻ vẫn trên bàn");
        CollectionAssert.Contains(_hints, HintId.DebuffDismissCost, $"Gợi ý phí ${fee}");
        Assert.AreNotEqual(new Vector2(123f, 45f), (Vector2)card.transform.position, "Thẻ được dời khỏi Printer");
    }

    [UnityTest]
    public IEnumerator TC_CRD_14c_PawnFinalCountdownTriggered_TuChoi()
    {
        var card = Cards.Place(Cards.Asset("FinalCountdown"));
        TestHooks.SetClock(0.05f);
        for (int i = 0; i < 20 && Tracker.CurrentPhase != FinalCountdownTracker.Phase.Triggered; i++) yield return null;
        Assert.AreEqual(FinalCountdownTracker.Phase.Triggered, Tracker.CurrentPhase);
        TestHooks.SetMoney(10);

        Pawning.PawnOnto(card);

        Assert.AreEqual(10, Money);
        Assert.IsTrue(card.gameObject.activeSelf, "Thẻ vẫn trên bàn");
        Assert.AreEqual(FinalCountdownTracker.Phase.Triggered, Tracker.CurrentPhase);
        CollectionAssert.Contains(_hints, HintId.CannotPawn);
    }

    // ---------- TC-CRD-15 ----------

    [UnityTest]
    public IEnumerator TC_CRD_15_HaiTheTangGiaGoi_NhanTheoKhoa()
    {
        float mult = Field<float>("CardCostModifier", "costMultiplier");
        float duration = Field<float>("CardCostModifier", "duration");
        int baseCost = Config.packCost;
        var service = CostModifierService.Instance;
        int Price() => Mathf.RoundToInt(baseCost * service.GetMultiplier(CostModifierService.Target.CardPack));   // cùng phép làm tròn với CardBox

        Cards.Place(Cards.Asset("CardCostModifier"));
        Assert.AreEqual(Mathf.RoundToInt(baseCost * mult), Price(), "Một thẻ");
        yield return Seconds(5f);
        Cards.Place(Cards.Asset("CardCostModifier"));
        Assert.AreEqual(Mathf.RoundToInt(baseCost * mult * mult), Price(), "Hai thẻ nhân với nhau");

        yield return Seconds(duration - 5f + 0.1f);                   // thẻ 1 hết hạn
        Assert.AreEqual(Mathf.RoundToInt(baseCost * mult), Price(), "Thẻ 1 hết hạn: còn hệ số của thẻ 2");

        // Giá thật khi mua gói đúng bằng giá trên.
        GiveItem("card_box");
        TestHooks.SetMoney(500);
        int expected = Price();
        Cards.ClickBox();
        Assert.AreEqual(500 - expected, Money, "Mua gói trừ đúng giá đã nhân");

        yield return Seconds(5f);
        Assert.AreEqual(baseCost, Price(), "Cả hai hết hạn: giá gốc");
    }

    // ---------- TC-CRD-17 ----------

    [UnityTest]
    public IEnumerator TC_CRD_17_HaiTheNoMoreMiningChongNhau()
    {
        var gate = ShopService.Instance.IconGate;
        float duration = Field<float>("DisableMining", "duration");
        Assert.IsTrue(gate.IsVisible("Mining"));

        Cards.Place(Cards.Asset("DisableMining"));
        Assert.IsFalse(gate.IsVisible("Mining"), "t = 0: ẩn");
        yield return Seconds(5f);
        Cards.Place(Cards.Asset("DisableMining"));
        yield return Seconds(duration - 5f + 0.5f);
        Assert.IsFalse(gate.IsVisible("Mining"), $"t = {duration + 0.5f}: thẻ A hết hạn nhưng thẻ B còn, vẫn ẩn");
        yield return Seconds(5f);
        Assert.IsTrue(gate.IsVisible("Mining"), $"t = {duration + 5.5f}: cả hai hết hạn, hiện lại");
    }

    // ---------- TC-CRD-19 ----------

    [UnityTest]
    public IEnumerator TC_CRD_19_TheCloseTheShop()
    {
        var home = TestHooks.Find<PhoneHome>();
        var shopWindow = TestHooks.GetField<GameObject>(home, "shopWindow");
        var shopIcon = TestHooks.GetField<PhoneIconView[]>(home, "icons").First(i => i != null && i.AppId == "shop");
        float duration = Field<float>("DisableShop", "duration");
        Assert.AreEqual("Shop", Field<string>("DisableShop", "targetAppName"), "Tên trong asset viết hoa “Shop”, icon là “shop”");

        DeviceLayer.Instance.OpenPhone();
        yield return Seconds(0.5f);
        home.OpenApp("shop");
        yield return Frames(2);
        Assert.IsTrue(shopWindow.activeInHierarchy, "Cửa sổ Shop đang mở");

        Cards.Place(Cards.Asset("DisableShop"));
        yield return Frames(2);
        Assert.IsFalse(shopWindow.activeInHierarchy, "Cửa sổ Shop đóng ngay");
        Assert.IsFalse(shopIcon.gameObject.activeSelf, "Icon Shop ẩn");

        yield return Seconds(duration + 0.5f);
        Assert.IsTrue(ShopService.Instance.IconGate.IsVisible("shop"));
        Assert.IsTrue(shopIcon.gameObject.activeSelf, "Hết hạn: icon hiện lại");
    }

    // ---------- TC-CRD-20 ----------

    [UnityTest]
    public IEnumerator TC_CRD_20_TheCableEater()
    {
        var mining = TestHooks.Find<MiningApp>();
        yield return OpenLaptopApp(mining);
        var pickaxe = TestHooks.GetField<PickaxeView>(mining, "pickaxe");
        int hits = 0;
        pickaxe.Hit += () => hits++;
        Assert.IsTrue(Phone.Power.IsUsable, "Phone đang bật");
        float duration = Field<float>("CableEater", "duration");

        Cards.Place(Cards.Asset("CableEater"));
        Assert.IsFalse(Phone.Power.IsUsable, "Phone tắt ngay");
        Phone.PressPower();
        yield return null;
        Assert.AreEqual(PhonePower.State.Off, Phone.Power.Current, "Nút nguồn không phản ứng");
        Assert.IsTrue(LaptopController.IsLocked, "Laptop bị khóa");
        TestHooks.SetField(pickaxe, "_lastHitTime", -100f);
        TestHooks.Call(pickaxe, "TryHit");
        Assert.AreEqual(0, hits, "Cuốc không đào được");

        yield return Seconds(duration + 0.2f);
        Assert.IsFalse(LaptopController.IsLocked, "Hết hạn: Laptop mở khóa");
        Assert.AreEqual(PhonePower.State.Off, Phone.Power.Current, "Phone vẫn tắt (Phụ lục B, B3)");

        Phone.PressPower();
        yield return null;
        Assert.AreEqual(PhonePower.State.Booting, Phone.Power.Current, "Nhấn nguồn: khởi động");
        yield return Seconds(Config.phoneBootSeconds - 0.2f);
        Assert.AreEqual(PhonePower.State.Booting, Phone.Power.Current);
        yield return Seconds(0.4f);
        Assert.AreEqual(PhonePower.State.On, Phone.Power.Current, $"Bật sau {Config.phoneBootSeconds} s");
    }

    // ---------- TC-CRD-21 ----------

    [Test]
    public void TC_CRD_21_TheSpamRobocall_KhoangChoX05()
    {
        var calls = Phone.Calls;
        Cards.Place(Cards.Asset("Robocalls"));
        Assert.AreEqual(0.5f, calls.IntervalScale, "Gọi dày gấp đôi");

        float min = Config.ringIntervalMin * 0.5f, max = Config.ringIntervalMax * 0.5f;
        for (int i = 0; i < 200; i++)
        {
            float gap = (float)TestHooks.Call(calls, "NextInterval");
            Assert.That(gap, Is.InRange(min - 1e-4f, max + 1e-4f), $"Khoảng chờ {i}: {gap} s");
        }

        // Khoảng cách tối thiểu sau một cuộc gọi không đổi.
        calls.ForceRingNow();
        calls.Tick(0.02f);
        Assert.AreEqual(CallScheduler.State.Ringing, calls.Current);
        calls.Decline();
        Assert.AreEqual(CallScheduler.State.Gap, calls.Current);
        Assert.AreEqual(Config.annoyanceMinGap, TestHooks.GetField<float>(calls, "_timer"), "Khoảng cách tối thiểu giữ nguyên");
    }

    // ---------- TC-CRD-22 ----------

    [UnityTest]
    public IEnumerator TC_CRD_22_LopMoPanelMoGoiCheMeter()
    {
        GiveItem("card_box");
        TestHooks.SetMoney(50);
        TestHooks.SetClock(100f);
        Cards.ClickBox();
        yield return Frames(2);
        var panel = TestHooks.GetField<GameObject>(Cards.RevealPanel, "panel");
        Assert.IsTrue(panel.activeInHierarchy, "Panel mở gói đang hiện");
        int afterPack = Money;
        Assert.AreEqual(50 - Config.packCost, afterPack, "Trừ giá gói");

        // Nhấp vào chỗ nút Feed: tia UI dừng ở lớp mờ của panel, không tới nút.
        var feed = TestHooks.GetField<Button>(Meter.Instance, "feedButton");
        var canvas = feed.GetComponentInParent<Canvas>().rootCanvas;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var rt = (RectTransform)feed.transform;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
        Assert.IsNotEmpty(hits, "Phải có thứ gì đó dưới con trỏ");
        Assert.IsFalse(hits[0].gameObject.transform.IsChildOf(feed.transform), $"Lớp trên cùng là {hits[0].gameObject.name}, không phải nút Feed");
        Assert.IsTrue(hits[0].gameObject.transform.IsChildOf(panel.transform), $"Lớp chặn là của panel mở gói ({hits[0].gameObject.name})");

        float clock = ClockService.Instance.RemainingTime;
        yield return Seconds(5f);
        Assert.AreEqual(afterPack, Money, "Meter không nhận tiền");
        Assert.That(ClockService.Instance.RemainingTime, Is.EqualTo(clock - 5f).Within(0.05f), "Clock vẫn chạy ≈ 5 s");
    }
}
