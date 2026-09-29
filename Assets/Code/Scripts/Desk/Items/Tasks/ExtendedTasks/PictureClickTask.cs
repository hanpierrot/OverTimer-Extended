using UnityEngine;

public class PictureClickTask : ClickTask
{
    [Header("Picture Frames")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite reactSprite;
    
    protected override int MinClicks => GameManager.Instance.GameConfig.pictureClickTaskMinClicks;
    protected override int MaxClicks => GameManager.Instance.GameConfig.pictureClickTaskMaxClicks;
    protected override float LockDuration => GameManager.Instance.GameConfig.pictureClickTaskLockDuration;
    
    protected override void OnClicked()
    {
        if (spriteRenderer != null && reactSprite != null)
            spriteRenderer.sprite = reactSprite;
    }

    protected override void OnWobbleFinished()
    {
        if (spriteRenderer != null && normalSprite != null)
            spriteRenderer.sprite = normalSprite;
    }
    
    protected override void ApplyReward()
    {
        MoneyService.Instance.Add(GameManager.Instance.GameConfig.pictureClickTaskMoneyReward, "picture click task");
    }
}
