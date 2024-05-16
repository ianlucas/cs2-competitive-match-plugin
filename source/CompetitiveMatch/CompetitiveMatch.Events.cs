/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public void OnChangeMatchBotFill(object? sender, bool value)
    {
        var quota = value ? match_max_players.Value : 0;
        ExecuteCommands(new List<string>
        {
            "bot_quota_mode fill",
            $"bot_quota {quota}"
        });
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
            OnChangeMatchBotFill(null, match_bot_fill.Value);
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
                            GetCallForActionString(
                                color: isReady ? "lime" : "red",
                                state: isReady ? "READY" : "NOT READY",
                                description: isReady ? "Waiting other players..." : "Type !ready (to be ready)."));
                        break;

                    case MatchPhase.KnifeVote:
                        // when round starts must restart the game and keep the players in their current team.
                        var inWinnerTeam = player.Team == KnifeWinner;
                        var timeLeft = FormatTimeLeft(KnifeVoteStartedAt, 120);
                        player.PrintToCenterHtml(
                            GetCallForActionString(
                                color: inWinnerTeam ? "lime" : "red",
                                state: inWinnerTeam ? "WON KNIFE ROUND" : "LOST KNIFE ROUND",
                                description: inWinnerTeam ? "Type !stay (to stay) or !switch (to switch) team sides." : "Waiting opponent team...",
                                timeLeft));
                        break;
                }

            });
        }
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        if (Phase == MatchPhase.Knife)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                player.PrintToCenter("Knife Round Commencing");
            });
        }
        return HookResult.Continue;
    }

    public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            if (Phase == MatchPhase.Warmup)
            {
                var inGameMoneyServices = player.InGameMoneyServices;
                if (inGameMoneyServices != null)
                {
                    inGameMoneyServices.Account = 16000;
                    Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
                }
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            var playerPawn = player.PlayerPawn.Value;
            var pawn = player.Pawn.Value;
            if (Phase == MatchPhase.KnifeVote)
            {

                if (player.IsBot)
                {
                    if (pawn != null)
                    {
                        pawn.Health = pawn.MaxHealth;
                    }
                }
                else if (playerPawn != null)
                {
                    playerPawn.Health = playerPawn.MaxHealth;
                }
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnRoundEndPre(EventRoundEnd @event, GameEventInfo _)
    {
        if (Phase == MatchPhase.Knife)
        {
            var gameRules = GetGameRules();
            var knifeWinner = GetKnifeWinner();
            if (gameRules != null)
            {
                var reason = 10;
                var message = "";
                switch (knifeWinner)
                {
                    case CsTeam.CounterTerrorist:
                        reason = 8;
                        message = "#SFUI_Notice_CTs_Win";
                        break;
                    case CsTeam.Terrorist:
                        reason = 9;
                        message = "#SFUI_Notice_Terrorists_Win";
                        break;
                }
                gameRules.RoundEndReason = reason;
                gameRules.RoundEndFunFactToken = "";
                gameRules.RoundEndMessage = message;
                gameRules.RoundEndWinnerTeam = (int)knifeWinner;
                gameRules.RoundEndFunFactData1 = 0;
                gameRules.RoundEndFunFactPlayerSlot = 0;
                KnifeWinner = knifeWinner;
                KnifeVoteStartedAt = Now();
                Phase = MatchPhase.KnifeVote;
                Utilities.GetPlayers().ForEach(player => FreezePlayer(player));
            }
            else
            {
                Logger.LogCritical($"[CompetitiveMatch] Unable to get CCSGameRules.");
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnRoundMvpPre(EventRoundMvp @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            if (Phase == MatchPhase.Knife || Phase == MatchPhase.KnifeVote)
            {
                player.MVPs = 0;
            }
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
