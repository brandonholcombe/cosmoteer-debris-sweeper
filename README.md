# Debris Sweeper (Cosmoteer mod)

Leftover junk chunks from destroyed ships never despawn in vanilla Cosmoteer,
and after a few big battles a system fills with hundreds of physics objects
that tank the framerate. This repo contains two complementary mods:

1. **Passive decay** (data-only, `mod.rules`): junk slowly erodes away using
   the same engine mechanism the vanilla PvP modes already use
   (`JunkDamageFractionPerTick` / `JunkDamageChancePerTick` — see
   `Data/modes/pvp/pvp_arena/pvp_arena.rules` in the game files), applied to
   Career and Creative mode with gentler values so salvaging is still
   practical.
2. **Clear-debris hotkey** (C# via EnhancedModLoader, `csharp/`): press
   **F9** to instantly delete all junk in the current system, **Ctrl+F9** to
   also delete loose resource nuggets. On-demand, so salvage sites are never
   touched until you decide you're done with them.

## Important: how junk decay interacts with salvage areas

Career mode *builds its discoverable salvage sites out of junk*: ship
graveyards (75–100 wreck ships), storage pods, and abandoned ships are all
spawned with `Allegiance = Junk` (see
`Data/modes/career/sectors/sysgen_misc_discoverables.rules`) — the same
allegiance battle debris gets. The engine's junk decay cannot tell them
apart, so with the passive-decay mod enabled, **salvage POIs in the system
you currently occupy will erode on the same timer as battle debris** (career
only simulates your current system, so unvisited systems are untouched until
you arrive).

Practical options, pick one:

- **Hotkey only** (recommended): delete the two Career action blocks from
  `mod.rules` (keep Creative if you like) and use F9 when you're done
  looting an area. Salvage sites stay pristine forever.
- **Passive decay, gentle**: the Career default is tuned to ~11 minutes per
  part, which is usually enough time to strip the good bits from a wreck —
  but a big graveyard will visibly thin out while you work it. Slow it
  further by lowering `JunkDamageChancePerTick` (e.g. `0.0005` ≈ 22 min).
- **Both**: gentle passive decay as a background garbage collector, hotkey
  for instant cleanup after big fights.

## How it behaves

- The sim runs at 30 ticks/second. Each junk part has a small chance per tick
  of taking a hit for 5% of its max health (`0.05`).
- Expected result: an average junk part fully erodes in **~11 minutes** in
  Career (chance `0.001`) and **~5–6 minutes** in Creative (chance `0.002`),
  so a wrecked ship crumbles away over several minutes. Salvage what you want
  first — resources dropped by decaying parts still appear per normal drop
  rates.
- Junk that is already littering an existing save starts decaying as soon as
  the save is loaded with the mod enabled. **Savegame compatible** both ways
  (enabling or disabling mid-campaign is safe — it only adds two numeric
  fields to the mode definitions).

## Install

1. Copy this folder (containing `mod.rules`) to your Cosmoteer mods
   directory:
   - Windows: `%USERPROFILE%\Saved Games\Cosmoteer\<your Steam ID>\Mods\debris_sweeper\`
2. Launch Cosmoteer → **Mods** → enable **Debris Sweeper** → restart when
   prompted.
3. If the game version has moved past the ones listed in
   `CompatibleGameVersions` (currently 0.30.x), add the new version string to
   that list in `mod.rules`.

## Tuning

Edit the four `ToAdd` values in `mod.rules` (Fraction and Chance, once for
Career and once for Creative):

| Preset | Fraction | Chance | Junk lifetime (avg) |
|---|---|---|---|
| Extra gentle | `0.05` | `0.0005` | ~22 min |
| Career default | `0.05` | `0.001` | ~11 min |
| Creative default | `0.05` | `0.002` | ~5–6 min |
| Moderate | `0.1` | `0.002` | ~3 min |
| Vanilla-PvP aggressive | `0.1` | `0.01` | ~30 s |

Delete the Creative-mode action block if you only want decay in Career.

## Things to verify in-game (first run)

These two fields are only *used* by the PvP modes in vanilla, so confirm the
career-mode code honors them too:

1. Destroy an NPC ship, note a junk chunk, and check it visibly degrades and
   disappears within the expected window (speed up game time to test faster).
2. Confirm **asteroids don't crumble** — asteroids share the "junk" allegiance
   internally. The vanilla PvP modes run this decay with asteroid fields
   enabled and asteroids survive there, so this should be fine, but check a
   mining site after ~10 minutes to be sure.
3. Confirm salvage still works on fresh wrecks.

## Clear-debris hotkey (C# mod, `csharp/DebrisSweeperHotkey`)

An [EnhancedModLoader](https://github.com/C0dingschmuser/EnhancedModLoader)
(EML) mod. **F9** deletes every junk chunk in the current system;
**Ctrl+F9** also deletes loose resource nuggets. It shows a popup with the
sweep count (set `ShowSweepReport = false` in `Main.cs` to silence it once
you trust it).

### Build (on the Windows machine, needs the game installed)

1. Install the [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
   (Cosmoteer runs on .NET 7; EML requires matching it).
2. ```
   cd csharp\DebrisSweeperHotkey
   dotnet build -c Release -p:Platform=x64
   ```
   If Steam isn't in the default location, add
   `-p:CosmoteerBin="D:\SteamLibrary\steamapps\common\Cosmoteer\Bin"`.

### Install

1. Install EML itself: subscribe to it on the
   [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2937901869)
   and run its `Installer.bat` (copies its `AVRT.dll` into Cosmoteer's `Bin`
   folder).
2. Copy `bin\x64\Release\net7.0-windows\DebrisSweeperHotkey.dll` **and**
   `DebrisSweeperHotkey.runtimeconfig.json` into `Cosmoteer\Bin\EML_Mods\`
   (create the folder if needed).
3. Launch the game, load a save, press F9 near some debris.

### Caveats

- Written against the public EML examples
  ([EML_TestMod](https://github.com/C0dingschmuser/EML_TestMod),
  [ProjectileSpawner](https://github.com/C0dingschmuser/ProjectileSpawner));
  uncertain game internals (ship enumeration, junk flag, removal method) go
  through reflection with several candidate member names, so version drift
  degrades to "nothing happens" + an error popup rather than a crash. If F9
  reports 0 removed with debris on screen, the member names need updating —
  open the project in Visual Studio (IntelliSense works against the
  publicized game DLLs) or send me `eml_log.txt` and the game version.
- Deleting junk this way vaporizes it — no salvage, no resource drops. Loot
  first, then sweep.
- Multiplayer: sweeping only on the host will likely desync clients; use it
  in single-player.
- If a game update moves Cosmoteer to .NET 8+, update `TargetFramework` /
  `RuntimeFrameworkVersion` in the csproj to match and grab the matching EML
  build.

## Optional: reduce loose-resource spam

Loose resource nuggets are the *other* never-despawning lag source. There's no
lifetime field for nuggets either, but you can cut them off at the source by
lowering NPC drop rates. Add actions like this to `mod.rules` (this changes
the economy — you get less free loot from kills):

```
{
    Action = Overrides
    OverrideIn = "<resources/resources.rules>/VeryCommonMaterialDropRates"
    Overrides { PartDestroyed = 5% }
}
{
    Action = Overrides
    OverrideIn = "<resources/resources.rules>/CommonMaterialDropRates"
    Overrides { PartDestroyed = 5% }
}
{
    Action = Overrides
    OverrideIn = "<resources/resources.rules>/RareMaterialDropRates"
    Overrides { PartDestroyed = 5% }
}
{
    Action = Overrides
    OverrideIn = "<resources/resources.rules>/ElementDropRates"
    Overrides { PartDestroyed = 5% }
}
```

## References

- [Modding/Actions — Cosmoteer Wiki](https://cosmoteer.wiki.gg/wiki/Modding/Actions)
- [Modding/mod.rules — Cosmoteer Wiki](https://cosmoteer.wiki.gg/wiki/Modding/mod.rules)
- Vanilla data files mirror: [Rojamahorse/CosmoteerUpdates](https://github.com/Rojamahorse/CosmoteerUpdates)
  (source for the `JunkDamage*` fields and drop-rate tables referenced above)
