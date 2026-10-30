using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public static DoorController Instance { get; private set; }

    [Header("Parcel tower (left of the door)")]
    [SerializeField] private ParcelView parcelPrefab;
    [SerializeField] private Transform towerBase;
    [Tooltip("Sideways wobble between stacked boxes, in world units.")]
    [SerializeField] private float towerWobble = 0.25f;
    [SerializeField] private float shakeSeconds = 0.25f;
    [SerializeField] private float shakeStrength = 0.18f;

    [Header("Empty boxes (Room view)")]
    [SerializeField] private EmptyBox emptyBoxPrefab;
    [Tooltip("Two opposite corners of the area where empty boxes can lie (world units, Room view). Any two diagonal points work. Defaults: the strip between the Door zone and the Desk zone, 1.5 inside each edge.")]
    [SerializeField] private Vector2 emptyBoxCornerA = new Vector2(-5.3f, -4.45f);
    [SerializeField] private Vector2 emptyBoxCornerB = new Vector2(7.78f, 2.5f);
    [Tooltip("Scene view only: draw where empty boxes can appear (green), the bottom edge boxes drop to (yellow) and the boxes now lying there (cyan).")]
    [SerializeField] private bool drawEmptyBoxGizmos = true;

    [Header("Effects and sound (all optional)")]
    [SerializeField] private PoofEffect poofPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorbellSound;
    [SerializeField] private AudioClip boxHitSound;
    [SerializeField] private AudioClip boxOpenSound;

    [Header("Letters (right of the door)")]
    [SerializeField] private LetterView letterPrefab;
    [Tooltip("Centre of the scattered pile.")]
    [SerializeField] private Transform letterCenter;
    [Tooltip("How far letters may scatter from the centre (half width, half height), in world units.")]
    [SerializeField] private Vector2 letterSpread = new Vector2(2.2f, 1.1f);
    [SerializeField] private float letterTilt = 25f;
    [Tooltip("The Tax paper copy (the panel under Canvas) that a tax letter opens.")]
    [SerializeField] private Tax taxPanel;
    [Tooltip("Placeholder jokes for the junk flyers.")]
    [SerializeField] private string[] junkLines =
    {
        "You may already be a winner!",
        "Act now: your car's extended warranty is expiring!",
        "Hot singles in your area want to sell you a mattress.",
        "Congratulations! You have been selected to receive this flyer.",
        "Pizza, 2-for-1, valid in a town you have never visited.",
    };

    public DoorDepot Depot { get; private set; }
    public LetterPile Letters { get; private set; }

    private readonly List<ParcelView> _pool = new List<ParcelView>();
    private readonly List<PoofEffect> _poofs = new List<PoofEffect>();
    private readonly List<ShopItemBinding> _pendingReveal = new List<ShopItemBinding>();
    private float _boxHeight = 2f;

    private readonly List<EmptyBox> _emptyBoxes = new List<EmptyBox>();
    private readonly List<EmptyBox> _emptyBoxAge = new List<EmptyBox>(); 
    private Vector3 _emptyBoxScale = Vector3.one;
    private bool _hasEmptyBoxArea;
    
    public EmptyBoxArea EmptyBoxArea { get; private set; }
    public bool HasEmptyBoxArea => _hasEmptyBoxArea;
    public IReadOnlyList<EmptyBox> EmptyBoxes => _emptyBoxes;

    private readonly List<LetterView> _letterPool = new List<LetterView>();
    private JunkMailTimer _junkTimer;
    private float _taxLetterLeft = -1f;
    private float _taxLetterDelay;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        var config = GameManager.Instance.GameConfig;
        Depot = new DoorDepot(config.doorParcelSlots, config.parcelClicksToOpen);
        Depot.Added += HandleParcelAdded;
        Depot.Opened += HandleParcelOpened;
        StartLetters(config);

        if (parcelPrefab == null) { Debug.LogWarning("[Door] No parcel prefab assigned: parcels will not show."); return; }

        for (int i = 0; i < Depot.Capacity; i++)
        {
            var view = Instantiate(parcelPrefab, transform);
            view.gameObject.name = "Parcel" + i;
            view.Clicked += HandleParcelClicked;
            view.gameObject.SetActive(false);
            _pool.Add(view);
        }
        var sr = _pool.Count > 0 ? _pool[0].GetComponentInChildren<SpriteRenderer>() : null;
        if (sr != null) _boxHeight = sr.bounds.size.y;

        if (poofPrefab != null)
            for (int i = 0; i < 3; i++)
            {
                var puff = Instantiate(poofPrefab, transform);
                puff.gameObject.SetActive(false);
                _poofs.Add(puff);
            }

        BuildEmptyBoxes(config);

        if (CameraRig.Instance != null) CameraRig.Instance.ViewChanged += HandleViewChanged;
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        foreach (var box in _emptyBoxes) if (box != null) box.Dropped -= HandleEmptyBoxDropped;
        if (CameraRig.Instance != null) CameraRig.Instance.ViewChanged -= HandleViewChanged;
        if (ITRSService.Instance != null)
        {
            ITRSService.Instance.OnAssessmentIssued -= HandleBillIssued;
            ITRSService.Instance.OnAssessmentResolved -= HandleBillResolved;
        }
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Letters == null || GameManager.Instance.IsGameOver) return;
        float dt = ClockService.Instance.GameDelta;
        if (dt <= 0f) return;

        if (_taxLetterLeft >= 0f)
        {
            _taxLetterLeft -= dt;
            if (_taxLetterLeft < 0f) DropTaxLetter();
        }
        if (_junkTimer != null && _junkTimer.Tick(dt)) DropJunk();
    }

    public void Receive(Order order)
    {
        if (order.leaveOpen)
        {
            if (ShopService.Instance != null) ShopService.Instance.Deliver(order.itemIds);
        }
        else Depot.Receive(order.id, order.itemIds);

        if (audioSource != null && doorbellSound != null) audioSource.PlayOneShot(doorbellSound);
        if (!order.leaveOpen && HintService.Instance != null) HintService.Instance.ShowOnce(HintId.PackageArrived);
    }

    private void HandleParcelAdded(Parcel parcel)
    {
        var view = _pool.Find(v => !v.gameObject.activeSelf);
        if (view == null) return;
        view.Bind(parcel);
        view.gameObject.SetActive(true);

        Vector3 target = SpotOf(Depot.IndexOf(parcel));
        view.transform.position = target + Vector3.up * 5f; 
        view.transform.DOMove(target, 0.35f).SetEase(Ease.OutBounce);
        AudioService.Play(SfxId.ParcelSlide);
    }

    private void HandleParcelClicked(ParcelView view)
    {
        var parcel = view.Data;
        if (parcel == null) return;

        view.transform.DOKill();
        Vector3 spot = SpotOf(Depot.IndexOf(parcel));
        view.transform.position = spot;
        if (!Depot.Click(parcel))
        {
            view.transform.DOShakePosition(shakeSeconds, new Vector3(shakeStrength, shakeStrength * 0.4f, 0f), 24, 90f, false, true);
            if (audioSource != null && boxHitSound != null) audioSource.PlayOneShot(boxHitSound);
        }
    }

    private void HandleParcelOpened(Parcel parcel)
    {
        ParcelView view = _pool.Find(v => v.Data == parcel);
        if (view != null)
        {
            Poof(view.transform.position);
            view.transform.DOKill();
            view.Unbind();          
        }
        if (audioSource != null && boxOpenSound != null) audioSource.PlayOneShot(boxOpenSound);

        SpawnEmptyBox();
        if (ShopService.Instance != null) ShopService.Instance.Deliver(parcel.itemIds);

        for (int i = 0; i < Depot.Tower.Count; i++)
        {
            var v = _pool.Find(x => x.Data == Depot.Tower[i]);
            if (v != null) v.transform.DOMove(SpotOf(i), 0.2f).SetEase(Ease.OutQuad);
        }
    }

    private Vector3 SpotOf(int index)
    {
        Vector3 b = towerBase != null ? towerBase.position : transform.position;
        float wobble = (index % 2 == 0 ? -0.5f : 0.5f) * towerWobble;
        return new Vector3(b.x + wobble, b.y + index * _boxHeight * 0.98f, b.z);
    }

    private void BuildEmptyBoxes(GameConfig config)
    {
        if (emptyBoxPrefab == null) { Debug.LogWarning("[Door] No empty box prefab assigned: unboxed parcels will leave no box."); return; }

        EmptyBoxArea = new EmptyBoxArea(emptyBoxCornerA, emptyBoxCornerB);
        _hasEmptyBoxArea = true;
        _emptyBoxScale = emptyBoxPrefab.transform.localScale;

        for (int i = 0; i < Mathf.Max(1, config.doorEmptyBoxSlots); i++)
        {
            var box = Instantiate(emptyBoxPrefab, transform);
            box.gameObject.name = "EmptyBox" + i;
            box.Dropped += HandleEmptyBoxDropped;
            box.gameObject.SetActive(false);
            _emptyBoxes.Add(box);
        }
    }

    /// <summary>Puts an empty box somewhere in the area. Uses a free one, else recycles the oldest (the unboxing flow calls this; tests and the cheat menu can too).</summary>
    public EmptyBox SpawnEmptyBox()
    {
        if (!_hasEmptyBoxArea || _emptyBoxes.Count == 0) return null;

        var box = _emptyBoxes.Find(b => !b.gameObject.activeSelf);
        if (box == null) box = _emptyBoxAge.Find(b => b.gameObject.activeSelf); 
        if (box == null) box = _emptyBoxes[0];
        _emptyBoxAge.Remove(box);
        _emptyBoxAge.Add(box);

        box.transform.DOKill();
        box.gameObject.SetActive(true);
        Vector2 point = EmptyBoxArea.RandomPoint(RngService.Instance.Random);
        box.transform.SetPositionAndRotation(new Vector3(point.x, point.y, box.transform.position.z), Quaternion.identity);
        box.transform.localScale = _emptyBoxScale * 0.2f;
        box.transform.DOScale(_emptyBoxScale, 0.25f).SetEase(Ease.OutBack).SetTarget(box.transform);

        if (HintService.Instance != null) HintService.Instance.ShowOnce(HintId.EmptyBoxFirst);
        return box;
    }

    private void HandleEmptyBoxDropped(EmptyBox box)
    {
        Vector2 at = box.transform.position;
        if (EmptyBoxArea.Contains(at)) return;
        Vector2 landing = EmptyBoxArea.DropPoint(at);                 // down to the bottom edge, x clamped into the area
        box.transform.DOKill();
        box.transform.DOMove(new Vector3(landing.x, landing.y, box.transform.position.z), 0.3f).SetEase(Ease.OutQuad).SetTarget(box.transform);
    }

    public void RevealOnDesk(ShopItemBinding binding)
    {
        if (binding == null) return;
        if (CameraRig.Instance != null && CameraRig.Instance.CurrentView != CameraView.Desk)
        {
            _pendingReveal.Add(binding);
            return;
        }
        Reveal(binding, 0f);
    }

    private void HandleViewChanged(CameraView view)
    {
        if (view == CameraView.Door && Depot != null && Depot.Tower.Count > 0 && HintService.Instance != null)
            HintService.Instance.ShowOnce(HintId.ParcelOpen);
        if (view != CameraView.Desk || _pendingReveal.Count == 0) return;
        float delay = 0.15f;
        foreach (var b in _pendingReveal) { Reveal(b, delay); delay += 0.3f; }
        _pendingReveal.Clear();
    }

    private void Reveal(ShopItemBinding binding, float delay)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            if (binding == null) return;
            Poof(binding.transform.position);
            binding.gameObject.SetActive(true);
            binding.transform.DOPunchScale(Vector3.one * 0.25f, 0.45f, 6, 0.5f).SetTarget(this);
            if (HintService.Instance != null) HintService.Instance.ShowOnce(HintId.ShopPurchased);
        }).SetTarget(this);
    }

    private void Poof(Vector3 position)
    {
        if (poofPrefab == null) return;
        var puff = _poofs.Find(p => !p.gameObject.activeSelf);
        if (puff == null) puff = _poofs[0]; 
        puff.Play(position);
    }

    private void StartLetters(GameConfig config)
    {
        _taxLetterDelay = config.taxLetterDelay;
        Letters = new LetterPile(config.letterSlots);
        _junkTimer = new JunkMailTimer(RngService.Instance.Random, config.junkMailIntervalMin, config.junkMailIntervalMax, config.junkMailGrace);
        Letters.Added += HandleLetterAdded;
        Letters.Removed += HandleLetterRemoved;

        if (letterPrefab != null)
            for (int i = 0; i < Letters.Capacity; i++)
            {
                var view = Instantiate(letterPrefab, transform);
                view.gameObject.name = "Letter" + i;
                view.Clicked += HandleLetterClicked;
                view.gameObject.SetActive(false);
                _letterPool.Add(view);
            }
        else Debug.LogWarning("[Door] No letter prefab assigned: letters will not show.");

        if (ITRSService.Instance != null)
        {
            ITRSService.Instance.OnAssessmentIssued += HandleBillIssued;
            ITRSService.Instance.OnAssessmentResolved += HandleBillResolved;
        }
    }

    private void HandleBillIssued(ITRSService.Assessment assessment) => _taxLetterLeft = _taxLetterDelay;

    private void HandleBillResolved()
    {
        _taxLetterLeft = -1f;                       
        var letter = Letters.FirstOf(LetterKind.Tax);
        if (letter != null) Letters.Remove(letter);    
    }

    private void DropTaxLetter()
    {
        if (ITRSService.Instance == null || !ITRSService.Instance.HasPendingAssessment) return;
        if (Letters.FirstOf(LetterKind.Tax) != null) return;
        if (Letters.Add(LetterKind.Tax, "") == null) return;
        Ring();
        if (HintService.Instance != null) HintService.Instance.ShowOnce(HintId.MailTrayFirstBill);
    }

    public void DropJunk()
    {
        var rng = RngService.Instance.Random;
        string line = junkLines != null && junkLines.Length > 0 ? junkLines[rng.Next(junkLines.Length)] : "You may already be a winner!";
        if (Letters.Add(LetterKind.Junk, line) != null) Ring();
    }

    private void Ring()
    {
        if (audioSource != null && doorbellSound != null) audioSource.PlayOneShot(doorbellSound);
    }

    private void HandleLetterAdded(Letter letter)
    {
        var view = _letterPool.Find(v => !v.gameObject.activeSelf);
        if (view == null) return;
        view.Bind(letter);

        var rng = RngService.Instance.Random;
        Vector3 c = letterCenter != null ? letterCenter.position : transform.position;
        float x = c.x + ((float)rng.NextDouble() * 2f - 1f) * letterSpread.x;
        float y = c.y + ((float)rng.NextDouble() * 2f - 1f) * letterSpread.y;
        float tilt = ((float)rng.NextDouble() * 2f - 1f) * letterTilt;
        view.transform.SetPositionAndRotation(new Vector3(x, y, c.z), Quaternion.Euler(0f, 0f, tilt));
        view.gameObject.SetActive(true);

        foreach (var sr in view.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder = 4 + letter.id % 100 * 2 + (sr.transform == view.transform ? 0 : 1);

        Vector3 full = letterPrefab.transform.localScale;
        view.transform.localScale = full * 0.2f;
        view.transform.DOScale(full, 0.25f).SetEase(Ease.OutBack);
    }

    private void HandleLetterRemoved(Letter letter)
    {
        var view = _letterPool.Find(v => v.Data == letter);
        if (view == null) return;
        Poof(view.transform.position);
        view.transform.DOKill();
        view.Unbind();
    }

    private void HandleLetterClicked(LetterView view)
    {
        var letter = view.Data;
        if (letter == null) return;

        if (letter.kind == LetterKind.Tax)
        {
            if (taxPanel != null && ITRSService.Instance != null && ITRSService.Instance.HasPendingAssessment) taxPanel.Show();
            return;
        }

        if (HintService.Instance != null) HintService.Instance.Show(HintId.JunkFlyer, letter.text);
        Letters.Remove(letter);   
    }

    private void OnDrawGizmos()
    {
        if (!drawEmptyBoxGizmos) return;

        // Play mode uses the built area; in Edit mode the corners are read live so moving them updates the picture.
        EmptyBoxArea area = _hasEmptyBoxArea ? EmptyBoxArea : new EmptyBoxArea(emptyBoxCornerA, emptyBoxCornerB);

        Vector3 centre = new Vector3((area.Min.x + area.Max.x) * 0.5f, (area.Min.y + area.Max.y) * 0.5f, 0f);
        Vector3 size = new Vector3(area.Max.x - area.Min.x, area.Max.y - area.Min.y, 0.1f);
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.15f);
        Gizmos.DrawCube(centre, size);
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 1f);
        Gizmos.DrawWireCube(centre, size);

        Gizmos.color = Color.yellow;   
        Gizmos.DrawLine(new Vector3(area.Min.x, area.Min.y, 0f), new Vector3(area.Max.x, area.Min.y, 0f));

        Gizmos.color = Color.cyan;
        foreach (var box in _emptyBoxes)
            if (box != null && box.gameObject.activeSelf) Gizmos.DrawWireCube(box.transform.position, new Vector3(2.5f, 2.5f, 0.1f));
    }
}
