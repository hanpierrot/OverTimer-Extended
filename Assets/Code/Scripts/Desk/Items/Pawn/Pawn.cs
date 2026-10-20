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
    private Vector3 _initialPosition;
    
    private void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
        GetComponent<Collider2D>().isTrigger = true;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        _initialPosition = transform.position;
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
        transform.position = _initialPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & pawnableLayers) == 0) return;

        if (other.TryGetComponent(out IPawnable pawnable))
        {
            if (!pawnable.CanBePawned)
            {
                HintService.Instance?.Show(HintId.CannotPawn);
                return;
            }
            
            // A catalog item (it has a ShopItemBinding): the catalog decides. A starting item that isn't pawnable, or an item
            // the player does not own, pays nothing. Objects without a binding (cards, junk) keep their own PawnValue.
            var binding = other.GetComponentInParent<ShopItemBinding>(true);
            if (binding != null && ShopService.Instance != null)
            {
                var shop = ShopService.Instance;
                int value = shop.Ownership.Has(binding.ItemId) ? shop.Ownership.MarkPawned(binding.ItemId) : 0;
                if (value <= 0)
                {
                    if (HintService.Instance != null) HintService.Instance.Show(HintId.CannotPawn);
                    return;
                }
                MoneyService.Instance.Add(value, "pawn");
                pawnable.OnPawned();
                TutorialService.TryShow("first_pawn");
                return;
            }

            MoneyService.Instance.Add(pawnable.PawnValue, "pawn");
            pawnable.OnPawned();
            TutorialService.TryShow("first_pawn");
        }
    }
}
