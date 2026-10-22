using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// The Billiards laptop app: draw a pre-made table, pick a bet and a difficulty, pot the balls in order within the
/// shot limit. This class is the state machine and the money; the round, rules and physics are pure C# (BilliardsRound).
///
/// State flow:  Racked -> Aiming -> Rolling -> (Judging) -> Aiming | Won | Lost -> Racked
///
/// Leaving the app:
///  - Racked / Aiming: the table stays exactly as it is (phone calls and tax bills can only be handled from the desk,
///    so a round must survive a short trip away).
///  - Rolling: the shot is already out, so leaving forfeits the bet. No free reset after a miss.
///  - Won / Lost: the finished table is cleared, the next visit draws a new one.
///
/// Services are read through .Instance on use, never through serialized references.
/// </summary>
public class BilliardsApp : LaptopApp
{
    private enum State { Racked, Aiming, Rolling, Won, Lost }

    private const int MaxStepsPerFrame = 8;

    [Header("Table")]
    [SerializeField] private RectTransform felt;                 // exactly 2:1, anchored and pivoted bottom-left
    [SerializeField] private RectTransform ballsRoot;
    [SerializeField] private BallView ballTemplate;              // inactive template; cloned per ball id
    [SerializeField] private BallSpriteSet ballSprites;
    [SerializeField] private TableView tableView;                // rails, felt look and the pocket holes
    [SerializeField] private CueAimController aim;
    [SerializeField] private BilliardsDebugOverlay debugOverlay; // test only: shown by the cheat menu
    [SerializeField] private float ballArtPixel = 1f;            // balls are drawn on whole multiples of this many canvas units

    [Header("HUD")]
    [SerializeField] private RectTransform layoutRoot;           // rebuilt once on first open so the felt has its real size
    [SerializeField] private TMP_Text infoLabel;
    [SerializeField] private BallQueueView queueView;
    [SerializeField] private TMP_Text statusLabel;

    [Header("Playing bar")]
    [SerializeField] private GameObject playPanel;
    [SerializeField] private TMP_Text shotsLabel;
    [SerializeField] private TMP_Text betInfoLabel;

    [Header("Racked controls")]
    [SerializeField] private GameObject rackedPanel;
    [SerializeField] private Button betMinusButton;
    [SerializeField] private Button betPlusButton;
    [SerializeField] private TMP_Text betLabel;
    [SerializeField] private Button[] difficultyButtons;         // easy, medium, hard
    [SerializeField] private TMP_Text[] difficultyLabels;

