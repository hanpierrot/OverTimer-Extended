using System.Collections.Generic;
using UnityEngine;

public static class AimAssist
{
    /// Where does the cue ball first touch another ball if it travels straight along dir (unit vector)?
    /// Touch happens when centres are 2r apart, so this is a ray vs circle-of-radius-2r test.
    public static bool FirstBallContact(Vector2 cue, Vector2 dir, IReadOnlyList<Vector2> others, float r,
                                        out int hitIndex, out float t)
    {
        hitIndex = -1; t = float.MaxValue;
        float R2 = 4f * r * r;
        for (int i = 0; i < others.Count; i++)
        {
            Vector2 m = cue - others[i];
            float b = Vector2.Dot(m, dir);
            if (b > 0f) continue;                    // ball is behind the cue
            float disc = b * b - (Vector2.Dot(m, m) - R2);
            if (disc < 0f) continue;                 // line passes it by
            float ti = -b - Mathf.Sqrt(disc);
            if (ti >= 0f && ti < t) { t = ti; hitIndex = i; }
        }
        return hitIndex >= 0;
    }

    /// Distance until the cue ball's edge reaches a cushion. Felt is (0,0)-(w,h) in table units.
    public static float DistanceToCushion(Vector2 p, Vector2 dir, float w, float h, float r)
    {
        float t = float.MaxValue;
        if (dir.x > 0f) t = System.Math.Min(t, (w - r - p.x) / dir.x);
        if (dir.x < 0f) t = System.Math.Min(t, (r - p.x) / dir.x);
        if (dir.y > 0f) t = System.Math.Min(t, (h - r - p.y) / dir.y);
        if (dir.y < 0f) t = System.Math.Min(t, (r - p.y) / dir.y);
        return t;
    }

    /// Classic ghost-ball prediction (equal masses, no spin): object ball leaves along the line of centres,
    /// cue ball leaves along the tangent. cut: 1 = full-ball hit, 0 = thinnest possible cut.
    public static void Deflections(Vector2 ghost, Vector2 objectBall, Vector2 dir,
                                   out Vector2 objectDir, out Vector2 cueDir, out float cut)
    {
        objectDir = (objectBall - ghost).normalized;
        cut       = Vector2.Dot(dir, objectDir);
        cueDir    = (dir - objectDir * cut).normalized;
    }
}

public static class UiSegment
{
    /// Stretch an Image into a line from a to b. RectTransform pivot must be (0, 0.5).
    /// Image type Tiled with a 2x2 dot sprite gives a dotted pixel-art line.
    public static void Set(RectTransform seg, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        seg.anchoredPosition = a;
        seg.sizeDelta        = new Vector2(d.magnitude, seg.sizeDelta.y);
        seg.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }
}
