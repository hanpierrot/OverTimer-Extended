using System;
using System.Collections.Generic;
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
    
    private static bool _pendingPreRevealWindow;
    public static event Action OnPreRevealDelivered;
    public static void QueuePreRevealWindow() => _pendingPreRevealWindow = true;
    
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
    }

    private void HandleTicketClicked(Vector2 worldPos)
    {
        if (!_isPurchased || _isBeingViewed) return;
        _isBeingViewed = true;
        
        if (closeButton != null) closeButton.onClick.AddListener(CashOut);
        
        PopulateWindows();
    }

    public bool TryPurchase()
    {
        if (!MoneyService.Instance.TrySpend(Cost, "ticket")) return false;
        
        _isPurchased = true;
        _isResolved = false;

        if (_pendingPreRevealWindow)
        {
            _pendingPreRevealWindow = false;
            _hasPreRevealWindow = true;
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
            OnPreRevealDelivered?.Invoke();
        }
    }
    
    private void HandleWindowRevealed(ScratchWindow window) => RefreshLiveResult();
    
    private void RefreshLiveResult()
    {
        if (_isResolved || resultLabel == null) return;

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
        if (!_isBeingViewed || _isResolved) return;

        foreach (var w in windows)
            if (!w.IsRevealed) return;

        Resolve();
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
        int maxMatch = CountBestMatch(out int bestSymbolIndex);
        if (maxMatch <= 1) return 0;

        float multiplier = symbolConfig.symbolPool[bestSymbolIndex].payoutMultiplier;
        return Mathf.RoundToInt(BaseReward * (maxMatch - 1) * multiplier);
    }

    private int CountBestMatch(out int bestSymbolIndex)
    {
        var counts = new Dictionary<int, int>();
        
        for (int i = 0; i < windows.Length; i++)
        {
            if (!windows[i].IsRevealed) continue;

            int idx = _activeSymbolIndex[i];
            counts.TryGetValue(idx, out int c);
            counts[idx] = c + 1;
        }

        int best = 0;
        int bestIdx = -1;
        foreach (var kv in counts)
        {
            bool better = kv.Value > best 
                          || (kv.Value == best && bestIdx >= 0 && symbolConfig.symbolPool[kv.Key].payoutMultiplier > symbolConfig.symbolPool[bestIdx].payoutMultiplier);

            if (better)
            {
                best = kv.Value;
                bestIdx = kv.Key;
            }
        }
        
        bestSymbolIndex = bestIdx;
        return best;
    }
}
