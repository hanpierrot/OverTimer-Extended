using System.Collections.Generic;
using UnityEngine;
using Overtime.Blackjack;

/// <summary>Renders one hand as a row of fixed CardSlotView slots.</summary>
public class CardRowView : MonoBehaviour
{
    [SerializeField] private CardSlotView[] cardSlots;
    [SerializeField] private CardDeckConfig deck;

    public void Render(IReadOnlyList<Card> hand, int hideIndex)
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (i >= hand.Count)
            {
                cardSlots[i].Hide();
                continue;
            }

            if (i == hideIndex)
                cardSlots[i].ShowFaceDown();
            else
                cardSlots[i].ShowCard(hand[i], deck != null ? deck.GetSprite(hand[i]) : null);
        }
    }
}
