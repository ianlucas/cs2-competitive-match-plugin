/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void OnChangeMatchBotFill(object? sender, bool value)
    {
        var quota = value ? match_max_players.Value : 0;
        ExecuteCommands([
            "bot_quota_mode fill",
            $"bot_quota {quota}"
        ]);
    }

    public void OnMapStart(string mapname)
    {
        IsInitialized = false;
    }

    public void OnTick()
    {
        if (!IsInitialized)
        {
            StartWarmup();
            IsInitialized = true;
        }

        if (Match.Phase == MatchPhase_t.Warmup ||
            match_bot_fill.Value)
        {
            var players = Utilities.GetPlayers();

            if (Match.Phase == MatchPhase_t.Warmup)
            {
                foreach (var player in players)
                {
                    if (!player.IsBot)
                    {
                        var isReady = IsPlayerInATeam(player) && GetPlayerState(player)?.IsReady == true;
                        SetPlayerClan(player, Localizer[isReady ? "match.ready" : "match.not_ready"]);
                    }
                }
            }

            if (match_bot_fill.Value)
            {
                var maxPlayers = match_max_players.Value / 2;
                foreach (var team in MatchTeams)
                {
                    int botCount = 0;
                    int humanCount = 0;
                    int? botToKick = null;
                    foreach (var player in players)
                    {
                        if (player.Team == team)
                        {
                            if (player.IsBot)
                            {
                                botCount++;
                                if (botToKick == null && player.UserId != null)
                                {
                                    botToKick = player.UserId;
                                }
                            }
                            else
                            {
                                humanCount++;
                            }
                        }
                    }
                    if (botCount + humanCount > maxPlayers && botToKick != null)
                    {
                        Server.ExecuteCommand($"kickid {botToKick}");
                    }
                }
            }
        }
    }
}
