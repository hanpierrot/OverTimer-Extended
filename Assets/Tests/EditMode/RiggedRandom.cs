using System;
using System.Collections.Generic;
using Overtime.Blackjack;

/// <summary>
/// Bộ bài xếp sẵn cho BlackjackGame. BlackjackGame xào bài bằng Fisher-Yates
/// (j = rng.Next(i + 1), i chạy từ 51 xuống 1) rồi rút từ cuối deck. Lớp này tính trước
/// dãy j để sau khi xào, các lá được rút ra đúng theo thứ tự drawOrder.
/// Các lá không nêu trong drawOrder nằm ở đầu deck (rút sau cùng).
/// </summary>
public class RiggedRandom : Random
{
    private readonly Queue<int> _swaps = new Queue<int>();

    /// <param name="drawOrder">Lá rút ra lần lượt. Thứ tự chia của Deal(): người chơi, nhà cái, người chơi, nhà cái.</param>
    public RiggedRandom(params Card[] drawOrder)
    {
        // Deck trước khi xào, giống BlackjackGame.BuildAndShuffle().
        var deck = new List<Card>(52);
        for (int s = 0; s < 4; s++)
        for (int r = 1; r <= 13; r++)
            deck.Add(new Card(r, (Suit)s));

        // Deck mong muốn: lá rút thứ k nằm ở vị trí 51 - k.
        var target = new Card[52];
        var used = new HashSet<(int, Suit)>();
        for (int k = 0; k < drawOrder.Length; k++)
        {
            if (!used.Add((drawOrder[k].Rank, drawOrder[k].Suit)))
                throw new ArgumentException($"Lá {drawOrder[k]} bị lặp trong drawOrder.");
            target[51 - k] = drawOrder[k];
        }

        int fill = 0;
        foreach (var c in deck)
        {
            if (used.Contains((c.Rank, c.Suit))) continue;
            target[fill++] = c;
        }

        // Mô phỏng Fisher-Yates và ghi lại j cần thiết ở mỗi bước.
        for (int i = 51; i > 0; i--)
        {
            int j = IndexOf(deck, target[i], i);
            _swaps.Enqueue(j);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    private static int IndexOf(List<Card> deck, Card card, int upTo)
    {
        for (int i = 0; i <= upTo; i++)
            if (deck[i].Rank == card.Rank && deck[i].Suit == card.Suit) return i;
        throw new InvalidOperationException($"Không tìm thấy {card} trong deck.");
    }

    public override int Next(int maxValue)
    {
        if (_swaps.Count == 0)
            throw new InvalidOperationException("RiggedRandom chỉ dùng cho một lần xào bài (một lần Deal).");
        return _swaps.Dequeue();
    }
}
