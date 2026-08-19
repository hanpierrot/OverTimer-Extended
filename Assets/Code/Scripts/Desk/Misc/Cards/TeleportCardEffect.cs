using System.Collections;
using UnityEngine;

public class TeleportCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private GameObject target;
    [SerializeField] private Vector2 rangeMin = new Vector2(-4f, -3f);
    [SerializeField] private Vector2 rangeMax = new Vector2(4f, 3f);
    [SerializeField] private float returnDelay = 3f;
    
    private Coroutine returnRoutine;
    
    public void Apply()
    {
        if (target == null) return;
        
        if (returnRoutine != null) StopCoroutine(returnRoutine);

        Vector3 originalPos = target.transform.position;
        
        Vector2 randomPos = new Vector2(
            Random.Range(rangeMin.x, rangeMax.x),
            Random.Range(rangeMin.y, rangeMax.y));

        target.transform.position = randomPos;
        
        returnRoutine = StartCoroutine(ReturnAfterDelay(originalPos));
    }
    
    private IEnumerator ReturnAfterDelay(Vector3 originalPos)
    {
        yield return new WaitForSeconds(returnDelay);
        target.transform.position = originalPos;
        returnRoutine = null;
    }
}
