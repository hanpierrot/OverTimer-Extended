using System;
using System.Collections.Generic;
using UnityEngine;

/// Snapshot of every physics constant. Built from GameConfig once per round so the sim never reads config mid-shot.
[Serializable]
public struct BilliardsSimParams
{
    public float ballRadius, cornerPocketRadius, sidePocketRadius;
    public float maxShotSpeed, rollingDecel, ballRestitution, cushionRestitution, stopSpeed, maxShotSeconds;

    public static BilliardsSimParams FromConfig(GameConfig c) => new BilliardsSimParams
    {
        ballRadius         = c.billiardsBallRadius,
        cornerPocketRadius = c.billiardsCornerPocketRadius,
        sidePocketRadius   = c.billiardsSidePocketRadius,
        maxShotSpeed       = c.billiardsMaxShotSpeed,
        rollingDecel       = c.billiardsRollingDecel,
        ballRestitution    = c.billiardsBallRestitution,
        cushionRestitution = c.billiardsCushionRestitution,
        stopSpeed          = c.billiardsStopSpeed,
        maxShotSeconds     = c.billiardsMaxShotSeconds,
    };
}

/// Pure C#, deterministic, fixed-step. Rules: no UnityEngine.Random, no Time, no Physics2D, no MonoBehaviour.
/// Balls are iterated in index order and pairs are checked i < j, so a given input always gives the same output.
/// Table space: felt (0,0)-(2,1), y up. See BILLIARDS_IMPLEMENTATION.md, Phase 2, for the step order.
public sealed class BilliardsSim
{
    public const float FixedDt = 1f / 120f;
    const float RespotEpsilon = 0.001f;
    const int   RespotRings   = 12;
    const int   BallPasses    = 4;
    const int   SeparationPasses = 3;

    static readonly Pocket[] AllPockets = { Pocket.TL, Pocket.TM, Pocket.TR, Pocket.BL, Pocket.BM, Pocket.BR };

    /// Table space: felt (0,0)-(2,1), y up. Pocket centres.
    public static Vector2 PocketCentre(Pocket p) => p switch
    {
        Pocket.BL => new Vector2(0f, 0f), Pocket.BM => new Vector2(1f, 0f), Pocket.BR => new Vector2(2f, 0f),
        Pocket.TL => new Vector2(0f, 1f), Pocket.TM => new Vector2(1f, 1f), Pocket.TR => new Vector2(2f, 1f),
        _ => throw new ArgumentOutOfRangeException(nameof(p)),
    };

    /// A straight stretch of cushion. `line` is the fixed coordinate (y for horizontal, x for vertical),
    /// lo..hi the extent along the other axis, `inward` +1/-1 the side the felt is on.
    struct Wall { public bool horizontal; public float line, lo, hi, inward; }

    readonly BilliardsSimParams _p;
    readonly string[] _ids;
    readonly Dictionary<string, int> _index;
    readonly float[] _spawnX, _spawnY;
    readonly float[] _x, _y, _vx, _vy;
    readonly bool[] _potted;
    readonly int _cue;
    readonly Wall[] _walls;
    readonly float[] _jawX, _jawY;
    readonly int _maxSteps;

    ShotReport _report = new ShotReport();
    int  _steps;
    bool _settled = true;

    public BilliardsSim(BilliardsPreset preset, BilliardsSimParams p)
    {
        if (preset == null || preset.balls == null) throw new ArgumentException("preset has no balls");
        _p = p;

        int n = preset.balls.Length;
        _ids = new string[n];
        _index = new Dictionary<string, int>(n);
        _spawnX = new float[n]; _spawnY = new float[n];
        _x = new float[n]; _y = new float[n]; _vx = new float[n]; _vy = new float[n];
        _potted = new bool[n];
        _cue = -1;

        for (int i = 0; i < n; i++)
        {
            var b = preset.balls[i];
            _ids[i] = b.id;
            if (_index.ContainsKey(b.id)) throw new ArgumentException($"duplicate ball id '{b.id}'");
            _index[b.id] = i;
            if (b.id == "cue") _cue = i;
            // Preset space is 0..1 on both axes (stretched 2:1); table space is isotropic 0..2 x 0..1.
            _spawnX[i] = _x[i] = b.x * FeltMapper.TableW;
            _spawnY[i] = _y[i] = b.y * FeltMapper.TableH;
        }
        if (_cue < 0) throw new ArgumentException("preset has no cue ball");

        _walls = BuildWalls(p, out _jawX, out _jawY);
        _maxSteps = (int)Math.Ceiling(p.maxShotSeconds / FixedDt);
    }

