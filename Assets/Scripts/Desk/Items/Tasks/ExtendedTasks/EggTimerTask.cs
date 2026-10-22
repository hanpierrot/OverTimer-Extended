using System;
using UnityEngine;

public class EggTimerTask : HoldTask
{
    [Header("Sprite Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] holdSprites;

    private float _restUntil;
    
    public static event Action<float> TimePaid;

    protected override int ConfigPawnValue => GameManager.Instance.GameConfig.eggTimerPawnValue;

    protected override bool CanStartHold => ClockService.Instance == null || ClockService.Instance.GameTime >= _restUntil;

    protected override void ApplyReward()
    {
        base.ApplyReward();
        TimePaid?.Invoke(GameManager.Instance.GameConfig.timePayout);
        _restUntil = ClockService.Instance.GameTime + GameManager.Instance.GameConfig.eggTimerCooldown;
        OnHoldReset();
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
