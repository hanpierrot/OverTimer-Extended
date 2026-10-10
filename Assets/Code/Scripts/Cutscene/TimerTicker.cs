using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TimerTicker : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private float secondsPerStep = 1f;
    [SerializeField] private float holdAfter = 0.6f;
    [SerializeField] private string timeFormat = "mm\\:ss";

    private Coroutine _routine;

    public void Play(int fromSeconds, int toSeconds, Action onFinished)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Run(fromSeconds, toSeconds, onFinished));
    }

    private IEnumerator Run(int from, int to, Action onFinished)
    {
        int sign = to >= from ? 1 : -1;
        int steps = Mathf.Abs(to - from);

        for (int i = 0; i <= steps; i++)
        {
            target.text = TimeSpan.FromSeconds(from + sign * i).ToString(timeFormat);
            if (i < steps) yield return new WaitForSecondsRealtime(secondsPerStep);
        }

        yield return new WaitForSecondsRealtime(holdAfter);
        _routine = null;
        onFinished?.Invoke();
    }
}
