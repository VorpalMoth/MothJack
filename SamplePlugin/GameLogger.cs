using System;
using System.IO;

namespace SamplePlugin;

public class GameLogger
{
    private string logPath;

    public GameLogger()
    {
        var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var logDir = Path.Combine(docPath, "BlackjackDealer");
        Directory.CreateDirectory(logDir);
        var fileName = $"session_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        logPath = Path.Combine(logDir, fileName);
        Log($"╔══════════════════════════════════════════╗");
        Log($"║     BLACKJACK SESSION LOG                ║");
        Log($"║     {DateTime.Now:yyyy-MM-dd HH:mm:ss}               ║");
        Log($"╚══════════════════════════════════════════╝");
        Log($"");
    }

    public void Log(string message)
    {
        try
        {
            File.AppendAllText(logPath, $"{message}{Environment.NewLine}");
        }
        catch { }
    }

    public void LogSessionStart(string dealerName)
    {
        Log($"Dealer: {dealerName}");
        Log($"");
    }

    public void LogPlayerSeated(string playerName)
    {
        Log($"  + {playerName} joined the table");
    }

    public void LogPlayerLeft(string playerName, long bank)
    {
        Log($"  - {playerName} left the table");
    }

    public void LogDealerHand(int total)
    {
        // handled inline now
    }

    public void LogPlayerHand(string playerName, int total)
    {
        // handled inline now
    }

    public void LogCard(string target, int cardValue)
    {
        // handled inline now
    }

    public void LogBust(string playerName, int total)
    {
        // handled inline now
    }

    public void LogHandResult(string playerName, PlayerHandResult result, long bet, long bankAfter)
    {
        // handled inline now
    }

    public void LogDealerResult(string result, int total)
    {
        // handled inline now
    }

    public void LogHandSeparator()
    {
        // handled inline now
    }

    public void LogPlayerSummary(Player player)
    {
        Log($"");
        Log($"┌─── PLAYER SUMMARY: {player.Name} ───");
        Log($"│  Hands Played : {player.HandsPlayed}  (W:{player.HandsWon} L:{player.HandsLost} P:{player.HandsPushed})");
        Log($"│  Total Wagered: {player.TotalBetAmount:N0} gil");
        Log($"│  Net Result   : {(player.Bank >= 0 ? "+" : "")}{player.Bank:N0} gil");
        Log($"└─────────────────────────────────────");
        Log($"");
    }
}
