using System.Collections.Generic;
using System.Linq;

namespace SamplePlugin;

public class GameState
{
    public bool IsSessionActive { get; set; } = false;
    public string DealerName { get; set; } = string.Empty;
    public List<Player> SeatedPlayers { get; set; } = new();
    public HandPhase CurrentPhase { get; set; } = HandPhase.Waiting;
    public int DealerTotal { get; set; } = 0;
    public string? LastHitPlayerName { get; set; } = null;
    public bool DealerHandActive { get; set; } = false;
    public int LastCardDealt { get; set; } = 0;
    public int HandNumber { get; set; } = 0;

    private record GameSnapshot(
        List<PlayerSnapshot> Players,
        int DealerTotal,
        bool DealerHandActive,
        string? LastHitPlayerName,
        int LastCardDealt,
        int HandNumber
    );

    private Stack<GameSnapshot> undoStack = new();
    private const int MaxUndoSteps = 10;
    private GameLogger logger;

    public GameState(GameLogger logger)
    {
        this.logger = logger;
    }

    public void SaveSnapshot()
    {
        var snapshot = new GameSnapshot(
            SeatedPlayers.Select(p => new PlayerSnapshot
            {
                Name = p.Name,
                Bank = p.Bank,
                CurrentBet = p.CurrentBet,
                IsSeated = p.IsSeated,
                LastHandResult = p.LastHandResult,
                HandTotal = p.HandTotal,
                IsDoubledDown = p.IsDoubledDown,
                IsSplit = p.IsSplit,
                SplitIndex = p.SplitIndex,
                HandComplete = p.HandComplete,
                HandsPlayed = p.HandsPlayed,
                HandsWon = p.HandsWon,
                HandsLost = p.HandsLost,
                HandsPushed = p.HandsPushed,
                TotalBetAmount = p.TotalBetAmount,
                StartingBank = p.StartingBank
            }).ToList(),
            DealerTotal,
            DealerHandActive,
            LastHitPlayerName,
            LastCardDealt,
            HandNumber
        );

        undoStack.Push(snapshot);
        if (undoStack.Count > MaxUndoSteps)
        {
            var temp = undoStack.ToList();
            temp.RemoveAt(temp.Count - 1);
            undoStack = new Stack<GameSnapshot>(temp.AsEnumerable().Reverse());
        }
    }

    public bool CanUndo => undoStack.Count > 0;

    public void Undo()
    {
        if (undoStack.Count == 0) return;

        var snapshot = undoStack.Pop();
        SeatedPlayers = snapshot.Players.Select(s => new Player
        {
            Name = s.Name,
            Bank = s.Bank,
            CurrentBet = s.CurrentBet,
            IsSeated = s.IsSeated,
            LastHandResult = s.LastHandResult,
            HandTotal = s.HandTotal,
            IsDoubledDown = s.IsDoubledDown,
            IsSplit = s.IsSplit,
            SplitIndex = s.SplitIndex,
            HandComplete = s.HandComplete,
            HandsPlayed = s.HandsPlayed,
            HandsWon = s.HandsWon,
            HandsLost = s.HandsLost,
            HandsPushed = s.HandsPushed,
            TotalBetAmount = s.TotalBetAmount,
            StartingBank = s.StartingBank
        }).ToList();

        DealerTotal = snapshot.DealerTotal;
        DealerHandActive = snapshot.DealerHandActive;
        LastHitPlayerName = snapshot.LastHitPlayerName;
        LastCardDealt = snapshot.LastCardDealt;
        HandNumber = snapshot.HandNumber;
        logger.Log($"UNDO: Reverted to previous state ({undoStack.Count} steps remaining)");
    }

    public bool AddPlayer(string name, long startingBank)
    {
        if (SeatedPlayers.Exists(p => p.Name == name && !p.IsSplit))
            return false;

        SeatedPlayers.Add(new Player
        {
            Name = name,
            Bank = startingBank,
            StartingBank = startingBank,
            IsSeated = true
        });
        logger.LogPlayerSeated(name);
        return true;
    }

