using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;   // the pure classes take a System.Random (RngService); never UnityEngine.Random

/// Headless checks for the Phone/Shop plan's pure classes (PHONE_SHOP_DELIVERY_IMPLEMENTATION Appendix B).
/// Grows with each phase: add checks, never delete one.
public static class PhoneShopSelfTests
{
    static int _fails, _passes;
    static readonly System.Collections.Generic.List<string> _failed = new System.Collections.Generic.List<string>();

    static void Check(bool ok, string name)
    {
        if (ok) _passes++; else { _fails++; _failed.Add(name); }
    }

    [MenuItem("Tools/OverTimer/PhoneShop/Run Self-Tests")]
    public static void Run()
    {
        var failed = RunAll();
        foreach (var name in failed) Debug.LogError("[PhoneShop] FAIL " + name);
        if (_fails == 0) Debug.Log($"[PhoneShop] ALL OK ({_passes} checks)");
        else Debug.LogError($"[PhoneShop] {_fails} FAILED, {_passes} passed");
    }

    /// Runs every check and returns the names of the failed ones (empty = all passed).
    public static System.Collections.Generic.IReadOnlyList<string> RunAll()
    {
        _fails = 0; _passes = 0; _failed.Clear();

        ClockCore();
        CallsAndPower();
        InboxAndBanners();
        TaxDeadlines();
        Dialogues();
        ShopMath();
        ShopCatalogAsset();

        return _failed.ToArray();
    }

