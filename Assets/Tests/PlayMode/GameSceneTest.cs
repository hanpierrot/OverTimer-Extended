using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Lớp nền cho test Play Mode cần scene game thật. Mỗi test:
///  - sao lưu PlayerPrefs và khôi phục sau khi chạy (GameOver ghi lịch sử ván vào máy);
///  - đánh dấu đã xem hướng dẫn, vì TutorialPanel dừng game ở lần chơi đầu;
///  - nạp lại SampleScene để bắt đầu từ ván mới;
///  - đặt seed cố định cho RngService.
/// Test cần thời gian xác định thì gọi UseFixedFrameRate: Time.deltaTime khi đó cố định,
/// không phụ thuộc tốc độ máy (giả lập delta time, mục 1.7).
/// </summary>
public abstract class GameSceneTest
{
    protected const string GameScene = "SampleScene";
    protected const int TestSeed = 12345;

    /// <summary>
    /// Package của Editor (không phải game) có thể ghi lỗi giữa chừng một test, ví dụ Unity AI Assistant báo
    /// không kết nối được máy chủ khi mạng chập chờn. Lỗi có stack trace đi qua các package này được bỏ qua;
    /// mọi lỗi khác vẫn làm test thất bại.
    /// </summary>
    private static readonly string[] IgnoredErrorSources = { "com.unity.ai.assistant" };

    private PlayerPrefsSnapshot _prefs;
    private bool _runInBackground;
    private readonly List<string> _unexpectedErrors = new();

    protected GameConfig Config => GameManager.Instance.GameConfig;

    [UnitySetUp]
    public IEnumerator LoadFreshGame()
    {
        _unexpectedErrors.Clear();
        LogAssert.ignoreFailingMessages = true;   // tự lọc lỗi trong HandleLog thay cho bộ kiểm tra mặc định
        Application.logMessageReceivedThreaded += HandleLog;

        _prefs = PlayerPrefsSnapshot.Take();
        PlayerPrefs.SetInt("TutorialSeen", 1);
        BeforeSceneLoad();

        // Unity mặc định dừng Play Mode khi cửa sổ Editor mất focus; test phải chạy cả khi bạn chuyển cửa sổ.
        _runInBackground = Application.runInBackground;
        Application.runInBackground = true;

        yield return SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
        yield return null;   // chờ mọi Start() chạy xong

        TestHooks.SeedRng(TestSeed);
    }

    [UnityTearDown]
    public IEnumerator RestoreState()
    {
        Time.captureFramerate = 0;
        // GameOver đóng băng game (timeScale 0, AudioListener.pause). Trả về bình thường sau mỗi test: bộ gửi kết quả của
        // Test Runner trên Player (RemoteTestResultSender) đợi bằng WaitForSeconds theo thời gian game, nên nếu test cuối
        // để timeScale = 0 thì kết quả không bao giờ được gửi về Editor ("Queue size" đứng yên, Wait For Player Run).
        Time.timeScale = 1f;
        AudioListener.pause = false;
        BilliardsDebug.PresetId = null;                        // công cụ debug Billiards là static, sống qua lần nạp scene
        BilliardsDebug.Mirror = BilliardsDebug.MirrorMode.Random;
        Application.runInBackground = _runInBackground;
        _prefs?.Restore();
        yield return null;

        Application.logMessageReceivedThreaded -= HandleLog;
        lock (_unexpectedErrors)
            Assert.IsEmpty(_unexpectedErrors, "Có lỗi được ghi ra Console trong lúc chạy test:\n" + string.Join("\n---\n", _unexpectedErrors));
    }

    private void HandleLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Log || type == LogType.Warning) return;
        foreach (var source in IgnoredErrorSources)
            if (stackTrace != null && stackTrace.Contains(source)) return;

        lock (_unexpectedErrors) _unexpectedErrors.Add($"[{type}] {message}\n{stackTrace}");
    }

    /// <summary>Chạy sau khi sao lưu PlayerPrefs, trước khi nạp scene: chỗ để ca kiểm thử đặt dữ liệu lưu ban đầu.</summary>
    protected virtual void BeforeSceneLoad() { }

    /// <summary>Nạp lại scene như khi người chơi mở lại game (PlayerPrefs giữ nguyên).</summary>
    protected static IEnumerator ReloadScene()
    {
        yield return SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
        yield return null;
    }

    /// <summary>Mỗi khung hình trôi đúng 1 / fps giây (game time).</summary>
    protected static void UseFixedFrameRate(int fps) => Time.captureFramerate = fps;

    protected static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    protected static LaptopController Laptop => TestHooks.Find<LaptopController>();

    /// <summary>
    /// Đưa một món của Shop lên bàn như thể đã mua và nhận hàng. Đầu mỗi ván Power Bank, Card Box và Lucky Nine stack
    /// bị ẩn (startsOwned = 0, ShopItemBinding tắt chúng); vật bị tắt không nhận nhấp chuột nên test cần gọi hàm này trước.
    /// Dùng id trong ItemCatalog: "power_bank", "card_box", "lucky_stack".
    /// </summary>
    protected static void GiveItem(string id)
    {
        var shop = ShopService.Instance;
        Assert.IsNotNull(shop, "Thiếu ShopService trong scene");
        Assert.IsTrue(shop.Ownership.Has(id), $"Không có món '{id}' trong ItemCatalog");
        shop.Ownership.MarkDelivered(id);
        var binding = shop.BindingFor(id);
        Assert.IsNotNull(binding, $"Không có ShopItemBinding cho '{id}' trong scene");
        binding.gameObject.SetActive(true);   // OnEnable của vật gắn lại sự kiện nhấp chuột
    }

    /// <summary>
    /// Mở một ứng dụng trên Laptop qua đúng đường của game (LaptopController.RequestOpen, có thời gian chuyển ứng dụng).
    /// Laptop mặc định bị khóa cho tới khi giải Wire Task; ca nào không kiểm thử Wire Task thì mở khóa thẳng.
    /// </summary>
    protected static IEnumerator OpenLaptopApp(LaptopApp app)
    {
        var laptop = Laptop;
        laptop.Disabled = false;
        laptop.gameObject.SetActive(true);
        yield return null;

        laptop.RequestOpen(app);
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!app.gameObject.activeInHierarchy && Time.realtimeSinceStartup < deadline) yield return null;
        NUnit.Framework.Assert.IsTrue(app.gameObject.activeInHierarchy, $"Không mở được ứng dụng {app.AppName}");
        yield return null;   // để Start() của ứng dụng chạy
    }
}
