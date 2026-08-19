using UnityEngine;

/// <summary>
/// The "Taxes" desk object - the Mail Tray of DESIGN.md §4/§5.6. Purely a notification
/// layer: when ITRSService issues an assessment it does NOT auto-resolve (see
/// ITRSService.Pay/Ignore) - it waits for the player to click this object. Until then,
/// this nags with a repeating sound. On click it hands the assessment straight to the
/// Tax panel via a direct call (not an event) - the panel starts inactive in the scene,
/// so it can't reliably subscribe to an event before the first notice fires.
/// </summary>
[RequireComponent(typeof(PointerReceiver))]
[RequireComponent(typeof(Collider2D))]
public class TaxNotice : DeskObject, IPawnable
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip nastySound;
    [SerializeField] private float nagInterval = 2f;
    [SerializeField] private Tax taxPanel;
    [SerializeField] private GameObject noticeVisual;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    public int PawnValue => pawnValue;

    public bool HasPendingBill { get; private set; }

    private float nagTimer;
    private ITRSService.Assessment pendingAssessment;

    private void Start()
    {
        // Not OnEnable: Awake order between Taxes and ITRSService isn't guaranteed,
        // so ITRSService.Instance can still be null there. Start() only runs after
        // every object's Awake has completed, so the singleton is always ready here.
        ITRSService.Instance.OnAssessmentIssued += HandleAssessmentIssued;
    }

    private void OnDestroy()
    {
        if (ITRSService.Instance != null) ITRSService.Instance.OnAssessmentIssued -= HandleAssessmentIssued;
    }

    private void HandleAssessmentIssued(ITRSService.Assessment assessment)
    {
        pendingAssessment = assessment;
        HasPendingBill = true;
        nagTimer = 0f;
        if (noticeVisual) noticeVisual.SetActive(true);
        PlayNastySound();
    }

    private void Update()
    {
        if (!HasPendingBill) return;

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

        HasPendingBill = false;
        if (noticeVisual) noticeVisual.SetActive(false);
        if (taxPanel != null) taxPanel.Show(pendingAssessment);
    }
}
