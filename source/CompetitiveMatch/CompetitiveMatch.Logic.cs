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
        var players = PlayerStateManager.Values.Where(state => state.IsReady);
        if (players.Count() == match_max_players.Value)
        {
            StartMatch();
        }
    }

    public void StartMatch()
    {
        foreach (var playerState in PlayerStateManager.Values)
        {
            var player = playerState.Controller;
            if (player != null)
            {
                playerState.IsReady = true;
                playerState.StartingTeam = player.Team;
            }
        }
        ExecuteKnife();
        Phase = MatchPhase_t.Knife;
    }

    public void AssignPlayerKnifeVote(CCSPlayerController player, KnifeVote_t vote)
    {
        var playerState = GetPlayerState(player);
        if (playerState.StartingTeam == KnifeWinner && playerState.KnifeVote == KnifeVote_t.None)
        {
            playerState.KnifeVote = vote;
        }
    }

    public void TryStartLive()
    {
        var winnerPlayers = PlayerStateManager.Values.Where(state => state.StartingTeam == KnifeWinner);
        var votesNeeded = (int)Math.Ceiling(winnerPlayers.Count() / 2.0);
        foreach (var knifeVote in new List<KnifeVote_t> { KnifeVote_t.Stay, KnifeVote_t.Switch })
        {
            var count = PlayerStateManager.Values.Where(state => state.KnifeVote == knifeVote).Count();
            if (count == votesNeeded)
            {
                KnifeVoteDecision = knifeVote;
                if (knifeVote == KnifeVote_t.Switch)
                {
                    var otherPlayers = PlayerStateManager.Values.Where(state => state.StartingTeam != KnifeWinner);
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
        Phase = MatchPhase_t.LiveFirstRound;
        ExecuteLive();
        switch (KnifeVoteDecision)
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
