using System.Collections.Generic;
using UnityEngine;

public class SetCompletionTracker : MonoBehaviour
{
    public static SetCompletionTracker Instance { get; private set; }

    private readonly Dictionary<CardSO, bool> _pieceOnHand = new();
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

    public void BeginTracking(SetCompletionCardSO anchor, CardSO[] pieces)
    {
        _anchor = anchor;
        _anchorPlaced = true;
        _pieceOnHand.Clear();
        foreach (var p in pieces) _pieceOnHand[p] = false;
        
        if (CardHandManager.Instance != null)
            foreach (var placed in CardHandManager.Instance.Placed)
                if (_pieceOnHand.ContainsKey(placed.Data)) _pieceOnHand[placed.Data] = true;

        CheckWin();
    }
    
    public void StopTracking() => _anchorPlaced = false;

    private void HandleCardPlaced(CardSO card) => UpdatePiece(card, true);
    private void HandleCardRemoved(CardSO card) => UpdatePiece(card, false);

    private void UpdatePiece(CardSO card, bool onHand)
    {
        if (!_anchorPlaced || !_pieceOnHand.ContainsKey(card)) return;
        _pieceOnHand[card] = onHand;
        CheckWin();
    }
    
    private void CheckWin()
    {
        if (!_anchorPlaced || GameManager.Instance.IsGameOver) return;

        foreach (var onHand in _pieceOnHand.Values)
            if (!onHand) return;

        if (_anchor != null)
        {
            CardCollectionManager.Instance?.Register(_anchor);
            foreach (CardSO piece in _anchor.pieces)
                CardCollectionManager.Instance?.Register(piece);
        }
        GameManager.Instance.Win();
    }
}
