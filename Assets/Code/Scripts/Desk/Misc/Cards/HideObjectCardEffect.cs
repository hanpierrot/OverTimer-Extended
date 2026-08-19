using System.Collections;
using UnityEngine;

public class HideObjectCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private GameObject target;
    [SerializeField] private float duration = 4f;
    
    private Coroutine hideRoutine;

    public void Apply()
    {
        if (target == null) return;

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        
        hideRoutine = StartCoroutine(HideRoutine());
    }
    
    private IEnumerator HideRoutine()
    {
        target.SetActive(false);
        yield return new WaitForSeconds(duration);
        target.SetActive(true);
        hideRoutine = null;
    }
}
