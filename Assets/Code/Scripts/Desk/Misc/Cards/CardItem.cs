using UnityEngine;

public class CardItem : DeskObject, IPawnable
{
    [SerializeField] private SpriteRenderer artRenderer;

    private CardSO _data;
    private EffectCardSO _effect;
    private bool _hasRemoved;

    public int PawnValue => _data != null ? _data.pawnValue : 0;

    public void Setup(CardSO data)
    {
        _data = data;
        _effect = data as EffectCardSO;

        if (artRenderer != null) artRenderer.sprite = data.image;

        _effect?.OnPlaced(this);
    }
    
    public void RemoveSelf() => Destroy(gameObject);
    
    public void ScheduleAutoRemove(float seconds)
    {
        if (seconds <= 0f) return;
        Invoke(nameof(RemoveSelf), seconds);
    }
    
    protected override void OnDragMoved(Vector2 worldPos)
    {
        transform.position = worldPos;
    }

    private void OnDestroy()
    {
        if (_hasRemoved) return;
        _hasRemoved = true;

        _effect?.OnRemoved(this);
    }
}
