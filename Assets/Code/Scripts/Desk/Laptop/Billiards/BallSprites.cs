using System;
using System.Globalization;

/// Ball id -> sprite name in Assets/Art/Billiard Kit.
/// The art is NOT under Resources, so sprites must be referenced by a serialized array
/// (e.g. a BallSpriteSet ScriptableObject) and looked up with Matches(sprite.name, id).
/// The kit is imported in Sprite Mode "Multiple", so each sprite is really named "A1_0", not "A1".
public static class BallSprites
{
    public static string SpriteNameFor(string id)
    {
        if (id == "cue") return "Ball-White";
        if (!int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
            throw new ArgumentException($"ball id '{id}' is not 'cue' or 1-15");
        if (n == 8) return "H8";
        if (n >= 1 && n <= 7) return $"{(char)('A' + n - 1)}{n}";           // solids: A1 .. G7
        if (n >= 9 && n <= 15) { int k = n - 8; return $"{(char)('A' + k - 1)} - {k}"; } // stripes: "A - 1" (9) .. "G - 7" (15)
        throw new ArgumentException($"no sprite for ball '{id}'");
    }

    /// True if a sprite (by its Sprite.name) is the art for this ball. Accepts both "A1" and "A1_0".
    public static bool Matches(string spriteName, string id)
    {
        string want = SpriteNameFor(id);
        return spriteName == want || spriteName == want + "_0";
    }
}
