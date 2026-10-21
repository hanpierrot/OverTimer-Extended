using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TicketTierKind { TripleMatch, LuckyNine }

[RequireComponent(typeof(DeskObjectFocus))]
public class ScratchTicket : MonoBehaviour
{
    [Serializable]
    public struct SymbolEntry
    {
        public Sprite sprite;
        public float weight;
        public float payoutMultiplier;
    }
    
    [Header("Tier")]
    [SerializeField] private TicketTierKind tierKind = TicketTierKind.TripleMatch;
    
    [Header("Symbols")]
    [SerializeField] private TicketSymbolConfig symbolConfig;
    
    [Header("Scratch Panel")]
    [SerializeField] private GameObject scratchPanel;
    [SerializeField] private ScratchWindow[] windows;
    [SerializeField] private TMP_Text resultLabel;
    [SerializeField] private Button closeButton;
    
    private int[] _activeSymbolIndex;
    private bool _isResolved;
    private bool _isPurchased;
    private bool _isBeingViewed;
    private PointerReceiver _receiver;
    
    private bool _hasPreRevealWindow;
    private bool _isJackpot;
    private bool _isCursed;       // Lucky Nine only: all nine windows are the lowest symbol; finishing the last window ends the run
    private bool _omenShown;
    private bool _scratchedAll;

    private static readonly Color CurseColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    private static bool _pendingPreRevealWindow;
    public static event Action OnPreRevealDelivered;
    
    public TicketTierKind TierKind => tierKind;
    
    private static readonly List<CardItem> _preRevealCards = new List<CardItem>();
    private CardItem[] _windowOwners = System.Array.Empty<CardItem>();

    public static void QueuePreRevealWindow(CardItem owner = null)
    {
        _pendingPreRevealWindow = true;
        if (owner != null && !_preRevealCards.Contains(owner)) _preRevealCards.Add(owner);
    }

    public static void WithdrawPreRevealWindow(CardItem owner)
    {
        if (owner == null) return;
        if (_preRevealCards.Remove(owner) && _preRevealCards.Count == 0) _pendingPreRevealWindow = false;
    }
    
    /// <summary>The ticket whose panel is open (dev tools use it to open windows).</summary>
    public static ScratchTicket Viewed { get; private set; }

