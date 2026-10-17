using NUnit.Framework;

/// <summary>
/// Mục 4.3.5 của kế hoạch kiểm thử, TC-BIL-7…11: số cú, hết cú, tiền thưởng, tái lập mô phỏng
/// và lời giải của các bàn phát hành. Dùng GameConfig.asset và Resources/BilliardsPresets thật.
/// </summary>
public class BilliardsRoundTests
{
    private const string StraightPot = "straight_pot_01";

    private GameConfig _config;

    [OneTimeSetUp]
    public void LoadConfig() => _config = TestAssets.LoadGameConfig();

    private BilliardsRound NewRound(BilliardsPreset preset, BilliardsDifficulty difficulty, int bet = 10)
        => new BilliardsRound(preset, default, difficulty, bet, _config);

    private static ShotResult FireToRest(BilliardsRound round, ShotInput shot)
    {
        round.Fire(shot);
        round.Sim.RunToRest();
        return round.Resolve();
    }

    // ---------- TC-BIL-7 ----------

    [Test]
    public void TC_BIL_7a_SoCuTheoDoKho_TuPar()
    {
        var preset = new BilliardsPreset { par = 2 };
        int[] offsets = _config.billiardsParOffsets;

        Assert.AreEqual(6, preset.ShotsFor(BilliardsDifficulty.Easy, offsets));
        Assert.AreEqual(4, preset.ShotsFor(BilliardsDifficulty.Medium, offsets));
        Assert.AreEqual(2, preset.ShotsFor(BilliardsDifficulty.Hard, offsets));
    }

    [Test]
    public void TC_BIL_7b_SoCuGhiDe_ChiApDungChoDoKhoDuocGhi()
    {
        var preset = new BilliardsPreset { par = 2, shots = new[] { 7, 0, 0 } };
        int[] offsets = _config.billiardsParOffsets;

        Assert.AreEqual(7, preset.ShotsFor(BilliardsDifficulty.Easy, offsets));
        Assert.AreEqual(4, preset.ShotsFor(BilliardsDifficulty.Medium, offsets));
    }

    [Test]
    public void SoCu_GiaTriTrongGameConfigKhopKeHoach()
    {
        CollectionAssert.AreEqual(new[] { 4, 2, 0 }, _config.billiardsParOffsets,
            "Kế hoạch ghi billiardsParOffsets = 4 / 2 / 0");
    }

    // ---------- TC-BIL-8 ----------

    [Test]
    public void TC_BIL_8_HetCu_ThuaOutOfShots()
    {
        var round = NewRound(TestAssets.LoadPreset(StraightPot), BilliardsDifficulty.Hard);
        Assert.AreEqual(2, round.ShotsLeft);

        // Cú rất nhẹ hướng xuống: bi cái chỉ lăn vài cm, không chạm bi nào, không vào lỗ.
        var weakShot = ShotInput.Quantized(-90f, 0.05f);

        var first = FireToRest(round, weakShot);
        Assert.AreEqual(Verdict.Continue, first.verdict);
        Assert.AreEqual(1, round.ShotsLeft);
        Assert.IsFalse(round.IsOver);

        var second = FireToRest(round, weakShot);
        Assert.AreEqual(Verdict.Fail, second.verdict);
        Assert.AreEqual("Out of shots", second.failReason);
        Assert.IsTrue(round.IsOver);
    }

    // ---------- TC-BIL-9 ----------

    [TestCase(10, BilliardsDifficulty.Easy, 24)]
    [TestCase(10, BilliardsDifficulty.Medium, 40)]
    [TestCase(10, BilliardsDifficulty.Hard, 48)]
    [TestCase(40, BilliardsDifficulty.Easy, 96)]
    [TestCase(40, BilliardsDifficulty.Medium, 158)]
    [TestCase(40, BilliardsDifficulty.Hard, 192)]
    public void TC_BIL_9_TienThuongKhiThang(int bet, BilliardsDifficulty difficulty, int expected)
    {
        var preset = TestAssets.LoadPreset(StraightPot);

        Assert.AreEqual(expected, BilliardsPayout.OnWin(bet, difficulty, preset, _config));
    }

    [Test]
    public void TienThuong_GiaTriTrongGameConfigKhopKeHoach()
    {
        // Các số $24 / $40 / $48 ở TC-BIL-9 được tính từ những giá trị này. Nếu nhóm cân bằng lại
        // Billiards (BILLIARDS_BALANCE.md đề xuất đổi billiardsTargetRate), ca này báo để cập nhật tài liệu.
        CollectionAssert.AreEqual(new[] { 1.5f, 3f, 4f }, _config.billiardsTargetRate);
        CollectionAssert.AreEqual(new[] { 0.85f, 0.65f, 0.5f }, _config.billiardsAssumedWinRate);
        Assert.AreEqual(7f, _config.billiardsSecondsPerShot);
        Assert.AreEqual(40, _config.billiardsBetMax);
    }

    // ---------- TC-BIL-10 ----------

    [Test]
    public void TC_BIL_10_CungCuDanh_KetQuaGiongHetNhau()
    {
        var preset = TestAssets.LoadPreset(StraightPot);
        var physics = BilliardsSimParams.FromConfig(_config);
        var shot = ShotInput.Quantized(27f, 0.45f);

        var a = new BilliardsSim(preset, physics);
        var b = new BilliardsSim(preset, physics);
        a.Shoot(shot);
        b.Shoot(shot);
        var reportA = a.RunToRest();
        var reportB = b.RunToRest();

        Assert.AreEqual(reportA.firstContact, reportB.firstContact);
        Assert.AreEqual(reportA.cueScratched, reportB.cueScratched);
        CollectionAssert.AreEqual(reportA.pots, reportB.pots);
        for (int i = 0; i < a.BallCount; i++)
            Assert.AreEqual(a.Position(i), b.Position(i), $"Bi {a.IdOf(i)} dừng ở vị trí khác nhau");
        Assert.AreEqual(a.StateHash(), b.StateHash());
    }

    // ---------- TC-BIL-11 ----------

    [Test]
    public void TC_BIL_11_MoiBanPhatHanh_LoiGiaiThangTrongPar()
    {
        var presets = BilliardsPresetLibrary.All;
        Assert.That(presets.Count, Is.GreaterThan(0), "Không nạp được bàn nào từ Resources/BilliardsPresets");

        foreach (var preset in presets)
        {
            Assert.IsNotNull(preset.solution, $"{preset.id}: không có lời giải lưu sẵn");
            Assert.That(preset.solution.Length, Is.GreaterThan(0), $"{preset.id}: lời giải rỗng");
            Assert.That(preset.solution.Length, Is.LessThanOrEqualTo(preset.par),
                $"{preset.id}: lời giải dài hơn par");

            var round = NewRound(preset, BilliardsDifficulty.Easy);
            ShotResult last = null;
            foreach (var shot in preset.solution)
            {
                last = FireToRest(round, shot);   // lời giải đã được lượng tử hóa khi ghi, bắn nguyên như verifier
                if (last.verdict != Verdict.Continue) break;
            }

            Assert.AreEqual(Verdict.Win, last.verdict,
                $"{preset.id}: lời giải không thắng ({last.failReason})");
        }
    }
}
