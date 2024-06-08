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

namespace CompetitiveMatch;

public partial class CompetitiveMatch
{
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player?.IsBot == false)
        {
            if (Match.Phase != MatchPhase_t.Warmup)
            {
                KillTimer(Timer_t.MatchForfeit);
            }

            // @todo: loaded matches must prevent player joining teams.
            if (Match.Phase != MatchPhase_t.Warmup)
            {
                try
                {
                    if (IsPlayerInMatch(player))
                    {
                        var team = GetPlayerTeam(player);
                        var expectedTeam = team.ToCsTeam();
                        if (expectedTeam != null && player.Team != expectedTeam)
                        {
                            player.ChangeTeam(expectedTeam.Value);
                        }
                    }
                    else
                    {
                        // @todo: kick player
                        player.ChangeTeam(CsTeam.Spectator);
                    }
                }
                catch
                {
                    // @todo: kick player
                    player.ChangeTeam(CsTeam.Spectator);
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
                CreateTimer(Timer_t.KnifeVotePrinter, ChatInterval, PrintKnifeVote, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                CreateTimer(Timer_t.KnifeVoteTimeout, 59.0f, StartLive, TimerFlags.STOP_ON_MAPCHANGE);
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
            //-- Infinite money on warmup.
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
        //-- Disables MVP on knife round.
        if (Match.Phase == MatchPhase_t.Knife)
        {
            var players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var player in players.Where(IsPlayerConnected))
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
            //-- When knife round ends, assign winner team and goes to knife vote.
            case MatchPhase_t.Knife:
                var team = EvaluateKnifeWinnerTeam();
                OverwriteRoundEndPanel(team);
                Match.KnifeWinnerTeam = GetTeamState(team);
                SetPhase(MatchPhase_t.PreKnifeVote);
                break;
        }
        return HookResult.Continue;
    }

    public HookResult OnCsWinPanelMatch(EventCsWinPanelMatch _, GameEventInfo __)
    {
        //-- When match ends, restarts to warmup.
        var mp_match_restart_delay = ConVar.Find("mp_match_restart_delay")?.GetPrimitiveValue<int>();
        var restartDelay = mp_match_restart_delay != null ? mp_match_restart_delay - 1 : 1;
        // @todo: handle next map here.
        AddTimer((float)restartDelay, StartWarmup, TimerFlags.STOP_ON_MAPCHANGE);
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
                //-- Handle players disconnecting mid-game (forfeit system).
                if (!Utilities.GetPlayers().Where(other =>
                    other.IsBot == false &&
                    other.SteamID != player.SteamID &&
                    other.Connected == PlayerConnectedState.PlayerConnected).Any())
                {
                    CreateTimer(Timer_t.MatchForfeit, 60.0f, StartForfeit, TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        }
        return HookResult.Continue;
    }
}
