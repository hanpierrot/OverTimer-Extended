using System.Collections;
using NUnit.Framework;
using Overtime.Blackjack;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.3.2 của kế hoạch kiểm thử, TC-BJK-7, 8: phần BlackjackApp (ante trả bằng giây Clock, đóng ứng dụng giữa ván).
/// Luật chơi (TC-BJK-1…6, 9) đã kiểm thử ở Edit Mode trên BlackjackGame.
/// </summary>
public class BlackjackAppTests : GameSceneTest
{
    private BlackjackApp _app;

    [UnitySetUp]
    public IEnumerator OpenBlackjack()
    {
        UseFixedFrameRate(50);
        _app = TestHooks.Find<BlackjackApp>();
        yield return OpenLaptopApp(_app);
    }

    private BlackjackGame Game => TestHooks.GetField<BlackjackGame>(_app, "_game");

    private void ClickDeal() => TestHooks.ClickButton(_app, "dealButton");

    // ---------- TC-BJK-7 ----------

    [Test]
    public void TC_BJK_7a_Clock34_KhongDuThoiGian_TuChoi()
    {
        TestHooks.SetClock(34f);

        ClickDeal();

        Assert.AreEqual(34f, ClockService.Instance.RemainingTime);
        Assert.AreEqual(HandState.Idle, Game.State);
    }

    [Test]
    public void TC_BJK_7b_Clock35_ChoPhep_TruAnte()
    {
        TestHooks.SetClock(35f);

        ClickDeal();

        Assert.AreEqual(15f, ClockService.Instance.RemainingTime);
        Assert.AreNotEqual(HandState.Idle, Game.State);
    }

    // ---------- TC-BJK-8 ----------

    [UnityTest]
    public IEnumerator TC_BJK_8_DongAppGiuaVan_MatAnteKhongCoThuong()
    {
        // Chọn seed mà ván chia ra KHÔNG phải Natural (Natural kết thúc ngay, không còn “giữa ván”).
        int seed = 0;
        while (true)
        {
            var probe = new BlackjackGame(new System.Random(seed));
            probe.Deal();
            if (probe.State == HandState.PlayerTurn) break;
            seed++;
        }
        TestHooks.SetField(_app, "_game", new BlackjackGame(new System.Random(seed)));

        TestHooks.SetMoney(30);
        TestHooks.SetClock(100f);
        ClickDeal();
        Assert.AreEqual(HandState.PlayerTurn, Game.State, "Ván phải đang ở lượt người chơi");
        Assert.AreEqual(80f, ClockService.Instance.RemainingTime, "Ante 20 s đã bị trừ");

        Laptop.CloseCurrentApp();

        Assert.AreEqual(HandState.Idle, Game.State, "Ván bị hủy khi đóng ứng dụng");
        Assert.AreEqual(30, MoneyService.Instance.Current);

        yield return Frames(150);          // 3 s: quá thời gian lật bài + hiện kết quả
        Assert.AreEqual(30, MoneyService.Instance.Current, "Không được trả thưởng sau khi đã đóng ứng dụng");
    }
}
