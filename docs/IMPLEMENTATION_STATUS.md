# Souls of Terra — Implementation Status

> This document describes the current prototype, not every intended feature. Last reviewed: 2026-08-25.

## Current playable loop

1. Kill eligible enemies and collect their soul orbs.
2. Risk the complete carried balance on death.
3. Right-click the resulting bloodstain to recover it before dying again.
4. Speak to Soulless and purchase a Broken Terra Blade for 100 souls.
5. Hold the blade and right-click an Iron or Lead Anvil in a clear 4-by-2 area to create a Terra Shrine.
6. Defeat King Slime, then spend 2,500 souls at the shrine to create Slime Essence.
7. Craft the prototype Slimebound Blade at a normal anvil.
8. Defeat the Eye of Cthulhu, strengthen the shrine to tier 1, then spend 5,000 souls to create Eye Essence.
9. Craft Servant's Gaze at a normal anvil and release delayed homing servant volleys.
10. In a newly generated world, discover the Buried Court beneath spawn after Skeletron.
11. Purchase a reusable Warden's Fragment for 10,000 souls and use it on the court altar.
12. Break all four seals, defeat The Sealed Congregation, and condense Congregation Essence at shrine tier 3.

## Implemented

### Soul balance and rewards

- Saved 64-bit abstract balance per player character.
- Deterministic global rewards derived from NPC value.
- Stat-based fallback for bosses without a monetary value.
- Server-configurable global reward multiplier.
- One reward for a complete segmented enemy.
- Exclusions for friendly, town, invulnerable, statue-spawned, and dummy NPCs.
- Server-authoritative balance gain and spending with multiplayer synchronization.
- UI counter and accumulated recent-gain notification.
- Soul counter anchored to the bottom-right corner with an original nine-sliced dark frame, round soul icon, and subtle collection pulse.

### Soul orb presentation

- Procedurally drawn orb; no custom pixel sprite required.
- Smooth rim, transparent center, rotating internal wisps, glow, and attraction trail.
- Continuous size and color scaling through white, green, blue, and purple.
- Exclusive orange/yellow boss presentation.
- Automatic attraction and collection by a nearby player.
- Free-for-all multiplayer collection.
- Safe main-thread disposal of generated graphics resources during mod unload.

### Death and bloodstains

- Full-balance drop on genuine player death.
- One surviving bloodstain per persistent character identity.
- Previous bloodstain loss on another death.
- Manual right-click recovery.
- Free-for-all recovery.
- World persistence across save and reload.
- Last-safe-position fallback for hazardous death locations.

### Soulless and progression

- Initial forced spawn near an active player.
- Normal town NPC behavior, vulnerability, housing, combat, death, and respawning.
- Context-sensitive introductory dialogue.
- Shared left-anchored UI styling with compact typography, item icons, locked and unaffordable states, hover styling, and action feedback.
- Row-based Soulless transactions and a five-column, three-row-visible Terra Shrine catalogue with scrolling, self-contained essence cards, hover details, and one contextual Condense action.
- Unlimited Broken Terra Blade purchases for 100 souls each.
- Nine world-wide shrine upgrade tiers tied to major vanilla milestones.
- Two-tab Soulless menu separating shrine services from Soul Crystal conversion.
- Three tradable Soul Crystal denominations with 25% conversion loss, tiered unlocks, one-click consumption, and soul-release effects.

### Terra Shrine and essence prototype

- Right-click transformation of a complete vanilla Iron or Lead Anvil while holding the core.
- Server validation of range, held item, source anvil, clear 4-by-2 space, solid support, and all purchases.
- Four-by-two Terra Shrine rendered solely with original static Soul Anvil art.
- Hidden shrine style preserves which anvil material must be returned.
- Shrine menu with King Slime and Eye of Cthulhu progression checks.
- Slime Essence condensation for 2,500 souls.
- Eye Essence condensation for 5,000 souls after the Eye is defeated and shrine tier 1 is purchased.
- Successful condensation immediately grants the essence, then sends seven curved soul wisps from the player into the shrine with a two-part vanilla sound cue and final arrival pulse.
- Shrine destruction returns the core and the original Iron or Lead Anvil.
- Slimebound Blade standard anvil recipe for Magic Storage compatibility.
- Slimebound Blade Royal Viscosity prototype: an exceptionally slow 1.6x sword with a miss-independent 1, 1, 3 firing cycle; its compact gel balls use closely matched damage and bounce areas, pierce enemies, and bounce up to three times.
- Servant's Gaze magic staff recipe with Eye Essence, Lenses, and either Demonite or Crimtane Bars.
- Servant's Gaze releases three custom-art harmless eye servants that awaken after one second, independently home through terrain, and rupture into small damaging gore bursts.

### Buried Court and Sealed Congregation

