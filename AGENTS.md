# Agents Guide: StardewFaster

This file helps future coding agents quickly understand and safely modify this repository.

## Project Snapshot

- **Type:** Stardew Valley mod for SMAPI
- **Language:** C#
- **Target framework:** `net6.0`
- **Primary package:** `Pathoschild.Stardew.ModBuildConfig` (`4.4.0`)
- **Mod metadata:** `manifest.json`
- **Entry DLL:** `StardewFaster.dll`
- **UniqueID:** `EpicDuque.StardewFaster`
- **Minimum SMAPI API:** `4.0.0`

## Repository Layout

- `ModEntry.cs` - Mod entrypoint; wires up enabled features.
- `ModConfig.cs` - Root config and nested per-feature config classes.
- `EatingFeature.cs` - Faster eating/drinking behavior.
- `HyperTreeFertilizerFeature.cs` - Custom fertilizer item + data patching.
- `NoAnimalCollisionFeature.cs` - Lets player pass through animals/pets.
- `MemoryOverlayFeature.cs` - Debug overlay with memory/frame metrics.
- `RobinUpgradeFeature.cs` - One-time building-upgrade purchase flow.
- `manifest.json` - SMAPI manifest metadata.
- `StardewFaster.csproj` - Build and deploy settings.
- `assets/` - Mod assets used by feature logic.

## Runtime Architecture

### 1) Entry and Registration

`ModEntry` extends `Mod` and, in `Entry(IModHelper helper)`, reads `ModConfig` and conditionally instantiates each feature.

Common pattern:

```csharp
new SomeFeature(this, config.SomeFeature).Register();
```

### 2) Feature Pattern

Each feature generally follows:

1. Constructor: accepts `Mod` and its typed feature config.
2. `Register()` method: subscribes to SMAPI events.
3. Private event handlers: implement behavior.

### 3) Config Pattern

`ModConfig` contains one nested config per feature, each with an `Enabled` flag and feature-specific settings.

## Feature Notes

### HyperTreeFertilizerFeature

- Patches game data through `AssetRequested` (`Data/Objects`, `Data/CraftingRecipes`).
- Introduces a custom fertilizer object and crafting recipe.
- Includes nightly growth logic for relevant tree types.

### RobinUpgradeFeature

- Most complex feature in project.
- Integrates with Robin/carpeneter shop flow and building upgrades.
- Uses multiplayer-safe synchronization patterns (message handling + persistent flags).

### MemoryOverlayFeature

- Diagnostic feature.
- Tracks memory and frame timing for display overlay.
- Toggle key is config-driven.

## Conventions and Gotchas

- Namespace is `StardewFaster` across source files.
  - Keep namespace consistency in edits unless intentionally refactoring.
- Guard event handlers with context checks where relevant (`Context.IsWorldReady`, host/player checks, etc.).
- Prefer throttled tick handling (`e.IsMultipleOf(...)`) for per-tick logic to avoid overhead.
- For persistent shared state, prefer SMAPI-safe storage/message patterns already used in `RobinUpgradeFeature`.

## Build and Validation

Typical local commands:

```powershell
dotnet restore
dotnet build -c Debug
```

Current project settings indicate:

- `EnableModDeploy=true` in `StardewFaster.csproj` (auto-deploy workflow expected).
- Build outputs in `bin/Debug/net6.0/` and `bin/Release/net6.0/`.

## Agent Editing Checklist

When adding or changing a feature:

1. Add/extend config in `ModConfig.cs`.
2. Register feature conditionally in `ModEntry.cs`.
3. Keep feature self-contained in its own `*Feature.cs` file.
4. Use SMAPI event hooks with early-return guards.
5. Validate build with `dotnet build`.
6. If gameplay/data affected, verify both single-player and multiplayer behavior.

## Recommended Next Documentation

- Add a user-facing `README.md` with install, config examples, and feature descriptions.
- Document exact asset IDs/keys used by `HyperTreeFertilizerFeature`.
- Add a quick test matrix for regression checks after gameplay-affecting changes.

