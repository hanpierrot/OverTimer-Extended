using UnityEngine;

/// The 16 ball sprites (cue + 1..15). The kit lives under Assets/Art, not Resources, so it can't be loaded by name
/// at runtime; this asset holds the references and BallSprites.Matches picks the right one for an id.
[CreateAssetMenu(fileName = "BallSpriteSet", menuName = "Overtime/Billiards/Ball Sprite Set")]
public class BallSpriteSet : ScriptableObject
{
    [SerializeField] private Sprite[] sprites;

    public Sprite Get(string id)
    {
        if (sprites == null) return null;
        foreach (var s in sprites)
        {
            if (s == null) continue;
            try { if (BallSprites.Matches(s.name, id)) return s; }
            catch (System.ArgumentException) { return null; }          // not a ball id
        }
        return null;
    }
}
