using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum HistorySort { BestScore, LongestSurvival, MostMoney, FastestEnd }

public class RunHistoryPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private RunHistoryEntryView entryPrefab;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button sortButton;
    [SerializeField] private TMP_Text sortLabel;
    [SerializeField] private GameObject emptyLabel;

    private HistorySort _sort = HistorySort.BestScore;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (sortButton != null) sortButton.onClick.AddListener(CycleSort);
    }

    private void Open()
    {
        Refresh();
        if (panel != null) panel.SetActive(true);
    }

    private void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void CycleSort()
    {
        _sort = (HistorySort)(((int)_sort + 1) % 4);
        Refresh();
    }

    private void Refresh()
    {
        foreach (Transform child in entryContainer)
            Destroy(child.gameObject);

        if (sortLabel != null) sortLabel.text = "SORT: " + SortName(_sort);

        var stats = RunStatsService.Instance;
        var records = stats != null ? Sorted(stats.LoadHistory()) : new List<RunRecord>();

        if (emptyLabel != null) emptyLabel.SetActive(records.Count == 0);

        int currentRun = stats != null && stats.HasLastRecord ? stats.LastRecord.runNumber : -1;

        for (int i = 0; i < records.Count; i++)
        {
            var entry = Instantiate(entryPrefab, entryContainer);
            entry.Setup(records[i], i % 2 == 1, currentRun > 0 && records[i].runNumber == currentRun);
        }
    }

    private List<RunRecord> Sorted(RunRecord[] source)
    {
        switch (_sort)
        {
            case HistorySort.LongestSurvival:
                return source.OrderByDescending(r => r.bestTime).ThenByDescending(RunRating.Score).ToList();
            case HistorySort.MostMoney:
                return source.OrderByDescending(r => r.totalMoneyEarned).ThenByDescending(RunRating.Score).ToList();
            case HistorySort.FastestEnd:
                return source.OrderBy(r => r.bestTime).ThenByDescending(RunRating.Score).ToList();
            default:
                return source.OrderByDescending(RunRating.Score).ThenByDescending(r => r.runNumber).ToList();
        }
    }

    private static string SortName(HistorySort sort)
    {
        switch (sort)
        {
            case HistorySort.LongestSurvival: return "LONGEST RUN";
            case HistorySort.MostMoney: return "MOST MONEY";
            case HistorySort.FastestEnd: return "FASTEST END";
            default: return "BEST SCORE";
        }
    }
}
