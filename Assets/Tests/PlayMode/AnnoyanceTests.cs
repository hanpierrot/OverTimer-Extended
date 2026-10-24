using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.5.1 của kế hoạch kiểm thử, TC-ANN-1…8: Phone, ITRS (thuế) và AnnoyanceManager (một phiền nhiễu một lúc).
/// Mỗi khung hình = 0,02 s (UseFixedFrameRate(50)).
/// </summary>
public class AnnoyanceTests : GameSceneTest
{
    private const int Fps = 50;

    private static Phone Phone => Phone.Instance;
    private static ITRSService Itrs => ITRSService.Instance;
    private static AnnoyanceManager Annoyance => AnnoyanceManager.Instance;

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        yield return null;
    }

    [Test]
    public void GiaTriTrongGameConfigKhopKeHoach()
    {
        Assert.AreEqual(30f, Config.ringIntervalMin);
        Assert.AreEqual(60f, Config.ringIntervalMax);
        Assert.AreEqual(1.5f, Config.ringEffectMultiplier);
        Assert.AreEqual(10f, Config.maxRingDuration);
        Assert.AreEqual(45f, Config.billInterval);
        Assert.AreEqual(12f, Config.annoyanceMinGap);
        Assert.AreEqual(20, Config.billMinimum);
        Assert.AreEqual(0.2f, Config.billRate);
        Assert.AreEqual(2f, Config.shortfallPenalty);
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
        var gaps = new float[200];
        for (int i = 0; i < gaps.Length; i++)
        {
            TestHooks.Call(Phone, "ScheduleNextRing");
            gaps[i] = TestHooks.GetField<float>(Phone, "nextRingTimer");
        }

        Assert.That(gaps.Min(), Is.GreaterThanOrEqualTo(30f));
        Assert.That(gaps.Max(), Is.LessThanOrEqualTo(60f));
        // Phân bố đều trên [30, 60]: độ lệch chuẩn 30/√12 ≈ 8,66; sai số trung bình 3σ/√200 ≈ 1,84 s.
        Assert.That(gaps.Average(), Is.EqualTo(45f).Within(1.84f));
    }

    [UnityTest]
    public IEnumerator TC_ANN_1b_KhiReo_ClockNhanh15VaTuDungSau10Giay()
    {
        TestHooks.SetField(Phone, "nextRingTimer", 0.01f);
        yield return null;
        Assert.IsTrue(Phone.IsRinging);
        Assert.AreEqual(1.5f, ClockService.Instance.TickMultiplier);

        yield return Frames(10 * Fps + 2);

        Assert.IsFalse(Phone.IsRinging, "Phone phải tự dừng sau tối đa 10 s");
        Assert.AreEqual(1f, ClockService.Instance.TickMultiplier);
    }

    // ---------- TC-ANN-2 ----------

    [UnityTest]
    public IEnumerator TC_ANN_2_ITRSDoiKhiDangBan_ThuLaiMoiGiay()
    {
        Assert.IsTrue(Annoyance.TryBegin("test: sự kiện khác đang chạy"));
        TestHooks.SetField(Itrs, "timer", Config.billInterval - 0.05f);

        yield return Frames(5);                                  // đến giây thứ 45
        Assert.IsFalse(Itrs.HasPendingAssessment, "Đang bận: chưa được phát hóa đơn");
        Assert.AreEqual(Config.billInterval - 1f, TestHooks.GetField<float>(Itrs, "timer"), 0.05f,
            "Phải hẹn thử lại sau khoảng 1 s");

        yield return Frames(Fps + 2);                            // 1 s sau: vẫn bận
        Assert.IsFalse(Itrs.HasPendingAssessment);

        Annoyance.End("test");
        TestHooks.SetField(Annoyance, "lastEventEndTime", -999f);  // bỏ qua khoảng cách 12 s (đã có TC-ANN-4)
        yield return Frames(Fps + 2);
        Assert.IsTrue(Itrs.HasPendingAssessment, "Rảnh thì phải phát hóa đơn trong vòng 1 s");
    }

    // ---------- TC-ANN-3 ----------

    [UnityTest]
    public IEnumerator TC_ANN_3_PhoneVaITRSCungKhungHinh_ChiMotSuKien()
    {
        TestHooks.SetField(Phone, "nextRingTimer", 0.01f);
        TestHooks.SetField(Itrs, "timer", Config.billInterval - 0.01f);

        yield return null;

        Assert.IsTrue(Phone.IsRinging ^ Itrs.HasPendingAssessment,
            $"Chỉ được một sự kiện: Phone reo = {Phone.IsRinging}, có hóa đơn = {Itrs.HasPendingAssessment}");
    }

    // ---------- TC-ANN-4 ----------

    [Test]
    public void TC_ANN_4_KhoangCachToiThieu12Giay()
    {
        TestHooks.SetField(Annoyance, "lastEventEndTime", Time.time - 11f);
        Assert.IsFalse(Annoyance.CanStart, "11 s sau sự kiện trước: phải từ chối");

        float last = Time.time - 12f;
        if (Time.time - last < 12f) last -= 1e-5f;               // bù làm tròn số thực để khoảng cách đúng bằng 12 s
        TestHooks.SetField(Annoyance, "lastEventEndTime", last);
        Assert.IsTrue(Annoyance.CanStart, "12 s sau sự kiện trước: phải cho phép");
    }

    // ---------- TC-ANN-5 ----------

    [TestCase(50, 20)]
    [TestCase(100, 20)]
    [TestCase(200, 40)]
    public void TC_ANN_5_GiaTriHoaDonThue(int earned, int expectedDue)
    {
        Assert.AreEqual(expectedDue, IssueBill(earned).amountDue);
    }

    // ---------- TC-ANN-6 ----------

    [UnityTest] public IEnumerator TC_ANN_6a_Money29_NutPayBiVoHieu() => PayButtonWith(29);
    [UnityTest] public IEnumerator TC_ANN_6b_Money30_NutPayDuocBat() => PayButtonWith(30);

    private static IEnumerator PayButtonWith(int money)
    {
        var bill = IssueBill(150);                               // 20% × 150 = $30
        Assert.AreEqual(30, bill.amountDue);
        var tax = TestHooks.Find<Tax>();
        tax.Show(bill);
        TestHooks.SetMoney(money);

        yield return null;

        bool interactable = TestHooks.GetField<UnityEngine.UI.Button>(tax, "payButton").interactable;
        Assert.AreEqual(money >= 30, interactable, $"Money = {money}, hóa đơn $30");
    }

    // ---------- TC-ANN-7 ----------

    [Test]
    public void TC_ANN_7a_Ignore_PhatGap2LanThoiGian()
    {
        var tax = TestHooks.Find<Tax>();
        tax.Show(IssueBill(150));
        TestHooks.SetClock(100f);

        TestHooks.ClickButton(tax, "ignoreButton");

        Assert.AreEqual(40f, ClockService.Instance.RemainingTime, "Phạt 30 × 2 = 60 s");
        Assert.IsFalse(GameManager.Instance.IsGameOver);
    }

    [Test]
    public void TC_ANN_7b_IgnoreKhiKhongDuThoiGian_Thua()
    {
        var tax = TestHooks.Find<Tax>();
        tax.Show(IssueBill(150));
        TestHooks.SetClock(50f);

        TestHooks.ClickButton(tax, "ignoreButton");

        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.ClockLoss, GameManager.Instance.LastEndReason);
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