    public void RemovePlayer(string name)
    {
        var player = GetPlayer(name);
        if (player != null)
            logger.LogPlayerSummary(player);
        SeatedPlayers.RemoveAll(p => p.Name == name);
    }

    public Player? GetPlayer(string name)
    {
        return SeatedPlayers.Find(p => p.Name == name && !p.IsSplit);
    }

    public List<Player> GetAllHandsForPlayer(string name)
    {
        return SeatedPlayers.Where(p => p.Name == name).ToList();
    }

    private void TryReopenHand1(string playerName)
    {
        var hand1 = SeatedPlayers.FirstOrDefault(p =>
            p.Name == playerName && p.SplitIndex == 1 && p.HandComplete);
        if (hand1 != null)
        {
            hand1.HandComplete = false;
            hand1.LastHandResult = PlayerHandResult.None;
            LastHitPlayerName = playerName;
        }
    }

    public void PlayerStand(string playerName)
    {
        SaveSnapshot();
        var activeHand = SeatedPlayers.FirstOrDefault(p =>
            p.Name == playerName && !p.HandComplete);
        if (activeHand == null) return;

        activeHand.HandComplete = true;
        logger.Log($"  {playerName} stands on {activeHand.HandTotal}");

        if (activeHand.SplitIndex == 2)
            TryReopenHand1(playerName);
    }

    public void PlayerBust(string playerName)
    {
        SaveSnapshot();
        var activeHand = SeatedPlayers.FirstOrDefault(p =>
            p.Name == playerName && !p.HandComplete);
        if (activeHand == null) return;

        activeHand.HandComplete = true;
        activeHand.LastHandResult = PlayerHandResult.Bust;
        activeHand.Bank -= activeHand.CurrentBet;
        activeHand.HandsLost++;
        logger.Log($"  {playerName} BUSTS with {activeHand.HandTotal}  (-{activeHand.CurrentBet:N0} gil) | Bank: {activeHand.Bank:N0} gil");

        if (activeHand.SplitIndex == 2)
            TryReopenHand1(playerName);
    }

    public void AddCardToLastHitPlayer(int cardValue)
    {
        SaveSnapshot();
        if (LastHitPlayerName == null) return;

        var player = SeatedPlayers.FirstOrDefault(p =>
            p.Name == LastHitPlayerName && !p.HandComplete);
        if (player == null) return;

        player.HandTotal += cardValue;
        LastCardDealt = cardValue;
        logger.Log($"  {player.Name} draws {cardValue} → total {player.HandTotal}");

        if (player.IsDoubledDown)
        {
            player.HandComplete = true;
            logger.Log($"  {player.Name} DD complete on {player.HandTotal}");
            if (player.SplitIndex == 2)
                TryReopenHand1(player.Name);
        }

        if (player.HandTotal > 21)
        {
            player.HandComplete = true;
            logger.Log($"  {player.Name} BUSTS with {player.HandTotal}");
            if (player.SplitIndex == 2)
                TryReopenHand1(player.Name);
        }
    }

    public void AddCardToDealer(int cardValue)
    {
        SaveSnapshot();
        DealerTotal += cardValue;
        logger.Log($"  Dealer draws {cardValue} → total {DealerTotal}");
    }

    public void SplitPlayer(string name, BankManager bankManager)
    {
        SaveSnapshot();
        var original = GetPlayer(name);
        if (original == null) return;

        original.HandTotal -= LastCardDealt;
        original.IsSplit = true;
        original.SplitIndex = 1;
        original.LastHandResult = PlayerHandResult.None;

        var splitHand = new Player
        {
            Name = name,
            Bank = 0, // starts at 0, consolidated after resolution
            CurrentBet = original.CurrentBet,
            IsSeated = true,
            IsSplit = true,
            SplitIndex = 2,
            HandTotal = LastCardDealt,
            LastHandResult = PlayerHandResult.None
        };

        bankManager.PlaceBet(original, original.CurrentBet);
        var index = SeatedPlayers.IndexOf(original);
        SeatedPlayers.Insert(index + 1, splitHand);
        logger.Log($"  {name} SPLITS — hand1={original.HandTotal} hand2={LastCardDealt}");

        var hand1 = SeatedPlayers.FirstOrDefault(p => p.Name == name && p.SplitIndex == 1);
        if (hand1 != null) hand1.HandComplete = true;
        LastHitPlayerName = name;
    }