    // Clone support: arrays are copied, immutable data (ids, walls, params) is shared.
    BilliardsSim(BilliardsSim o)
    {
        _p = o._p; _ids = o._ids; _index = o._index; _cue = o._cue; _walls = o._walls;
        _jawX = o._jawX; _jawY = o._jawY; _maxSteps = o._maxSteps;
        _spawnX = o._spawnX; _spawnY = o._spawnY;                       // never mutated after construction
        _x = (float[])o._x.Clone(); _y = (float[])o._y.Clone();
        _vx = (float[])o._vx.Clone(); _vy = (float[])o._vy.Clone();
        _potted = (bool[])o._potted.Clone();
        _steps = o._steps; _settled = o._settled;
        _report = new ShotReport { firstContact = o._report.firstContact, cueScratched = o._report.cueScratched };
        foreach (var pot in o._report.pots) _report.pots.Add(pot);
    }

    static Wall[] BuildWalls(BilliardsSimParams p, out float[] jawX, out float[] jawY)
    {
        float cr = p.cornerPocketRadius, sr = p.sidePocketRadius;
        float w = FeltMapper.TableW, h = FeltMapper.TableH;
        var walls = new[]
        {
            new Wall { horizontal = true,  line = 0f, lo = cr,     hi = 1f - sr, inward =  1f },   // bottom, left of the side pocket
            new Wall { horizontal = true,  line = 0f, lo = 1f + sr, hi = w - cr, inward =  1f },   // bottom, right of it
            new Wall { horizontal = true,  line = h,  lo = cr,     hi = 1f - sr, inward = -1f },   // top, left
            new Wall { horizontal = true,  line = h,  lo = 1f + sr, hi = w - cr, inward = -1f },   // top, right
            new Wall { horizontal = false, line = 0f, lo = cr,     hi = h - cr,  inward =  1f },   // left
            new Wall { horizontal = false, line = w,  lo = cr,     hi = h - cr,  inward = -1f },   // right
        };

        // Every segment end is a jaw point: balls hit it as a circle against a point so they can't slip past the end.
        jawX = new float[walls.Length * 2];
        jawY = new float[walls.Length * 2];
        for (int i = 0; i < walls.Length; i++)
        {
            var wl = walls[i];
            jawX[i * 2]     = wl.horizontal ? wl.lo : wl.line;
            jawY[i * 2]     = wl.horizontal ? wl.line : wl.lo;
            jawX[i * 2 + 1] = wl.horizontal ? wl.hi : wl.line;
            jawY[i * 2 + 1] = wl.horizontal ? wl.line : wl.hi;
        }
        return walls;
    }

    /// What the simulation treats as cushion, in table space: the straight segments (the felt edge, where a ball's own
    /// edge bounces) and the jaw points at their ends. For the debug overlay only; reads nothing from a live sim.
    public static void GetTableGeometry(BilliardsSimParams p, List<(Vector2 a, Vector2 b)> cushions, List<Vector2> jaws)
    {
        var walls = BuildWalls(p, out float[] jawX, out float[] jawY);
        foreach (var w in walls)
            cushions.Add(w.horizontal ? (new Vector2(w.lo, w.line), new Vector2(w.hi, w.line))
                                      : (new Vector2(w.line, w.lo), new Vector2(w.line, w.hi)));
        for (int i = 0; i < jawX.Length; i++) jaws.Add(new Vector2(jawX[i], jawY[i]));
    }

    public int BallCount => _ids.Length;
    public string IdOf(int index) => _ids[index];
    public int IndexOf(string id) => id != null && _index.TryGetValue(id, out int i) ? i : -1;
    public Vector2 Position(int index) => new Vector2(_x[index], _y[index]);   // table units
    public Vector2 Velocity(int index) => new Vector2(_vx[index], _vy[index]);
    public bool IsPotted(int index) => _potted[index];
    public int  StepsThisShot => _steps;

