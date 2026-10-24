using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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

    private static ScratchWindow[] Windows(ScratchTicket ticket) => TestHooks.GetField<ScratchWindow[]>(ticket, "windows");

    private static ScratchTicket.SymbolEntry[] Symbols(ScratchTicket ticket) =>
        TestHooks.GetField<TicketSymbolConfig>(ticket, "symbolConfig").symbolPool;

    private static void LockSymbols(ScratchTicket ticket, params int[] symbolIndex)
    {
        Assert.AreEqual(Windows(ticket).Length, symbolIndex.Length);
        TestHooks.SetField(ticket, "_activeSymbolIndex", symbolIndex);
    }

    private static int Reward(ScratchTicket ticket) => (int)TestHooks.Call(ticket, "ComputeCurrentReward");

    [Test]
    public void GiaTriTrongGameConfigKhopKeHoach()
    {
        Assert.AreEqual(25, Config.tripleMatchCost);
        Assert.AreEqual(55, Config.tripleMatchBaseReward);
    }

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
        TestHooks.SetMoney(25);
        var ticket = BuyOne(stack);
        Open(ticket);
        foreach (var w in Windows(ticket)) w.Reveal();
        var symbols = Symbols(ticket);
        Assert.AreEqual(new[] { 0.25f, 0.6f, 2f }, symbols.Select(s => s.payoutMultiplier).ToArray(),
            "Hệ số trong TripleMatchSymbols.asset đã đổi: cập nhật số trong kế hoạch");

        // baseReward 55 × (maxMatch − 1) × hệ số, làm tròn bằng Mathf.RoundToInt.
        LockSymbols(ticket, 0, 1, 2);
        Assert.AreEqual(0, Reward(ticket), "maxMatch = 1");

        LockSymbols(ticket, 0, 0, 1);
        Assert.AreEqual(14, Reward(ticket), "maxMatch = 2, hệ số 0,25: 55 × 1 × 0,25 = 13,75");
        LockSymbols(ticket, 2, 2, 0);
        Assert.AreEqual(110, Reward(ticket), "maxMatch = 2, hệ số 2: 55 × 1 × 2");

        LockSymbols(ticket, 0, 0, 0);
        Assert.AreEqual(28, Reward(ticket), "maxMatch = 3, hệ số 0,25: 55 × 2 × 0,25 = 27,5");
        LockSymbols(ticket, 1, 1, 1);
        Assert.AreEqual(66, Reward(ticket), "maxMatch = 3, hệ số 0,6: 55 × 2 × 0,6");
        LockSymbols(ticket, 2, 2, 2);
        Assert.AreEqual(220, Reward(ticket), "maxMatch = 3, hệ số 2: 55 × 2 × 2");
    }

    [Test]
    public void TC_TKT_4_HoaSoOTrung_TinhTheoHeSoCaoHon()
    {
        var stack = Stack(TicketTierKind.LuckyNine);
        TestHooks.SetMoney(Config.luckyNineCost);
        var ticket = BuyOne(stack);
        Open(ticket);
        foreach (var w in Windows(ticket)) w.Reveal();
        var symbols = Symbols(ticket);

        // Biểu tượng 0 (hệ số thấp nhất) và 4 (cao nhất) cùng trùng 3 ô; 3 ô còn lại khác nhau.
        LockSymbols(ticket, 0, 0, 0, 4, 4, 4, 1, 2, 3);

        int expected = Mathf.RoundToInt(Config.luckyNineBaseReward * 2 * symbols[4].payoutMultiplier);
        Assert.AreEqual(expected, Reward(ticket));
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
    public void TC_TKT_6_CashOut_TraThuongTheoKetQuaDaChot()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        TestHooks.SetMoney(25);
        var ticket = BuyOne(stack);
        Open(ticket);
        LockSymbols(ticket, 0, 0, 0);       // kết quả đã chốt: trùng 3 ô biểu tượng 0 → $28
        Windows(ticket)[0].Reveal();        // mới cào 1 ô

        ticket.CashOut();

        Assert.IsTrue(Windows(ticket).All(w => w.IsRevealed), "Cash Out phải lộ nốt các ô");
        Assert.AreEqual(28, Money, "Kế hoạch: trả thưởng theo kết quả đã chốt lúc mua (trùng 3 ô = $28)");
        Assert.IsFalse(ticket.gameObject.activeSelf, "Vé phải trở về pool");
    }

    // ---------- TC-TKT-7 ----------

    private static readonly MethodInfo PickWeightedIndex =
        typeof(ScratchTicket).GetMethod("PickWeightedIndex", BindingFlags.Static | BindingFlags.NonPublic);

    private static void AssertDistribution(TicketTierKind tier, double[] expected)
    {
        Assert.IsNotNull(PickWeightedIndex, "ScratchTicket không còn hàm PickWeightedIndex");
        var symbols = Symbols(Pool(Stack(tier))[0]);
        Assert.AreEqual(expected.Length, symbols.Length);
        const int n = 10000;

        foreach (int seed in new[] { 1, 2, 3 })
        {
            var rng = new System.Random(seed);
            var counts = new int[symbols.Length];
            for (int i = 0; i < n; i++)
                counts[(int)PickWeightedIndex.Invoke(null, new object[] { symbols, rng })]++;

            for (int k = 0; k < expected.Length; k++)
            {
                double p = expected[k];
                double tolerance = 3 * Math.Sqrt(p * (1 - p) / n);   // tiêu chí ca thống kê, mục 4.1.4
                double actual = counts[k] / (double)n;
                Assert.That(actual, Is.EqualTo(p).Within(tolerance),
                    $"{tier}, seed {seed}, biểu tượng {k}: {actual:P2}, đặc tả {p:P0} ± {tolerance:P2}");
            }
        }
    }

    [Test]
    public void TC_TKT_7a_PhanBoBieuTuong_TripleMatch() =>
        AssertDistribution(TicketTierKind.TripleMatch, new[] { 0.75, 0.20, 0.05 });

    [Test]
    public void TC_TKT_7b_PhanBoBieuTuong_LuckyNine() =>
        AssertDistribution(TicketTierKind.LuckyNine, new[] { 0.40, 0.27, 0.18, 0.10, 0.05 });

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
}