    public void DoubleDown(string name, BankManager bankManager)
    {
        SaveSnapshot();
        var player = SeatedPlayers.FirstOrDefault(p =>
            p.Name == name && !p.HandComplete);
        if (player == null) return;

        player.IsDoubledDown = true;
        player.CurrentBet *= 2;
        logger.Log($"  {name} DOUBLES DOWN — bet now {player.CurrentBet:N0} gil");
    }

    public void ResolveHand(Player player, PlayerHandResult result)
    {
        player.HandsPlayed++;
        player.TotalBetAmount += player.CurrentBet;
        player.LastHandResult = result;

        switch (result)
        {
            case PlayerHandResult.Win:
                player.Bank += player.CurrentBet;
                player.HandsWon++;
                break;
            case PlayerHandResult.Lose:
            case PlayerHandResult.Bust:
                player.Bank -= player.CurrentBet;
                player.HandsLost++;
                break;
            case PlayerHandResult.Blackjack:
                player.Bank += (long)(player.CurrentBet * 1.5);
                player.HandsWon++;
                break;
            case PlayerHandResult.Push:
                player.HandsPushed++;
                break;
        }

        string amount = result == PlayerHandResult.Push ? "PUSH" :
                        result == PlayerHandResult.Win || result == PlayerHandResult.Blackjack ? $"+{player.CurrentBet:N0} gil" :
                        $"-{player.CurrentBet:N0} gil";

        if (!player.IsSplit)
            logger.Log($"  {player.Name,-25} {player.HandTotal,3}  →  {result,-10} ({amount}) | Bank: {player.Bank:N0} gil");

        player.HandTotal = 0;
        player.HandComplete = false;
        player.IsDoubledDown = false;
    }

    private void LogSplitResults()
    {
        var splitNames = SeatedPlayers
            .Where(p => p.IsSplit || p.SplitIndex > 0)
            .Select(p => p.Name)
            .Distinct()
            .ToList();

        foreach (var name in splitNames)
        {
            var hand1 = SeatedPlayers.FirstOrDefault(p => p.Name == name && p.SplitIndex == 1);
            var hand2 = SeatedPlayers.FirstOrDefault(p => p.Name == name && p.SplitIndex == 2);
            if (hand1 == null || hand2 == null) continue;

            long hand1Change = hand1.LastHandResult == PlayerHandResult.Win ? hand1.CurrentBet :
                               hand1.LastHandResult == PlayerHandResult.Blackjack ? (long)(hand1.CurrentBet * 1.5) :
                               hand1.LastHandResult == PlayerHandResult.Push ? 0 : -hand1.CurrentBet;

            long hand2Change = hand2.LastHandResult == PlayerHandResult.Win ? hand2.CurrentBet :
                               hand2.LastHandResult == PlayerHandResult.Blackjack ? (long)(hand2.CurrentBet * 1.5) :
                               hand2.LastHandResult == PlayerHandResult.Push ? 0 : -hand2.CurrentBet;

            long combinedChange = hand1Change + hand2Change;
            long consolidatedBank = hand1.Bank + hand2.Bank;
            string combined = combinedChange >= 0 ? $"+{combinedChange:N0} gil" : $"{combinedChange:N0} gil";

            logger.Log($"  {name} SPLIT HANDS:");
            logger.Log($"    Hand 1: {hand1.LastHandResult,-10} ({(hand1Change >= 0 ? "+" : "")}{hand1Change:N0} gil)");
            logger.Log($"    Hand 2: {hand2.LastHandResult,-10} ({(hand2Change >= 0 ? "+" : "")}{hand2Change:N0} gil)");
            logger.Log($"    Combined: {combined} | Bank: {consolidatedBank:N0} gil");
        }
    }

