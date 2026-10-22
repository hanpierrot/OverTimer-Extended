using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The paper copy of the current ITRS bill (Assets/Prefab/Panels/Tax.prefab), opened from the desk notice (TaxNotice).
/// It is the SAME bill the Taxes phone app shows: paying here or there resolves both. Close only closes - there is no
/// Ignore (Q1): leaving the bill unpaid costs the shortfall penalty when the 30 s window runs out.
/// The panel starts inactive, so it never subscribes to events; it just reads ITRSService each frame while open.
/// </summary>
public class Tax : MonoBehaviour
{
    [Header("Notice of Assessment")]
    [SerializeField] private TextMeshProUGUI earnedValueText;
    [SerializeField] private TextMeshProUGUI rateValueText;
    [SerializeField] private TextMeshProUGUI minimumValueText;
    [SerializeField] private TextMeshProUGUI amountDueText;

    [Header("Buttons")]
    [SerializeField] private Button payButton;
    [SerializeField] private TextMeshProUGUI payButtonLabel;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        payButton.onClick.AddListener(HandlePay);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    private void Update() => Refresh();

    private void Refresh()
    {
        var itrs = ITRSService.Instance;
        if (itrs == null || !itrs.HasPendingAssessment)
        {
            gameObject.SetActive(false);                 // the bill was paid in the app (or penalised): the paper copy goes away
            return;
        }

        var a = itrs.CurrentAssessment;
        int due = itrs.CurrentAmountDue;
        PhoneUi.SetText(earnedValueText, $"${a.earnedSinceNotice}");
        PhoneUi.SetText(rateValueText, $"{Mathf.RoundToInt(a.rate * 100f)}%");
        PhoneUi.SetText(minimumValueText, $"${a.minimum}");
        PhoneUi.SetText(amountDueText, $"${due}");
        PhoneUi.SetText(payButtonLabel, $"PAY ${due}");
        payButton.interactable = MoneyService.Instance.Current >= due;
    }

    private void HandlePay()
    {
        if (ITRSService.Instance.Pay()) gameObject.SetActive(false);
    }
}
