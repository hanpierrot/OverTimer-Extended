using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardItem : MonoBehaviour, IPawnable
{
    [SerializeField] private SpriteRenderer artRenderer;
    [SerializeField] private CardFlip flip;

    private PointerReceiver _receiver;
    private CardSO _data;
    private EffectCardSO _effect;
    private bool _hasEnded;
    private Coroutine _repeatRoutine;
    private bool _dragging;
    private bool _relocateWhenDropped;

    /// <summary>A debuff pawned while its effect still runs pays nothing (it costs debuffDismissFee, charged by the Printer).</summary>
    public int PawnValue => _data == null || IsDebuffActive ? 0 : _data.pawnValue;

    /// <summary>A debuff that has been placed and has not ended yet.</summary>
    public bool IsDebuffActive => _effect != null && _effect.isDebuff && !_hasEnded && gameObject.activeInHierarchy;
    public bool CanBePawned => _effect == null || !_effect.BlocksPawning;
    public CardSO Data => _data;
    public Sprite CurrentArt => artRenderer != null ? artRenderer.sprite : null;
    public bool IsFaceDown => flip != null && flip.IsShowingBack;
    
    private void Awake() => _receiver = GetComponent<PointerReceiver>();

    private void OnEnable()
    {
        _receiver.ClickDown += HandleCardClicked;
        _receiver.DragStart += HandleDragStart;
        _receiver.DragUpdate += HandleDragMoved;
        _receiver.DragEnd += HandleDragEnd;
    }

    private void OnDisable()
    {
        _receiver.ClickDown -= HandleCardClicked;
        _receiver.DragStart -= HandleDragStart;
        _receiver.DragUpdate -= HandleDragMoved;
        _receiver.DragEnd -= HandleDragEnd;
        _dragging = false;
        _relocateWhenDropped = false;
    }
    
    public void Setup(CardSO data)
    {
        CancelInvoke(nameof(EndEffect));   // a pooled card must not inherit the previous card's timer
        _hasEnded = false;
        
        _data = data;
        _effect = data as EffectCardSO;

        if (artRenderer != null) artRenderer.sprite = data.image;
        
        if (flip != null)
            flip.ResetFlip();
        
        if (CardHandManager.Instance != null) CardHandManager.Instance.Register(this);

        _effect?.OnPlaced(this);
    }

    private void HandleCardClicked(Vector2 worldPos)
    {
        if (_data == null || IsFaceDown) return;
        
        CardPanel.Instance?.Show(_data, artRenderer != null ? artRenderer.sprite : null);
    }

    private void HandleDragStart(Vector2 worldPos) => _dragging = true;

    private void HandleDragMoved(Vector2 worldPos) => transform.position = worldPos;

    private void HandleDragEnd(Vector2 worldPos)
    {
        _dragging = false;
        if (!_relocateWhenDropped) return;

        _relocateWhenDropped = false;
        MoveToPlacementSpot();
    }

    /// <summary>
    /// Moves the card to another spot of the placement area (the Printer refused to pawn it). A card the player is still
    /// holding moves when it is dropped, otherwise the drag would pull it straight back.
    /// </summary>
    public void Relocate()
    {
        if (_dragging) _relocateWhenDropped = true;
        else MoveToPlacementSpot();
    }

    private void MoveToPlacementSpot()
    {
        if (CardHandManager.Instance != null)
            transform.position = CardHandManager.Instance.GetRandomPlacementPosition();
    }
    
    public void SetArt(Sprite sprite)
    {
        if (artRenderer != null) artRenderer.sprite = sprite;
    }
    
    public void ScheduleRepeating(Action callback, float interval)
    {
        if (_repeatRoutine != null) StopCoroutine(_repeatRoutine);
        _repeatRoutine = StartCoroutine(RepeatRoutine(callback, interval));
    }
    
    private IEnumerator RepeatRoutine(Action callback, float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            callback?.Invoke();
        }
    }
    
    public void CancelRepeating()
    {
        if (_repeatRoutine != null) { StopCoroutine(_repeatRoutine); _repeatRoutine = null; }
    }
    
    public void EndEffect()
    {
        if (_hasEnded) return;
        _hasEnded = true;

        _effect?.OnRemoved(this);

        if (flip != null) flip.FlipTo(true);

        if (_data != null) CardCollectionManager.Instance?.Register(_data);
    }
    
    public void ScheduleAutoRemove(float seconds)
    {
        if (seconds <= 0f) return;
        Invoke(nameof(EndEffect), seconds);
    }

    public void OnPawned()
    {
        CancelInvoke(nameof(EndEffect));
        if (CardHandManager.Instance != null) CardHandManager.Instance.Release(this);
        
        _effect?.OnPawned(this);

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
