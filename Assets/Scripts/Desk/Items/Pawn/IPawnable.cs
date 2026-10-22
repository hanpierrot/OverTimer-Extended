using UnityEngine;

public interface IPawnable
{
    int PawnValue { get; }
    bool CanBePawned {get; }
    
    void OnPawned();
}