    // ---- Phase 1.3: CallerRoller, CallScheduler, PhonePower
    static void CallsAndPower()
    {
        var cfg = new PhoneConfig(); var rng = new Random(1); int N = 200000;
        var shown = new System.Collections.Generic.Dictionary<CallerKind, int>();
        var truth = new System.Collections.Generic.Dictionary<CallTruth, int>();
        for (int i = 0; i < N; i++)
        {
            var c = CallerRoller.Roll(rng, cfg, true);
            shown.TryGetValue(c.shownAs, out int s); shown[c.shownAs] = s + 1;
            truth.TryGetValue(c.truth, out int t); truth[c.truth] = t + 1;
        }
        double sp = truth[CallTruth.Telemarketer] / (double)N;
        Check(Math.Abs(shown[CallerKind.Telemarketer] / (double)N - 0.25) < 0.01 && Math.Abs(shown[CallerKind.Unknown] / (double)N - 0.40) < 0.01, "caller ID weights 25/25/40/10 (Unknown is the most common)");
        Check(Math.Abs(sp - 0.49) < 0.01, $"real telemarketer share ~49% (got {sp:P1})");
        rng = new Random(2); bool anyItrs = false;
        for (int i = 0; i < 50000; i++) if (CallerRoller.Roll(rng, cfg, false).truth == CallTruth.ITRS) anyItrs = true;
        Check(!anyItrs, "no ITRS calls while no bill is pending");
        var unk = new CallInfo { shownAs = CallerKind.Unknown, truth = CallTruth.Client };
        Check(unk.PausesClock && !new CallInfo { truth = CallTruth.Telemarketer }.PausesClock, "client/ITRS pause the clock, telemarketer doesn't");

        var cs = new CallScheduler(cfg, new Random(3)); var log = new System.Collections.Generic.List<string>();
        cs.RingStarted += c => log.Add("ring"); cs.RingStopped += (c, e) => log.Add(e.ToString()); cs.CallFinished += c => log.Add("done");
        float first = 0; while (cs.Current != CallScheduler.State.Ringing && first < 100) { cs.Tick(0.1f); first += 0.1f; }
        Check(log.Count == 1 && log[0] == "ring" && first >= 29.9f && first <= 60.2f, $"first ring within 30-60 s (at {first:F1} s)");
        for (int i = 0; i < 200; i++) cs.Tick(0f);
        Check(cs.Current == CallScheduler.State.Ringing, "paused clock (gameDelta 0) freezes the ring timer");
        Check(cs.Answer() && cs.Current == CallScheduler.State.InCall, "answer -> in call");
        for (int i = 0; i < 1000; i++) cs.Tick(0.1f);
        Check(cs.Current == CallScheduler.State.InCall, "no second call while one is active (100 s later)");
        cs.EndCall(); float tt = 0; while (cs.Current != CallScheduler.State.Ringing) { cs.Tick(0.1f); tt += 0.1f; if (tt > 200) break; }
        Check(tt >= 12f + 30f - 0.11f && tt <= 12f + 60f + 0.11f, $"next ring = 12 s gap + 30-60 s interval (got {tt:F1} s)");
        for (int i = 0; i < 101; i++) cs.Tick(0.1f);
        Check(log[log.Count - 1] == "Missed", "unanswered ring times out after 10 s -> Missed");
        var off = new CallScheduler(cfg, new Random(4)); bool on = false; off.CanRing = () => on; int missed = 0, rang = 0;
        off.RingStopped += (c, e) => { if (e == CallEnd.Missed) missed++; }; off.RingStarted += c => rang++;
        for (int i = 0; i < 1300; i++) off.Tick(0.1f);
        Check(rang == 0 && missed >= 2, $"phone off: calls are logged as missed, none ring ({missed} missed)");
        on = true; off.Tick(0.1f); Check(off.Current != CallScheduler.State.Ringing, "booting the phone does NOT release a backlog of calls");

        var fast = new CallScheduler(cfg, new Random(5));
        while (fast.Current != CallScheduler.State.Ringing) fast.Tick(0.1f);
        fast.Answer(); fast.EndCall(); fast.IntervalScale = 0.5f; float ft = 0;      // the scale applies to intervals scheduled from now on
        while (fast.Current != CallScheduler.State.Ringing && ft < 200) { fast.Tick(0.1f); ft += 0.1f; }
        Check(ft <= 12f + 30f + 0.2f, $"Robocalls (IntervalScale 0.5): next ring within 12 s gap + 30 s (at {ft:F1} s)");
        var abort = new CallScheduler(cfg, new Random(6)); int stops = 0; abort.RingStopped += (c, e) => stops++;
        while (abort.Current != CallScheduler.State.Ringing) abort.Tick(0.1f);
        abort.Abort(); Check(abort.Current == CallScheduler.State.Gap && stops == 1, "Abort() while ringing ends the ring as missed and enters the gap");

        var pw = new PhonePower(5f);
        pw.Press(); Check(pw.Current == PhonePower.State.Off, "press: on -> off");
        pw.Press(); Check(pw.Current == PhonePower.State.Booting, "press: off -> booting");
        for (int i = 0; i < 49; i++) pw.Tick(0.1f); Check(pw.Current == PhonePower.State.Booting, "still booting at 4.9 s");
        pw.Tick(0.2f); Check(pw.IsUsable, "on after 5 s");
        object eater = "cableEater"; pw.ForceOff(eater);
        Check(pw.Current == PhonePower.State.Off && !pw.Press(), "Cable Eater forces off; the button is dead");
        pw.ReleaseForceOff(eater); Check(pw.Current == PhonePower.State.Off && pw.Press() && pw.Current == PhonePower.State.Booting, "after release the player must boot it (stays off)");
        pw.ForceOff(eater); Check(pw.Current == PhonePower.State.Off, "forced off mid-boot cancels the boot");
    }

