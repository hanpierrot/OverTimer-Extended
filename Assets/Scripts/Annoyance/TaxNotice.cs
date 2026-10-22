using UnityEngine;

/// <summary>
/// The "Taxes" desk object - the paper copy of the current ITRS bill (the other copy is the Taxes phone app).
/// It appears when a bill is issued and nags until the player first opens it, then stays on the desk until the bill is
/// resolved (paid in the app or here, or penalised), when it hides itself. Clicking opens the Tax panel via a direct call
/// (the panel starts inactive, so it can't subscribe to events itself). Phase 3.2 moves this copy to the mailbox.
/// </summary>
[RequireComponent(typeof(PointerReceiver))]
[RequireComponent(typeof(Collider2D))]
public class TaxNotice : DeskObject
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip nastySound;
    [SerializeField] private float nagInterval = 2f;
    [SerializeField] private Tax taxPanel;
    [SerializeField] private GameObject noticeVisual;

    public bool HasPendingBill { get; private set; }

    private float nagTimer;
    private bool _opened;

    private void Start()
    {
        // Not OnEnable: Awake order between Taxes and ITRSService isn't guaranteed,
        // so ITRSService.Instance can still be null there. Start() only runs after
        // every object's Awake has completed, so the singleton is always ready here.
        ITRSService.Instance.OnAssessmentIssued += HandleAssessmentIssued;
        ITRSService.Instance.OnAssessmentResolved += HandleAssessmentResolved;
    }

    private void OnDestroy()
    {
        if (ITRSService.Instance == null) return;
        ITRSService.Instance.OnAssessmentIssued -= HandleAssessmentIssued;
        ITRSService.Instance.OnAssessmentResolved -= HandleAssessmentResolved;
    }

    private void HandleAssessmentIssued(ITRSService.Assessment assessment)
    {
        HasPendingBill = true;
        _opened = false;
        nagTimer = 0f;
        if (noticeVisual) noticeVisual.SetActive(true);
        PlayNastySound();
        if (HintService.Instance != null) HintService.Instance.ShowOnce(HintId.MailTrayFirstBill);
    }

    private void HandleAssessmentResolved()
    {
        HasPendingBill = false;
        if (noticeVisual) noticeVisual.SetActive(false);   // recalled: the other copy settled it
    }

    private void Update()
    {
        if (!HasPendingBill || _opened) return;

        nagTimer -= Time.deltaTime;
        if (nagTimer <= 0f)
        {
            PlayNastySound();
            nagTimer = nagInterval;
        }
    }

    private void PlayNastySound()
    {
        if (audioSource != null && nastySound != null)
            audioSource.PlayOneShot(nastySound);
    }

    protected override void OnClicked(Vector2 worldPos)
    {
        if (!HasPendingBill) return;

        _opened = true;
        if (taxPanel != null) taxPanel.Show();
    }

    public void OnPawned() => gameObject.SetActive(false);
}
