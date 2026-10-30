using UnityEngine;

/// <summary>
/// Where the empty boxes of unboxed parcels may lie (DESIGN §5.7): a rectangle in the Room view between the Door zone and
/// the Desk zone, given by two opposite corners set on the DoorController. A box dropped outside it goes back to the
/// bottom edge, straight below where it was let go (x clamped into the area). Plain geometry, no scene access, so it is tested headless.
/// </summary>
public readonly struct EmptyBoxArea
{
    public readonly Vector2 Min;
    public readonly Vector2 Max;

    public EmptyBoxArea(Vector2 min, Vector2 max)
    {
        Min = new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y));
        Max = new Vector2(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
    }

    public bool Contains(Vector2 point) => point.x >= Min.x && point.x <= Max.x && point.y >= Min.y && point.y <= Max.y;

    /// <summary>A uniformly random point inside the area.</summary>
    public Vector2 RandomPoint(System.Random rng) =>
        new Vector2(Min.x + (float)rng.NextDouble() * (Max.x - Min.x), Min.y + (float)rng.NextDouble() * (Max.y - Min.y));

    /// <summary>The spot on the bottom edge straight below the point (y = Min.y). An x outside the area moves to the nearest x inside it.</summary>
    public Vector2 DropPoint(Vector2 point) => new Vector2(Mathf.Clamp(point.x, Min.x, Max.x), Min.y);
}
