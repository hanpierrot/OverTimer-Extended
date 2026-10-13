using UnityEngine;

/// Mirrors the table for the PLAYER only. The simulation always runs the preset exactly as authored,
/// so every stored solution stays valid in all four orientations - no re-validation needed.
public readonly struct TableMirror
{
    public readonly bool flipX, flipY;
    public TableMirror(bool flipX, bool flipY) { this.flipX = flipX; this.flipY = flipY; }

    public static TableMirror Roll(System.Random rng) => new TableMirror(rng.Next(2) == 1, rng.Next(2) == 1);

    /// Normalized felt position (0..1). A reflection is its own inverse: same call for sim->view and view->sim.
    public Vector2 Point(Vector2 p) => new Vector2(flipX ? 1f - p.x : p.x, flipY ? 1f - p.y : p.y);

    /// Direction (aim, preview lines). Same call both ways.
    public Vector2 Dir(Vector2 d) => new Vector2(flipX ? -d.x : d.x, flipY ? -d.y : d.y);

    /// Pocket id. Same call both ways.
    public Pocket MapPocket(Pocket p)
    {
        bool top = p == Pocket.TL || p == Pocket.TM || p == Pocket.TR;
        int col  = (p == Pocket.TL || p == Pocket.BL) ? 0 : (p == Pocket.TM || p == Pocket.BM) ? 1 : 2;
        if (flipY) top = !top;
        if (flipX) col = 2 - col;
        return top ? (col == 0 ? Pocket.TL : col == 1 ? Pocket.TM : Pocket.TR)
                   : (col == 0 ? Pocket.BL : col == 1 ? Pocket.BM : Pocket.BR);
    }
}
