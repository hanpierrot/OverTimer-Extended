using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    
    [Header("Cutscene")]
    [SerializeField] private Transform cutsceneButtonContainer;
    
    private void Awake()
    {
        giveMoneyButton.onClick.AddListener(GiveMoney);
        giveTimeButton.onClick.AddListener(GiveTime);
    }

    private void Start()
    {
        BuildCardButtons();
        BuildCutsceneButton();
    }

    private void GiveMoney()
    {
        if (int.TryParse(moneyAmountInput.text, out int amount))
            if(amount > 0f)
                MoneyService.Instance.Add(amount, "cheat");
            else
                MoneyService.Instance.TrySpend(amount, "cheat");
    }

    private void GiveTime()
    {
        if (float.TryParse(timeAmountInput.text, out float seconds))
            if(seconds > 0f)
                ClockService.Instance.AddSeconds(seconds, "cheat");
            else
                ClockService.Instance.Spend(seconds, "cheat");
    }

    private void BuildCardButtons()
    {
        if (CardCollectionManager.Instance == null) return;
        
        foreach (var card in CardCollectionManager.Instance.AllCards)
            AddButton(cardButtonContainer, card.cardName, () => GiveCard(card));
    }

    private void BuildCutsceneButton()
    {
        if (cutsceneButtonContainer == null) return;

        foreach (EndReason reason in Enum.GetValues(typeof(EndReason)))
        {
            EndReason r =  reason;
            AddButton(cutsceneButtonContainer, $"Preview {r}", () => PreviewEnding(r));
            AddButton(cutsceneButtonContainer, $"GameOver {r}", () => TriggerGameOver(r));
        }
        
        AddButton(cutsceneButtonContainer, "FC Intro", PreviewFinalCountdownIntro);
        AddButton(cutsceneButtonContainer, "Stop Cutscene", () => CutsceneController.Instance?.StopAll());
    }

    private void AddButton(Transform parent, string text, UnityAction onClick)
    {
        Button btn =  Instantiate(cardButtonPrefab, parent);
        
        var label = btn.GetComponentInChildren<TMP_Text>();
        if(label != null) label.text = text;
        
        btn.onClick.AddListener(onClick);
    }

    private void PreviewEnding(EndReason reason)
    {
        if (CutsceneController.Instance == null) return;
        
        CutsceneController.Instance.StopAll();
        CutsceneController.Instance.PlayEnding(reason, null);
        gameObject.SetActive(false);
    }

    private void PreviewFinalCountdownIntro()
    {
        if (CutsceneController.Instance == null) return;

        CutsceneController.Instance.StopAll();
        CutsceneController.Instance.PlayFinalCountdownIntro();
        gameObject.SetActive(false);
    }

    private void TriggerGameOver(EndReason reason)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        CutsceneController.Instance?.StopAll();
        GameManager.Instance.GameOver(reason);
        gameObject.SetActive(false);
    }

    private void GiveCard(CardSO card)
    {
        if (revealPanel != null) revealPanel.TryPlaceCard(card);
    }
}
