/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
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

public enum TimerType_t
{
    CommandsPrint,
    KnifeVotePrint,
    KnifeVoteTimeout,
    MatchForfeit
}

public class MatchPlayerState(MatchTeamState team, CCSPlayerController player)
{
    public bool IsReady = false;
    public KnifeVote_t KnifeVote = KnifeVote_t.None;
    public string Name = player.PlayerName;
    public ulong SteamID = player.SteamID;
    public MatchTeamState Team = team;
}

public class MatchTeamState(CsTeam team)
{
    public string Name = "";
    public MatchPlayerState? Leader;
    public Dictionary<ulong, MatchPlayerState> Players = [];
    public CsTeam StartingTeam = team;

    public MatchPlayerState AddPlayer(CCSPlayerController player)
    {
        var playerState = new MatchPlayerState(this, player);
        Players[playerState.SteamID] = playerState;
        if (Leader == null)
        {
            Leader = playerState;
            Name = $"team_{playerState.Name}";
        }
        return playerState;
    }

    public void RemovePlayer(CCSPlayerController player)
    {
        if (Leader?.SteamID == player.SteamID)
        {
            Leader = null;
            Name = "";
        }
        Players.Remove(player.SteamID);
    }
}

public class MatchState
{
    public List<MatchTeamState> Teams = [new(CsTeam.Terrorist), new(CsTeam.CounterTerrorist)];

    public MatchPhase_t Phase = MatchPhase_t.Warmup;
    public MatchTeamState? KnifeWinnerTeam;
    public KnifeVote_t KnifeVoteDecision = KnifeVote_t.None;

    public void Reset()
    {
        Teams = [new(CsTeam.Terrorist), new(CsTeam.CounterTerrorist)];
        Phase = MatchPhase_t.Warmup;
        KnifeVoteDecision = KnifeVote_t.None;
        KnifeWinnerTeam = null;
    }
}

public partial class CompetitiveMatch
{
    public readonly FakeConVar<bool> match_bot_fill = new("match_bot_fill", "Whether to fill vacant slots with bots.", true);
    public readonly FakeConVar<int> match_max_players = new("match_max_players", "Max players in the match.", 10);
    public readonly FakeConVar<bool> match_whitelist = new("match_whitelist", "Whether use whitelist.", false);
    public readonly FakeConVar<string> match_servername = new("match_servername", "Name of the server.", "MATCH^");

    public readonly float ChatInterval = 15.0f;
    public readonly List<CsTeam> MatchTeams = [CsTeam.Terrorist, CsTeam.CounterTerrorist];

    public bool IsInitialized = false;
    public CCSGameRulesProxy? GameRulesProxy;
    public MatchState Match = new();

    public Dictionary<TimerType_t, CounterStrikeSharp.API.Modules.Timers.Timer> MatchTimers = [];
}
