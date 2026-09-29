using UnityEngine;

public class EggTimerTask : HoldTask
{
    [Header("Sprite Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] holdSprites;

    protected override void UpdateHoldSprite()
    {
        if (spriteRenderer == null || holdSprites.Length == 0) return;

        float progress = Mathf.Clamp01(holdTimer / CrankTime);
        int index = Mathf.Min(Mathf.FloorToInt(progress * holdSprites.Length), holdSprites.Length - 1);
        spriteRenderer.sprite = holdSprites[index];
    }

    protected override void OnHoldReset()
    {
        if (spriteRenderer != null && holdSprites.Length > 0)
            spriteRenderer.sprite = holdSprites[0];
    }
}