- New-world generation of a 168-by-84 ruined castle beneath spawn with a 144-by-60 combat interior and collapsed physical passage.
- Saved and multiplayer-synchronized arena bounds, dais coordinates, and boss-defeat state.
- Protected temporary Demon Altar dais with server-validated interaction range, progression, held key, and duplicate-boss checks.
- Unlimited reusable Warden's Fragment purchases from Soulless for 10,000 souls after Skeletron.
- Procedurally rendered transparent core, internal wisps/faces, chains, glow, pulses, and phase-two afterimages.
- Four simultaneously vulnerable orbiting seal NPCs with independent health, clean/broken textures, synchronized formations, and chain-breaking deaths.
- Coordinated phase-one attacks: Crossed Sentence, Processional Arc, and Hollow Benediction.
- Released phase-two attacks: Choir of Judgment, Final Confession, and Collapse of the Many. Choir of Judgment replaces the contact-charge sequence with a warned, movement-biased slow beam sweep that can hit each player once; its custom shader renders separate animated spectral-body and bloom ribbons with shaped endpoints.
- One aggregate boss bar representing the core plus every surviving seal.
- Arena retreat behavior with a three-second grace period and encounter-projectile cleanup.
- Congregation Essence unlock at Terra Shrine tier 3 for 20,000 souls.

## Current shrine upgrade table

| Tier unlocked | Milestone | Prototype cost |
|---:|---|---:|
| 1 | Eye of Cthulhu | 10,000 |
| 2 | Eater of Worlds or Brain of Cthulhu | 20,000 |
| 3 | Skeletron | 30,000 |
| 4 | Wall of Flesh | 50,000 |
| 5 | All three mechanical bosses | 120,000 |
| 6 | Plantera | 200,000 |
| 7 | Golem | 300,000 |
| 8 | Lunatic Cultist | 450,000 |
| 9 | Moon Lord | 750,000 |

These values have not been reconciled with measured boss rewards and are deliberately marked as prototypes.

## Placeholder content

- Soulless reuses and tints the Tax Collector sprite and head.
- Broken Terra Blade reuses the Broken Hero Sword sprite.
- Terra Shrine renderer scales its base art to 64 by 32; the intended source sprite is 32 by 16 for crisp 2x pixel clusters, and no blade is rendered.
- Slime Essence has its first original 24-by-24 sprite with a royal crown core.
- Slimebound Blade has its first original 40-by-40 sprite and retains prototype combat values.
- Menu text is functional but not yet visually themed.
- The Buried Court uses vanilla masonry and a protected Demon Altar until original castle and dais tiles are authored.
- Warden's Fragment and Congregation Essence temporarily reuse existing placeholder essence art.

## Known limitations and risks

- King Slime and Eye of Cthulhu have implemented essences and one equipment demonstration each.
- Shrine upgrades currently unlock tiers but do not yet gate additional implemented recipes.
- Upgrade costs require balance testing against actual soul payouts.
- The anvil transformation, clearance checks, and shrine behavior need multiplayer playtesting.
- The custom menu does not yet provide explicit success or failure messages.
- Free-for-all bloodstains and orbs permit another player to take them by design.
- Modded NPCs with unusual or misleading `value` data may need fallback logic or exceptions.
- The complete `.tmod` package cannot be replaced externally while tModLoader has it loaded; compile-only validation still succeeds.
- Existing worlds do not receive a Buried Court; the first implementation requires a newly generated world.
- The arena layout, multi-tile altar framing, seal hitboxes, attack timings, and procedural collision visuals require in-game validation.
- The Sealed Congregation currently has no unique equipment pool, treasure bag, trophy, relic, or dedicated music.

## Verification status

- `dotnet build SoulsOfTerra.csproj -t:Compile -v:minimal`: passes with zero warnings and zero errors.
- Full external packaging: blocked only by the running tModLoader process locking `SoulsOfTerra.tmod`.
- In-game testing is still required for the complete Soulless-to-shrine vertical slice.

## Recommended next test

1. Build and reload from inside tModLoader.
2. Confirm Soulless appears in both a new world and an existing test world.
3. Purchase several cores and verify exact soul deductions.
4. Transform both Iron and Lead Anvils on clear ground; verify blocked and unsupported transformations fail safely.
5. Break and replace a shrine; confirm both items drop exactly once.
6. Defeat King Slime and condense several essences.
7. Confirm the Slimebound Blade appears and crafts in both vanilla recipe search and Magic Storage.
8. Repeat purchase, transformation, condensation, and recovery with a multiplayer client.
9. Save and reload; verify shrine tier, Soulless state, player balance, and bloodstains.
10. Generate a new world and verify the Buried Court is centered beneath spawn in the Underground layer without damaging the surface spawn.
11. Confirm the collapsed passage, castle framing, floor, and temporary altar render and collide correctly.
12. Defeat Skeletron, buy multiple Warden's Fragments, and confirm none are consumed by summoning.
13. Test all phase-one formations, damage every seal into its broken state, and confirm the combined boss bar falls correctly.
14. Verify Choir of Judgment's warning, 24-degree sweep, single-hit rule, camera feedback, and terrain piercing at several resolutions.
15. Complete phase two, verify Congregation Essence unlocks at shrine tier 3, then save and reload the defeat flag.
16. Repeat summoning, seal synchronization, retreat, projectile cleanup, and victory with a multiplayer client.

## Near-term implementation backlog

- Add clear menu feedback for insufficient souls and locked progression.
- Reconcile all shrine upgrade costs with measured milestone rewards.
- Create original art and animation for Soulless, the Broken Terra Blade, Terra Shrine, Slime Essence, and Slimebound equipment.
- Add the remaining King Slime weapon and armor recipes.
- Decide which boss essence follows King Slime and define its equipment identity.
- Add automated or repeatable multiplayer regression checks where practical.
