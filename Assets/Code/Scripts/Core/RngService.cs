using UnityEngine;

/// <summary>
/// The single shared source of randomness (DESIGN.md §7.2, §8). Everything that
/// needs a random number - tickets, card drops, blackjack - pulls from Random here.
/// Never use UnityEngine.Random; it can't be seeded per-run the same way.
/// </summary>
public class RngService : MonoBehaviour
{
    public static RngService Instance { get; private set; }

    [Header("Set a fixed seed to make a run reproducible for debugging")]
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int seed;

    public System.Random Random { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Random = useFixedSeed ? new System.Random(seed) : new System.Random();
    }
}
