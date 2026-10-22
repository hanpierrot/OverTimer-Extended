using UnityEngine;

/// Converts between sim TABLE space (0..2 x 0..1, isotropic) and felt-local CANVAS units (isotropic),
/// applying the display mirror. The only place the mirror touches positions or aim.
public readonly struct FeltMapper
{
    public const float TableW = 2f, TableH = 1f;
    readonly float _scale;            // felt canvas units per table unit
    readonly TableMirror _mirror;

    /// feltWidthCanvas: width of the felt RectTransform in canvas units. Its height must be exactly half.
    public FeltMapper(float feltWidthCanvas, TableMirror mirror)
    {
        _scale = feltWidthCanvas / TableW;
        _mirror = mirror;
    }

    public Vector2 TableToFelt(Vector2 t)
    {
        Vector2 n = _mirror.Point(new Vector2(t.x / TableW, t.y / TableH));
        return new Vector2(n.x * TableW, n.y * TableH) * _scale;
    }

    public Vector2 FeltToTable(Vector2 f)
    {
        Vector2 n = _mirror.Point(new Vector2(f.x / _scale / TableW, f.y / _scale / TableH));
        return new Vector2(n.x * TableW, n.y * TableH);
    }

    /// Aim: felt-local drag vector -> sim direction. Felt units are isotropic, so this is just the mirror.
    /// NEVER compute an angle from 0..1 normalized coordinates: that space is stretched 2:1.
    public Vector2 FeltDirToTable(Vector2 feltDelta) => _mirror.Dir(feltDelta).normalized;
    public Vector2 TableDirToFelt(Vector2 tableDir)  => _mirror.Dir(tableDir).normalized;

    public float FeltToTableDistance(float feltDistance) => feltDistance / _scale;
    public float TableToFeltDistance(float tableDistance) => tableDistance * _scale;

    public static float AngleDeg(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    /// Snap a DRAWN position to whole art pixels. Never feed the result back into the sim.
    public static Vector2 Snap(Vector2 feltPos, float artPixel) =>
        new Vector2(Mathf.Round(feltPos.x / artPixel) * artPixel, Mathf.Round(feltPos.y / artPixel) * artPixel);
}
