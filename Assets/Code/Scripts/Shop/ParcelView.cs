using System;
using UnityEngine;

/// <summary>
/// One cardboard box in the tower at the door. A pooled object: DoorController binds it to a Parcel when one arrives and
/// recycles it when the parcel is opened. Clicking it (Door view only: it carries a ViewGate) reports to the controller.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ParcelView : DeskObject
{
    public Parcel Data { get; private set; }
    public event Action<ParcelView> Clicked;

    public void Bind(Parcel parcel) => Data = parcel;

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
