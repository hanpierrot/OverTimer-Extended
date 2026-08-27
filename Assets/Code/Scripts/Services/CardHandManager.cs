using System;
using System.Collections.Generic;
using UnityEngine;

public class CardHandManager : MonoBehaviour
{
    public static CardHandManager Instance { get; private set; }
    
    [SerializeField] private CardItem[] pool;

    private readonly HashSet<CardItem> _placed = new HashSet<CardItem>();

    public bool HasRoom => _placed.Count < GameManager.Instance.GameConfig.handCap;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        foreach (var card in pool)
            if (card != null) card.gameObject.SetActive(false);
    }

    public CardItem TryGetFreeCard()
    {
        foreach (var card in pool)
            if (card != null && !card.gameObject.activeSelf) return card;
        return null;
    }
    
    public void Register(CardItem card) => _placed.Add(card);
    public void Release(CardItem card) => _placed.Remove(card);
}
