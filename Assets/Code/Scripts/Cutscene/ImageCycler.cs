using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ImageCycler : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private float switchInterval = 0.08f;
    [SerializeField] private float cycleDuration = 2f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip laughingClip;

    private Coroutine _routine;

    private void OnEnable() => Begin();

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
        PlayLaughingClip();
        _routine = null;
    }

    public void PlayLaughingClip()
    {
        if (audioSource == null || laughingClip == null) return;
        audioSource.PlayOneShot(laughingClip);
    }
}
