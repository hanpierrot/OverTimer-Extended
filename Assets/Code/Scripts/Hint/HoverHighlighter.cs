using UnityEngine;

public class HoverHighlighter : MonoBehaviour
{
    [SerializeField] private float padding = 0.15f;
    [SerializeField] private float checkInterval = 0.05f;

    [Header("Outline")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = new Color32(0xFF, 0xD1, 0x66, 0xFF);
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private int sortingOrder = 100;

    private LineRenderer _line;
    private Collider2D _current;
    private float _timer;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        if (lineMaterial != null) _line.sharedMaterial = lineMaterial;
        _line.startColor = lineColor;
        _line.endColor = lineColor;
        _line.startWidth = lineWidth;
        _line.endWidth = lineWidth;
        _line.sortingOrder = sortingOrder;
        _line.useWorldSpace = true;
        _line.loop = true;
        _line.positionCount = 4;
        _line.enabled = false;
    }

    private void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer <= 0f)
        {
            _timer = checkInterval;
            _current = Pick();
        }

        Refresh();
    }

    private Collider2D Pick()
    {
        if (InputManager.Instance == null) return null;

        Collider2D c = InputManager.Instance.PeekTarget();
        if (c == null) return null;

        if (c.TryGetComponent(out PointerReceiver receiver) && !receiver.IsInteractable) return null;
        if (c.TryGetComponent(out CameraViewButton button) && button.Target == CameraView.Room) return null;
        return c;
    }

    private void Refresh()
    {
        if (_current == null || !_current.gameObject.activeInHierarchy)
        {
            _line.enabled = false;
            return;
        }

        Bounds b = _current.bounds;
        float p = padding;
        _line.SetPosition(0, new Vector3(b.min.x - p, b.min.y - p, 0f));
        _line.SetPosition(1, new Vector3(b.min.x - p, b.max.y + p, 0f));
        _line.SetPosition(2, new Vector3(b.max.x + p, b.max.y + p, 0f));
        _line.SetPosition(3, new Vector3(b.max.x + p, b.min.y - p, 0f));
        _line.enabled = true;
    }
}
