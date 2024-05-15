/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Modules.Cvars;

namespace CompetitiveMatch;

public enum MatchPhase
{
    Warmup,
    Knife,
    Live
}

public partial class CompetitiveMatch
{
    public readonly FakeConVar<bool> match_bot_fill = new("match_bot_fill", "Whether to fill vacant slots with bots.", false);
    public readonly FakeConVar<int> match_max_players = new("match_max_players", "Max players in the match.", 10);
    public readonly FakeConVar<bool> match_whitelist = new("match_whitelist", "Whether use whitelist.", false);
    public readonly FakeConVar<string> match_hostname = new("match_hostname", "Hostname of the match.", "Competitive Match");

    public MatchPhase Phase = MatchPhase.Warmup;
    public bool IsInitialized = false;
    public Dictionary<ulong, bool> PlayerReadyManager = [];
}
