using System;
using System.Collections.Generic;
using UnityEngine;

public class CardHandManager : MonoBehaviour
{
    public static CardHandManager Instance { get; private set; }
    
    private readonly List<CardItem> heldCards = new List<CardItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    public bool HasRoom => heldCards.Count < GameManager.Instance.GameConfig.handCap;

    public void Register(CardItem card) => heldCards.Add(card);
    public void Release(CardItem card) => heldCards.Remove(card);
}
