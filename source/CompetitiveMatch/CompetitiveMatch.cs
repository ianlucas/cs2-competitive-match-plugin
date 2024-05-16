/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Events;

namespace CompetitiveMatch;

[MinimumApiVersion(227)]
public partial class CompetitiveMatch : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Competitive Match Plugin";
    public override string ModuleName => "CompetitiveMatch";
    public override string ModuleVersion => "1.0.0";

    public override void Load(bool hotReload)
    {
        match_bot_fill.ValueChanged += OnChangeMatchBotFill;
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventItemPickup>(OnItemPickup);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
        RegisterEventHandler<EventRoundMvp>(OnRoundMvpPre, HookMode.Pre);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }
}
