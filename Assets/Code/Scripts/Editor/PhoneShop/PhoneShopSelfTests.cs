using System;
using UnityEditor;
using UnityEngine;
using Random = System.Random;   // the pure classes take a System.Random (RngService); never UnityEngine.Random

/// Headless checks for the Phone/Shop plan's pure classes (PHONE_SHOP_DELIVERY_IMPLEMENTATION Appendix B).
/// Grows with each phase: add checks, never delete one.
public static class PhoneShopSelfTests
{
    static int _fails, _passes;

    static void Check(bool ok, string name)
    {
        if (ok) _passes++; else { _fails++; Debug.LogError("[PhoneShop] FAIL " + name); }
    }

    [MenuItem("Tools/OverTimer/PhoneShop/Run Self-Tests")]
    public static void Run()
    {
        _fails = 0; _passes = 0;

        ClockCore();
        CallsAndPower();
        InboxAndBanners();
        TaxDeadlines();
        Dialogues();
        ShopMath();
        ShopCatalogAsset();

        if (_fails == 0) Debug.Log($"[PhoneShop] ALL OK ({_passes} checks)");
        else Debug.LogError($"[PhoneShop] {_fails} FAILED, {_passes} passed");
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
            new ItemDef { id = "laptop", kind = ItemKind.Physical, catalogValue = 150, startsOwned = true, pawnable = true },
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
        Check(PawnMath.PawnValue(cat[0], 0, true) == 120, "Laptop pawns for $120 (80% of $150)");
        Check(PawnMath.PawnValue(cat[1], 0, true) == 0 && PawnMath.PawnValue(cat[2], 0, true) == 0, "Phone and Triple stack can't be pawned");
        Check(PawnMath.PawnValue(cat[6], 60, true) == 0, "licenses can't be pawned");

        // (pawn, buyback at 0 s, 30 s, 60 s, 95 s). Card Box is $40 (Ari, 2026-10-18), so its row differs from the plan's $60 row.
        var expect = new System.Collections.Generic.Dictionary<string, (int pawn, int b0, int b30, int b60, int b90)>
        {
            { "laptop", (120, 165, 173, 180, 188) }, { "power_bank", (20, 28, 29, 30, 32) },
            { "card_box", (32, 44, 46, 48, 50) }, { "lucky_stack", (24, 33, 35, 36, 38) },
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
        Check(PawnMath.BuybackMultiplier(29.9f) == 1.10f && PawnMath.BuybackMultiplier(90f) == 1.25f && PawnMath.NextStepIn(95f) < 0, "buyback steps, capped at 90 s");
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

        var ds = new DeliveryScheduler(); var arrived = new System.Collections.Generic.List<Order>(); ds.Arrived += arrived.Add;
        ds.Ship(new[] { "power_bank" }, 20f, false); ds.ShipJunk(5f);
        for (int i = 0; i < 49; i++) ds.Tick(0.1f); Check(arrived.Count == 0, "nothing at 4.9 s");
        ds.Tick(0.2f); Check(arrived.Count == 1 && arrived[0].junk, "junk box at 5 s");
        for (int i = 0; i < 300; i++) ds.Tick(0f); Check(arrived.Count == 1, "paused clock: shipping doesn't advance");
        for (int i = 0; i < 160; i++) ds.Tick(0.1f); Check(arrived.Count == 2 && arrived[1].itemIds[0] == "power_bank", "Power Bank at 20 s");
        ds.Arrived += o => { if (!o.junk) ds.ShipJunk(1f); };
        ds.Ship(new[] { "card_box" }, 1f, true); ds.Tick(1.1f); Check(ds.InTransit.Count == 1, "a handler can ship from inside Arrived safely");

        var led = new PawnLedger(); led.Record("laptop", 150, 100f);
        Check(led.Price("laptop", 100f) == 165 && led.Price("laptop", 131f) == 173 && led.Price("laptop", 500f) == 188, "ledger buyback uses game time since pawn");

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

    // The real catalog asset (Assets/Settings/Configs/ItemCatalog.asset) against the table in the plan (sec. 3.6).
    static void ShopCatalogAsset()
    {
        var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemCatalogSO>("Assets/Settings/Configs/ItemCatalog.asset");
        Check(catalog != null, "ItemCatalog.asset exists");
        if (catalog == null) return;
        var d = catalog.BuildDefs();
        var own = new Ownership(d);                       // throws on a duplicate id
        string[] ids = { "laptop", "phone", "printer", "meter", "collection", "egg_timer", "bear", "picture", "triple_stack",
                         "power_bank", "card_box", "lucky_stack", "license_blackjack", "license_billiards" };
        bool all = true; foreach (var id in ids) if (!own.Has(id)) { all = false; Debug.LogError("[PhoneShop] catalog is missing '" + id + "'"); }
        Check(all && d.Count == ids.Length, "catalog has exactly the 14 planned items");
        if (!all) return;
        Check(own.Def("power_bank").price == 25 && own.Def("card_box").price == 40 && own.Def("lucky_stack").price == 30 &&
              own.Def("license_blackjack").price == 60 && own.Def("license_billiards").price == 60, "catalog prices: Power Bank 25, Card Box 40, Lucky Nine 30, licenses 60");
        Check(own.Def("laptop").catalogValue == 150 && own.Def("laptop").pawnable && PawnMath.PawnValue(own.Def("laptop"), 0, true) == 120, "Laptop: value $150, pawns for $120");
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
