using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunHistoryEntryView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image accent;
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text openedText;
    [SerializeField] private TMP_Text newText;
    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private Color rowColorA = new Color32(0x2A, 0x2A, 0x33, 0xFF);
    [SerializeField] private Color rowColorB = new Color32(0x24, 0x24, 0x2C, 0xFF);
    [SerializeField] private Color currentRowColor = new Color32(0x3A, 0x3A, 0x52, 0xFF);
    [SerializeField] private Color winColor = new Color32(0x4C, 0xAF, 0x72, 0xFF);
    [SerializeField] private Color lossColor = new Color32(0xD9, 0x53, 0x4F, 0xFF);
    [SerializeField] private Color textColor = new Color32(0xE6, 0xE6, 0xE6, 0xFF);
    [SerializeField] private Color dimColor = new Color32(0x8A, 0x8A, 0x94, 0xFF);

    public void Setup(RunRecord record, bool alternate, bool isCurrent)
    {
        bool parsed = RunRating.TryParseReason(record.winCondition, out EndReason reason);
        bool win = parsed && RunRating.IsWin(reason);
        Color tint = win ? winColor : lossColor;

        background.color = isCurrent ? currentRowColor : (alternate ? rowColorB : rowColorA);
        accent.color = tint;

        indexText.text = record.runNumber > 0 ? $"#{record.runNumber}" : "-";
        indexText.color = isCurrent ? textColor : dimColor;

        string title = parsed ? RunRating.Title(reason) : record.winCondition;
        resultText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(tint)}>{(win ? "WIN" : "LOSE")}</color>\n"
                          + $"<size=50%><color=#{ColorUtility.ToHtmlStringRGB(dimColor)}>{title}</color></size>";

        timeText.text = RunRating.FormatTime(record.bestTime);
        moneyText.text = "$" + record.totalMoneyEarned.ToString("N0", CultureInfo.InvariantCulture);
        openedText.text = record.cardsOpened.ToString();

        newText.text = record.newCardsToCollection > 0 ? "+" + record.newCardsToCollection : "0";
        newText.color = record.newCardsToCollection > 0 ? textColor : dimColor;

        scoreText.text = Mathf.RoundToInt(RunRating.Score(record)).ToString();
    }
}
