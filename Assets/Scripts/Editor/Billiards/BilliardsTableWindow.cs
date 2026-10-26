using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// Tools/OverTimer/Billiards/Table Editor: author a table without entering play mode.
///  - Layout: drag balls (snaps to 0.005 in preset space), right-click a ball to remove it, add balls by id, and edit
///    order / forbidden / called as text.
///  - Record: switch on "Shoot", pull back from anywhere on the felt and release; every quantized shot goes through the
///    real simulation and referee, and is appended to the solution. A faint ghost ball and path show where the cue ball
///    will first touch something.
///  - Solve: brute-force a solution with BilliardsSolver, then replay it here.
///  - Verify / Export: the same verifier the build runs; Export refuses unless the recorded shots win the table.
public class BilliardsTableWindow : EditorWindow
{
    const string Folder = "Assets/Resources/BilliardsPresets";
    const float Snap = 0.005f;

    BilliardsPreset _p;
    GameConfig _cfg;
    BilliardsSimParams _prm;
    string[] _files = Array.Empty<string>();
    int _fileIndex;

    string _orderText = "", _forbiddenText = "", _calledText = "", _newBallId = "";
    bool _shoot;
    BilliardsRound _round;
    readonly List<ShotInput> _shots = new List<ShotInput>();
    ShotResult _last;
    string _log = "";
    bool _won;
    Vector2 _scroll;

    int _drag = -1;
    bool _aiming;
    Vector2 _aimFrom, _aimTo;
    readonly List<List<Vector2>> _trails = new List<List<Vector2>>();

    int _solverMaxShots = 6, _solverBeam = 2;
    bool _solverBankOnly;

    [MenuItem("Tools/OverTimer/Billiards/Table Editor")]
    static void Open() => GetWindow<BilliardsTableWindow>("Billiards Table").Show();

    void OnEnable()
    {
        _cfg = EditorAssetFinder.FindSingle<GameConfig>();
        if (_cfg != null) _prm = BilliardsSimParams.FromConfig(_cfg);
        RefreshFiles();
        if (_p == null) NewTable();
    }

    // ================= files =================

    void RefreshFiles()
    {
        var list = new List<string>();
        if (Directory.Exists(Folder))
            foreach (var f in Directory.GetFiles(Folder, "*.json")) list.Add(Path.GetFileNameWithoutExtension(f));
        list.Sort(string.CompareOrdinal);
        _files = list.ToArray();
    }

    void NewTable()
    {
        _p = new BilliardsPreset
        {
            id = "new_table", name = "New Table", par = 1,
            balls = new[] { new BallSpawn { id = "cue", x = 0.25f, y = 0.5f }, new BallSpawn { id = "1", x = 0.7f, y = 0.5f } },
            order = new[] { "1" }, forbidden = new string[0], called = new CalledPocket[0], solution = new ShotInput[0],
        };
        AfterLoad();
    }

    void Load(string name)
    {
        var parsed = BilliardsPresetLibrary.Parse(File.ReadAllText($"{Folder}/{name}.json"), name);
        if (parsed == null) return;
        _p = parsed;
        AfterLoad();
        // show the stored solution so it can be replayed / extended
        foreach (var s in _p.solution ?? Array.Empty<ShotInput>()) _shots.Add(ShotInput.Quantized(s.angle, s.power));
        Replay();
    }

    void AfterLoad()
    {
        _orderText = string.Join(", ", _p.order ?? Array.Empty<string>());
        _forbiddenText = string.Join(", ", _p.forbidden ?? Array.Empty<string>());
        var sb = new StringBuilder();
        foreach (var c in _p.called ?? Array.Empty<CalledPocket>()) sb.Append(sb.Length > 0 ? ", " : "").Append(c.ball).Append(':').Append(c.pocket);
        _calledText = sb.ToString();
        _shots.Clear();
        ResetRound();
    }

    // ================= round =================

    /// The preset as currently typed in (order / forbidden / called parsed from their text boxes).
    BilliardsPreset Working()
    {
        var clone = JsonUtility.FromJson<BilliardsPreset>(JsonUtility.ToJson(_p));
        clone.order = Split(_orderText);
        clone.forbidden = Split(_forbiddenText);
        var called = new List<CalledPocket>();
        foreach (var part in Split(_calledText))
        {
            var bits = part.Split(':');
            if (bits.Length == 2) called.Add(new CalledPocket { ball = bits[0].Trim(), pocket = bits[1].Trim() });
        }
        clone.called = called.ToArray();
        return clone;
    }

