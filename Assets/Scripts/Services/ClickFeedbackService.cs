using UnityEngine;

public class ClickFeedbackService : MonoBehaviour
{
    public static ClickFeedbackService Instance { get; private set; }
    
    [SerializeField] private ParticleSystem[] pool;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void PlayAt(Vector2 worldPos)
    {
        foreach (var ps in pool)
        {
            if (ps.isPlaying) continue;

            ps.transform.position = worldPos;
            ps.Play();
            return;
        }
    }
}
