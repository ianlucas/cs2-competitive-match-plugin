/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
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

    public void TryStartLive()
    {
        var winnerPlayers = MatchMap.Players.Values.Where(state => state.StartingTeam == MatchMap.KnifeWinner);
        var votesNeeded = (int)Math.Ceiling(winnerPlayers.Count() / 2.0);
        foreach (var knifeVote in new List<KnifeVote_t> { KnifeVote_t.Stay, KnifeVote_t.Switch })
        {
            var count = MatchMap.Players.Values.Where(state => state.KnifeVote == knifeVote).Count();
            if (count == votesNeeded)
            {
                MatchMap.KnifeVoteDecision = knifeVote;
                if (knifeVote == KnifeVote_t.Switch)
                {
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
                StartLive();
                return;
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
}
