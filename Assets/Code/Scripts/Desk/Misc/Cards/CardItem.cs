using System;
using UnityEngine;

public class CardItem : DeskObject, IPawnable
{
    [Header("Click")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite clickedSprite;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    public int PawnValue => pawnValue;

    private void Start()
    {
        CardHandManager.Instance.Register(this);
    }

    protected override void OnClicked(Vector2 worldPos)
    {
        if (spriteRenderer.sprite == clickedSprite) return;
        
        spriteRenderer.sprite = clickedSprite;
        
        foreach (var effect in GetComponents<ICardEffect>())
            effect.Apply();
    }

    protected override void OnDragMoved(Vector2 worldPos)
    {
        transform.position = worldPos;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.identity, 180f * Time.deltaTime);
    }
}
