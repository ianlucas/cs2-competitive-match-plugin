/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
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
            PlayerStateManager.Clear();
            HandleMatchBotFillChange(null, match_bot_fill.Value);
            ExecuteWarmup();
            IsInitialized = true;
        }

        if (Phase == MatchPhase.Warmup || Phase == MatchPhase.KnifeVote)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                var builder = new StringBuilder();
                switch (Phase)
                {
                    case MatchPhase.Warmup:
                        var isReady = GetPlayerState(player).IsReady;
                        player.PrintToCenterHtml(
                            GetStateString(
                                color: isReady ? "lime" : "red",
                                state: isReady ? "READY" : "NOT READY",
                                description: isReady ? "Waiting other players..." : "Type !ready to be ready"));
                        break;

                    case MatchPhase.KnifeVote:
                        // when round starts must restart the game and keep the players in their current team.
                        var inWinnerTeam = player.Team == KnifeWinner;
                        player.PrintToCenterHtml(
                            GetStateString(
                                color: inWinnerTeam ? "lime" : "red",
                                state: inWinnerTeam ? "WON KNIFE ROUND" : "LOST KNIFE ROUND",
                                description: inWinnerTeam ? "Type !stay (to stay) or !switch (to swich) team sides." : "Waiting opponent team..."));
                        break;
                }

            });
        }
    }

    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo _)
    {
        if (Phase == MatchPhase.Knife)
        {
            var knifeWinner = GetKnifeWinner();
            int reason = 10;
            switch (knifeWinner)
            {
                case CsTeam.CounterTerrorist:
                    reason = 8;
                    break;
                case CsTeam.Terrorist:
                    reason = 9;
                    break;
            }
            @event.Reason = reason;
            // @todo: need to reverse this, isn't working
            KnifeWinner = knifeWinner;
            Phase = MatchPhase.KnifeVote;
            foreach (var player in Utilities.GetPlayers()
                .Where(player => player.PawnIsAlive))
            {
                player.PrintToChat($"reason is {reason}");
                FreezePlayer(player);
            }
            return HookResult.Changed;
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            if (Phase == MatchPhase.Warmup)
            {
                PlayerStateManager.Remove(player.SteamID);
            }
        }
        return HookResult.Continue;
    }
}
