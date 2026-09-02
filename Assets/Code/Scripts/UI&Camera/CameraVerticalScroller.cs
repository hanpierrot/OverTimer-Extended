using System;
using UnityEngine;

public class CameraVerticalScroller : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float minY;
    [SerializeField] private float maxY;

    [SerializeField] private HoldButton upButton;
    [SerializeField] private HoldButton downButton;

    private int _direction;
    
    private void Awake()
    {
        upButton.HoldStarted += () => _direction = 1;
        upButton.HoldEnded += () => { if (_direction == 1) _direction = 0; };

        downButton.HoldStarted += () => _direction = -1;
        downButton.HoldEnded += () => { if (_direction == -1) _direction = 0; };
    }

    private void Start() => RefreshButtonVisibility();

    private void Update()
    {
        if (_direction != 0)
        {
            float y = cameraTransform.position.y + _direction * scrollSpeed * Time.deltaTime;
            y = Mathf.Clamp(y, minY, maxY);

            cameraTransform.position = new Vector3(cameraTransform.position.x, y, cameraTransform.position.z);
        }
        
        RefreshButtonVisibility();
    }

    private void RefreshButtonVisibility()
    {
        bool atTop = cameraTransform.position.y >= maxY;
        bool atBottom = cameraTransform.position.y <= minY;

        upButton.gameObject.SetActive(!atTop);
        downButton.gameObject.SetActive(!atBottom);
    }
}
