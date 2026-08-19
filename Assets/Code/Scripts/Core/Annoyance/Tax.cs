using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the Tax panel (Assets/Prefab/Panels/Tax.prefab) - the fake IRS
/// notice DESIGN.md's Mail Tray shows when a bill is due (DESIGN.md §5.6). TaxNotice
/// calls Show() directly once the player clicks the desk object (this panel starts
/// inactive in the scene, so an event it subscribes to in OnEnable would miss the
/// first notice - see TaxNotice). Pay/Ignore call straight back into ITRSService.
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
    [SerializeField] private Button ignoreButton;
    [SerializeField] private Button closeButton;

    private ITRSService.Assessment current;

    private void Awake()
    {
        payButton.onClick.AddListener(HandlePay);
        ignoreButton.onClick.AddListener(HandleIgnore);
        closeButton.onClick.AddListener(HandleClose);
    }

    public void Show(ITRSService.Assessment assessment)
    {
        current = assessment;

        earnedValueText.text = $"${assessment.earnedSinceNotice}";
        rateValueText.text = $"{Mathf.RoundToInt(assessment.rate * 100f)}%";
        minimumValueText.text = $"${assessment.minimum}";
        amountDueText.text = $"${assessment.amountDue}";
        payButtonLabel.text = $"PAY ${assessment.amountDue}";

        gameObject.SetActive(true);
    }

    private void Update()
    {
        payButton.interactable = MoneyService.Instance.Current >= current.amountDue;
    }

    private void HandlePay()
    {
        if (ITRSService.Instance.Pay()) gameObject.SetActive(false);
    }

    private void HandleIgnore()
    {
        ITRSService.Instance.Ignore();
        gameObject.SetActive(false);
    }

    // Closing without deciding must still resolve the bill - otherwise HasPendingAssessment
    // stays true forever and AnnoyanceManager stays busy, blocking every later annoyance.
    private void HandleClose() => HandleIgnore();
}