    [Header("Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultLabel;
    [SerializeField] private Button nextTableButton;

    private GameConfig _cfg;
    private bool _initialized;
    private bool _available;

    private State _state = State.Racked;
    private BilliardsPreset _preset;
    private TableMirror _mirror;                                 // what is drawn: the rolled mirror, or the one a cheat forces
    private TableMirror _rolledMirror;
    private BilliardsSim _previewSim;                           // draws the racked table before any round exists
    private BilliardsRound _round;
    private FeltMapper _mapper;
    private string _lastPlayedId;
    private string _notice;
    private string _shotNote;                                    // what a card did to the shot in flight (cue slip), shown with the result
    private float _shownMultiplier = 1f;                         // the cost multiplier the racked screen was last drawn with
    private NoticeKind _noticeKind;
    private bool _noticeFresh;                                  // the status line just changed: give it a little punch
    private IReadOnlyList<string> _pendingPopped, _pendingRespot; // what the last shot did, handed to the queue strip once
    private readonly HashSet<string> _pocketed = new HashSet<string>();
    private Color _statusBase, _shotsBase;

    private enum NoticeKind { Info, Good, Bad }

    [Header("Feedback")]
    [SerializeField] private Color goodColor = new Color32(40, 140, 60, 255);
    [SerializeField] private Color badColor = new Color32(190, 45, 35, 255);

    // Sound hooks: no clips yet. Whoever adds audio subscribes to these; nothing else needs to change.
    public event Action<string> OnPocket;                        // a ball dropped (its id), as it happens during the roll
    public event Action<int> OnWin;                              // the table was cleared (profit)
    public event Action<string> OnFail;                          // the table was lost (reason)
    public event Action<string> OnFoul;                          // a foul that did not end the round (the message shown)

    private int _bet;
    private BilliardsDifficulty _difficulty;
    private int _charge;                                         // what the round actually cost (bet x cost modifier)
    private int _payoutOnWin;                                    // computed from the base bet at Play time
    private string _headBeforeShot;
    private float _acc;

    private readonly Dictionary<string, BallView> _views = new Dictionary<string, BallView>();

    // ---- read-only surface for tests and later phases ----
    public string StateName => _state.ToString();
    public BilliardsRound Round => _round;
    public BilliardsPreset Preset => _preset;
    public int Bet => _bet;
    public int Charge => _charge;
    public int PayoutOnWin => _payoutOnWin;

    // ================= LaptopApp =================

    public override void OnAppOpened()
    {
        EnsureInitialized();
        if (!_available) { ShowUnavailable(); return; }

        var money = MoneyService.Instance;
        if (money != null) { money.OnMoneyChanged -= HandleMoneyChanged; money.OnMoneyChanged += HandleMoneyChanged; }
        BilliardsDebug.Changed -= HandleDebugChanged;
        BilliardsDebug.Changed += HandleDebugChanged;

        if (_preset == null) DrawTable();
        else BindTable();                                        // coming back to a kept table
        ShowState();
        ApplyOverlay();
    }

    public override void OnAppClosed()
    {
        var money = MoneyService.Instance;
        if (money != null) money.OnMoneyChanged -= HandleMoneyChanged;
        BilliardsDebug.Changed -= HandleDebugChanged;
        if (aim != null) aim.Cancel();
        if (!_initialized) return;

        switch (_state)
        {
            case State.Rolling:
                // The shot is already out: walking away now would be a free reset after a miss.
                _notice = "You left mid-shot. That bet is lost.";
                _noticeKind = NoticeKind.Bad;
                BilliardsRoundLog.End(BilliardsRoundLog.Outcome.Forfeit, 0);
                ClearTable();
                break;
            case State.Won:
            case State.Lost:
                ClearTable();
                break;
            // Racked and Aiming keep the table exactly as it is.
        }
    }

    private void OnDestroy() => BilliardsRoundLog.End(BilliardsRoundLog.Outcome.Abandoned, 0);   // Restart with a round on the table

    private void OnDisable()
    {
        var money = MoneyService.Instance;
        if (money != null) money.OnMoneyChanged -= HandleMoneyChanged;
        BilliardsDebug.Changed -= HandleDebugChanged;
        if (statusLabel != null)                                  // no tween left running on a hidden app
        {
            DOTween.Kill(statusLabel.rectTransform);
            statusLabel.rectTransform.localScale = Vector3.one;
        }
    }

    // ================= setup =================

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        var gm = GameManager.Instance;
        _cfg = gm != null ? gm.GameConfig : null;
        string problem = null;
        _available = _cfg != null && _cfg.ValidateBilliards(out problem);
        if (_cfg != null && !_available) Debug.LogWarning("[Billiards] unavailable: " + problem);
        if (!_available) return;

        if (statusLabel != null) _statusBase = statusLabel.color;
        if (shotsLabel != null) _shotsBase = shotsLabel.color;
        _bet = _cfg.billiardsBetMin;
        if (betMinusButton != null) betMinusButton.onClick.AddListener(() => ChangeBet(-_cfg.billiardsBetStep));
        if (betPlusButton != null) betPlusButton.onClick.AddListener(() => ChangeBet(_cfg.billiardsBetStep));
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            int d = i;
            difficultyButtons[i].onClick.AddListener(() => PlayRound(d));
        }
        if (nextTableButton != null) nextTableButton.onClick.AddListener(NextTable);
        if (aim != null) aim.Fired += Shoot;

        if (ballTemplate != null) ballTemplate.gameObject.SetActive(false);
        if (layoutRoot != null) LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);   // felt size must be real before it is measured
        var simParams = BilliardsSimParams.FromConfig(_cfg);
        if (tableView != null) tableView.Layout(simParams, felt.rect.width);
        if (debugOverlay != null) debugOverlay.Build(simParams, felt.rect.width);
    }

    private void ShowUnavailable()
    {
        if (statusLabel != null) statusLabel.text = "Billiards unavailable";
        if (rackedPanel != null) rackedPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (aim != null) aim.SetActive(false);
    }

    // ================= tables =================

    private void DrawTable()
    {
        var rng = RngService.Instance;
        if (rng == null) { _preset = null; return; }
        var drawn = BilliardsPresetLibrary.Draw(rng.Random, _lastPlayedId, BilliardsDebug.PresetId);
        _preset = drawn.preset;
        _rolledMirror = drawn.mirror;
        _mirror = BilliardsDebug.Apply(_rolledMirror);
        _round = null;
        _state = State.Racked;
        _difficulty = BilliardsDifficulty.Medium;
        if (_preset != null) _previewSim = new BilliardsSim(_preset, BilliardsSimParams.FromConfig(_cfg));
        BindTable();
    }

    private void BindTable()
    {
        if (_preset == null) return;
        _mapper = new FeltMapper(felt.rect.width, _mirror);
        float diameter = Mathf.Round(_cfg.billiardsBallRadius * felt.rect.width);   // 2r table units x (feltWidth / 2) per unit; whole pixels keep the sprite crisp
        if (aim != null)
        {
            aim.Configure(_mapper, () => ActiveSim, diameter);
            aim.SetAssist(_cfg.billiardsAssist[(int)_difficulty]);                  // the assist follows the difficulty of the round
        }
        var wanted = new HashSet<string>();
        foreach (var b in _preset.balls)
        {
            wanted.Add(b.id);
            if (!_views.TryGetValue(b.id, out BallView view) || view == null)
            {
                view = Instantiate(ballTemplate, ballsRoot);
                _views[b.id] = view;
            }
            view.Setup(b.id, ballSprites != null ? ballSprites.Get(b.id) : null, diameter);
        }
        foreach (var kv in _views) if (!wanted.Contains(kv.Key) && kv.Value != null) kv.Value.Hide();
        _ballCentre = diameter * 0.5f;
        RefreshBalls(false);
    }

    private BilliardsSim ActiveSim => _round != null ? _round.Sim : _previewSim;

    /// animate: play the pot and re-spot tweens for balls whose state just changed. False when the whole table is being
    /// (re)drawn: a new table, or coming back to a kept one.
    private void RefreshBalls(bool animate)
    {
        var sim = ActiveSim;
        if (sim == null) return;
        for (int i = 0; i < sim.BallCount; i++)
        {
            if (!_views.TryGetValue(sim.IdOf(i), out BallView view) || view == null) continue;
            bool potted = sim.IsPotted(i);
            if (potted)
            {
                if (animate && _pocketed.Add(sim.IdOf(i))) OnPocket?.Invoke(sim.IdOf(i));
                view.Sync(true, Vector2.zero, NearestPocketFelt(view.FeltPosition), animate);
                continue;
            }

            // The sim keeps its exact position; only the DRAWN position is snapped, so balls settle onto whole art pixels
            // instead of shimmering between them as they slow down. Corner-based so even and odd diameters both stay crisp.
            Vector2 corner = _mapper.TableToFelt(sim.Position(i)) - Vector2.one * _ballCentre;
            view.Sync(false, FeltMapper.Snap(corner, ballArtPixel) + Vector2.one * _ballCentre, Vector2.zero, animate);
        }
        if (debugOverlay != null && BilliardsDebug.Overlay) debugOverlay.Refresh(sim, _mapper);
    }

    private float _ballCentre;                                   // half the drawn ball diameter

    private static readonly Pocket[] AllPockets = { Pocket.TL, Pocket.TM, Pocket.TR, Pocket.BL, Pocket.BM, Pocket.BR };

    /// Where a ball that just dropped rolls to: the pocket nearest to where it was last drawn.
    private Vector2 NearestPocketFelt(Vector2 from)
    {
        Vector2 best = from;
        float bestSqr = float.MaxValue;
        foreach (var pk in AllPockets)
        {
            Vector2 c = _mapper.TableToFelt(BilliardsSim.PocketCentre(pk));
            float d = (c - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = c; }
        }
        return best;
    }

    private void ClearTable()
    {
        if (_preset != null) _lastPlayedId = _preset.id;
        _preset = null;
        _round = null;
        _previewSim = null;
        _state = State.Racked;
        foreach (var kv in _views) if (kv.Value != null) kv.Value.Hide();
    }

    // ================= test switches (cheat menu) =================

    private void HandleDebugChanged()
    {
        ApplyOverlay();
        if (!_available || _preset == null) return;
        // A forced mirror takes effect on a table nobody has played yet; a round in progress keeps what it was dealt.
        string forced = BilliardsDebug.PresetId;
        if (_state == State.Racked && !string.IsNullOrEmpty(forced) && _preset.id != forced)
        {
            ClearTable();                                          // a forced table replaces an unplayed one
            DrawTable();
            ShowState();
            return;
        }
        TableMirror wanted = BilliardsDebug.Apply(_rolledMirror);
        if (_state == State.Racked && (wanted.flipX != _mirror.flipX || wanted.flipY != _mirror.flipY))
        {
            _mirror = wanted;
            BindTable();
            ShowState();
        }
    }

    private void ApplyOverlay()
    {
        if (debugOverlay == null) return;
        debugOverlay.SetVisible(BilliardsDebug.Overlay);
        if (BilliardsDebug.Overlay && ActiveSim != null) debugOverlay.Refresh(ActiveSim, _mapper);
    }

    private void NextTable()
    {
        ClearTable();
        DrawTable();
        ShowState();
    }

    // ================= money / starting a round =================

    private float CostMultiplier()
    {
        var svc = CostModifierService.Instance;
        return svc != null ? svc.GetMultiplier(CostModifierService.Target.Billiards) : 1f;
    }

    private int ChargeFor(int bet) => Mathf.RoundToInt(bet * CostMultiplier());

    /// The biggest bet the player can pay for: not above the table max, and the charge must be affordable.
    private int MaxAffordableBet()
    {
        var money = MoneyService.Instance;
        int have = money != null ? money.Current : 0;
        int best = _cfg.billiardsBetMin - 1;
        for (int b = _cfg.billiardsBetMin; b <= _cfg.billiardsBetMax; b += _cfg.billiardsBetStep)
            if (ChargeFor(b) <= have) best = b;
        return best;                                              // below betMin means "can't afford even the smallest bet"
    }

    private void ChangeBet(int delta)
    {
        int cap = Mathf.Max(_cfg.billiardsBetMin, Mathf.Min(_cfg.billiardsBetMax, MaxAffordableBet()));
        _bet = Mathf.Clamp(_bet + delta, _cfg.billiardsBetMin, cap);
        RefreshRacked();
    }

    /// Play button for difficulty 0 (easy), 1 (medium) or 2 (hard).
    public void PlayRound(int difficultyIndex)
    {
        if (_state != State.Racked || _preset == null || !_available) return;
        if (IsGameOver()) return;

        var money = MoneyService.Instance;
        if (money == null) return;

        _difficulty = (BilliardsDifficulty)Mathf.Clamp(difficultyIndex, 0, 2);
        int cap = MaxAffordableBet();
        if (cap < _cfg.billiardsBetMin) { RefreshRacked(); return; }                        // can't afford the smallest bet
        _bet = Mathf.Clamp(_bet, _cfg.billiardsBetMin, cap);

        int charge = ChargeFor(_bet);
        if (!money.TrySpend(charge, "billiards")) { RefreshRacked(); return; }

        _charge = charge;
        _payoutOnWin = BilliardsPayout.OnWin(_bet, _difficulty, _preset, _cfg);             // from the BASE bet, not the charge
        // Chalk Up cards: taken only now, after the bet was really paid, so a refused Play never uses one up. The payout
        // stays the one for the difficulty's normal shots: extra shots are the card's gift, not a cheaper table.
        int extraShots = BilliardsPendingEffects.TakeExtraShots();
        _round = new BilliardsRound(_preset, _mirror, _difficulty, _bet, _cfg, extraShots);
        _notice = extraShots > 0 ? $"Chalk Up: +{extraShots} extra {(extraShots == 1 ? "shot" : "shots")}" : null;
        _noticeKind = NoticeKind.Good;
        _noticeFresh = extraShots > 0;
        _shotNote = null;
        BilliardsRoundLog.Begin(_preset.id, _difficulty.ToString(), _bet, charge, _round.ShotsLeft);
        BilliardsRoundLog.AimStarted();
        _state = State.Aiming;
        BindTable();
        ShowState();
    }

    private bool IsGameOver() => GameManager.Instance != null && GameManager.Instance.IsGameOver;

    private void HandleMoneyChanged(int current, string reason)
    {
        if (_state == State.Racked && _available && gameObject.activeInHierarchy) RefreshRacked();
    }

    // ================= shooting =================

    /// Called by the aim controller (or a test) with an already-quantized shot.
    public void Shoot(ShotInput shot)
    {
        if (_state != State.Aiming || _round == null || IsGameOver()) return;
        if (_round.ShotsLeft <= 0) return;

        // Card effects that act on the shot itself. Cue Slip changes the power BEFORE quantizing (random draw from the
        // run's seeded RngService), so the simulation still receives one exact, replayable input.
        _shotNote = null;
        float slip = BilliardsPendingEffects.TakeCueSlip();
        if (slip > 0f)
        {
            var rng = RngService.Instance;
            float factor = 1f + (rng != null ? (float)(rng.Random.NextDouble() * 2.0 - 1.0) : 0f) * slip;
            var slipped = ShotInput.Quantized(shot.angle, shot.power * factor);
            _shotNote = $"Cue slip! power {shot.power:0.00} became {slipped.power:0.00}";
            shot = slipped;
        }
        BilliardsPendingEffects.TakeTrickShot();                                    // the preview was available for this shot: used up

        _headBeforeShot = _round.Referee.Head;
        _round.Fire(shot);
        BilliardsRoundLog.ShotFired();
        _pocketed.Clear();
        _acc = 0f;
        _state = State.Rolling;
        if (aim != null) aim.SetActive(false);
        ShowState();
    }

    private void Update()
    {
        if (!_initialized || !_available) return;
        if (IsGameOver())                                         // freeze: no input, no money, nothing moves
        {
            if (aim != null) aim.SetActive(false);
            BilliardsRoundLog.End(BilliardsRoundLog.Outcome.Abandoned, 0);
            return;
        }
        // A cost-modifier card can arrive or run out while the racked screen is open: redraw the price when it changes.
        if (_state == State.Racked && _preset != null && !Mathf.Approximately(CostMultiplier(), _shownMultiplier)) RefreshRacked();

        if (_state != State.Rolling || _round == null) return;
        if (LaptopController.IsLocked) return;                    // a paused call holds the Laptop: the shot freezes mid-roll

        _acc += Time.deltaTime;
        int steps = 0;
        bool settled = false;
        while (_acc >= BilliardsSim.FixedDt && steps < MaxStepsPerFrame)
        {
            _acc -= BilliardsSim.FixedDt;
            steps++;
            if (!_round.Sim.Step()) { settled = true; break; }
        }
        _acc = Mathf.Min(_acc, MaxStepsPerFrame * BilliardsSim.FixedDt);   // a slow frame just plays out slower on screen
        RefreshBalls(true);
        if (settled) Judge();
    }

    private void Judge()
    {
        var report = _round.Sim.Report;
        ShotResult result = _round.Resolve();
        string message = DescribeShot(result, report);
        RefreshBalls(true);                                       // the re-spots from Resolve() pop back in
        _pendingPopped = result.popped;                           // the queue strip slides these out / shakes the re-spots
        _pendingRespot = result.respot;
        _noticeKind = result.foul || (result.popped.Count == 0 && result.respot.Count > 0) ? NoticeKind.Bad
                    : result.popped.Count > 0 ? NoticeKind.Good : NoticeKind.Info;

        if (result.verdict == Verdict.Win)
        {
            if (IsGameOver()) return;
            int refund = Mathf.Min(_charge, _payoutOnWin);        // the stake comes back untaxed; only the profit is income
            int profit = _payoutOnWin - refund;
            var money = MoneyService.Instance;
            if (money != null)
            {
                money.Add(refund, MoneyService.StakeRefundReason);
                money.Add(profit, "billiards");
            }
            _state = State.Won;
            if (resultLabel != null) resultLabel.text = $"TABLE CLEARED!\n+${profit} profit  (${_payoutOnWin} back)";
            OnWin?.Invoke(profit);
            BilliardsRoundLog.End(BilliardsRoundLog.Outcome.Win, _payoutOnWin);
        }
        else if (result.verdict == Verdict.Fail)
        {
            _state = State.Lost;
            if (resultLabel != null) resultLabel.text = $"TABLE LOST\n{result.failReason}";
            OnFail?.Invoke(result.failReason);
            BilliardsRoundLog.End(BilliardsRoundLog.Outcome.Lost, 0);
        }
        else
        {
            _state = State.Aiming;
            if (result.foul) OnFoul?.Invoke(message);
            BilliardsRoundLog.ShotJudged();
        }
        if (_shotNote != null)                                    // a card changed this shot: say so, whatever happened
        {
            if ((_state == State.Won || _state == State.Lost) && resultLabel != null) resultLabel.text += "\n" + _shotNote;
            message = string.IsNullOrEmpty(message) ? _shotNote : _shotNote + "  ·  " + message;
            if (_noticeKind == NoticeKind.Info) _noticeKind = NoticeKind.Bad;
            _shotNote = null;
        }
        _notice = message;
        _noticeFresh = !string.IsNullOrEmpty(message);
        ShowState();
    }

    private string DescribeShot(ShotResult result, ShotReport report)
    {
        if (result.verdict != Verdict.Continue) return null;
        if (result.foul)
        {
            if (report.cueScratched) return "Foul: scratch";
            if (report.firstContact == null) return "Foul: touched nothing";
            return $"Foul: hit {report.firstContact} first (needed {_headBeforeShot})";
        }
        string back = RespotMessage(result, report);
        if (result.popped.Count > 0) return "Potted " + string.Join(", ", result.popped) + (back != null ? "  ·  " + back : "");
        return back ?? "No pot";
    }

    /// Why balls are going back: the pocket was not the called one, or the ball simply was not next. Null when nothing goes back.
    private string RespotMessage(ShotResult result, ShotReport report)
    {
        if (result.respot.Count == 0) return null;
        var wrongPocket = new List<string>();
        var outOfTurn = new List<string>();
        foreach (string id in result.respot)
        {
            if (id == "cue") continue;
            bool called = false, wrong = false;
            if (_preset.called != null)
                foreach (var c in _preset.called)
                    if (c.ball == id) { called = true; break; }
            if (called)
                foreach (var pot in report.pots)
                    if (pot.Item1 == id && Enum.TryParse(CalledPocketName(id), out Pocket want) && pot.Item2 != want) wrong = true;
            (wrong ? wrongPocket : outOfTurn).Add(id);
        }
        if (wrongPocket.Count > 0) return $"{string.Join(", ", wrongPocket)} went in the wrong pocket, back on the table";
        if (outOfTurn.Count > 0) return $"{string.Join(", ", outOfTurn)} went in out of turn, back on the table";
        return null;
    }

    private string CalledPocketName(string ballId)
    {
        foreach (var c in _preset.called) if (c.ball == ballId) return c.pocket;
        return null;
    }

    // ================= UI =================

    private void ShowState()
    {
        bool playing = _state == State.Aiming || _state == State.Rolling;
        // Both bottom panels share one fixed-height slot in the layout, so switching never moves the table.
        if (rackedPanel != null) rackedPanel.SetActive(_state == State.Racked);
        if (playPanel != null) playPanel.SetActive(playing);
        if (resultPanel != null) resultPanel.SetActive(_state == State.Won || _state == State.Lost);
        if (aim != null) aim.SetActive(_state == State.Aiming && !IsGameOver());

        if (_preset != null)
        {
            if (infoLabel != null) infoLabel.text = _round != null
                ? $"{_preset.name}  ·  {_difficulty}"
                : $"{_preset.name}  ·  Par {_preset.par}";
            ShowQueue();
        }
        if (tableView != null)                                   // ring the pocket the head ball has been called to
            tableView.ShowCalledPocket(playing && _round != null ? HeadCalledPocket() : null);
        if (playing && _round != null)
        {
            if (shotsLabel != null)
            {
                shotsLabel.text = $"SHOTS LEFT: {_round.ShotsLeft}";
                shotsLabel.color = _round.ShotsLeft <= 1 ? badColor : _shotsBase;          // the last shot is the one to be careful with
            }
            if (betInfoLabel != null) betInfoLabel.text = $"Bet ${_bet}  ·  win ${_payoutOnWin}";
        }
        RefreshBalls(false);
        RefreshRacked();

        if (statusLabel != null && _available)
        {
            bool hasNotice = !string.IsNullOrEmpty(_notice) && _state != State.Won && _state != State.Lost;
            if (_state == State.Racked) statusLabel.text = _notice ?? "Pick a difficulty to start";
            else if (_state == State.Won || _state == State.Lost) statusLabel.text = "";
            else statusLabel.text = !string.IsNullOrEmpty(_notice) ? _notice
                                  : _state == State.Aiming ? (BilliardsPendingEffects.TrickShotPending
                                        ? "Trick Shot ready: the faint balls show where everything ends up"
                                        : "Drag back from the table and release to shoot") : "";

            bool coloured = hasNotice || (_state == State.Racked && !string.IsNullOrEmpty(_notice));
            statusLabel.color = !coloured ? _statusBase
                              : _noticeKind == NoticeKind.Good ? goodColor
                              : _noticeKind == NoticeKind.Bad ? badColor : _statusBase;
            if (_noticeFresh && coloured)
            {
                var rt = statusLabel.rectTransform;
                DOTween.Kill(rt);
                rt.localScale = Vector3.one;
                rt.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f).SetId(rt);
            }
            _noticeFresh = false;
        }
    }

    private void ShowQueue()
    {
        if (queueView == null || _preset == null) return;
        IReadOnlyList<string> queue = _round != null ? _round.Referee.Queue : _preset.order;
        queueView.Apply(queue, CalledPocketLabel, _pendingPopped, _pendingRespot);
        _pendingPopped = _pendingRespot = null;                   // animate once, not on every later refresh
    }

    /// The pocket the head ball is called to, as the PLAYER sees it (mirrored), or null.
    private Pocket? HeadCalledPocket()
    {
        string head = _round.Referee.Head;
        if (head == null || _preset == null || _preset.called == null) return null;
        foreach (var c in _preset.called)
            if (c.ball == head && Enum.TryParse(c.pocket, out Pocket pk)) return _mirror.MapPocket(pk);
        return null;
    }

    /// The called pocket for a ball as the PLAYER sees it (mirrored), or null when the ball has none.
    private string CalledPocketLabel(string ballId)
    {
        if (_preset == null || _preset.called == null) return null;
        foreach (var c in _preset.called)
            if (c.ball == ballId && System.Enum.TryParse(c.pocket, out Pocket pk)) return _mirror.MapPocket(pk).ToString();
        return null;
    }

    private void RefreshRacked()
    {
        if (_state != State.Racked || _preset == null || !_available) return;

        _shownMultiplier = CostMultiplier();
        int cap = MaxAffordableBet();
        bool canPlay = cap >= _cfg.billiardsBetMin;
        _bet = Mathf.Clamp(_bet, _cfg.billiardsBetMin, Mathf.Max(_cfg.billiardsBetMin, Mathf.Min(_cfg.billiardsBetMax, cap)));
        int charge = ChargeFor(_bet);

        if (betLabel != null)
            betLabel.text = Mathf.Approximately(CostMultiplier(), 1f) ? $"${_bet}" : $"${_bet}\n<size=16>pay ${charge}</size>";
        if (betMinusButton != null) betMinusButton.interactable = _bet > _cfg.billiardsBetMin;
        if (betPlusButton != null) betPlusButton.interactable = canPlay && _bet + _cfg.billiardsBetStep <= Mathf.Min(_cfg.billiardsBetMax, cap);

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            var d = (BilliardsDifficulty)i;
            difficultyButtons[i].interactable = canPlay;
            if (difficultyLabels != null && i < difficultyLabels.Length && difficultyLabels[i] != null)
            {
                int shots = _preset.ShotsFor(d, _cfg.billiardsParOffsets);
                difficultyLabels[i].text = $"{d} · {shots} {(shots == 1 ? "shot" : "shots")}\nwin ${BilliardsPayout.OnWin(_bet, d, _preset, _cfg)}";
            }
        }
        if (!canPlay && statusLabel != null)
            statusLabel.text = $"Need ${ChargeFor(_cfg.billiardsBetMin)} to play";
    }
}
