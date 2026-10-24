using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.4.1 của kế hoạch kiểm thử, TC-CRD-1…9 (TC-CRD-10 đang treo, chờ xác nhận chuyện dừng Clock).
/// Mua gói = nhấp CardBox qua PointerReceiver. “Hand” = số thẻ đang đặt trên bàn (CardHandManager.Placed).
/// </summary>
public class CardBoxTests : GameSceneTest
{
    private static int Money => MoneyService.Instance.Current;
    [UnitySetUp]
    public IEnumerator OwnCardBox()
    {
        GiveItem("card_box");                       // Card Box là món mua ở Shop, đầu ván bị ẩn và không nhận nhấp chuột
        Assert.IsTrue(Cards.Box.gameObject.activeInHierarchy, "Card Box phải hiện thì các ca mua gói mới có nghĩa");
        yield break;
    }

    private static GameObject PackPanel => TestHooks.GetField<GameObject>(Cards.RevealPanel, "panel");

    // ---------- TC-CRD-1, 2, 3 ----------

    [Test]
    public void TC_CRD_1_Money35Hand4_MuaDuoc()
    {
        Cards.FillHand(4);
        TestHooks.SetMoney(35);

        Cards.ClickBox();

        Assert.AreEqual(0, Money);
        Assert.IsTrue(PackPanel.activeSelf, "Phải mở bảng gói thẻ");
        Assert.AreEqual(3, Cards.Slots.Count(s => s.gameObject.activeSelf), "Gói có 3 thẻ");
    }

    [Test]
    public void TC_CRD_2_HandDay_TuChoi()
    {
        Cards.FillHand(5);
        TestHooks.SetMoney(50);

        Cards.ClickBox();

        Assert.AreEqual(50, Money);
        Assert.IsFalse(PackPanel.activeSelf);
    }

    [Test]
    public void TC_CRD_3_ThieuTien_TuChoi()
    {
        TestHooks.SetMoney(34);

        Cards.ClickBox();

        Assert.AreEqual(34, Money);
        Assert.IsFalse(PackPanel.activeSelf);
    }

    // ---------- TC-CRD-4 ----------

    [Test]
    public void TC_CRD_4_PhanBoDoHiem()
    {
        var expected = new (CardSO.Rarity rarity, double p)[]
        {
            (CardSO.Rarity.C, 0.60), (CardSO.Rarity.R, 0.25), (CardSO.Rarity.SR, 0.12), (CardSO.Rarity.UR, 0.03),
        };
        const int n = 10000;

        foreach (int seed in new[] { 1, 2, 3 })
        {
            var rng = new System.Random(seed);
            var counts = expected.ToDictionary(e => e.rarity, _ => 0);
            for (int i = 0; i < n; i++)
                counts[(CardSO.Rarity)TestHooks.Call(Cards.Box, "RollRarity", Config, rng)]++;

            foreach (var (rarity, p) in expected)
            {
                double tolerance = 3 * Math.Sqrt(p * (1 - p) / n);
                double actual = counts[rarity] / (double)n;
                Assert.That(actual, Is.EqualTo(p).Within(tolerance),
                    $"Seed {seed}, độ hiếm {rarity}: {actual:P2}, đặc tả {p:P0} ± {tolerance:P2}");
            }
        }
    }

    // ---------- TC-CRD-5 ----------

    [Test]
    public void TC_CRD_5_DebuffBatBuocDat_DatDuocDuVuotHandCap()
    {
        Cards.FillHand(5);
        var debuff = Cards.Asset("Butterfingers");
        Assert.IsTrue(((EffectCardSO)debuff).isDebuff);
        Cards.BeginPack(debuff, Cards.Plain(5), Cards.Plain(6));
        var slot = Cards.Slots[0];

        TestHooks.ClickButton(slot, "cardButton");     // lật thẻ

        Assert.IsTrue(Cards.ButtonVisible(slot, "placeButton"), "Debuff phải có nút Place");
        Assert.IsFalse(Cards.ButtonVisible(slot, "sellButton"), "Debuff không được bán");
        Assert.IsFalse(Cards.ButtonVisible(slot, "collectionButton"), "Debuff không được thêm vào Collection");

        TestHooks.ClickButton(slot, "placeButton");

        Assert.AreEqual(6, Cards.HandCount, "Place debuff phải thành công dù Hand đã đủ 5");
        Assert.IsFalse(slot.gameObject.activeSelf);
    }

