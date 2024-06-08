# CS2 Competitive Match Plugin

> A minimalist [CounterStrikeSharp](https://docs.cssharp.dev) plugin for running Counter-Strike 2 matches

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
2. [Download the latest release](https://github.com/ianlucas/cs2-inventory-simulator-plugin/releases) of CS2 Competitive Match Plugin.
3. Extract the ZIP file contents into `addons/counterstrikesharp`.

### Configuration

#### `match_bot_fill` ConVar

* Whether to fill vacant slots with bots. (Not recommended for performance.)
* **Type:** `bool`
* **Default:** `false`

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
