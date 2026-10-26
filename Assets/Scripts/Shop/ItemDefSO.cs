using UnityEngine;

/// <summary>
/// One row of the item catalog (Assets/Data/Configs/Items/*.asset). The catalog decides what the shop sells, what an
/// item pawns for and whether it starts on the desk. The id is permanent (saves and the pawn ledger key on it).
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "OverTimer/Shop/Item")]
public class ItemDefSO : ScriptableObject
{
    [Tooltip("Permanent key, e.g. \"power_bank\". Never rename.")]
    public string id;
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemKind kind;
    [Tooltip("Shop price. 0 for starting items that are never sold.")]
    public int price;
    [Tooltip("Pawn basis for a pawnable STARTING item (Laptop: 135). Bought items pawn for 80% of the price paid.")]
    public int catalogValue;
    public bool startsOwned, pawnable, sold;
    [Tooltip("A physical item that is its own object on the desk: it needs a ShopItemBinding in the scene (hides until owned, is looked up when pawned). Leave off for things that are UI or part of the wall.")]
    public bool sceneObject;
    [Tooltip("Licenses only: must equal the Laptop app's name (the DesktopIconView appName).")]
    public string appName;

    public ItemDef ToDef() => new ItemDef
    {
        id = id, displayName = displayName, kind = kind, price = price, catalogValue = catalogValue,
        startsOwned = startsOwned, pawnable = pawnable, sold = sold,
    };
}
