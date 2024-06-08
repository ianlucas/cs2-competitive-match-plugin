/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void StartWarmup()
    {
        KillAllTimers();
        Match.Reset();
        OnChangeMatchBotFill(null, match_bot_fill.Value);
        ExecuteWarmup();
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
            }
            var teamNameIndex = team.StartingTeam == CsTeam.Terrorist ? 2 : 1;
            Server.ExecuteCommand($"mp_teamname_{teamNameIndex} {team.Name}");
        }
        ExecuteKnife();
        Match.Phase = MatchPhase_t.Knife;
    }

    public void AnnounceKnifeVote()
    {
        var team = Match.KnifeWinnerTeam;
        if (team != null && team.Name != "" && team.Leader != null)
        {
            Server.PrintToChatAll(Localizer["match.knife_vote", match_servername.Value, team.Name, team.Leader.Name]);
        }
    }

    public void TryStartLive()
    {
        foreach (var vote in new List<KnifeVote_t> { KnifeVote_t.Stay, KnifeVote_t.Switch })
        {
            if (Match.KnifeWinnerTeam != null &&
                Match.KnifeWinnerTeam.Players.Where(player => player.Value.KnifeVote == vote && player.Key == Match.KnifeWinnerTeam.Leader?.SteamID).Any())
            {
                KillTimer(TimerType_t.KnifeVoteTimeout);
                KillTimer(TimerType_t.KnifeVotePrint);
                Match.KnifeVoteDecision = vote;
                var decision = vote == KnifeVote_t.Switch ? "switch sides" : "stay";
                Server.PrintToChatAll(Localizer["match.knife_decision", match_servername.Value, Match.KnifeWinnerTeam.Name, decision]);
                if (vote == KnifeVote_t.Switch)
                {
                    (Match.Teams[1].StartingTeam, Match.Teams[0].StartingTeam) = (Match.Teams[0].StartingTeam, Match.Teams[1].StartingTeam);
                }
                StartLive();
            }
        }
    }

    public void StartLive()
    {
        KillTimer(TimerType_t.KnifeVoteTimeout);
        KillTimer(TimerType_t.KnifeVotePrint);
        Match.Phase = MatchPhase_t.PreLive;
        switch (Match.KnifeVoteDecision)
        {
            case KnifeVote_t.Switch:
                foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
                {
                    player.ChangeTeam(ToggleTeam(player.Team));
                }
                break;
        }
        ExecuteLive();
    }

    public void StartForfeit()
    {
        StartWarmup();
    }
}
