using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Call app window. While ringing it shows only who is calling (caller ID) and ACCEPT / DECLINE: no details, no
/// timers. After ACCEPT the caller speaks (CallerLine) and the main button becomes the player's reply for that line
/// ("Got you.", then "Hang up." on the last line); DECLINE stays greyed out for the whole call. When the call is over the
/// window shows the call history (CallHistoryView). It only reads PhoneController; every button calls straight back into it.
/// Layout: the headline on top; a body column (caller picture in a circle, who is calling, what they say); the buttons in
/// a row at the bottom. Names, pictures and lines come from the CallerProfileCatalog on PhoneController.
/// </summary>
public class CallScreen : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text headline;
    [SerializeField] private TMP_Text callerLabel;
    [SerializeField] private TMP_Text detail;

    [Header("Caller picture (a circle with the picture nested inside)")]
    [SerializeField] private GameObject profileRoot;
    [SerializeField] private Image portrait;

    [Header("Live call vs. history")]
    [Tooltip("Everything shown while a call rings or is active (headline, body, buttons). Hidden when there is no call.")]
    [SerializeField] private GameObject[] liveParts;
    [Tooltip("The history list + transcript, shown when there is no call.")]
    [SerializeField] private GameObject historyRoot;

    [Header("Buttons")]
    [SerializeField] private Button primaryButton;
    [SerializeField] private TMP_Text primaryLabel;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private TMP_Text secondaryLabel;

    private enum Action { None, Answer, Decline, Reply }
    private Action _primary, _secondary;

    private void Awake()
    {
        primaryButton.onClick.AddListener(() => Run(_primary));
        secondaryButton.onClick.AddListener(() => Run(_secondary));
    }

    private void Update()
    {
        var phone = PhoneController.Instance;
        if (phone == null || phone.Calls == null) return;

        if (phone.IsRinging) ShowRinging(phone);
        else if (phone.ActiveCall != null) ShowCall(phone);
        else ShowIdle();
    }

    private void ShowRinging(PhoneController phone)
    {
        var call = phone.RingingCall;
        Set(phone, "INCOMING CALL", call.shownAs, call.profileSeed, "",
            Action.Answer, "ACCEPT", true, Action.Decline, "DECLINE", true);
    }

    private void ShowCall(PhoneController phone)
    {
        var call = phone.ActiveCall;
        var kind = call.truth == CallTruth.Client ? CallerKind.Client : call.truth == CallTruth.ITRS ? CallerKind.ITRS : CallerKind.Telemarketer;
        // DECLINE is still there, greyed out for the whole call.
        Set(phone, "ON THE LINE", kind, call.profileSeed, phone.CallerLine,
            Action.Reply, phone.ReplyLabel, phone.ReplyEnabled, Action.Decline, "DECLINE", false);
    }

    private void ShowIdle()
    {
        // No call: the live view hides and the history (list + transcript) takes its place.
        SetLive(false);
    }

    private void SetLive(bool on)
    {
        foreach (var part in liveParts) PhoneUi.SetActive(part, on);
        PhoneUi.SetActive(historyRoot, !on);
    }

    private void Set(PhoneController phone, string head, CallerKind face, int seed, string text,
                     Action a1, string l1, bool on1, Action a2, string l2, bool on2)
    {
        SetLive(true);
        PhoneUi.SetText(headline, head);
        ShowCaller(phone, face, seed);
        PhoneUi.SetText(detail, text);
        _primary = a1; _secondary = a2;
        PhoneUi.SetActive(primaryButton.gameObject, a1 != Action.None);
        PhoneUi.SetActive(secondaryButton.gameObject, a2 != Action.None);
        PhoneUi.SetText(primaryLabel, l1);
        PhoneUi.SetText(secondaryLabel, l2);
        primaryButton.interactable = on1;
        secondaryButton.interactable = on2;
    }

    private void ShowCaller(PhoneController phone, CallerKind face, int seed)
    {
        var profile = phone.Catalog.Pick(face, seed);
        PhoneUi.SetText(callerLabel, phone.CallerName(face, seed));
        if (portrait != null)
        {
            var sprite = profile != null ? profile.portrait : null;
            if (portrait.sprite != sprite) portrait.sprite = sprite;
            portrait.enabled = sprite != null;               // no picture yet: the blank circle shows
        }
    }

    private static void Run(Action action)
    {
        var phone = PhoneController.Instance;
        if (phone == null) return;
        switch (action)
        {
            case Action.Answer: phone.Answer(); break;
            case Action.Decline: phone.Decline(); break;
            case Action.Reply: phone.Reply(); break;
        }
    }
}