    public static void ResetPending()
    {
        Viewed = null;
        _pendingPreRevealWindow = false;
        _preRevealCards.Clear();
        OnPreRevealDelivered = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void HookPendingReset()
    {
        ResetPending();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedReset;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedReset;
    }

    private static void OnSceneLoadedReset(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => ResetPending();
    
    private GameConfig Config => GameManager.Instance.GameConfig;

    private int Cost
    {
        get
        {
            int baseCost = tierKind == TicketTierKind.TripleMatch ? Config.tripleMatchCost : Config.luckyNineCost;
            float mult = CostModifierService.Instance != null ? CostModifierService.Instance.GetMultiplier(CostModifierService.Target.Ticket) : 1f;
            return Mathf.RoundToInt(baseCost * mult);
        }
    }
    private int BaseReward => tierKind == TicketTierKind.TripleMatch ? Config.tripleMatchBaseReward : Config.luckyNineBaseReward;

    private void Awake()
    {
        _receiver = GetComponent<PointerReceiver>();
    }

    private void OnEnable() 
    {
        _receiver.ClickDown += HandleTicketClicked;
        
        foreach (var w in windows) w.OnRevealed += HandleWindowRevealed;
    }
    private void OnDisable()
    {
        _receiver.ClickDown -= HandleTicketClicked;
        
        foreach (var w in windows) w.OnRevealed -= HandleWindowRevealed;
        
        if (closeButton != null) closeButton.onClick.RemoveListener(CashOut);
        if (scratchPanel != null) scratchPanel.transform.DOKill();
        if (Viewed == this) Viewed = null;
    }

    private void HandleTicketClicked(Vector2 worldPos)
    {
        if (!_isPurchased || _isBeingViewed) return;
        _isBeingViewed = true;
        Viewed = this;

        HintService.Instance?.ShowOnce(HintId.TicketCashOut);

        if (closeButton != null) closeButton.onClick.AddListener(CashOut);
        
        PopulateWindows();
    }

    public bool TryPurchase()
    {
        if (!CheatFlags.FreeTicketsOn && !MoneyService.Instance.TrySpend(Cost, "ticket")) return false;
        
        _isPurchased = true;
        _isResolved = false;
        _scratchedAll = false;

        if (_pendingPreRevealWindow)
        {
            _pendingPreRevealWindow = false;
            _hasPreRevealWindow = true;
            _windowOwners = _preRevealCards.ToArray(); 
            _preRevealCards.Clear();
        }
        
        RollSymbols();
        if (resultLabel != null) resultLabel.text = "0$";

        return true;
    }

    public void CashOut()
    {
        if (!_isResolved) Resolve();
        if (closeButton != null) closeButton.onClick.RemoveListener(CashOut);
        
        _isPurchased = false;
        _isBeingViewed = false;
        if (Viewed == this) Viewed = null;

        if (scratchPanel != null)
        {
            scratchPanel.SetActive(false);
            resultLabel.text = "";
        }
        gameObject.SetActive(false);
    }

    private void RollSymbols()
    {
        _activeSymbolIndex = new int[windows.Length];
        for (int i = 0; i < windows.Length; i++)
            _activeSymbolIndex[i] = PickWeightedIndex(symbolConfig.symbolPool, RngService.Instance.Random);

        _isJackpot = false;
        _isCursed = false;
        _omenShown = false;
        if (tierKind != TicketTierKind.LuckyNine) return;

        // One roll, two exclusive bands: [0, jackpot) is a jackpot, [jackpot, jackpot + curse) is cursed.
        // The roll is always drawn, so a cheat that forces the result does not change the random stream.
        var forced = CheatFlags.ConsumeNextLuckyNine();
        double roll = RngService.Instance.Random.NextDouble();
        bool jackpot = forced == CheatFlags.NextTicket.Jackpot
                       || (forced == CheatFlags.NextTicket.None && roll < Config.luckyNineJackpotChance);
        bool cursed = !jackpot && (forced == CheatFlags.NextTicket.Cursed
                       || (forced == CheatFlags.NextTicket.None
                           && roll < Config.luckyNineJackpotChance + Config.luckyNineCurseChance));

        if (jackpot)
        {
            _isJackpot = true;
            FillAll(IndexOfHighestMultiplier(symbolConfig.symbolPool));
            if (Debug.isDebugBuild) Debug.Log("[Ending] Lucky Nine: this ticket is a JACKPOT");
        }
        else if (cursed)
        {
            _isCursed = true;
            FillAll(IndexOfLowestMultiplier(symbolConfig.symbolPool));
            if (Debug.isDebugBuild) Debug.Log("[Ending] Lucky Nine: this ticket is CURSED");
        }
    }

    private void FillAll(int symbolIndex)
    {
        for (int i = 0; i < windows.Length; i++) _activeSymbolIndex[i] = symbolIndex;
    }

    public static int IndexOfLowestMultiplier(SymbolEntry[] pool)
    {
        int best = 0;
        for (int i = 1; i < pool.Length; i++)
            if (pool[i].payoutMultiplier < pool[best].payoutMultiplier) best = i;
        return best;
    }

    public static int IndexOfHighestMultiplier(SymbolEntry[] pool)
    {
        int best = 0;
        for (int i = 1; i < pool.Length; i++)
            if (pool[i].payoutMultiplier > pool[best].payoutMultiplier) best = i;
        return best;
    }

    private void PopulateWindows()
    {
        for(int i = 0; i <  windows.Length; i++)
            windows[i].Populate(symbolConfig.symbolPool[_activeSymbolIndex[i]].sprite);
        
        if (_hasPreRevealWindow)
        {
            _hasPreRevealWindow = false;
            int idx = RngService.Instance.Random.Next(windows.Length);
            windows[idx].Reveal();
            var served = _windowOwners;
            _windowOwners = System.Array.Empty<CardItem>();
            foreach (var card in served) if (card != null) card.EndEffect();
            OnPreRevealDelivered?.Invoke();
        }
    }
    
    /// <summary>Dev: open windows until `total` are revealed (9 = all), as if the player had scratched them.</summary>
    public void RevealWindows(int total)
    {
        if (!_isBeingViewed || _isResolved) return;

        int revealed = 0;
        foreach (var w in windows) if (w.IsRevealed) revealed++;
        for (int i = 0; i < windows.Length && revealed < total; i++)
        {
            if (windows[i].IsRevealed) continue;
            windows[i].Reveal();
            revealed++;
        }
    }

    private void HandleWindowRevealed(ScratchWindow window)
    {
        RefreshLiveResult();
        CheckCurseOmen();
    }

    // The way out of a cursed ticket: once enough windows are open the player is told to Cash Out.
    private void CheckCurseOmen()
    {
        if (!_isCursed || _omenShown || !_isBeingViewed || _isResolved) return;

        int revealed = 0;
        foreach (var w in windows) if (w.IsRevealed) revealed++;
        if (revealed < Config.luckyNineCurseOmenWindows) return;

        _omenShown = true;
        if (HintService.Instance != null) HintService.Instance.Show(HintId.TicketCursed);
    }

    private void RefreshLiveResult()
    {
        if (!_isBeingViewed || _isResolved || resultLabel == null) return;

        int reward = ComputeCurrentReward();
        resultLabel.text = reward > 0 ? $"+${reward}" : "$0";
    }

    private static int PickWeightedIndex(SymbolEntry[] pool, System.Random rng)
    {
        float total = 0f;
        for(int i = 0; i < pool.Length; i++) total += pool[i].weight;
        
        float roll = (float) (rng.NextDouble() * total);
        float cumunlative = 0f;
        for (int i = 0; i < pool.Length; i++)
        {
            cumunlative += pool[i].weight;
            if (roll <= cumunlative) return i;
        }
        return pool.Length - 1;
    }

    private void Update()
    {
        if (!_isBeingViewed || _isResolved || _scratchedAll) return;

        foreach (var w in windows)
            if (!w.IsRevealed) return;

        FinishScratching();
    }

    private void FinishScratching()
    {
        _scratchedAll = true;
        RefreshLiveResult();

        if (_isCursed)
        {
            _isCursed = false;
            StartCoroutine(CurseBeat());
            return;
        }

        if (_isJackpot)
        {
            _isJackpot = false;
            GameManager.Instance.GameOver(EndReason.TicketJackpotWin);
        }
    }

    // A short beat before the loss: the panel shakes and the label turns red, and Cash Out is locked so the player cannot flee.
    private IEnumerator CurseBeat()
    {
        float seconds = Config.curseBeatSeconds;
        if (closeButton != null) closeButton.interactable = false;
        if (resultLabel != null) resultLabel.color = CurseColor;
        if (scratchPanel != null) scratchPanel.transform.DOShakePosition(seconds, 8f, 30).SetUpdate(true);

        yield return new WaitForSecondsRealtime(seconds);

        if (!GameManager.Instance.IsGameOver) GameManager.Instance.GameOver(EndReason.TicketCurseLoss);
    }

    private void Resolve()
    {
        if (_isResolved) return;
        _isResolved = true;

        int reward = ComputeCurrentReward();
        
        foreach (var w in windows) w.Reveal();

        if (reward > 0) MoneyService.Instance.Add(reward, "ticket");

        if (resultLabel != null)
            resultLabel.text = reward > 0 ? $"+${reward}" : "$0";
    }

    private int ComputeCurrentReward()
    {
        var counts = new Dictionary<int, int>();

        for (int i = 0; i < windows.Length; i++)
        {
            if (!windows[i].IsRevealed) continue;

            int idx = _activeSymbolIndex[i];
            counts.TryGetValue(idx, out int c);
            counts[idx] = c + 1;
        }

        float reward = 0f;
        foreach (var kv in counts)
        {
            if (kv.Value <= 1) continue;
            reward += BaseReward * (kv.Value - 1) * symbolConfig.symbolPool[kv.Key].payoutMultiplier;
        }

        return Mathf.RoundToInt(reward);
    }
}
