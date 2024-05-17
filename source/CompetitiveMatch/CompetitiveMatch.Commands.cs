/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    [ConsoleCommand("css_ready", "Mark yourself as ready.")]
    public void OnReadyCommand(CCSPlayerController? player, CommandInfo _)
    {
        // @todo: check player can be ready.
        if (player != null && MatchMap.Phase == MatchPhase_t.Warmup)
        {
            GetPlayerState(player).IsReady = true;
            TryStartMatch();
        }
    }

    [ConsoleCommand("css_stay", "Vote to stay in the current team.")]
    public void OnStayCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && MatchMap.Phase == MatchPhase_t.KnifeVote)
        {
            AssignPlayerKnifeVote(player, KnifeVote_t.Stay);
            TryStartLive();
        }
    }

    [ConsoleCommand("css_switch", "Vote to switch team side.")]
    public void OnSwitchCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && MatchMap.Phase == MatchPhase_t.KnifeVote)
        {
            AssignPlayerKnifeVote(player, KnifeVote_t.Switch);
            TryStartLive();
        }
    }

    [ConsoleCommand("css_start", "Force match start with the players on the server.")]
    public void OnStartCommand(CCSPlayerController? player, CommandInfo _)
    {
        // @TODO: Check if player is @css/admin.
        if (player != null && MatchMap.Phase == MatchPhase_t.Warmup)
        {
            StartMatch();
        }
    }

    [ConsoleCommand("css_restart", "Force match restart.")]
    public void OnRestartCommand(CCSPlayerController? player, CommandInfo _)
    {
        // @TODO: Check if player is @css/admin.
        if (player != null && MatchMap.Phase != MatchPhase_t.Warmup)
        {
            StartWarmup();
        }
    }
}
