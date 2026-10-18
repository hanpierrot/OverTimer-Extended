using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the Laptop and the Phone while they are on screen (plan Phase 1.2, layout from Q4).
///   None   - desk visible, nothing blurred.
///   Laptop - desk blurred; Laptop on screen with the Phone docked over its right edge.
///   Phone  - desk blurred; the Laptop has slid out to the left and only the Phone is on screen.
/// Lives inside the main Canvas (above Timer and the camera arrows, below the end screen, hints and tutorials).
/// Everything animates in unscaled time so it keeps moving while the clock is paused.
/// </summary>
public class DeviceLayer : MonoBehaviour
{
    public enum Mode { None, Laptop, Phone }

    public static DeviceLayer Instance { get; private set; }

    [Header("Parts")]
    [SerializeField] private BlurSnapshot blur;
    [SerializeField] private RectTransform laptopSlot;
    [SerializeField] private RectTransform phoneSlot;
    [SerializeField] private GameObject laptopLockCover;
    [SerializeField] private GameObject laptopClock;
    [SerializeField] private Button phoneBodyButton;
    [SerializeField] private Button phoneCloseButton;

    [Header("Layout (canvas units, 960 x 540 reference)")]
    [SerializeField] private float laptopDockX = -60f;
    [SerializeField] private float laptopHiddenX = -1000f;
    [SerializeField] private float phoneDockX = 372f;
    [Tooltip("Phone-only view: the phone sits left and its app window opens to the right, like the reference UI.")]
    [SerializeField] private float phoneActiveX = -300f;
    [Tooltip("How far right of its dock spot the phone waits while hidden, so it slides in and out of the screen edge.")]
    [SerializeField] private float phoneHideOffset = 260f;
    [SerializeField] private float slideSeconds = 0.25f;

    private readonly HashSet<object> _laptopLockers = new HashSet<object>();
    private readonly HashSet<object> _phoneLockers = new HashSet<object>();
    private bool _laptopOpen;
    private bool _laptopAppOpen;

    public Mode Current { get; private set; } = Mode.None;
    public bool IsLaptopLocked => _laptopLockers.Count > 0;
    public bool IsPhoneLockedOpen => _phoneLockers.Count > 0;
    public event Action<Mode> ModeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (phoneBodyButton != null) phoneBodyButton.onClick.AddListener(HandlePhoneBodyClicked);
        if (phoneCloseButton != null) phoneCloseButton.onClick.AddListener(ClosePhone);

        if (laptopSlot != null) laptopSlot.anchoredPosition = new Vector2(laptopDockX, 0f);
        if (phoneSlot != null) phoneSlot.gameObject.SetActive(false);
        RefreshChrome();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        if (Instance == this) Instance = null;
    }

    // ---- Laptop (called by LaptopController.OnEnable / OnDisable) ----

    public void NotifyLaptopOpened()
    {
        _laptopOpen = true;
        if (Current == Mode.None) Apply(Mode.Laptop);
        else RefreshChrome();
    }

    public void NotifyLaptopClosed()
    {
        _laptopOpen = false;
        _laptopAppOpen = false;
        if (Current == Mode.Laptop) Apply(Mode.None);
        else RefreshChrome();
    }

    // ---- Phone ----

    /// <summary>Phone-only view. From the Laptop view the Laptop slides out to the left.</summary>
    public void OpenPhone() => Apply(Mode.Phone);

    /// <summary>Back to the Laptop if it is open (it slides back in), otherwise back to the desk.</summary>
    public void ClosePhone()
    {
        if (Current != Mode.Phone || IsPhoneLockedOpen) return;
        Apply(_laptopOpen ? Mode.Laptop : Mode.None);
    }

    /// <summary>A paused call (client/ITRS) keeps the phone open: closing it would leave the clock paused on the desk.</summary>
    public void LockPhoneOpen(object holder)
    {
        if (holder == null) return;
        _phoneLockers.Add(holder);
        RefreshChrome();
    }

    public void UnlockPhoneOpen(object holder)
    {
        if (holder == null) return;
        _phoneLockers.Remove(holder);
        RefreshChrome();
    }

    private void HandlePhoneBodyClicked()
    {
        if (Current == Mode.Laptop) OpenPhone();
    }

    // ---- Laptop lock (a paused call: the Laptop must not act, plan P16) ----

    public void LockLaptop(object holder)
    {
        if (holder == null) return;
        _laptopLockers.Add(holder);
        RefreshChrome();
    }

    public void UnlockLaptop(object holder)
    {
        if (holder == null) return;
        _laptopLockers.Remove(holder);
        RefreshChrome();
    }

    // ---- Layout ----

    /// <summary>A Laptop app (Mining, Blackjack, Billiards...) opened or closed. While one is open its window is centred and
    /// covers the docked Phone, so the player cannot reach the Phone from inside an app.</summary>
    public void NotifyLaptopAppOpen(bool open)
    {
        if (_laptopAppOpen == open) return;
        _laptopAppOpen = open;
        if (Current == Mode.Laptop) Layout(true);
    }

    private void Apply(Mode next)
    {
        Mode prev = Current;
        if (next == prev) return;

        DOTween.Kill(this);
        Current = next;

        if (prev == Mode.None && blur != null) blur.Show();
        if (next == Mode.None && blur != null) blur.Hide();

        Layout(prev != Mode.None && next != Mode.None);
        ModeChanged?.Invoke(next);
    }

    private void Layout(bool slide)
    {
        DOTween.Kill(this);
        bool phoneVisible = Current == Mode.Phone || (Current == Mode.Laptop && !_laptopAppOpen);

        float laptopX = Current == Mode.Phone ? laptopHiddenX : (_laptopAppOpen ? 0f : laptopDockX);
        float phoneX = Current == Mode.Phone ? phoneActiveX : (phoneVisible ? phoneDockX : phoneDockX + phoneHideOffset);
        MoveTo(laptopSlot, laptopX, slide, null);

        if (phoneSlot != null)
        {
            if (phoneVisible) phoneSlot.gameObject.SetActive(true);
            // Hiding slides the docked phone off the right edge first, then switches it off (its InputBlocker goes with it).
            MoveTo(phoneSlot, phoneX, slide && (phoneVisible || phoneSlot.gameObject.activeSelf),
                   phoneVisible ? (System.Action)null : () => phoneSlot.gameObject.SetActive(false));
        }

        RefreshChrome();
    }

    private void MoveTo(RectTransform slot, float x, bool slide, Action onDone)
    {
        if (slot == null) return;
        if (!slide)
        {
            slot.anchoredPosition = new Vector2(x, slot.anchoredPosition.y);
            onDone?.Invoke();
            return;
        }
        var tween = slot.DOAnchorPosX(x, slideSeconds).SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(this);
        if (onDone != null) tween.OnComplete(() => onDone());
    }

    private void RefreshChrome()
    {
        if (laptopLockCover != null) laptopLockCover.SetActive(Current == Mode.Laptop && IsLaptopLocked);
        if (laptopClock != null) laptopClock.SetActive(_laptopOpen);
        if (phoneBodyButton != null) phoneBodyButton.interactable = Current == Mode.Laptop;
        if (phoneCloseButton != null)
        {
            phoneCloseButton.gameObject.SetActive(Current == Mode.Phone);
            phoneCloseButton.interactable = !IsPhoneLockedOpen;
        }
    }
}
