using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The laptop hosts multiple apps (Mining, Blackjack) on one screen. Only ONE
/// app is active at a time - that mutual exclusion is physical rather than a
/// rule, which is the main win of putting blackjack on the laptop (BLACKJACK.md §1).
///
/// CRITICAL (DESIGN.md §5.5): the laptop screen must NOT go fullscreen. The
/// desk has to stay visible or the phone, cat and mail tray stop existing.
/// </summary>
public class LaptopController : MonoBehaviour
{
    [SerializeField] private List<LaptopApp> apps = new List<LaptopApp>();
    [SerializeField] private GameObject switchingOverlay;

    private LaptopApp _current;
    private bool _switching;
    private Coroutine _switchRoutine;

    // Set by a whole-laptop debuff, if one is ever added. Default design does
    // NOT use this - see BLACKJACK.md §2.1 (Dead Battery split into two apps).
    public bool Disabled { get; set; }

    /// <summary>True while a paused call holds the Laptop (DeviceLayer.LockLaptop). Apps stop acting: no Mining hits, no rolling Billiards shot.</summary>
    public static bool IsLocked => DeviceLayer.Instance != null && DeviceLayer.Instance.IsLaptopLocked;

    private void OnEnable()
    {
        if (DeviceLayer.Instance != null) DeviceLayer.Instance.NotifyLaptopOpened();
    }

    private void Start()
    {
        // Apps are siblings under the same Canvas, not children of this
        // GameObject - they don't inherit visibility from the Laptop panel
        // being open/closed. Start with none open; the home screen (Apps
        // grid) is what shows when the Laptop panel itself is opened.
        foreach (var app in apps)
        {
            if (app == null) continue; // an unfilled slot in the Apps list
            app.gameObject.SetActive(false);
        }
    }

    public void RequestOpen(LaptopApp app)
    {
        if (app == null || Disabled || _switching) return;
        if (app == _current && app.gameObject.activeSelf) return;
        _switchRoutine = StartCoroutine(SwitchRoutine(app));
    }

    public void RequestOpen(string appName)
    {
        // A hidden icon (no license, or a "No More X" card) can't be opened any other way either.
        if (ShopService.Instance != null && !ShopService.Instance.IconGate.IsVisible(appName)) return;
        var app = apps.Find(a => a != null && a.AppName == appName);
        if (app != null) RequestOpen(app);
    }

    /// <summary>Closes whichever app is open and drops back to the home screen (Apps grid). Free - unlike switching between apps, going home isn't a paid action.</summary>
    public void CloseCurrentApp()
    {
        if (_switching) { CancelSwitch(); return; }
        if (_current == null) return;

        _current.OnAppClosed();
        _current.gameObject.SetActive(false);
        _current = null;
        PublishAppState(false);
    }

    /// <summary>
    /// Closes the whole laptop, back to the desk. Apps are siblings of the
    /// Laptop panel, not children of it (see Start()), so hiding this
    /// GameObject alone would leave an open app floating on screen with no
    /// laptop shell around it - close the current app first so its own
    /// OnAppClosed cleanup (e.g. forfeiting unbanked mining/blackjack money)
    /// still runs.
    /// </summary>
    public void CloseLaptop()
    {
        CloseCurrentApp();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Abort a switch in progress. The previous app was already closed and
    /// hidden when the switch began, so the laptop falls back to the home
    /// screen. Without this, deactivating the laptop mid-switch killed the
    /// coroutine before it could clear _switching, and every later RequestOpen
    /// returned early.
    /// </summary>
    private void CancelSwitch()
    {
        if (_switchRoutine != null) StopCoroutine(_switchRoutine);
        _switchRoutine = null;
        _switching = false;
        _current = null;
        if (switchingOverlay != null) switchingOverlay.SetActive(false);
        PublishAppState(false);
    }

    private void OnDisable()
    {
        if (DeviceLayer.Instance != null) DeviceLayer.Instance.NotifyLaptopClosed();
        if (_switching) CancelSwitch();
    }

    /// <summary>
    /// App switching costs real time (config.laptopSwitchTime). Deliberate:
    /// hopping between mining and blackjack should have a price.
    /// </summary>
    private IEnumerator SwitchRoutine(LaptopApp next)
    {
        _switching = true;
        if (switchingOverlay != null) switchingOverlay.SetActive(true);

        if (_current != null)
        {
            _current.OnAppClosed();
            _current.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(GameManager.Instance.GameConfig.laptopSwitchTime);

        OpenImmediate(next);

        if (switchingOverlay != null) switchingOverlay.SetActive(false);
        _switching = false;
        _switchRoutine = null;
    }

    private void OpenImmediate(LaptopApp app)
    {
        _current = app;
        PublishAppState(true);                // the window slides to the centre and the docked phone hides
        app.gameObject.SetActive(true);
        app.OnAppOpened();
    }

    /// <summary>Tells the DeviceLayer whether an app window is up (it centres the window and hides the docked Phone).</summary>
    private void PublishAppState(bool open)
    {
        if (DeviceLayer.Instance != null) DeviceLayer.Instance.NotifyLaptopAppOpen(open);
    }
}
