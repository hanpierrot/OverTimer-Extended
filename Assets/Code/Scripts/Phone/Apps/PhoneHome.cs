using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The phone's screen and the window beside it (phone-only view). Reads PhoneController:
///   Off      - black screen, power button still works (unless Cable Eater holds it off)
///   Booting  - black screen with a progress bar
///   On       - home screen with the app icons; the app window opens to the right of the phone
/// The window shows only in the DeviceLayer's Phone view. Apps: "call" (CallScreen), "inbox" (InboxScreen),
/// "taxes" (TaxesScreen), "shop" (ShopScreen). An incoming or active call always takes the window.
/// </summary>
public class PhoneHome : MonoBehaviour
{
    [Header("Screen")]
    [SerializeField] private GameObject offScreen;
    [SerializeField] private GameObject bootRoot;
    [SerializeField] private Image bootFill;
    [SerializeField] private GameObject homeRoot;
    [SerializeField] private Button powerButton;
    [SerializeField] private PhoneIconView[] icons;

    [Header("App windows (one frame per app; only one shows at a time)")]
    [SerializeField] private GameObject callWindow;
    [SerializeField] private GameObject inboxWindow;
    [SerializeField] private GameObject taxesWindow;
    [SerializeField] private GameObject shopWindow;

    private string _app = "call";

    private void Awake()
    {
        if (powerButton != null) powerButton.onClick.AddListener(() =>
        {
            if (PhoneController.Instance != null) PhoneController.Instance.PressPower();
        });
        foreach (var icon in icons)
        {
            if (icon == null || icon.Button == null) continue;
            string id = icon.AppId;
            icon.Button.onClick.AddListener(() => OpenApp(id));
        }
    }

    private void Update()
    {
        var phone = PhoneController.Instance;
        if (phone == null || phone.Power == null) return;

        var power = phone.Power;
        PhoneUi.SetActive(offScreen, !power.IsUsable);
        PhoneUi.SetActive(bootRoot, power.Current == PhonePower.State.Booting);
        PhoneUi.SetActive(homeRoot, power.IsUsable);
        if (bootFill != null) bootFill.fillAmount = power.BootProgress;
        if (powerButton != null) powerButton.interactable = !power.IsForcedOff;

        var itrs = ITRSService.Instance;
        var gate = ShopService.Instance != null ? ShopService.Instance.IconGate : null;
        foreach (var icon in icons)
        {
            if (icon == null) continue;
            if (gate != null) PhoneUi.SetActive(icon.gameObject, gate.IsVisible(icon.AppId));   // "Close The Shop" hides the Shop icon
            if (icon.AppId == "inbox") icon.SetBadge(phone.Inbox.Unread);
            else if (icon.AppId == "taxes") icon.SetBadge(itrs != null && itrs.HasPendingAssessment ? 1 : 0);
        }

        // The window shows in the phone-only view while the phone is on. An incoming call always takes the window.
        bool phoneView = DeviceLayer.Instance != null && DeviceLayer.Instance.Current == DeviceLayer.Mode.Phone;
        bool show = phoneView && power.IsUsable;
        string app = (phone.IsRinging || phone.ActiveCall != null) ? "call" : _app;
        if (gate != null && !gate.IsVisible(app)) app = "call";           // the open app's icon just got hidden
        PhoneUi.SetActive(callWindow, show && app == "call");
        PhoneUi.SetActive(inboxWindow, show && app == "inbox");
        PhoneUi.SetActive(taxesWindow, show && app == "taxes");
        PhoneUi.SetActive(shopWindow, show && app == "shop");
    }

    public void OpenApp(string id)
    {
        var phone = PhoneController.Instance;
        // Mid-call the Call window stays up; the other apps wait until it ends.
        if (phone != null && (phone.IsRinging || phone.ActiveCall != null)) return;
        var gate = ShopService.Instance != null ? ShopService.Instance.IconGate : null;
        if (gate != null && !gate.IsVisible(id)) return;
        _app = id;
        if (id == "shop") TutorialService.TryShow("shop_first_open");
    }
}
