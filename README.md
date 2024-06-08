# CS2 Competitive Match Plugin

> A minimalist [CounterStrikeSharp](https://docs.cssharp.dev) plugin for running Counter-Strike 2 matches

The goal is to create the minimal API for supporting both match creation on-the-fly (for players inside the server) and through a match file. This plugin does not create additional folders in your game server installation. In-game System-wise the plugin tries to emulate FACEIT matches.

## Current Features

- Ready system
- Knife round

## Future Roadmap

- Surrender command (`!gg`)
- Pause command (`!pause`)
- Match system
- Match stats

## Installation

1. Install the latest release of [Metamod and CounterStrikeSharp](https://docs.cssharp.dev/docs/guides/getting-started.html).
2. [Download the latest release](https://github.com/ianlucas/cs2-competitive-match-plugin/releases) of CS2 Competitive Match Plugin.
3. Extract the ZIP file contents into `addons/counterstrikesharp`.

### Configuration

#### `match_bot_fill` ConVar

* Whether to fill vacant slots with bots. (Not recommended for performance.)
* **Type:** `bool`
* **Default:** `true`

#### `match_max_players` ConVar

* Max players in the match.
* **Type:** `int`
* **Default:** `10`

#### `match_whitelist` ConVar

* Whether use whitelist.
* **Type:** `bool`
* **Default:** `false`

#### `match_servername` ConVar

* Name of the server.
* **Type:** `string`
* **Default:** `MATCH^`