    static string[] Split(string text)
    {
        var parts = new List<string>();
        foreach (var s in text.Split(',')) { var t = s.Trim(); if (t.Length > 0) parts.Add(t); }
        return parts.ToArray();
    }

    void ResetRound()
    {
        _last = null; _won = false; _trails.Clear(); _log = "";
        if (_cfg == null || _p.balls == null) { _round = null; return; }
        var w = Working();
        w.shots = new[] { 99, 99, 99 };                                        // no shot limit while authoring
        try { _round = new BilliardsRound(w, default, BilliardsDifficulty.Easy, _cfg.billiardsBetMax, _cfg); }
        catch (Exception e) { _round = null; _log = "Cannot build the table: " + e.Message; }
    }

    void Replay()
    {
        var shots = new List<ShotInput>(_shots);
        _shots.Clear();
        ResetRound();
        foreach (var s in shots) { if (_round == null || _round.IsOver) break; Fire(s); }
    }

    void Fire(ShotInput shot)
    {
        if (_round == null || _round.IsOver) return;
        _trails.Clear();
        for (int i = 0; i < _round.Sim.BallCount; i++) _trails.Add(new List<Vector2>());
        _round.Fire(shot);
        var sim = _round.Sim;
        int step = 0;
        while (sim.Step())
        {
            if (++step % 3 != 0) continue;
            for (int i = 0; i < sim.BallCount; i++) if (!sim.IsPotted(i)) _trails[i].Add(sim.Position(i));
        }
        _last = _round.Resolve();
        _shots.Add(shot);
        _won = _last.verdict == Verdict.Win;
        _log = $"shot {_shots.Count}: {shot.angle:0.0} deg, power {shot.power:0.00} -> " +
               $"{(_last.foul ? "FOUL  " : "")}popped [{string.Join(",", _last.popped)}] respot [{string.Join(",", _last.respot)}]  " +
               $"{_last.verdict}{(_last.failReason != null ? " (" + _last.failReason + ")" : "")}  next: {_round.Referee.Head ?? "-"}";
        Repaint();
    }

    // ================= GUI =================