    // ---- Call dialogue (client/ITRS/telemarketer scripts)
    static void Dialogues()
    {
        var d = new CallDialogue(2);
        Check(d.Index == 0 && !d.IsFinal, "a 2-line call starts on line 0, not final");
        Check(d.Advance() && d.IsFinal && d.Index == 1, "reply moves to the last line");
        Check(!d.Advance() && d.Index == 1, "the last line can't be advanced past");
        var one = new CallDialogue(1); Check(one.IsFinal, "a 1-line script is final at once");
        Check(new CallDialogue(0).IsFinal, "an empty script is treated as one final line");

        var cat = ScriptableObject.CreateInstance<CallerProfileCatalog>();
        foreach (CallTruth t in new[] { CallTruth.Telemarketer, CallTruth.Client, CallTruth.ITRS })
        {
            var s = cat.Script(t);
            Check(s.Count >= 2 && s[s.Count - 1].replyLabel == "Hang up.", t + " default script ends with a \"Hang up.\" line");
        }
        Check(cat.Script(CallTruth.Client)[0].replyLabel == "Got you.", "client's first reply is \"Got you.\"");
        Check(cat.Pick(CallerKind.Client, -5) != null && cat.Pick(CallerKind.Unknown, 123456789) != null, "profile pick works for any seed");
        Check(!string.IsNullOrEmpty(cat.NoReplyLine) && cat.NoReplyLine.Contains("Are you there"), "client no-reply goodbye line exists");
        UnityEngine.Object.DestroyImmediate(cat);

        var rec = new CallRecord { name = "Client", outcome = CallEnd.Answered };
        rec.transcript.Add("Client: hi");
        Check(rec.transcript.Count == 1 && rec.result == "", "call record starts with no result");
    }

