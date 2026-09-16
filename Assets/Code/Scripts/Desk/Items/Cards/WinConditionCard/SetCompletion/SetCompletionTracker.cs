using System.Collections.Generic;
using UnityEngine;

public class SetCompletionTracker : MonoBehaviour
{
    public static SetCompletionTracker Instance { get; private set; }

    private List<CardSO[]> _pieceGroups;
    private readonly Dictionary<CardSO, int> _cardCount = new();
    private bool _anchorPlaced;
    private SetCompletionCardSO _anchor;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (CardHandManager.Instance != null)
        {
            CardHandManager.Instance.CardPlaced += HandleCardPlaced;
            CardHandManager.Instance.CardRemoved += HandleCardRemoved;
        }
    }

    private void OnDestroy()
    {
        if (CardHandManager.Instance != null)
        {
            CardHandManager.Instance.CardPlaced -= HandleCardPlaced;
            CardHandManager.Instance.CardRemoved -= HandleCardRemoved;
        }
    }

    public void BeginTracking(SetCompletionCardSO anchor, PieceGroup[] pieces)
    {
        _anchor = anchor;
        _anchorPlaced = true;
        _pieceGroups = new List<CardSO[]>();
        _cardCount.Clear();
        
        foreach (var group in pieces)
        {
            _pieceGroups.Add(group.alternatives);
            foreach (var alt in group.alternatives)
                if (!_cardCount.ContainsKey(alt)) _cardCount[alt] = 0;
        }

        if (CardHandManager.Instance != null)
            foreach (var placed in CardHandManager.Instance.Placed)
                if (_cardCount.ContainsKey(placed.Data)) _cardCount[placed.Data]++;

        CheckWin();
    }
    
    public void StopTracking() => _anchorPlaced = false;

    private void HandleCardPlaced(CardSO card) => UpdateCount(card, +1);
    private void HandleCardRemoved(CardSO card) => UpdateCount(card, -1);

    private void UpdateCount(CardSO card, int delta)
    {
        if (!_anchorPlaced || !_cardCount.ContainsKey(card)) return;
        _cardCount[card] = Mathf.Max(0, _cardCount[card] + delta);
        CheckWin();
    }
    
    private void CheckWin()
    {
        if (!_anchorPlaced || GameManager.Instance.IsGameOver) return;

        var satisfiedByGroup = new List<CardSO>();
        foreach (var group in _pieceGroups)
        {
            CardSO satisfied = null;
            foreach (var alt in group)
                if (_cardCount.TryGetValue(alt, out int c) && c > 0) { satisfied = alt; break; }

            if (satisfied == null) return;
            satisfiedByGroup.Add(satisfied);
        }

        if (_anchor != null)
        {
            CardCollectionManager.Instance?.Register(_anchor);
            foreach (var satisfied in satisfiedByGroup)
                CardCollectionManager.Instance?.Register(satisfied);
        }

        GameManager.Instance.Win();
    }
}