    void OnGUI()
    {
        if (_cfg == null) { EditorGUILayout.HelpBox("GameConfig.asset not found.", MessageType.Error); return; }
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        using (new EditorGUILayout.HorizontalScope())
        {
            int pick = EditorGUILayout.Popup("Load", -1, _files);
            if (pick >= 0) { _fileIndex = pick; Load(_files[pick]); }
            if (GUILayout.Button("New", GUILayout.Width(60))) NewTable();
            if (GUILayout.Button("Refresh", GUILayout.Width(70))) RefreshFiles();
        }

        EditorGUI.BeginChangeCheck();
        _p.id = EditorGUILayout.TextField("Id (file name)", _p.id);
        _p.name = EditorGUILayout.TextField("Name", _p.name);
        _orderText = EditorGUILayout.TextField("Order (comma)", _orderText);
        _forbiddenText = EditorGUILayout.TextField("Forbidden (comma)", _forbiddenText);
        _calledText = EditorGUILayout.TextField("Called (ball:POCKET, ...)", _calledText);
        _p.payoutScale = EditorGUILayout.FloatField("Payout scale", _p.payoutScale);
        if (EditorGUI.EndChangeCheck()) { _shots.Clear(); ResetRound(); }

        DrawFelt();

        using (new EditorGUILayout.HorizontalScope())
        {
            bool shoot = GUILayout.Toggle(_shoot, _shoot ? "Mode: SHOOT (pull back to fire)" : "Mode: LAYOUT (drag balls)", "Button");
            if (shoot != _shoot) { _shoot = shoot; _aiming = false; }
            if (GUILayout.Button("Undo shot", GUILayout.Width(90)) && _shots.Count > 0) { _shots.RemoveAt(_shots.Count - 1); Replay(); }
            if (GUILayout.Button("Clear shots", GUILayout.Width(90))) { _shots.Clear(); ResetRound(); }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            _newBallId = EditorGUILayout.TextField("Add ball id", _newBallId);
            if (GUILayout.Button("Add", GUILayout.Width(60)) && _newBallId.Trim().Length > 0) AddBall(_newBallId.Trim());
        }

        EditorGUILayout.LabelField("Solution", _shots.Count == 0 ? "(no shots recorded)" : string.Join("  |  ", Describe()));
        if (!string.IsNullOrEmpty(_log)) EditorGUILayout.HelpBox(_log, _won ? MessageType.Info : MessageType.None);

        EditorGUILayout.Space();
        _solverMaxShots = EditorGUILayout.IntSlider("Solver: max shots", _solverMaxShots, 1, 8);
        _solverBeam = EditorGUILayout.IntSlider("Solver: beam", _solverBeam, 1, 4);
        _solverBankOnly = EditorGUILayout.Toggle("Solver: potting shots must bank", _solverBankOnly);
        if (GUILayout.Button("Solve (brute force)")) Solve();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Verify this table")) VerifyCurrent();
            using (new EditorGUI.DisabledScope(!_won))
                if (GUILayout.Button("Export to Resources/BilliardsPresets")) Export();
        }
        if (!_won) EditorGUILayout.HelpBox("Export is disabled until the recorded shots win the table.", MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    IEnumerable<string> Describe()
    {
        for (int i = 0; i < _shots.Count; i++) yield return $"{i + 1}: {_shots[i].angle:0.0}° @ {_shots[i].power:0.00}";
    }

    void AddBall(string id)
    {
        foreach (var b in _p.balls) if (b.id == id) { _log = $"ball '{id}' already exists"; return; }
        var list = new List<BallSpawn>(_p.balls) { new BallSpawn { id = id, x = 0.5f, y = 0.5f } };
        _p.balls = list.ToArray();
        _newBallId = "";
        _shots.Clear(); ResetRound();
    }

    void RemoveBall(int index)
    {
        if (_p.balls[index].id == "cue") return;
        var list = new List<BallSpawn>(_p.balls);
        list.RemoveAt(index);
        _p.balls = list.ToArray();
        _shots.Clear(); ResetRound();
    }

    // ================= felt =================

    Rect _felt;

    Vector2 ToGui(Vector2 table) => new Vector2(_felt.x + table.x / FeltMapper.TableW * _felt.width, _felt.yMax - table.y / FeltMapper.TableH * _felt.height);
    Vector2 ToTable(Vector2 gui) => new Vector2((gui.x - _felt.x) / _felt.width * FeltMapper.TableW, (_felt.yMax - gui.y) / _felt.height * FeltMapper.TableH);

    void DrawFelt()
    {
        float w = Mathf.Min(position.width - 24f, 800f);
        Rect area = GUILayoutUtility.GetRect(w, w / 2f + 4f);
        _felt = new Rect(area.x + 2f, area.y + 2f, w - 4f, (w - 4f) / 2f);
        float scale = _felt.width / FeltMapper.TableW;                           // gui pixels per table unit

        EditorGUI.DrawRect(new Rect(_felt.x - 6, _felt.y - 6, _felt.width + 12, _felt.height + 12), new Color32(130, 90, 40, 255));
        EditorGUI.DrawRect(_felt, new Color32(0, 120, 60, 255));

        Handles.BeginGUI();
        foreach (Pocket pk in Enum.GetValues(typeof(Pocket)))
        {
            bool side = pk == Pocket.TM || pk == Pocket.BM;
            Handles.color = new Color(0f, 0.05f, 0.03f, 0.85f);
            Handles.DrawSolidDisc(ToGui(BilliardsSim.PocketCentre(pk)), Vector3.forward, (side ? _prm.sidePocketRadius : _prm.cornerPocketRadius) * scale);
        }

        // trails of the last shot
        Handles.color = new Color(1f, 1f, 1f, 0.35f);
        foreach (var trail in _trails)
            for (int i = 1; i < trail.Count; i++) Handles.DrawLine(ToGui(trail[i - 1]), ToGui(trail[i]));

        // balls: from the live round when there is one, otherwise from the layout
        int n = _p.balls.Length;
        for (int i = 0; i < n; i++)
        {
            var b = _p.balls[i];
            Vector2 pos = new Vector2(b.x * FeltMapper.TableW, b.y * FeltMapper.TableH);
            bool potted = false;
            if (_round != null && _shots.Count > 0)
            {
                int k = _round.Sim.IndexOf(b.id);
                if (k >= 0) { potted = _round.Sim.IsPotted(k); pos = _round.Sim.Position(k); }
            }
            if (potted) continue;
            Vector2 g = ToGui(pos);
            Handles.color = BallColor(b.id);
            Handles.DrawSolidDisc(g, Vector3.forward, _prm.ballRadius * scale);
            Handles.color = Color.black;
            Handles.DrawWireDisc(g, Vector3.forward, _prm.ballRadius * scale);
            GUI.Label(new Rect(g.x - 12, g.y - 9, 24, 18), b.id == "cue" ? "" : b.id, CenteredLabel());
        }

        if (_aiming) DrawAim();
        Handles.EndGUI();

        HandleInput();
    }

    static GUIStyle _centered;
    static GUIStyle CenteredLabel()
    {
        if (_centered == null) _centered = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        return _centered;
    }

    static Color BallColor(string id)
    {
        if (id == "cue") return Color.white;
        if (!int.TryParse(id, out int n)) return Color.gray;
        Color[] solids = { new Color32(240, 200, 0, 255), new Color32(30, 70, 200, 255), new Color32(200, 40, 40, 255), new Color32(110, 40, 150, 255),
                           new Color32(235, 120, 20, 255), new Color32(20, 130, 50, 255), new Color32(130, 30, 30, 255), new Color32(20, 20, 20, 255) };
        Color c = solids[(n - 1) % 8];
        return n > 8 ? Color.Lerp(c, Color.white, 0.35f) : c;
    }

    void HandleInput()
    {
        var e = Event.current;
        if (_round == null) return;
        float scale = _felt.width / FeltMapper.TableW;

        if (!_shoot)
        {
            if (e.type == EventType.MouseDown && _felt.Contains(e.mousePosition))
            {
                _drag = -1;
                for (int i = 0; i < _p.balls.Length; i++)
                {
                    var b = _p.balls[i];
                    Vector2 g = ToGui(new Vector2(b.x * FeltMapper.TableW, b.y * FeltMapper.TableH));
                    if (Vector2.Distance(g, e.mousePosition) > _prm.ballRadius * scale) continue;
                    if (e.button == 1) RemoveBall(i); else _drag = i;
                    e.Use();
                    break;
                }
            }
            else if (e.type == EventType.MouseDrag && _drag >= 0 && _drag < _p.balls.Length)
            {
                Vector2 t = ToTable(e.mousePosition);
                _p.balls[_drag].x = Mathf.Clamp(Mathf.Round(t.x / FeltMapper.TableW / Snap) * Snap, 0f, 1f);
                _p.balls[_drag].y = Mathf.Clamp(Mathf.Round(t.y / FeltMapper.TableH / Snap) * Snap, 0f, 1f);
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && _drag >= 0)
            {
                _drag = -1;
                _shots.Clear();
                ResetRound();
                e.Use();
            }
            return;
        }

        // shoot mode: pull back from anywhere
        if (_round.IsOver) return;
        if (e.type == EventType.MouseDown && e.button == 0 && _felt.Contains(e.mousePosition)) { _aiming = true; _aimTo = e.mousePosition; e.Use(); }
        else if (e.type == EventType.MouseDrag && _aiming) { _aimTo = e.mousePosition; e.Use(); Repaint(); }
        else if (e.type == EventType.MouseDown && e.button == 1) { _aiming = false; e.Use(); Repaint(); }
        else if (e.type == EventType.MouseUp && _aiming && e.button == 0)
        {
            _aiming = false;
            e.Use();
            if (TryAim(e.mousePosition, out ShotInput shot, out _)) Fire(shot);
        }
    }

    bool TryAim(Vector2 mouse, out ShotInput shot, out Vector2 cueGui)
    {
        shot = null; cueGui = default;
        int cue = _round.Sim.IndexOf("cue");
        if (cue < 0 || _round.Sim.IsPotted(cue)) return false;
        cueGui = ToGui(_round.Sim.Position(cue));
        Vector2 pull = cueGui - mouse;
        if (pull.magnitude < 1f) return false;
        float power = Mathf.Clamp01(pull.magnitude / (_felt.height * 0.6f));
        if (power < 0.05f) return false;
        shot = ShotInput.Quantized(Mathf.Atan2(-pull.y, pull.x) * Mathf.Rad2Deg, power);   // gui y points down
        return true;
    }

    void DrawAim()
    {
        if (!TryAim(_aimTo, out ShotInput shot, out Vector2 cueGui)) return;
        float scale = _felt.width / FeltMapper.TableW;
        Vector2 dir = shot.Direction;
        Handles.color = new Color(1f, 1f, 1f, 0.9f);
        if (AimPredictor.Predict(_round.Sim, dir, out AimPrediction pr))
        {
            Vector2 end = ToGui(pr.end);
            Handles.DrawDottedLine(cueGui, end, 4f);
            Handles.color = new Color(1f, 1f, 1f, 0.35f);
            Handles.DrawSolidDisc(end, Vector3.forward, _prm.ballRadius * scale);
        }
        Handles.color = Color.yellow;
        Handles.DrawLine(cueGui, _aimTo);
        GUI.Label(new Rect(_aimTo.x + 8, _aimTo.y - 8, 140, 18), $"{shot.angle:0.0}°  power {shot.power:0.00}", EditorStyles.whiteBoldLabel);
    }

    // ================= solve / verify / export =================

    void Solve()
    {
        var w = Working();
        if (!BilliardsPresetValidator.Validate(w, _cfg.billiardsParOffsets, _prm)) { _log = "The table has problems (see the console warnings)."; return; }
        var options = new BilliardsSolver.Options
        {
            maxShots = _solverMaxShots, beam = _solverBeam, bankOnly = _solverBankOnly,
            log = msg => EditorUtility.DisplayProgressBar("Billiards solver", msg, 0.5f),
        };
        List<ShotInput> found;
        string notes;
        try { found = BilliardsSolver.Solve(w, _cfg, options, out notes); }
        finally { EditorUtility.ClearProgressBar(); }

        if (found == null) { _log = notes; return; }
        _shots.Clear();
        _shots.AddRange(found);
        Replay();
        _log = $"Solver found {found.Count} shot(s):\n{notes}";
    }

    BilliardsPreset ExportCopy()
    {
        var copy = Working();
        copy.par = _shots.Count;
        copy.solution = _shots.ToArray();
        foreach (var b in copy.balls) { b.x = Round4(b.x); b.y = Round4(b.y); }
        if (copy.shots != null && copy.shots.Length == 3 && copy.shots[2] > 0 && copy.shots[2] < copy.par) copy.shots[2] = 0;   // hard may not be below par
        return copy;
    }

    static float Round4(float v) => (float)Math.Round(v, 4);

    void VerifyCurrent()
    {
        if (_shots.Count == 0) { _log = "Record or solve a solution first."; return; }
        var copy = ExportCopy();
        var row = BilliardsPresetVerifier.VerifyPreset(copy, _cfg);
        string report = BilliardsPresetVerifier.Report(new List<BilliardsPresetVerifier.Row> { row }, out _, out _, out _, out _);
        _log = "Verify:\n" + report;
        Debug.Log("[Billiards] Table editor verify\n" + report);
    }

    void Export()
    {
        if (!_won) { _log = "Refusing to export: the recorded shots do not win the table."; return; }
        if (string.IsNullOrWhiteSpace(_p.id)) { _log = "Give the table an id first."; return; }
        var copy = ExportCopy();
        var row = BilliardsPresetVerifier.VerifyPreset(copy, _cfg);
        if (!row.pass) { _log = "Refusing to export: the verifier fails this table: " + row.reason; return; }

        string path = BilliardsPresetJson.Write(copy);
        RefreshFiles();
        _log = $"Exported {path} (par {copy.par}, robust {row.robust * 100:0}%{(row.knifeEdge ? ", KNIFE-EDGE" : "")}).";
        Debug.Log("[Billiards] " + _log);
    }
}
