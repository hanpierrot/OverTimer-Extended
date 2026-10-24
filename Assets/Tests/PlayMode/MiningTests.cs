using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.3.1 của kế hoạch kiểm thử, TC-MIN-1, 2, 3, 5, 6, 7 (TC-MIN-4 và TC-MIN-8 là [Dự kiến]).
/// Một “nhát đào” gọi MiningApp.OnHit, đúng hàm mà PickaxeView.Hit kích hoạt khi người chơi kéo cuốc vào khối và kéo ra.
/// TC-MIN-6 kiểm thử riêng giới hạn tốc độ của PickaxeView.
/// </summary>
public class MiningTests : GameSceneTest
{
    private MiningApp _app;

    [UnitySetUp]
    public IEnumerator OpenMining()
    {
        UseFixedFrameRate(50);
        _app = TestHooks.Find<MiningApp>();
        yield return OpenLaptopApp(_app);
    }

    private List<BlockTypeDefinition> Vein => TestHooks.GetField<List<BlockTypeDefinition>>(_app, "_vein");
    private int BlockIndex => TestHooks.GetField<int>(_app, "_blockIndex");
    private int Unconfirmed => TestHooks.GetField<int>(_app, "_unconfirmed");
    private BlockConfig Blocks => TestHooks.GetField<BlockConfig>(_app, "blocks");

    /// <summary>Sinh vein mới với độ dài cho trước, rồi trả RngService về seed của test.</summary>
    private void StartVeinOfLength(int length)
    {
        TestHooks.SetAutoProperty(RngService.Instance, "Random", new FixedRandom(length));
        TestHooks.Call(_app, "StartVein");
        TestHooks.SeedRng(TestSeed);
        Assert.AreEqual(length, Vein.Count);
    }

    private void Hit() => TestHooks.Call(_app, "OnHit");

    private void MineUntilBlockIndex(int index)
    {
        for (int i = 0; i < 500 && BlockIndex < index; i++) Hit();
        Assert.AreEqual(index, BlockIndex, "Không phá được đủ khối");
    }

    private void MineWholeVein()
    {
        int length = Vein.Count;
        MineUntilBlockIndex(length);
    }

    // ---------- TC-MIN-1 ----------

    [Test]
    public void TC_MIN_1_DoDaiVein_TrongKhoang6Den10()
    {
        var seen = new HashSet<int>();
        for (int i = 0; i < 1000; i++)
        {
            TestHooks.Call(_app, "StartVein");
            int length = Vein.Count;
            Assert.That(length, Is.InRange(6, 10), $"Vein thứ {i} dài {length}");
            seen.Add(length);
        }

        for (int length = 6; length <= 10; length++)
            Assert.IsTrue(seen.Contains(length), $"Sau 1.000 vein chưa gặp độ dài {length}");
    }

    // ---------- TC-MIN-2 ----------

    [Test]
    public void TC_MIN_2_ThuTuKhoi_TheoBlockConfig()
    {
        StartVeinOfLength(10);

        for (int i = 0; i < 10; i++)
            Assert.AreSame(Blocks.blocks[i], Vein[i], $"Khối thứ {i + 1} sai thứ tự");
        Assert.AreEqual("Dirt", Vein[0].label);
        Assert.AreEqual("Wood", Vein[1].label);
        Assert.AreEqual("Stone", Vein[2].label);
        Assert.AreEqual("Coal", Vein[3].label);
    }

    // ---------- TC-MIN-3 ----------

    [UnityTest]
    public IEnumerator TC_MIN_3_HoanThanhVein6Khoi_Nhan29VaVeinMoiSau1Giay()
    {
        StartVeinOfLength(6);
        TestHooks.SetMoney(0);

        MineWholeVein();

        Assert.AreEqual(29, MoneyService.Instance.Current);
        Assert.AreEqual(0, Unconfirmed);

        yield return Frames(45);           // 0,9 s
        Assert.AreEqual(6, BlockIndex, "Vein mới xuất hiện trước 1,0 s");

        yield return Frames(10);           // 1,1 s
        Assert.AreEqual(0, BlockIndex, "Sau 1,0 s phải có vein mới");
    }

    // ---------- TC-MIN-5 ----------

    [Test]
    public void TC_MIN_5_DongAppGiuaVein_MatTienChuaXacNhan()
    {
        StartVeinOfLength(6);
        TestHooks.SetMoney(20);
        MineUntilBlockIndex(3);
        Assert.AreEqual(1 + 2 + 4, Unconfirmed);
        float clockBefore = ClockService.Instance.RemainingTime;

        Laptop.CloseCurrentApp();

        Assert.AreEqual(0, Unconfirmed);
        Assert.AreEqual(20, MoneyService.Instance.Current);
        Assert.AreEqual(clockBefore, ClockService.Instance.RemainingTime, "Đóng ứng dụng không được trừ Clock");
    }

    // ---------- TC-MIN-6 ----------

    [Test]
    public void TC_MIN_6_GioiHanTocDoNhatDao()
    {
        var pickaxe = TestHooks.GetField<PickaxeView>(_app, "pickaxe");
        float cooldown = TestHooks.GetField<float>(pickaxe, "hitCooldown");
        int hits = 0;
        pickaxe.Hit += () => hits++;

        // Nhát trước diễn ra cách đây T - 0,01 s: nhát này phải bị bỏ qua.
        TestHooks.SetField(pickaxe, "_lastHitTime", UnityEngine.Time.time - (cooldown - 0.01f));
        TestHooks.Call(pickaxe, "TryHit");
        Assert.AreEqual(0, hits, $"Nhát cách {cooldown - 0.01f:0.00} s (< T = {cooldown} s) phải bị bỏ qua");

        // Cách T + 0,01 s: hợp lệ.
        TestHooks.SetField(pickaxe, "_lastHitTime", UnityEngine.Time.time - (cooldown + 0.01f));
        TestHooks.Call(pickaxe, "TryHit");
        Assert.AreEqual(1, hits, $"Nhát cách {cooldown + 0.01f:0.00} s (> T) phải hợp lệ");

        // Đúng bằng T: mã dùng phép so sánh “< T” nên bằng T là hợp lệ (xác nhận bằng rà soát mã,
        // vì phép trừ số thực của Time.time không cho ra đúng T để kiểm thử tự động ổn định).
    }

    // ---------- TC-MIN-7 ----------

    [Test]
    public void TC_MIN_7a_LuckyRuby_NhanDoi()
    {
        StartVeinOfLength(6);
        TestHooks.SetMoney(0);
        MiningApp.QueueNextVeinMultiplier(2f);   // hiệu ứng thẻ Overtime / Lucky Ruby (OvertimeCardSO.OnPlaced)

        MineWholeVein();

        Assert.AreEqual(58, MoneyService.Instance.Current);
    }

    [Test]
    public void TC_MIN_7b_ButterFinger_KhongNhanGi()
    {
        StartVeinOfLength(6);
        TestHooks.SetMoney(0);
        MiningApp.QueueNextVeinSlip();           // hiệu ứng thẻ Butterfingers

        MineWholeVein();

        Assert.AreEqual(0, MoneyService.Instance.Current);
    }
}
