using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Call app window: the incoming-call screen (Answer / Decline), the three call screens by what the call really
/// is (telemarketer pitch, client job, ITRS) and, when idle, the recent-call log. It only reads PhoneController; every
/// button calls straight back into it.
/// </summary>
public class CallScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text headline;
    [SerializeField] private TMP_Text body;
    [SerializeField] private TMP_Text timer;
    [SerializeField] private Button primaryButton;
    [SerializeField] private TMP_Text primaryLabel;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private TMP_Text secondaryLabel;

    private enum Action { None, Answer, Decline, HangUp, TakeJob, PaymentPlan }
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
        else ShowIdle(phone);
    }

    private void ShowRinging(PhoneController phone)
    {
        var call = phone.RingingCall;
        Set("INCOMING CALL", call.shownAs == CallerKind.Unknown ? "Unknown number" : call.shownAs.ToString(), "",
            Action.Answer, "Answer", true, Action.Decline, "Decline", true);
    }

    private void ShowCall(PhoneController phone)
    {
        var call = phone.ActiveCall;
        switch (call.truth)
        {
            case CallTruth.Telemarketer:
                bool listening = phone.PitchLeft > 0f;
                Set("TELEMARKETER", "\"Have you got a minute to talk about your car's extended warranty?\"",
                    listening ? $"Listening... {Mathf.CeilToInt(phone.PitchLeft)}s" : "",
                    Action.HangUp, "Hang up", !listening, Action.None, "", false);
                break;

            case CallTruth.Client:
                if (phone.ClientAccepted)
                    Set("CLIENT", $"Thanks! +${phone.ClientMoney}", "", Action.None, "", false, Action.None, "", false);
                else
                    Set("CLIENT", $"A client needs a quick job done.\n+${phone.ClientMoney} now.", $"{Mathf.CeilToInt(Mathf.Max(0f, phone.ClientLeft))}s",
                        Action.TakeJob, $"Take the job (+${phone.ClientMoney})", true, Action.HangUp, "No thanks", true);
                break;

            default:   // ITRS
                var itrs = ITRSService.Instance;
                bool bill = itrs != null && itrs.HasPendingAssessment;
                if (phone.PaymentPlanTaken)
                    Set("ITRS", $"Payment plan agreed.\n+{phone.ItrsExtension:0} seconds to pay your ${itrs.CurrentAmountDue} bill.", "",
                        Action.None, "", false, Action.None, "", false);
                else if (bill)
                    Set("ITRS", $"Your bill of ${itrs.CurrentAmountDue} is due in {Mathf.CeilToInt(itrs.SecondsLeft)}s.", "",
                        Action.PaymentPlan, $"Payment plan (+{phone.ItrsExtension:0}s)", true, Action.HangUp, "Hang up", true);
                else
                    Set("ITRS", "Your bill is being processed.", "", Action.HangUp, "Hang up", true, Action.None, "", false);
                break;
        }
    }

    private void ShowIdle(PhoneController phone)
    {
        var log = phone.CallLog;
        string text = "No calls yet.";
        if (log.Count > 0)
        {
            int n = Mathf.Min(6, log.Count);
            var lines = new string[n];
            for (int i = 0; i < n; i++) lines[i] = log[i];
            text = string.Join("\n", lines);
        }
        Set("RECENT CALLS", text, "", Action.None, "", false, Action.None, "", false);
    }

    private void Set(string head, string text, string time, Action a1, string l1, bool on1, Action a2, string l2, bool on2)
    {
        PhoneUi.SetText(headline, head);
        PhoneUi.SetText(body, text);
        PhoneUi.SetText(timer, time);
        _primary = a1; _secondary = a2;
        PhoneUi.SetActive(primaryButton.gameObject, a1 != Action.None);
        PhoneUi.SetActive(secondaryButton.gameObject, a2 != Action.None);
        PhoneUi.SetText(primaryLabel, l1);
        PhoneUi.SetText(secondaryLabel, l2);
        primaryButton.interactable = on1;
        secondaryButton.interactable = on2;
    }

    private static void Run(Action action)
    {
        var phone = PhoneController.Instance;
        if (phone == null) return;
        switch (action)
        {
            case Action.Answer: phone.Answer(); break;
            case Action.Decline: phone.Decline(); break;
            case Action.HangUp: phone.HangUp(); break;
            case Action.TakeJob: phone.AcceptClientJob(); break;
            case Action.PaymentPlan: phone.AcceptPaymentPlan(); break;
        }
    }
}
