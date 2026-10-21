using System;
using System.Collections.Generic;
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
    [Tooltip("Row that holds the tab buttons. When empty, the tabs are the first buttons of the cheat grid")]
    [SerializeField] private Transform tabButtonContainer;

    // Every cheat belongs to one tab. All buttons are children of the same grid; a tab only switches the others off,
    // and the grid ignores inactive children, so no prefab change is needed.
    private const string TabEndings = "Endings";
    private const string TabGames = "Games";
    private const string TabEconomy = "Economy";
    private const string TabAnnoyance = "Annoyance";
    private const string TabDev = "Dev";
    private static readonly string[] TabOrder = { TabEndings, TabGames, TabEconomy, TabAnnoyance, TabDev };

    private readonly List<(string tab, GameObject go)> _entries = new();
    private readonly List<(string tab, TMP_Text label, Image image)> _tabButtons = new();

    // Visual style only (the prefab holds the button's base colour).
    private static readonly Color TabColor = new Color(0.27f, 0.31f, 0.38f, 1f);
    private static readonly Color TabSelectedColor = new Color(0.25f, 0.65f, 0.84f, 1f);
    private string _currentTab = TabOrder[0];
    private CatController _cat;
    private bool _clockFrozen;
    private int _hintIndex = -1;

    private void Awake()
    {
        giveMoneyButton.onClick.AddListener(GiveMoney);
        giveTimeButton.onClick.AddListener(GiveTime);
    }

    private void Start()
    {
        BuildCardButtons();
        BuildCutsceneButton();
        BuildEndingCheats();
        BuildEconomyCheats();
        BuildAnnoyanceCheats();
        BuildHintAndHistoryCheats();
        BuildMiscCheats();
        BuildTabStrip();
    }

    // ---------- Tabs ----------

    private Button AddCheat(string tab, string text, UnityAction onClick)
    {
        Button btn = AddButton(cutsceneButtonContainer, text, onClick);
        _entries.Add((tab, btn.gameObject));
        btn.gameObject.SetActive(tab == _currentTab);
        return btn;
    }

    private void AddCheatToggle(string tab, string text, Func<bool> get, Action<bool> set)
    {
        TMP_Text label = null;

        void Refresh()
        {
            if (label != null) label.text = $"{text}: {(get() ? "ON" : "OFF")}";
        }

        Button btn = AddCheat(tab, text, () => { set(!get()); Refresh(); });
        label = btn.GetComponentInChildren<TMP_Text>();
        Refresh();
    }

    private void BuildTabStrip()
    {
        if (cutsceneButtonContainer == null) return;

        Transform parent = tabButtonContainer != null ? tabButtonContainer : cutsceneButtonContainer;
        for (int i = 0; i < TabOrder.Length; i++)
        {
            string tab = TabOrder[i];
            Button b = AddButton(parent, tab, () => SelectTab(tab));
            if (parent == cutsceneButtonContainer) b.transform.SetSiblingIndex(i);   // no tab row: tabs lead the grid
            _tabButtons.Add((tab, b.GetComponentInChildren<TMP_Text>(), b.GetComponent<Image>()));
        }
        SelectTab(_currentTab);
    }

    private void SelectTab(string tab)
    {
        _currentTab = tab;
        foreach (var e in _entries) e.go.SetActive(e.tab == tab);
        foreach (var t in _tabButtons)
        {
            if (t.label != null) t.label.text = t.tab;
            if (t.image != null) t.image.color = t.tab == tab ? TabSelectedColor : TabColor;
        }
    }

    // ---------- Endings and the games behind them ----------

    private void BuildCutsceneButton()
    {
        if (cutsceneButtonContainer == null) return;

        foreach (EndReason reason in Enum.GetValues(typeof(EndReason)))
        {
            EndReason r =  reason;
            AddCheat(TabEndings, $"Preview {r}", () => PreviewEnding(r));
            AddCheat(TabEndings, $"GameOver {r}", () => TriggerGameOver(r));
        }

        AddCheat(TabDev, "Reset tutorials", () => { if (TutorialService.Instance != null) TutorialService.Instance.ResetAll(); });
        AddCheat(TabDev, "FC Intro", PreviewFinalCountdownIntro);
        AddCheat(TabDev, "Stop Cutscene", () => CutsceneController.Instance?.StopAll());
    }

    private void BuildEndingCheats()
    {
        if (cutsceneButtonContainer == null) return;

        AddCheat(TabEndings, "Next hand: Charlie", () => { CheatFlags.ForceCharlie = true; Debug.Log("[Cheat] next Blackjack hand = Charlie"); });
        AddCheat(TabEndings, "Next Lucky Nine: Jackpot", () => { CheatFlags.NextLuckyNine = CheatFlags.NextTicket.Jackpot; Debug.Log("[Cheat] next Lucky Nine = Jackpot"); });
        AddCheat(TabEndings, "Next Lucky Nine: Cursed", () => { CheatFlags.NextLuckyNine = CheatFlags.NextTicket.Cursed; Debug.Log("[Cheat] next Lucky Nine = Cursed"); });

        foreach (int n in new[] { 3, 5, 6, 8, 9 })
        {
            int total = n;
            AddCheat(TabGames, total == 9 ? "Open ticket: all windows" : $"Open ticket: {total} windows", () => RevealViewedTicket(total));
        }
        AddCheatToggle(TabGames, "Free tickets", () => CheatFlags.FreeTickets, v => CheatFlags.FreeTickets = v);
        AddCheatToggle(TabGames, "Every vein = max", () => CheatFlags.ForceFullVein, v => CheatFlags.ForceFullVein = v);
        for (int s = 0; s <= 2; s++)
        {
            int streak = s;
            AddCheat(TabGames, $"Mother Lode streak = {streak}", () => CheatFlags.PendingMiningStreak = streak);
        }
        AddCheat(TabGames, "Next vein: Slip", () => MiningApp.QueueNextVeinSlip());
        AddCheat(TabGames, "Bank vein now", MiningApp.DebugFinishVein);
    }

    private void RevealViewedTicket(int total)
    {
        if (ScratchTicket.Viewed != null) ScratchTicket.Viewed.RevealWindows(total);
    }

    // ---------- Clock and money ----------

    private void BuildEconomyCheats()
    {
        if (cutsceneButtonContainer == null) return;

        var cfg = GameManager.Instance.GameConfig;
        AddCheat(TabEconomy, "Clock: near loss", () => SetClock(cfg.loseClock + 5f));
        AddCheat(TabEconomy, "Clock: near win", () => SetClock(cfg.winClock - 10f));
        AddCheat(TabEconomy, "Clock +60 s", () => ClockService.Instance.AddSeconds(60f, "cheat"));
        AddCheat(TabEconomy, "Clock -60 s", () => ClockService.Instance.Spend(60f, "cheat"));
        AddCheatToggle(TabEconomy, "Freeze clock", () => _clockFrozen, v =>
        {
            _clockFrozen = v;
            if (v) ClockService.Instance.Pause(this); else ClockService.Instance.Resume(this);
        });
        AddCheat(TabEconomy, "Money +$100", () => MoneyService.Instance.Add(100, "cheat"));
        AddCheat(TabEconomy, "Money +$1000", () => MoneyService.Instance.Add(1000, "cheat"));
        AddCheat(TabEconomy, "Money = 0", () => MoneyService.Instance.TrySpend(MoneyService.Instance.Current, "cheat"));
    }

    private static void SetClock(float seconds)
    {
        var clock = ClockService.Instance;
        float delta = seconds - clock.CurrentSeconds;
        if (delta > 0f) clock.AddSeconds(delta, "cheat");
        else clock.Spend(-delta, "cheat");
    }

    private void GiveMoney()
    {
        if (!int.TryParse(moneyAmountInput.text, out int amount) || amount == 0) return;
        if (amount > 0) MoneyService.Instance.Add(amount, "cheat");
        else MoneyService.Instance.TrySpend(-amount, "cheat");
    }

    private void GiveTime()
    {
        if (!float.TryParse(timeAmountInput.text, out float seconds) || seconds == 0f) return;
        if (seconds > 0f) ClockService.Instance.AddSeconds(seconds, "cheat");
        else ClockService.Instance.Spend(-seconds, "cheat");
    }

    // ---------- Annoyances ----------

    private void BuildAnnoyanceCheats()
    {
        if (cutsceneButtonContainer == null) return;

        AddCheat(TabAnnoyance, "Ring phone now", () => { if (PhoneController.Instance != null) PhoneController.Instance.Calls.ForceRingNow(); });
        AddCheat(TabAnnoyance, "Tax bill now", () => { if (ITRSService.Instance != null) ITRSService.Instance.ForceBillNow(); });
        AddCheat(TabAnnoyance, "Junk flyer now", () => { if (DoorController.Instance != null) DoorController.Instance.DropJunk(); });
        AddCheat(TabAnnoyance, "Package arrives (empty box)", () => { if (ShopService.Instance != null) ShopService.Instance.Delivery.ShipJunk(0f); });
        AddCheat(TabAnnoyance, "Cat blocks Laptop", () => CatBlock(FindFirstObjectByType<DeskLaptopOpener>()));
        AddCheat(TabAnnoyance, "Cat blocks Phone", () => { if (PhoneController.Instance != null) CatBlock(PhoneController.Instance.GetComponent<PointerReceiver>()); });
        AddCheat(TabAnnoyance, "Cat leaves", () => { var cat = Cat(); if (cat != null) cat.DebugRelease(); });
    }

    private CatController Cat()
    {
        if (_cat == null) _cat = FindFirstObjectByType<CatController>();
        return _cat;
    }

    private void CatBlock(Component target)
    {
        var cat = Cat();
        if (cat != null && target != null) cat.DebugBlock(target.GetComponent<PointerReceiver>());
    }

    // ---------- Hints, history, misc ----------

    private void BuildHintAndHistoryCheats()
    {
        if (cutsceneButtonContainer == null) return;

        AddCheat(TabDev, "Hint: next", () => ShowHintAt(_hintIndex + 1));
        AddCheat(TabDev, "Hint: previous", () => ShowHintAt(_hintIndex - 1));
        AddCheat(TabDev, "Hint: reset seen", () => { if (HintService.Instance != null) HintService.Instance.ResetSeen(); });
        AddCheat(TabDev, "History: one run per ending", AddSampleRuns);
        AddCheat(TabDev, "History: clear", () => { if (RunStatsService.Instance != null) RunStatsService.Instance.DebugClearHistory(); });
    }

    private void ShowHintAt(int index)
    {
        if (HintService.Instance == null) return;

        var ids = (HintId[])Enum.GetValues(typeof(HintId));
        _hintIndex = ((index % ids.Length) + ids.Length) % ids.Length;
        HintService.Instance.ShowAlways(ids[_hintIndex], 12);
        Debug.Log($"[Cheat] hint {ids[_hintIndex]} ({_hintIndex + 1}/{ids.Length})");
    }

    private void AddSampleRuns()
    {
        if (RunStatsService.Instance == null) return;

        int i = 0;
        foreach (EndReason reason in Enum.GetValues(typeof(EndReason)))
        {
            RunStatsService.Instance.DebugAddRecord(new RunRecord
            {
                bestTime = 120f + 55f * i,
                winCondition = reason.ToString(),
                totalMoneyEarned = 80 * (i + 1),
                cardsOpened = i % 6,
                newCardsToCollection = i % 3,
            });
            i++;
        }
    }

    private void BuildMiscCheats()
    {
        if (cutsceneButtonContainer == null) return;

        AddCheat(TabDev, "Restart run", () => { if (GameManager.Instance != null) GameManager.Instance.Restart(); });
        AddCheat(TabDev, "Close laptop and ticket", CloseOpenPanels);
        AddCheat(TabDev, "Clear hand (no payout)", ClearHand);
    }

    private void CloseOpenPanels()
    {
        if (ScratchTicket.Viewed != null) ScratchTicket.Viewed.CashOut();
        var laptop = FindFirstObjectByType<LaptopController>();
        if (laptop != null) laptop.CloseLaptop();
    }

    private void ClearHand()
    {
        if (CardHandManager.Instance == null) return;
        foreach (var card in new List<CardItem>(CardHandManager.Instance.Placed)) card.OnPawned();
    }

    // ---------- Cards and shared helpers ----------

    private void BuildCardButtons()
    {
        if (CardCollectionManager.Instance == null) return;

        foreach (var card in CardCollectionManager.Instance.AllCards)
            AddButton(cardButtonContainer, card.cardName, () => GiveCard(card));
    }

    private Button AddButton(Transform parent, string text, UnityAction onClick)
    {
        Button btn =  Instantiate(cardButtonPrefab, parent);

        var label = btn.GetComponentInChildren<TMP_Text>();
        if(label != null) label.text = text;

        btn.onClick.AddListener(onClick);
        return btn;
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
