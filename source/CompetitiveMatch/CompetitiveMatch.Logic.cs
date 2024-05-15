/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    // try start match is triggered only when players are using !ready.
    public void TryStartMatch()
    {
        var players = Utilities.GetPlayers();
        if (players.Count == match_max_players.Value)
        {
            foreach (var player in players)
            {
                GetPlayerState(player).StartingTeam = player.Team;
            }
            // @todo check if players are ready!
            StartMatch();
        }
    }

    public void StartMatch()
    {
        ExecuteKnife();
        Phase = MatchPhase.Knife;
    }
}
