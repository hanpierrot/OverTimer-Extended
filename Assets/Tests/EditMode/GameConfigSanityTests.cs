using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// Ràng buộc bất biến của GameConfig và của các asset biểu tượng vé, thay cho các ca "giá trị khớp kế hoạch" cũ
/// (những ca đó ghim từng con số và đỏ mỗi lần cân bằng lại). Ca ở đây chỉ đỏ khi cấu hình vô lý hoặc phá một
/// quy tắc thiết kế (DESIGN §2), không đỏ khi Ari chỉnh số trong giới hạn hợp lý.
/// </summary>
public class GameConfigSanityTests
{
    private GameConfig _c;

    [OneTimeSetUp]
    public void LoadConfig() => _c = TestAssets.LoadGameConfig();

    // ---------- Clock, Meter, tiền (I1, I6) ----------

    [Test]
    public void Clock_ThuaLonHonThang_VaThuaTai0()
    {
        Assert.AreEqual(0f, _c.loseClock, "I1: thua khi Clock về 0");
        Assert.Greater(_c.startClock, _c.loseClock);
        Assert.Greater(_c.winClock, _c.startClock, "I1: thắng ở mốc cao hơn lúc bắt đầu");
    }

    [Test]
    public void TyGiaMeter_Bang1()
    {
        Assert.AreEqual(1f, _c.exchangeRate, "I6: $1 = 1 s, cố định");
        Assert.Greater(_c.meterMinFeed, 0);
        Assert.LessOrEqual(_c.meterMinFeed, _c.startMoney, "Tiền khởi đầu phải đủ để dùng Meter lần đầu");
    }

    // ---------- Egg Timer (I3, P18) ----------

    [Test]
    public void EggTimer_LoiRongKhongVuot075GiayMoiGiay()
    {
        Assert.Greater(_c.crankTime, 0f);
        Assert.Greater(_c.timePayout, 0f);
        Assert.GreaterOrEqual(_c.eggTimerCooldown, 0f);
        float net = _c.timePayout / (_c.crankTime + _c.eggTimerCooldown) - 1f;   // giây Clock thu về trừ giây Clock trôi
        Assert.LessOrEqual(net, 0.75f + 1e-4f, "P18: Egg Timer ròng tối đa +0,75 s/s, nếu không nó thành nguồn thời gian vô hạn");
    }

    // ---------- Mining ----------

    [Test]
    public void Mining_DoDaiVeinHopLe_VaBangThuongTangDan()
    {
        Assert.GreaterOrEqual(_c.veinLengthMin, 1);
        Assert.LessOrEqual(_c.veinLengthMin, _c.veinLengthMax);
        Assert.GreaterOrEqual(_c.blockPayout.Length, _c.veinLengthMax, "Cần đủ khối cho vein dài nhất");
        Assert.GreaterOrEqual(_c.veinRespawn, 0f);
        for (int i = 1; i < _c.blockPayout.Length; i++)
            Assert.GreaterOrEqual(_c.blockPayout[i], _c.blockPayout[i - 1], $"Khối {i + 1} phải trả không ít hơn khối {i}");
    }

    // ---------- Blackjack ----------

    [Test]
    public void Blackjack_ThuongNaturalLonHonThangLonHonHoa()
    {
        Assert.Greater(_c.blackjackPushPayout, 0);
        Assert.Greater(_c.blackjackWinPayout, _c.blackjackPushPayout, "Thắng phải trả hơn hòa");
        Assert.Greater(_c.blackjackNaturalPayout, _c.blackjackWinPayout, "Natural phải trả hơn thắng thường");
        Assert.Greater(_c.blackjackAnte, 0f);
        Assert.GreaterOrEqual(_c.blackjackClockMargin, 0f);
        Assert.Greater(_c.blackjackStreakStep, 0f);
        Assert.GreaterOrEqual(_c.blackjackStreakCap, _c.blackjackStreakStep, "Trần Hot Table phải chứa ít nhất một bậc");
    }

    // ---------- Vé số, Card Box ----------

    [Test]
    public void Ve_GiaVaThuongCoBanDuong()
    {
        Assert.Greater(_c.tripleMatchCost, 0);
        Assert.Greater(_c.tripleMatchBaseReward, 0);
        Assert.Greater(_c.luckyNineCost, 0);
        Assert.Greater(_c.luckyNineBaseReward, 0);
    }

    [Test]
    public void CardBox_HandCapVaTrongSoDoHiem()
    {
        Assert.Greater(_c.handCap, 0);
        Assert.LessOrEqual(_c.handCap, 7, "Pool CardItem cố định 7 lá (E8)");
        Assert.Greater(_c.packCost, 0);
        Assert.Greater(_c.commonWeight, 0f);
        Assert.Greater(_c.rareWeight, 0f);
        Assert.Greater(_c.superRareWeight, 0f);
        Assert.Greater(_c.ultrRareWeight, 0f);
        Assert.GreaterOrEqual(_c.debuffDismissFee, 0, "Phí dismiss không được âm");
    }

    // ---------- Phone, ITRS, Cat ----------

    [Test]
    public void Phone_KhoangReoHopLe_VaCuocGoiLamClockNhanh()
    {
        Assert.Greater(_c.ringIntervalMin, 0f);
        Assert.LessOrEqual(_c.ringIntervalMin, _c.ringIntervalMax);
        Assert.Greater(_c.maxRingDuration, 0f);
        Assert.Greater(_c.ringEffectMultiplier, 1f, "I2: cuộc gọi làm Clock chạy nhanh hơn");
        Assert.GreaterOrEqual(_c.annoyanceMinGap, 0f);
    }

