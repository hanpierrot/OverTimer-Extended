using System;
using System.Collections.Generic;

public enum MessageKind { License, Delivery, Tax, Sale, MissedCall, Junk, System }

public sealed class InboxMessage
{
    public int id;
    public MessageKind kind;
    public string title;
    public string body;
    public string actionId;      // e.g. "license:blackjack" -> the Activate button; null = no button
    public bool read;
    public bool actionDone;
}

/// Everything that happens is logged here, so a missed banner is never lost.
public sealed class Inbox
{
    readonly List<InboxMessage> _all = new List<InboxMessage>();
    int _nextId = 1;

    public IReadOnlyList<InboxMessage> All => _all;
    public int Unread { get { int n = 0; foreach (var m in _all) if (!m.read) n++; return n; } }
    public event Action<InboxMessage> Added;

    public InboxMessage Add(MessageKind kind, string title, string body, string actionId = null)
    {
        var m = new InboxMessage { id = _nextId++, kind = kind, title = title, body = body, actionId = actionId };
        _all.Insert(0, m);                // newest first
        Added?.Invoke(m);
        return m;
    }

    public void MarkRead(InboxMessage m) => m.read = true;
}

/// Short-lived popups on the phone. At most MaxVisible at once; the rest wait their turn.
/// Banners are cosmetic: every banner's content is already in the Inbox.
public sealed class BannerQueue
{
    public const int MaxVisible = 3;
    readonly float _lifetime;
    readonly List<(InboxMessage msg, float left)> _visible = new List<(InboxMessage, float)>();
    readonly Queue<InboxMessage> _waiting = new Queue<InboxMessage>();

    public IReadOnlyList<(InboxMessage msg, float left)> Visible => _visible;
    public int Waiting => _waiting.Count;
    public event Action<InboxMessage> Shown, Hidden;

    public BannerQueue(float lifetime) { _lifetime = lifetime; }

    public void Push(InboxMessage m)
    {
        if (_visible.Count < MaxVisible) Show(m); else _waiting.Enqueue(m);
    }

    /// Unscaled seconds: banners keep moving during paused calls.
    public void Tick(float realDelta)
    {
        for (int i = _visible.Count - 1; i >= 0; i--)
        {
            var (m, left) = _visible[i];
            left -= realDelta;
            if (left <= 0f) { _visible.RemoveAt(i); Hidden?.Invoke(m); }
            else _visible[i] = (m, left);
        }
        while (_visible.Count < MaxVisible && _waiting.Count > 0) Show(_waiting.Dequeue());
    }

    /// Phone off / forced off: drop the popups (the Inbox still has everything).
    public void Clear() { foreach (var (m, _) in _visible) Hidden?.Invoke(m); _visible.Clear(); _waiting.Clear(); }

    void Show(InboxMessage m) { _visible.Add((m, _lifetime)); Shown?.Invoke(m); }
}
