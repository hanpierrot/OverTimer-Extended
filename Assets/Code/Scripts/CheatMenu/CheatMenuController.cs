using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheatMenuController : MonoBehaviour
{
    [Header("Money")]
    [SerializeField] private TMP_InputField moneyAmountInput;
    [SerializeField] private Button giveMoneyButton;

    [Header("Time")]
    [SerializeField] private TMP_InputField timeAmountInput;
    [SerializeField] private Button giveTimeButton;

    [Header("Card")]
    [SerializeField] private CardPackRevealPanel revealPanel;
    [SerializeField] private Transform cardButtonContainer;
    [SerializeField] private Button cardButtonPrefab;
    
    private void Awake()
    {
        giveMoneyButton.onClick.AddListener(GiveMoney);
        giveTimeButton.onClick.AddListener(GiveTime);
    }

    private void Start()
    {
        BuildCardButtons();
    }

    private void GiveMoney()
    {
        if (int.TryParse(moneyAmountInput.text, out int amount) && amount > 0)
            MoneyService.Instance.Add(amount, "cheat");
    }

    private void GiveTime()
    {
        if (float.TryParse(timeAmountInput.text, out float seconds) && seconds > 0f)
            ClockService.Instance.AddSeconds(seconds, "cheat");
    }

    private void BuildCardButtons()
    {
        if (CardCollectionManager.Instance == null) return;
        
        foreach (var card in CardCollectionManager.Instance.AllCards)
        {
            Button btn = Instantiate(cardButtonPrefab, cardButtonContainer);

            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = card.cardName;

            btn.onClick.AddListener(() => GiveCard(card));
        }
    }

    private void GiveCard(CardSO card)
    {
        if (revealPanel != null) revealPanel.TryPlaceCard(card);
    }
}
