# AGENTS.md

## Project identity

- The mod is **BetterDropItem**. It is a fork (branch) of **`KookehsDropItemMod_Fork`**, which is itself a BepInEx + R2API port of Kookeh's original DropItemMod (upstream: `tristanmcpherson/KookehsItemDropMod`). Keep that lineage stated in user-facing docs; the gameplay design is not original to this fork.
- BepInEx plugin GUID: `BetterDropItem` (was `KookehsDropItemMod_Fork`). The GUID keys the BepInEx config file and the RiskOfOptions settings, so changing it again starts every user on fresh settings.
- Assembly/DLL name follows the csproj file name: `BetterDropItem.dll`.
- The directory is **not currently a git repo** (no `.git`). There is no branch, history, or rollback other than manual backups.

## Repository layout

- The project lives at the repository root: `BetterDropItem.csproj`, `BetterDropItem.sln`, and the `.cs` sources are all flat in the root directory.
- Game, BepInEx, R2API, and Unity assemblies are vendored in `libs/` and referenced with explicit `HintPath`s. The build is offline: do not delete `libs` or replace these references with NuGet packages. `libs/SoftDependencies/RiskOfOptions.dll` is compile-only, never shipped.
- `bin/` and `obj/` at the root are build artifacts.

## Build and verification

- Build: `dotnet build BetterDropItem.sln` (SDK-style, `netstandard2.1`, needs a modern .NET SDK / VS 2022). There is no test project, CI, linter, or formatter config; verification is compile plus a manual in-game test with BepInEx + R2API.
- There is no post-build deploy step. Copy `bin/<Configuration>/netstandard2.1/BetterDropItem.dll` into the game's `BepInEx/plugins` yourself.
- Release version lives in the `ModVersion` const in `BetterDropItemPlugin.cs`, not in MSBuild metadata. `manifest.json` carries the same number in `version_number`. Bump both for user-visible changes, and add a line to the hand-maintained README changelog.
- `manifest.json` carries the Thunderstore metadata. Its `version_number` must match `ModVersion`, and its `dependencies` pin the exact BepInEx/R2API package versions users will be forced to install by r2modman. `RiskofThunder-R2API_Core-5.3.0` is declared while `libs/R2API/R2API.Core.dll` is 5.2.0 — rebuild against 5.3.0 before uploading if that matters.

## Architecture

- BepInEx 5 plugin (`BaseUnityPlugin.Awake`). Everything lives in a single `BetterDropItem` namespace; the plugin class is `BetterDropItemPlugin` (deliberately not named `BetterDropItem`, to avoid a type/namespace name collision).
- Drop flow: clicking an item/equipment icon (`DropItemHandler.OnPointerClick`) sends a `DropItemMessage` via R2API Networking to the server; `OnReceived` (server-only) revalidates, removes one item/equipment from the inventory, and spawns a pickup droplet. The console commands (`ConsoleCommands.cs`) call the same `DropItemHandler.DropItem(CharacterBody, Inventory, PickupIndex)` overload, so both entry points share one server path.
- Droppability is enforced by `DropItemHandler.VerifyIsDroppable` on both sides. Config options are intentionally `(Server-Side)` so clients cannot bypass them; keep validation server-authoritative.
- Tier rules: `ItemTier.NoTier` is never droppable; Lunar and Void need their config flags; Lunar equipment is always allowed; items whose tier has no `ItemTierDef` (modded) are droppable; Prayer Beads (`DLC2Content.Items.ExtraStatsOnLevelUp`) is blacklisted while `Prevent Prayer Beads Abuse` is on.
- The Prayer Beads and Bazaar checks are deliberately outside the `if (tier != null)` block so unknown-tier items cannot skip them.
- `DropItemHandler.DropItem` returns `bool`; callers must only fire a notification when it returns `true`.
- UI integration uses MonoMod IL hooks: `ItemInventoryDisplay.AllocateIcons` and `ScoreboardStrip.SetMaster` attach click handlers, and `PickupDropletController.CreatePickup` copies `MarkNonRecyclableComponent` onto the spawned pickup (setting `Duplicated`/`Recycled` so Recycler/Duplicator ignore it). These hooks match IL patterns and private game members and are the most update-fragile code; re-verify after replacing `libs`.
- `Publicized.cs` requests skip-verification so private game members are callable; keep it.
- RiskOfOptions is a soft dependency. Only reference its types inside `RiskOfOptionsCompat`, which is guarded by `[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]` and a `Chainloader.PluginInfos` check, so the mod still loads without it.
- `DropItemHandler` is deliberately both a `MonoBehaviour` (one instance per icon) and the static drop utility.

## Conventions and gotchas

- Numbered `LogDebug` calls in `DropItemHandler` ("1", "2.1", ...) are tracing leftovers, not meaningful states.
- Keep `DropItemMessage.Serialize`/`Deserialize` symmetric, and register message types in `Awake` with `NetworkingAPI.RegisterMessageType`.
- Config sections/keys are literal (`"General"`, `"Tiers (Server-Side)"`); renaming them silently resets users' saved settings.
- `CommandHelper` is marked obsolete in the vendored R2API but still works and is the only command registration path. The supported replacement is the assembly-level `[assembly: HG.Reflection.SearchableAttribute.OptIn]` attribute (type lives in `HGCSharpUtils.dll`, already referenced). Only switch after verifying the commands still register in-game.
- `drop_item` clamps its count to the permanent stacks the sender owns, plus a hard cap of 100, because a client can send any integer.
