using UnityEngine;
using Overtime.Blackjack;

/// <summary>
/// Maps every one of the 52 playing-card sprites to its Card struct. One
/// array per suit, indexed by rank - 1 (rank 1 = Ace at index 0, ... 13 = King
/// at index 12), matching BlackjackGame.Card's Rank/Suit fields exactly.
/// </summary>
[CreateAssetMenu(fileName = "CardDeckConfig", menuName = "Overtime/Card Deck Config")]
public class CardDeckConfig : ScriptableObject
{
    [SerializeField] private Sprite[] clubs = new Sprite[13];
    [SerializeField] private Sprite[] diamonds = new Sprite[13];
    [SerializeField] private Sprite[] hearts = new Sprite[13];
    [SerializeField] private Sprite[] spades = new Sprite[13];

    public Sprite GetSprite(Card card)
    {
        Sprite[] suitSprites = card.Suit switch
        {
            Suit.Clubs => clubs,
            Suit.Diamonds => diamonds,
            Suit.Hearts => hearts,
            Suit.Spades => spades,
            _ => null
        };

        if (suitSprites == null || card.Rank < 1 || card.Rank > 13) return null;
        return suitSprites[card.Rank - 1];
    }
}
