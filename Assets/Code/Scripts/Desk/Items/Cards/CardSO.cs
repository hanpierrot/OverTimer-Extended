using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Overtime/Cards/Normal Card")]
public class CardSO : ScriptableObject
{
    public enum Rarity { C, R, SR, UR }

    [Header("Identity")]
    public string cardName;
    public Sprite image;
    public Sprite backSprite;
    [TextArea] public string description;
    
    [Header("Drop")]
    public Rarity rarity;
    public float weight;
    
    [Header("Value")]
    public int pawnValue;
}
