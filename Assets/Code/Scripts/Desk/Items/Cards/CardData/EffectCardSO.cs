using UnityEngine;
using UnityEngine.Serialization;


public abstract class EffectCardSO : CardSO
{
    [FormerlySerializedAs("isDebuff")]
    [SerializeField] private bool _isDebuff;
    
    public virtual bool isDebuff => _isDebuff;

    public virtual bool BlocksPawning => false;
    
    public abstract void OnPlaced(CardItem card);
    public abstract void OnRemoved(CardItem card);
}
