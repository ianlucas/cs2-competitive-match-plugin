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
                    if (!player.IsBot && IsPlayerInATeam(player) && GetPlayerState(player)?.IsReady == false)
                    {
                        player.PrintToCenterAlert(Localizer["match.not_ready"]);
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

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player?.IsBot == false)
        {
            if (Match.Phase != MatchPhase_t.Warmup)
            {
                KillTimer(TimerType_t.MatchForfeit);
            }

            // @todo: loaded matches must prevent player joining teams.
            if (Match.Phase != MatchPhase_t.Warmup)
            {
                var gameRules = GetGameRules();
                var mp_maxrounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>();
                var mp_overtime_maxrounds = ConVar.Find("mp_overtime_maxrounds")?.GetPrimitiveValue<int>();
                var startingTeam = GetPlayerTeam(player).StartingTeam;
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
            if (Match.Phase != MatchPhase_t.Warmup)
            {
                return HookResult.Stop;
            }
            if (IsPlayerInATeam(player))
            {
                var playerState = GetPlayerState(player);
                if (playerState.IsReady)
                {
                    return HookResult.Stop;
                }
                if (Match.Phase == MatchPhase_t.Warmup)
                {
                    playerState.Team.RemovePlayer(player);
                }
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        switch (Match.Phase)
        {
            case MatchPhase_t.Knife:
                PrintToChatAll3x(Localizer["match.knife", match_servername.Value]);
                break;

            case MatchPhase_t.PreKnifeVote:
                SetPhase(MatchPhase_t.KnifeVote);
                PrintKnifeVote();
                CreateTimer(TimerType_t.KnifeVotePrint, ChatInterval, PrintKnifeVote, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                CreateTimer(TimerType_t.KnifeVoteTimeout, 59.0f, StartLive, TimerFlags.STOP_ON_MAPCHANGE);
                ExecuteWarmup(60);
                break;

            case MatchPhase_t.PreLive:
                SetPhase(MatchPhase_t.Live);
                PrintToChatAll3x(Localizer["match.live", match_servername.Value]);
                Server.PrintToChatAll(Localizer["match.live_disclaimer", match_servername.Value]);
                break;
        }
        return HookResult.Continue;
    }

    public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null)
        {
            if (GetGameRules()?.WarmupPeriod == true)
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

    public HookResult OnRoundMvpPre(EventRoundMvp _, GameEventInfo __)
    {
        if (Match.Phase == MatchPhase_t.Knife)
        {
            foreach (var player in Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller").Where(
                        player => player is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected }))
            {
                player.MVPs = 0;
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnRoundEndPre(EventRoundEnd @event, GameEventInfo _)
    {
        switch (Match.Phase)
        {
            case MatchPhase_t.Knife:
                var gameRules = GetGameRules();
                //var team = EvaluateKnifeWinnerTeam();
                var team = CsTeam.Terrorist;
                if (gameRules != null)
                {
                    var reason = 10;
                    var message = "";
                    switch (team)
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
                    gameRules.RoundEndWinnerTeam = (int)team;
                    gameRules.RoundEndFunFactData1 = 0;
                    gameRules.RoundEndFunFactPlayerSlot = 0;
                    Match.KnifeWinnerTeam = GetTeamState(team);
                    SetPhase(MatchPhase_t.PreKnifeVote);
                }
                else
                {
                    Logger.LogCritical("[CompetitiveMatch] Unable to get CCSGameRules.");
                }
                break;
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

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player?.IsBot == false)
        {
            if (Match.Phase == MatchPhase_t.Warmup)
            {
                GetPlayerTeam(player).RemovePlayer(player);
            }
            else
            {
                if (!Utilities.GetPlayers().Where(other =>
                    other.IsBot == false &&
                    other.SteamID != player.SteamID &&
                    other.Connected == PlayerConnectedState.PlayerConnected).Any())
                {
                    CreateTimer(TimerType_t.MatchForfeit, 60.0f, () => StartForfeit(), TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        }
        return HookResult.Continue;
    }
}
