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
    KnifeVote,
    LiveFirstRound,
    Live
}

public enum KnifeVote_t
{
    None,
    Stay,
    Switch
}

public class PlayerState(CCSPlayerController? player)
{
    public bool IsReady = false;
    public CsTeam StartingTeam = CsTeam.None;
    public KnifeVote_t KnifeVote = KnifeVote_t.None;
    public CCSPlayerController? Controller = player;
}

public class MatchMapState
{
    public CsTeam KnifeWinner = CsTeam.None;
    public Dictionary<ulong, PlayerState> Players = [];
    public KnifeVote_t KnifeVoteDecision = KnifeVote_t.None;
    public long KnifeStartedAt = 0;
    public long KnifeVoteStartedAt = 0;
    public long LiveStartedAt = 0;
    public MatchPhase_t Phase = MatchPhase_t.Warmup;
}


public partial class CompetitiveMatch
{
    public readonly FakeConVar<bool> match_bot_fill = new("match_bot_fill", "Whether to fill vacant slots with bots.", true);
    public readonly FakeConVar<int> match_max_players = new("match_max_players", "Max players in the match.", 10);
    public readonly FakeConVar<bool> match_whitelist = new("match_whitelist", "Whether use whitelist.", false);
    public readonly FakeConVar<string> match_hostname = new("match_hostname", "Hostname of the match.", "Competitive Match");

    public readonly PlayerState BotState = new(null);
    public MatchMapState MatchMap = new();
    public CounterStrikeSharp.API.Modules.Timers.Timer? MatchForfeitTimer;
    public CCSGameRulesProxy? GameRulesProxy;
    public bool IsInitialized = false;
}
