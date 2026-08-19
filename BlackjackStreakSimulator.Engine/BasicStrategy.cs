namespace BlackjackStreakSimulator.Engine;

// Fixed decision table used by every seat, including the tracked player —
// no human-controlled play decisions anywhere in the app (see CLAUDE.md).
// Multi-deck, dealer-hits-soft-17 chart; double on any two cards; double
// after split allowed; resplit up to 4 hands, Aces excepted.
public static class BasicStrategy
{
    public static PlayerAction GetAction(Hand hand, Card dealerUpCard, int seatHandCount)
    {
        // Split Aces get exactly one card and never act again, regardless
        // of what that card was — defensive guard, not just RoundEngine's
        // job to remember not to ask again.
        if (hand.IsSplitAces && hand.Cards.Count >= 2)
        {
            return PlayerAction.Stand;
        }

        int dealerValue = dealerUpCard.PointValue;
        bool canSplitFurther = seatHandCount < 4 && !hand.IsSplitAces;

        if (hand.CanSplit && canSplitFurther)
        {
            return GetPairAction(hand.Cards[0].Rank, dealerValue);
        }

        bool canDouble = hand.Cards.Count == 2;

        return hand.IsSoft
            ? GetSoftTotalAction(hand.Value, dealerValue, canDouble)
            : GetHardTotalAction(hand.Value, dealerValue, canDouble);
    }

    private static PlayerAction GetPairAction(CardRank rank, int dealerValue)
    {
        return rank switch
        {
            CardRank.Ace => PlayerAction.Split,

            CardRank.Nine => dealerValue is 7 or 10 or 11
                ? PlayerAction.Stand
                : PlayerAction.Split,

            CardRank.Eight => PlayerAction.Split,

            CardRank.Seven => dealerValue <= 7 ? PlayerAction.Split : PlayerAction.Hit,

            CardRank.Six => dealerValue <= 6 ? PlayerAction.Split : PlayerAction.Hit,

            // Never split 5s — always played as a hard 10 instead.
            CardRank.Five => GetHardTotalAction(10, dealerValue, canDouble: true),

            CardRank.Four => dealerValue is 5 or 6 ? PlayerAction.Split : PlayerAction.Hit,

            CardRank.Three => dealerValue <= 7 ? PlayerAction.Split : PlayerAction.Hit,

            CardRank.Two => dealerValue <= 7 ? PlayerAction.Split : PlayerAction.Hit,

            // Ten, Jack, Queen, King: never split — it's already a hard 20.
            _ => PlayerAction.Stand
        };
    }

    private static PlayerAction GetHardTotalAction(int total, int dealerValue, bool canDouble)
    {
        if (total >= 17)
        {
            return PlayerAction.Stand;
        }

        if (total <= 8)
        {
            return PlayerAction.Hit;
        }

        if (total == 9)
        {
            return canDouble && dealerValue is >= 3 and <= 6
                ? PlayerAction.Double
                : PlayerAction.Hit;
        }

        if (total == 10)
        {
            return canDouble && dealerValue <= 9
                ? PlayerAction.Double
                : PlayerAction.Hit;
        }

        if (total == 11)
        {
            // H17: double vs everything, including a dealer Ace.
            return canDouble ? PlayerAction.Double : PlayerAction.Hit;
        }

        if (total == 12)
        {
            return dealerValue is >= 4 and <= 6 ? PlayerAction.Stand : PlayerAction.Hit;
        }

        // 13-16
        return dealerValue is >= 2 and <= 6 ? PlayerAction.Stand : PlayerAction.Hit;
    }

    private static PlayerAction GetSoftTotalAction(int total, int dealerValue, bool canDouble)
    {
        if (total >= 20)
        {
            return PlayerAction.Stand;
        }

        if (total == 19)
        {
            // "Double if allowed, else stand" — soft 19 is already strong.
            return canDouble && dealerValue == 6 ? PlayerAction.Double : PlayerAction.Stand;
        }

        if (total == 18)
        {
            if (dealerValue is 7 or 8)
            {
                return PlayerAction.Stand;
            }
            if (dealerValue is 9 or 10 or 11)
            {
                return PlayerAction.Hit;
            }
            // dealer 2-6: double if allowed, else stand (not hit).
            return canDouble ? PlayerAction.Double : PlayerAction.Stand;
        }

        if (total == 17)
        {
            return canDouble && dealerValue is >= 3 and <= 6
                ? PlayerAction.Double
                : PlayerAction.Hit;
        }

        if (total is 15 or 16)
        {
            return canDouble && dealerValue is >= 4 and <= 6
                ? PlayerAction.Double
                : PlayerAction.Hit;
        }

        // soft 13, 14 (and the practically-unreachable soft 12)
        return canDouble && dealerValue is 5 or 6
            ? PlayerAction.Double
            : PlayerAction.Hit;
    }
}
