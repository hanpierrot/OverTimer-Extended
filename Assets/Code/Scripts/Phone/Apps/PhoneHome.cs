using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The phone's screen and the window beside it (phone-only view). Reads PhoneController:
///   Off      - black screen, power button still works (unless Cable Eater holds it off)
///   Booting  - black screen with a progress bar
///   On       - home screen with the app icons; the app window opens to the right of the phone
/// The window shows only in the DeviceLayer's Phone view. Apps: "call" (CallScreen), "inbox" (InboxScreen),
/// "taxes" (TaxesScreen). An incoming or active call always takes the window.
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

    [Header("Window")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private TMP_Text windowTitle;
    [SerializeField] private GameObject callScreenRoot;
    [SerializeField] private GameObject inboxScreenRoot;
    [SerializeField] private GameObject taxesScreenRoot;

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
        foreach (var icon in icons)
        {
            if (icon == null) continue;
            if (icon.AppId == "inbox") icon.SetBadge(phone.Inbox.Unread);
            else if (icon.AppId == "taxes") icon.SetBadge(itrs != null && itrs.HasPendingAssessment ? 1 : 0);
        }

        // The window shows in the phone-only view while the phone is on. An incoming call always takes the window.
        bool phoneView = DeviceLayer.Instance != null && DeviceLayer.Instance.Current == DeviceLayer.Mode.Phone;
        bool show = phoneView && power.IsUsable;
        PhoneUi.SetActive(windowRoot, show);
        if (!show) return;

        ShowApp((phone.IsRinging || phone.ActiveCall != null) ? "call" : _app);
    }

    public void OpenApp(string id)
    {
        var phone = PhoneController.Instance;
        // Mid-call the Call window stays up; the other apps wait until it ends.
        if (phone != null && (phone.IsRinging || phone.ActiveCall != null)) return;
        _app = id;
    }

    private void ShowApp(string app)
    {
        PhoneUi.SetActive(callScreenRoot, app == "call");
        PhoneUi.SetActive(inboxScreenRoot, app == "inbox");
        PhoneUi.SetActive(taxesScreenRoot, app == "taxes");
        PhoneUi.SetText(windowTitle, app == "inbox" ? "INBOX" : app == "taxes" ? "TAXES" : "CALL");
    }
}
