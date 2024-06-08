/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;
using System.Xml;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void ExecuteCommands(List<string> commands)
    {
        commands.ForEach(command => Server.ExecuteCommand(command));
    }

    public bool IsPlayerInMatch(CCSPlayerController player)
    {
        return Match.Teams.Any(teamState => teamState.Players.ContainsKey(player.SteamID));
    }

    public MatchTeamState GetPlayerTeam(CCSPlayerController player)
    {
        return Match.Teams
            .FirstOrDefault(teamState => teamState.Players.ContainsKey(player.SteamID))
            ?? GetTeamState(player.Team);
    }

    public MatchPlayerState GetPlayerState(CCSPlayerController player)
    {
        return Match.Teams
            .SelectMany(teamState => teamState.Players)
            .FirstOrDefault(kvp => kvp.Key == player.SteamID).Value
            ?? GetPlayerTeam(player).AddPlayer(player);
    }

    public MatchTeamState GetTeamState(CsTeam team)
    {
        return Match.Teams
            .FirstOrDefault(ts => ts.StartingTeam == team)
            ?? throw new Exception("Team not found.");
    }

    public bool IsPlayerInATeam(CCSPlayerController player)
    {
        return player.Team == CsTeam.Terrorist || player.Team == CsTeam.CounterTerrorist;
    }

    public (int alivePlayers, int totalHealth) GetAlivePlayers(CsTeam team)
    {
        var players = Utilities.GetPlayers().Where(player => player.Team == team);

        int alive = players.Count(player =>
            (player.IsBot ? player.Pawn?.Value : player.PlayerPawn?.Value)?.Health > 0);

        int health = players.Sum(player =>
            (player.IsBot ? player.Pawn?.Value : player.PlayerPawn?.Value)?.Health ?? 0);

        return (alive, health);
    }

    public CsTeam EvaluateKnifeWinnerTeam()
    {
        (int tAlive, int tHealth) = GetAlivePlayers(CsTeam.Terrorist);
        (int ctAlive, int ctHealth) = GetAlivePlayers(CsTeam.CounterTerrorist);

        if (ctAlive != tAlive)
        {
            return ctAlive > tAlive ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
        }
        if (ctHealth != tHealth)
        {
            return ctHealth > tHealth ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
        }

        return (CsTeam)(new Random()).Next(2, 4);
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

    public void SetStringConVar(string name, string value)
    {
        var conVar = ConVar.Find(name);
        if (conVar != null)
        {
            conVar.StringValue = value;
        }
    }

    public void PrintToChatAll3x(string value)
    {
        for (var i = 0; i < 3; i++)
        {
            Server.PrintToChatAll(value);
        }
    }

    public void KillAllTimers()
    {
        foreach (var timer in MatchTimers.Values)
        {
            timer.Kill();
        }
    }

    public void KillTimer(TimerType_t type)
    {
        if (MatchTimers.TryGetValue(type, out var timer))
        {
            timer.Kill();
        }
        MatchTimers.Remove(type);
    }

    public void CreateTimer(TimerType_t type, float interval, Action callback, TimerFlags? flags = null)
    {
        KillTimer(type);
        MatchTimers[type] = AddTimer(interval, callback, flags);
    }
}
