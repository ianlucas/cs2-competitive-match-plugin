/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Text;

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

        if (MatchMap.Phase == MatchPhase_t.Warmup ||
            MatchMap.Phase == MatchPhase_t.Knife ||
            MatchMap.Phase == MatchPhase_t.KnifeVote ||
            MatchMap.Phase == MatchPhase_t.LiveFirstRound)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                var builder = new StringBuilder();
                switch (MatchMap.Phase)
                {
                    case MatchPhase_t.Warmup:
                        var isReady = GetPlayerState(player).IsReady;
                        player.PrintToCenterHtml(
                            GetCallForActionString(
                                color: isReady ? "lime" : "red",
                                state: isReady ? "READY" : "NOT READY",
                                description: isReady ? "Waiting other players..." : "Type !ready (to be ready)."));
                        break;

                    case MatchPhase_t.Knife:
                        if (DiffNow(MatchMap.KnifeStartedAt) < 10)
                        {
                            player.PrintToCenterHtml("KNIFE ROUND COMMENCING");
                        }
                        break;

                    case MatchPhase_t.KnifeVote:
                        var knifeVote = GetPlayerState(player).KnifeVote;
                        var inWinnerTeam = player.Team == MatchMap.KnifeWinner;
                        var timeLeft = FormatTimeLeft(MatchMap.KnifeVoteStartedAt, 120);
                        var didVote = knifeVote != KnifeVote_t.None;
                        var didVoteToStay = knifeVote == KnifeVote_t.Stay;
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
                        if (DiffNow(MatchMap.LiveStartedAt) < 10)
                        {
                            player.PrintToCenterHtml("MATCH COMMENCING");
                        }
                        break;
                }
            });
        }
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player?.IsBot == false)
        {
            if (MatchMap.Phase != MatchPhase_t.Warmup)
            {
                MatchForfeitTimer?.Kill();
            }
            // @todo: loaded matches must prevent player joining teams.
            if (MatchMap.Phase != MatchPhase_t.Warmup)
            {
                var gameRules = GetGameRules();
                var mp_maxrounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>();
                var mp_overtime_maxrounds = ConVar.Find("mp_overtime_maxrounds")?.GetPrimitiveValue<int>();
                var startingTeam = GetPlayerState(player).StartingTeam;
                if (gameRules != null && mp_maxrounds != null && mp_overtime_maxrounds != null)
                {
                    var currentRound = gameRules.TotalRoundsPlayed + 1;
                    var isHalfTime = currentRound <= mp_maxrounds
                        ? currentRound > mp_maxrounds / 2
                        : ((currentRound - mp_maxrounds - 1) % mp_overtime_maxrounds) + 1 > mp_overtime_maxrounds / 3;
                    var assignedTeam = isHalfTime
                        ? startingTeam == CsTeam.Terrorist
                            ? CsTeam.CounterTerrorist
                            : CsTeam.Terrorist
                        : startingTeam;
                    if (player.Team != assignedTeam)
                    {
                        player.ChangeTeam(assignedTeam);
                    }
                }
                else
                {
                    Logger.LogCritical("[CompetitiveMatch] Unable to get CCSGameRules.");
                }
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerJoinTeam(CCSPlayerController? player, CommandInfo _)
    {
        // @todo: loaded matches must prevent player joining teams.
        if (player?.IsBot == false && MatchMap.Phase != MatchPhase_t.Warmup)
        {
            return HookResult.Stop;
        }
        return HookResult.Continue;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        switch (MatchMap.Phase)
        {
            case MatchPhase_t.Knife:
                MatchMap.KnifeStartedAt = Now();
                break;

            case MatchPhase_t.KnifeVote:
                //@todo: test this scenario
                StartLive();
                break;

            case MatchPhase_t.LiveFirstRound:
                MatchMap.LiveStartedAt = Now();
                break;
        }
        return HookResult.Continue;
    }

    public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            if (MatchMap.Phase == MatchPhase_t.Warmup)
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
            if (MatchMap.Phase == MatchPhase_t.KnifeVote)
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
        if (MatchMap.Phase == MatchPhase_t.Knife)
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
                MatchMap.KnifeWinner = knifeWinner;
                MatchMap.KnifeVoteStartedAt = Now();
                MatchMap.Phase = MatchPhase_t.KnifeVote;
                Utilities.GetPlayers().ForEach(player =>
                {
                    player.MVPs = 0;
                    FreezePlayer(player);
                });
            }
            else
            {
                Logger.LogCritical("[CompetitiveMatch] Unable to get CCSGameRules.");
            }
        }
        if (MatchMap.Phase == MatchPhase_t.LiveFirstRound)
        {
            MatchMap.Phase = MatchPhase_t.Live;
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player?.IsBot == false)
        {
            if (MatchMap.Phase == MatchPhase_t.Warmup)
            {
                MatchMap.Players.Remove(player.SteamID);
            }
            else
            {
                if (!Utilities.GetPlayers().Where(other =>
                    other.IsBot == false &&
                    other.SteamID != player.SteamID &&
                    other.Connected == PlayerConnectedState.PlayerConnected).Any())
                {
                    MatchForfeitTimer?.Kill();
                    MatchForfeitTimer = AddTimer(60.0f, () => StartForfeit(), TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        }
        return HookResult.Continue;
    }
}
