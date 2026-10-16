using UnityEditor;

/// Editor shortcuts for the same switches the cheat menu has (BilliardsDebug). They work in play mode.
public static class BilliardsDebugMenu
{
    private const string Root = "Tools/OverTimer/Billiards/";

    [MenuItem(Root + "Toggle Debug Overlay")]
    private static void ToggleOverlay() => BilliardsDebug.Overlay = !BilliardsDebug.Overlay;

    [MenuItem(Root + "Mirror/Random")] private static void MirrorRandom() => BilliardsDebug.Mirror = BilliardsDebug.MirrorMode.Random;
    [MenuItem(Root + "Mirror/None")]   private static void MirrorNone()   => BilliardsDebug.Mirror = BilliardsDebug.MirrorMode.None;
    [MenuItem(Root + "Mirror/X")]      private static void MirrorX()      => BilliardsDebug.Mirror = BilliardsDebug.MirrorMode.X;
    [MenuItem(Root + "Mirror/Y")]      private static void MirrorY()      => BilliardsDebug.Mirror = BilliardsDebug.MirrorMode.Y;
    [MenuItem(Root + "Mirror/XY")]     private static void MirrorXY()     => BilliardsDebug.Mirror = BilliardsDebug.MirrorMode.XY;
}
