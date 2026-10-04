using System;
using TMPro;
using UnityEngine;

public class ImageCycler : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private int fromSeconds = 359;
    [SerializeField] private int toSeconds = 360;
    [SerializeField] private string timeFormat = "mm\\:ss";
    
    [SerializeField] private float progress;
    
    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (target == null) return;
        
        int steps = Mathf.Abs(toSeconds - fromSeconds);
        int sign = toSeconds >= fromSeconds ? 1 : -1;
        int index = Mathf.Min(steps, Mathf.FloorToInt(Mathf.Clamp01(progress) * (steps + 1)));
        
        target.text = TimeSpan.FromSeconds(fromSeconds + sign * index).ToString(timeFormat);
    }
}
