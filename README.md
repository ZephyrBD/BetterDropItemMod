# BetterDropItem

Allows players to drop their own items out of their inventories to trade with friends / throw away items!

**BetterDropItem is a fork (branch) of [`KookehsDropItemMod_Fork`](https://github.com/tristanmcpherson/KookehsItemDropMod), which is itself a BepInEx + R2API port of [Kookeh](https://thunderstore.io/c/riskofrain2/p/Kookehs/DropItemMod/)'s original DropItemMod.** The original design and the bulk of the implementation belong to Kookeh, with the port and upstream maintenance by tristanmcpherson. This fork keeps the same drop flow and adds server-authoritative validation, anti-exploit protections and extra configuration.

Repository: `BetterDropItemMod` · Package / plugin name: `BetterDropItem`

## What this fork adds on top of upstream

- **Server-authoritative validation.** Every drop is re-checked on the server, so a client cannot bypass the host's settings by editing their own config. All tier and safety options are marked `(Server-Side)`.
- **Anti-exploit protections** (both switchable): dropped items cannot be recycled or duplicated, and Prayer Beads cannot be dropped at all.
- **Permanent-stack safety.** Temp items (e.g. from a Shrine of Order mid-transition) are never dropped.
- **Configurable tiers.** Lunar, Void and Bazaar drops can each be allowed or blocked, and `NoTier` items are always blocked.
- **Optional Command artifact support.** When enabled and the Command artifact is active, dropped items land as Command cubes so the dropper can reroll them.

## Installation

1. Install [BepInEx](https://thunderstore.io/c/riskofrain2/p/BepInEx/BepInExPack_RoR2/) and [R2API](https://thunderstore.io/c/riskofrain2/p/RiskofThunder/R2API/).
2. Drop `BetterDropItem.dll` into `BepInEx/plugins`.
3. [RiskOfOptions](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/) is optional and adds an in-game settings page.

If you are upgrading from the old fork, delete `KookehsDropItemMod_Fork.dll` from `BepInEx/plugins` first. The plugin GUID changed, so settings are not migrated and the old config file can be removed.

## Configuration

| Section | Key | Default | Meaning |
| --- | --- | --- | --- |
| General | Enable Notifications | `true` | Show a notification when something is dropped. |
| General | Allow in Bazaar (Server-Side) | `true` | Allow drops while in the Bazaar. |
| General | Prevent Recycle (Server-Side) | `true` | Dropped items cannot be recycled or duplicated. |
| General | Prevent Prayer Beads Abuse (Server-Side) | `true` | Prayer Beads are undroppable. |
| General | Command Artifact Support (Server-Side) | `false` | Turn drops into Command cubes while Command is active. |
| Tiers (Server-Side) | Allow Lunar | `false` | Allow Lunar items (Lunar equipment is always allowed). |
| Tiers (Server-Side) | Allow Void | `false` | Allow Void items. |

## Console commands

Runs on the host:

- `drop_item <item> [count]` — drop an item by name or `ItemIndex` name; `count` is clamped to what you own (max 100).
- `drop_equip` — drop your current equipment.

## Building from source

```
dotnet build BetterDropItem.sln -c Release
```

Output: `bin/Release/netstandard2.1/BetterDropItem.dll`.

The build is offline and expects the game, engine and mod-loader assemblies under `libs/`. **That folder is intentionally not in this repository** — it holds copyrighted Risk of Rain 2, Unity and BepInEx binaries that cannot be redistributed. To build, copy them in yourself:

- `libs/` — `RoR2.dll`, `Assembly-CSharp.dll`, `HGCSharpUtils.dll`, `HGUnityUtils.dll`, the `UnityEngine.*.dll` set, `com.unity.multiplayer-hlapi.Runtime.dll` and the other references listed in `BetterDropItem.csproj`, taken from `Risk of Rain 2_Data/Managed`.
- `libs/BepInEx/` — `BepInEx.dll`, `BepInEx.Preloader.dll`, `0Harmony.dll`, `0Harmony20.dll` from `BepInEx/core`.
- `libs/R2API/` — `R2API.Core.dll`, `R2API.Networking.dll`, `R2API.CommandHelper.dll`.
- `libs/SoftDependencies/` — `RiskOfOptions.dll` (compile-time only, never shipped).

There is no test project, CI, linter or formatter; verification is compile plus an in-game test with BepInEx + R2API.

## Credits and license

- Original mod: **Kookeh** — [DropItemMod](https://thunderstore.io/c/riskofrain2/p/Kookehs/DropItemMod/).
- BepInEx + R2API port and the `KookehsItemDropMod_Fork` lineage: **tristanmcpherson** — [KookehsItemDropMod](https://github.com/tristanmcpherson/KookehsItemDropMod).
- This fork's changes: **ZephyrBD**.

This repository is released under the [MIT License](LICENSE). That grant covers this fork's own code and modifications. The upstream projects are credited above; at the time of writing the upstream repository does not carry an explicit license file, so all prior rights in the original code remain with its authors.

## Changelog:

- 1.x - Readme update
- 2.0.0 - Complete remake
- 2.1.0 - Updated for RoR2 1.0, fix so items with no tier can't be dropped
- 2.2.0 - Updated for the Anniversary Update
- 2.3.x - Updated for DLC patch
- 2.3.2 - Fixed NullReferenceException spam
- 2.3.4 - Fixed for latest update
- 2.4.0 - Added some extra features from YEET.
- 2.4.1 - 2.4.3 - Prayer Beads protection, Recycler/Duplicator fixes, DLC2/DLC3 compatibility, configurable recycle prevention.
- 2.5.0 - Restored drop_item / drop_equip console commands, notifications enabled by default again (6 seconds), items with an unknown tier (modded items without an ItemTierDef) are droppable again, added Prevent Recycle / Prevent Prayer Beads Abuse toggles, optional Command artifact support.
- 3.0.0 - Renamed to BetterDropItem. New plugin GUID (`BetterDropItem`), new assembly/DLL name (`BetterDropItem.dll`), namespaces merged into a single `BetterDropItem` namespace. Existing config files are not migrated, and the old `KookehsDropItemMod_Fork.dll` has to be removed from `BepInEx/plugins`.
