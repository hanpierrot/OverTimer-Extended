using System;

/// Test switches for the Billiards app, set from the cheat menu or Tools/OverTimer/Billiards. Session-only: nothing
/// is saved, and the defaults (overlay off, mirror random) are what the player always gets.
public static class BilliardsDebug
{
    public enum MirrorMode { Random, None, X, Y, XY }

    private static bool _overlay;
    private static MirrorMode _mirror = MirrorMode.Random;

    /// Raised after either switch changes.
    public static event Action Changed;

    public static bool Overlay
    {
        get => _overlay;
        set { if (_overlay == value) return; _overlay = value; Changed?.Invoke(); }
    }

    public static MirrorMode Mirror
    {
        get => _mirror;
        set { if (_mirror == value) return; _mirror = value; Changed?.Invoke(); }
    }

    /// The mirror to draw a table with: the forced one, or the rolled one when no mirror is forced.
    public static TableMirror Apply(TableMirror rolled)
    {
        switch (_mirror)
        {
            case MirrorMode.None: return new TableMirror(false, false);
            case MirrorMode.X:    return new TableMirror(true, false);
            case MirrorMode.Y:    return new TableMirror(false, true);
            case MirrorMode.XY:   return new TableMirror(true, true);
            default:              return rolled;
        }
    }
}
