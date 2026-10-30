using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.5.3 của kế hoạch kiểm thử (Play Mode): cuộc gọi trên Phone, trên scene thật. TC-PHN-10 … 16, TC-PHN-20.
/// Phần logic thuần (CallScheduler, CallerRoller, CallDialogue) ở EditMode/PhoneLogicTests.
/// "Cheat ép cuộc gọi" = đặt trọng số người gọi chỉ còn một loại rồi CallScheduler.ForceRingNow (cùng đường với cheat menu).
/// Tiền thưởng, thời gian chờ và gia hạn đọc từ GameConfig.
/// Mỗi khung hình = 0,02 s giờ game (UseFixedFrameRate(50)). Các hạn chờ "giờ thực" của PhoneController (khách chờ trả lời,
/// tự cúp máy) đếm bằng Time.unscaledDeltaTime, không bị captureFramerate cố định, nên test chờ chúng theo Time.unscaledTime.
/// </summary>
public class PhoneCallTests : GameSceneTest
{
    private const int Fps = 50;

    private static PhoneController Phone => PhoneController.Instance;
    private static ClockService Clock => ClockService.Instance;
    private static ITRSService Itrs => ITRSService.Instance;
    private static int Money => MoneyService.Instance.Current;

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        TestHooks.SetClock(500f);
        yield return null;
    }

    private static IEnumerator Seconds(float s) => Frames(Mathf.RoundToInt(s * Fps));

    private static IEnumerator RealSeconds(float s)
    {
        float end = Time.unscaledTime + s;
        while (Time.unscaledTime < end) yield return null;
    }

    /// <summary>Cho Phone reo ngay với đúng một loại người gọi.</summary>
    private static IEnumerator Ring(CallTruth truth)
    {
        var cfg = Phone.Config;
        var saved = (cfg.weightTelemarketer, cfg.weightClient, cfg.weightITRS, cfg.weightUnknown);
        cfg.weightTelemarketer = truth == CallTruth.Telemarketer ? 1 : 0;
        cfg.weightClient = truth == CallTruth.Client ? 1 : 0;
        cfg.weightITRS = truth == CallTruth.ITRS ? 1 : 0;
        cfg.weightUnknown = 0;
        Phone.Calls.ForceRingNow();
        for (int i = 0; i < 10 && !Phone.IsRinging; i++) yield return null;
        // Trả lại trọng số thật: cuộc gọi kế tiếp (sau khi ca kết thúc) không được bốc từ bộ trọng số rỗng.
        (cfg.weightTelemarketer, cfg.weightClient, cfg.weightITRS, cfg.weightUnknown) = saved;
        Assert.IsTrue(Phone.IsRinging, "Phone phải reo");
        Assert.AreEqual(truth, Phone.RingingCall.truth);
    }

    private static IEnumerator RingAndAnswer(CallTruth truth)
    {
        yield return Ring(truth);
        Phone.Answer();
        Assert.IsNotNull(Phone.ActiveCall, "Đã nghe máy");
        yield return null;
    }

    /// <summary>Bấm nút trả lời cho tới khi câu cuối hiện ra (chưa cúp máy).</summary>
    private static void ReplyUntilFinal()
    {
        for (int i = 0; i < 20 && !Phone.Dialogue.IsFinal; i++)
        {
            Assert.IsTrue(Phone.ReplyEnabled, "Nút trả lời phải bấm được");
            Phone.Reply();
        }
        Assert.IsTrue(Phone.Dialogue.IsFinal, "Phải tới câu cuối");
    }

    private static int IssueBill()
    {
        TestHooks.SetField(Itrs, "earnedSinceLastBill", 150);
        Itrs.ForceBillNow();
        Assert.IsTrue(Itrs.HasPendingAssessment);
        return Itrs.CurrentAmountDue;
    }

    private static TaxDeadline Deadline => TestHooks.GetField<TaxDeadline>(Itrs, "_deadline");

    // ---------- TC-PHN-10 ----------

    [UnityTest]
    public IEnumerator TC_PHN_10_CuocGoiClient_DungClockKhoaLaptop_TraTien()
    {
        TestHooks.SetClock(100f);
        TestHooks.SetMoney(0);
        yield return RingAndAnswer(CallTruth.Client);
        yield return Seconds(0.5f);                          // để DeviceLayer mở xong Phone

        float clock = Clock.RemainingTime;
        yield return Seconds(1f);
        Assert.IsFalse(Clock.IsRunning, "Clock dừng từ lúc ACCEPT");
        Assert.AreEqual(clock, Clock.RemainingTime, "Clock không trôi trong cuộc gọi");
        Assert.IsTrue(LaptopController.IsLocked, "Laptop bị khóa");
        Assert.IsFalse(InputManager.Instance.IsInputEnabled, "Vật trên bàn không nhấp được (I5)");
        Assert.IsTrue(DeviceLayer.Instance.IsPhoneLockedOpen, "Không đóng được Phone");
        DeviceLayer.Instance.ClosePhone();
        Assert.AreEqual(DeviceLayer.Mode.Phone, DeviceLayer.Instance.Current, "ClosePhone bị bỏ qua");
        Assert.IsFalse(Phone.Calls.Decline(), "DECLINE không dùng được khi đang nói chuyện");

        ReplyUntilFinal();
        Assert.AreEqual(Config.clientGigMoney, Money, "Câu cuối: nhận tiền khách trả");
        Assert.AreEqual("Hang up.", Phone.ReplyLabel, "Nút ở câu cuối là “Hang up.”");

        Phone.Reply();                                        // Hang up.
        yield return Seconds(0.5f);
        Assert.IsNull(Phone.ActiveCall, "Đã cúp máy");
        Assert.IsTrue(Clock.IsRunning, "Clock chạy lại");
        Assert.IsFalse(LaptopController.IsLocked, "Laptop mở khóa");
        Assert.AreEqual($"+${Config.clientGigMoney}", Phone.CallHistory[0].result, "Lịch sử cuộc gọi ghi “+$tiền”");
    }

    // ---------- TC-PHN-11 ----------

    [UnityTest]
    public IEnumerator TC_PHN_11_ClientChoQuaLau_TuCupMay()
    {
        TestHooks.SetMoney(0);
        yield return RingAndAnswer(CallTruth.Client);
        string noReply = Phone.Catalog.NoReplyLine;

        yield return RealSeconds(Config.beneficialCallTimeout - 0.3f);
        Assert.AreNotEqual(noReply, Phone.CallerLine, "Chưa hết thời gian chờ");
        yield return RealSeconds(0.6f);
        Assert.AreEqual(noReply, Phone.CallerLine, "Hiện câu “Are you there?”");
        Assert.IsFalse(Phone.ReplyEnabled, "Nút trả lời xám");
        Phone.Reply();
        Assert.IsNotNull(Phone.ActiveCall, "Bấm trả lời lúc này không có tác dụng");

        yield return RealSeconds(Config.callGoodbyeSeconds + 0.3f);
        Assert.IsNull(Phone.ActiveCall, "Khách cúp máy");
        Assert.AreEqual(0, Money, "Không có tiền");
        Assert.IsTrue(Clock.IsRunning, "Clock chạy lại");
    }

    // ---------- TC-PHN-12 ----------

    [UnityTest]
    public IEnumerator TC_PHN_12_CuocGoiTelemarketer_ClockVanChay_KhongThuong()
    {
        TestHooks.SetClock(100f);
        TestHooks.SetMoney(0);
        yield return RingAndAnswer(CallTruth.Telemarketer);

        float clock = Clock.RemainingTime;
        yield return Seconds(Config.telemarketerPitch - 0.5f);
        Assert.IsTrue(Clock.IsRunning, "Clock vẫn chạy");
        Assert.Less(Clock.RemainingTime, clock, "Clock trôi trong cuộc gọi Telemarketer");
        Assert.IsFalse(LaptopController.IsLocked, "Laptop không bị khóa");
        Assert.IsFalse(Phone.ReplyEnabled, "Nút trả lời đầu xám trong lúc chào hàng");

        yield return Seconds(1f);
        Assert.IsTrue(Phone.ReplyEnabled, $"Sau {Config.telemarketerPitch} s game nút bấm được");

        ReplyUntilFinal();
        Phone.Reply();
        yield return null;
        Assert.IsNull(Phone.ActiveCall);
        Assert.AreEqual(0, Money, "Không có thưởng");
    }

    // ---------- TC-PHN-13 ----------

    [UnityTest]
    public IEnumerator TC_PHN_13a_CuocGoiITRS_CoHoaDon_GiaHan()
    {
        IssueBill();
        yield return RingAndAnswer(CallTruth.ITRS);
        TestHooks.SetAutoProperty(Deadline, "SecondsLeft", 10f);

        ReplyUntilFinal();

        Assert.AreEqual(10f + Config.itrsCallExtension, Itrs.SecondsLeft, "Hóa đơn được gia hạn");
        Phone.Reply();
        yield return null;
        Assert.IsNull(Phone.ActiveCall);
    }

    [UnityTest]
    public IEnumerator TC_PHN_13b_CuocGoiITRS_KhongCoHoaDon_HoaDonKeDenMuonHon()
    {
        int amount = IssueBill();
        yield return Ring(CallTruth.ITRS);                  // ITRS chỉ gọi khi có hóa đơn; trả xong rồi mới nghe máy
        TestHooks.SetMoney(amount);
        Assert.IsTrue(Itrs.Pay());
        Phone.Answer();
        yield return null;

        ReplyUntilFinal();
        Assert.AreEqual(Phone.Catalog.ItrsNoBillLine, Phone.CallerLine);
        Assert.AreEqual(Config.itrsCallExtension, Itrs.NextBillBonus, "Thời gian cộng sang hóa đơn kế");
        Phone.Reply();
        yield return null;

        // Hóa đơn kế đến khi bộ đếm của ITRS đạt billInterval + itrsCallExtension (không phải billInterval).
        float lastTimer = 0f;
        int frames = 0;
        float limit = (Config.billInterval + Config.itrsCallExtension + 5f) * Fps;
        while (!Itrs.HasPendingAssessment && frames++ < limit)
        {
            lastTimer = TestHooks.GetField<float>(Itrs, "timer");
            yield return null;
        }
        Assert.IsTrue(Itrs.HasPendingAssessment, "Có hóa đơn kế");
        Assert.That(lastTimer, Is.GreaterThan(Config.billInterval + Config.itrsCallExtension - 0.1f),
            $"Hóa đơn kế sau {Config.billInterval} + {Config.itrsCallExtension} s");
        Assert.AreEqual(Config.taxPayWindow, Itrs.SecondsLeft, 0.05f, "Hạn trả không đổi");
        Assert.AreEqual(0f, Itrs.NextBillBonus, "Phần cộng thêm đã dùng hết");
    }

    // ---------- TC-PHN-14 ----------

    [UnityTest]
    public IEnumerator TC_PHN_14_TuChoiCuocGoi()
    {
        TestHooks.SetMoney(10);
        yield return Ring(CallTruth.Client);
        Assert.AreEqual(Config.ringEffectMultiplier, Clock.TickMultiplier, "Đang reo: Clock nhanh hơn");
        int inbox = Phone.Inbox.All.Count;

        Phone.Decline();
        yield return null;

        Assert.IsFalse(Phone.IsRinging, "Hết reo");
        Assert.AreEqual(1f, Clock.TickMultiplier, "Clock về ×1");
        Assert.IsNull(Phone.ActiveCall);
        Assert.AreEqual(10, Money, "Không mất tiền");
        Assert.IsTrue(Clock.IsRunning, "Không mất giờ: Clock chạy bình thường");
        Assert.AreEqual(CallEnd.Declined, Phone.CallHistory[0].outcome, "Lịch sử có cuộc bị từ chối");
        Assert.AreEqual(inbox + 1, Phone.Inbox.All.Count);
        Assert.IsTrue(Phone.Inbox.All.Any(m => m.kind == MessageKind.MissedCall && m.title == "Declined call"), "Inbox có cuộc bị từ chối");
    }

    // ---------- TC-PHN-15 ----------

    [UnityTest]
    public IEnumerator TC_PHN_15_ImLangOCauCuoi_TienTraNgay_TuCup()
    {
        TestHooks.SetMoney(0);
        yield return RingAndAnswer(CallTruth.Client);

        ReplyUntilFinal();
        Assert.AreEqual(Config.clientGigMoney, Money, "Nhận tiền ngay khi câu cuối hiện");

        yield return RealSeconds(Config.callHangUpDelay - 0.3f);
        Assert.IsNotNull(Phone.ActiveCall, "Chưa tự cúp");
        yield return RealSeconds(0.6f);
        Assert.IsNull(Phone.ActiveCall, $"Tự cúp sau {Config.callHangUpDelay} s");
        Assert.AreEqual(Config.clientGigMoney, Money, "Không cộng thêm");
        Assert.IsTrue(Clock.IsRunning, "Clock chạy lại");
    }

    // ---------- TC-PHN-16 ----------

    [UnityTest]
    public IEnumerator TC_PHN_16_LichSuCuocGoi_Giu30DongMoiNhat()
    {
        int inbox = Phone.Inbox.All.Count(m => m.kind == MessageKind.MissedCall);
        int answered = 0, declined = 0, missed = 0;
        for (int i = 0; i < 31; i++)
        {
            yield return Ring(CallTruth.Client);
            switch (i % 3)
            {
                case 0:
                    Phone.Answer();
                    ReplyUntilFinal();
                    Phone.Reply();
                    answered++;
                    break;
                case 1: Phone.Decline(); declined++; break;
                default: Phone.Calls.Abort(); missed++; break;   // như khi hết 10 s reo mà không ai nghe
            }
            Assert.IsNull(Phone.ActiveCall);
        }

        var history = Phone.CallHistory;
        Assert.AreEqual(30, history.Count, "Chỉ giữ 30 dòng");
        for (int i = 1; i < history.Count; i++) Assert.Greater(history[i - 1].id, history[i].id, "Mới nhất ở đầu");
        Assert.AreEqual(CallEnd.Answered, history[0].outcome, "Cuộc thứ 31 (trả lời) ở đầu");
        Assert.AreEqual(declined + missed, Phone.Inbox.All.Count(m => m.kind == MessageKind.MissedCall) - inbox,
            "Cuộc nhỡ và từ chối vào Inbox, cuộc đã trả lời thì không");
        Assert.AreEqual(11, answered);
    }

    // ---------- TC-PHN-20 ----------

    [UnityTest]
    public IEnumerator TC_PHN_20_GameOverRestartGiuaCuocGoi()
    {
        yield return RingAndAnswer(CallTruth.Client);
        Assert.IsFalse(Clock.IsRunning);
        Assert.IsTrue(LaptopController.IsLocked);

        GameManager.Instance.GameOver(EndReason.ClockLoss);
        yield return null;
        var oldPhone = Phone;
        GameManager.Instance.Restart();
        while (Phone == null || Phone == oldPhone) yield return null;
        yield return Frames(2);

        Assert.IsFalse(GameManager.Instance.IsGameOver);
        Assert.IsTrue(Clock.IsRunning, "Ván mới: Clock chạy");
        Assert.IsFalse(LaptopController.IsLocked, "Laptop không khóa");
        Assert.IsTrue(Phone.Power.IsUsable, "Phone bật");
        Assert.IsNull(Phone.ActiveCall, "Không còn cuộc gọi treo");
        Assert.IsFalse(Phone.IsRinging);
        Assert.IsTrue(InputManager.Instance.IsInputEnabled, "Bàn nhận nhấp chuột");
    }
}