    public void CleanupSplitHands()
    {
        var splitNames = SeatedPlayers
            .Where(p => p.IsSplit || p.SplitIndex > 0)
            .Select(p => p.Name)
            .Distinct()
            .ToList();

        foreach (var name in splitNames)
        {
            var hand1 = SeatedPlayers.FirstOrDefault(p => p.Name == name && p.SplitIndex == 1);
            var hand2 = SeatedPlayers.FirstOrDefault(p => p.Name == name && p.SplitIndex == 2);

            if (hand1 != null && hand2 != null)
                hand1.Bank = hand1.Bank + hand2.Bank;

            if (hand2 != null)
                SeatedPlayers.Remove(hand2);

            if (hand1 != null)
            {
                hand1.IsSplit = false;
                hand1.SplitIndex = 0;
                hand1.LastHandResult = PlayerHandResult.None;
            }
        }
    }

    public void ResolveAllAgainstDealer()
    {
        SaveSnapshot();
        HandNumber++;
        logger.Log($"");
        logger.Log($"=== HAND {HandNumber} RESULTS ===");
        logger.Log($"Dealer stands on {DealerTotal}");

        foreach (var player in SeatedPlayers)
        {
            if (player.LastHandResult == PlayerHandResult.Bust) continue;
            if (player.HandTotal > DealerTotal)
                ResolveHand(player, PlayerHandResult.Win);
            else if (player.HandTotal < DealerTotal)
                ResolveHand(player, PlayerHandResult.Lose);
            else
                ResolveHand(player, PlayerHandResult.Push);
        }

        LogSplitResults();
        logger.Log($"");
        DealerTotal = 0;
        CleanupSplitHands();
    }

    public void ResolveAllWin()
    {
        SaveSnapshot();
        HandNumber++;
        logger.Log($"");
        logger.Log($"=== HAND {HandNumber} RESULTS ===");
        logger.Log($"Dealer BUSTS — all active players win!");

        foreach (var player in SeatedPlayers)
        {
            if (player.LastHandResult != PlayerHandResult.Bust)
                ResolveHand(player, PlayerHandResult.Win);
        }

        LogSplitResults();
        logger.Log($"");
        DealerTotal = 0;
        CleanupSplitHands();
    }

    public void ResolveAllLose()
    {
        SaveSnapshot();
        HandNumber++;
        logger.Log($"");
        logger.Log($"=== HAND {HandNumber} RESULTS ===");
        logger.Log($"Dealer BLACKJACK — all active players lose!");

        foreach (var player in SeatedPlayers)
        {
            if (player.LastHandResult != PlayerHandResult.Bust)
                ResolveHand(player, PlayerHandResult.Lose);
        }

        LogSplitResults();
        logger.Log($"");
        DealerTotal = 0;
        CleanupSplitHands();
    }

    public void ResetHand()
    {
        DealerTotal = 0;
        LastHitPlayerName = null;
        DealerHandActive = false;
        CurrentPhase = HandPhase.Waiting;
        CleanupSplitHands();
        foreach (var player in SeatedPlayers)
        {
            player.HandTotal = 0;
            player.HandComplete = false;
            player.IsDoubledDown = false;
            player.LastHandResult = PlayerHandResult.None;
        }
    }
}

public class PlayerSnapshot
{
    public string Name { get; set; } = string.Empty;
    public long Bank { get; set; }
    public long CurrentBet { get; set; }
    public bool IsSeated { get; set; }
    public PlayerHandResult LastHandResult { get; set; }
    public int HandTotal { get; set; }
    public bool IsDoubledDown { get; set; }
    public bool IsSplit { get; set; }
    public int SplitIndex { get; set; }
    public bool HandComplete { get; set; }
    public int HandsPlayed { get; set; }
    public int HandsWon { get; set; }
    public int HandsLost { get; set; }
    public int HandsPushed { get; set; }
    public long TotalBetAmount { get; set; }
    public long StartingBank { get; set; }
}

public enum HandPhase
{
    Waiting,
    Betting,
    Playing,
    Resolving
}
