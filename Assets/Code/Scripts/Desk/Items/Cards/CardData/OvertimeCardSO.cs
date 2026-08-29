using UnityEngine;

[CreateAssetMenu(fileName = "Overtime", menuName = "Overtime/Cards/Effects/Overtime")]
public class OvertimeCardSO : EffectCardSO
{
    [SerializeField] private float payoutMultiplier = 2f;

    public override void OnPlaced(CardItem card)
    {
        MiningApp.QueueNextVeinMultiplier(payoutMultiplier);

        void HandleResolved()
        {
            MiningApp.OnNextVeinEffectResolved -= HandleResolved;
            card.EndEffect();
        }
        MiningApp.OnNextVeinEffectResolved += HandleResolved;
    }
    
    public override void OnRemoved(CardItem card) { }
}
