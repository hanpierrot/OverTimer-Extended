using UnityEngine;

[CreateAssetMenu(fileName = "CreepyJester", menuName = "Overtime/Cards/Effects/Creepy Jester")]
public class CreepyJesterCardSO : EffectCardSO
{
    [SerializeField] private Sprite[] artPool;
    [SerializeField] private float intervalSeconds = 20f;

    public override void OnPlaced(CardItem card)
    {
        int index = 0;

        if (artPool.Length > 0) card.SetArt(artPool[0]);
        
        card.ScheduleRepeating(() =>
        {
            index++;
            card.SetArt(artPool[index]);

            if (index >= artPool.Length - 1)
            {
                card.CancelRepeating();
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                GameManager.Instance.GameOver(EndReason.CreepyJesterLoss);
            }
        }, intervalSeconds);
    }

    public override void OnRemoved(CardItem card)
    {
        card.CancelRepeating();
    }
}
