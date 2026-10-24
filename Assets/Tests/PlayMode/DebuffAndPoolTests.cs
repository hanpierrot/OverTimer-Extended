using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Các thay đổi về thẻ ngày 2026-10-22 (PROJECT.md A26, A27): thẻ của app có license chỉ rơi ra khi đã có license
/// (<c>CardSO.IsAvailable</c>), phí dismiss khi cầm đồ một debuff còn đang chạy, và cổng timeScale của Meter.
/// </summary>
public class DebuffAndPoolTests : GameSceneTest
{
    private static int Money => MoneyService.Instance.Current;
    private static TMP_Text HintLabel => TestHooks.GetField<TMP_Text>(HintService.Instance, "label");

    private static void SetLicense(string app, bool owned) => ShopService.Instance.IconGate.SetLicensed(app, owned);

    // ---------- CardSO.IsAvailable ----------

    [Test]
    public void TheAnIconApp_ChiKhaDungKhiCoLicense()
    {
        var card = Cards.Asset("DisableBilliards");

        SetLicense("Billiards", false);
        Assert.IsFalse(card.IsAvailable, "Chưa có license Billiards thì thẻ vô nghĩa, không được rơi ra");

        SetLicense("Billiards", true);
        Assert.IsTrue(card.IsAvailable);
    }

    [Test]
    public void TheAnIconApp_KhongCanLicense_VoiAppMienPhi()
    {
        Assert.IsTrue(Cards.Asset("DisableMining").IsAvailable, "Mining là app khởi đầu");
    }

    [Test]
    public void TheDoiGia_ChiKhaDungKhiCoLicenseCuaApp()
    {
        var blackjack = Cards.Asset("BlackjackCostModifier");
        var billiards = Cards.Asset("BilliardsCostModifier");
        SetLicense("Blackjack", false);
        SetLicense("Billiards", false);
        Assert.IsFalse(blackjack.IsAvailable);
        Assert.IsFalse(billiards.IsAvailable);

        SetLicense("Blackjack", true);
        Assert.IsTrue(blackjack.IsAvailable);
        Assert.IsFalse(billiards.IsAvailable, "License của app khác không mở thẻ này");

        Assert.IsTrue(Cards.Asset("TicketCostModifier").IsAvailable, "Thẻ giá vé không cần license");
    }

    [Test]
    public void MuaGoi_KhongBaoGioRaTheKhongKhaDung()
    {
        SetLicense("Blackjack", false);
        SetLicense("Billiards", false);
        var box = Cards.Box;

        for (int i = 0; i < 500; i++)
        {
            var rolled = (CardSO)TestHooks.Call(box, "RollCard", Config);
            Assert.IsTrue(rolled.IsAvailable, $"Lần {i + 1}: rơi ra thẻ {rolled.name} không khả dụng");
        }
    }

    // ---------- Phí dismiss debuff đang chạy ----------

    [Test]
    public void DebuffDangChay_CamDo_TonPhiVaKhongNhanTien()
    {
        var card = Cards.Place(Cards.Asset("Butterfingers"));
        Assert.IsTrue(card.IsDebuffActive);
        Assert.AreEqual(0, card.PawnValue, "Debuff đang chạy không có giá cầm đồ");
        TestHooks.SetMoney(20);

        Pawning.PawnOnto(card);

        Assert.AreEqual(20 - Config.debuffDismissFee, Money, "Trả phí dismiss, không nhận tiền");
        Assert.IsFalse(card.gameObject.activeSelf, "Thẻ đã bị cầm");
    }

    [Test]
    public void DebuffDangChay_ThieuTienPhi_TuChoiGoiYVaDoiViTri()
    {
        var card = Cards.Place(Cards.Asset("Butterfingers"));
        int broke = Config.debuffDismissFee - 1;
        TestHooks.SetMoney(broke);
        Vector3 before = card.transform.position;
        HintLabel.text = "";

        Pawning.PawnOnto(card);

        Assert.AreEqual(broke, Money, "Không đủ tiền thì không trừ gì");
        Assert.IsTrue(card.gameObject.activeSelf, "Thẻ ở lại bàn");
        Assert.IsTrue(card.IsDebuffActive, "Hiệu ứng vẫn chạy");
        Assert.AreEqual(string.Format(HintCatalog.Get(HintId.DebuffDismissCost), Config.debuffDismissFee), HintLabel.text);
        Assert.AreNotEqual(before, card.transform.position, "Thẻ được chuyển sang chỗ khác trong vùng đặt");
    }

    [Test]
    public void DebuffDaHetHieuLuc_CamDo_NhanGiaThuong_KhongTonPhi()
    {
        var card = Cards.Place(Cards.Asset("Butterfingers"));
        card.EndEffect();
        Assert.IsFalse(card.IsDebuffActive);
        TestHooks.SetMoney(0);

        Pawning.PawnOnto(card);

        Assert.AreEqual(card.Data.pawnValue, Money, "Thẻ đã hết hiệu lực pawn như thẻ thường");
    }

    [Test]
    public void TheKhongPhaiDebuff_CamDoKhiDangChay_KhongTonPhi()
    {
        var card = Cards.Place(Cards.Asset("SnailSpeed"));
        TestHooks.SetMoney(0);

        Pawning.PawnOnto(card);

        Assert.AreEqual(card.Data.pawnValue, Money);
    }

    // ---------- Cổng timeScale của Meter ----------

    [Test]
    public void Meter_KhongHoatDongKhiGameDangDungHinh()
    {
        TestHooks.SetMoney(50);
        TestHooks.SetClock(100f);
        GameManager.Instance.PauseGame();                       // như khi CardPanel hoặc hướng dẫn đầu game đang mở
        try
        {
            TestHooks.ClickMeter();

            Assert.AreEqual(50, Money, "Đang dừng hình: Feed không được tiêu tiền");
            Assert.AreEqual(100f, ClockService.Instance.RemainingTime);
        }
        finally
        {
            GameManager.Instance.ResumeGame();
        }

        TestHooks.ClickMeter();

        Assert.AreEqual(0, Money, "Hết dừng hình thì Meter dùng lại được");
        Assert.AreEqual(150f, ClockService.Instance.RemainingTime);
    }
}
