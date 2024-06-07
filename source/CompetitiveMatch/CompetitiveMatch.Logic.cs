/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void StartWarmup()
    {
        MatchMap = new(/* @todo: for premade matches, pass here default stuff */);
        UpdateStringConVar("mp_teamname_1", "");
        UpdateStringConVar("mp_teamname_2", "");
        OnChangeMatchBotFill(null, match_bot_fill.Value);
        ExecuteWarmup();
    }

    public void TryStartMatch()
    {
        var players = MatchMap.Players.Values.Where(state => state.IsReady);
        if (players.Count() == match_max_players.Value)
        {
            StartMatch();
        }
    }

    public void StartMatch()
    {
        foreach (var playerState in MatchMap.Players.Values)
        {
            var player = playerState.Controller;
            if (player != null)
            {
                playerState.IsReady = true;
                playerState.StartingTeam = player.Team;
                playerState.Name = player.PlayerName;
                var teamIndex = player.TeamNum - 2;
                var team = MatchMap.Teams[teamIndex];
                if (team.LeaderSteamID == 0)
                {
                    team.LeaderSteamID = player.SteamID;
                    team.LeaderName = player.PlayerName;
                    team.Name = $"team_{player.PlayerName}";
                    Server.ExecuteCommand($"mp_teamname_{teamIndex + 1} {team.Name}");
                }
            }
        }
        ExecuteKnife();
        MatchMap.Phase = MatchPhase_t.Knife;
    }

    public void AssignPlayerKnifeVote(CCSPlayerController player, KnifeVote_t vote)
    {
        var playerState = GetPlayerState(player);
        if (playerState.StartingTeam == MatchMap.KnifeWinner && playerState.KnifeVote == KnifeVote_t.None)
        {
            playerState.KnifeVote = vote;
        }
    }

    public void AnnounceKnifeVote()
    {
        var winnerTeam = MatchMap.Teams[(byte)MatchMap.KnifeWinner - 2];
        if (winnerTeam.Name != "")
        {
            Server.PrintToChatAll($"{{green}}[MATCH] Team {winnerTeam.Name} won the knife round, {winnerTeam.LeaderName} needs to !switch or !switch.");
        }
    }

    public void TryStartLive()
    {
        if (!MatchMap.KnifeVoteDemocracy)
        {
            var winnerTeam = MatchMap.Teams[(byte)MatchMap.KnifeWinner - 2];
            foreach (var knifeVote in new List<KnifeVote_t> { KnifeVote_t.Stay, KnifeVote_t.Switch })
            {
                if (MatchMap.Players.Where(player => player.Value.KnifeVote == knifeVote && player.Key == winnerTeam.LeaderSteamID).Any())
                {
                    SetKnifeDecision(knifeVote);
                }
            }
        }
        else
        {
            var winnerPlayers = MatchMap.Players.Values.Where(state => state.StartingTeam == MatchMap.KnifeWinner);
            var votesNeeded = (int)Math.Ceiling(winnerPlayers.Count() / 2.0);
            foreach (var knifeVote in new List<KnifeVote_t> { KnifeVote_t.Stay, KnifeVote_t.Switch })
            {
                var count = MatchMap.Players.Values.Where(state => state.KnifeVote == knifeVote).Count();
                if (count == votesNeeded)
                {
                    SetKnifeDecision(knifeVote);
                    StartLive();
                    return;
                }
            }
        }

    }

    public void SetKnifeDecision(KnifeVote_t knifeVote)
    {
        MatchMap.KnifeVoteDecision = knifeVote;
        var winnerTeam = MatchMap.Teams[(byte)MatchMap.KnifeWinner - 2];
        if (winnerTeam.Name != "")
        {
            var decision = knifeVote == KnifeVote_t.Switch ? "switch sides" : "stay";
            Server.PrintToChatAll($"{{green}} Team {winnerTeam.Name} decided to {decision}!");
        }
        if (knifeVote == KnifeVote_t.Switch)
        {
            var winnerPlayers = MatchMap.Players.Values.Where(state => state.StartingTeam == MatchMap.KnifeWinner);
            var otherPlayers = MatchMap.Players.Values.Where(state => state.StartingTeam != MatchMap.KnifeWinner);
            foreach (var player in otherPlayers)
            {
                player.StartingTeam = ToggleTeam(player.StartingTeam);
            }
            foreach (var player in winnerPlayers)
            {
                player.StartingTeam = ToggleTeam(player.StartingTeam);
            }
        }
    }

    public void StartLive()
    {
        MatchMap.Phase = MatchPhase_t.LiveFirstRound;
        ExecuteLive();
        switch (MatchMap.KnifeVoteDecision)
        {
            case KnifeVote_t.Switch:
                Server.ExecuteCommand("mp_swapteams");
                break;
            default:
                Server.ExecuteCommand("mp_restartgame 1");
                break;
        }
    }

    public void StartForfeit()
    {
        StartWarmup();
    }
}
