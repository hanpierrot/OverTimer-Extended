using TMPro;
using UnityEngine;

public class RunHistoryEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    public void Setup(RunRecord record)
    {
        if (label != null)
            label.text = $"{record.winCondition} — Best {record.bestTime:0}s — ${record.totalMoneyEarned} — {record.cardsOpened} opened — {record.newCardsToCollection} new";
    }
}
