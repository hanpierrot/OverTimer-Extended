using TMPro;
using UnityEngine;

/// <summary>The small clock in a device's top-left corner (the main Timer is blurred behind the devices).</summary>
[RequireComponent(typeof(TMP_Text))]
public class DeviceClock : MonoBehaviour
{
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color urgentColor = new Color(1f, 0.25f, 0.25f);
    [Tooltip("The text turns red under this many seconds.")]
    [SerializeField] private float urgentBelowSeconds = 30f;

    private TMP_Text _text;
    private int _shown = -1;

    private void Awake() => _text = GetComponent<TMP_Text>();

    private void Update()
    {
        var clock = ClockService.Instance;
        if (clock == null) return;

        float seconds = Mathf.Max(0f, clock.CurrentSeconds);
        int whole = Mathf.CeilToInt(seconds);
        if (whole != _shown)
        {
            _shown = whole;
            _text.text = $"{whole / 60:00}:{whole % 60:00}";
        }
        _text.color = seconds < urgentBelowSeconds ? urgentColor : normalColor;
    }
}
