using System;
using UnityEngine;

/// <summary>
/// One letter on the pile at the door (a pooled object, like ParcelView). Placeholder art: a paper rectangle with a
/// coloured stamp (red = tax, blue = junk). Clicking it (Door view only: it carries a ViewGate) reports to the controller.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LetterView : DeskObject
{
    [SerializeField] private SpriteRenderer stamp;
    [SerializeField] private Color taxColor = new Color(0.85f, 0.2f, 0.25f, 1f);
    [SerializeField] private Color junkColor = new Color(0.3f, 0.5f, 0.9f, 1f);

    public Letter Data { get; private set; }
    public event Action<LetterView> Clicked;

    public void Bind(Letter letter)
    {
        Data = letter;
        if (stamp != null) stamp.color = letter.kind == LetterKind.Tax ? taxColor : junkColor;
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
