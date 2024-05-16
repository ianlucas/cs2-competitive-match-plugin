/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void ExecuteCommands(List<string> commands)
    {
        commands.ForEach(command => Server.ExecuteCommand(command));
    }

    public PlayerState GetPlayerState(CCSPlayerController player)
    {
        if (player.IsBot)
        {
            return BotState;
        }
        if (PlayerStateManager.TryGetValue(player.SteamID, out var state))
        {
            return state;
        }
        var newState = new PlayerState();
        PlayerStateManager[player.SteamID] = newState;
        return newState;
    }

    private void SetPlayerMoveType(CCSPlayerController player, MoveType_t moveType)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn != null)
        {
            pawn.MoveType = moveType;
            pawn.ActualMoveType = moveType;

            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
    }

    // @todo: minor issue, but player may be sent flying if they jump right before we freeze them...
    private void FreezePlayer(CCSPlayerController player) => SetPlayerMoveType(player, MoveType_t.MOVETYPE_NONE);
    // @todo: remove if not needed
    private void UnfreezePlayer(CCSPlayerController player) => SetPlayerMoveType(player, MoveType_t.MOVETYPE_WALK);

    // Adapted from https://github.com/shobhit-pathak/MatchZy/blob/b4e4800bda5a72064a72a295695faeef28e3d12f/Utility.cs#L281
    public (int alivePlayers, int totalHealth) GetAlivePlayers(CsTeam team)
    {
        int count = 0;
        int totalHealth = 0;
        Utilities.GetPlayers().ForEach(player =>
        {
            var playerPawn = player.PlayerPawn.Value;
            var pawn = player.Pawn.Value;
            if (player.Team == team)
            {
                if (player.IsBot)
                {
                    if (pawn != null)
                    {
                        if (pawn.Health > 0) count++;
                        totalHealth += pawn.Health;
                    }
                }
                else if (playerPawn != null)
                {
                    if (playerPawn.Health > 0) count++;
                    totalHealth += playerPawn.Health;
                }
            }
        });
        return (count, totalHealth);
    }

    // Adapted from https://github.com/shobhit-pathak/MatchZy/blob/b4e4800bda5a72064a72a295695faeef28e3d12f/Utility.cs#L464
    public CsTeam GetKnifeWinner()
    {
        (int tAlive, int tHealth) = GetAlivePlayers(CsTeam.Terrorist);
        (int ctAlive, int ctHealth) = GetAlivePlayers(CsTeam.CounterTerrorist);
        if (ctAlive > tAlive)
        {
            return CsTeam.CounterTerrorist;
        }
        else if (tAlive > ctAlive)
        {
            return CsTeam.Terrorist;
        }
        else if (ctHealth > tHealth)
        {
            return CsTeam.CounterTerrorist;
        }
        else if (tHealth > ctHealth)
        {
            return CsTeam.Terrorist;
        }
        return (CsTeam)(new Random()).Next(2, 4);
    }

    public string GetCallForActionString(string color, string state, string description, string? timeLeft = null)
    {
        var builder = new StringBuilder();
        builder.Append("<b><font class='fontSize-s' color='silver'>");
        builder.Append(match_hostname.Value);
        builder.Append("</font><b/><br><font class='fontSize-l' color='");
        builder.Append(color);
        builder.Append("'>");
        builder.Append(state);
        builder.Append("</font><br>");
        builder.Append(description);
        if (timeLeft != null)
        {
            builder.Append("<br><br>");
            builder.Append(timeLeft);
        }
        return builder.ToString();
    }

    public CCSGameRules? GetGameRules()
    {
        if (GameRulesProxy?.IsValid == true)
        {
            return GameRulesProxy.GameRules;
        }
        GameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();
        return GameRulesProxy?.GameRules;
    }

    public CsTeam ToggleTeam(CsTeam team)
    {
        return team == CsTeam.CounterTerrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
    }

    public long Now()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public long DiffNow(long value)
    {
        return Now() - value;
    }

    public string FormatTimeLeft(long startedAt, long totalTime)
    {
        long elapsedTime = DiffNow(startedAt);
        if (elapsedTime >= totalTime)
        {
            return "0:00";
        }
        long timeLeft = totalTime - elapsedTime;
        long minutesLeft = timeLeft / 60;
        long secondsLeft = timeLeft % 60;
        return $"{minutesLeft}:{secondsLeft:D2}";
    }
}