    // ---- Phase 2.1: catalog, pawn and buyback maths, checkout, ownership, icon gate, delivery, ledger, validator
    static void ShopMath()
    {
        var cat = new System.Collections.Generic.List<ItemDef>
        {
            new ItemDef { id = "laptop", kind = ItemKind.Physical, catalogValue = 135, startsOwned = true, pawnable = true },
            new ItemDef { id = "phone", kind = ItemKind.Physical, startsOwned = true, pawnable = false },
            new ItemDef { id = "triple_stack", kind = ItemKind.Physical, startsOwned = true, pawnable = false },
            new ItemDef { id = "power_bank", kind = ItemKind.Physical, price = 25, pawnable = true, sold = true },
            new ItemDef { id = "card_box", kind = ItemKind.Physical, price = 40, pawnable = true, sold = true },
            new ItemDef { id = "lucky_stack", kind = ItemKind.Physical, price = 30, pawnable = true, sold = true },
            new ItemDef { id = "license_blackjack", kind = ItemKind.Digital, price = 60, sold = true },
            new ItemDef { id = "license_billiards", kind = ItemKind.Digital, price = 60, sold = true },
        };
        var own = new Ownership(cat);
        Check(own.IsOwned("laptop") && !own.CanBuy("laptop") && own.CanBuy("power_bank"), "starting kit owned; shop items buyable");
        Check(PawnMath.PawnValue(cat[0], 0, true) == 108, "Laptop pawns for $108 (80% of $135)");
        Check(PawnMath.PawnValue(cat[1], 0, true) == 0 && PawnMath.PawnValue(cat[2], 0, true) == 0, "Phone and Triple stack can't be pawned");
        Check(PawnMath.PawnValue(cat[6], 60, true) == 0, "licenses can't be pawned");

        // (pawn, buyback at 0 s, 30 s, 60 s, 95 s). Card Box is $40 (Ari, 2026-10-18), so its row differs from the plan's $60 row.
        var expect = new System.Collections.Generic.Dictionary<string, (int pawn, int b0, int b30, int b60, int b90)>
        {
            { "laptop", (108, 142, 146, 152, 156) }, { "power_bank", (20, 27, 27, 28, 29) },
            { "card_box", (32, 42, 44, 45, 46) }, { "lucky_stack", (24, 32, 33, 34, 35) },
        };
        bool tableOk = true;
        foreach (var kv in expect)
        {
            ItemDef d = null; foreach (var x in cat) if (x.id == kv.Key) d = x;
            int basis = d.startsOwned ? d.catalogValue : d.price;
            int pv = PawnMath.PawnValue(d, d.startsOwned ? 0 : d.price, true);
            var got = (pv, PawnMath.BuybackPrice(basis, 0), PawnMath.BuybackPrice(basis, 30), PawnMath.BuybackPrice(basis, 60), PawnMath.BuybackPrice(basis, 95));
            if (got != kv.Value) { tableOk = false; Debug.LogError($"[PhoneShop]    {kv.Key}: got {got} want {kv.Value}"); }
        }
        Check(tableOk, "pawn + buyback table matches the decision log for every item");
        Check(PawnMath.BuybackMultiplier(29.9f) == 1.05f && PawnMath.BuybackMultiplier(90f) == 1.15f && PawnMath.NextStepIn(95f) < 0, "buyback steps, capped at 90 s");
        bool roundTripLoses = true;
        for (int price = 1; price <= 500; price++) for (int s = 0; s <= 120; s += 15)
            if (PawnMath.BuybackPrice(price, s) <= (int)Math.Floor(price * 0.8)) roundTripLoses = false;
        Check(roundTripLoses, "for every price $1-$500 and every wait, buyback > pawn (no money printer)");

        var sc = new ShopConfig();
        var q = Checkout.Quote(new System.Collections.Generic.List<ItemDef> { cat[3], cat[6] }, Shipping.Standard, false, false, sc);
        Check(q.total == 85 && q.shipping == 0 && q.etaSeconds == 20f, "Power Bank + Blackjack license, standard: $85, 20 s");
        q = Checkout.Quote(new System.Collections.Generic.List<ItemDef> { cat[6] }, Shipping.Express, true, false, sc);
        Check(q.total == 60 && q.shipping == 0 && q.leaveOpenFee == 0 && q.etaSeconds == 0f, "license only: no shipping or leave-open fee, instant");
        q = Checkout.Quote(new System.Collections.Generic.List<ItemDef> { cat[3], cat[4], cat[5] }, Shipping.Express, true, false, sc);
        Check(q.total == 95 + 8 + 3 && q.etaSeconds == 5f, "3 physical items, express + leave open: fees charged once ($106)");
        q = Checkout.Quote(new System.Collections.Generic.List<ItemDef> { cat[3], cat[4], cat[5] }, Shipping.Standard, false, true, sc);
        int paidSum = 0; foreach (var v in q.pricePaid.Values) paidSum += v;
        Check(q.discount == 19 && paidSum == q.subtotal - q.discount && q.total == 76, "20% coupon: $19 off, per-item prices add up exactly ($76)");
        Check(PawnMath.PawnValue(cat[4], q.pricePaid["card_box"], true) < PawnMath.PawnValue(cat[4], 40, true), "coupon purchase pawns at 80% of the discounted price");
        own.MarkOrdered("power_bank", 25); Check(!own.CanBuy("power_bank"), "can't order the same item twice");
        own.MarkDelivered("power_bank"); Check(own.MarkPawned("power_bank") == 20 && own.Of("power_bank") == Ownership.Status.Pawned && !own.CanBuy("power_bank"), "pawned item pays $20 and shows as Sold (until Batch 4)");
        Check(own.MarkPawned("phone") == 0 && own.IsOwned("phone"), "pawning the Phone does nothing");
        bool dup = false; try { new Ownership(new[] { cat[0], cat[0] }); } catch (ArgumentException) { dup = true; }
        Check(dup, "a duplicate item id is rejected");

        var g = new IconGate(); g.SetLicensed("Blackjack", false);
        g.CardHide("Blackjack"); g.CardRelease("Blackjack");
        Check(!g.IsVisible("Blackjack"), "card removal does NOT reveal an unlicensed app");
        g.SetLicensed("Blackjack", true); Check(g.IsVisible("Blackjack"), "licence shows it");
        g.CardHide("Mining"); g.CardHide("Mining"); g.CardRelease("Mining");
        Check(!g.IsVisible("Mining"), "two overlapping No More Mining cards: one ending keeps it hidden");
        g.CardRelease("Mining"); Check(g.IsVisible("Mining"), "both ended: visible");

        var gate = new IconGate(); gate.CardHide("Shop");
        Check(!gate.IsVisible("shop") && !gate.IsVisible("SHOP"), "icon names ignore case: a card hiding \"Shop\" hides the phone's \"shop\" icon");
        gate.CardRelease("shop"); Check(gate.IsVisible("Shop"), "...and releasing it with another case shows it again");

        var cart = new ShopCart(); var own2 = new Ownership(cat);
        Check(cart.TryAdd(cat[3], own2) && !cart.TryAdd(cat[3], own2), "cart: an item goes in once");
        Check(!cart.TryAdd(cat[0], own2), "cart: an owned item (Laptop) can't be added");
        cart.TryAdd(cat[6], own2); var cq = cart.Quote(Shipping.Standard, false, sc);
        Check(cq.total == 85 && cart.Count == 2, "cart: Power Bank + Blackjack quote $85");
        own2.MarkOrdered("power_bank", 25); Check(!cart.TryAdd(cat[3], own2), "cart: an item already on the way can't be added again");
        Check(cart.Remove("power_bank") && !cart.Remove("power_bank") && cart.Count == 1, "cart: remove works once");
        cart.Clear(); Check(cart.Count == 0 && cart.Quote(Shipping.Express, true, sc).total == 0, "cart: empty cart costs nothing");

        var lo = new ShopCart(); lo.TryAdd(cat[3], new Ownership(cat));
        Check(lo.Quote(Shipping.Standard, true, sc).total == 28 && lo.Quote(Shipping.Express, true, sc).total == 36, "cart: leave it opened adds $3 once (25 -> 28, express 36)");

        var depot = new DoorDepot(2, 3); var towerLog = new System.Collections.Generic.List<string>();
        depot.Added += p => towerLog.Add("+" + p.orderId); depot.Opened += p => towerLog.Add("open" + p.orderId);
        var d1 = depot.Receive(1, new[] { "power_bank" }); var d2 = depot.Receive(2, new[] { "card_box" }); var d3 = depot.Receive(3, new[] { "lucky_stack" });
        Check(depot.Tower.Count == 2 && depot.Waiting == 1 && depot.Tower[0] == d1 && depot.Tower[1] == d2, "door: a full tower makes the next parcel wait (nothing lost)");
        Check(!depot.Click(d1) && !depot.Click(d1) && depot.Tower.Count == 2, "door: two clicks do not open a 3-click parcel");
        Check(depot.Click(d1) && depot.Tower.Count == 2 && depot.Tower[0] == d2 && depot.Tower[1] == d3 && depot.Waiting == 0, "door: the third click opens it and the waiting parcel takes its place");
        Check(!depot.Click(d1), "door: an opened parcel can't be clicked again");
        Check(string.Join(",", towerLog) == "+1,+2,open1,+3", "door: events in order (+1,+2,open1,+3)");

        var pile = new LetterPile(3); var pileLog = new System.Collections.Generic.List<string>();
        pile.Added += l => pileLog.Add("+" + l.kind); pile.Removed += l => pileLog.Add("-" + l.kind);
        var tax = pile.Add(LetterKind.Tax, ""); var j1 = pile.Add(LetterKind.Junk, "a"); var j2 = pile.Add(LetterKind.Junk, "b");
        var j3 = pile.Add(LetterKind.Junk, "c");
        Check(pile.Letters.Count == 3 && !pile.Letters.Contains(j1) && pile.Letters.Contains(tax) && pile.Letters.Contains(j3), "mail: a full pile pushes the OLDEST junk letter off, never the tax letter");
        pile.Remove(j2); pile.Remove(j3);
        var t2 = new LetterPile(1); t2.Add(LetterKind.Tax, "");
        Check(t2.Add(LetterKind.Junk, "x") == null && t2.Letters.Count == 1, "mail: junk can't push a tax letter off a full pile");
        Check(pile.FirstOf(LetterKind.Tax) == tax && pile.FirstOf(LetterKind.Junk) == null, "mail: FirstOf finds the tax letter");
        Check(pile.Remove(tax) && !pile.Remove(tax), "mail: a letter is removed once");

        var jt = new JunkMailTimer(new Random(11), 60f, 120f, 60f); float firstJunk = 0f; bool junkDue = false;
        while (!junkDue && firstJunk < 400f) { firstJunk += 0.5f; junkDue = jt.Tick(0.5f); }
        Check(junkDue && firstJunk >= 60f && firstJunk <= 120.5f, $"junk mail: the first letter comes between 60 and 120 s (at {firstJunk:F1} s), never before the grace");
        var jt2 = new JunkMailTimer(new Random(12), 60f, 120f, 60f); int junkCount = 0; float elapsed = 0f;
        for (int i = 0; i < 6000; i++) { elapsed += 0.1f; if (jt2.Tick(0.1f)) junkCount++; }
        Check(junkCount >= 5 && junkCount <= 10, $"junk mail: about one every 60-120 s ({junkCount} in {elapsed:F0} s)");
        var jt3 = new JunkMailTimer(new Random(13), 60f, 120f, 60f); bool paused = false;
        for (int i = 0; i < 1000; i++) if (jt3.Tick(0f)) paused = true;
        Check(!paused, "junk mail: a paused clock (gameDelta 0) brings none");

        var ds = new DeliveryScheduler(); var arrived = new System.Collections.Generic.List<Order>(); ds.Arrived += arrived.Add;
        ds.Ship(new[] { "power_bank" }, 20f, false); ds.ShipJunk(5f);
        for (int i = 0; i < 49; i++) ds.Tick(0.1f); Check(arrived.Count == 0, "nothing at 4.9 s");
        ds.Tick(0.2f); Check(arrived.Count == 1 && arrived[0].junk, "junk box at 5 s");
        for (int i = 0; i < 300; i++) ds.Tick(0f); Check(arrived.Count == 1, "paused clock: shipping doesn't advance");
        for (int i = 0; i < 160; i++) ds.Tick(0.1f); Check(arrived.Count == 2 && arrived[1].itemIds[0] == "power_bank", "Power Bank at 20 s");
        ds.Arrived += o => { if (!o.junk) ds.ShipJunk(1f); };
        ds.Ship(new[] { "card_box" }, 1f, true); ds.Tick(1.1f); Check(ds.InTransit.Count == 1, "a handler can ship from inside Arrived safely");

        var led = new PawnLedger(); led.Record("laptop", 135, 100f);
        Check(led.Price("laptop", 100f) == 142 && led.Price("laptop", 131f) == 146 && led.Price("laptop", 500f) == 156, "ledger buyback uses game time since pawn");
        var bq = Checkout.QuoteBuyback("laptop", led.Price("laptop", 100f), Shipping.Standard, false, new ShopConfig());
        Check(bq.total == 142 && bq.etaSeconds == 20f && bq.hasPhysical, "buyback quote, standard: item price only, 20 s");
        bq = Checkout.QuoteBuyback("laptop", 171, Shipping.Express, true, new ShopConfig());
        Check(bq.total == 171 + 8 + 3 && bq.etaSeconds == 5f, "buyback quote, express + leave open: fees on top of the price");
        led.Remove("laptop"); Check(!led.TryGet("laptop", out _), "ledger: Remove forgets the entry");
        var bbOwn = new Ownership(new[] { new ItemDef { id = "laptop", startsOwned = true, pawnable = true, catalogValue = 135 } });
        Check(bbOwn.PawnBasis("laptop") == 135 && bbOwn.MarkPawned("laptop") == 108 && bbOwn.Of("laptop") == Ownership.Status.Pawned, "starting item: basis = catalog value, pays 108");
        bbOwn.MarkBoughtBack("laptop"); Check(bbOwn.Of("laptop") == Ownership.Status.Ordered && bbOwn.PawnBasis("laptop") == 135, "bought back: Ordered again, same basis");
        bbOwn.MarkDelivered("laptop"); Check(bbOwn.IsOwned("laptop"), "delivered: Owned again");

        var defs = new System.Collections.Generic.List<ItemDefCheck>
        {
            new ItemDefCheck { id = "a", kind = ItemKind.Physical, pawnable = true, needsBinding = true, bindingLayer = 3 },
            new ItemDefCheck { id = "b", kind = ItemKind.Physical, needsBinding = true, bindingLayer = 3 },
            new ItemDefCheck { id = "c", kind = ItemKind.Digital },
        };
        Check(ShopValidation.Check(defs, new[] { "a", "b" }).Count == 0, "validator: a clean catalog passes");
        Check(ShopValidation.Check(defs, new[] { "a" }).Count == 1, "validator: a scene item without a binding is reported");
        Check(ShopValidation.Check(defs, new[] { "a", "a", "b" }).Count == 1, "validator: a double binding is reported");
        Check(ShopValidation.Check(defs, new[] { "a", "b", "zzz" }).Count == 1, "validator: an unknown id on a binding is reported");
        defs[0].bindingLayer = 0; Check(ShopValidation.Check(defs, new[] { "a", "b" }).Count == 1, "validator: a pawnable item off layer 3 is reported");
        defs[0].bindingLayer = 3; defs.Add(new ItemDefCheck { id = "a", kind = ItemKind.Digital });
        Check(ShopValidation.Check(defs, new[] { "a", "b" }).Count >= 1, "validator: a duplicate id is reported");
    }

