using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PointerReceiver))]
public class Pawn : MonoBehaviour
{
    [SerializeField] private LayerMask pawnableLayers = ~0;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pawnSound;

    private PointerReceiver receiver;
    
    private void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
        GetComponent<Collider2D>().isTrigger = true;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnEnable()
    {
        receiver.DragStart += HandleDragStart;
        receiver.DragUpdate += HandleDragUpdate;
        receiver.DragEnd += HandleDragEnd;
    }

    private void OnDisable()
    {
        receiver.DragStart -= HandleDragStart;
        receiver.DragUpdate -= HandleDragUpdate;
        receiver.DragEnd -= HandleDragEnd;
    }

    private void HandleDragStart(Vector2 worldPos)
    {
        if (audioSource == null || pawnSound == null) return;

        audioSource.clip = pawnSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void HandleDragUpdate(Vector2 worldPos)
    {
        transform.position = worldPos;
    }

    private void HandleDragEnd(Vector2 worldPos)
    {
        if (audioSource != null) audioSource.Stop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & pawnableLayers) == 0) return;

        if (other.TryGetComponent(out IPawnable pawnable))
        {
            MoneyService.Instance.Add(pawnable.PawnValue, "pawn");
            Destroy(other.gameObject);
        }
    }
}
