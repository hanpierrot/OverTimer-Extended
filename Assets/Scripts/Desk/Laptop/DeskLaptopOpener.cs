using UnityEngine;

/// <summary>
/// Put this on the physical desk Laptop sprite. Clicking it opens the Laptop
/// UI panel (the home screen with the app icons). Closing happens via the
/// panel's own CloseButton, not by clicking the desk object again.
/// </summary>
public class DeskLaptopOpener : DeskObject, IPawnable, IRestorable
{
    [SerializeField] private GameObject laptopPanel;
    [SerializeField] private LaptopController laptopController;
    
    public int PawnValue => GameManager.Instance.GameConfig.laptopPawnValue;
    public bool CanBePawned => true;

    protected override void OnClicked(Vector2 worldPos)
    {
        if (laptopController != null && laptopController.Disabled)
        {
            HintService.Instance?.Show(HintId.LaptopNoPower);
            return;
        }
        
        laptopPanel.SetActive(true);
    }
    
    public void OnPawned()
    {
        // Close the laptop (and the open app, which forfeits unbanked money) first. Licenses and the solved wire puzzle
        // are not touched: they live in Ownership / LaptopController.Disabled.
        if (laptopController != null) laptopController.CloseLaptop();
        gameObject.SetActive(false);
    }

    /// <summary>Bought back: comes back closed.</summary>
    public void OnRestored()
    {
        if (laptopPanel != null) laptopPanel.SetActive(false);
    }
}
