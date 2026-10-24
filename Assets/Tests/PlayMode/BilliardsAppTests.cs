using System.Collections;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.3.5 của kế hoạch kiểm thử, TC-BIL-12…19: phần BilliardsApp (tiền cược, thuế, đóng ứng dụng, thẻ).
/// Luật và tiền thưởng (TC-BIL-1…11) đã kiểm thử ở Edit Mode.
/// Bàn luôn là straight_pot_01, không lật gương (công cụ debug của Ari: BilliardsDebug.PresetId / Mirror).
/// Cú đánh đi qua BilliardsApp.Shoot, đúng hàm mà CueAimController gọi khi người chơi thả cơ.
/// Chưa có asset cho thẻ Chalk Up và Cue Slip (Ari chỉ viết class), nên TC-BIL-16, 17 dùng thẳng BilliardsPendingEffects.
/// </summary>
public class BilliardsAppTests : GameSceneTest
{
    private const string Table = "straight_pot_01";
    private const int Fps = 50;

    private BilliardsApp _app;

    [UnitySetUp]
    public IEnumerator OpenBilliards()
    {
        BilliardsDebug.PresetId = Table;
        BilliardsDebug.Mirror = BilliardsDebug.MirrorMode.None;
        UseFixedFrameRate(Fps);
        _app = TestHooks.Find<BilliardsApp>();
        yield return OpenLaptopApp(_app);
        Assert.AreEqual(Table, _app.Preset?.id, "Không dựng được bàn straight_pot_01");
        Assert.AreEqual("Racked", _app.StateName);
    }

    private static int Money => MoneyService.Instance.Current;

    private IEnumerator Shoot(ShotInput shot)
    {
        _app.Shoot(shot);
        Assert.AreEqual("Rolling", _app.StateName);
        for (int i = 0; i < 60 * Fps && _app.StateName == "Rolling"; i++) yield return null;
        Assert.AreNotEqual("Rolling", _app.StateName, "Bi không dừng sau 60 s");
    }

    private IEnumerator PlaySolutionToWin()
    {
        foreach (var shot in _app.Preset.solution)
        {
            yield return Shoot(shot);
            if (_app.StateName != "Aiming") break;
        }
        Assert.AreEqual("Won", _app.StateName, "Lời giải lưu sẵn phải thắng bàn");
    }

    private IEnumerator ReopenApp()
    {
        Laptop.CloseCurrentApp();
        yield return null;
        yield return OpenLaptopApp(_app);
    }

    // ---------- TC-BIL-12 ----------

    [Test]
    public void TC_BIL_12a_Money9_KhongVaoDuocVan()
    {
        TestHooks.SetMoney(9);

        _app.PlayRound(0);

        Assert.AreEqual("Racked", _app.StateName);
        Assert.AreEqual(9, Money);
    }

    [Test]
    public void TC_BIL_12b_Money10_VaoVan()
    {
        TestHooks.SetMoney(10);

        _app.PlayRound(0);

        Assert.AreEqual("Aiming", _app.StateName);
        Assert.AreEqual(0, Money);
    }

    // ---------- TC-BIL-13 ----------

    [UnityTest]
    public IEnumerator TC_BIL_13_TheHighRiskHighReward_TruGap15ThuongTheoCuocGoc()
    {
        Cards.Place(Cards.Asset("BilliardsCostModifier"));        // ×1,5 cho Billiards trong 20 s
        TestHooks.SetMoney(50);

        _app.PlayRound(0);
        Assert.AreEqual(15, _app.Charge);
        Assert.AreEqual(35, Money);

        yield return PlaySolutionToWin();

        Assert.AreEqual(24, _app.PayoutOnWin, "Thưởng tính từ cược gốc $10");
        Assert.AreEqual(35 + 24, Money);
    }

    // ---------- TC-BIL-14 ----------

    [UnityTest]
    public IEnumerator TC_BIL_14_ThueChiTinhTrenLoiNhuan()
    {
        TestHooks.SetMoney(10);
        _app.PlayRound(0);
        int taxBase = TestHooks.GetField<int>(ITRSService.Instance, "earnedSinceLastBill");
        int earned = TestHooks.GetField<int>(RunStatsService.Instance, "_totalMoneyEarned");

        yield return PlaySolutionToWin();

        Assert.AreEqual(24, Money);
        Assert.AreEqual(taxBase + 14, TestHooks.GetField<int>(ITRSService.Instance, "earnedSinceLastBill"),
            "ITRS chỉ tính $14 lợi nhuận, không tính $10 tiền cược hoàn lại");
        Assert.AreEqual(earned + 14, TestHooks.GetField<int>(RunStatsService.Instance, "_totalMoneyEarned"),
            "Money Earned chỉ tăng $14");
    }

    // ---------- TC-BIL-15 ----------

    [UnityTest]
    public IEnumerator TC_BIL_15a_DongKhiRacked_GiuBan()
    {
        TestHooks.SetMoney(30);

        yield return ReopenApp();

        Assert.AreEqual("Racked", _app.StateName);
        Assert.AreEqual(Table, _app.Preset.id);
        Assert.AreEqual(30, Money);
    }

    [UnityTest]
    public IEnumerator TC_BIL_15b_DongKhiAiming_GiuVanVaTienCuoc()
    {
        TestHooks.SetMoney(30);
        _app.PlayRound(0);
        int shots = _app.Round.ShotsLeft;
        var round = _app.Round;

        yield return ReopenApp();

        Assert.AreEqual("Aiming", _app.StateName);
        Assert.AreSame(round, _app.Round, "Vẫn là ván cũ");
        Assert.AreEqual(shots, _app.Round.ShotsLeft);
        Assert.AreEqual(20, Money, "Không hoàn cũng không trừ thêm tiền cược");
    }

