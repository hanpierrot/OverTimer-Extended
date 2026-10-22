using UnityEngine;

/// <summary>
/// Put on every physical catalog object that lives on the desk. It links the object to its catalog row: the object hides
/// itself at the start of a run unless the player owns the item (delivery shows it later), and the Pawn looks the item
/// up through it. The object MUST be saved ACTIVE in the scene: an inactive object never runs Awake, so it could not
/// register itself (a bought item would never appear).
/// </summary>
public class ShopItemBinding : MonoBehaviour
{
    [Tooltip("Catalog id, e.g. \"power_bank\". Must match an ItemDefSO.")]
    [SerializeField] private string itemId;

    public string ItemId => itemId;

    /// <summary>Where the item sat at the start of the run. A bought-back item returns here, not to the printer it was pawned on.</summary>
    public Vector3 HomePosition { get; private set; }

    private void Awake()
    {
        HomePosition = transform.position;
        ShopService.Register(this);      // queued if the service has not woken up yet
    }

    private void Start()
    {
        var shop = ShopService.Instance;
        if (shop == null || !shop.Ownership.Has(itemId)) return;   // ShopService warns about unknown ids
        if (!shop.Ownership.IsOwned(itemId)) gameObject.SetActive(false);
    }
}
