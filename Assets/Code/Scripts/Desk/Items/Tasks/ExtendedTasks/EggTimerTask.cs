using UnityEngine;

public class EggTimerTask : HoldTask
{
    [Header("Sprite Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] holdSprites;

    private float _restUntil;       // game time: presses are ignored until then (cooldown after each crank)

    protected override int ConfigPawnValue => GameManager.Instance.GameConfig.eggTimerPawnValue;

    // The cooldown lives here, not in HoldTask: the Power Bank shares that base and has its own timing.
    protected override bool CanStartHold => ClockService.Instance == null || ClockService.Instance.GameTime >= _restUntil;

    protected override void ApplyReward()
    {
        base.ApplyReward();
        _restUntil = ClockService.Instance.GameTime + GameManager.Instance.GameConfig.eggTimerCooldown;
        OnHoldReset();              // show the resting frame while it cools down
    }

    protected override void UpdateHoldSprite()
    {
        if (spriteRenderer == null || holdSprites.Length == 0) return;

        float progress = Mathf.Clamp01(holdTimer / CrankTime);
        int index = Mathf.Min(Mathf.FloorToInt(progress * holdSprites.Length), holdSprites.Length - 1);
        spriteRenderer.sprite = holdSprites[index];
    }

    public override void OnRestored()
    {
        _restUntil = 0f;
        base.OnRestored();
    }

    protected override void OnHoldReset()
    {
        if (spriteRenderer != null && holdSprites.Length > 0)
            spriteRenderer.sprite = holdSprites[0];
    }
}
