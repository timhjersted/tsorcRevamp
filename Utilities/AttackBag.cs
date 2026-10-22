using System;
using System.Collections.Generic;
using Terraria;

namespace tsorcRevamp.Utilities
{
    /// <summary>
    /// Shuffled grab bag for enemy move selection (attack-quality-pass §6): deal each card once, refill when empty, and never
    /// repeat the last card while another one is open. Independent weighted rolls starve the rare moves; a bag can't.
    ///
    /// Weight = how many copies the refill adds. Gates (range, cooldown, line of sight, "never X right after Y") are the
    /// caller's predicate: a gated card is skipped but stays in the bag, so a gate only defers a move, never cuts it.
    /// The refill callback runs at refill time, so its contents may depend on the current phase / band.
    ///
    /// Server / single-player only. Never sync the bag itself — sync the card you drew (the attack state) instead.
    /// Draw logic follows the Great Red Knight's ChooseFromBag (RedKnightAttackController.cs).
    /// </summary>
    public sealed class AttackBag<T> where T : struct
    {
        private readonly List<T> cards = new List<T>();
        private readonly List<int> eligibleIndices = new List<int>();
        private readonly Action<List<T>> refill;

        /// <summary>The card drawn last, or null before the first draw. Readable by gate predicates for pair rules.</summary>
        public T? Last { get; private set; }

        /// <summary>Cards still waiting in the bag (debug / logging only).</summary>
        public int Count => cards.Count;

        /// <param name="refill">Adds this pass's cards to the (already empty) list. Add a card twice to double its weight.</param>
        public AttackBag(Action<List<T>> refill)
        {
            this.refill = refill;
        }

        /// <summary>Empty the bag so the next draw refills it. Call on a phase change so the new phase's cards are dealt at once.</summary>
        public void Clear()
        {
            cards.Clear();
        }

        /// <summary>
        /// Draw one card whose gate is open and remove it from the bag. Four passes, stopping at the first that finds a card:
        /// no repeat → allow the repeat (it's the only one open) → refill, no repeat → refill, allow the repeat.
        /// Returns false when nothing anywhere is eligible right now; the caller picks its own fallback move.
        /// </summary>
        public bool TryDraw(Func<T, bool> isEligible, out T drawn)
        {
            if (cards.Count == 0)
            {
                Refill();
            }

            CollectEligible(isEligible, true);

            if (eligibleIndices.Count == 0)
            {
                CollectEligible(isEligible, false);
            }

            if (eligibleIndices.Count == 0)
            {
                Refill();
                CollectEligible(isEligible, true);
            }

            if (eligibleIndices.Count == 0)
            {
                CollectEligible(isEligible, false);
            }

            if (eligibleIndices.Count == 0)
            {
                drawn = default;
                return false;
            }

            int pickedSlot = Main.rand.Next(eligibleIndices.Count);
            int cardIndex = eligibleIndices[pickedSlot];
            drawn = cards[cardIndex];
            cards.RemoveAt(cardIndex);
            Last = drawn;
            return true;
        }

        private void Refill()
        {
            cards.Clear();
            refill(cards);
        }

        // Fills eligibleIndices with every gate-passing card. avoidRepeat skips copies of the last card drawn.
        private void CollectEligible(Func<T, bool> isEligible, bool avoidRepeat)
        {
            eligibleIndices.Clear();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < cards.Count; i++)
            {
                T card = cards[i];
                bool isRepeat = Last.HasValue && comparer.Equals(card, Last.Value);

                if (avoidRepeat && isRepeat)
                {
                    continue;
                }

                if (isEligible(card))
                {
                    eligibleIndices.Add(i);
                }
            }
        }
    }
}
