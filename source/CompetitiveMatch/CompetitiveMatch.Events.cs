/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void HandleMatchBotFillValueChanged(object? sender, bool value)
    {
        if (value)
        {
            ExecuteCommands(new List<string>
            {
                "bot_quota_mode fill",
                $"bot_quota {match_max_players}"
            });
        } else
        {
            ExecuteCommands(new List<string>
            {
                "bot_quota_mode fill",
                "bot_quota 0"
            });
        }
    }
}
