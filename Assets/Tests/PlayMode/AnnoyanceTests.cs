using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.5.1 của kế hoạch kiểm thử, TC-ANN-1…8: Phone (CallScheduler: một cuộc gọi một lúc) và ITRS (thuế). AnnoyanceManager đã bị loại bỏ.
/// Mỗi khung hình = 0,02 s (UseFixedFrameRate(50)).
/// </summary>
public class AnnoyanceTests : GameSceneTest
{
    private const int Fps = 50;

    private static PhoneController Phone => PhoneController.Instance;
    private static ITRSService Itrs => ITRSService.Instance;

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        yield return null;
    }

    /// <summary>Phát hóa đơn ngay với số tiền kiếm được cho trước (bỏ qua chờ 45 s).</summary>
    private static ITRSService.Assessment IssueBill(int earned)
    {
        TestHooks.SetField(Itrs, "earnedSinceLastBill", earned);
        TestHooks.Call(Itrs, "IssueAssessment");
        return Itrs.CurrentAssessment;
    }

    // ---------- TC-ANN-1 ----------

    [Test]
    public void TC_ANN_1a_KhoangCachGiuaCacLanReo()
    {
        // Khoảng cách nằm trong CallScheduler (lớp thuần C#): hàm khởi tạo bốc khoảng chờ đầu tiên.
        var cfg = new PhoneConfig();
        var rng = new System.Random(1);
        var gaps = new float[200];
        for (int i = 0; i < gaps.Length; i++)
            gaps[i] = TestHooks.GetField<float>(new CallScheduler(cfg, rng), "_timer");

        Assert.That(gaps.Min(), Is.GreaterThanOrEqualTo(30f));
        Assert.That(gaps.Max(), Is.LessThanOrEqualTo(60f));
        // Phân bố đều trên [30, 60]: độ lệch chuẩn 30/√12 ≈ 8,66; sai số trung bình 3σ/√200 ≈ 1,84 s.
        Assert.That(gaps.Average(), Is.EqualTo(45f).Within(1.84f));
    }

    [UnityTest]
    public IEnumerator TC_ANN_1b_KhiReo_ClockNhanh15VaTuDungSau10Giay()
    {
        Phone.Calls.ForceRingNow();
        yield return null;
        Assert.IsTrue(Phone.IsRinging);
        Assert.AreEqual(1.5f, ClockService.Instance.TickMultiplier);

        yield return Frames(10 * Fps + 2);

        Assert.IsFalse(Phone.IsRinging, "Phone phải tự dừng sau tối đa 10 s");
        Assert.AreEqual(1f, ClockService.Instance.TickMultiplier);
    }

    // ---------- TC-ANN-2 ----------

    [UnityTest]
    public IEnumerator TC_ANN_2_KhongPhatHoaDonMoiKhiDangCoHoaDonMo()
    {
        var first = IssueBill(150);
        int issued = 0;
        Itrs.OnAssessmentIssued += _ => issued++;
        TestHooks.SetField(Itrs, "timer", Config.billInterval - 0.05f);

        yield return Frames(Fps * 2);

        Assert.AreEqual(0, issued, "P6: không phát hóa đơn mới chồng lên hóa đơn chưa trả");
        Assert.AreEqual(first.amountDue, Itrs.CurrentAssessment.amountDue);
    }

    // ---------- TC-ANN-3 ----------

    [UnityTest]
    public IEnumerator TC_ANN_3_HoaDonKhongBiCuocGoiChan_CaHaiCungXuatHien()
    {
        Phone.Calls.ForceRingNow();
        TestHooks.SetField(Itrs, "timer", Config.billInterval - 0.01f);

        yield return Frames(2);

        Assert.IsTrue(Phone.IsRinging);
        Assert.IsTrue(Itrs.HasPendingAssessment, "I8: hóa đơn là thông báo, không bị cuộc gọi chặn");
    }

    // ---------- TC-ANN-4 ----------

    [Test]
    public void TC_ANN_4_KhoangCachToiThieu12GiayGiuaHaiCuocGoi()
    {
        var cfg = new PhoneConfig { ringIntervalMin = 1f, ringIntervalMax = 1f };
        var s = new CallScheduler(cfg, new System.Random(1));

        s.Tick(1f);
        Assert.AreEqual(CallScheduler.State.Ringing, s.Current);
        Assert.IsTrue(s.Decline());
        Assert.AreEqual(CallScheduler.State.Gap, s.Current);

        s.Tick(11.9f);
        Assert.AreEqual(CallScheduler.State.Gap, s.Current, "11,9 s sau cuộc gọi trước: chưa được reo");
        s.Tick(0.2f);
        Assert.AreEqual(CallScheduler.State.Waiting, s.Current, "đủ 12 s: được phép lên lịch cuộc gọi kế");
    }

    // ---------- TC-ANN-5 ----------

    // Không dùng [TestCase]: fixture có test tham số hóa không nạp được trong bản Player WebGL ("DllNotFoundException: libc").
    [Test]
    public void TC_ANN_5_GiaTriHoaDonThue()
    {
        foreach (var (earned, expectedDue) in new[] { (50, 20), (100, 20), (200, 40) })
            Assert.AreEqual(expectedDue, IssueBill(earned).amountDue, $"kiếm được ${earned}");
    }

    // ---------- TC-ANN-6 ----------

    [UnityTest] public IEnumerator TC_ANN_6a_Money29_NutPayBiVoHieu() => PayButtonWith(29);
    [UnityTest] public IEnumerator TC_ANN_6b_Money30_NutPayDuocBat() => PayButtonWith(30);

    private static IEnumerator PayButtonWith(int money)
    {
        var bill = IssueBill(150);                               // 20% × 150 = $30
        Assert.AreEqual(30, bill.amountDue);
        var tax = TestHooks.Find<Tax>();
        tax.Show();
        TestHooks.SetMoney(money);

        yield return null;

        bool interactable = TestHooks.GetField<UnityEngine.UI.Button>(tax, "payButton").interactable;
        Assert.AreEqual(money >= 30, interactable, $"Money = {money}, hóa đơn $30");
    }

    // ---------- TC-ANN-7 ----------

    [Test]
    public void TC_ANN_7a_HetHan_PhatGap2LanThoiGian()
    {
        IssueBill(150);                                          // $30
        TestHooks.SetClock(100f);

        TestHooks.GetField<TaxDeadline>(Itrs, "_deadline").Tick(Config.taxPayWindow);   // cửa sổ 30 s hết

        Assert.AreEqual(40f, ClockService.Instance.RemainingTime, "Phạt 30 × 2 = 60 s");
        Assert.IsFalse(Itrs.HasPendingAssessment);
        Assert.IsFalse(GameManager.Instance.IsGameOver);
    }

    [Test]
    public void TC_ANN_7b_HetHanKhiKhongDuThoiGian_Thua()
    {
        IssueBill(150);
        TestHooks.SetClock(50f);

        TestHooks.GetField<TaxDeadline>(Itrs, "_deadline").Tick(Config.taxPayWindow);

        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.ClockLoss, GameManager.Instance.LastEndReason);
    }

    [Test]
    public void TC_ANN_7c_TraDuTienTrongHan_KhongBiPhat()
    {
        IssueBill(150);
        TestHooks.SetMoney(30);
        TestHooks.SetClock(100f);

        Assert.IsTrue(Itrs.Pay());

        Assert.AreEqual(0, MoneyService.Instance.Current);
        Assert.AreEqual(100f, ClockService.Instance.RemainingTime);
        Assert.IsFalse(Itrs.HasPendingAssessment);
    }

    // ---------- TC-ANN-8 ----------

    [Test]
    public void TC_ANN_8a_TheFvckTheTax_HoaDonKeBang0()
    {
        Cards.Place(Cards.Asset("TaxExemption"));
        TestHooks.SetField(Itrs, "earnedSinceLastBill", 150);

        Itrs.ForceBillNow();

        Assert.AreEqual(0, Itrs.CurrentAssessment.amountDue);
    }

    [Test]
    public void TC_ANN_8b_TheTaxIncrease_KhongCoHoaDon_HienNgay()
    {
        Assert.IsFalse(Itrs.HasPendingAssessment);

        Cards.Place(Cards.Asset("Audit"));

        Assert.IsTrue(Itrs.HasPendingAssessment, "Hóa đơn phải xuất hiện ngay");
    }

    [Test]
    public void TC_ANN_8c_TheTaxIncrease_DangCoHoaDon30_ThanhGapDoi()
    {
        IssueBill(150);
        Assert.AreEqual(30, Itrs.CurrentAssessment.amountDue);

        Cards.Place(Cards.Asset("Audit"));

        Assert.AreEqual(60, Itrs.CurrentAssessment.amountDue);
    }
}
