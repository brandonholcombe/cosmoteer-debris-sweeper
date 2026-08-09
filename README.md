# Debris Sweeper (Cosmoteer mod)

Leftover junk chunks from destroyed ships never despawn in vanilla Cosmoteer,
and after a few big battles a system fills with hundreds of physics objects
that tank the framerate. This mod makes junk slowly erode away using the same
engine mechanism the vanilla PvP modes already use
(`JunkDamageFractionPerTick` / `JunkDamageChancePerTick` — see
`Data/modes/pvp/pvp_arena/pvp_arena.rules` in the game files), applied to
Career and Creative mode with gentler values so salvaging is still practical.

## How it behaves

- The sim runs at 30 ticks/second. Each junk part has a small chance per tick
  (`0.002`) of taking a hit for 5% of its max health (`0.05`).
- Expected result: an average junk part fully erodes in **~5–6 minutes**, so a
  wrecked ship crumbles away over several minutes. Salvage what you want
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
| Gentle (default) | `0.05` | `0.002` | ~5–6 min |
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

## If career mode ignores the fields (fallback plan)

If junk provably doesn't decay in Career with this mod enabled, the
data-driven route is a dead end (the decay would be PvP-only in the game
code) and the options are:

- **[Resource Remover](https://steamcommunity.com/sharedfiles/filedetails/?id=3038190336)-style
  approach**: add "incinerator" parts that crew feed debris/resources into.
  Proven to work, but manual gameplay rather than automatic cleanup.
- **C# mod** via
  [EnhancedModLoader](https://github.com/C0dingschmuser/EnhancedModLoader):
  a small patch that periodically deletes ships with junk allegiance older
  than N minutes. Guaranteed to work, heavier to build/maintain.

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
