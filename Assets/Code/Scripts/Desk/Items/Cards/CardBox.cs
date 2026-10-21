using UnityEngine;

[RequireComponent(typeof(PointerReceiver))]
[RequireComponent(typeof(Collider2D))]
public class CardBox : MonoBehaviour, IPawnable, IRestorable
{
    [SerializeField] private int packSize = 3;
    [SerializeField] private CardPackRevealPanel revealPanel;

    [Header("Roll pool")]
    [SerializeField] private string cardResourcesPath = "CardSO";

    public int PawnValue => GameManager.Instance.GameConfig.cardBoxPawnValue;
    // Not while the pack reveal is open: the cards are already rolled and paid for.
    public bool CanBePawned => revealPanel == null || !revealPanel.gameObject.activeInHierarchy;

    private PointerReceiver _receiver;
    private CardSO[] _allCards;

    private void Awake()
    {
        _receiver = GetComponent<PointerReceiver>();
        _allCards = Resources.LoadAll<CardSO>(cardResourcesPath);
    }
    
    private void OnEnable() => _receiver.ClickDown += HandleClick;
    private void OnDisable() => _receiver.ClickDown -= HandleClick;

    private void HandleClick(Vector2 worldPos)
    {
        GameConfig config = GameManager.Instance.GameConfig;

        if (CardHandManager.Instance != null && !CardHandManager.Instance.HasRoom)
        {
            HintService.Instance?.Show(HintId.HandFull, config.handCap);
            return;
        }
        
        int packCost = config.packCost;
        if (CostModifierService.Instance != null)
            packCost = Mathf.RoundToInt(packCost * CostModifierService.Instance.GetMultiplier(CostModifierService.Target.CardPack));

        if (!MoneyService.Instance.TrySpend(packCost, "card pack"))
        {
            HintService.Instance?.Show(HintId.NotEnoughMoney);
            return;
        }
        
        var rolled = new CardSO[packSize];
        for (int i = 0; i < packSize; i++)
            rolled[i] = RollCard(config);

        revealPanel.Begin(rolled);
    }

    private CardSO RollCard(GameConfig config)
    {
        var rng = RngService.Instance.Random;
        CardSO.Rarity rarity = RollRarity(config, rng);

        var candidates = System.Array.FindAll(_allCards, c => c.rarity == rarity);
        if (candidates.Length == 0) candidates = _allCards;

        return PickWeightedCard(candidates, rng);
    }

    private CardSO.Rarity RollRarity(GameConfig config, System.Random rng)
    {
        var weights = new (CardSO.Rarity rarity, float weight)[]
        {
            (CardSO.Rarity.C, config.commonWeight),
            (CardSO.Rarity.R, config.rareWeight),
            (CardSO.Rarity.SR, config.superRareWeight),
            (CardSO.Rarity.UR, config.ultrRareWeight),
        };

        float total = 0f;
        foreach (var w in weights) total += w.weight;

        float roll = (float)(rng.NextDouble() * total);
        float cumulative = 0f;
        foreach (var w in weights)
        {
            cumulative += w.weight;
            if (roll <= cumulative) return w.rarity;
        }
        return weights[weights.Length - 1].rarity;
    }

    private static CardSO PickWeightedCard(CardSO[] pool, System.Random rng)
    {
        float total = 0f;
        foreach (var c in pool) total += c.weight;
        
        float roll = (float)(rng.NextDouble() * total);
        float cumulative = 0f;
        foreach (var c in pool)
        {
            cumulative += c.weight;
            if (roll <= cumulative) return c;
        }
        return pool[pool.Length - 1];
    }
    
    public void OnPawned() => gameObject.SetActive(false);

    /// <summary>Bought back: nothing to reset, the box holds no state.</summary>
    public void OnRestored() { }
}
