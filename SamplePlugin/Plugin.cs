using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using System.Linq;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/blackjack";

    public Configuration Configuration { get; init; }
    public GameState GameState { get; init; }
    public BankManager BankManager { get; init; }
    public GameLogger GameLogger { get; init; }

    public readonly WindowSystem WindowSystem = new("MothJack");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        GameLogger = new GameLogger();
        GameState = new GameState(GameLogger);
        BankManager = new BankManager();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, GameState, BankManager);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the MothJack Dashboard"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        ChatGui.ChatMessage += OnChatMessage;

        Log.Information("MothJack Plugin loaded!");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        ChatGui.ChatMessage -= OnChatMessage;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();

    private string CleanPlayerName(string raw)
    {
        var cleaned = raw.Trim();

        while (cleaned.Length > 0 && !char.IsLetter(cleaned[0]))
            cleaned = cleaned.Substring(1).Trim();

        while (cleaned.Length > 0 && !char.IsLetter(cleaned[^1]))
            cleaned = cleaned.Substring(0, cleaned.Length - 1).Trim();

        return cleaned;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (type != XivChatType.Party) return;

        var msg = message.TextValue.Trim();

        // Dice result
        if (msg.Contains("Random! (1-13)"))
        {
            var parts = msg.Split(' ');
            var lastPart = new string(parts[^1].Where(c => char.IsDigit(c)).ToArray());
            if (int.TryParse(lastPart, out int cardValue) && cardValue >= 1 && cardValue <= 13)
            {
                if (cardValue > 10) cardValue = 10;
                if (GameState.DealerHandActive)
                {
                    GameState.AddCardToDealer(cardValue);
                }
                else
                {
                    GameState.AddCardToLastHitPlayer(cardValue);
                    var player = GameState.SeatedPlayers.FirstOrDefault(p =>
                        p.Name == GameState.LastHitPlayerName && !p.HandComplete);
                    if (player != null && player.HandTotal > 21)
                        ChatGui.Print($"{player.Name} busts with {player.HandTotal}!");
                }
            }
            return;
        }

        // Player seated
        if (msg.EndsWith("has taken a seat!"))
        {
            var playerName = CleanPlayerName(msg.Replace(" has taken a seat!", "").Trim());
            if (!GameState.AddPlayer(playerName, 0))
                ChatGui.Print($"{playerName} is already seated at the table!");
            return;
        }

        // Player left
        if (msg.EndsWith("has relinquished their seat!"))
        {
            var playerName = CleanPlayerName(msg.Replace(" has relinquished their seat!", "").Trim());
            var player = GameState.GetPlayer(playerName);
            if (player != null)
                ChatGui.Print($"{playerName} has left the table with {BankManager.FormatGil(player.Bank)}.");
            GameState.RemovePlayer(playerName);
            return;
        }

        // Dealer's hand - must come BEFORE player hand check
        if (msg.Contains("Dealer's Hand"))
        {
            GameState.DealerHandActive = true;
            GameState.DealerTotal = 0;
            GameState.LastHitPlayerName = null;
            return;
        }

        // Dealer draws
        if (msg.Contains("Dealer - Draw"))
        {
            GameState.DealerHandActive = true;
            return;
        }

        // Player's initial hand
        if (msg.Contains("'s Hand"))
        {
            var playerName = CleanPlayerName(msg.Replace("'s Hand", "").Trim());
            GameState.LastHitPlayerName = playerName;
            GameState.DealerHandActive = false;
            return;
        }

        // Player hits
        if (msg.Contains("Hits"))
        {
            var playerName = CleanPlayerName(msg.Substring(0, msg.IndexOf("Hits")).Trim());
            GameState.LastHitPlayerName = playerName;
            GameState.DealerHandActive = false;
            return;
        }

        // Player doubles down
        if (msg.Contains("Doubles Down"))
        {
            var playerName = CleanPlayerName(msg.Substring(0, msg.IndexOf("Doubles Down")).Trim());
            GameState.DoubleDown(playerName, BankManager);
            GameState.LastHitPlayerName = playerName;
            GameState.DealerHandActive = false;
            return;
        }

        // Player splits
        if (msg.Contains("Splits"))
        {
            var playerName = CleanPlayerName(msg.Substring(0, msg.IndexOf("Splits")).Trim());
            GameState.SplitPlayer(playerName, BankManager);
            GameState.LastHitPlayerName = playerName;
            GameState.DealerHandActive = false;
            return;
        }

        // Player busts
        if (msg.Contains("Busts!") && !msg.Contains("Dealer"))
        {
            var playerName = CleanPlayerName(msg.Substring(0, msg.IndexOf("Busts!")).Trim());
            GameState.PlayerBust(playerName);
            return;
        }

        // Player stands
        if (msg.Contains("Stands") && !msg.Contains("Dealer"))
        {
            var playerName = CleanPlayerName(msg.Substring(0, msg.IndexOf("Stands")).Trim());
            GameState.PlayerStand(playerName);
            GameState.LastHitPlayerName = playerName;
            return;
        }

        // Dealer bust
        if (msg.Contains("Bust!"))
        {
            GameState.ResolveAllWin();
            return;
        }

        // Dealer blackjack
        if (msg.Contains("Blackjack!"))
        {
            GameState.ResolveAllLose();
            return;
        }

        // Dealer stands
        if (msg.Contains("Dealer stands on"))
        {
            GameState.ResolveAllAgainstDealer();
            return;
        }
    }
}
