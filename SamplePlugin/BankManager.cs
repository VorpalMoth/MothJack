namespace SamplePlugin;

public class BankManager

{

    public bool PlaceBet(Player player, long betAmount)
    {
        if (betAmount <= 0)
            return false;

        player.CurrentBet = betAmount;
        return true;
    }

    public string FormatGil(long amount)
    {
        return $"{amount:N0} gil";
    }
}
