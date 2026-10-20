using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// The door end of delivery (DESIGN §5.7). When an order arrives the doorbell rings and, unless the order was shipped
/// "Leave it opened &amp; ready", a cardboard box joins the tower on the left of the door (Door view). The player clicks the
/// box a few times (shake and sound), it springs open in a puff of smoke, the box goes back to the pool for the next
/// delivery, and the items are Delivered: each one appears on the desk in a poof the next time the player looks at the
/// Desk view (or at once if they already are). Nothing here blocks anything and nothing is lost: if the tower is full the
/// order waits for a free spot.
/// Phase 3.2 adds the letters pile (tax letters, junk mail) on the right of the door.
/// </summary>
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

    [Header("Effects and sound (all optional)")]
    [SerializeField] private PoofEffect poofPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorbellSound;
    [SerializeField] private AudioClip boxHitSound;
    [SerializeField] private AudioClip boxOpenSound;

    public DoorDepot Depot { get; private set; }

    private readonly List<ParcelView> _pool = new List<ParcelView>();
    private readonly List<PoofEffect> _poofs = new List<PoofEffect>();
    private readonly List<ShopItemBinding> _pendingReveal = new List<ShopItemBinding>();
    private float _boxHeight = 2f;

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

        if (parcelPrefab == null) { Debug.LogWarning("[Door] No parcel prefab assigned: parcels will not show."); return; }

        // The pool is built once at the start of the run (one box per tower spot) and recycled for every delivery.
        for (int i = 0; i < Depot.Capacity; i++)
        {
            var view = Instantiate(parcelPrefab, transform);
            view.gameObject.name = "Parcel" + i;
            view.Clicked += HandleParcelClicked;
            view.gameObject.SetActive(false);
            _pool.Add(view);
        }
        var sr = _pool.Count > 0 ? _pool[0].GetComponentInChildren<SpriteRenderer>() : null;   // measured on a real instance (a prefab asset has no scene bounds)
        if (sr != null) _boxHeight = sr.bounds.size.y;

        if (poofPrefab != null)
            for (int i = 0; i < 3; i++)
            {
                var puff = Instantiate(poofPrefab, transform);
                puff.gameObject.SetActive(false);
                _poofs.Add(puff);
            }

        if (CameraRig.Instance != null) CameraRig.Instance.ViewChanged += HandleViewChanged;
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        if (CameraRig.Instance != null) CameraRig.Instance.ViewChanged -= HandleViewChanged;
        if (Instance == this) Instance = null;
    }

    // ---- Arrivals ----

    /// <summary>An order reached the door. Rings the bell; a "left opened" order goes straight to the desk.</summary>
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

    // ---- Tower ----

    private void HandleParcelAdded(Parcel parcel)
    {
        var view = _pool.Find(v => !v.gameObject.activeSelf);
        if (view == null) return;                           // cannot happen: the pool is as big as the tower
        view.Bind(parcel);
        view.gameObject.SetActive(true);

        Vector3 target = SpotOf(Depot.IndexOf(parcel));
        view.transform.position = target + Vector3.up * 5f;  // drops in from above
        view.transform.DOMove(target, 0.35f).SetEase(Ease.OutBounce);
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
            view.Unbind();                                  // the box goes back to the pool
        }
        if (audioSource != null && boxOpenSound != null) audioSource.PlayOneShot(boxOpenSound);

        if (ShopService.Instance != null) ShopService.Instance.Deliver(parcel.itemIds);

        // The boxes above slide down.
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

    // ---- Items appearing on the desk ----

    /// <summary>An item was delivered: it pops onto the desk in a poof when the player is looking at the Desk view.</summary>
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
        if (puff == null) puff = _poofs[0];                  // all busy (several items at once): restart the oldest
        puff.Play(position);
    }
}
