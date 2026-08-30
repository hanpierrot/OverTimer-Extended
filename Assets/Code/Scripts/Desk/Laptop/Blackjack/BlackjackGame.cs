using System;
using System.Collections.Generic;

namespace Overtime.Blackjack
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades }

    /// <summary>Rank is 1..13 (1 = Ace, 11/12/13 = J/Q/K).</summary>
    public readonly struct Card
    {
        public readonly int Rank;
        public readonly Suit Suit;

        public Card(int rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public bool IsAce => Rank == 1;
        public int BaseValue => Rank >= 10 ? 10 : Rank;

        public override string ToString()
        {
            string r = Rank switch
            {
                1 => "A", 11 => "J", 12 => "Q", 13 => "K",
                _ => Rank.ToString()
            };
            return r + Suit.ToString()[0];
        }
    }

    public enum HandState { Idle, PlayerTurn, DealerTurn, Resolved }

    public enum Outcome
    {
        None,
        PlayerBust,
        DealerBust,
        PlayerWin,
        DealerWin,
        Push,
        PlayerNatural
    }

    /// <summary>
    /// Pure blackjack logic. No Unity types, no timers, no coroutines.
    /// The game's global Clock provides all time pressure - this class
    /// deliberately has no internal countdown. See DESIGN.md I2.
    ///
    /// House rules (locked):
    ///   - Fresh shuffled 52-card deck per hand (no counting, jam-appropriate)
    ///   - Dealer stands on ALL 17s, including soft 17
    ///   - Double-down: one card only, then auto-stand, payout x2
    ///   - No split, no insurance, no surrender
    /// </summary>
    public class BlackjackGame
    {
        private readonly Random _rng;
        private readonly List<Card> _deck = new List<Card>(52);

        public readonly List<Card> PlayerHand = new List<Card>(8);
        public readonly List<Card> DealerHand = new List<Card>(8);

        public HandState State { get; private set; } = HandState.Idle;
        public Outcome Result { get; private set; } = Outcome.None;
        public bool DoubledDown { get; private set; }

        /// <summary>True while the dealer's hole card should render face down.</summary>
        public bool DealerHoleHidden => State == HandState.PlayerTurn;

        /// <summary>Pass the shared seeded RNG from RngService so runs stay reproducible.</summary>
        public BlackjackGame(Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        // ---------- Hand value ----------

        /// <summary>Best hand total, demoting aces from 11 to 1 as needed.</summary>
        public static int Value(IReadOnlyList<Card> hand)
        {
            int total = 0;
            int aces = 0;

            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].IsAce)
                {
                    aces++;
                    total += 11;
                }
                else
                {
                    total += hand[i].BaseValue;
                }
            }

            while (total > 21 && aces > 0)
            {
                total -= 10;
                aces--;
            }

            return total;
        }

        /// <summary>True if an ace is still counting as 11 (display "soft 17" etc).</summary>
        public static bool IsSoft(IReadOnlyList<Card> hand)
        {
            int hard = 0;
            bool hasAce = false;

            for (int i = 0; i < hand.Count; i++)
            {
                hard += hand[i].BaseValue;
                if (hand[i].IsAce) hasAce = true;
            }

            return hasAce && hard + 10 <= 21;
        }

        public static bool IsNatural(IReadOnlyList<Card> hand)
            => hand.Count == 2 && Value(hand) == 21;

        public int PlayerValue => Value(PlayerHand);
        public int DealerValue => Value(DealerHand);

        /// <summary>Dealer total excluding the hidden hole card, for UI during PlayerTurn.</summary>
        public int DealerUpValue
        {
            get
            {
                if (DealerHand.Count == 0) return 0;
                var up = new List<Card> { DealerHand[0] };
                return Value(up);
            }
        }

        // ---------- Deck ----------

        private void BuildAndShuffle()
        {
            _deck.Clear();

            for (int s = 0; s < 4; s++)
            for (int r = 1; r <= 13; r++)
                _deck.Add(new Card(r, (Suit)s));

            // Fisher-Yates
            for (int i = _deck.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
            }
        }

        private Card Draw()
        {
            if (_deck.Count == 0) BuildAndShuffle();
            Card c = _deck[_deck.Count - 1];
            _deck.RemoveAt(_deck.Count - 1);
            return c;
        }

        // ---------- Flow ----------

        /// <summary>
        /// Deal a new hand. The ante must already have been charged by the caller
        /// (BlackjackApp), because only CountdownTimer may touch the clock.
        /// </summary>
        public void Deal()
        {
            BuildAndShuffle();
            PlayerHand.Clear();
            DealerHand.Clear();
            DoubledDown = false;
            Result = Outcome.None;

            PlayerHand.Add(Draw());
            DealerHand.Add(Draw());   // index 0 = up card
            PlayerHand.Add(Draw());
            DealerHand.Add(Draw());   // index 1 = hole card

            State = HandState.PlayerTurn;

            // Natural resolves immediately - no dealer turn, no decisions.
            if (IsNatural(PlayerHand))
            {
                Result = IsNatural(DealerHand) ? Outcome.Push : Outcome.PlayerNatural;
                State = HandState.Resolved;
            }
        }

        public bool CanHit => State == HandState.PlayerTurn && PlayerValue < 21;

        /// <summary>Double is only legal on the opening two cards.</summary>
        public bool CanDouble => State == HandState.PlayerTurn && PlayerHand.Count == 2;

        public void Hit()
        {
            if (!CanHit) return;

            PlayerHand.Add(Draw());

            if (PlayerValue > 21)
            {
                Result = Outcome.PlayerBust;
                State = HandState.Resolved;
            }
        }

        /// <summary>
        /// Caller must charge the second ante BEFORE calling this.
        /// One card, then auto-stand.
        /// </summary>
        public void DoubleDown()
        {
            if (!CanDouble) return;

            DoubledDown = true;
            PlayerHand.Add(Draw());

            if (PlayerValue > 21)
            {
                Result = Outcome.PlayerBust;
                State = HandState.Resolved;
                return;
            }

            Stand();
        }

        public void Stand()
        {
            if (State != HandState.PlayerTurn) return;

            State = HandState.DealerTurn;

            // Dealer stands on all 17s (soft included).
            while (DealerValue < 17)
                DealerHand.Add(Draw());

            if (DealerValue > 21)          Result = Outcome.DealerBust;
            else if (DealerValue > PlayerValue) Result = Outcome.DealerWin;
            else if (DealerValue < PlayerValue) Result = Outcome.PlayerWin;
            else                                Result = Outcome.Push;

            State = HandState.Resolved;
        }

        public void Reset()
        {
            PlayerHand.Clear();
            DealerHand.Clear();
            DoubledDown = false;
            Result = Outcome.None;
            State = HandState.Idle;
        }

        // ---------- Payout ----------

        /// <summary>
        /// Money awarded for the resolved hand. Doubling multiplies the payout by 2.
        /// Values come from GameConfig - never hardcode them at the call site.
        /// </summary>
        public int Payout(int winPayout, int pushPayout, int naturalPayout)
        {
            int baseAmount = Result switch
            {
                Outcome.PlayerNatural => naturalPayout,
                Outcome.PlayerWin     => winPayout,
                Outcome.DealerBust    => winPayout,
                Outcome.Push          => pushPayout,
                _                     => 0
            };

            // A natural can't be doubled (it resolves on the deal), so this is safe.
            return DoubledDown ? baseAmount * 2 : baseAmount;
        }

        public string ResultText => Result switch
        {
            Outcome.PlayerNatural => "BLACKJACK",
            Outcome.PlayerWin     => "YOU WIN",
            Outcome.DealerBust    => "DEALER BUST",
            Outcome.Push          => "PUSH",
            Outcome.DealerWin     => "DEALER WINS",
            Outcome.PlayerBust    => "BUST",
            _                     => ""
        };
    }
}
