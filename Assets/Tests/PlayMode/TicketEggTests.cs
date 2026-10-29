using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Kế hoạch kiểm thử (Play Mode): vé số theo công thức thưởng mới TC-TKT-11 … 14, 17 và thời gian hồi Egg Timer TC-TSK-7.
/// Mua vé = nhấp chồng vé (TicketStack, qua PointerReceiver); mở vé = nhấp vé; Cash Out = ScratchTicket.CashOut (nút đóng).
/// Ca cần kết quả cụ thể đặt thẳng biểu tượng đã chốt (_activeSymbolIndex), chọn biểu tượng theo hệ số trong asset.
/// Số tiền mong đợi tính từ asset theo công thức DESIGN §5.3: tổng trên mọi biểu tượng lặp của
/// baseReward × (số ô − 1) × hệ số, làm tròn .5 lên (27,5 → 28 như ví dụ trong kế hoạch).
/// </summary>
public class TicketEggTests : GameSceneTest
{
    private const int Fps = 50;
    private static int Money => MoneyService.Instance.Current;

    private readonly List<HintId> _hints = new List<HintId>();
    private void RecordHint(HintId id) => _hints.Add(id);

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        // Mèo đi lang thang rồi ngồi chặn một vật trên bàn (có thể là vé đang dùng), làm lần nhấp mở vé không có tác dụng.
        // Ca nào về Cat đã có CatTests; ở đây cho mèo nghỉ.
        TestHooks.Find<CatController>().gameObject.SetActive(false);
        _hints.Clear();
        HintService.HintShown += RecordHint;
        yield return null;
    }

    [TearDown]
    public void StopRecording() => HintService.HintShown -= RecordHint;

    // ---- Vé ----

    private static TicketStack Stack(TicketTierKind tier) =>
        UnityEngine.Object.FindObjectsByType<TicketStack>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(s => TestHooks.GetField<ScratchTicket[]>(s, "ticketPool")[0].TierKind == tier);

    private static ScratchTicket[] Pool(TicketStack stack) => TestHooks.GetField<ScratchTicket[]>(stack, "ticketPool");
    private static ScratchTicket[] ActiveTickets(TicketStack stack) => Pool(stack).Where(t => t.gameObject.activeSelf).ToArray();
    private static void Buy(TicketStack stack) => stack.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);

    private static ScratchTicket BuyAndOpen(TicketStack stack)
    {
        var before = ActiveTickets(stack);
        Buy(stack);
        var ticket = ActiveTickets(stack).Except(before).Single();
        ticket.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);
        Assert.IsTrue(TestHooks.GetField<bool>(ticket, "_isBeingViewed"), "Vé phải mở ra sau khi nhấp");
        Assert.IsFalse(Windows(ticket).Any(w => w.IsRevealed), "Vé mới mở chưa lộ ô nào");
        return ticket;
    }

    private static ScratchWindow[] Windows(ScratchTicket t) => TestHooks.GetField<ScratchPanelView>(t, "panelView").Windows;
    private static ScratchTicket.SymbolEntry[] Symbols(ScratchTicket t) => TestHooks.GetField<TicketSymbolConfig>(t, "symbolConfig").symbolPool;

    private static void LockSymbols(ScratchTicket t, params int[] symbolIndex)
    {
        Assert.AreEqual(Windows(t).Length, symbolIndex.Length);
        TestHooks.SetField(t, "_activeSymbolIndex", symbolIndex);
        TestHooks.SetField(t, "_isJackpot", false);
        TestHooks.SetField(t, "_isCursed", false);
    }

    private static void RevealFirst(ScratchTicket t, int count)
    {
        var w = Windows(t);
        for (int i = 0; i < count; i++) w[i].Reveal();
    }

    /// <summary>Các biểu tượng theo hệ số tăng dần.</summary>
    private static int[] ByMultiplier(ScratchTicket.SymbolEntry[] s) =>
        Enumerable.Range(0, s.Length).OrderBy(i => s[i].payoutMultiplier).ToArray();

    private static int Expected(ScratchTicket.SymbolEntry[] s, int baseReward, params int[] shown)
    {
        double reward = 0;
        foreach (var g in shown.GroupBy(i => i).Where(g => g.Count() > 1))
            reward += baseReward * (g.Count() - 1) * (double)s[g.Key].payoutMultiplier;
        return (int)Math.Floor(reward + 0.5 + 1e-9);
    }

    private IEnumerator CashOutAll(TicketStack stack, int baseReward, Func<int[], int[]> pick, string label)
    {
        TestHooks.SetMoney(100);
        var ticket = BuyAndOpen(stack);
        var s = Symbols(ticket);
        var shown = pick(ByMultiplier(s));
        LockSymbols(ticket, shown);
        int money = Money;
        RevealFirst(ticket, shown.Length);
        yield return null;
        ticket.CashOut();
        Assert.AreEqual(Expected(s, baseReward, shown), Money - money, label);
        yield return null;
    }

    // ---------- TC-TKT-11 ----------

    [UnityTest]
    public IEnumerator TC_TKT_11_ThuongTripleMatch_CongThucMoi()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        int b = Config.tripleMatchBaseReward;
        Assert.AreEqual(3, Symbols(Pool(stack)[0]).Length, "Ca này viết cho bộ 3 biểu tượng của Triple Match");

        yield return CashOutAll(stack, b, m => new[] { m[0], m[1], m[2] }, "(a) ba biểu tượng khác nhau: $0");
        yield return CashOutAll(stack, b, m => new[] { m[1], m[1], m[0] }, "(b) 2 ô hệ số giữa + 1 ô khác");
        yield return CashOutAll(stack, b, m => new[] { m[2], m[2], m[2] }, "(c) 3 ô hệ số cao nhất");
        yield return CashOutAll(stack, b, m => new[] { m[0], m[0], m[0] }, "(d) 3 ô hệ số thấp nhất");
    }

    // ---------- TC-TKT-12 ----------

    [UnityTest]
    public IEnumerator TC_TKT_12_ThuongLuckyNine_CongMoiBieuTuongLap()
    {
        GiveItem("lucky_stack");
        var stack = Stack(TicketTierKind.LuckyNine);
        Assert.AreEqual(5, Symbols(Pool(stack)[0]).Length, "Ca này viết cho bộ 5 biểu tượng của Lucky Nine");

        // 3 ô thấp nhất, 2 ô thứ hai, 2 ô thứ ba, 1 ô thứ tư, 1 ô cao nhất.
        yield return CashOutAll(stack, Config.luckyNineBaseReward,
            m => new[] { m[0], m[0], m[0], m[1], m[1], m[2], m[2], m[3], m[4] }, "Cộng thưởng của mọi biểu tượng lặp");
    }

    // ---------- TC-TKT-13 ----------

    [UnityTest]
    public IEnumerator TC_TKT_13_ChiTraKhiCashOut_ChiTinhODaLo()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        int cost = Config.tripleMatchCost;
        int b = Config.tripleMatchBaseReward;

        // (a) Cào lộ cả 3 ô nhưng chưa Cash Out: chưa trả tiền.
        TestHooks.SetMoney(100);
        var ticket = BuyAndOpen(stack);
        Assert.AreEqual(100 - cost, Money, "Mua vé");
        var s = Symbols(ticket);
        int top = ByMultiplier(s).Last();
        LockSymbols(ticket, top, top, top);
        RevealFirst(ticket, 3);
        yield return Frames(10);
        Assert.AreEqual(100 - cost, Money, "(a) cào hết không tự trả");
        ticket.CashOut();
        yield return null;

        // (b) Cash Out khi mới lộ 2 ô.
        TestHooks.SetMoney(100);
        ticket = BuyAndOpen(stack);
        LockSymbols(ticket, top, top, top);
        RevealFirst(ticket, 2);
        yield return null;
        ticket.CashOut();
        Assert.AreEqual(100 - cost + Expected(s, b, top, top), Money, "(b) chỉ tính 2 ô đã lộ");
        yield return null;

        // (c) Cash Out khi chưa cào ô nào.
        TestHooks.SetMoney(100);
        ticket = BuyAndOpen(stack);
        LockSymbols(ticket, top, top, top);
        ticket.CashOut();
        Assert.AreEqual(100 - cost, Money, "(c) chưa cào: $0");
    }

    // ---------- TC-TKT-14 ----------

    [Test]
    public void TC_TKT_14_Pool2VeMoiLoai()
    {
        var stack = Stack(TicketTierKind.TripleMatch);
        int cost = Config.tripleMatchCost;
        Assert.AreEqual(2, Pool(stack).Length, "Mỗi loại có 2 vé trong pool");
        TestHooks.SetMoney(100);

        Buy(stack);
        Buy(stack);
        Assert.AreEqual(2, ActiveTickets(stack).Length, "Lần 1, 2: phát vé");
        Assert.AreEqual(100 - 2 * cost, Money);

        _hints.Clear();
        Buy(stack);
        Assert.AreEqual(2, ActiveTickets(stack).Length, "Lần 3: không phát thêm");
        Assert.AreEqual(100 - 2 * cost, Money, "Lần 3: không trừ tiền");
        CollectionAssert.Contains(_hints, HintId.TooManyTickets);
        Assert.AreEqual("Too many tickets on the desk. Cash one out first.", HintCatalog.Get(HintId.TooManyTickets));
    }

    // ---------- TC-TKT-17 ----------

    [Test]
    public void TC_TKT_17a_RTP_TripleMatch() => AssertRtp(TicketTierKind.TripleMatch);

    [Test]
    public void TC_TKT_17b_RTP_LuckyNine()
    {
        GiveItem("lucky_stack");
        AssertRtp(TicketTierKind.LuckyNine);
    }

    /// <summary>
    /// Vé thật bốc biểu tượng (ScratchTicket.RollSymbols, RNG seed cố định) và tính thưởng (ComputeCurrentReward, mọi ô đã lộ).
    /// So với kỳ vọng chính xác tính độc lập từ asset (liệt kê mọi tổ hợp số ô của từng biểu tượng, phân phối đa thức).
    /// Vé Jackpot/Curse của Lucky Nine bị loại (tương đương cheat tắt hai kết thúc này).
    /// </summary>
    private void AssertRtp(TicketTierKind tier)
    {
        var stack = Stack(tier);
        TestHooks.SetMoney(1000);
        var ticket = BuyAndOpen(stack);
        foreach (var w in Windows(ticket)) w.Reveal();
        var s = Symbols(ticket);
        int cells = Windows(ticket).Length;
        int b = tier == TicketTierKind.TripleMatch ? Config.tripleMatchBaseReward : Config.luckyNineBaseReward;
        int cost = tier == TicketTierKind.TripleMatch ? Config.tripleMatchCost : Config.luckyNineCost;

        var (mean, variance, pZero, min, max) = Exact(s, cells, b);
        const int n = 10000;
        foreach (int seed in new[] { 1, 2, 3 })
        {
            TestHooks.SeedRng(seed);
            double sum = 0; int zeros = 0, lo = int.MaxValue, hi = int.MinValue, kept = 0;
            while (kept < n)
            {
                TestHooks.Call(ticket, "RollSymbols");
                if (TestHooks.GetField<bool>(ticket, "_isJackpot") || TestHooks.GetField<bool>(ticket, "_isCursed")) continue;
                int r = (int)TestHooks.Call(ticket, "ComputeCurrentReward");
                sum += r; if (r == 0) zeros++; lo = Math.Min(lo, r); hi = Math.Max(hi, r); kept++;
            }
            double avg = sum / n, tol = 3 * Math.Sqrt(variance / n);
            Debug.Log($"[TC-TKT-17] {tier} seed {seed}: mean ${avg:0.00} (exact ${mean:0.00}, RTP {mean / cost:P1}), $0 {zeros / (double)n:P1} (exact {pZero:P1}), min ${lo}, max ${hi} (exact {min}…{max})");
            Assert.That(avg, Is.EqualTo(mean).Within(tol), $"{tier} seed {seed}: thưởng trung bình");
            double pTol = 3 * Math.Sqrt(pZero * (1 - pZero) / n) + 1e-9;
            Assert.That(zeros / (double)n, Is.EqualTo(pZero).Within(pTol), $"{tier} seed {seed}: tỉ lệ $0");
            Assert.GreaterOrEqual(lo, min, "Không thấp hơn mức nhỏ nhất có thể");
            Assert.LessOrEqual(hi, max, "Không cao hơn mức lớn nhất có thể");
        }
        if (tier == TicketTierKind.LuckyNine) Assert.Greater(min, 0, "Lucky Nine không bao giờ trả $0");
    }

    /// <summary>Kỳ vọng, phương sai, xác suất $0, thưởng nhỏ/lớn nhất: liệt kê mọi vector số ô (c_1 … c_k), tổng = cells.</summary>
    private static (double mean, double variance, double pZero, int min, int max) Exact(ScratchTicket.SymbolEntry[] s, int cells, int b)
    {
        double total = s.Sum(x => (double)x.weight);
        var p = s.Select(x => x.weight / total).ToArray();
        double mean = 0, sq = 0, pZero = 0; int min = int.MaxValue, max = int.MinValue;
        var c = new int[s.Length];

        void Visit(int k, int left)
        {
            if (k == s.Length - 1)
            {
                c[k] = left;
                double prob = Factorial(cells), reward = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    prob *= Math.Pow(p[i], c[i]) / Factorial(c[i]);
                    if (c[i] > 1) reward += b * (c[i] - 1) * (double)s[i].payoutMultiplier;
                }
                int r = Mathf.RoundToInt((float)reward);          // cùng phép làm tròn với ComputeCurrentReward
                mean += prob * r; sq += prob * r * r;
                if (r == 0) pZero += prob;
                if (prob > 0) { min = Math.Min(min, r); max = Math.Max(max, r); }
                return;
            }
            for (int n = 0; n <= left; n++) { c[k] = n; Visit(k + 1, left - n); }
        }
        Visit(0, cells);
        return (mean, sq - mean * mean, pZero, min, max);
    }

    private static double Factorial(int n) { double f = 1; for (int i = 2; i <= n; i++) f *= i; return f; }

    // ---------- TC-TSK-7 ----------

    private static IEnumerator Hold(PointerReceiver receiver, int frames)
    {
        receiver.OnPressStart(Vector2.zero);
        for (int i = 0; i < frames; i++)
        {
            yield return null;
            receiver.OnPressUpdate(Vector2.zero);
        }
        receiver.OnPressEnd(Vector2.zero);
    }

    [UnityTest]
    public IEnumerator TC_TSK_7_ThoiGianHoiEggTimer()
    {
        var egg = TestHooks.Find<EggTimerTask>();
        Assert.IsTrue(egg.gameObject.activeInHierarchy);
        var receiver = egg.GetComponent<PointerReceiver>();
        float cooldown = Config.eggTimerCooldown;
        int holdFrames = Mathf.CeilToInt(Config.crankTime * Fps) + 1;
        TestHooks.SetClock(100f);

        float before = ClockService.Instance.RemainingTime;
        yield return Hold(receiver, holdFrames);
        Assert.That(ClockService.Instance.RemainingTime - before, Is.EqualTo(Config.timePayout - holdFrames / (float)Fps).Within(0.01f), "Lần đầu được cộng giờ");

        // (a) Giữ lại sau cooldown − 0,05 s: không có tiến trình, không cộng giờ.
        yield return Frames(Mathf.FloorToInt((cooldown - 0.05f) * Fps));
        before = ClockService.Instance.RemainingTime;
        yield return Hold(receiver, holdFrames);
        Assert.That(ClockService.Instance.RemainingTime - before, Is.EqualTo(-holdFrames / (float)Fps).Within(0.01f), "(a) Đang hồi: không cộng giờ");

        // (b) Đúng biên: GameTime == thời điểm hết hồi thì giữ được (phép so sánh >=). Đặt mốc bằng đúng GameTime hiện tại
        // vì số khung của 0,75 s (37,5) không nguyên.
        TestHooks.SetField(egg, "_restUntil", ClockService.Instance.GameTime);
        before = ClockService.Instance.RemainingTime;
        receiver.OnPressStart(Vector2.zero);
        Assert.IsTrue(TestHooks.GetFieldInHierarchy<bool>(egg, "isHolding"), "(b) Đúng lúc hết hồi: bắt đầu giữ được");
        for (int i = 0; i < holdFrames; i++) { yield return null; receiver.OnPressUpdate(Vector2.zero); }
        receiver.OnPressEnd(Vector2.zero);
        Assert.That(ClockService.Instance.RemainingTime - before, Is.EqualTo(Config.timePayout - holdFrames / (float)Fps).Within(0.01f), "(b) Giữ đủ: được cộng giờ");

        // (c) Sau cooldown + 0,05 s: bình thường.
        yield return Frames(Mathf.CeilToInt((cooldown + 0.05f) * Fps));
        before = ClockService.Instance.RemainingTime;
        yield return Hold(receiver, holdFrames);
        Assert.That(ClockService.Instance.RemainingTime - before, Is.EqualTo(Config.timePayout - holdFrames / (float)Fps).Within(0.01f), "(c) Hết hồi: được cộng giờ");
    }
}
