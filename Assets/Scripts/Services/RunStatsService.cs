using System.Collections.Generic;
using UnityEngine;

public class RunStatsService : MonoBehaviour
{
    public static RunStatsService Instance { get; private set; }
    
    public RunRecord LastRecord { get; private set; }
    public bool HasLastRecord { get; private set; }

    private const int HistorySize = 20;
    
    private float _bestTime;
    private int _lastMoney;
    private int _totalMoneyEarned;
    private int _cardsOpened;
    private int _newCardsToCollection;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _lastMoney = MoneyService.Instance.Current;

        ClockService.Instance.OnTimeChanged += HandleTimeChanged;
        MoneyService.Instance.OnMoneyChanged += HandleMoneyChanged;
        CardRevealSlot.CardRevealed += HandleCardRevealed;
        if (CardCollectionManager.Instance != null) CardCollectionManager.Instance.NewCardRegistered += HandleNewCard;
        GameManager.Instance.OnGameOver += HandleGameOver;
    }
    
    private void OnDestroy()
    {
        if (ClockService.Instance != null) ClockService.Instance.OnTimeChanged -= HandleTimeChanged;
        if (MoneyService.Instance != null) MoneyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
        CardRevealSlot.CardRevealed -= HandleCardRevealed;
        if (CardCollectionManager.Instance != null) CardCollectionManager.Instance.NewCardRegistered -= HandleNewCard;
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= HandleGameOver;
    }

    private void HandleTimeChanged(float seconds)
    {
        if (seconds > _bestTime) _bestTime = seconds;
    }

    private void HandleMoneyChanged(int current, string reason)
    {
        int delta = current - _lastMoney;
        _lastMoney = current;
        if (delta > 0 && reason != MoneyService.StakeRefundReason) _totalMoneyEarned += delta;
    }
    
    private void HandleCardRevealed() => _cardsOpened++;

    private void HandleNewCard(CardSO card) => _newCardsToCollection++;

    private void HandleGameOver(EndReason reason)
    {
        var record = new RunRecord
        {
            bestTime = _bestTime,
            winCondition = reason.ToString(),
            totalMoneyEarned = _totalMoneyEarned,
            cardsOpened = _cardsOpened,
            newCardsToCollection = _newCardsToCollection,
            runNumber = PlayerPrefs.GetInt("RunHistory_Total", 0) + 1,
        };

        PlayerPrefs.SetInt("RunHistory_Total", record.runNumber);
        LastRecord = record;
        HasLastRecord = true;
        SaveRecord(record);
    }
    
    private void SaveRecord(RunRecord record)
    {
        int nextSlot = PlayerPrefs.GetInt("RunHistory_NextSlot", 0);
        string json = JsonUtility.ToJson(record);

        PlayerPrefs.SetString($"RunHistory_{nextSlot}", json);
        PlayerPrefs.SetInt("RunHistory_NextSlot", (nextSlot + 1) % HistorySize);
        PlayerPrefs.Save();
    }

    /// <summary>Dev: store a fabricated run in the history (cheat menu).</summary>
    public void DebugAddRecord(RunRecord record)
    {
        if (!Debug.isDebugBuild) return;

        record.runNumber = PlayerPrefs.GetInt("RunHistory_Total", 0) + 1;
        PlayerPrefs.SetInt("RunHistory_Total", record.runNumber);
        SaveRecord(record);
    }

    /// <summary>Dev: erase the whole run history (cheat menu).</summary>
    public void DebugClearHistory()
    {
        if (!Debug.isDebugBuild) return;

        for (int i = 0; i < HistorySize; i++) PlayerPrefs.DeleteKey($"RunHistory_{i}");
        PlayerPrefs.DeleteKey("RunHistory_NextSlot");
        PlayerPrefs.DeleteKey("RunHistory_Total");
        PlayerPrefs.Save();
    }

    public RunRecord[] LoadHistory()
    {
        var list = new List<RunRecord>();
        for (int i = 0; i < HistorySize; i++)
        {
            string json = PlayerPrefs.GetString($"RunHistory_{i}", null);
            if (!string.IsNullOrEmpty(json))
                list.Add(JsonUtility.FromJson<RunRecord>(json));
        }

        return list.ToArray();
    }
}
