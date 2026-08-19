namespace BlackjackStreakSimulator.Engine;

// Fixed rule, not an interface — dealer play is never swapped, same reasoning
// as the local/remote engine decision. No state of its own: a pure function
// of the hand and shoe it's given.
public static class DealerPlay
{
    public static void Play(Hand dealerHand, Shoe shoe)
    {
        while (dealerHand.Value < 17 || (dealerHand.Value == 17 && dealerHand.IsSoft))
        {
            dealerHand.AddCard(shoe.DrawCard());
        }
    }
}
