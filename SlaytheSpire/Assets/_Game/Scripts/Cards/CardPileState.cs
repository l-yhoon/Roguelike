using System;
using System.Collections.Generic;

namespace Roguelike.Cards
{
    public sealed class CardPileState
    {
        private readonly List<string> draw;
        private readonly List<string> hand = new List<string>();
        private readonly List<string> discard = new List<string>();
        private readonly Random random;

        public IReadOnlyList<string> DrawPile { get; }
        public IReadOnlyList<string> Hand { get; }
        public IReadOnlyList<string> DiscardPile { get; }
        public event Action Changed;

        public CardPileState(IReadOnlyList<string> deck, Random random = null)
        {
            this.random = random ?? new Random();
            draw = new List<string>(deck);
            DrawPile = draw.AsReadOnly();
            Hand = hand.AsReadOnly();
            DiscardPile = discard.AsReadOnly();
            ShuffleDrawPile();
        }

        public void Draw(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int previousHandCount = hand.Count;
            for (int i = 0; i < amount; i++)
            {
                if (draw.Count == 0)
                {
                    if (discard.Count == 0) break;
                    draw.AddRange(discard);
                    discard.Clear();
                    ShuffleDrawPile();
                }

                int index = draw.Count - 1;
                hand.Add(draw[index]);
                draw.RemoveAt(index);
            }
            if (hand.Count != previousHandCount) Changed?.Invoke();
        }

        public void DiscardHand()
        {
            if (hand.Count == 0) return;
            discard.AddRange(hand);
            hand.Clear();
            Changed?.Invoke();
        }

        public bool DiscardFromHand(int index)
        {
            if (index < 0 || index >= hand.Count) return false;
            discard.Add(hand[index]);
            hand.RemoveAt(index);
            Changed?.Invoke();
            return true;
        }

        private void ShuffleDrawPile()
        {
            for (int i = draw.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (draw[i], draw[swapIndex]) = (draw[swapIndex], draw[i]);
            }
        }
    }
}
