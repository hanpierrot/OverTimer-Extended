using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Overtime.Blackjack;

/// <summary>
/// One card slot: the real card sprite when a CardDeckConfig has one, a
/// colored rank+suit fallback when it doesn't (keeps things working even if
/// a sprite is missing), a solid-color back for face-down cards, or hidden
/// entirely when the hand doesn't reach this slot yet.
///
/// Label is optional - slots built as pure sprite art (no TMP_Text child)
/// work fine, the fallback text path just won't render anything if a sprite
/// is ever missing rather than crashing.
/// </summary>
public class CardSlotView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color faceColor = Color.white;
    [SerializeField] private Color backColor = new Color(0f, 0.1f, 0.5f);
    [SerializeField] private Color redSuitColor = new Color(0.75f, 0.1f, 0.1f);
    [SerializeField] private Color blackSuitColor = Color.black;

    public void ShowCard(Card card, Sprite sprite)
    {
        gameObject.SetActive(true);

        if (sprite != null)
        {
            background.sprite = sprite;
            background.color = Color.white;
            if (label != null) label.text = "";
            return;
        }

        // Fallback: no art for this card yet, still fully playable.
        background.sprite = null;
        background.color = faceColor;
        if (label != null)
        {
            label.text = $"{RankText(card.Rank)}{SuitSymbol(card.Suit)}";
            label.color = IsRed(card.Suit) ? redSuitColor : blackSuitColor;
        }
    }

    public void ShowFaceDown()
    {
        gameObject.SetActive(true);
        background.sprite = null;
        background.color = backColor;
        if (label != null) label.text = "";
    }

    public void Hide() => gameObject.SetActive(false);

    private static bool IsRed(Suit suit) => suit == Suit.Hearts || suit == Suit.Diamonds;

    private static string SuitSymbol(Suit suit) => suit switch
    {
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        Suit.Hearts => "♥",
        Suit.Spades => "♠",
        _ => "?"
    };

    private static string RankText(int rank) => rank switch
    {
        1 => "A", 11 => "J", 12 => "Q", 13 => "K",
        _ => rank.ToString()
    };
}
