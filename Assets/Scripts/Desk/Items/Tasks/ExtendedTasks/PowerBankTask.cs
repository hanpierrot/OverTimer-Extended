using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PowerBankTask : HoldTask
{
    [Header("Charge Frames")]
    [SerializeField] private SpriteRenderer chargeRenderer;
    [SerializeField] private Sprite[] chargeSprites;
    
    protected override int ConfigPawnValue => GameManager.Instance.GameConfig.powerBankPawnValue;
    protected override bool ResetProgressOnRelease => false;
    protected override float CrankTime => GameManager.Instance.GameConfig.powerBankCrankTime;
    
    private void Start() => UpdateHoldSprite();
    
    protected override void UpdateHoldSprite()
    {
        if (chargeRenderer == null || chargeSprites == null || chargeSprites.Length == 0) return;

        float progress = Mathf.Clamp01(holdTimer / CrankTime);
        int index = Mathf.RoundToInt(progress * (chargeSprites.Length - 1));
        
        chargeRenderer.sprite = chargeSprites[index];
    }

    protected override void ApplyReward()
    {
        MoneyService.Instance.Add(GameManager.Instance.GameConfig.powerBankMoneyReward, "power bank");
    }
    
    protected override void OnTaskCompleted()
    {
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        var config = GameManager.Instance.GameConfig;
        if (receiver != null) receiver.SetInteractable(false, HintId.PowerBankRecharging);
        
        yield return new WaitForSeconds(config.powerBankFullHoldDelay);
        
        float startFill = holdTimer;
        float t = 0f;
        while (t < config.powerBankCooldown)
        {
            t += Time.deltaTime;
            holdTimer = Mathf.Lerp(startFill, 0f, t / config.powerBankCooldown);
            UpdateHoldSprite();
            yield return null;
        }

        holdTimer = 0f;
        UpdateHoldSprite();
        
        yield return new WaitForSeconds(config.powerBankRestTime);
        
        if (receiver != null) receiver.SetInteractable(true);
    }
}
