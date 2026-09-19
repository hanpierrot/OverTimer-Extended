using TMPro;
using UnityEngine;

public class EndGameSummaryUI : MonoBehaviour
{
    public static EndGameSummaryUI Instance { get; private set; }
    
    [SerializeField] private TMP_Text bestTimeText;
    [SerializeField] private TMP_Text moneyEarnedText;
    [SerializeField] private TMP_Text cardsOpenedText;
    [SerializeField] private TMP_Text newCardsText;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    public void Show(RunRecord record)
    {
        if (bestTimeText != null) bestTimeText.text = $"Best Time: {record.bestTime:0}s";
        if (moneyEarnedText != null) moneyEarnedText.text = $"Money Earned: ${record.totalMoneyEarned}";
        if (cardsOpenedText != null) cardsOpenedText.text = $"Cards Opened: {record.cardsOpened}";
        if (newCardsText != null) newCardsText.text = $"New Cards: {record.newCardsToCollection}";
    }
}
