using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Turns the "Phone UI" bitmap font sheets (Assets/Art/Sprites/Phone UI/Font Big.png and Font Small.png) into real fonts:
/// a .ttf whose glyphs are the sheet's pixels as square outlines, plus a static TextMeshPro font asset (raster, point
/// filtered, like the existing "supersoft" pixel font).
///
/// Sheet layout (both sheets): 16 x 16 pixel cells; each 16-row band is one row of characters and the glyph's baseline is
/// the band's bottom row. Bands from the bottom (y0): 0 circumflex capitals, 16 diaeresis capitals, 32 grave capitals,
/// 48 acute capitals, 64 A-Z, 80 circumflex, 96 diaeresis, 112 grave, 128 acute, 144 a-z, 160 symbols, 176 digits.
/// Only the dark set (y < 192) is read; the cream set above is the same glyphs in another colour.
///
/// Font design: 1 sheet pixel = 100 font units, 10 pixels per em. Use TMP font sizes that are multiples of 10 for crisp
/// pixels (size 20 = every pixel is 2 canvas units). Not on the sheet: $ (synthesised from S), : (synthesised from . and
/// the middle dot), &lt; &gt; &amp; ~ ^ ` and Vietnamese letters. TMP falls back to another font for those.
/// </summary>
public static class BitmapFontBuilder
{
    const string SheetFolder = "Assets/Art/Sprites/Phone UI/";
    const string OutputFolder = "Assets/Art/Fonts/";
    const int Cell = 16;
    const int UnitsPerPixel = 100;
    const int UnitsPerEm = 1000;

    struct Rect { public int x0, y0, x1, y1; }

    sealed class Glyph
    {
        public List<int> codepoints = new List<int>();
        public List<Rect> rects = new List<Rect>();
        public int advance;               // pixels
        public int xMin, yMin, xMax, yMax;
    }

