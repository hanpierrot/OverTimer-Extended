using UnityEngine;

[RequireComponent(typeof(PointerReceiver), typeof(ViewGate))]
public class CameraViewButton : MonoBehaviour
{
    [SerializeField] private CameraView target;
    
    public CameraView Target => target;

    private PointerReceiver _receiver;

    private void Awake() => _receiver = GetComponent<PointerReceiver>();

    private void OnEnable() => _receiver.ClickDown += HandleClick;

    private void OnDisable() => _receiver.ClickDown -= HandleClick;

    private void HandleClick(Vector2 worldPos)
    {
        if (CameraRig.Instance != null) CameraRig.Instance.GoTo(target);
    }
}
