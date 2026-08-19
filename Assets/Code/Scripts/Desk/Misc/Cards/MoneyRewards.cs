using UnityEngine;

public class MoneyRewards : MonoBehaviour, ICardEffect
{
    [SerializeField] private int moneyAmount = 20;
    
    public void Apply()
    {
        if (moneyAmount >= 0)
            MoneyService.Instance.Add(moneyAmount, "Card reward");
        else
            CountdownTimer.Instance.SubstractTime(-moneyAmount);
    }
}
