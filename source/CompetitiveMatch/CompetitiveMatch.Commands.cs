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
        if (player != null)
        {
            PlayerReadyManager[player.SteamID] = true;
        }
    }
}
