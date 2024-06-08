/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace CompetitiveMatch;

public enum MatchPhase_t
{
    Warmup,
    Knife,
    PreKnifeVote,
    KnifeVote,
    PreLive,
    Live
}

public enum KnifeVote_t
{
    None,
    Stay,
    Switch
}

public enum Timer_t
{
    CommandsPrinter,
    KnifeVotePrinter,
    KnifeVoteTimeout,
    MatchForfeit
}

public partial class CompetitiveMatch
{
    public class MatchTeamPlayerState(MatchTeamState team, CCSPlayerController player)
    {
        public bool IsReady = false;
        public KnifeVote_t KnifeVote = KnifeVote_t.None;
        public string Name = player.PlayerName;
        public ulong SteamID = player.SteamID;
        public MatchTeamState Team = team;
    }

    public class MatchTeamState(CsTeam team)
    {
        public string? Name;
        public MatchTeamPlayerState? Leader;
        public Dictionary<ulong, MatchTeamPlayerState> Players = [];
        public CsTeam StartingTeam = team;

        private void UpdateLeader()
        {
            if (Leader == null)
            {
                var playerState = Players.Values.FirstOrDefault();
                if (playerState != null)
                {
                    Leader = playerState;
                    Name = $"team_{playerState.Name}";
                    return;
                }
                Name = null;
            }
        }

        public MatchTeamPlayerState AddPlayer(CCSPlayerController player)
        {
            var playerState = new MatchTeamPlayerState(this, player);
            Players[playerState.SteamID] = playerState;
            UpdateLeader();
            return playerState;
        }

        public void RemovePlayer(CCSPlayerController player)
        {
            if (Leader?.SteamID == player.SteamID)
            {
                Leader = null;
            }
            Players.Remove(player.SteamID);
            UpdateLeader();
        }

        public CsTeam? ToCsTeam()
        {
            var gameRules = GetGameRules();
            var mp_maxrounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>();
            var mp_overtime_maxrounds = ConVar.Find("mp_overtime_maxrounds")?.GetPrimitiveValue<int>();
            if (gameRules != null && mp_maxrounds != null && mp_overtime_maxrounds != null)
            {
                var currentRound = gameRules.TotalRoundsPlayed + 1;
                var isHalfTime = currentRound <= mp_maxrounds
                    ? currentRound > mp_maxrounds / 2
                    : ((currentRound - mp_maxrounds - 1) % mp_overtime_maxrounds) + 1 > mp_overtime_maxrounds / 3;
                return isHalfTime
                    ? ToggleTeam(StartingTeam)
                    : StartingTeam;
            }
            return null;
        }
    }

    public class MatchState
    {
        public List<MatchTeamState> Teams = [new(CsTeam.Terrorist), new(CsTeam.CounterTerrorist)];
        public MatchPhase_t Phase = MatchPhase_t.Warmup;
        public MatchTeamState? KnifeWinnerTeam;
    }

    public readonly FakeConVar<bool> match_bot_fill = new("match_bot_fill", "Whether to fill vacant slots with bots.", true);
    public readonly FakeConVar<int> match_max_players = new("match_max_players", "Max players in the match.", 10);
    public readonly FakeConVar<bool> match_whitelist = new("match_whitelist", "Whether use whitelist.", false);
    public readonly FakeConVar<string> match_servername = new("match_servername", "Name of the server.", "MATCH^");

    public readonly float ChatInterval = 15.0f;
    public readonly List<CsTeam> MatchTeams = [CsTeam.Terrorist, CsTeam.CounterTerrorist];

    public bool IsInitialized = false;
    public MatchState Match = new();
    public Dictionary<Timer_t, CounterStrikeSharp.API.Modules.Timers.Timer> MatchTimers = [];
    
    static CCSGameRulesProxy? GameRulesProxy;
}
