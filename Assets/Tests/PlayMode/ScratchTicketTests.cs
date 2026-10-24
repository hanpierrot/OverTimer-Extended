using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.3.3 của kế hoạch kiểm thử, TC-TKT-1…8 và 10 (TC-TKT-9 là [Dự kiến]).
/// Mua vé = nhấp chồng vé (TicketStack) qua PointerReceiver; mở vé = nhấp vào vé.
/// Ca cần kết quả cụ thể đặt thẳng biểu tượng đã chốt (_activeSymbolIndex) sau khi mua.
/// </summary>
public class ScratchTicketTests : GameSceneTest
{
    private static int Money => MoneyService.Instance.Current;

    private static TicketStack Stack(TicketTierKind tier) =>
        UnityEngine.Object.FindObjectsByType<TicketStack>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(s => TestHooks.GetField<ScratchTicket[]>(s, "ticketPool")[0].TierKind == tier);

    private static ScratchTicket[] Pool(TicketStack stack) => TestHooks.GetField<ScratchTicket[]>(stack, "ticketPool");

    private static ScratchTicket[] ActiveTickets(TicketStack stack) => Pool(stack).Where(t => t.gameObject.activeSelf).ToArray();

    private static void Buy(TicketStack stack) => stack.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);

    private static ScratchTicket BuyOne(TicketStack stack)
    {
        Buy(stack);
        var active = ActiveTickets(stack);
        Assert.AreEqual(1, active.Length, "Phải có đúng 1 vé trên bàn sau khi mua");
        return active[0];
    }

    private static void Open(ScratchTicket ticket) => ticket.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);

    // "windows" của ScratchTicket nay là property đọc từ ScratchPanelView, nên lấy thẳng từ panelView.
    private static ScratchWindow[] Windows(ScratchTicket ticket) =>
        TestHooks.GetField<ScratchPanelView>(ticket, "panelView").Windows;

    private static ScratchTicket.SymbolEntry[] Symbols(ScratchTicket ticket) =>
        TestHooks.GetField<TicketSymbolConfig>(ticket, "symbolConfig").symbolPool;

    private static void LockSymbols(ScratchTicket ticket, params int[] symbolIndex)
    {
        Assert.AreEqual(Windows(ticket).Length, symbolIndex.Length);
        TestHooks.SetField(ticket, "_activeSymbolIndex", symbolIndex);
        TestHooks.SetField(ticket, "_isJackpot", false);   // a Lucky Nine bought here may have rolled an ending ticket; these tests check the plain reward
        TestHooks.SetField(ticket, "_isCursed", false);
    }

    /// <summary>
    /// Thưởng mong đợi theo công thức đã chốt (DESIGN §5.3), tính từ asset: tổng trên mọi biểu tượng lặp lại của
    /// baseReward × (số ô − 1) × hệ số. Hệ số và trọng số lấy từ asset nên test không hỏng khi cân bằng lại.
    /// </summary>
    private static int ExpectedReward(ScratchTicket.SymbolEntry[] symbols, int baseReward, params int[] shown)
    {
        float reward = 0f;
        foreach (var group in shown.GroupBy(i => i).Where(g => g.Count() > 1))
            reward += baseReward * (group.Count() - 1) * symbols[group.Key].payoutMultiplier;
        return Mathf.RoundToInt(reward);
    }

    private static int Reward(ScratchTicket ticket) => (int)TestHooks.Call(ticket, "ComputeCurrentReward");

    // ---------- TC-TKT-1, 2 ----------

    [Test]
    public void TC_TKT_1_MuaKhiThieuTien_KhongPhatVe()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        TestHooks.SetMoney(24);

        Buy(stack);

        Assert.AreEqual(0, ActiveTickets(stack).Length);
        Assert.AreEqual(24, Money);
    }

    [Test]
    public void TC_TKT_2_MuaKhiDuTien_ChotBieuTuongNgayLucMua()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        TestHooks.SetMoney(25);

        var ticket = BuyOne(stack);

        Assert.AreEqual(0, Money);
        var locked = TestHooks.GetField<int[]>(ticket, "_activeSymbolIndex");
        Assert.IsNotNull(locked, "Biểu tượng phải được chốt ngay lúc mua");
        Assert.AreEqual(Windows(ticket).Length, locked.Length);
    }

    // ---------- TC-TKT-3, 4 ----------

    [Test]
    public void TC_TKT_3_BangThuongTheoSoOTrung()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        TestHooks.SetMoney(Config.tripleMatchCost);
        var ticket = BuyOne(stack);
        Open(ticket);
        foreach (var w in Windows(ticket)) w.Reveal();
        var symbols = Symbols(ticket);
        int baseReward = Config.tripleMatchBaseReward;
        Assert.AreEqual(3, Windows(ticket).Length, "Triple Match có 3 ô");

        LockSymbols(ticket, 0, 1, 2);
        Assert.AreEqual(0, Reward(ticket), "ba biểu tượng khác nhau: không trùng thì không thưởng");

        for (int k = 0; k < symbols.Length; k++)
        {
            int other = (k + 1) % symbols.Length;

            LockSymbols(ticket, k, k, other);
            Assert.AreEqual(ExpectedReward(symbols, baseReward, k, k, other), Reward(ticket),
                $"trùng 2 ô biểu tượng {k}: baseReward × 1 × hệ số");

            LockSymbols(ticket, k, k, k);
            Assert.AreEqual(ExpectedReward(symbols, baseReward, k, k, k), Reward(ticket),
                $"trùng 3 ô biểu tượng {k}: baseReward × 2 × hệ số");
        }
    }

    [Test]
    public void TC_TKT_4_NhieuBoSoOTrung_CongDonTungBieuTuong()
    {
        // Luật mới (DESIGN §5.3): mỗi biểu tượng lặp lại đều được trả, rồi cộng lại. Luật cũ chỉ trả biểu tượng cao nhất.
        GiveItem("lucky_stack");                                   // Lucky Nine là món mua ở Shop, đầu ván bị ẩn
        var stack = Stack(TicketTierKind.LuckyNine);
        TestHooks.SetMoney(Config.luckyNineCost);
        var ticket = BuyOne(stack);
        Open(ticket);
        foreach (var w in Windows(ticket)) w.Reveal();
        var symbols = Symbols(ticket);
        int baseReward = Config.luckyNineBaseReward;
        Assert.AreEqual(9, Windows(ticket).Length, "Lucky Nine có 9 ô");
        Assert.Greater(symbols[0].payoutMultiplier, 0f, "Biểu tượng 0 phải có hệ số dương thì ca này mới phân biệt được cộng dồn");

        // Biểu tượng 0 và 4 cùng trùng 3 ô; 3 ô còn lại khác nhau.
        LockSymbols(ticket, 0, 0, 0, 4, 4, 4, 1, 2, 3);

        int sum = ExpectedReward(symbols, baseReward, 0, 0, 0, 4, 4, 4, 1, 2, 3);
        int onlyBest = ExpectedReward(symbols, baseReward, 4, 4, 4);   // thưởng nếu chỉ biểu tượng 4 được trả (luật cũ)

        Assert.AreEqual(sum, Reward(ticket));
        Assert.Greater(Reward(ticket), onlyBest, "Cộng dồn: phải lớn hơn thưởng riêng của biểu tượng 4");
    }

    // ---------- TC-TKT-5 ----------

    [Test]
    public void TC_TKT_5_NguongTuLoO_59Va60PhanTram()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        TestHooks.SetMoney(25);
        var ticket = BuyOne(stack);
        Open(ticket);
        var window = Windows(ticket)[0];
        int playable = TestHooks.GetField<int>(window, "_totalPlayablePixels");
        int resolution = TestHooks.GetField<int>(window, "maskResolution");

        // Nét cào ở góc ô (ngoài vòng tròn) không xóa thêm điểm ảnh nào, chỉ kích hoạt bước kiểm tra ngưỡng.
        void ScratchCornerWith(int clearedPixels)
        {
            TestHooks.SetField(window, "_clearedCount", clearedPixels);
            var corner = new Vector2Int(0, 0);
            TestHooks.Call(window, "StampLine", corner, corner);
        }

        ScratchCornerWith(Mathf.FloorToInt(playable * 0.59f));
        Assert.IsFalse(window.IsRevealed, "Cào 59% chưa được tự lộ");

        ScratchCornerWith(Mathf.CeilToInt(playable * 0.60f));
        Assert.IsTrue(window.IsRevealed, "Cào 60% phải tự lộ");
        Assert.That(resolution, Is.GreaterThan(0));
    }

    // ---------- TC-TKT-6 ----------

    [Test]
    public void TC_TKT_6a_CashOut_TraThuongTheoCacODaCao()
    {
        // Luật mới: chỉ trả thưởng cho các ô đã cào; Cash Out lộ nốt phần còn lại nhưng không tính thêm.
        var stack = Stack(TicketTierKind.TripleMatch);
        TestHooks.SetMoney(Config.tripleMatchCost);
        var ticket = BuyOne(stack);
        Open(ticket);
        var symbols = Symbols(ticket);
        LockSymbols(ticket, 0, 0, 0);
        Windows(ticket)[0].Reveal();
        Windows(ticket)[1].Reveal();        // đã cào 2 ô trùng nhau, ô thứ 3 chưa

        ticket.CashOut();

        Assert.IsTrue(Windows(ticket).All(w => w.IsRevealed), "Cash Out phải lộ nốt các ô");
        Assert.AreEqual(ExpectedReward(symbols, Config.tripleMatchBaseReward, 0, 0), Money,
            "Trả theo 2 ô đã cào (trùng 2), không theo 3 ô");
        Assert.IsFalse(ticket.gameObject.activeSelf, "Vé phải trở về pool");
    }

    [Test]
    public void TC_TKT_6b_CashOutKhiChuaCao_KhongTraGi()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        TestHooks.SetMoney(Config.tripleMatchCost);
        var ticket = BuyOne(stack);
        Open(ticket);
        LockSymbols(ticket, 0, 0, 0);       // vé trúng lớn nhưng chưa cào ô nào

        ticket.CashOut();

        Assert.AreEqual(0, Money, "Không cào ô nào thì không có gì để trả");
    }

    // ---------- TC-TKT-7 ----------

    private static readonly MethodInfo PickWeightedIndex =
        typeof(ScratchTicket).GetMethod("PickWeightedIndex", BindingFlags.Static | BindingFlags.NonPublic);

    /// <summary>Phân bố thực tế khớp trọng số trong asset (xác suất = trọng số / tổng trọng số).</summary>
    private static void AssertDistribution(TicketTierKind tier)
    {
        Assert.IsNotNull(PickWeightedIndex, "ScratchTicket không còn hàm PickWeightedIndex");
        var symbols = Symbols(Pool(Stack(tier))[0]);
        double total = symbols.Sum(s => (double)s.weight);
        Assert.Greater(total, 0.0, "Tổng trọng số phải dương");
        const int n = 10000;

        foreach (int seed in new[] { 1, 2, 3 })
        {
            var rng = new System.Random(seed);
            var counts = new int[symbols.Length];
            for (int i = 0; i < n; i++)
                counts[(int)PickWeightedIndex.Invoke(null, new object[] { symbols, rng })]++;

            for (int k = 0; k < symbols.Length; k++)
            {
                double p = symbols[k].weight / total;
                double tolerance = 3 * Math.Sqrt(p * (1 - p) / n);   // tiêu chí ca thống kê, mục 4.1.4
                double actual = counts[k] / (double)n;
                Assert.That(actual, Is.EqualTo(p).Within(tolerance),
                    $"{tier}, seed {seed}, biểu tượng {k}: {actual:P2}, theo asset {p:P1} ± {tolerance:P2}");
            }
        }
    }

    [Test]
    public void TC_TKT_7a_PhanBoBieuTuong_TripleMatch() => AssertDistribution(TicketTierKind.TripleMatch);

    [Test]
    public void TC_TKT_7b_PhanBoBieuTuong_LuckyNine() => AssertDistribution(TicketTierKind.LuckyNine);

    // ---------- TC-TKT-8 ----------

    [Test]
    public void TC_TKT_8_ObjectPool_Mua50Lan_KhongTaoVeMoi()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        var poolBefore = new HashSet<ScratchTicket>(Pool(stack));
        int ticketObjectsBefore = UnityEngine.Object.FindObjectsByType<ScratchTicket>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        for (int i = 0; i < 50; i++)
        {
            TestHooks.SetMoney(25);
            var ticket = BuyOne(stack);
            Assert.IsTrue(poolBefore.Contains(ticket), $"Lần mua {i + 1}: vé không thuộc pool ban đầu");
            Open(ticket);
            ticket.CashOut();
        }

        int ticketObjectsAfter = UnityEngine.Object.FindObjectsByType<ScratchTicket>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        Assert.AreEqual(ticketObjectsBefore, ticketObjectsAfter, "Số vé trong scene phải không đổi (không Instantiate/Destroy)");
        Assert.AreEqual(0, ActiveTickets(stack).Length);
    }

    // ---------- TC-TKT-10 ----------

    [Test]
    public void TC_TKT_10_NhapDupNutMua_ChiPhat1Ve()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        TestHooks.SetMoney(30);

        Buy(stack);
        Buy(stack);

        Assert.AreEqual(1, ActiveTickets(stack).Length);
        Assert.AreEqual(5, Money);
    }

    // ---------- Ending Lucky Nine: Jackpot và Curse (DESIGN §5.14 D) ----------

    /// <summary>Mua một vé Lucky Nine với kết quả bị ép bằng CheatFlags (chỉ hoạt động trong Editor và bản dev), rồi mở vé.</summary>
    private ScratchTicket BuyEndingTicket(CheatFlags.NextTicket kind)
    {
        GiveItem("lucky_stack");                                   // Lucky Nine là món mua ở Shop, đầu ván bị ẩn
        CheatFlags.NextLuckyNine = kind;
        TestHooks.SetMoney(Config.luckyNineCost);
        var ticket = BuyOne(Stack(TicketTierKind.LuckyNine));
        Open(ticket);
        return ticket;
    }

    private static int[] Shown(ScratchTicket ticket) => TestHooks.GetField<int[]>(ticket, "_activeSymbolIndex");

    private static void RevealWindows(ScratchTicket ticket, int count)
    {
        var windows = Windows(ticket);
        for (int i = 0; i < count; i++) windows[i].Reveal();
    }

    private static TMP_Text HintLabel => TestHooks.GetField<TMP_Text>(HintService.Instance, "label");

    [Test]
    public void Jackpot_ChinOGiongNhau_LaBieuTuongHeSoCaoNhat()
    {
        var ticket = BuyEndingTicket(CheatFlags.NextTicket.Jackpot);

        int best = ScratchTicket.IndexOfHighestMultiplier(Symbols(ticket));
        Assert.That(Shown(ticket), Has.All.EqualTo(best), "Jackpot: cả 9 ô là biểu tượng hiếm nhất");
    }

    [UnityTest]
    public IEnumerator Jackpot_ThangKhiCaoHetCaChinO()
    {
        var ticket = BuyEndingTicket(CheatFlags.NextTicket.Jackpot);

        RevealWindows(ticket, 8);
        yield return Frames(3);
        Assert.IsFalse(GameManager.Instance.IsGameOver, "Còn 1 ô chưa cào: chưa thắng");

        Windows(ticket)[8].Reveal();
        yield return Frames(3);

        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.TicketJackpotWin, GameManager.Instance.LastEndReason);
    }

    [UnityTest]
    public IEnumerator Jackpot_CashOutKhiChuaCaoHet_KhongThang()
    {
        var ticket = BuyEndingTicket(CheatFlags.NextTicket.Jackpot);
        RevealWindows(ticket, 8);

        ticket.CashOut();
        yield return Frames(3);

        Assert.IsFalse(GameManager.Instance.IsGameOver, "Rút tiền sớm thì bỏ lỡ ending");
    }

    [Test]
    public void Curse_ChinOGiongNhau_LaBieuTuongHeSoThapNhat()
    {
        var ticket = BuyEndingTicket(CheatFlags.NextTicket.Cursed);

        int worst = ScratchTicket.IndexOfLowestMultiplier(Symbols(ticket));
        Assert.That(Shown(ticket), Has.All.EqualTo(worst), "Curse: cả 9 ô là biểu tượng phổ biến nhất");
    }

    [UnityTest]
    public IEnumerator Curse_CaoHetCaChinO_KhoaCashOutRoiThuaSauNhipRung()
    {
        var ticket = BuyEndingTicket(CheatFlags.NextTicket.Cursed);

        RevealWindows(ticket, 9);
        yield return Frames(3);

        Assert.IsFalse(GameManager.Instance.IsGameOver, "Còn nhịp rung trước khi thua");
        Assert.IsFalse(TestHooks.GetField<ScratchPanelView>(ticket, "panelView").CloseButton.interactable,
            "Cash Out bị khóa trong lúc vé rung");

        yield return new WaitForSecondsRealtime(Config.curseBeatSeconds + 0.5f);

        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.TicketCurseLoss, GameManager.Instance.LastEndReason);
    }

    [UnityTest]
    public IEnumerator Curse_CashOutKhiChuaCaoHet_KhongThua()
    {
        var ticket = BuyEndingTicket(CheatFlags.NextTicket.Cursed);
        RevealWindows(ticket, 3);

        ticket.CashOut();
        yield return new WaitForSecondsRealtime(Config.curseBeatSeconds + 0.5f);

        Assert.IsFalse(GameManager.Instance.IsGameOver, "Rút tiền trước khi cào hết thì thoát lời nguyền");
    }

    [UnityTest]
    public IEnumerator Curse_DenONguongDiemBao_HienGoiYTicketCursed()
    {
        HintService.Instance.ResetSeen();
        var ticket = BuyEndingTicket(CheatFlags.NextTicket.Cursed);
        HintLabel.text = "";

        RevealWindows(ticket, Config.luckyNineCurseOmenWindows - 1);
        yield return Frames(2);
        Assert.AreNotEqual(HintCatalog.Get(HintId.TicketCursed), HintLabel.text, "Chưa tới ngưỡng điềm báo");

        Windows(ticket)[Config.luckyNineCurseOmenWindows - 1].Reveal();
        yield return Frames(2);

        Assert.AreEqual(HintCatalog.Get(HintId.TicketCursed), HintLabel.text);
    }

    [UnityTest]
    public IEnumerator VeTripleMatch_KhongBaoGioLaEnding_DuCoCongTacEpLuckyNine()
    {
        CheatFlags.NextLuckyNine = CheatFlags.NextTicket.Jackpot;      // chỉ áp cho Lucky Nine
        TestHooks.SetMoney(Config.tripleMatchCost);
        var ticket = BuyOne(Stack(TicketTierKind.TripleMatch));
        Open(ticket);

        foreach (var w in Windows(ticket)) w.Reveal();
        yield return Frames(3);

        Assert.IsFalse(GameManager.Instance.IsGameOver, "Triple Match không có Jackpot hay Curse");
    }
}
