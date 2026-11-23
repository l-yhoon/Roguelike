using System;
using System.Collections.Generic;

namespace Roguelike.Cards
{
    // Count labels are derived from real collections, not baked into the artwork.
    public sealed class CardPileState
    {
        private readonly List<string> draw;
        private readonly List<string> hand = new List<string>();
        private readonly List<string> discard = new List<string>();
        public IReadOnlyList<string> DrawPile => draw.AsReadOnly();
        public IReadOnlyList<string> Hand => hand.AsReadOnly();
        public IReadOnlyList<string> DiscardPile => discard.AsReadOnly();
        public event Action Changed;
        public CardPileState(IReadOnlyList<string> deck)
        {
            draw = new List<string>();
            for (int i = 0; i < deck.Count; i++) draw.Add(deck[i]);
        }
        public void Draw(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            // Reshuffling and turn-based draws are implemented with the turn system.
            for (int i = 0; i < amount && draw.Count > 0; i++)
            {
                int index = draw.Count - 1;
                hand.Add(draw[index]);
                draw.RemoveAt(index);
            }
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
    }
}