    public bool IsMoving
    {
        get
        {
            for (int i = 0; i < _ids.Length; i++)
                if (!_potted[i] && (_vx[i] != 0f || _vy[i] != 0f)) return true;
            return false;
        }
    }

    /// The report for the shot in progress / just settled. A fresh ShotReport starts on every Shoot().
    public ShotReport Report => _report;

    /// shot must already be quantized (ShotInput.Quantized). Velocity = Direction * power * maxShotSpeed.
    public void Shoot(ShotInput shot)
    {
        _report = new ShotReport();
        _steps = 0;
        _settled = false;
        if (_potted[_cue]) { _settled = true; return; }           // no cue ball on the table: nothing to hit
        Vector2 d = shot.Direction;
        float speed = shot.power * _p.maxShotSpeed;
        _vx[_cue] = d.x * speed;
        _vy[_cue] = d.y * speed;
    }

    /// Advance exactly one FixedDt. Returns false once every ball is at rest (or the safety cap hit).
    public bool Step()
    {
        if (_settled) return false;
        _steps++;

        int n = _ids.Length;
        float r = _p.ballRadius;

        // 1. Move.
        for (int i = 0; i < n; i++)
        {
            if (_potted[i]) continue;
            _x[i] += _vx[i] * FixedDt;
            _y[i] += _vy[i] * FixedDt;
        }

        // 2. Rolling friction: speed drops linearly, direction is kept; below stopSpeed the ball is at rest.
        for (int i = 0; i < n; i++)
        {
            if (_potted[i]) continue;
            float speed = Sqrt(_vx[i] * _vx[i] + _vy[i] * _vy[i]);
            if (speed <= 0f) continue;
            float slowed = Math.Max(0f, speed - _p.rollingDecel * FixedDt);
            if (slowed < _p.stopSpeed) { _vx[i] = 0f; _vy[i] = 0f; }
            else { float k = slowed / speed; _vx[i] *= k; _vy[i] *= k; }
        }

        // 3. Ball against ball, pairs i < j. In a tight cluster, pushing one pair apart can push a ball into a third,
        // so the pass repeats (at most BallPasses times, stopping as soon as a pass finds no overlap). A pair that is
        // already separating gets no second impulse, so a plain two-ball hit is unchanged by the extra passes.
        float minDist = 2f * r;
        for (int pass = 0; pass < BallPasses; pass++)
        {
            bool touched = false;
            for (int i = 0; i < n; i++)
            {
                if (_potted[i]) continue;
                for (int j = i + 1; j < n; j++)
                {
                    if (_potted[j]) continue;
                    float dx = _x[j] - _x[i], dy = _y[j] - _y[i];
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= minDist * minDist) continue;
                    touched = true;

                    float d = Sqrt(d2);
                    float nx = 1f, ny = 0f;
                    if (d > 1e-6f) { nx = dx / d; ny = dy / d; }
                    float half = (minDist - d) * 0.5f;
                    _x[i] -= nx * half; _y[i] -= ny * half;
                    _x[j] += nx * half; _y[j] += ny * half;

                    float vrel = (_vx[i] - _vx[j]) * nx + (_vy[i] - _vy[j]) * ny;
                    if (vrel > 0f)                                   // approaching
                    {
                        float jImp = (1f + _p.ballRestitution) * 0.5f * vrel;
                        _vx[i] -= jImp * nx; _vy[i] -= jImp * ny;
                        _vx[j] += jImp * nx; _vy[j] += jImp * ny;
                    }

                    if (_report.firstContact == null)
                    {
                        if (i == _cue) _report.firstContact = _ids[j];
                        else if (j == _cue) _report.firstContact = _ids[i];
                    }
                }
            }
            if (!touched) break;
        }

        // 4. Cushions, then jaw points. A cushion hit never counts as first contact, so bank shots are legal.
        for (int i = 0; i < n; i++)
        {
            if (_potted[i]) continue;
            for (int w = 0; w < _walls.Length; w++) CollideWall(i, _walls[w]);
            for (int k = 0; k < _jawX.Length; k++) CollideJaw(i, _jawX[k], _jawY[k]);
        }

