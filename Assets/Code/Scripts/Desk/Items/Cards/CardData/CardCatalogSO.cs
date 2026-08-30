using UnityEngine;

[CreateAssetMenu(fileName = "CardCatalog", menuName = "Overtime/Cards/Card Catalog")]
public class CardCatalogSO : ScriptableObject
{
    public CardSO[] allCards;
}
