using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Kế hoạch kiểm thử (Play Mode): TC-CLC-7, 8 (Clock tạm dừng, tốc độ chồng nhau), TC-CAM-1 … 4 (ba góc nhìn, ViewGate) và
/// TC-CAM-7, 8 (khóa Laptop theo từng khóa, ứng dụng dừng khi Laptop bị khóa), trên scene thật.
/// Nhấp vào vùng chuyển góc nhìn = gọi CameraViewButton.HandleClick, đúng hàm PointerReceiver.ClickDown kích hoạt.
/// Khóa Laptop của một cuộc gọi tạm dừng Clock = DeviceLayer.LockLaptop(holder), đúng lời gọi trong PhoneController.BeginCall.
/// Tốc độ, kích thước camera và thời gian chuyển đọc từ GameConfig / asset thẻ, không ghim số cân bằng.
/// Mỗi khung hình = 0,02 s (UseFixedFrameRate(50)).
/// </summary>
public class CameraClockTests : GameSceneTest
{
    private const int Fps = 50;

    private static ClockService Clock => ClockService.Instance;
    private static CameraRig Rig => CameraRig.Instance;
    private static Camera Cam => TestHooks.GetField<Camera>(Rig, "cam");

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        TestHooks.SetClock(500f);
        yield return null;
    }

    private static IEnumerator Seconds(float s) => Frames(Mathf.RoundToInt(s * Fps));

    private static CameraViewButton[] Buttons =>
        Object.FindObjectsByType<CameraViewButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

    private static Collider2D ColliderOf(Component c)
    {
        var col = c.GetComponentInChildren<Collider2D>(true);
        Assert.IsNotNull(col, $"{c.name} không có Collider2D");
        return col;
    }

    /// <summary>Vùng nhấp ở góc nhìn hiện tại dẫn tới góc nhìn target.</summary>
    private static CameraViewButton ZoneTo(CameraView target)
    {
        var zone = Buttons.FirstOrDefault(b => b.Target == target && ViewGate.IsAllowed(ColliderOf(b), Rig.CurrentView));
        Assert.IsNotNull(zone, $"Ở góc {Rig.CurrentView} không có vùng nhấp để sang {target}");
        return zone;
    }

    private static float SizeOf(CameraView view, GameConfig c) =>
        view == CameraView.Desk ? c.deskCameraSize : view == CameraView.Door ? c.doorCameraSize : c.roomCameraSize;

    // ---------- TC-CLC-7 ----------

    [UnityTest]
    public IEnumerator TC_CLC_7_GameDeltaBang0KhiTamDung()
    {
        var holder = new object();
        TestHooks.SetClock(100f);
        Clock.Pause(holder);
        for (int i = 0; i < 2 * Fps; i++)
        {
            yield return null;
            Assert.AreEqual(0f, Clock.GameDelta, "GameDelta = 0 khi tạm dừng");
        }
        Assert.AreEqual(100f, Clock.RemainingTime, "Clock vẫn 100");

        Clock.Resume(holder);
        yield return Seconds(1f);
        Assert.That(Clock.RemainingTime, Is.EqualTo(99f).Within(0.05f), "Sau Resume Clock chạy lại");
    }

    // ---------- TC-CLC-8 ----------

    [UnityTest]
    public IEnumerator TC_CLC_8_PhoneReoChongSnailSpeed_NhanTocDo()
    {
        var phone = PhoneController.Instance;
        phone.Calls.ForceRingNow();
        for (int i = 0; i < 10 && !phone.IsRinging; i++) yield return null;
        Assert.IsTrue(phone.IsRinging, "Phone phải reo");

        var snail = Cards.Asset("SnailSpeed");
        float slow = TestHooks.GetField<float>(snail, "tickMultiplier");
        var card = Cards.Place(snail);
        float ring = Config.ringEffectMultiplier;
        Assert.That(Clock.TickMultiplier, Is.EqualTo(ring * slow).Within(1e-4f), "Hai tốc độ nhân với nhau");

        TestHooks.SetClock(100f);
        yield return Seconds(4f);
        Assert.That(Clock.RemainingTime, Is.EqualTo(100f - 4f * ring * slow).Within(0.05f), $"Sau 4 s: Clock ≈ {100f - 4f * ring * slow}");

        Pawning.PawnOnto(card);
        Assert.AreEqual(ring, Clock.TickMultiplier, 1e-4f, "Pawn thẻ: chỉ còn tốc độ reo");
        float before = Clock.RemainingTime;
        yield return Seconds(2f);
        Assert.IsTrue(phone.IsRinging, "Phone vẫn reo");
        Assert.That(before - Clock.RemainingTime, Is.EqualTo(2f * ring).Within(0.05f), $"Clock giảm thêm ≈ {2f * ring} s");
    }

    // ---------- TC-CAM-1 ----------

    [UnityTest]
    public IEnumerator TC_CAM_1_ChuyenBaGocNhin_LuonQuaRoom()
    {
        Assert.AreEqual(CameraView.Room, Rig.CurrentView, "Ván bắt đầu ở Room");
        Assert.AreEqual(Config.roomCameraSize, Cam.orthographicSize, 1e-3f);

        var changes = new List<CameraView>();
        System.Action<CameraView> onChanged = v => changes.Add(v);
        Rig.ViewChanged += onChanged;

        var path = new[] { CameraView.Desk, CameraView.Room, CameraView.Door, CameraView.Room };
        foreach (var target in path)
        {
            var from = Rig.CurrentView;
            TestHooks.Call(ZoneTo(target), "HandleClick", Vector2.zero);
            Assert.IsTrue(Rig.IsMoving, $"Bắt đầu chuyển {from} → {target}");
            int frames = 0, changesBefore = changes.Count;
            while (Rig.IsMoving && frames < 5 * Fps)
            {
                Assert.AreEqual(from, Rig.CurrentView, "CurrentView chỉ đổi khi tween xong");
                Assert.AreEqual(changesBefore, changes.Count, "ViewChanged chưa phát trong lúc tween");
                yield return null;
                frames++;
            }
            Assert.AreEqual(target, Rig.CurrentView);
            Assert.AreEqual(changesBefore + 1, changes.Count, "ViewChanged phát một lần sau khi tween xong");
            Assert.That(frames / (float)Fps, Is.EqualTo(Config.cameraFocusSeconds).Within(0.06f), "Thời gian chuyển");
            Assert.AreEqual(SizeOf(target, Config), Cam.orthographicSize, 1e-3f, $"Kích thước camera ở {target}");

            if (target == CameraView.Desk)
                Assert.IsFalse(Buttons.Any(b => b.Target == CameraView.Door && ViewGate.IsAllowed(ColliderOf(b), CameraView.Desk)),
                    "Từ Desk không sang thẳng Door được (DESIGN §4.1)");
        }
        Rig.ViewChanged -= onChanged;
        CollectionAssert.AreEqual(path, changes);
    }

    // ---------- TC-CAM-2 ----------

    [UnityTest]
    public IEnumerator TC_CAM_2_GoiChuyenKhiDangChuyenHoacTrungGoc()
    {
        Assert.IsTrue(Rig.GoTo(CameraView.Desk));
        yield return null;
        Assert.IsFalse(Rig.GoTo(CameraView.Door), "Đang chuyển: GoTo khác bị từ chối");
        while (Rig.IsMoving) yield return null;
        Assert.AreEqual(CameraView.Desk, Rig.CurrentView, "Camera dừng ở Desk");

        Assert.IsFalse(Rig.GoTo(CameraView.Desk), "GoTo trùng góc nhìn bị từ chối");
        yield return Frames(5);
        Assert.IsFalse(Rig.IsMoving);
        Assert.AreEqual(CameraView.Desk, Rig.CurrentView);
    }

    // ---------- TC-CAM-3 ----------

    [UnityTest]
    public IEnumerator TC_CAM_3_ChanInputKhiDangChuyen_ClockVanChay()
    {
        TestHooks.SetClock(100f);
        Assert.IsTrue(InputManager.Instance.IsInputEnabled);
        Assert.IsTrue(Rig.GoTo(CameraView.Desk));
        int frames = 0;
        while (Rig.IsMoving)
        {
            // Mọi nhấp chuột lên vật (Egg Timer, Phone …) đi qua InputManager.OnClickStarted, hàm này dừng ngay khi IsInputEnabled = false.
            Assert.IsTrue(InputGate.IsBlocked, "InputGate bị chặn trong lúc chuyển");
            Assert.IsFalse(InputManager.Instance.IsInputEnabled, "Không nhận nhấp chuột lên vật");
            Assert.IsTrue(Clock.IsRunning, "Clock vẫn chạy");
            yield return null;
            frames++;
        }
        Assert.IsFalse(InputGate.IsBlocked, "Hết chặn khi tween xong");
        Assert.IsTrue(InputManager.Instance.IsInputEnabled);
        Assert.That(Clock.RemainingTime, Is.EqualTo(100f - frames / (float)Fps).Within(0.05f), "Clock giảm bình thường");
    }

    // ---------- TC-CAM-4 ----------

    [Test]
    public void TC_CAM_4_VatChiPhanHoiOGocNhinChoPhep()
    {
        var shop = ShopService.Instance;
        var inRoom = new (string name, Component obj)[]
        {
            ("bàn", ZoneTo(CameraView.Desk)),
            ("cửa", ZoneTo(CameraView.Door)),
            ("Picture", TestHooks.Find<PictureClickTask>()),
            ("Egg Timer", TestHooks.Find<EggTimerTask>()),
        };
        var deskOnly = new (string name, Component obj)[]
        {
            ("Phone", PhoneController.Instance),
            ("Printer", TestHooks.Find<Pawn>()),
            ("Bear", shop.BindingFor("bear")),
            ("Triple Match stack", shop.BindingFor("triple_stack")),
        };

        foreach (var (name, obj) in inRoom)
        {
            Assert.IsNotNull(obj, $"Không tìm thấy {name}");
            Assert.IsTrue(ViewGate.IsAllowed(ColliderOf(obj), CameraView.Room), $"{name} phản hồi ở Room");
        }
        foreach (var (name, obj) in deskOnly)
        {
            Assert.IsNotNull(obj, $"Không tìm thấy {name}");
            Assert.IsFalse(ViewGate.IsAllowed(ColliderOf(obj), CameraView.Room), $"{name} không phản hồi ở Room");
            Assert.IsTrue(ViewGate.IsAllowed(ColliderOf(obj), CameraView.Desk), $"{name} dùng được ở Desk");
        }
    }

    // ---------- TC-CAM-7 ----------

    [UnityTest]
    public IEnumerator TC_CAM_7_KhoaLaptopTheoTungKhoa()
    {
        // Cable Eater tắt Phone, nên không thể nhận cuộc gọi khi thẻ đang chạy (TC-CRD-20). Ca này kiểm quy tắc
        // "mỗi bên giữ khóa của mình": khóa của cuộc gọi (DeviceLayer) và khóa của Cable Eater (LaptopLock).
        var call = new object();
        DeviceLayer.Instance.LockLaptop(call);
        Cards.Place(Cards.Asset("CableEater"));
        Assert.IsTrue(LaptopController.IsLocked);

        yield return Seconds(5f);
        DeviceLayer.Instance.UnlockLaptop(call);                // cuộc gọi kết thúc
        Assert.IsTrue(LaptopController.IsLocked, "Khóa của Cable Eater vẫn còn");

        float duration = TestHooks.GetField<float>(Cards.Asset("CableEater"), "duration");
        int frames = 0;
        while (LaptopController.IsLocked && frames < (duration + 2f) * Fps) { yield return null; frames++; }
        Assert.IsFalse(LaptopController.IsLocked, "Mở khóa khi Cable Eater hết hạn");
        Assert.That(5f + frames / (float)Fps, Is.EqualTo(duration).Within(0.1f), $"Hết hạn sau {duration} s");
        Assert.IsFalse(LaptopLock.IsLocked, "Cable Eater đã trả khóa của nó");
    }

    // ---------- TC-CAM-8 ----------

    [UnityTest]
    public IEnumerator TC_CAM_8a_KhoaLaptop_CuocKhongDao()
    {
        var mining = TestHooks.Find<MiningApp>();
        yield return OpenLaptopApp(mining);
        var pickaxe = TestHooks.GetField<PickaxeView>(mining, "pickaxe");
        int hits = 0;
        pickaxe.Hit += () => hits++;

        var call = new object();
        DeviceLayer.Instance.LockLaptop(call);
        TestHooks.SetField(pickaxe, "_lastHitTime", -100f);
        TestHooks.Call(pickaxe, "TryHit");
        Assert.AreEqual(0, hits, "Khóa: nhát cuốc không có tác dụng");

        DeviceLayer.Instance.UnlockLaptop(call);
        TestHooks.Call(pickaxe, "TryHit");
        Assert.AreEqual(1, hits, "Mở khóa: đào lại được");
    }

    [UnityTest]
    public IEnumerator TC_CAM_8b_KhoaLaptop_BiDungRoiChayTiep_KetQuaNhuKhongNgat()
    {
        BilliardsDebug.PresetId = "straight_pot_01";
        BilliardsDebug.Mirror = BilliardsDebug.MirrorMode.None;
        var app = TestHooks.Find<BilliardsApp>();
        yield return OpenLaptopApp(app);
        Assert.AreEqual("straight_pot_01", app.Preset != null ? app.Preset.id : null);
        TestHooks.SetMoney(1000);
        app.PlayRound(0);
        yield return null;
        Assert.AreEqual("Aiming", app.StateName);

        // Kết quả chuẩn: cùng cú đánh trên bản sao của ván, mô phỏng một mạch không ngắt.
        var shot = app.Preset.solution[0];
        var reference = app.Round.Clone();
        reference.Fire(shot);
        while (reference.Sim.Step()) { }
        reference.Resolve();

        app.Shoot(shot);
        yield return Frames(10);
        Assert.AreEqual("Rolling", app.StateName);
        var call = new object();
        DeviceLayer.Instance.LockLaptop(call);
        var frozen = Snapshot(app.Round.Sim);
        int steps = app.Round.Sim.StepsThisShot;
        yield return Seconds(1f);
        Assert.AreEqual("Rolling", app.StateName, "Bi đứng yên giữa chừng, chưa phân xử");
        Assert.AreEqual(steps, app.Round.Sim.StepsThisShot, "Mô phỏng dừng khi Laptop bị khóa");
        CollectionAssert.AreEqual(frozen, Snapshot(app.Round.Sim), "Vị trí bi không đổi");

        DeviceLayer.Instance.UnlockLaptop(call);
        for (int i = 0; i < 60 * Fps && app.StateName == "Rolling"; i++) yield return null;

        CollectionAssert.AreEqual(Snapshot(reference.Sim), Snapshot(app.Round.Sim), "Kết quả như cú đánh không bị ngắt");
        Assert.AreEqual(reference.IsOver, app.Round.IsOver);
    }

    private static List<string> Snapshot(BilliardsSim sim)
    {
        var list = new List<string>();
        for (int i = 0; i < sim.BallCount; i++)
        {
            var p = sim.Position(i);
            list.Add($"{sim.IdOf(i)}:{sim.IsPotted(i)}:{p.x:R}:{p.y:R}");
        }
        return list;
    }
}
