using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Payout table shown beside a scratch panel: one row per symbol (icon, odds, multiplier) and, optionally, a final block
/// "9x rarest symbol = Win the game" for the Lucky Nine jackpot. Rows are built once from the symbol config, so the table
/// can never drift from the real weights and multipliers.
///
/// Layout is computed in pixels from iconSize and fontSize: rows grow to fit the icon, the columns start after the icon,
/// and the table widens itself if the texts would not fit, so changing those two fields never makes things overlap.
/// </summary>
public class TicketPayoutTable : MonoBehaviour
{
    [SerializeField] private TicketSymbolConfig symbolConfig;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float rowHeight = 34f;
    [SerializeField] private float padding = 8f;
    [SerializeField] private float iconSize = 26f;
    [SerializeField] private int fontSize = 18;
    [SerializeField] private Color textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color headerColor = new Color(0.65f, 0.65f, 0.7f, 1f);
    [SerializeField] private Color winColor = new Color(1f, 0.82f, 0.4f, 1f);

    [Header("Win row")]
    [SerializeField] private bool showWinRow = true;
    [Tooltip("Display only: how many of the rarest symbol the jackpot ticket shows (the number of windows)")]
    [SerializeField] private int winMatchCount = 9;

    private bool _built;

    private void Awake() => Build();

    private void Build()
    {
        if (_built || symbolConfig == null || symbolConfig.symbolPool == null || symbolConfig.symbolPool.Length == 0) return;
        _built = true;

        var pool = symbolConfig.symbolPool;
        float totalWeight = 0f;
        for (int i = 0; i < pool.Length; i++) totalWeight += pool[i].weight;

        var rt = (RectTransform)transform;
        float iconColumn = padding + iconSize + padding;
        float textWidth = fontSize * 2.8f;                                   // room for "27%" and "x0.35"
        float width = Mathf.Max(rt.sizeDelta.x, iconColumn + 2f * textWidth + padding);

        float symbolRowH = Mathf.Max(rowHeight, iconSize + 4f);
        float headerH = Mathf.Max(rowHeight, (fontSize - 4) * 1.4f);
        float winLineH = Mathf.Max(rowHeight, fontSize * 1.4f);
        float height = padding * 2f + headerH + pool.Length * symbolRowH + (showWinRow ? symbolRowH + winLineH : 0f);
        rt.sizeDelta = new Vector2(width, height);

        float columnWidth = (width - iconColumn - padding) * 0.5f;
        float y = padding;

        RectTransform r = MakeRow("Header", y, headerH);
        MakeText(r, "OddsHeader", "odds", iconColumn, columnWidth, TextAlignmentOptions.Center, headerColor, fontSize - 4);
        MakeText(r, "PaysHeader", "pays", iconColumn + columnWidth, columnWidth, TextAlignmentOptions.Right, headerColor, fontSize - 4);
        y += headerH;

        for (int i = 0; i < pool.Length; i++)
        {
            r = MakeRow($"Row{i}", y, symbolRowH);
            MakeIcon(r, pool[i].sprite, padding);
            float pct = totalWeight > 0f ? pool[i].weight / totalWeight * 100f : 0f;
            MakeText(r, "Odds", $"{pct:0.#}%", iconColumn, columnWidth, TextAlignmentOptions.Center, textColor, fontSize);
            MakeText(r, "Pays", $"x{pool[i].payoutMultiplier:0.##}", iconColumn + columnWidth, columnWidth, TextAlignmentOptions.Right, textColor, fontSize);
            y += symbolRowH;
        }

        if (showWinRow)
        {
            // Two lines so the icon never sits on the text: "9x [icon]" centred, then "= Win the game" under it.
            int top = ScratchTicket.IndexOfHighestMultiplier(pool);
            float countWidth = fontSize * 1.8f;
            float groupWidth = countWidth + 4f + iconSize;
            float startX = (width - groupWidth) * 0.5f;

            r = MakeRow("WinRow", y, symbolRowH);
            MakeText(r, "Count", $"{winMatchCount}x", startX, countWidth, TextAlignmentOptions.Right, winColor, fontSize);
            MakeIcon(r, pool[top].sprite, startX + countWidth + 4f);
            y += symbolRowH;

            r = MakeRow("WinText", y, winLineH);
            MakeText(r, "Win", "= Win the game", padding, width - 2f * padding, TextAlignmentOptions.Center, winColor, fontSize);
        }
    }

    private RectTransform MakeRow(string rowName, float top, float height)
    {
        var go = new GameObject(rowName, typeof(RectTransform));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, -top);
        return rt;
    }

    private void MakeIcon(RectTransform parent, Sprite sprite, float x)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(iconSize, iconSize);
        rt.anchoredPosition = new Vector2(x, 0f);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void MakeText(RectTransform parent, string objName, string text, float x, float width,
        TextAlignmentOptions alignment, Color color, int size)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(width, 0f);
        rt.anchoredPosition = new Vector2(x, 0f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
    }
}
