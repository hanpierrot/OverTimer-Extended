using System.Collections.Generic;
using UnityEngine;

/// <summary>The list of every catalog item (Assets/Settings/Configs/ItemCatalog.asset).</summary>
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "OverTimer/Shop/Catalog")]
public class ItemCatalogSO : ScriptableObject
{
    public List<ItemDefSO> items = new List<ItemDefSO>();

    public List<ItemDef> BuildDefs()
    {
        var defs = new List<ItemDef>(items.Count);
        foreach (var item in items) if (item != null) defs.Add(item.ToDef());
        return defs;
    }

    public ItemDefSO Find(string id)
    {
        foreach (var item in items) if (item != null && item.id == id) return item;
        return null;
    }
}
