﻿/*---------------------------------------------------------------------------------------------
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


        if (Phase == MatchPhase_t.LiveFirstRound && (DiffNow(LiveStartedAt + 1) < 7))
        {
            var gameRules = GetGameRules();
            if (gameRules != null)
            {
                gameRules.TeamIntroPeriod = true;
            }
        }

        if (Phase == MatchPhase_t.Warmup ||
            (Phase == MatchPhase_t.Knife && DiffNow(KnifeStartedAt) < 10) ||
            Phase == MatchPhase_t.KnifeVote ||
            (Phase == MatchPhase_t.LiveFirstRound && DiffNow(LiveStartedAt) < 10))
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                var builder = new StringBuilder();
                var playerState = GetPlayerState(player);
                switch (Phase)
                {
                    case MatchPhase_t.Warmup:
                        var isReady = playerState.IsReady;
                        player.PrintToCenterHtml(
                            GetCallForActionString(
                                color: isReady ? "lime" : "red",
                                state: isReady ? "READY" : "NOT READY",
                                description: isReady ? "Waiting other players..." : "Type !ready (to be ready)."));
                        break;

                    case MatchPhase_t.Knife:
                        player.PrintToCenterHtml("KNIFE ROUND COMMENCING");
                        break;

                    case MatchPhase_t.KnifeVote:
                        var inWinnerTeam = player.Team == KnifeWinner;
                        var timeLeft = FormatTimeLeft(KnifeVoteStartedAt, 120);
                        var didVote = playerState.KnifeVote != KnifeVote_t.None;
                        var didVoteToStay = playerState.KnifeVote == KnifeVote_t.Stay;
                        player.PrintToCenterHtml(
                            GetCallForActionString(
                                color: inWinnerTeam ? "lime" : "red",
                                state: inWinnerTeam ? "WON KNIFE ROUND" : "LOST KNIFE ROUND",
                                description: inWinnerTeam
                                    ? didVote
                                        ? didVoteToStay ? "You voted to stay." : "You voted to switch."
                                        : "Type !stay (to stay) or !switch (to switch) team sides."
                                    : "Waiting opponent team...",
                                timeLeft));
                        break;

                    case MatchPhase_t.LiveFirstRound:
                        if (GetGameRules()?.TeamIntroPeriod != true)
                        {
                            player.PrintToCenterHtml("MATCH COMMENCING");
                        }
                        break;
                }
            });
        }
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        switch (Phase)
        {
            case MatchPhase_t.Knife:
                KnifeStartedAt = Now();
                break;

            case MatchPhase_t.KnifeVote:
                //@todo: test this scenario
                StartLive();
                break;

            case MatchPhase_t.LiveFirstRound:
                LiveStartedAt = Now();
                break;
        }
        return HookResult.Continue;
    }

    public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            if (Phase == MatchPhase_t.Warmup)
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
            if (Phase == MatchPhase_t.KnifeVote)
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
        if (Phase == MatchPhase_t.Knife)
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
                Phase = MatchPhase_t.KnifeVote;
                Utilities.GetPlayers().ForEach(player =>
                {
                    player.MVPs = 0;
                    FreezePlayer(player);
                });
            }
            else
            {
                Logger.LogCritical($"[CompetitiveMatch] Unable to get CCSGameRules.");
            }
        }
        if (Phase == MatchPhase_t.LiveFirstRound)
        {
            Phase = MatchPhase_t.Live;
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            if (Phase == MatchPhase_t.Warmup)
            {
                PlayerStateManager.Remove(player.SteamID);
            }
        }
        return HookResult.Continue;
    }
}
