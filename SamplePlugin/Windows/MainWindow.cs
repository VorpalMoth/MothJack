using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin plugin;
    private GameState gameState;
    private BankManager bankManager;

    private string? editingBetPlayerName = null;
    private string betEditInput = string.Empty;

    public MainWindow(Plugin plugin, GameState gameState, BankManager bankManager)
        : base("MothJack Dashboard##mothjack",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        this.gameState = gameState;
        this.bankManager = bankManager;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!gameState.IsSessionActive)
        {
            ImGui.Text("No active session");
            if (ImGui.Button("Start Session"))
            {
#pragma warning disable CS0618
                var playerName = Plugin.ClientState.LocalPlayer?.Name.TextValue ?? "Unknown";
#pragma warning restore CS0618
                gameState.DealerName = playerName;
                gameState.IsSessionActive = true;
            }
            return;
        }

        // Header
        ImGui.Text($"Dealer: {gameState.DealerName}");
        ImGui.SameLine();
        ImGui.Text($"  Phase: {gameState.CurrentPhase}");
        ImGui.SameLine();
        if (ImGui.Button("End Session"))
        {
            gameState.IsSessionActive = false;
            gameState.SeatedPlayers.Clear();
            gameState.DealerName = string.Empty;
        }
        ImGui.SameLine();
        if (ImGui.Button("Reset Hand"))
        {
            gameState.ResetHand();
        }

        ImGui.SameLine();
        if (ImGui.Button("Undo"))
        {
            if (gameState.CanUndo)
                gameState.Undo();
        }

        // Dealer total
        ImGui.Separator();
        ImGui.Text($"Dealer Total: {(gameState.DealerTotal > 0 ? gameState.DealerTotal.ToString() : "-")}");
        ImGui.Separator();

        // Players table
        ImGui.Text("Seated Players");
        if (ImGui.BeginTable("players", 6,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Bank", ImGuiTableColumnFlags.WidthFixed, 110);
            ImGui.TableSetupColumn("Bet", ImGuiTableColumnFlags.WidthFixed, 110);
            ImGui.TableSetupColumn("Hand", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Last Result", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var player in gameState.SeatedPlayers)
            {
                ImGui.TableNextRow();
                bool isActive = player.Name == gameState.LastHitPlayerName && !player.HandComplete;
                if (isActive)
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.2f, 0.5f, 0.2f, 0.5f)));

                // Name
                ImGui.TableNextColumn();
                var displayName = player.IsSplit ? $"{player.Name} ({player.SplitIndex})" : player.Name;
                ImGui.Text(displayName);

                // Bank
                ImGui.TableNextColumn();
                ImGui.Text(bankManager.FormatGil(player.Bank));

                // Bet - clickable to edit, closes on click away
                ImGui.TableNextColumn();
                if (editingBetPlayerName == displayName)
                {
                    ImGui.SetNextItemWidth(100);
                    ImGui.InputText($"##bet{displayName}", ref betEditInput, 32);
                    if (ImGui.IsItemDeactivated())
                    {
                        if (long.TryParse(betEditInput, out long newBet))
                            bankManager.PlaceBet(player, newBet);
                        editingBetPlayerName = null;
                        betEditInput = string.Empty;
                    }
                }
                else
                {
                    if (ImGui.Selectable(bankManager.FormatGil(player.CurrentBet)))
                    {
                        editingBetPlayerName = displayName;
                        betEditInput = player.CurrentBet.ToString();
                    }
                }

                // Hand total
                ImGui.TableNextColumn();
                var handDisplay = player.HandTotal > 0 ? player.HandTotal.ToString() : "-";
                if (player.HandTotal > 21)
                    handDisplay += " BUST";
                else if (player.HandComplete && player.IsDoubledDown)
                    handDisplay += " DD";
                ImGui.Text(handDisplay);

                // Last Result
                ImGui.TableNextColumn();
                ImGui.Text(player.LastHandResult.ToString());

                // Actions
                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"Win##{displayName}"))
                    gameState.ResolveHand(player, PlayerHandResult.Win);
                ImGui.SameLine();
                if (ImGui.SmallButton($"Lose##{displayName}"))
                    gameState.ResolveHand(player, PlayerHandResult.Lose);
                ImGui.SameLine();
                if (ImGui.SmallButton($"BJ##{displayName}"))
                    gameState.ResolveHand(player, PlayerHandResult.Blackjack);
                ImGui.SameLine();
                if (ImGui.SmallButton($"Push##{displayName}"))
                    gameState.ResolveHand(player, PlayerHandResult.Push);
                ImGui.SameLine();
                if (ImGui.SmallButton($"Remove##{displayName}"))
                    gameState.RemovePlayer(player.Name);
            }
            ImGui.EndTable();
        }
    }
}
