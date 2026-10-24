using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.5.2 của kế hoạch kiểm thử, TC-CAT-1…5. Mèo “chạm vật” = CatController.OnTriggerEnter2D với collider
/// của một vật có PointerReceiver (dùng Egg Timer). Kéo mèo đi qua PointerReceiver.OnDragStart/End.
/// Ghi chú: kế hoạch ghi mèo ngồi 3–8 s và chặn 4–8 s; GameConfig.asset hiện là 6–14 s và 3–6 s.
/// Các ca dưới đây kiểm thử theo kế hoạch (tài liệu), nên lệch config sẽ hiện thành ca Lỗi.
/// </summary>
public class CatTests : GameSceneTest
{
    private const int Fps = 10;

    private CatController _cat;
    private CatStateMachine _sm;
    private PointerReceiver _target;
    private Collider2D _targetCollider;

    [UnitySetUp]
    public IEnumerator FindCat()
    {
        UseFixedFrameRate(Fps);
        yield return null;
        _cat = TestHooks.Find<CatController>();
        _sm = TestHooks.GetField<CatStateMachine>(_cat, "_sm");
        var egg = TestHooks.Find<EggTimerTask>();
        _target = egg.GetComponent<PointerReceiver>();
        _targetCollider = egg.GetComponentInChildren<Collider2D>(true);
        Assert.IsNotNull(_targetCollider, "Egg Timer cần Collider2D để mèo chạm được");
    }

    private CatState State => _sm.CurrentState;

    private void ForceState(CatState state) => _sm.ChangeState(state);

    private void Touch() => TestHooks.Call(_cat, "OnTriggerEnter2D", _targetCollider);

    /// <summary>Cho mèo chắc chắn chặn ở lần chạm này, rồi trả RngService về seed của test.</summary>
    private void TouchAndAlwaysBlock()
    {
        ForceState(CatState.Wandering);
        TestHooks.SetAutoProperty(RngService.Instance, "Random", new FixedRandom(0, 0.0));
        Touch();
        TestHooks.SeedRng(TestSeed);
        Assert.AreEqual(CatState.SittingBlocking, State);
    }

    [Test]
    public void GiaTriTrongGameConfig_SoVoiKeHoach()
    {
        Assert.AreEqual(0.4f, Config.catBlockChance, "Kế hoạch ghi cơ hội chặn 40%");
        TestContext.Out.WriteLine($"Config hiện tại: ngồi {Config.catSitDurationMin}–{Config.catSitDurationMax} s (kế hoạch 3–8 s), " +
                                  $"chặn {Config.catBlockDurationMin}–{Config.catBlockDurationMax} s (kế hoạch 4–8 s).");
    }

    // ---------- TC-CAT-1 ----------

    [UnityTest]
    public IEnumerator TC_CAT_1_DenDiem_NgoiRoiQuayLaiLangThang()
    {
        ForceState(CatState.Wandering);
        TestHooks.SetField(_cat, "_targetPos", (Vector2)_cat.transform.position);   // coi như đã đến đích

        yield return null;
        Assert.AreEqual(CatState.SittingIdle, State);

        int frames = 0;
        while (State == CatState.SittingIdle && frames < 30 * Fps) { yield return null; frames++; }
        float seconds = frames / (float)Fps;

        Assert.AreEqual(CatState.Wandering, State);
        Assert.That(seconds, Is.InRange(3f, 8f + 1f / Fps), $"Mèo ngồi {seconds:0.0} s; kế hoạch ghi 3–8 s");
    }

    // ---------- TC-CAT-2 ----------

    [Test]
    public void TC_CAT_2_CoHoiChanVat_40PhanTram()
    {
        const int n = 1000;
        int blocks = 0;
        for (int i = 0; i < n; i++)
        {
            ForceState(CatState.Wandering);
            Touch();
            if (State == CatState.SittingBlocking) blocks++;
        }
        ForceState(CatState.Wandering);

        double rate = blocks / (double)n;
        double tolerance = 3 * Math.Sqrt(0.4 * 0.6 / n);           // ≈ 4,6%
        Assert.That(rate, Is.EqualTo(0.4).Within(tolerance), $"Tỷ lệ chặn {rate:P1}");
    }

    // ---------- TC-CAT-3 ----------

    [UnityTest]
    public IEnumerator TC_CAT_3_ThoiGianChan_4Den8GiayRoiTuMo()
    {
        var durations = new List<float>();
        for (int i = 0; i < 50 && durations.Count < 20; i++)
        {
            ForceState(CatState.Wandering);
            Touch();                                             // dùng RNG thật của test (seed cố định)
            if (State == CatState.SittingBlocking) durations.Add(TestHooks.GetField<float>(_cat, "_blockTimer"));
        }
        Assert.That(durations.Count, Is.GreaterThan(0));

        foreach (float d in durations)
            Assert.That(d, Is.InRange(4f, 8f), $"Thời gian chặn {d:0.00} s; kế hoạch ghi 4–8 s");

        // Tự mở sau khi hết thời gian chặn: chặn thêm một lần nữa rồi chờ.
        for (int i = 0; i < 50 && State != CatState.SittingBlocking; i++)
        {
            ForceState(CatState.Wandering);
            Touch();
        }
        Assert.AreEqual(CatState.SittingBlocking, State);
        float blockFor = TestHooks.GetField<float>(_cat, "_blockTimer");
        Assert.IsFalse(_target.IsInteractable);
        yield return Frames(Mathf.CeilToInt(blockFor * Fps) + 2);
        Assert.AreEqual(CatState.Wandering, State);
        Assert.IsTrue(_target.IsInteractable, "Vật phải được mở lại");
    }

    // ---------- TC-CAT-4 ----------

    [Test]
    public void TC_CAT_4_KeoMeo_GoChan()
    {
        TouchAndAlwaysBlock();
        Assert.IsFalse(_target.IsInteractable);
        var catReceiver = _cat.GetComponent<PointerReceiver>();

        catReceiver.OnDragStart(_cat.transform.position);
        Assert.AreEqual(CatState.Dragged, State);
        Assert.IsTrue(_target.IsInteractable, "Kéo mèo đi là vật được gỡ chặn");

        catReceiver.OnDragEnd(_cat.transform.position);
        Assert.AreEqual(CatState.Wandering, State);
    }

    // ---------- TC-CAT-5 ----------

    [Test]
    public void TC_CAT_5_MeoKhongBiAnnoyanceManagerGioiHan()
    {
        Assert.IsTrue(AnnoyanceManager.Instance.TryBegin("test: Phone đang reo"));
        Assert.IsFalse(AnnoyanceManager.Instance.CanStart);

        TouchAndAlwaysBlock();

        Assert.AreEqual(CatState.SittingBlocking, State, "Mèo vẫn chặn được khi đang có phiền nhiễu khác");
        AnnoyanceManager.Instance.End("test");
    }
}
