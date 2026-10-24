using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>Tiện ích cho test thẻ bài: lấy asset thẻ thật, đặt thẻ lên bàn, mở gói thẻ có nội dung định trước.</summary>
public static class Cards
{
    public static CardSO Asset(string name)
    {
        var card = Resources.Load<CardSO>($"CardSO/{name}");
        Assert.IsNotNull(card, $"Không có thẻ Resources/CardSO/{name}");
        return card;
    }

    /// <summary>Thẻ thường (không hiệu ứng) để lấp Hand.</summary>
    public static CardSO Plain(int i) => Asset(new[] { "Ari", "Han", "Sybil", "CtrlZ", "DeeKay", "Steve", "BEFDragon" }[i]);

    public static CardPackRevealPanel RevealPanel => TestHooks.Find<CardPackRevealPanel>();

    /// <summary>Đặt thẻ lên bàn qua đúng đường nút Place gọi (CardPackRevealPanel.TryPlaceCard).</summary>
    public static CardItem Place(CardSO card)
    {
        var before = CardHandManager.Instance.Placed.ToArray();
        Assert.IsTrue(RevealPanel.TryPlaceCard(card), $"Không đặt được thẻ {card.name} (hết CardItem trong pool?)");
        return CardHandManager.Instance.Placed.Except(before).Single();
    }

    public static void FillHand(int count)
    {
        for (int i = 0; i < count; i++) Place(Plain(i));
        Assert.AreEqual(count, CardHandManager.Instance.Placed.Count);
    }

    public static int HandCount => CardHandManager.Instance.Placed.Count;

    public static CardRevealSlot[] Slots => TestHooks.GetField<CardRevealSlot[]>(RevealPanel, "slots");

    /// <summary>Mở bảng gói thẻ với 3 thẻ định trước (bỏ qua bước tung ngẫu nhiên của CardBox).</summary>
    public static void BeginPack(params CardSO[] cards)
    {
        RevealPanel.Begin(cards);
        TestHooks.Call(RevealPanel, "OnPackClicked");
    }

    public static bool ButtonVisible(CardRevealSlot slot, string field) =>
        TestHooks.GetField<UnityEngine.UI.Button>(slot, field).gameObject.activeSelf;

    public static CardBox Box => TestHooks.Find<CardBox>();

    public static void ClickBox() => Box.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);
}
