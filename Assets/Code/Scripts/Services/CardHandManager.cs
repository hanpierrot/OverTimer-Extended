using System;
using System.Collections.Generic;
using UnityEngine;

public class CardHandManager : MonoBehaviour
{
    public static CardHandManager Instance { get; private set; }
    
    [SerializeField] private CardItem[] pool;
    
    [Header("Placement Area")]
    [SerializeField] private Vector2 placementCornerA;
    [SerializeField] private Vector2 placementCornerB;

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
    
    public Vector2 GetRandomPlacementPosition()
    {
        float tx = (float)RngService.Instance.Random.NextDouble();
        float ty = (float)RngService.Instance.Random.NextDouble();

        float x = Mathf.Lerp(placementCornerA.x, placementCornerB.x, tx);
        float y = Mathf.Lerp(placementCornerA.y, placementCornerB.y, ty);

        return new Vector2(x, y);
    }
}
