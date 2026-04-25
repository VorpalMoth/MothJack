# MothJack
### A FFXIV Dalamud Plugin for Blackjack Dealers

MothJack is a dealer-side dashboard plugin for running Blackjack games inside Final Fantasy XIV. It listens to party chat macros and automatically tracks hand totals, dealer cards, player banks, splits, doubles, and busts — so the dealer can focus on running the game instead of doing math.

---

## Features

- **Macro-driven** — the dealer's existing macros feed data into the plugin automatically
- **Hand tracking** — cards are tracked in real time for every player and the dealer
- **Split support** — full split hand tracking with separate rows and bank consolidation
- **Double Down** — automatically doubles the bet and closes the hand after one card
- **Bust detection** — automatically detects and announces busts in party chat
- **Active player highlight** — the current player's row is highlighted green
- **Clickable bet editing** — click any bet cell to edit it inline
- **Manual override buttons** — Win / Lose / BJ / Push per player for edge cases
- **10-level undo** — undo the last 10 actions if something goes wrong
- **Session logging** — every hand is logged to a text file in your Documents folder
- **Player summaries** — when a player leaves, their full session stats are logged

---

## Macro Setup

MothJack reads the following party chat messages. Your macros should send these exact phrases via `/p`:

### Session Control
| Action | Party Chat Message |
|--------|-------------------|
| Seat a player | `<t> has taken a seat!` |
| Remove a player | `<t> has relinquished their seat!` |

### Dealer Hand
| Action | Party Chat Message |
|--------|-------------------|
| Start dealer hand | `Dealer's Hand` |
| Dealer draws additional card | `Dealer - Draw` |
| Dealer stands | `Dealer stands on X` |
| Dealer busts | `Bust!` |
| Dealer blackjack | `Blackjack!` |

### Player Hand
| Action | Party Chat Message |
|--------|-------------------|
| Start player hand | `<t>'s Hand` |
| Player hits | `<t> Hits` |
| Player doubles down | `<t> Doubles Down` |
| Player splits | `<t> Splits` |
| Player stands | `<t> Stands` |
| Player busts | `<t> Busts!` |

> **Note:** FFXIV replaces `<t>` with your current target's name automatically. The `/dice party 13` command in your macros is read automatically — MothJack extracts the result and applies it to the correct hand.

---

## Example Macro — Player Hand

```
/p <t>'s Hand <wait.1>
/marking Circle <t> <wait.5>
/bstance <wait.5>
/sheathe
/dice party 13
/dice party 13
```

---

## Example Macro — Player Hits

```
/p <t> Hits <wait.1>
/bstance <wait.5>
/sheathe
/dice party 13
```

---

## Game Rules Supported

- **Face cards (J/Q/K)** counted as 10 automatically
- **Blackjack** pays 1.5x the bet
- **Split** — original bet applies to both hands, player pays for the second hand
- **Double Down** — bet doubles, one card dealt, hand closes
- **Dealer bust** — all active players win automatically
- **Dealer blackjack** — all active players lose automatically
- **Dealer stands** — hands resolved automatically against dealer total

---

## Session Logs

Every session creates a log file at:
```
Documents\BlackjackDealer\session_YYYY-MM-DD_HH-mm-ss.txt
```

Logs include:
- Every card dealt per player and dealer
- Hand-by-hand results with gil amounts
- Split hand breakdowns with combined totals
- Player summaries on exit (hands played, won, lost, net result)

---

## Dashboard UI

| Element | Description |
|---------|-------------|
| **Dealer Total** | Running dealer hand total |
| **Name** | Player name (split hands shown as Name (1) / Name (2)) |
| **Bank** | Running gil balance |
| **Bet** | Click to edit inline |
| **Hand** | Current hand total (shows BUST or DD) |
| **Last Result** | Result of last resolved hand |
| **Actions** | Manual override: Win / Lose / BJ / Push / Remove |
| **Undo** | Reverts the last action (up to 10 levels) |
| **Reset Hand** | Clears hand totals for a new hand |
| **End Session** | Closes the session and clears the table |

---

## Installation

MothJack is a developer plugin and is not listed in the Dalamud plugin installer.

1. Build the project in Visual Studio (Debug x64)
2. In FFXIV, open `/xlsettings` → Experimental → Dev Plugin Locations
3. Add the path to your build output folder
4. Open `/xlplugins` → Dev Tools and enable MothJack
5. Type `/blackjack` in game to open the dashboard

---

## Requirements

- FFXIV with [XIVLauncher](https://github.com/goatcorp/FFXIVQuickLauncher)
- Dalamud plugin framework
- Visual Studio 2022 (to build from source)
- .NET desktop development workload

---

## Notes

- MothJack is dealer-side only — players do not need the plugin installed
- Players interact normally through chat and the dealer uses macros to direct the plugin
- Banks start at 0 and track wins/losses relative to bets placed each hand
- The plugin does not interact with any FFXIV server data — it only reads party chat

---

*Built with Dalamud • Runs on FFXIV Patch 7.x*
