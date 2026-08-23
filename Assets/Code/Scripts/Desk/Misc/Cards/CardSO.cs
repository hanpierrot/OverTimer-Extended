using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Overtime/Cards/Card")]
public class CardSO : ScriptableObject
{
    public enum Tier { Common, Uncommon, Rare, Legendary }

    [Header("Identity")]
    public string cardName;
    public Sprite image;
    [TextArea] public string description;
    
    [Header("Drop")]
    public Tier tier;
    public float weight;
    
    [Header("Value")]
    public int pawnValue;
}
