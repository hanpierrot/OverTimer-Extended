using UnityEngine;

/// <summary>
/// Put this on the physical desk Laptop sprite. Clicking it opens the Laptop
/// UI panel (the home screen with the app icons). Closing happens via the
/// panel's own CloseButton, not by clicking the desk object again.
/// </summary>
public class DeskLaptopOpener : DeskObject, IPawnable
{
    [SerializeField] private GameObject laptopPanel;
    [SerializeField] private LaptopController laptopController;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    public int PawnValue => pawnValue;

    protected override void OnClicked(Vector2 worldPos)
    {
        if (laptopController != null && laptopController.Disabled) return;
        
        laptopPanel.SetActive(true);
    }
    
    public void OnPawned() => Destroy(gameObject);
}