    [MenuItem("Tools/OverTimer/Fonts/Build Phone UI Fonts")]
    public static void BuildAll()
    {
        Build("Font Big", "PhoneBig", "Phone Big");
        Build("Font Small", "PhoneSmall", "Phone Small");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Fonts] Built Phone Big and Phone Small in " + OutputFolder);
    }

    static void Build(string sheetName, string fileName, string familyName)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(SheetFolder + sheetName + ".png")))
        {
            Debug.LogError("[Fonts] cannot read " + sheetName);
            return;
        }
        var sheet = new Sheet(tex);
        var glyphs = ExtractGlyphs(sheet, out int capHeight, out int xHeight);

        string ttfPath = OutputFolder + fileName + ".ttf";
        File.WriteAllBytes(ttfPath, TtfWriter.Write(glyphs, familyName, capHeight, xHeight));
        AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceSynchronousImport);

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null) { Debug.LogError("[Fonts] Unity could not import " + ttfPath); return; }

        var all = new StringBuilder();
        foreach (var g in glyphs) foreach (int cp in g.codepoints) all.Append(char.ConvertFromUtf32(cp));
        string assetPath = CreateStaticRasterAsset(font, OutputFolder + fileName + " Pixel.asset", UnitsPerEm / UnitsPerPixel, all.ToString());
        Debug.Log($"[Fonts] {fileName}: {glyphs.Count} glyphs, cap height {capHeight}px, x-height {xHeight}px -> {assetPath}");
    }

    /// <summary>
    /// Builds a static, point-filtered, raster TMP font asset (like the project's "supersoft") from a TrueType pixel font.
    /// samplingSize must be the size at which one design pixel is exactly one atlas pixel.
    /// </summary>
    static string CreateStaticRasterAsset(Font font, string assetPath, int samplingSize, string characters)
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);

        var asset = TMP_FontAsset.CreateFontAsset(font, samplingSize, 2, GlyphRenderMode.RASTER, 512, 512,
                                                  AtlasPopulationMode.Dynamic, false);
        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var atlas in asset.atlasTextures)
        {
            atlas.name = asset.name + " Atlas";
            atlas.filterMode = FilterMode.Point;
            AssetDatabase.AddObjectToAsset(atlas, asset);
        }

        asset.TryAddCharacters(characters, out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[Fonts] {asset.name}: could not add {missing.Length} characters");

        // Freeze it (static) so it needs no source font at runtime, like the project's other pixel fonts.
        var so = new SerializedObject(asset);
        var mode = so.FindProperty("m_AtlasPopulationMode");
        if (mode != null) mode.intValue = (int)AtlasPopulationMode.Static;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return assetPath;
    }

    [MenuItem("Tools/OverTimer/Fonts/Build BoldPixels Font")]
    public static void BuildBoldPixels()
    {
        const string ttf = OutputFolder + "BoldPixels.ttf";
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        if (font == null) { Debug.LogError("[Fonts] " + ttf + " not found"); return; }

        // Everything the font has in: ASCII, Latin-1, Latin Extended-A, Vietnamese, a few typographic marks.
        var chars = new StringBuilder();
        void Range(int from, int to) { for (int c = from; c <= to; c++) if (font.HasCharacter((char)c)) chars.Append((char)c); }
        Range(0x20, 0x7E); Range(0xA0, 0x17F); Range(0x1A0, 0x1A1); Range(0x1AF, 0x1B0); Range(0x1EA0, 0x1EF9);
        foreach (int c in new[] { 0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2026, 0x20AB, 0x20AC })
            if (font.HasCharacter((char)c)) chars.Append((char)c);

        // 1024 units per em, one design pixel = 64 units, so 16 pt = 1 design pixel per atlas pixel.
        string path = CreateStaticRasterAsset(font, OutputFolder + "BoldPixels Pixel.asset", 16, chars.ToString());
        AssetDatabase.SaveAssets();
        Debug.Log($"[Fonts] BoldPixels: {chars.Length} characters -> {path}");
    }

    // ---- sheet reading ----

    sealed class Sheet
    {
        readonly Color32[] _pix; readonly int _w;
        public Sheet(Texture2D t) { _pix = t.GetPixels32(); _w = t.width; }
        public bool Ink(int x, int y) => _pix[y * _w + x].a > 127;
    }

    /// <summary>The ink pixels of one cell, as (x from the cell's left edge, y from the band's baseline row).</summary>
    static HashSet<(int x, int y)> Cut(Sheet s, int cellIndex, int bandY0)
    {
        var set = new HashSet<(int, int)>();
        for (int y = 0; y < Cell; y++)
            for (int x = 0; x < Cell; x++)
                if (s.Ink(cellIndex * Cell + x, bandY0 + y)) set.Add((x, y));
        return set;
    }

    static Glyph Make(HashSet<(int x, int y)> px, params int[] codepoints)
    {
        var g = new Glyph();
        g.codepoints.AddRange(codepoints);
        if (px.Count == 0) { g.advance = 3; return g; }

        int maxX = 0; g.xMin = int.MaxValue; g.yMin = int.MaxValue; g.yMax = int.MinValue;
        foreach (var (x, y) in px)
        {
            g.xMin = Math.Min(g.xMin, x); g.xMax = Math.Max(g.xMax, x + 1);
            g.yMin = Math.Min(g.yMin, y); g.yMax = Math.Max(g.yMax, y + 1);
            maxX = Math.Max(maxX, x);
        }
        g.advance = maxX + 1 + 1;                                   // ink + one pixel of spacing

        // Row runs, then merge identical runs on consecutive rows into one rectangle each.
        var open = new Dictionary<(int, int), int>();               // (x0,x1) -> start row
        for (int y = g.yMin; y <= g.yMax; y++)
        {
            var runs = new List<(int, int)>();
            int start = -1;
            for (int x = 0; x <= Cell; x++)
            {
                bool on = x < Cell && px.Contains((x, y));
                if (on && start < 0) start = x;
                if (!on && start >= 0) { runs.Add((start, x)); start = -1; }
            }
            var next = new Dictionary<(int, int), int>();
            foreach (var r in runs) next[r] = open.TryGetValue(r, out int y0) ? y0 : y;
            foreach (var kv in open)
                if (!next.ContainsKey(kv.Key)) g.rects.Add(new Rect { x0 = kv.Key.Item1, x1 = kv.Key.Item2, y0 = kv.Value, y1 = y });
            open = next;
        }
        return g;
    }

    static HashSet<(int x, int y)> Shift(HashSet<(int x, int y)> px, int dy)
    {
        var o = new HashSet<(int, int)>();
        foreach (var (x, y) in px) o.Add((x, y + dy));
        return o;
    }

    static List<Glyph> ExtractGlyphs(Sheet s, out int capHeight, out int xHeight)
    {
        var list = new List<Glyph>();

        void Row(string chars, int[] cells, int band)
        {
            for (int i = 0; i < chars.Length; i++) list.Add(Make(Cut(s, cells[i], band), chars[i]));
        }
        int[] Seq(int n) { var a = new int[n]; for (int i = 0; i < n; i++) a[i] = i; return a; }
        int[] accentCells = { 0, 4, 8, 13, 14, 20 };                 // under a, e, i, n, o, u
        int[] noN = { 0, 4, 8, 14, 20 };

        Row("0123456789", Seq(10), 176);
        Row("abcdefghijklmnopqrstuvwxyz", Seq(26), 144);
        Row("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Seq(26), 64);
        Row("áéíñóú", accentCells, 128); Row("àèìòù", noN, 112); Row("äëïöü", noN, 96); Row("âêîôû", noN, 80);
        Row("ÁÉÍÑÓÚ", accentCells, 48); Row("ÀÈÌÒÙ", noN, 32); Row("ÄËÏÖÜ", noN, 16); Row("ÂÊÎÔÛ", noN, 0);

        // Symbol band (y0 = 160), cell by cell.
        const int sym = 160;
        list.Add(Make(Cut(s, 0, sym), '+'));
        list.Add(Make(Cut(s, 1, sym), '-'));
        list.Add(Make(Cut(s, 2, sym), '_'));
        list.Add(Make(Cut(s, 3, sym), '='));
        list.Add(Make(Cut(s, 4, sym), '/'));
        list.Add(Make(Cut(s, 5, sym), '\\'));
        list.Add(Make(Cut(s, 6, sym), '*'));
        list.Add(Make(Cut(s, 7, sym), '%'));
        list.Add(Make(Cut(s, 8, sym), '¡'));
        list.Add(Make(Cut(s, 9, sym), '!'));
        list.Add(Make(Cut(s, 10, sym), '¿'));
        list.Add(Make(Cut(s, 11, sym), '?'));
        list.Add(Make(Cut(s, 12, sym), ','));
        list.Add(Make(Cut(s, 13, sym), ';'));
        list.Add(Make(Cut(s, 14, sym), '.'));
        list.Add(Make(Cut(s, 15, sym), '·'));
        // ':' is not on the sheet: the full stop plus the middle dot lifted one pixel leaves the gap a colon needs.
        var colon = Cut(s, 14, sym); colon.UnionWith(Shift(Cut(s, 15, sym), 1)); list.Add(Make(colon, ':'));
        list.Add(Make(Cut(s, 16, sym), '[')); list.Add(Make(Cut(s, 17, sym), ']'));
        list.Add(Make(Cut(s, 18, sym), '(')); list.Add(Make(Cut(s, 19, sym), ')'));
        list.Add(Make(Cut(s, 20, sym), '{')); list.Add(Make(Cut(s, 21, sym), '}'));
        list.Add(Make(Cut(s, 22, sym), '|'));
        // The three quote cells are drawn at the baseline on the sheet; lift them to the top of the capitals.
        list.Add(Make(Shift(Cut(s, 23, sym), 3), '\'', 0x2018, 0x2019));
        list.Add(Make(Shift(Cut(s, 24, sym), 3), '"', 0x201C));
        list.Add(Make(Shift(Cut(s, 25, sym), 3), 0x201D));
        list.Add(Make(Cut(s, 26, sym), '#'));
        list.Add(Make(Cut(s, 27, sym), '@'));

        // '$' is not on the sheet: an S with a stroke through it, one pixel above and below.
        var dollar = Cut(s, 18, 64);                                   // S
        int top = 0; foreach (var (x, y) in dollar) top = Math.Max(top, y);
        dollar.Add((1, top + 1)); dollar.Add((1, -1));
        list.Add(Make(dollar, '$'));

        list.Add(Make(new HashSet<(int, int)>(), ' ', 0xA0));

        capHeight = 0; xHeight = 0;
        foreach (var g in list)
        {
            if (g.codepoints.Contains('H')) capHeight = g.yMax;
            if (g.codepoints.Contains('x')) xHeight = g.yMax;
        }
        return list;
    }

    // ---- TrueType writer (just enough tables for FreeType / Unity) ----

    static class TtfWriter
    {
        sealed class W
        {
            readonly List<byte> _b = new List<byte>();
            public int Length => _b.Count;
            public void U8(int v) => _b.Add((byte)v);
            public void U16(int v) { _b.Add((byte)(v >> 8)); _b.Add((byte)v); }
            public void I16(int v) => U16(v & 0xFFFF);
            public void U32(uint v) { U16((int)(v >> 16)); U16((int)(v & 0xFFFF)); }
            public void Tag(string t) { foreach (char c in t) U8(c); }
            public void Bytes(byte[] a) => _b.AddRange(a);
            public void Pad4() { while (_b.Count % 4 != 0) _b.Add(0); }
            public byte[] ToArray() => _b.ToArray();
            public void SetU32(int at, uint v) { _b[at] = (byte)(v >> 24); _b[at + 1] = (byte)(v >> 16); _b[at + 2] = (byte)(v >> 8); _b[at + 3] = (byte)v; }
        }

        static uint Sum(byte[] data)
        {
            uint sum = 0;
            for (int i = 0; i < data.Length; i += 4)
            {
                uint v = 0;
                for (int k = 0; k < 4; k++) v = (v << 8) | (i + k < data.Length ? data[i + k] : (byte)0);
                sum += v;
            }
            return sum;
        }

        public static byte[] Write(List<Glyph> glyphs, string family, int capHeight, int xHeight)
        {
            const int P = UnitsPerPixel;
            // glyph 0 = .notdef (an outlined box), then every glyph in list order
            var all = new List<Glyph>();
            var notdef = new Glyph { advance = 6 };
            notdef.rects.Add(new Rect { x0 = 0, y0 = 0, x1 = 5, y1 = 7 });
            notdef.xMin = 0; notdef.yMin = 0; notdef.xMax = 5; notdef.yMax = 7;
            all.Add(notdef); all.AddRange(glyphs);
            int n = all.Count;

            int topPx = 0, bottomPx = 0;
            foreach (var g in glyphs) { topPx = Math.Max(topPx, g.yMax); bottomPx = Math.Min(bottomPx, g.yMin); }
            int ascender = (capHeight + 2) * P, descender = -P, lineGap = P;

            // glyf + loca
            var glyf = new W(); var loca = new W(); int maxPoints = 0, maxContours = 0;
            int xMin = int.MaxValue, yMin = int.MaxValue, xMax = int.MinValue, yMax = int.MinValue;
            for (int gi = 0; gi < n; gi++)
            {
                loca.U32((uint)glyf.Length);
                var g = all[gi];
                if (g.rects.Count == 0) continue;
                // .notdef is hollow: outer box + inner box (opposite direction)
                var contours = new List<List<(int x, int y)>>();
                if (gi == 0)
                {
                    contours.Add(Box(0, 0, 5, 7, false)); contours.Add(Box(1, 1, 4, 6, true));
                }
                else foreach (var r in g.rects) contours.Add(Box(r.x0, r.y0, r.x1, r.y1, false));

                int points = 0; foreach (var c in contours) points += c.Count;
                maxPoints = Math.Max(maxPoints, points); maxContours = Math.Max(maxContours, contours.Count);
                int gx0 = int.MaxValue, gy0 = int.MaxValue, gx1 = int.MinValue, gy1 = int.MinValue;
                foreach (var c in contours) foreach (var (x, y) in c) { gx0 = Math.Min(gx0, x); gy0 = Math.Min(gy0, y); gx1 = Math.Max(gx1, x); gy1 = Math.Max(gy1, y); }
                xMin = Math.Min(xMin, gx0 * P); yMin = Math.Min(yMin, gy0 * P); xMax = Math.Max(xMax, gx1 * P); yMax = Math.Max(yMax, gy1 * P);

                glyf.I16(contours.Count); glyf.I16(gx0 * P); glyf.I16(gy0 * P); glyf.I16(gx1 * P); glyf.I16(gy1 * P);
                int end = -1; foreach (var c in contours) { end += c.Count; glyf.U16(end); }
                glyf.U16(0);                                          // no instructions
                for (int i = 0; i < points; i++) glyf.U8(1);          // on-curve, 16-bit deltas
                int px = 0; foreach (var c in contours) foreach (var (x, y) in c) { glyf.I16(x * P - px); px = x * P; }
                int py = 0; foreach (var c in contours) foreach (var (x, y) in c) { glyf.I16(y * P - py); py = y * P; }
                glyf.Pad4();
            }
            loca.U32((uint)glyf.Length);

            // hmtx
            var hmtx = new W(); int advMax = 0;
            foreach (var g in all) { int adv = g.advance * P; advMax = Math.Max(advMax, adv); hmtx.U16(adv); hmtx.I16(g.rects.Count == 0 ? 0 : g.xMin * P); }

            // cmap (format 4, one segment per code point)
            var map = new SortedDictionary<int, int>();
            for (int gi = 1; gi < n; gi++) foreach (int cp in all[gi].codepoints) if (cp <= 0xFFFF) map[cp] = gi;
            int segs = map.Count + 1;
            var cmap = new W();
            cmap.U16(0); cmap.U16(1); cmap.U16(3); cmap.U16(1); cmap.U32(12);
            int sub = 16 + segs * 8;
            cmap.U16(4); cmap.U16(sub); cmap.U16(0);
            int pow = 1, log = 0; while (pow * 2 <= segs) { pow *= 2; log++; }
            cmap.U16(segs * 2); cmap.U16(pow * 2); cmap.U16(log); cmap.U16(segs * 2 - pow * 2);
            foreach (var kv in map) cmap.U16(kv.Key); cmap.U16(0xFFFF);
            cmap.U16(0);
            foreach (var kv in map) cmap.U16(kv.Key); cmap.U16(0xFFFF);
            foreach (var kv in map) cmap.I16(kv.Value - kv.Key); cmap.U16(1);
            for (int i = 0; i < segs; i++) cmap.U16(0);

            // head
            var head = new W();
            head.U32(0x00010000); head.U32(0x00010000); head.U32(0); head.U32(0x5F0F3CF5); head.U16(0x000B); head.U16(UnitsPerEm);
            head.U32(0); head.U32(0); head.U32(0); head.U32(0);
            head.I16(xMin); head.I16(yMin); head.I16(xMax); head.I16(yMax);
            head.U16(0); head.U16(8); head.I16(2); head.I16(1); head.I16(0);

            // hhea
            var hhea = new W();
            hhea.U32(0x00010000); hhea.I16(ascender); hhea.I16(descender); hhea.I16(lineGap); hhea.U16(advMax);
            hhea.I16(0); hhea.I16(0); hhea.I16(xMax); hhea.I16(1); hhea.I16(0); hhea.I16(0);
            for (int i = 0; i < 4; i++) hhea.I16(0);
            hhea.I16(0); hhea.U16(n);

            // maxp
            var maxp = new W();
            maxp.U32(0x00010000); maxp.U16(n); maxp.U16(maxPoints); maxp.U16(maxContours); maxp.U16(0); maxp.U16(0);
            maxp.U16(2); for (int i = 0; i < 8; i++) maxp.U16(0);

            // OS/2 (version 4)
            var os2 = new W();
            os2.U16(4); os2.I16(5 * P); os2.U16(400); os2.U16(5); os2.U16(0);
            os2.I16(P * 6); os2.I16(P * 6); os2.I16(0); os2.I16(P); os2.I16(P * 6); os2.I16(P * 6); os2.I16(0); os2.I16(P * 3); os2.I16(P); os2.I16(P * 3);
            os2.I16(0); for (int i = 0; i < 10; i++) os2.U8(0);
            os2.U32(0x00000003); os2.U32(0); os2.U32(0); os2.U32(0);
            os2.Tag("OVT "); os2.U16(0x0040); os2.U16(0x20); os2.U16(0xFFFF);
            os2.I16(ascender); os2.I16(descender); os2.I16(lineGap); os2.U16(Math.Max(topPx, 1) * P); os2.U16(Math.Max(-bottomPx, 1) * P);
            os2.U32(1); os2.U32(0); os2.I16(xHeight * P); os2.I16(capHeight * P); os2.U16(0); os2.U16(0x20); os2.U16(1);

            // name
            string[] names = { family, "Regular", family + " Regular", family.Replace(" ", "") + "-Regular" };
            int[] ids = { 1, 2, 4, 6 };
            var name = new W(); var strings = new W();
            name.U16(0); name.U16(ids.Length); name.U16(6 + ids.Length * 12);
            for (int i = 0; i < ids.Length; i++)
            {
                name.U16(3); name.U16(1); name.U16(0x409); name.U16(ids[i]); name.U16(names[i].Length * 2); name.U16(strings.Length);
                foreach (char c in names[i]) strings.U16(c);
            }
            name.Bytes(strings.ToArray());

            // post (format 3, no glyph names)
            var post = new W();
            post.U32(0x00030000); post.U32(0); post.I16(-P); post.I16(P); post.U32(0); post.U32(0); post.U32(0); post.U32(0); post.U32(0);

            var tables = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
            {
                { "OS/2", os2.ToArray() }, { "cmap", cmap.ToArray() }, { "glyf", glyf.ToArray() }, { "head", head.ToArray() },
                { "hhea", hhea.ToArray() }, { "hmtx", hmtx.ToArray() }, { "loca", loca.ToArray() }, { "maxp", maxp.ToArray() },
                { "name", name.ToArray() }, { "post", post.ToArray() },
            };

            var file = new W();
            int count = tables.Count; int pw = 1, lg = 0; while (pw * 2 <= count) { pw *= 2; lg++; }
            file.U32(0x00010000); file.U16(count); file.U16(pw * 16); file.U16(lg); file.U16(count * 16 - pw * 16);
            int offset = 12 + count * 16; var records = new List<(string tag, int off, int len, uint sum)>();
            foreach (var kv in tables) { records.Add((kv.Key, offset, kv.Value.Length, Sum(kv.Value))); offset += (kv.Value.Length + 3) & ~3; }
            foreach (var r in records) { file.Tag(r.tag); file.U32(r.sum); file.U32((uint)r.off); file.U32((uint)r.len); }
            int headAt = -1;
            foreach (var kv in tables) { if (kv.Key == "head") headAt = file.Length; file.Bytes(kv.Value); file.Pad4(); }

            uint total = Sum(file.ToArray());
            file.SetU32(headAt + 8, 0xB1B0AFBAu - total);
            return file.ToArray();
        }

        // clockwise (outer) or counter-clockwise (hole) rectangle, y up
        static List<(int x, int y)> Box(int x0, int y0, int x1, int y1, bool hole)
        {
            return hole
                ? new List<(int, int)> { (x0, y0), (x1, y0), (x1, y1), (x0, y1) }
                : new List<(int, int)> { (x0, y0), (x0, y1), (x1, y1), (x1, y0) };
        }
    }
}
