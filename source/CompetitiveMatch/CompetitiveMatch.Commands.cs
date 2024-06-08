/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    [ConsoleCommand("css_ready", "Mark yourself as ready.")]
    public void OnReadyCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && Match.Phase == MatchPhase_t.Warmup && IsPlayerInATeam(player))
        {
            var playerState = GetPlayerState(player);
            if (playerState.Team.Players.Count < (match_max_players.Value / 2))
            {
                playerState.IsReady = true;
                TryStartMatch();
            }
        }
    }

    [ConsoleCommand("css_stay", "Vote to stay in the current team.")]
    public void OnStayCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && Match.Phase == MatchPhase_t.KnifeVote)
        {
            GetPlayerState(player).KnifeVote = KnifeVote_t.Stay;
            TryStartLive();
        }
    }

    [ConsoleCommand("css_switch", "Vote to switch team side.")]
    public void OnSwitchCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && Match.Phase == MatchPhase_t.KnifeVote)
        {
            GetPlayerState(player).KnifeVote = KnifeVote_t.Switch;
            TryStartLive();
        }
    }
    
    [ConsoleCommand("css_start", "Force match start with the players on the server.")]
    [RequiresPermissions("@css/generic")]
    public void OnStartCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && Match.Phase == MatchPhase_t.Warmup)
        {
            foreach (var other in Utilities.GetPlayers().Where(player => !player.IsBot && IsPlayerInATeam(player)))
            {
                GetPlayerState(other).IsReady = true;
            }
            StartMatch();
        }
    }

    [ConsoleCommand("css_restart", "Force match restart.")]
    [RequiresPermissions("@css/generic")]
    public void OnRestartCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && Match.Phase != MatchPhase_t.Warmup)
        {
            StartWarmup();
        }
    }
}
