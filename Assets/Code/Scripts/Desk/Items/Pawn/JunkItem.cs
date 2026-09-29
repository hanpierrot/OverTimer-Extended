using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class JunkItem : MonoBehaviour, IPawnable
{
    [SerializeField] private int pawnValue = 5;

    public int PawnValue => pawnValue;
    public bool CanBePawned => true;

    public void OnPawned() => Destroy(gameObject);
}
