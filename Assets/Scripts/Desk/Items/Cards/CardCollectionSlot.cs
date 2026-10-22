using UnityEngine;
using UnityEngine.UI;

public class CardCollectionSlot : MonoBehaviour
{
    [SerializeField] private Image cardImage;

    public void Setup(CardSO card, bool collected)
    {
        cardImage.sprite = card.image;
        cardImage.color = collected ? Color.white : Color.gray2;
    }
}
