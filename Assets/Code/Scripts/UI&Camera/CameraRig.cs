using System;
using DG.Tweening;
using UnityEngine;

public enum CameraView { Room, Desk, Door }

public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance { get; private set; }
    
    [SerializeField] private Camera cam;
    [SerializeField] private Transform roomAnchor;
    [SerializeField] private Transform deskAnchor;
    [SerializeField] private Transform doorAnchor;

    public CameraView CurrentView { get; private set; } = CameraView.Room;
    public bool IsMoving { get; private set; }
    public event Action<CameraView> ViewChanged;

    private Tween _tween;
    private bool _blocking;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        var config = GameManager.Instance.GameConfig;
        Place(roomAnchor, config.roomCameraSize);
    }
    
    private void OnDisable()
    {
        if (_tween != null) _tween.Kill();
        EndBlock();
    }

    public bool GoTo(CameraView view)
    {
        if (IsMoving || view == CurrentView) return false;
        
        var config = GameManager.Instance.GameConfig;
        Transform anchor = AnchorOf(view);
        if (anchor == null) return false;
        
        Vector3 target = new Vector3(anchor.position.x, anchor.position.y, cam.transform.position.z);
        
        IsMoving = true;
        BeginBlock();
        
        _tween = DOTween.Sequence()
            .Join(cam.transform.DOMove(target, config.cameraFocusSeconds))
            .Join(cam.DOOrthoSize(SizeOf(view, config), config.cameraFocusSeconds))
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                CurrentView = view;
                IsMoving = false;
                EndBlock();
                ViewChanged?.Invoke(view);
            });
        return true;
    }
    
    private void Place(Transform anchor, float size)
    {
        if (anchor != null)
            cam.transform.position = new Vector3(anchor.position.x, anchor.position.y, cam.transform.position.z);
        cam.orthographicSize = size;
    }
    
    private Transform AnchorOf(CameraView view)
    {
        switch (view)
        {
            case CameraView.Desk: return deskAnchor;
            case CameraView.Door: return doorAnchor;
            default: return roomAnchor;
        }
    }
    
    private static float SizeOf(CameraView view, GameConfig config)
    {
        switch (view)
        {
            case CameraView.Desk: return config.deskCameraSize;
            case CameraView.Door: return config.doorCameraSize;
            default: return config.roomCameraSize;
        }
    }

    private void BeginBlock()
    {
        if (_blocking) return;
        _blocking = true;
        InputGate.Add();
    }

    private void EndBlock()
    {
        if (!_blocking) return;
        _blocking = false;
        IsMoving = false;
        InputGate.Remove();
    }
}
