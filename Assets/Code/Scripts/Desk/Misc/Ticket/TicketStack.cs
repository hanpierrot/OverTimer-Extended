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
    private int ticketRemain;
    
    private void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
        ticketRemain = ticketPool.Length;
        
        foreach (var ticket in ticketPool)
            ticket.gameObject.SetActive(false); 
    }

    private void OnEnable() => receiver.ClickDown += HandleClick;
    private void OnDisable() => receiver.ClickDown -= HandleClick;

    private void HandleClick(Vector2 worldPos)
    {
        if (ticketRemain <= 0) return;
        
        ScratchTicket ticket = ticketPool[ticketPool.Length - ticketRemain];
        ticketRemain--;

        ticket.transform.position = GetRandomSpawnPosition();
        ticket.gameObject.SetActive(true);
        
        if (!ticket.TryPurchase())
            ticket.gameObject.SetActive(false);
    }

    private Vector2 GetRandomSpawnPosition()
    {
        float x = (float)(spawnRangeMin.x + RngService.Instance.Random.NextDouble() * (spawnRangeMax.x - spawnRangeMin.x));
        float y = (float)(spawnRangeMin.y + RngService.Instance.Random.NextDouble() * (spawnRangeMax.y - spawnRangeMin.y));
        return new Vector2(x, y);
    }
}
