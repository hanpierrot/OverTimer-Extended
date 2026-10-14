using System;
using TMPro;
using UnityEngine;

public class EndGameSummaryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text bestTimeText;
    [SerializeField] private TMP_Text moneyEarnedText;
    [SerializeField] private TMP_Text cardsOpenedText;
    [SerializeField] private TMP_Text newCardsText;

    private void OnEnable()
    {
        var stats = RunStatsService.Instance;
        if (stats != null && stats.HasLastRecord) Show(stats.LastRecord);
    }

    public void Show(RunRecord record)
    {
        if (bestTimeText != null) bestTimeText.text = $"Best Time: {record.bestTime:0}s";
        if (moneyEarnedText != null) moneyEarnedText.text = $"Money Earned: ${record.totalMoneyEarned}";
        if (cardsOpenedText != null) cardsOpenedText.text = $"Cards Opened: {record.cardsOpened}";
        if (newCardsText != null) newCardsText.text = $"New Cards: {record.newCardsToCollection}";
    }
}
