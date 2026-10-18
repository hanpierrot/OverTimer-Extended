using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Who is calling and what they say.
///  - Profiles: a name and a profile picture for each kind of caller (Telemarketer, Client, Unknown, ITRS). Each call picks
///    one entry at random (stored on the call, so the ringing and answered screens show the same face). An empty list falls
///    back to the kind's name and a blank circle.
///  - Scripts: the conversation for each real caller (Telemarketer, Client, ITRS). Each line is what the caller says plus
///    the label of the button the player presses to answer it. The LAST line is where the caller's payoff happens (the
///    client pays, ITRS grants more time) and its button should say "Hang up.".
/// Asset: Assets/Settings/Configs/CallerProfiles.asset
/// </summary>
[CreateAssetMenu(fileName = "CallerProfiles", menuName = "Overtime/Phone/Caller Profiles")]
public class CallerProfileCatalog : ScriptableObject
{
    [Serializable]
    public class Profile
    {
        [Tooltip("Shown under the picture, e.g. \"Mr. Tanaka\".")]
        public string displayName;
        [Tooltip("Square picture; it is cropped to a circle.")]
        public Sprite portrait;
    }

    [Serializable]
    public class Line
    {
        [TextArea(2, 4)] [Tooltip("What the caller says.")]
        public string callerLine;
        [Tooltip("The player's button while this line is on screen, e.g. \"Got you.\". On the last line use \"Hang up.\".")]
        public string replyLabel;
    }

    [Header("Profiles - shown while ringing as \"Unknown\", and for unknown numbers")]
    [SerializeField] private List<Profile> unknown = new List<Profile> { new Profile { displayName = "Unknown number" } };

    [Header("Profiles - revealed when you answer")]
    [SerializeField] private List<Profile> telemarketers = new List<Profile> { new Profile { displayName = "Telemarketer" } };
    [SerializeField] private List<Profile> clients = new List<Profile> { new Profile { displayName = "Client" } };
    [SerializeField] private List<Profile> itrs = new List<Profile> { new Profile { displayName = "ITRS Agent" } };

    [Header("Scripts (the last line is the payoff; no numbers or timers: the player learns nothing before picking up)")]
    [SerializeField] private List<Line> telemarketerScript = new List<Line>
    {
        new Line { callerLine = "Hello there! Do you have a minute to talk about your car's extended warranty?", replyLabel = "Uh-huh." },
        new Line { callerLine = "Wonderful! It will only take a moment of your time...", replyLabel = "Hang up." },
    };
    [SerializeField] private List<Line> clientScript = new List<Line>
    {
        new Line { callerLine = "I am interested in buying one of your products, will you sign me up for it?", replyLabel = "Got you." },
        new Line { callerLine = "Awesome, thank you!", replyLabel = "Hang up." },
    };
    [SerializeField] private List<Line> itrsScript = new List<Line>
    {
        new Line { callerLine = "This is the Internal Time Revenue Service. We are calling about your account.", replyLabel = "Go on." },
        new Line { callerLine = "Fine, we will give you a little more time to pay. Do not make us call again.", replyLabel = "Hang up." },
    };
    [Tooltip("Replaces the ITRS last line when the bill was already settled by the time the player got there (the extra time goes to the NEXT bill).")]
    [SerializeField] private string itrsNoBillLine = "It looks like you already paid. Fine, we will give your next bill a little more time. Goodbye.";
    [Tooltip("What a client says when the player never answers their first line, right before they hang up.")]
    [SerializeField] private string noReplyLine = "Hello? Are you there?";

    public string ItrsNoBillLine => itrsNoBillLine;
    public string NoReplyLine => noReplyLine;

    /// <summary>The profile for this kind of caller; seed is the call's stored random number. Null if the list is empty.</summary>
    public Profile Pick(CallerKind kind, int seed)
    {
        List<Profile> list;
        switch (kind)
        {
            case CallerKind.Telemarketer: list = telemarketers; break;
            case CallerKind.Client: list = clients; break;
            case CallerKind.ITRS: list = itrs; break;
            default: list = unknown; break;
        }
        if (list == null || list.Count == 0) return null;
        return list[(seed & int.MaxValue) % list.Count];
    }

    /// <summary>The conversation for what the call really is. Never empty: a missing script becomes one "Hang up." line.</summary>
    public IReadOnlyList<Line> Script(CallTruth truth)
    {
        List<Line> list = truth == CallTruth.Client ? clientScript : truth == CallTruth.ITRS ? itrsScript : telemarketerScript;
        if (list != null && list.Count > 0) return list;
        return new List<Line> { new Line { callerLine = "...", replyLabel = "Hang up." } };
    }
}
