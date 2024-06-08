/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void SetPhase(MatchPhase_t phase)
    {
        switch (phase)
        {
            case MatchPhase_t.Knife:
                KillTimer(Timer_t.CommandsPrinter);
                ExecuteKnife();
                break;

            case MatchPhase_t.PreLive:
                KillTimer(Timer_t.KnifeVoteTimeout);
                KillTimer(Timer_t.KnifeVotePrinter);
                ExecuteLive();
                break;
        }
        Match.Phase = phase;
    }

    public void PrintCommands()
    {
        foreach (var player in Utilities.GetPlayers().Where(player => !player.IsBot && IsPlayerInATeam(player)))
        {
            var isReady = GetPlayerState(player)?.IsReady == true;
            Server.PrintToChatAll(Localizer["match.commands", match_servername.Value]);
            if (!isReady)
            {
                Server.PrintToChatAll(Localizer["match.commands_ready", match_servername.Value]);
            }
            Server.PrintToChatAll(Localizer["match.commands_gg", match_servername.Value]);
            Server.PrintToChatAll(Localizer["match.commands_pause", match_servername.Value]);
        }
    }

    public void StartWarmup()
    {
        KillAllTimers();
        Match = new();
        OnChangeMatchBotFill(null, match_bot_fill.Value);
        ExecuteWarmup();
        CreateTimer(Timer_t.CommandsPrinter, ChatInterval, PrintCommands, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }

    public void TryStartMatch()
    {
        var readyCount = Match.Teams.SelectMany(team => team.Players.Values).Count(state => state.IsReady);
        if (readyCount == match_max_players.Value)
        {
            StartMatch();
        }
    }

    public void StartMatch()
    {
        foreach (var (team, index) in Match.Teams.Select((item, index) => (item, index)))
        {
            foreach (var player in team.Players.Values)
            {
                player.IsReady = true;
                var controller = Utilities.GetPlayerFromSteamId(player.SteamID);
                if (controller != null)
                {
                    SetPlayerClan(controller, "");
                }
            }
            var teamNameIndex = team.StartingTeam == CsTeam.Terrorist ? 2 : 1;
            Server.ExecuteCommand($"mp_teamname_{teamNameIndex} {team.Name}");
        }
        SetPhase(match_knife_round_enabled.Value ? MatchPhase_t.Knife : MatchPhase_t.PreLive);
    }

    public void PrintKnifeVote()
    {
        var team = Match.KnifeWinnerTeam;
        if (team != null && team.Leader != null)
        {
            Server.PrintToChatAll(Localizer["match.knife_vote", match_servername.Value, GetTeamName(team), team.Leader.Name]);
        }
    }

    public void TryStartLive()
    {
        foreach (var vote in new List<KnifeVote_t> { KnifeVote_t.Stay, KnifeVote_t.Switch })
        {
            if (Match.KnifeWinnerTeam != null &&
                Match.KnifeWinnerTeam.Players.Where(player => player.Value.KnifeVote == vote && player.Key == Match.KnifeWinnerTeam.Leader?.SteamID).Any())
            {
                var decision = Localizer[vote == KnifeVote_t.Switch ? "match.knife_decision_switch" : "match.knife_decision_switch"];
                Server.PrintToChatAll(Localizer["match.knife_decision", match_servername.Value, GetTeamName(Match.KnifeWinnerTeam), decision]);
                if (vote == KnifeVote_t.Switch)
                {
                    foreach (var team in Match.Teams)
                    {
                        team.StartingTeam = ToggleTeam(team.StartingTeam);
                    }
                    foreach (var player in Utilities.GetPlayers().Where(IsPlayerInATeam))
                    {
                        player.ChangeTeam(ToggleTeam(player.Team));
                    }
                }
                StartLive();
            }
        }
    }

    public void StartLive()
    {
        SetPhase(MatchPhase_t.PreLive);
    }

    public void StartForfeit()
    {
        StartWarmup();
    }
}
