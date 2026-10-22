using System;
using UnityEngine;

public enum BilliardsDifficulty { Easy = 0, Medium = 1, Hard = 2 }

[Serializable] public class BallSpawn    { public string id; public float x; public float y; }
[Serializable] public class CalledPocket { public string ball; public string pocket; }  // "TL","TM","TR","BL","BM","BR"
/// One shot. angle: degrees CCW from +x, measured in TABLE space (isotropic). power: 0..1.
[Serializable]
public class ShotInput
{
    public float angle;
    public float power;

    /// Every shot - player or replay - goes through this, so recorded solutions are exact and short.
    public static ShotInput Quantized(float angleDeg, float power)
    {
        float a = (float)Math.Round(angleDeg * 10.0) / 10f;
        a = ((a % 360f) + 540f) % 360f - 180f;                 // wrap to [-180, 180)
        float p = (float)Math.Round(Mathf.Clamp(power, 0f, 1f) * 100.0) / 100f;
        return new ShotInput { angle = a, power = p };
    }

    public Vector2 Direction
    {
        get
        {
            double r = angle * Math.PI / 180.0;
            return new Vector2((float)Math.Cos(r), (float)Math.Sin(r));
        }
    }
}

[Serializable]
public class BilliardsPreset
{
    public string id;
    public string name;
    public int par;
    public int[] shots;              // optional [easy, medium, hard]; missing or 0 = derive from par
    public float payoutScale = 1f;   // optional hand-tune; JsonUtility keeps this default when the field is absent
    public BallSpawn[] balls;
    public string[] order;           // potting order; the last entry is the final ball
    public string[] forbidden;       // optional: potting one of these at any time = fail
    public CalledPocket[] called;    // optional
    public ShotInput[] solution;     // optional: known solution, replayed by the EditMode test

    public int ShotsFor(BilliardsDifficulty d, int[] parOffsets)
    {
        int i = (int)d;
        if (shots != null && i < shots.Length && shots[i] > 0) return shots[i];
        return par + parOffsets[i];
    }
}

public static class BilliardsPresetValidator
{
    /// physics: when given, also checks that no two balls overlap at the start and none starts inside a pocket.
    public static bool Validate(BilliardsPreset p, int[] parOffsets, BilliardsSimParams? physics = null)
    {
        bool ok = true;
        void Warn(string msg) { Debug.LogWarning($"[Billiards:{p.id}] {msg}"); ok = false; }

        int e = p.ShotsFor(BilliardsDifficulty.Easy,   parOffsets);
        int m = p.ShotsFor(BilliardsDifficulty.Medium, parOffsets);
        int h = p.ShotsFor(BilliardsDifficulty.Hard,   parOffsets);
        if (!(e >= m && m >= h)) Warn($"shots must not increase with difficulty: {e}/{m}/{h}");
        if (h < p.par)           Warn($"hard gives {h} shots but par is {p.par}: the known solution can't fit");
        if (p.shots != null && p.shots.Length > 3) Warn("shots has more than 3 entries");
        if (p.solution != null && p.solution.Length > 0 && p.solution.Length != p.par)
            Warn($"solution is {p.solution.Length} shots, par says {p.par}");

        var ids = new System.Collections.Generic.HashSet<string>();
        int cues = 0;
        foreach (var b in p.balls ?? Array.Empty<BallSpawn>())
        {
            if (!ids.Add(b.id)) Warn($"duplicate ball id '{b.id}'");
            if (b.id == "cue") cues++;
            if (b.x < 0f || b.x > 1f || b.y < 0f || b.y > 1f) Warn($"ball {b.id} is off the felt ({b.x}, {b.y})");
        }
        if (cues != 1) Warn($"need exactly one cue ball, found {cues}");

        if (physics.HasValue && p.balls != null)
        {
            float r = physics.Value.ballRadius;
            for (int i = 0; i < p.balls.Length; i++)
            {
                var a = p.balls[i];
                Vector2 pa = new Vector2(a.x * FeltMapper.TableW, a.y * FeltMapper.TableH);   // preset space is stretched 2:1
                for (int j = i + 1; j < p.balls.Length; j++)
                {
                    var b = p.balls[j];
                    float d = Vector2.Distance(pa, new Vector2(b.x * FeltMapper.TableW, b.y * FeltMapper.TableH));
                    if (d < 2f * r) Warn($"balls {a.id} and {b.id} overlap at the start (centres {d:0.000} apart, need {2f * r:0.000})");
                }
                foreach (Pocket pk in Enum.GetValues(typeof(Pocket)))
                {
                    bool side = pk == Pocket.TM || pk == Pocket.BM;
                    float rad = side ? physics.Value.sidePocketRadius : physics.Value.cornerPocketRadius;
                    if (Vector2.Distance(pa, BilliardsSim.PocketCentre(pk)) < rad + r)
                        Warn($"ball {a.id} starts too close to pocket {pk}");
                }
            }
        }
        if (p.order == null || p.order.Length == 0) Warn("order is empty");
        foreach (var id in p.order ?? Array.Empty<string>())     if (!ids.Contains(id)) Warn($"order names missing ball '{id}'");
        foreach (var id in p.forbidden ?? Array.Empty<string>()) if (!ids.Contains(id)) Warn($"forbidden names missing ball '{id}'");
        foreach (var c in p.called ?? Array.Empty<CalledPocket>())
            if (!Enum.TryParse<Pocket>(c.pocket, out _)) Warn($"called pocket '{c.pocket}' is not TL/TM/TR/BL/BM/BR");
        return ok;
    }
}