        // 4b. A cushion push can shove a ball back into a neighbour. Clean that up with POSITION-only corrections
        // (no impulses, so no energy changes), alternating with a position-only cushion clamp.
        for (int pass = 0; pass < SeparationPasses; pass++)
        {
            if (!SeparatePairs(minDist)) break;
            for (int i = 0; i < n; i++)
            {
                if (_potted[i]) continue;
                for (int w = 0; w < _walls.Length; w++) ClampWall(i, _walls[w]);
            }
        }

        // 5. Pockets: a ball whose centre is inside a pocket's radius drops. Same-step drops are appended in index order.
        for (int i = 0; i < n; i++)
        {
            if (_potted[i]) continue;
            for (int k = 0; k < AllPockets.Length; k++)
            {
                Pocket pk = AllPockets[k];
                Vector2 c = PocketCentre(pk);
                float rad = PocketRadius(pk);
                float dx = _x[i] - c.x, dy = _y[i] - c.y;
                if (dx * dx + dy * dy >= rad * rad) continue;

                _potted[i] = true;
                _vx[i] = 0f; _vy[i] = 0f;
                _report.pots.Add((_ids[i], pk));
                if (i == _cue) _report.cueScratched = true;
                break;
            }
        }

        // 6. Settle.
        if (!IsMoving) { _settled = true; return false; }
        if (_steps >= _maxSteps)                                     // safety cap: force-settle, never hang
        {
            for (int i = 0; i < n; i++) { _vx[i] = 0f; _vy[i] = 0f; }
            _settled = true;
            return false;
        }
        return true;
    }

    /// Headless: step until rest. For the verifier, tests and the Trick Shot preview.
    public ShotReport RunToRest()
    {
        while (Step()) { }
        return Report;
    }

    /// Put balls back: preset spot, or the nearest free spot searching outward in rings. Deterministic.
    public void ApplyRespots(IEnumerable<string> ids)
    {
        if (ids == null) return;
        foreach (var id in ids)
        {
            int i = IndexOf(id);
            if (i < 0 || !_potted[i]) continue;
            FindSpot(i, out float px, out float py);
            _x[i] = px; _y[i] = py;
            _vx[i] = 0f; _vy[i] = 0f;
            _potted[i] = false;
        }
    }

    void FindSpot(int self, out float px, out float py)
    {
        float r = _p.ballRadius;
        px = _spawnX[self]; py = _spawnY[self];
        if (IsFree(self, px, py)) return;

        for (int k = 1; k <= RespotRings; k++)
        {
            int count = 8 * k;
            double radius = k * 2.2 * r;
            for (int c = 0; c < count; c++)
            {
                double a = 2.0 * Math.PI * c / count;                // starts at angle 0, counter-clockwise
                float cx = _spawnX[self] + (float)(radius * Math.Cos(a));
                float cy = _spawnY[self] + (float)(radius * Math.Sin(a));
                if (IsFree(self, cx, cy)) { px = cx; py = cy; return; }
            }
        }
        // Nothing free within 12 rings (a pathological table): fall back to the preset spot.
        px = _spawnX[self]; py = _spawnY[self];
    }

    bool IsFree(int self, float px, float py)
    {
        float r = _p.ballRadius;
        if (px < r || px > FeltMapper.TableW - r || py < r || py > FeltMapper.TableH - r) return false;

        for (int k = 0; k < AllPockets.Length; k++)
        {
            Vector2 c = PocketCentre(AllPockets[k]);
            float rad = PocketRadius(AllPockets[k]);
            float dx = px - c.x, dy = py - c.y;
            if (dx * dx + dy * dy < rad * rad) return false;
        }

        float need = 2f * r + RespotEpsilon;
        for (int i = 0; i < _ids.Length; i++)
        {
            if (i == self || _potted[i]) continue;
            float dx = px - _x[i], dy = py - _y[i];
            if (dx * dx + dy * dy < need * need) return false;
        }
        return true;
    }

    /// Hash of every ball's position/velocity/potted flag. Determinism tests compare these. FNV-1a, 64 bit.
    public long StateHash()
    {
        const ulong prime = 1099511628211UL;
        ulong h = 14695981039346656037UL;
        for (int i = 0; i < _ids.Length; i++)
        {
            h = Mix(h, BitConverter.SingleToInt32Bits(_x[i]),  prime);
            h = Mix(h, BitConverter.SingleToInt32Bits(_y[i]),  prime);
            h = Mix(h, BitConverter.SingleToInt32Bits(_vx[i]), prime);
            h = Mix(h, BitConverter.SingleToInt32Bits(_vy[i]), prime);
            h = Mix(h, _potted[i] ? 1 : 0, prime);
        }
        return unchecked((long)h);
    }

    static ulong Mix(ulong h, int v, ulong prime)
    {
        for (int b = 0; b < 4; b++)
        {
            h ^= (byte)(v >> (8 * b));
            h *= prime;
        }
        return h;
    }

    /// Deep copy, for previews and robustness tests. Must not share mutable state.
    public BilliardsSim Clone() => new BilliardsSim(this);

    // ---- internals ----

    float PocketRadius(Pocket p) =>
        (p == Pocket.TM || p == Pocket.BM) ? _p.sidePocketRadius : _p.cornerPocketRadius;

    /// Pushes every overlapping pair apart by half the overlap each. Returns whether any pair needed it.
    bool SeparatePairs(float minDist)
    {
        bool any = false;
        int n = _ids.Length;
        for (int i = 0; i < n; i++)
        {
            if (_potted[i]) continue;
            for (int j = i + 1; j < n; j++)
            {
                if (_potted[j]) continue;
                float dx = _x[j] - _x[i], dy = _y[j] - _y[i];
                float d2 = dx * dx + dy * dy;
                if (d2 >= minDist * minDist) continue;

                float d = Sqrt(d2);
                float nx = 1f, ny = 0f;
                if (d > 1e-6f) { nx = dx / d; ny = dy / d; }
                float half = (minDist - d) * 0.5f;
                _x[i] -= nx * half; _y[i] -= ny * half;
                _x[j] += nx * half; _y[j] += ny * half;
                any = true;
            }
        }
        return any;
    }

    /// Cushion push-out without touching velocity.
    void ClampWall(int i, Wall w)
    {
        float along = w.horizontal ? _x[i] : _y[i];
        if (along < w.lo || along > w.hi) return;
        float across = w.horizontal ? _y[i] : _x[i];
        if ((across - w.line) * w.inward >= _p.ballRadius) return;
        if (w.horizontal) _y[i] = w.line + w.inward * _p.ballRadius; else _x[i] = w.line + w.inward * _p.ballRadius;
    }

    void CollideWall(int i, Wall w)
    {
        float along = w.horizontal ? _x[i] : _y[i];
        if (along < w.lo || along > w.hi) return;                    // past the segment end: the jaw point handles it

        float r = _p.ballRadius;
        float across = w.horizontal ? _y[i] : _x[i];
        float depth = (across - w.line) * w.inward;                  // distance from the cushion into the felt
        if (depth >= r) return;

        if (w.horizontal) _y[i] = w.line + w.inward * r; else _x[i] = w.line + w.inward * r;

        float vn = (w.horizontal ? _vy[i] : _vx[i]) * w.inward;
        if (vn >= 0f) return;
        float change = -(1f + _p.cushionRestitution) * vn * w.inward;
        if (w.horizontal) _vy[i] += change; else _vx[i] += change;
    }

    void CollideJaw(int i, float jx, float jy)
    {
        float r = _p.ballRadius;
        float dx = _x[i] - jx, dy = _y[i] - jy;
        float d2 = dx * dx + dy * dy;
        if (d2 >= r * r) return;

        float d = Sqrt(d2);
        if (d <= 1e-6f) return;                                      // exactly on the jaw point: no normal to push along
        float nx = dx / d, ny = dy / d;

        _x[i] = jx + nx * r;
        _y[i] = jy + ny * r;

        float vn = _vx[i] * nx + _vy[i] * ny;
        if (vn >= 0f) return;
        float k = -(1f + _p.cushionRestitution) * vn;
        _vx[i] += k * nx;
        _vy[i] += k * ny;
    }

    static float Sqrt(float v) => (float)Math.Sqrt(v);
}
