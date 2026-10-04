using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimerTicker : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private float switchInterval = 0.08f;
    [SerializeField] private float cycleDuration = 2f;
    
    private Coroutine _routine;

    public void Begin()
    {
        if (target == null || sprites == null || sprites.Length == 0) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        int pool = Mathf.Max(1, sprites.Length);
        int previous = -1;
        float elapsed = 0f;

        while (elapsed < cycleDuration)
        {
            int index;
            do index = Random.Range(0, pool);
            while (pool > 1 && index == previous);
            
            previous = index;
            target.sprite = sprites[index];
            
            yield return new WaitForSecondsRealtime(switchInterval);
            elapsed += switchInterval;
        }
        
        target.sprite = sprites[sprites.Length - 1];
        _routine = null;
    }
}
