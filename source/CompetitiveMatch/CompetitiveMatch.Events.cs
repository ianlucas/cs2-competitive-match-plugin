/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

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
                $"bot_quota {match_max_players.Value}"
            });
        }
        else
        {
            ExecuteCommands(new List<string>
            {
                "bot_quota_mode fill",
                "bot_quota 0"
            });
        }
    }

    public void OnMapStart(string mapname)
    {
        IsInitialized = false;
    }

    public void OnTick()
    {
        if (!IsInitialized)
        {
            HandleMatchBotFillValueChanged(null, false);
            StartWarmup();
            IsInitialized = true;
        }
    }
}
