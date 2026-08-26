using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PointerReceiver))]
[RequireComponent(typeof(Collider2D))]
public class TicketStack : MonoBehaviour, IPawnable
{
    [SerializeField] private ScratchTicket[] ticketPool;
    [SerializeField] private Vector2 spawnRangeMin = new Vector2(-4f, -3f);
    [SerializeField] private Vector2 spawnRangeMax = new Vector2(4f, 3f);
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    public int PawnValue => pawnValue;
    
    private PointerReceiver receiver;
    
    private void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
        
        foreach (var ticket in ticketPool)
            ticket.gameObject.SetActive(false); 
    }

    private void OnEnable() => receiver.ClickDown += HandleClick;
    private void OnDisable() => receiver.ClickDown -= HandleClick;

    private void HandleClick(Vector2 worldPos)
    {
        ScratchTicket ticket = FindFreeTicket();
        if(ticket ==  null) return;

        // Activate BEFORE purchasing: the pool starts inactive (see Awake),
        // so each ticket's own Awake() - and its child ScratchWindows' - has
        // never run yet. TryPurchase()/RollSymbols() calls Populate() on
        // those windows; calling it while still inactive means Populate()
        // touches fields ScratchWindow.Awake() hasn't allocated, which is
        // exactly the "works standalone, breaks through ScratchTicket" bug.
        ticket.transform.position = GetRandomSpawnPosition();
        ticket.gameObject.SetActive(true);

        if (!ticket.TryPurchase())
            ticket.gameObject.SetActive(false); // couldn't afford it - back to the pool
    }
    
    private ScratchTicket FindFreeTicket()
    {
        foreach (var t in ticketPool)
            if (!t.gameObject.activeSelf) return t;
        return null;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        float x = (float)(spawnRangeMin.x + RngService.Instance.Random.NextDouble() * (spawnRangeMax.x - spawnRangeMin.x));
        float y = (float)(spawnRangeMin.y + RngService.Instance.Random.NextDouble() * (spawnRangeMax.y - spawnRangeMin.y));
        return new Vector2(x, y);
    }
    
    public void OnPawned() => Destroy(gameObject);
}
