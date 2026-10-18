using TMPro;
using UnityEngine;

/// <summary>
/// Pop-up banners (BannerQueue.Visible: at most 3, a few real seconds each) stacked at the top-right of the screen. They
/// show whether or not the phone is on screen, but only while it is usable: powering it off clears the queue. Banners
/// are cosmetic: every message is also in the Inbox. A phone-top variant waits for the final phone art.
/// </summary>
public class BannerStack : MonoBehaviour
{
    [SerializeField] private GameObject[] rows;
    [SerializeField] private TMP_Text[] titles;
    [SerializeField] private TMP_Text[] bodies;

    private void Update()
    {
        var phone = PhoneController.Instance;
        int count = phone != null && phone.Banners != null && phone.Power.IsUsable ? phone.Banners.Visible.Count : 0;

        for (int i = 0; i < rows.Length; i++)
        {
            bool show = i < count;
            PhoneUi.SetActive(rows[i], show);
            if (!show) continue;
            var message = phone.Banners.Visible[i].msg;
            PhoneUi.SetText(titles[i], message.title);
            PhoneUi.SetText(bodies[i], message.body);
        }
    }
}
