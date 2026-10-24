using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// One letter on the pile at the door (a pooled object, like ParcelView). The sprite shown depends on the letter kind
/// (tax or junk). Clicking it (Door view only: it carries a ViewGate) reports to the controller.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LetterView : DeskObject
{
    [FormerlySerializedAs("stamp")]
    [SerializeField] private SpriteRenderer letterRenderer;
    [SerializeField] private Sprite taxSprite;
    [SerializeField] private Sprite junkSprite;

    public Letter Data { get; private set; }
    public event Action<LetterView> Clicked;

    public void Bind(Letter letter)
    {
        Data = letter;
        if (letterRenderer != null) letterRenderer.sprite = letter.kind == LetterKind.Tax ? taxSprite : junkSprite;
    }

    public void Unbind()
    {
        Data = null;
        gameObject.SetActive(false);
    }

    protected override void OnClicked(Vector2 worldPos)
    {
        if (Data != null && Clicked != null) Clicked(this);
    }
}
