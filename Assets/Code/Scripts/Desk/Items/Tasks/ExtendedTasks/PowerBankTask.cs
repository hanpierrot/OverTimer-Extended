using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PowerBankTask : HoldTask
{
    [Header("Power Bank Fill")]
    [SerializeField] private Image fillImage;
    [SerializeField] private float fullHoldDelay = 1.5f;
    [SerializeField] private float cooldownDuration = 5f;
    
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
        if (receiver != null) receiver.SetInteractable(false);
        
        yield return new WaitForSeconds(fullHoldDelay);
        
        float startFill = holdTimer;
        float t = 0f;
        while (t < cooldownDuration)
        {
            t += Time.deltaTime;
            holdTimer = Mathf.Lerp(startFill, 0f, t / cooldownDuration);
            UpdateHoldSprite();
            yield return null;
        }

        holdTimer = 0f;
        UpdateHoldSprite();
        if (receiver != null) receiver.SetInteractable(true);
    }
}
