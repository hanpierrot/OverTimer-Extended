using UnityEngine;

[CreateAssetMenu(fileName = "TicketSymbols", menuName = "Overtime/Ticket Symbol Config")]
public class TicketSymbolConfig : ScriptableObject
{
    public ScratchTicket.SymbolEntry[] symbolPool;
}
