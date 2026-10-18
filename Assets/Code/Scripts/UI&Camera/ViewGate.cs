using System;
using UnityEngine;

public class ViewGate : MonoBehaviour
{
    [SerializeField] private CameraView[] allowedViews = { CameraView.Desk };

    public bool Allows(CameraView view) => Array.IndexOf(allowedViews, view) >= 0;

    public static bool IsAllowed(Collider2D collider, CameraView view)
    {
        if (collider.TryGetComponent(out ViewGate gate)) return gate.Allows(view);
        return view == CameraView.Desk;
    }
}
