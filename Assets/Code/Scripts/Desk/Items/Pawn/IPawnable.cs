using UnityEngine;

public interface IPawnable
{
    int PawnValue { get; }
    
    void OnPawned();
}
