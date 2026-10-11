using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PowerBankTask : HoldTask
{
    [Header("Power Bank Fill")]
    [SerializeField] private Image fillImage;
    
    protected override int ConfigPawnValue => GameManager.Instance.GameConfig.powerBankPawnValue;
    protected override bool ResetProgressOnRelease => false;
    protected override float CrankTime => GameManager.Instance.GameConfig.powerBankCrankTime;
    
    protected override void UpdateHoldSprite()
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(holdTimer / CrankTime);
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
        if (receiver != null) receiver.SetInteractable(false);
        
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
        if (receiver != null) receiver.SetInteractable(true);
    }
}
