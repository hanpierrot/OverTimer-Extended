using System.Collections.Generic;
using UnityEngine;

public class CardCollectionManager : MonoBehaviour
{
    public static CardCollectionManager Instance { get; private set; }

    private readonly HashSet<CardSO> _collected = new HashSet<CardSO>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsCollected(CardSO card) => _collected.Contains(card);
    
    public bool Register(CardSO card) => _collected.Add(card);
}