    // The real catalog asset (found by type) against the table in the plan (sec. 3.6).
    static void ShopCatalogAsset()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemCatalogSO");
        var catalog = guids.Length == 0 ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        Check(catalog != null, "ItemCatalog.asset exists");
        if (catalog == null) return;
        var d = catalog.BuildDefs();
        var own = new Ownership(d);                       // throws on a duplicate id
        string[] ids = { "laptop", "phone", "printer", "meter", "collection", "egg_timer", "bear", "picture", "triple_stack",
                         "power_bank", "card_box", "lucky_stack", "license_blackjack", "license_billiards" };
        bool all = true; foreach (var id in ids) if (!own.Has(id)) { all = false; Check(false, "catalog is missing '" + id + "'"); }
        Check(all && d.Count == ids.Length, "catalog has exactly the 14 planned items");
        if (!all) return;
        Check(own.Def("power_bank").price == 25 && own.Def("card_box").price == 40 && own.Def("lucky_stack").price == 30 &&
              own.Def("license_blackjack").price == 60 && own.Def("license_billiards").price == 60, "catalog prices: Power Bank 25, Card Box 40, Lucky Nine 30, licenses 60");
        Check(own.Def("laptop").catalogValue == 135 && own.Def("laptop").pawnable && PawnMath.PawnValue(own.Def("laptop"), 0, true) == 108, "Laptop: value $135, pawns for $108");
        bool startingOk = true;
        foreach (var id in new[] { "laptop", "phone", "printer", "meter", "collection", "egg_timer", "bear", "picture", "triple_stack" }) if (!own.IsOwned(id)) startingOk = false;
        foreach (var id in new[] { "power_bank", "card_box", "lucky_stack", "license_blackjack", "license_billiards" }) if (own.IsOwned(id) || !own.CanBuy(id)) startingOk = false;
        Check(startingOk, "starting kit owned; Power Bank, Card Box, Lucky Nine and both licenses are for sale");
        bool unpawnable = true;
        foreach (var id in new[] { "phone", "printer", "meter", "collection", "egg_timer", "bear", "picture", "triple_stack", "license_blackjack", "license_billiards" }) if (own.Def(id).pawnable) unpawnable = false;
        Check(unpawnable, "only the Laptop and the three shop items can be pawned");
        Check(own.Def("license_blackjack").kind == ItemKind.Digital && own.Def("license_billiards").kind == ItemKind.Digital, "licenses are digital");
    }

    // ---- Phase 1.4: TaxDeadline
    static void TaxDeadlines()
    {
        var td = new TaxDeadline(40, 30f); int res = 0; td.Resolved += _ => res++;
        for (int i = 0; i < 299; i++) td.Tick(0.1f); Check(td.State == TaxDeadline.Outcome.Pending, "pending at 29.9 s");
        for (int i = 0; i < 100; i++) td.Tick(0f); Check(td.State == TaxDeadline.Outcome.Pending, "paused clock doesn't run the window out");
        td.Tick(0.2f); Check(td.State == TaxDeadline.Outcome.Penalized && res == 1, "unpaid at 30 s -> penalized once");
        td.MarkPaid(); td.Tick(5f); Check(res == 1 && td.State == TaxDeadline.Outcome.Penalized, "no second resolution");
        var td2 = new TaxDeadline(40, 30f); td2.Extend(15f); for (int i = 0; i < 400; i++) td2.Tick(0.1f);
        Check(td2.State == TaxDeadline.Outcome.Pending, "ITRS payment plan +15 s");
        td2.Double(); td2.MarkPaid(); Check(td2.Amount == 80 && td2.State == TaxDeadline.Outcome.Paid, "Audit doubles, then paid");
        var td3 = new TaxDeadline(40, 30f); td3.MarkPaid(); td3.Tick(100f); Check(td3.State == TaxDeadline.Outcome.Paid, "a paid bill never penalises later");
    }

    // ---- Phase 1.3: Inbox, BannerQueue
    static void InboxAndBanners()
    {
        var ib = new Inbox(); var bq = new BannerQueue(4f); ib.Added += bq.Push;
        for (int i = 0; i < 5; i++) ib.Add(MessageKind.System, "m" + i, "");
        Check(bq.Visible.Count == 3 && bq.Waiting == 2 && ib.Unread == 5, "5 at once: 3 banners, 2 waiting, all 5 in the Inbox");
        bq.Tick(4.1f); Check(bq.Visible.Count == 2 && bq.Waiting == 0, "after 4 s the waiting two show");
        bq.Tick(4.1f); Check(bq.Visible.Count == 0, "all banners gone; Inbox still has 5");
        Check(ib.All[0].title == "m4", "Inbox is newest first");
    }

    // ---- Phase 1.1: PauseLock, SpeedModifiers
    static void ClockCore()
    {
        var pl = new PauseLock(); int flips = 0; pl.PausedChanged += _ => flips++;
        object call = "call", cut = "cutscene";
        pl.Acquire(call); pl.Acquire(cut); pl.Release(call);
        Check(pl.IsPaused, "overlap: releasing the call keeps the cutscene's pause");
        pl.Release(cut); Check(!pl.IsPaused && flips == 2, "clock resumes only when the last holder releases (2 flips)");
        pl.Acquire(call); pl.Acquire(call); pl.Release(call); Check(!pl.IsPaused, "same holder twice counts once");
        Check(!pl.Release("nobody"), "releasing an unknown holder is a no-op");

        var sm = new SpeedModifiers(); object snail = "snail", ring = "ring";
        sm.Set(snail, 0.75f); sm.Set(ring, 1.5f);
        Check(Math.Abs(sm.Effective - 1.125f) < 1e-5, "snail 0.75 x ring 1.5 = 1.125 (stack, no clobber)");
        sm.Remove(ring); Check(Math.Abs(sm.Effective - 0.75f) < 1e-5, "ring ends: snail still applies");
        sm.Set("scarab", 0f); Check(sm.Effective == 0f, "Time Scarab x0 stops the clock");
        sm.Clear(); sm.Set("a", 2f); sm.Set("b", 2f); Check(sm.Effective == 3f, "clamped to 3");
        sm.Set("a", 1f); Check(sm.Count == 2 && sm.Effective == 2f, "same key replaces, never double-counts");
    }
}
