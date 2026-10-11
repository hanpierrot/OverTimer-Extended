using UnityEngine;

public enum JunkTier { Small, Medium, Large }

[RequireComponent(typeof(Collider2D))]
public class JunkItem : MonoBehaviour, IPawnable
{
    [SerializeField] private JunkTier tier = JunkTier.Small;

    public int PawnValue
    {
        get
        {
            var config = GameManager.Instance.GameConfig;
            switch (tier)
            {
                case JunkTier.Medium: return config.junkMediumPawnValue;
                case JunkTier.Large: return config.junkLargePawnValue;
                default: return config.junkSmallPawnValue;
            }
        }
    }
    
    public bool CanBePawned => true;

    public void OnPawned() => Destroy(gameObject);
}
