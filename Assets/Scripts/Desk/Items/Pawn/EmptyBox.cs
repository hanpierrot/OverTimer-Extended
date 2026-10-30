using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// The empty cardboard box an unboxed parcel leaves behind (DESIGN §5.7). It is a small <see cref="JunkItem"/>: dragged
/// onto the Printer it is pawned like any other junk (the Pawn trigger does the selling while it is dragged over it).
/// This script only moves the box with the pointer and tells <see cref="DoorController"/> where it was let go, so a box
/// dropped outside the allowed area can be sent back to a corner.
/// </summary>
[RequireComponent(typeof(JunkItem))]
public class EmptyBox : DeskObject
{
    /// <summary>The player let go of the box (it is still active, i.e. it was not pawned).</summary>
    public event Action<EmptyBox> Dropped;

    protected override void OnDragStarted(Vector2 worldPos)
    {
        transform.DOKill(true);                 // finish a pop or a return tween before the player takes over
    }

    protected override void OnDragMoved(Vector2 worldPos)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
    }

    protected override void OnDragEnded(Vector2 worldPos)
    {
        if (gameObject.activeInHierarchy && Dropped != null) Dropped(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        transform.DOKill();
    }
}
