using UnityEngine;

[RequireComponent(typeof(PointerReceiver))]
[RequireComponent(typeof(Collider2D))]
public class CardBox : MonoBehaviour, IPawnable
{
    [SerializeField] private int packSize = 3;
    [SerializeField] private CardPackRevealPanel revealPanel;

    [Header("Roll pool")]
    [SerializeField] private CardSO[] cardPool;

    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    public int PawnValue => pawnValue;

    private PointerReceiver _receiver;

    private void Awake() => _receiver = GetComponent<PointerReceiver>();
    private void OnEnable() => _receiver.ClickDown += HandleClick;
    private void OnDisable() => _receiver.ClickDown -= HandleClick;

    private void HandleClick(Vector2 worldPos)
    {
        if (CardHandManager.Instance != null && !CardHandManager.Instance.HasRoom) return;
        
        GameConfig config = GameManager.Instance.GameConfig;
        
        if (!MoneyService.Instance.TrySpend(config.packCost, "card pack")) return;
        
        var rolled = new CardSO[packSize];
        for (int i = 0; i < packSize; i++)
            rolled[i] = RollCard(config);

        revealPanel.Begin(rolled);
    }

    private CardSO RollCard(GameConfig config)
    {
        var rng = RngService.Instance.Random;
        CardSO.Rarity rarity = RollRarity(config, rng);

        var candidates = System.Array.FindAll(cardPool, c => c.rarity == rarity);
        if (candidates.Length == 0) candidates = cardPool;

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
    
    public void OnPawned() => Destroy(gameObject);
}
