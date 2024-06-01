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

        if (MatchMap.Phase == MatchPhase_t.Warmup)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                if (player.Team != CsTeam.Terrorist &&
                    player.Team != CsTeam.CounterTerrorist)
                {
                    return;
                }
                var builder = new StringBuilder();
                var isReady = GetPlayerState(player).IsReady;
                player.PrintToCenterHtml(
                    GetCallForActionString(
                        color: isReady ? "lime" : "red",
                        state: isReady ? "READY" : "NOT READY",
                        description: isReady ? "Waiting other players..." : "Type !ready (to be ready)."));
            });
        }
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player?.IsBot == false)
        {
            if (MatchMap.Phase == MatchPhase_t.Warmup)
            {
                player.ChangeTeam(CsTeam.Spectator);
            }

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
                    var expectedTeam = isHalfTime
                        ? startingTeam == CsTeam.Terrorist
                            ? CsTeam.CounterTerrorist
                            : CsTeam.Terrorist
                        : startingTeam;
                    if (player.Team != expectedTeam)
                    {
                        player.ChangeTeam(expectedTeam);
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
        if (player?.IsBot == false)
        {
            if (GetPlayerState(player).IsReady)
            {
                return HookResult.Stop;
            }
            if (MatchMap.Phase != MatchPhase_t.Warmup)
            {
                return HookResult.Stop;
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        switch (MatchMap.Phase)
        {
            case MatchPhase_t.Knife:
                Server.PrintToChatAll("{green}[MATCH] KNIFE!");
                Server.PrintToChatAll("{green}[MATCH] KNIFE!");
                Server.PrintToChatAll("{green}[MATCH] KNIFE!");
                break;

            case MatchPhase_t.KnifeVote:
                AnnounceKnifeVote();
                KnifeVoteTimer?.Kill();
                KnifeVoteTimer = AddTimer(17.0f, AnnounceKnifeVote, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                ExecuteWarmup(60);
                break;

            case MatchPhase_t.LiveFirstRound:
                KnifeVoteTimer?.Kill();
                Server.PrintToChatAll("{green}[MATCH] LIVE!");
                Server.PrintToChatAll("{green}[MATCH] LIVE!");
                Server.PrintToChatAll("{green}[MATCH] LIVE!");
                Server.PrintToChatAll("{green}[MATCH] Please be aware that this match has overtime enabled, there is no tie.");
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

    public HookResult OnWarmupEnd(EventWarmupEnd _, GameEventInfo __)
    {
        if (MatchMap.Phase == MatchPhase_t.KnifeVote)
        {
            KnifeVoteTimer?.Kill();
            StartLive();
        }
        return HookResult.Continue;
    }

    public HookResult OnRoundEndPre(EventRoundEnd @event, GameEventInfo _)
    {
        if (MatchMap.Phase == MatchPhase_t.Knife)
        {
            var gameRules = GetGameRules();
            var knifeWinner = EvaluateKnifeWinner();
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
                MatchMap.Phase = MatchPhase_t.KnifeVote;
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

    public HookResult OnCsWinPanelMatch(EventCsWinPanelMatch _, GameEventInfo __)
    {
        var mp_match_restart_delay = ConVar.Find("mp_match_restart_delay")?.GetPrimitiveValue<int>();
        var restartDelay = mp_match_restart_delay != null ? mp_match_restart_delay - 1 : 1;
        // @todo: handle next map here.
        AddTimer((float)restartDelay, () => StartWarmup());
        return HookResult.Continue;
    }
}
