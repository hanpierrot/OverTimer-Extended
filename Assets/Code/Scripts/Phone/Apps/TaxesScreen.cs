using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Taxes app window: the open ITRS bill, how long is left to pay it (running-clock seconds, so it stops during a
/// paused call), and a Pay button. The desk Tax panel is the paper copy of the same bill; paying either resolves both.
/// </summary>
public class TaxesScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text headline;
    [SerializeField] private TMP_Text body;
    [SerializeField] private TMP_Text timer;
    [SerializeField] private Button payButton;
    [SerializeField] private TMP_Text payLabel;

    private void Awake()
    {
        payButton.onClick.AddListener(() =>
        {
            if (ITRSService.Instance != null) ITRSService.Instance.Pay();
        });
    }

    private void Update()
    {
        var itrs = ITRSService.Instance;
        if (itrs == null || !itrs.HasPendingAssessment)
        {
            PhoneUi.SetText(headline, "NO BILL DUE");
            PhoneUi.SetText(body, "ITRS has nothing for you. Yet.");
            PhoneUi.SetText(timer, "");
            PhoneUi.SetActive(payButton.gameObject, false);
            return;
        }

        int due = itrs.CurrentAmountDue;
        bool canPay = MoneyService.Instance.Current >= due;
        PhoneUi.SetText(headline, $"BILL DUE: ${due}");
        PhoneUi.SetText(body, canPay ? "Pay before the time runs out, or ITRS takes it from your clock."
                                     : "Not enough money yet. Earn it before the time runs out.");
        PhoneUi.SetText(timer, $"{Mathf.CeilToInt(itrs.SecondsLeft)}s left");
        PhoneUi.SetActive(payButton.gameObject, true);
        PhoneUi.SetText(payLabel, $"PAY ${due}");
        payButton.interactable = canPay;
    }
}
