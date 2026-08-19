namespace BlackjackStreakSimulator.Engine;

public class Hand
{
    public List<Card> Cards { get; } = new();

    // This hand's own wager. Ordinarily equal to the seat's current bet, but
    // diverges once double-down or split are in play (each split hand starts
    // matching the original bet, then can move independently, e.g. a double).
    public decimal Bet { get; set; }

    // Set by whoever performs the split when this hand is created from one.
    // Not derivable from Cards alone — two identical cards look the same
    // whether they arrived via a normal deal or a split.
    public bool IsFromSplit { get; set; }

    // Split aces conventionally receive exactly one card each and can't be
    // hit again — basic strategy needs to know this to enforce that rule.
    public bool IsSplitAces { get; set; }

    public void AddCard(Card card)
    {
        Cards.Add(card);
    }

    public int Value => CalculateBestValue().Total;

    // True if an Ace is currently counted as 11 in the best total (e.g. Ace+6
    // is a soft 17). Dealer play depends on this: dealer hits soft 17.
    public bool IsSoft => CalculateBestValue().IsSoft;

    public bool IsBusted => Value > 21;

    // A natural blackjack only counts on the original two-card deal, not a
    // 21 reached via a split hand.
    public bool IsBlackjack => Cards.Count == 2 && Value == 21 && !IsFromSplit;

    // Same-rank only (e.g. two Jacks), not just same value (King + Jack does
    // not qualify) — see CLAUDE.md domain rules.
    public bool CanSplit => Cards.Count == 2 && Cards[0].Rank == Cards[1].Rank;

    public decimal SettleProfit(Hand dealerHand)
    {
        // Ordered most-specific case first — each check below can assume
        // every case above it was already ruled out, so nothing here needs
        // to defensively exclude another category (no "&& !X" needed).
        if (IsBlackjack && dealerHand.IsBlackjack)
        {
            return 0; // both natural: push
        }

        if (IsBlackjack)
        {
            return Bet * 1.5m; // natural beats any non-blackjack hand, 3:2
        }

        if (dealerHand.IsBlackjack)
        {
            return -Bet; // dealer's natural beats any non-blackjack hand
        }

        if (IsBusted)
        {
            return -Bet;
        }

        if (dealerHand.IsBusted)
        {
            return Bet;
        }

        if (Value > dealerHand.Value)
        {
            return Bet;
        }

        if (Value < dealerHand.Value)
        {
            return -Bet;
        }

        return 0; // equal values: push
    }

    private (int Total, bool IsSoft) CalculateBestValue()
    {
        int total = 0;
        int aceCount = 0;

        foreach (Card card in Cards)
        {
            total += card.PointValue;
            if (card.Rank == CardRank.Ace)
            {
                aceCount++;
            }
        }

        // Each Ace starts counted as 11 (per Card.PointValue); demote one at
        // a time to 1 (i.e. subtract 10) for as long as the hand is bust and
        // there's still a soft Ace to demote.
        int softAcesRemaining = aceCount;
        while (total > 21 && softAcesRemaining > 0)
        {
            total -= 10;
            softAcesRemaining--;
        }

        return (total, softAcesRemaining > 0);
    }
}
