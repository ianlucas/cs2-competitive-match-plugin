/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    [ConsoleCommand("css_ready", "Mark yourself as ready.")]
    public void OnReadyCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && Phase == MatchPhase.Warmup)
        {
            GetPlayerState(player).IsReady = true;
            TryStartMatch();
        }
    }

    [ConsoleCommand("css_start", "Force match start with the players on the server.")]
    public void OnStartCommand(CCSPlayerController? player, CommandInfo _)
    {
        // @TODO: Check if player is @css/admin.
        if (player != null && Phase == MatchPhase.Warmup)
        {
            // Todo populate player teams
            StartMatch();
        }
    }

    [ConsoleCommand("css_debug", "")]
    public void OnDebugCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null)
        {
            player.GiveNamedItem(CsItem.Deagle);
            player.GiveNamedItem(CsItem.AWP);
        }
    }
}