    [UnityTest]
    public IEnumerator TC_BIL_15c_DongKhiRolling_MatTienCuoc()
    {
        TestHooks.SetMoney(30);
        _app.PlayRound(0);
        _app.Shoot(_app.Preset.solution[0]);
        Assert.AreEqual("Rolling", _app.StateName);

        yield return ReopenApp();

        Assert.AreEqual("Racked", _app.StateName, "Ván bị hủy, bàn mới");
        Assert.IsNull(_app.Round);
        Assert.AreEqual(20, Money, "Mất $10 tiền cược");
        Assert.AreEqual("You left mid-shot. That bet is lost.", TestHooks.GetField<string>(_app, "_notice"));
    }

    [UnityTest]
    public IEnumerator TC_BIL_15d_DongKhiDaThang_DonBan()
    {
        TestHooks.SetMoney(10);
        _app.PlayRound(0);
        yield return PlaySolutionToWin();

        yield return ReopenApp();

        Assert.AreEqual("Racked", _app.StateName);
        Assert.IsNull(_app.Round, "Bàn đã thắng phải được dọn");
    }

    // ---------- TC-BIL-16 ----------

    [UnityTest]
    public IEnumerator TC_BIL_16a_ChalkUp_ThemMotCu_ThuongKhongDoi()
    {
        BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.ExtraShots, 1, null);
        TestHooks.SetMoney(10);

        _app.PlayRound(0);

        Assert.AreEqual(7, _app.Round.ShotsLeft, "Easy par 2 = 6 cú, +1 cú Chalk Up");
        Assert.AreEqual(24, _app.PayoutOnWin, "Thưởng không đổi");
        Assert.AreEqual(0, BilliardsPendingEffects.ExtraShots, "Hiệu ứng đã dùng");
        yield return null;
    }

    [Test]
    public void TC_BIL_16b_HaiTheChalkUp_PawnMot_ConMotCu()
    {
        var pool = TestHooks.GetField<CardItem[]>(CardHandManager.Instance, "pool");
        BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.ExtraShots, 1, pool[0]);
        BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.ExtraShots, 1, pool[1]);
        Assert.AreEqual(2, BilliardsPendingEffects.ExtraShots);

        BilliardsPendingEffects.Withdraw(pool[0]);              // thẻ thứ nhất bị cầm đồ

        Assert.AreEqual(1, BilliardsPendingEffects.ExtraShots);
    }

    // ---------- TC-BIL-17 ----------

    [UnityTest]
    public IEnumerator TC_BIL_17_CueSlip_LucLechTrong015VaTaiLapTheoSeed()
    {
        BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.CueSlip, 0.15f, null);
        TestHooks.SetMoney(10);
        _app.PlayRound(0);
        const int seed = 777;
        var aimed = ShotInput.Quantized(30f, 0.80f);
        float expectedFactor = 1f + (float)(new System.Random(seed).NextDouble() * 2.0 - 1.0) * 0.15f;
        float expected = ShotInput.Quantized(aimed.angle, aimed.power * expectedFactor).power;
        TestHooks.SeedRng(seed);

        yield return Shoot(aimed);

        string note = TestHooks.GetField<string>(_app, "_shotNote");
        Assert.IsNotNull(note, "Phải có thông báo Cue slip");
        float actual = float.Parse(note.Split(' ').Last(), CultureInfo.InvariantCulture);
        Assert.That(actual, Is.InRange(0.65f, 0.95f), note);
        Assert.AreEqual(expected, actual, 0.005f, "Cùng seed phải cho cùng lực");
        Assert.IsFalse(BilliardsPendingEffects.CueSlipPending, "Thẻ kết thúc sau cú đánh");
    }

    // ---------- TC-BIL-18 ----------

    [UnityTest]
    public IEnumerator TC_BIL_18_NotValid8Ball_KhoaIconRoiMoLai_VanCuConNguyen()
    {
        TestHooks.SetMoney(30);
        _app.PlayRound(0);
        var round = _app.Round;
        Laptop.CloseCurrentApp();
        var icon = Object.FindObjectsByType<DesktopIconView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .First(i => TestHooks.GetField<string>(i, "appName") == "Billiards");
        var card = Cards.Asset("DisableBilliards");
        float duration = TestHooks.GetField<float>(card, "duration");

        Cards.Place(card);
        Assert.IsFalse(icon.gameObject.activeSelf, "Trong thời gian thẻ hiệu lực: icon Billiards bị ẩn");

        yield return Frames(Mathf.CeilToInt(duration * Fps) + 5);
        Assert.IsTrue(icon.gameObject.activeSelf, $"Sau {duration} s: icon hiện lại");

        yield return OpenLaptopApp(_app);
        Assert.AreEqual("Aiming", _app.StateName);
        Assert.AreSame(round, _app.Round, "Ván cũ vẫn còn nguyên");
    }

    // ---------- TC-BIL-19 ----------

    [UnityTest]
    public IEnumerator TC_BIL_19_ChoiLai_XoaHieuUngDangCho()
    {
        BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.ExtraShots, 1, null);
        BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.ExtraShots, 1, null);
        Cards.Place(Cards.Plain(0));

        GameManager.Instance.Restart();
        yield return null;
        yield return null;

        Assert.AreEqual(0, BilliardsPendingEffects.ExtraShots);
        Assert.AreEqual(0, Cards.HandCount, "Bàn không còn thẻ");
    }
}
