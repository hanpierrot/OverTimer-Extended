using System;
using System.Collections.Generic;

/// Keyed clock-speed contributors (fixes L5: TickMultiplier was a single last-write-wins float,
/// so a phone ring and a speed card silently cancelled each other).
/// Effective multiplier = product of every active contributor, clamped to [Min, Max].
/// Min is 0 because the Time Scarab card deliberately stops the clock (x0).
public sealed class SpeedModifiers
{
    public const float Min = 0f, Max = 3f;
    readonly Dictionary<object, float> _mods = new Dictionary<object, float>();

    public float Effective
    {
        get
        {
            float m = 1f;
            foreach (var v in _mods.Values) m *= v;
            return m < Min ? Min : m > Max ? Max : m;
        }
    }

    public int Count => _mods.Count;

    /// Same key again replaces its value (never double-counts).
    public void Set(object key, float multiplier)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (float.IsNaN(multiplier) || multiplier < 0f) throw new ArgumentOutOfRangeException(nameof(multiplier));
        _mods[key] = multiplier;
    }

    /// Removing an absent key is a silent no-op.
    public void Remove(object key) { if (key != null) _mods.Remove(key); }

    public void Clear() => _mods.Clear();
}
