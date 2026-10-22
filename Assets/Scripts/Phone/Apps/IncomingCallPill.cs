using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Incoming call" pill at the top of the screen. It appears while the phone rings and a device hides the desk phone
/// (Laptop view), so the ring is never lost. Clicking it opens the phone-only view, where the call is answered.
/// In the phone-only view the call window itself shows the ring, so the pill hides.
/// </summary>
public class IncomingCallPill : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject visual;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(() =>
        {
            if (DeviceLayer.Instance != null) DeviceLayer.Instance.OpenPhone();
        });
    }

    private void Update()
    {
        var phone = PhoneController.Instance;
        var layer = DeviceLayer.Instance;
        bool show = phone != null && layer != null && phone.IsRinging && layer.Current == DeviceLayer.Mode.Laptop;
        PhoneUi.SetActive(visual, show);
        if (show) PhoneUi.SetText(label, "Incoming call: " + phone.RingingCall.shownAs);
    }
}
