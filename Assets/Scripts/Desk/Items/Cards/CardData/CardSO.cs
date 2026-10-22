using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Overtime/Cards/Normal")]
public class CardSO : ScriptableObject
{
    public enum Rarity { C, R, SR, UR }

    [Header("Identity")]
    public string cardName;
    public Sprite image;
    [TextArea] public string description;
    
    [Header("Drop")]
    public Rarity rarity;
    public float weight;
    
    [Header("Value")]
    public int pawnValue;
    
    [Header("Collection Visibility")]
    public bool hideUntilCollected;
    
    [Tooltip("Replaced by that one.")]
    public CardSO collectionRegisterAs;

    public CardSO CollectionTarget => collectionRegisterAs != null ? collectionRegisterAs : this;

    /// <summary>False while the card would do nothing (its app has no license yet). The Card Box skips it when it rolls a pack.</summary>
    public virtual bool IsAvailable => true;
}
