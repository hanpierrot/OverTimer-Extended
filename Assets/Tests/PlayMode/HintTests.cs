using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.7.6 của kế hoạch kiểm thử, TC-HNT-2…5 (TC-HNT-1 ở Edit Mode, TC-HNT-7 đang treo).
/// “Gợi ý hiện” = chữ trên nhãn của HintService đổi thành nội dung trong HintCatalog.
/// </summary>
public class HintTests : GameSceneTest
{
    protected override void BeforeSceneLoad()
    {
        foreach (HintId id in System.Enum.GetValues(typeof(HintId))) PlayerPrefs.DeleteKey($"HintSeen_{id}");
    }

    private static TMP_Text Label => TestHooks.GetField<TMP_Text>(HintService.Instance, "label");

    private static void ClearLabel() => Label.text = "";

    // ---------- TC-HNT-2 ----------

    [UnityTest]
    public IEnumerator TC_HNT_2_GoiYChiHienMotLan()
    {
        string text = HintCatalog.Get(HintId.TicketOpen);
        ClearLabel();

        HintService.Instance.ShowOnce(HintId.TicketOpen);
        Assert.AreEqual(text, Label.text, "Lần 1 phải hiện");
        Assert.AreEqual(1, PlayerPrefs.GetInt("HintSeen_TicketOpen", 0));

        ClearLabel();
        HintService.Instance.ShowOnce(HintId.TicketOpen);
        Assert.AreEqual("", Label.text, "Lần 2 không hiện");

        yield return ReloadScene();
        ClearLabel();
        HintService.Instance.ShowOnce(HintId.TicketOpen);
        Assert.AreEqual("", Label.text, "Mở lại game cũng không hiện");
    }

    // ---------- TC-HNT-3 ----------

    [Test]
    public void TC_HNT_3_ThoiGianChoGiuaHaiLanHien()
    {
        Assert.AreEqual(3f, Config.hintRepeatCooldown);
        var lastShown = TestHooks.GetField<Dictionary<HintId, float>>(HintService.Instance, "_lastShown");
        string text = HintCatalog.Get(HintId.NotEnoughMoney);

        lastShown[HintId.NotEnoughMoney] = Time.unscaledTime - 2.9f;
        ClearLabel();
        HintService.Instance.Show(HintId.NotEnoughMoney);
        Assert.AreEqual("", Label.text, "Mới 2,9 s: bỏ qua");

        lastShown[HintId.NotEnoughMoney] = Time.unscaledTime - 3.01f;
        HintService.Instance.Show(HintId.NotEnoughMoney);
        Assert.AreEqual(text, Label.text, "Đủ 3 s: hiện lại");
    }

    // ---------- TC-HNT-4 ----------

    [Test]
    public void TC_HNT_4_HetVan_KhongHienGoiY()
    {
        GameManager.Instance.GameOver(EndReason.ClockLoss);
        ClearLabel();

        HintService.Instance.Show(HintId.NotEnoughMoney);

        Assert.AreEqual("", Label.text);
    }

    // ---------- TC-HNT-5 ----------

    [Test]
    public void TC_HNT_5a_MeterThieuTien_GoiYMucToiThieu()
    {
        TestHooks.SetMoney(9);
        ClearLabel();

        TestHooks.ClickMeter();

        Assert.AreEqual("The Meter needs at least $10.", Label.text);
    }

    [Test]
    public void TC_HNT_5b_HandDay_GoiYHandFull()
    {
        Cards.FillHand(5);
        TestHooks.SetMoney(50);
        ClearLabel();

        Cards.ClickBox();

        Assert.AreEqual("Your hand is full (5). Pawn a card to make room.", Label.text);
    }
}

/// <summary>TC-HNT-6: hướng dẫn đầu game. Lớp riêng vì cần xóa khóa “đã xem hướng dẫn” trước khi nạp scene.</summary>
public class TutorialTests : GameSceneTest
{
    protected override void BeforeSceneLoad() => PlayerPrefs.DeleteKey("TutorialSeen");

    [UnityTest]
    public IEnumerator TC_HNT_6_HuongDanChiOLanChoiDau()
    {
        var panel = TestHooks.Find<TutorialPanel>();
        Assert.IsTrue(panel.gameObject.activeInHierarchy, "Lần chơi đầu phải hiện hướng dẫn");
        var progress = TestHooks.GetField<TMP_Text>(panel, "progressText");
        StringAssert.StartsWith("1/5", progress.text);

        for (int step = 2; step <= 5; step++)
        {
            panel.OnPointerClick(null);
            StringAssert.StartsWith($"{step}/5", progress.text);
        }
        panel.OnPointerClick(null);                              // bước 5: nhấp để bắt đầu

        Assert.IsFalse(panel.gameObject.activeSelf, "Hết bước 5 thì đóng");
        Assert.AreEqual(1, PlayerPrefs.GetInt("TutorialSeen", 0));

        yield return ReloadScene();
        Assert.IsFalse(TestHooks.Find<TutorialPanel>().gameObject.activeInHierarchy, "Lần sau không hiện");
    }
}
