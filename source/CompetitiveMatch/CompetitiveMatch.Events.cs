/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void HandleMatchBotFillChange(object? sender, bool value)
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
            PlayerReadyManager.Clear();
            HandleMatchBotFillChange(null, false);
            StartWarmup();
            IsInitialized = true;
        }

        if (Phase == MatchPhase.Warmup)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                var builder = new StringBuilder();
                var isReady = PlayerReadyManager.GetValueOrDefault(player.SteamID, false);
                builder.Append("<b><font class='fontSize-s' color='silver'>");
                builder.Append(match_hostname.Value);
                builder.Append("</font><b/><br><font class='fontSize-l' color='");
                builder.Append(isReady ? "lime" : "red");
                builder.Append("'>");
                builder.Append(isReady ? "READY" : "NOT READY");
                builder.Append("</font><br>");
                builder.Append(isReady ? "Waiting other players..." : "Type !ready to be ready");
                player.PrintToCenterHtml(builder.ToString());
            });
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            if (Phase == MatchPhase.Warmup)
            {
                PlayerReadyManager.Remove(player.SteamID);
            }
        }
        return HookResult.Continue;
    }
}
