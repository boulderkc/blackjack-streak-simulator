
namespace BlackjackStreakSimulator.Engine;

public static class RoundEngine
{
    // Plays a full round, from the initial cards all the way to settling the bets for the wins and losses.
    public static void PlayRound(Hand dealerHand, Shoe shoe, List<Seat> seats)
    {
        // Initial cards
        dealerHand.AddCard(shoe.DrawCard()); // hole card
        dealerHand.AddCard(shoe.DrawCard()); // up card

        foreach (Seat seat in seats)
        {
            seat.NewRound();
            // Draw 2 initial cards for each hand. 
            seat.Hands[0].AddCard(shoe.DrawCard());
            seat.Hands[0].AddCard(shoe.DrawCard());
        }

        // If dealer's starting up card is ace or ten, dealer peeks to see if they have blackjack. If yes, 
        // skip player turns and go directly to settlement. Here in code we don't need to peek, just check
        // if dealerhand is blackjack.
        if (dealerHand.IsBlackjack)
        {
            // Bets for wins and losses settled
            foreach (Seat seat in seats)
            {
                seat.ApplyRoundResult(dealerHand);
            }
            return;
        }

        // Each player takes their turn
        foreach (Seat seat in seats)
        {
            PlaySeatTurn(seat, dealerHand.Cards[1], shoe);
        }

        // Dealer completes their hand
        DealerPlay.Play(dealerHand, shoe);

        // Bets for wins and losses settled
        foreach (Seat seat in seats)
        {
            seat.ApplyRoundResult(dealerHand);
        }

    }

    private static void PlaySeatTurn(Seat seat, Card dealerUpCard, Shoe shoe)
    {
        // Hands.Count is re-checked every iteration, so a split appending a
        // new hand mid-loop gets picked up and played automatically - no
        // recursion needed, even for a hand that itself gets resplit.
        for (int i = 0; i < seat.Hands.Count; i++)
        {
            PlayHand(seat, i, dealerUpCard, shoe);
        }
    }

    private static void PlayHand(Seat seat, int handIndex, Card dealerUpCard, Shoe shoe)
    {
        Hand hand = seat.Hands[handIndex];

        while (true)
        {
            // Recomputed every iteration - a split earlier in this same loop
            // (or another hand entirely) changes how much is already staked.
            decimal remainingBankroll = seat.Bankroll - seat.Hands.Sum(h => h.Bet);
            PlayerAction action = BasicStrategy.GetAction(hand, dealerUpCard, seat.Hands.Count, remainingBankroll);

            switch (action)
            {
                case PlayerAction.Stand:
                    return;

                case PlayerAction.Hit:
                    hand.AddCard(shoe.DrawCard());
                    if (hand.IsBusted)
                    {
                        return;
                    }
                    break; // loop again on this same hand

                case PlayerAction.Double:
                    hand.Bet *= 2;
                    hand.AddCard(shoe.DrawCard());
                    return; // exactly one card, then done regardless of outcome

                case PlayerAction.Split:
                    Split(seat, handIndex, shoe);
                    break; // `hand` still refers to the same object - it now
                           // holds one card plus a fresh one from the split
            }
        }
    }

    private static void Split(Seat seat, int handIndex, Shoe shoe)
    {
        Hand originalHand = seat.Hands[handIndex];
        bool splittingAces = originalHand.Cards[0].Rank == CardRank.Ace;
        decimal bet = originalHand.Bet;

        Card movedCard = originalHand.Cards[1];
        originalHand.Cards.RemoveAt(1);
        originalHand.IsFromSplit = true;
        originalHand.IsSplitAces = splittingAces;
        originalHand.AddCard(shoe.DrawCard());

        Hand newHand = new Hand { Bet = bet, IsFromSplit = true, IsSplitAces = splittingAces };
        newHand.AddCard(movedCard);
        newHand.AddCard(shoe.DrawCard());

        seat.Hands.Add(newHand);
    }
}
