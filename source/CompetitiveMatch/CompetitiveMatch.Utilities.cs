﻿/*---------------------------------------------------------------------------------------------
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
        if (MatchMap.Players.TryGetValue(player.SteamID, out var state))
        {
            return state;
        }
        var newState = new PlayerState(player);
        newState.Name = player.PlayerName;
        MatchMap.Players[player.SteamID] = newState;
        return newState;
    }

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
    public CsTeam EvaluateKnifeWinner()
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

    public string GetCallForActionString(string color, string state, string description)
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
}
