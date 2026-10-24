using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.4.2 của kế hoạch kiểm thử, TC-EFX-1…5: vòng đời hiệu ứng thẻ và Collection.
/// Thời gian chạy ở 10 khung/giây (mỗi khung 0,1 s) để chờ hết hiệu ứng 15–20 s cho nhanh.
/// </summary>
public class CardEffectTests : GameSceneTest
{
    private const int Fps = 10;
    
    private readonly object _ringKey = new object();

    protected override void BeforeSceneLoad() => PlayerPrefs.DeleteKey("CardCollection_UnlockedIds");   // Collection trống

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        yield return null;
    }

    /// <summary>Chờ thêm 1 s sau mốc để hoạt ảnh lật thẻ (tween 0,4 s) chạy xong.</summary>
    private static IEnumerator Seconds(float seconds) => Frames(Mathf.CeilToInt((seconds + 1f) * Fps));

    // ---------- TC-EFX-1 ----------

    [UnityTest]
    public IEnumerator TC_EFX_1_HieuUngKetThucTuNhien_LatUpVaVanTinhVaoHand()
    {
        var snail = Cards.Asset("SnailSpeed");                  // hiệu lực 15 s
        var card = Cards.Place(snail);
        Assert.IsFalse(card.IsFaceDown);

        yield return Frames(Mathf.CeilToInt(15f * Fps) + 2);    // đủ 15 s game
        Assert.IsTrue(TestHooks.GetField<bool>(card, "_hasEnded"), "Hết 15 s: hiệu ứng đã kết thúc (Invoke EndEffect đã chạy)");

        // Hoạt ảnh lật là tween unscaled 0,4 s: chờ tối đa 3 s thay vì một số khung hình cố định.
        float waited = 0f;
        while (!card.IsFaceDown && waited < 3f) { yield return null; waited += Time.unscaledDeltaTime; }

        Assert.IsTrue(card.IsFaceDown, "Hết hiệu ứng: thẻ lật úp");
        Assert.IsTrue(card.gameObject.activeSelf, "Thẻ vẫn nằm trên bàn");
        Assert.AreEqual(1, Cards.HandCount, "Thẻ vẫn tính vào Hand");
    }

    // ---------- TC-EFX-2 ----------

    [Test]
    public void TC_EFX_2a_HetHieuUngRoiPawn_OnRemovedMotLan()
    {
        var effect = ScriptableObject.CreateInstance<CountingCardSO>();
        var card = Cards.Place(effect);

        card.EndEffect();
        card.OnPawned();

        Assert.AreEqual(1, effect.RemovedCount);
        Object.Destroy(effect);
    }

    [Test]
    public void TC_EFX_2b_PawnSom_OnRemovedMotLan()
    {
        var effect = ScriptableObject.CreateInstance<CountingCardSO>();
        var card = Cards.Place(effect);

        card.OnPawned();
        card.EndEffect();                                       // gọi thêm cũng không được chạy lại

        Assert.AreEqual(1, effect.RemovedCount);
        Assert.AreEqual(1, effect.PawnedCount);
        Object.Destroy(effect);
    }

    // ---------- TC-EFX-3 ----------

    [UnityTest]
    public IEnumerator TC_EFX_3a_HetHieuUngTuNhien_DuocGhiCollection()
    {
        var snail = Cards.Asset("SnailSpeed");
        Assert.IsFalse(CardCollectionManager.Instance.IsCollected(snail));
        Cards.Place(snail);

        yield return Seconds(15f);

        Assert.IsTrue(CardCollectionManager.Instance.IsCollected(snail));
    }

    [Test]
    public void TC_EFX_3b_PawnSom_KhongDuocGhiCollection()
    {
        var scarab = Cards.Asset("TimeScarab");
        var card = Cards.Place(scarab);

        card.OnPawned();

        Assert.IsFalse(CardCollectionManager.Instance.IsCollected(scarab));
    }

    // ---------- TC-EFX-4 ----------

    [Test]
    public void TC_EFX_4_HieuUngTocDo_NhanDonTheoKhoa()
    {
        ClockService.Instance.SetSpeed(_ringKey, Config.ringEffectMultiplier);   // Phone đang reo: 1,5
        var snail = Cards.Asset("SnailSpeed");                                   // tickMultiplier 0,75 trong asset

        var card = Cards.Place(snail);
        Assert.AreEqual(1.125f, ClockService.Instance.TickMultiplier, 1e-6f,
            "Mỗi nguồn có khóa riêng và nhân với nhau: 1,5 × 0,75");

        card.EndEffect();
        Assert.AreEqual(1.5f, ClockService.Instance.TickMultiplier, 1e-6f,
            "Hết thẻ thì chỉ còn nguồn Phone");

        ClockService.Instance.ClearSpeed(_ringKey);
        Assert.AreEqual(1f, ClockService.Instance.TickMultiplier, 1e-6f);
    }

    // ---------- TC-EFX-5 ----------

    [Test]
    public void TC_EFX_5_ThePhuPhiVeSo_VeTangGia_GoiTheKhongDoi()
    {
        Cards.Place(Cards.Asset("TicketCostModifier"));         // ×1,5 cho vé số trong 20 s
        GiveItem("card_box");                                   // Card Box bị ẩn đầu ván
        var stack = Object.FindObjectsByType<TicketStack>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(t => TestHooks.GetField<ScratchTicket[]>(t, "ticketPool")[0].TierKind == TicketTierKind.TripleMatch);   // chồng Triple có sẵn từ đầu ván
        int expected = Mathf.RoundToInt(Config.tripleMatchCost * 1.5f);   // vé $25 → 37,5 → $38 (Mathf.RoundToInt)
        TestHooks.SetMoney(expected);
        stack.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);
        Assert.AreEqual(0, MoneyService.Instance.Current, $"Vé phải có giá {expected}");

        TestHooks.SetMoney(Config.packCost);
        Cards.ClickBox();
        Assert.AreEqual(0, MoneyService.Instance.Current, "Gói thẻ không bị ảnh hưởng: vẫn $35");
    }

    // ---------- TC-EFX-6 ----------

    [Test]
    public void TC_EFX_6_HaiTheGiaVe_NhanVoiNhau_HetMotTheConLaiTheKia()
    {
        // Lỗi N4: hai thẻ cùng đích từng ghi đè nhau, thẻ hết hạn trước đặt lại cả hai về 1.
        var asset = Cards.Asset("TicketCostModifier");
        float each = TestHooks.GetField<float>(asset, "costMultiplier");
        var cost = CostModifierService.Instance;

        var first = Cards.Place(asset);
        var second = Cards.Place(asset);
        Assert.AreEqual(each * each, cost.GetMultiplier(CostModifierService.Target.Ticket), 1e-5f, "Hai thẻ nhân với nhau");

        first.EndEffect();
        Assert.AreEqual(each, cost.GetMultiplier(CostModifierService.Target.Ticket), 1e-5f, "Chỉ còn thẻ thứ hai");

        second.EndEffect();
        Assert.AreEqual(1f, cost.GetMultiplier(CostModifierService.Target.Ticket), 1e-5f);
    }

    [Test]
    public void TC_EFX_6b_PawnTheGiaVe_GoHeSoCuaThe()
    {
        var asset = Cards.Asset("TicketCostModifier");
        var card = Cards.Place(asset);

        card.OnPawned();

        Assert.AreEqual(1f, CostModifierService.Instance.GetMultiplier(CostModifierService.Target.Ticket), 1e-5f);
    }
}
