using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardItem : MonoBehaviour, IPawnable
{
    [SerializeField] private SpriteRenderer artRenderer;

    private PointerReceiver _receiver;
    private CardSO _data;
    private EffectCardSO _effect;
    private bool _hasEnded;

    public int PawnValue => _data != null ? _data.pawnValue : 0;
    
    private void Awake() => _receiver = GetComponent<PointerReceiver>();

    private void OnEnable()
    {
        _receiver.ClickDown += HandleCardClicked;
        _receiver.DragUpdate += HandleDragMoved;
    }

    private void OnDisable()
    {
        _receiver.ClickDown -= HandleCardClicked;
        _receiver.DragUpdate -= HandleDragMoved;
    }
    
    public void Setup(CardSO data)
    {
        _hasEnded = false;
        
        _data = data;
        _effect = data as EffectCardSO;

        if (artRenderer != null) artRenderer.sprite = data.image;
        
        if (CardHandManager.Instance != null) CardHandManager.Instance.Register(this);

        _effect?.OnPlaced(this);
    }

    private void HandleCardClicked(Vector2 worldPos) => CardPanel.Instance?.Show(_data);

    private void HandleDragMoved(Vector2 worldPos) => transform.position = worldPos;
    
    public void EndEffect()
    {
        if (_hasEnded) return;
        _hasEnded = true;

        _effect?.OnRemoved(this);

        if (artRenderer != null && _data != null && _data.backSprite != null)
            artRenderer.sprite = _data.backSprite;

        if (_data != null) CardCollectionManager.Instance?.Register(_data);
    }
    
    public void ScheduleAutoRemove(float seconds)
    {
        if (seconds <= 0f) return;
        Invoke(nameof(EndEffect), seconds);
    }

    public void OnPawned()
    {
        if (CardHandManager.Instance != null) CardHandManager.Instance.Release(this);

        if (!_hasEnded)
        {
            _hasEnded = true;
            _effect?.OnRemoved(this);
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_hasEnded) return;
        _hasEnded = true;

        _effect?.OnRemoved(this);
    }
}