    // ---------- TC-CRD-6 ----------

    [Test]
    public void TC_CRD_6a_BanTheConUp_10Do()
    {
        Cards.BeginPack(Cards.Asset("Steve"), Cards.Plain(0), Cards.Plain(1));
        TestHooks.SetMoney(0);

        TestHooks.ClickButton(Cards.Slots[0], "sellButton");

        Assert.AreEqual(10, Money);
    }

    [Test]
    public void TC_CRD_6b_BanTheDaLat_BangPawnValue()
    {
        var steve = Cards.Asset("Steve");
        Cards.BeginPack(steve, Cards.Plain(0), Cards.Plain(1));
        TestHooks.SetMoney(0);

        TestHooks.ClickButton(Cards.Slots[0], "cardButton");
        TestHooks.ClickButton(Cards.Slots[0], "sellButton");

        Assert.AreEqual(steve.pawnValue, Money);
        Assert.AreEqual(35, Money, "Steve có pawnValue 35");
    }

    // ---------- TC-CRD-7 ----------

    protected override void BeforeSceneLoad() => PlayerPrefs.DeleteKey("CardCollection_UnlockedIds");   // Collection trống

    [Test]
    public void TC_CRD_7_NutAddToCollection()
    {
        var steve = Cards.Asset("Steve");
        var deeKay = Cards.Asset("DeeKay");
        var debuff = Cards.Asset("Butterfingers");
        CardCollectionManager.Instance.Register(deeKay);         // (b) đã có trong Collection

        Cards.BeginPack(steve, deeKay, debuff);
        foreach (var s in Cards.Slots) TestHooks.ClickButton(s, "cardButton");

        Assert.IsTrue(Cards.ButtonVisible(Cards.Slots[0], "collectionButton"), "(a) Thẻ chưa có: phải hiện nút");
        Assert.IsFalse(Cards.ButtonVisible(Cards.Slots[1], "collectionButton"), "(b) Thẻ đã có: không hiện nút");
        Assert.IsFalse(Cards.ButtonVisible(Cards.Slots[2], "collectionButton"), "(c) Debuff: không hiện nút");

        TestHooks.ClickButton(Cards.Slots[0], "collectionButton");
        Assert.IsTrue(CardCollectionManager.Instance.IsCollected(steve), "(a) Thêm được vào Collection");
    }

    // ---------- TC-CRD-8 ----------

    [Test]
    public void TC_CRD_8_PoolCardItemCoDinh7()
    {
        int CardItemCount() => UnityEngine.Object.FindObjectsByType<CardItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        Assert.AreEqual(7, CardItemCount(), "Kế hoạch ghi pool có đúng 7 CardItem");

        for (int round = 0; round < 5; round++)
        {
            Cards.BeginPack(Cards.Plain(0), Cards.Plain(1), Cards.Plain(2));
            foreach (var s in Cards.Slots) TestHooks.ClickButton(s, "cardButton");
            foreach (var s in Cards.Slots) TestHooks.ClickButton(s, "placeButton");
            Assert.AreEqual(3, Cards.HandCount);

            foreach (var card in CardHandManager.Instance.Placed.ToArray()) card.OnPawned();   // bán thẻ giữa các lần
            Assert.AreEqual(0, Cards.HandCount);
        }

        Assert.AreEqual(7, CardItemCount(), "Không được tạo thêm CardItem");
    }

    // ---------- TC-CRD-9 ----------

    [Test]
    public void TC_CRD_9_NhapDupCardBox_ChiMo1Goi()
    {
        TestHooks.SetMoney(40);

        Cards.ClickBox();
        Cards.ClickBox();                                       // lần 2 bị từ chối vì chỉ còn $5

        Assert.AreEqual(5, Money, "Chỉ mở và trừ tiền 1 gói");
        Assert.IsTrue(PackPanel.activeSelf);
    }
}
