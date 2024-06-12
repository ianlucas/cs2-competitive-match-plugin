/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

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

    public MatchTeamPlayerState GetPlayerState(CCSPlayerController player)
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

    public bool IsPlayerConnected(CCSPlayerController player)
    {
        return player is
        {
            IsValid: true,
            IsHLTV: false,
            Connected: PlayerConnectedState.PlayerConnected
        };
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

    public static CCSGameRules? GetGameRules()
    {
        if (GameRulesProxy?.IsValid == true)
        {
            return GameRulesProxy.GameRules;
        }
        GameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();
        return GameRulesProxy?.GameRules;
    }

    public static CsTeam ToggleTeam(CsTeam team)
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
        MatchTimers.Clear();
    }

    public void KillTimer(Timer_t type)
    {
        if (MatchTimers.TryGetValue(type, out var timer))
        {
            timer.Kill();
        }
        MatchTimers.Remove(type);
    }

    public void CreateTimer(Timer_t type, float interval, Action callback, TimerFlags? flags = null)
    {
        KillTimer(type);
        MatchTimers[type] = AddTimer(interval, callback, flags);
    }

    public void SetPlayerClan(CCSPlayerController player, string clan)
    {
        if (player.Clan != clan)
        {
            player.Clan = clan;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
            new GameEvent("nextlevel_changed", false).FireEvent(false);
        }
    }

    public string GetCsTeamName(CsTeam team)
    {
        return Localizer[team == CsTeam.Terrorist ? "match.t" : "match.ct"];
    }

    public string GetTeamName(MatchTeamState teamState)
    {
        if (teamState.Name != null)
        {
            return teamState.Name;
        }
        var team = teamState.ToCsTeam();
        if (team != null)
        {
            return GetCsTeamName(team.Value);
        }
        return "Unknown";
    }

    public void OverwriteRoundEndPanel(CsTeam team)
    {
        var gameRules = GetGameRules();
        if (gameRules != null)
        {
            var reason = 10;
            var message = "";
            switch (team)
            {
                case CsTeam.CounterTerrorist:
                    reason = 8;
                    message = "#SFUI_Notice_CTs_Win";
                    break;
                case CsTeam.Terrorist:
                    reason = 9;
                    message = "#SFUI_Notice_Terrorists_Win";
                    break;
            }
            gameRules.RoundEndReason = reason;
            gameRules.RoundEndFunFactToken = "";
            gameRules.RoundEndMessage = message;
            gameRules.RoundEndWinnerTeam = (int)team;
            gameRules.RoundEndFunFactData1 = 0;
            gameRules.RoundEndFunFactPlayerSlot = 0;
        }
    }
}
