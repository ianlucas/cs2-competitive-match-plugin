/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
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
            var pawn = player.PlayerPawn.Value;
            if (player.Team == team && pawn != null)
            {
                if (pawn.Health > 0) count++;
                totalHealth += pawn.Health;
            }
        });
        return (count, totalHealth);
    }

    // Adapted from https://github.com/shobhit-pathak/MatchZy/blob/b4e4800bda5a72064a72a295695faeef28e3d12f/Utility.cs#L464
    public CsTeam GetKnifeWinner()
    {
        (int tAlive, int tHealth) = GetAlivePlayers(CsTeam.Terrorist);
        (int ctAlive, int ctHealth) = GetAlivePlayers(CsTeam.CounterTerrorist);
        Server.PrintToChatAll($"{tAlive}:{tHealth}");
        Server.PrintToChatAll($"{ctAlive}:{ctHealth}");
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

    public string GetStateString(string color, string state, string description)
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
}

public static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue defaultValue)
            where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