    [Test]
    public void Itrs_ThueHopLe()
    {
        Assert.Greater(_c.billInterval, 0f);
        Assert.That(_c.billRate, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
        Assert.Greater(_c.billMinimum, 0);
        Assert.Greater(_c.shortfallPenalty, 0f);
        Assert.Greater(_c.taxPayWindow, 0f);
    }

    [Test]
    public void Cat_KhoangThoiGianVaXacSuatHopLe()
    {
        Assert.Greater(_c.catSitDurationMin, 0f);
        Assert.LessOrEqual(_c.catSitDurationMin, _c.catSitDurationMax);
        Assert.Greater(_c.catBlockDurationMin, 0f);
        Assert.LessOrEqual(_c.catBlockDurationMin, _c.catBlockDurationMax);
        Assert.That(_c.catBlockChance, Is.InRange(0f, 1f));
    }

    // ---------- Billiards ----------

    [Test]
    public void Billiards_CauHinhQuaKiemTraCuaGame()
    {
        Assert.IsTrue(_c.ValidateBilliards(out string problem), $"GameConfig.ValidateBilliards: {problem}");
        Assert.Greater(_c.billiardsBetStep, 0);
    }

    // ---------- Ending phụ (DESIGN §5.14 D) ----------

    [Test]
    public void EndingPhu_XacSuatVaNguongHopLe()
    {
        Assert.That(_c.luckyNineJackpotChance, Is.InRange(0f, 1f));
        Assert.That(_c.luckyNineCurseChance, Is.InRange(0f, 1f));
        Assert.Less(_c.luckyNineJackpotChance + _c.luckyNineCurseChance, 1f, "Hai dải Jackpot và Curse không được phủ hết vé");
        Assert.That(_c.luckyNineCurseOmenWindows, Is.InRange(1, 9), "Lucky Nine có 9 ô: điềm báo phải xuất hiện trước ô cuối");
        Assert.Greater(_c.curseBeatSeconds, 0f);

        Assert.GreaterOrEqual(_c.miningStreakTarget, 2, "Mother Lode cần ít nhất 2 vein liên tiếp");
        Assert.That(_c.miningFullVeinStreakChance, Is.InRange(0f, 1f));

        Assert.GreaterOrEqual(_c.blackjackCharlieCards, 5, "Charlie là một ván dài, ít nhất 5 lá");
        Assert.That(_c.blackjackCharlieChance, Is.InRange(0f, 1f));
    }

    [Test]
    public void DiemRating_TrongKhoang01_VaTongTrongSoDuong()
    {
        foreach (var score in new[]
                 {
                     _c.ratingClockWin, _c.ratingSetCompletionWin, _c.ratingFinalCountdownWin, _c.ratingClockLoss,
                     _c.ratingFinalCountdownLoss, _c.ratingCreepyJesterLoss, _c.ratingMechanicWin, _c.ratingMechanicLoss,
                 })
            Assert.That(score, Is.InRange(0f, 1f));
        Assert.Greater(_c.ratingOutcomeWeight + _c.ratingSurvivalWeight + _c.ratingMoneyWeight, 0f);
        Assert.Greater(_c.ratingMoneyHalfPoint, 0f);
        Assert.GreaterOrEqual(_c.ratingMechanicWin, _c.ratingMechanicLoss, "Thắng bằng cơ chế không được điểm thấp hơn thua");
    }

    // ---------- Asset biểu tượng vé ----------

    private static TicketSymbolConfig[] LoadSymbolConfigs() =>
        AssetDatabase.FindAssets("t:TicketSymbolConfig")
            .Select(g => AssetDatabase.LoadAssetAtPath<TicketSymbolConfig>(AssetDatabase.GUIDToAssetPath(g))).ToArray();

    [Test]
    public void BieuTuongVe_TrongSoDuong_HeSoKhongAm()
    {
        var configs = LoadSymbolConfigs();
        Assert.IsNotEmpty(configs, "Không tìm thấy asset TicketSymbolConfig nào");
        foreach (var cfg in configs)
        {
            Assert.IsNotEmpty(cfg.symbolPool, $"{cfg.name}: không có biểu tượng");
            Assert.Greater(cfg.symbolPool.Sum(s => s.weight), 0f, $"{cfg.name}: tổng trọng số phải dương");
            foreach (var s in cfg.symbolPool)
            {
                Assert.GreaterOrEqual(s.weight, 0f, $"{cfg.name}: trọng số âm");
                Assert.GreaterOrEqual(s.payoutMultiplier, 0f, $"{cfg.name}: hệ số âm");
                Assert.IsNotNull(s.sprite, $"{cfg.name}: biểu tượng thiếu sprite");
            }
        }
    }

    [Test]
    public void BieuTuongVe_BieuTuongHiemNhatCoHeSoCaoNhat()
    {
        // Jackpot dùng biểu tượng hệ số cao nhất, Curse dùng thấp nhất; DESIGN §5.3: hiếm hơn thì trả cao hơn.
        foreach (var cfg in LoadSymbolConfigs())
        {
            var pool = cfg.symbolPool;
            int rarest = System.Array.IndexOf(pool, pool.OrderBy(s => s.weight).First());
            int commonest = System.Array.IndexOf(pool, pool.OrderByDescending(s => s.weight).First());
            Assert.AreEqual(pool[rarest].payoutMultiplier, pool[ScratchTicket.IndexOfHighestMultiplier(pool)].payoutMultiplier,
                $"{cfg.name}: biểu tượng hiếm nhất phải có hệ số cao nhất");
            Assert.AreEqual(pool[commonest].payoutMultiplier, pool[ScratchTicket.IndexOfLowestMultiplier(pool)].payoutMultiplier,
                $"{cfg.name}: biểu tượng phổ biến nhất phải có hệ số thấp nhất");
        }
    }
}
