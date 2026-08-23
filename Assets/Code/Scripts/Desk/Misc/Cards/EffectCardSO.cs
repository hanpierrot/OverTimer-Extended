using UnityEngine;


public abstract class EffectCardSO : CardSO
{
    public abstract void OnPlaced(CardItem card);
    public abstract void OnRemoved(CardItem card);
}
