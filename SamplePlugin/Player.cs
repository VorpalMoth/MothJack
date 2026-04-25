namespace SamplePlugin;

public class Player
{
    public string Name { get; set; } = string.Empty;
    public long Bank { get; set; } = 0;
    public long CurrentBet { get; set; } = 0;
    public bool IsSeated { get; set; } = false;
    public PlayerHandResult LastHandResult { get; set; } = PlayerHandResult.None;
    public int HandTotal { get; set; } = 0;
    public bool IsDoubledDown { get; set; } = false;
    public bool IsSplit { get; set; } = false;
    public int SplitIndex { get; set; } = 0;
    public bool HandComplete { get; set; } = false;
    public int HandsPlayed { get; set; } = 0;
    public int HandsWon { get; set; } = 0;
    public int HandsLost { get; set; } = 0;
    public int HandsPushed { get; set; } = 0;
    public long TotalBetAmount { get; set; } = 0;
    public long StartingBank { get; set; } = 0;
}

public enum PlayerHandResult
{
    None,
    Win,
    Lose,
    Push,
    Blackjack,
    Bust
}
