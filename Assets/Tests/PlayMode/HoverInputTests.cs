using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

/// <summary>
/// TC-CAM-5 và TC-FBK-8 bằng một chuột ảo của Input System (InputSystem.AddDevice&lt;Mouse&gt;, InputState.Change):
/// InputManager đọc vị trí qua action "&lt;Mouse&gt;/position", nên chuột ảo đi đúng đường của chuột thật. Phần nhìn
/// (độ dày, màu viền sáng) và kéo Cat vẫn kiểm thủ công. HoverHighlighter đếm theo Time.unscaledDeltaTime (giờ thực),
/// nên các ca chờ theo Time.realtimeSinceStartup.
/// </summary>
public class HoverInputTests : GameSceneTest
{
    private Mouse _mouse;
    private readonly System.Collections.Generic.List<InputDevice> _disabled = new System.Collections.Generic.List<InputDevice>();

    [UnitySetUp]
    public IEnumerator AddMouse()
    {
        UseFixedFrameRate(50);
        TestHooks.SetClock(500f);
        TestHooks.Find<CatController>().gameObject.SetActive(false);   // Cat có thể ngồi chặn vật đang rê chuột
        // Action "<Mouse>/position" là kiểu Value: có nhiều chuột thì control có độ lớn hơn thắng, nên chuột thật (nếu có)
        // có thể lấn chuột ảo. Tắt tạm các chuột khác trong lúc test, bật lại ở TearDown.
        _disabled.Clear();
        foreach (var d in InputSystem.devices)
            if (d is Mouse && d.enabled) _disabled.Add(d);
        foreach (var d in _disabled) InputSystem.DisableDevice(d);
        _mouse = InputSystem.AddDevice<Mouse>("TestMouse");
        _mouse.MakeCurrent();
        Point(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        yield return null;
    }

    [TearDown]
    public void RemoveMouse()
    {
        if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
        _mouse = null;
        foreach (var d in _disabled) if (d.added) InputSystem.EnableDevice(d);
        _disabled.Clear();
    }

    private void Point(Vector2 screen, bool pressed = false, Vector2 scroll = default)
    {
        var state = new MouseState { position = screen, scroll = scroll };
        if (pressed) state = state.WithButton(MouseButton.Left);
        InputState.Change(_mouse, state);
    }

    private static IEnumerator GoTo(CameraView view)
    {
        if (CameraRig.Instance.CurrentView != view) Assert.IsTrue(CameraRig.Instance.GoTo(view), $"Không chuyển được sang góc {view}");
        while (CameraRig.Instance.IsMoving) yield return null;
        yield return null;
    }

    private static Vector2 ScreenOf(Component c)
    {
        var col = c.GetComponentInChildren<Collider2D>();
        Assert.IsNotNull(col, $"{c.name} không có collider");
        return Camera.main.WorldToScreenPoint(col.bounds.center);
    }

    // ---------- TC-CAM-5 ----------

    [UnityTest]
    public IEnumerator TC_CAM_5_KhongCuonKhongZoomKhongKeo()
    {
        var cam = Camera.main;
        foreach (var view in new[] { CameraView.Room, CameraView.Desk, CameraView.Door })
        {
            yield return GoTo(view);
            Vector3 pos = cam.transform.position;
            float size = cam.orthographicSize;

            // Lăn chuột lên và xuống.
            var mid = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            for (int i = 0; i < 10; i++) { Point(mid, scroll: new Vector2(0f, i < 5 ? 120f : -120f)); yield return null; }
            Point(mid);

            // Kéo nền từ giữa sang trái.
            Point(mid, pressed: true);
            yield return null;
            for (int i = 1; i <= 20; i++) { Point(mid + Vector2.left * (i * 15f), pressed: true); yield return null; }
            Point(mid + Vector2.left * 300f);
            yield return null;

            // Giữ chuột ở bốn mép màn hình.
            foreach (var edge in new[] { new Vector2(1f, mid.y), new Vector2(Screen.width - 2f, mid.y), new Vector2(mid.x, 1f), new Vector2(mid.x, Screen.height - 2f) })
            {
                Point(edge);
                for (int i = 0; i < 25; i++) yield return null;
            }

            Assert.AreEqual(view, CameraRig.Instance.CurrentView, $"{view}: vẫn ở góc nhìn cũ");
            Assert.IsFalse(CameraRig.Instance.IsMoving, $"{view}: camera không di chuyển");
            Assert.AreEqual(pos, cam.transform.position, $"{view}: vị trí camera không đổi");
            Assert.AreEqual(size, cam.orthographicSize, $"{view}: không zoom");
        }
    }

    // ---------- TC-FBK-8 ----------

    private static HoverHighlighter Glow => TestHooks.Find<HoverHighlighter>();
    private static Collider2D Shown => TestHooks.GetField<Collider2D>(Glow, "_shown");
    private static float Amount => TestHooks.GetField<float>(Glow, "_amount");

    /// <summary>Rê chuột tới điểm rồi chờ (giờ thực) đủ một lần dò, thời gian sáng lên và một chút dư.</summary>
    private IEnumerator HoverAt(Vector2 screen, float extra = 0.1f)
    {
        Point(screen);
        float wait = TestHooks.GetField<float>(Glow, "checkInterval") + Config.hoverGlowFadeOutSeconds + extra;
        float end = Time.realtimeSinceStartup + wait;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    private IEnumerator AssertGlows(Component target, string label)
    {
        yield return HoverAt(ScreenOf(target));
        var col = target.GetComponentInChildren<Collider2D>();
        Assert.AreEqual(col, Shown, $"{label}: phát sáng (đang chọn: {(Shown != null ? Shown.name : "không có")})");
        Assert.AreEqual(1f, Amount, 1e-4f, $"{label}: sáng hẳn sau {Config.hoverGlowFadeInSeconds} s");
    }

    private IEnumerator AssertNoGlow(Vector2 screen, string label)
    {
        yield return HoverAt(screen);
        Assert.AreEqual(0f, Amount, 1e-4f, $"{label}: không phát sáng (đang chọn: {(Shown != null ? Shown.name : "không có")})");
    }

    [UnityTest]
    public IEnumerator TC_FBK_8a_Room_EggTimerSang_PhoneKhong()
    {
        yield return GoTo(CameraView.Room);
        var egg = TestHooks.Find<EggTimerTask>();
        yield return AssertGlows(egg, "Room: Egg Timer");

        // Rời đi: tắt hẳn trong khoảng hoverGlowFadeOutSeconds (+ một lần dò).
        yield return AssertNoGlow(new Vector2(-200f, -200f), "Rời khỏi Egg Timer (con trỏ ra ngoài màn hình)");
        Assert.IsNull(Shown, "Không còn vật nào được chọn");

        // Ở Room, Phone nằm trên mặt bàn: chỗ đó trúng vùng bàn (CameraViewButton → Desk, DESIGN §4.2 "only the desk,
        // the door, the Picture and the Egg Timer glow"), nên vùng bàn sáng còn chính Phone không sáng.
        var phoneCol = PhoneController.Instance.GetComponentInChildren<Collider2D>();
        yield return HoverAt(ScreenOf(PhoneController.Instance));
        Assert.AreNotEqual(phoneCol, Shown, "Room: Phone không phát sáng");
        Assert.IsTrue(Shown == null || (Shown.TryGetComponent(out CameraViewButton zone) && zone.Target == CameraView.Desk),
            $"Room: chỗ Phone chỉ có thể sáng vùng bàn (đang chọn: {(Shown != null ? Shown.name : "không có")})");
    }

    [UnityTest]
    public IEnumerator TC_FBK_8b_Desk_VatBiChan_PanelMo_VungBack()
    {
        yield return GoTo(CameraView.Desk);
        var stack = Object.FindObjectsByType<TicketStack>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)[0];
        yield return AssertGlows(stack, "Desk: chồng vé tương tác được");

        // Vật bị chặn (như khi Cat ngồi lên): PointerReceiver không tương tác được.
        var receiver = stack.GetComponent<PointerReceiver>();
        receiver.SetInteractable(false);
        yield return AssertNoGlow(ScreenOf(stack), "Vật bị chặn");
        receiver.SetInteractable(true);
        yield return AssertGlows(stack, "Hết chặn");

        // Panel đang mở (Tax có InputBlocker).
        var itrs = ITRSService.Instance;
        TestHooks.SetField(itrs, "earnedSinceLastBill", 150);
        itrs.ForceBillNow();
        var tax = TestHooks.GetField<Tax>(DoorController.Instance, "taxPanel");
        tax.Show();
        yield return AssertNoGlow(ScreenOf(stack), "Khi có panel mở");
        tax.gameObject.SetActive(false);

        // Vùng Back (nút về góc Room).
        // Vùng Back nằm dưới các vật khác (vật thắng vùng khi chồng nhau) và mỗi góc nhìn có vùng Back riêng: quét màn hình
        // tìm một điểm mà cú nhấp ở góc Desk trúng một vùng Back (CameraViewButton → Room).
        Vector2? backPoint = null;
        for (int ix = 1; ix < 32 && backPoint == null; ix++)
        for (int iy = 1; iy < 18 && backPoint == null; iy++)
        {
            Vector2 s = new Vector2(Screen.width * ix / 32f, Screen.height * iy / 18f);
            var hit = (Collider2D)TestHooks.Call(InputManager.Instance, "PickTarget", (Vector2)Camera.main.ScreenToWorldPoint(s));
            if (hit != null && hit.TryGetComponent(out CameraViewButton zone) && zone.Target == CameraView.Room) backPoint = s;
        }
        Assert.IsTrue(backPoint.HasValue, "Ở Desk có điểm mà cú nhấp trúng vùng Back");
        yield return AssertNoGlow(backPoint.Value, "Vùng Back");
    }

    [UnityTest]
    public IEnumerator TC_FBK_8c_VatVuaBiCamDo_TatSang()
    {
        yield return GoTo(CameraView.Desk);
        // Mua thật (GiveItem chỉ đánh dấu đã giao, giá gốc 0 nên cầm đồ bị từ chối).
        var shop = ShopService.Instance;
        Assert.IsTrue(shop.AddToCart("power_bank"));
        TestHooks.SetMoney(shop.Quote().total);
        Assert.IsTrue(shop.TryBuy(), "Mua Power Bank");
        shop.Deliver(new[] { "power_bank" });
        yield return null;
        yield return null;
        var item = shop.BindingFor("power_bank");
        yield return AssertGlows(item, "Power Bank");
        Vector2 at = ScreenOf(item);

        Pawning.PawnOnto(item);
        Assert.IsFalse(item.gameObject.activeSelf, "Đã cầm đồ");
        yield return AssertNoGlow(at, "Vật vừa bị cầm đồ");
    }
}
