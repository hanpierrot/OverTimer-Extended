using System;
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
    
    [Header("Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;
    [Tooltip("Soft light around the screen. Shown only while the Laptop is usable (WireTask solved).")]
    [SerializeField] private GameObject glow;
    
    public int PawnValue => GameManager.Instance.GameConfig.laptopPawnValue;
    public bool CanBePawned => true;

    private void Start()
    {
        if (laptopController == null) return;
        laptopController.DisabledChanged += ApplySprite;
        ApplySprite(laptopController.Disabled);
    }

    private void OnDestroy()
    {
        if (laptopController != null) laptopController.DisabledChanged -= ApplySprite;
    }

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
        if (laptopController != null) ApplySprite(laptopController.Disabled);
    }

    private void ApplySprite(bool disabled)
    {
        if (glow != null) glow.SetActive(!disabled);
        if (spriteRenderer == null) return;
        Sprite s = disabled ? offSprite : onSprite;
        if (s != null) spriteRenderer.sprite = s;
    }
}
