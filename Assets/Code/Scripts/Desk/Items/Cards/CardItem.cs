using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DeskObjectFocus))]
public class CardItem : MonoBehaviour, IPawnable
{
    [SerializeField] private SpriteRenderer artRenderer;
    [SerializeField] private GameObject cardPanel;
    [SerializeField] private Image panelImage;
    [SerializeField] private TMP_Text panelNameLabel;
    [SerializeField] private TMP_Text panelTierLabel;
    [SerializeField] private TMP_Text panelDescriptionLabel;

    private PointerReceiver _receiver;
    private CardSO _data;
    private EffectCardSO _effect;
    private bool _hasEnded;

    public int PawnValue => _data != null ? _data.pawnValue : 0;
    
    [Header("Debug (test-only - xoá khi có CardBox thật)")]
    [SerializeField] private CardSO debugTestCard;

    [ContextMenu("Debug Setup")]
    private void DebugSetup() => Setup(debugTestCard);
    
    private void Awake()
    {
        _receiver = GetComponent<PointerReceiver>();
        DebugSetup();
    }

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
        Debug.Log($"[CardItem] Setup called with data={data}, type={data?.GetType().Name}");
        
        _data = data;
        _effect = data as EffectCardSO;
        
        Debug.Log($"[CardItem] _effect resolved to: {(_effect != null ? _effect.GetType().Name : "null")}");

        if (artRenderer != null) artRenderer.sprite = data.image;

        _effect?.OnPlaced(this);
    }

    private void HandleCardClicked(Vector2 worldPos)
    {
        if (panelImage != null) panelImage.sprite = _data.image;
        if (panelNameLabel != null) panelNameLabel.text = _data.cardName;
        if (panelTierLabel != null) panelTierLabel.text = "Rarity: " + _data.rarity;
        if (panelDescriptionLabel != null) panelDescriptionLabel.text = _data.description;
    }

    private void HandleDragMoved(Vector2 worldPos)
    {
        transform.position = worldPos;
    }
    
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

    private void OnDestroy()
    {
        if (_hasEnded) return;
        _hasEnded = true;

        _effect?.OnRemoved(this);
    }
}
