# Souls of Terra — Implementation Status

> This document describes the current prototype, not every intended feature. Last reviewed: 2026-08-27.

## Current playable loop

1. Kill eligible enemies and collect their soul orbs.
2. Risk the complete carried balance on death.
3. Right-click the resulting bloodstain to recover it before dying again.
4. Speak to Soulless and purchase a Broken Terra Blade for 100 souls.
5. Hold the blade and right-click an Iron or Lead Anvil in a clear 4-by-2 area to create a Terra Shrine.
6. Defeat King Slime, then spend 2,500 souls at the shrine to create Slime Essence.
7. Imbue any Copper-through-Platinum metal broadsword with Slime Essence to create Slimebound Blade.
8. Defeat the Eye of Cthulhu, strengthen the shrine to tier 1, then spend 5,000 souls to create Eye Essence.
9. Imbue a Ruby Staff with Eye Essence to create Servant's Gaze and release delayed homing servant volleys.
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
- Shader-driven void-violet ground pool with a procedural fallback.
- Four capped value tiers controlling cyan-edged wisp count, brightness, and pulse.
- Hover and Smart Interact response that brightens the rim and leans wisps toward the player.
- Immediate authoritative recovery followed by a short cosmetic collapse and ground burst.

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
- Unified imbuement-only weapon registry with grouped input support, server validation, actual-input ritual rendering, and load-time enforcement against missing entries or conventional weapon recipes.
- Slimebound Blade binding accepts every Copper-through-Platinum metal broadsword plus one Slime Essence.
- Shared soul-swing system: opt-in held projectile, cursor-locked aim, path presets, in-house rim/core ribbon shader, and one hit per NPC per swing.
- Slimebound Blade Royal Viscosity prototype: an exceptionally slow 1.6x sword using alternating lateral soul-swings with a miss-independent 1, 1, 3 firing cycle; gel balls launch from the blade tip at the cut, use closely matched damage and bounce areas, pierce enemies, and bounce up to three times.
- Essencebound Breaker Blade left-click: cursor-locked falling soul-swing (~52 ticks, linen ribbon, ~124px reach) that attaches bandage tethers on hit; right-click execution remains exclusive and unchanged.
- Servant's Gaze binding consumes one Ruby Staff and one Eye Essence.
- Servant's Gaze releases three custom-art harmless eye servants that awaken after one second, independently home through terrain, and rupture into small damaging gore bursts.

### Buried Court and Sealed Congregation

- New-world generation of a protected 168-by-84 vaulted castle hall beneath spawn, with a flat 144-by-60 combat interior, ruined side stair, galleries, wall bays, controlled collapse, and stepped central dais.
- Six-second top-center location reveal using authored pixel lettering, smooth fades, shadow, restrained teal bloom, and a ten-minute retrigger cooldown; vanilla Dungeon music plays while inside the hall.
- Saved and multiplayer-synchronized arena bounds, dais coordinates, and boss-defeat state.
- Protected custom Warden's Reliquary monument with server-validated interaction range, progression, held key, and duplicate-boss checks.
- Ground-aligned 200%-scale reliquary art with dormant layered glow, orbiting motes, local lighting, and proximity-gated screen refraction.
- Synchronized summon ritual with ascending soul spirals, four spectral seals, an implosion/release bloom, camera response, sound sequence, and final refractive shockwave before server-authoritative boss creation.
- Unlimited reusable Warden's Fragment purchases from Soulless for 10,000 souls after Skeletron.
- Warden's Fragment lore is conveyed through its borrowed-authority tooltip, Soulless's court dialogue, the reliquary's acceptance message, and the Congregation's final warning; its original ward-sigil sprite is integrated with enlarged inventory rendering.
- Procedurally rendered transparent core, internal wisps/faces, chains, glow, pulses, and phase-two afterimages.
- Four simultaneously vulnerable orbiting seal NPCs with independent health, clean/broken textures, synchronized formations, and chain-breaking deaths.
- Coordinated phase-one attacks: Crossed Sentence, Processional Arc, and Hollow Benediction.
- Released phase-two attacks: Choir of Judgment, Final Confession, and Collapse of the Many. Choir of Judgment replaces the contact-charge sequence with a warned, movement-biased slow beam sweep that can hit each player once; its custom shader renders separate animated spectral-body and bloom ribbons with shaped endpoints.
- One aggregate boss bar representing the core plus every surviving seal.
- Expert and Master victories award a standard pre-Hardmode treasure bag; its initial contents mirror the Classic healing-potion reward until unique equipment is designed.
- Arena retreat behavior with a three-second grace period and encounter-projectile cleanup.
- Congregation Essence unlock at Terra Shrine tier 3 for 20,000 souls.
- Compeditus summon prototype: Imp Staff imbuement, placeholder item/buff art, one code-drawn controller core, up to four reduced Congregation seals, standard minion targeting, tile-aware restrained-homing lances, and a delayed localized implosion judgment.
- Unison melee prototype: Muramasa imbuement, committed two-fist clap, and a closed Hollow Benediction ring (~360px, no gaps) locked at the smash; one hit per enemy per ring, next clap waits until the hymn finishes.
- Crux ranged prototype: Handgun imbuement, cursor-locked crossed sentence (~240px X, dedicated write-and-knot shader), both arms hit once per enemy per volley, one volley at a time, consumes bullets.
- Recipe-first Imbuement flow with boss-based discovery, shrine-tier requirements, missing-inventory feedback, scalable scrolling, framed ingredient/result slots, ready-only selection, and a focused grid-free ritual screen.

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
- The Buried Court uses layered vanilla masonry with an original custom-drawn Warden's Reliquary monument while its broader architecture is validated before original court tiles are authored.
- Congregation Essence temporarily reuses existing placeholder essence art.

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
- The Sealed Congregation currently has three unique equipment rewards through Congregation Essence imbuement (Compeditus, Unison, and Crux); it still lacks a broader equipment pool, trophy, relic, and dedicated music, while its treasure bag contains only the provisional healing-potion reward.

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
7. Confirm every metal broadsword variant appears as a valid Slimebound Blade input and that selecting a ready recipe links an owned variant, opens the ritual, and returns to the browser after binding.
8. Repeat purchase, transformation, condensation, and recovery with a multiplayer client.
9. Save and reload; verify shrine tier, Soulless state, player balance, and bloodstains.
10. Generate a new world and verify the Buried Court is centered beneath spawn in the Underground layer without damaging the surface spawn.
11. Confirm the collapsed passage, castle framing, floor, and temporary altar render and collide correctly.
12. Defeat Skeletron, buy multiple Warden's Fragments, and confirm none are consumed by summoning.
13. Test all phase-one formations, damage every seal into its broken state, and confirm the combined boss bar falls correctly.
14. Verify Choir of Judgment's warning, 24-degree sweep, single-hit rule, camera feedback, and terrain piercing at several resolutions.
15. Complete phase two, verify Congregation Essence unlocks at shrine tier 3, then save and reload the defeat flag.
16. Repeat summoning, seal synchronization, retreat, projectile cleanup, and victory with a multiplayer client.
17. Imbue an Imp Staff into Compeditus; summon one through four seals and verify slot use, targeting priority, terrain traversal, line-of-sight attacks, lance collision, judgment cadence, recall at four seals, dismissal, and multiplayer synchronization.
18. Imbue Slimebound Blade and confirm cursor-locked alternating lateral swings, a lingering soul-ribbon, one hit per enemy per swing, gel balls firing from the tip at the cut, and the every-third-swing royal volley.
19. Imbue Muramasa into Unison; confirm the clap animation, a closed expanding ring with no safe gaps, one hit per enemy, knockback away from the smash, and that a second clap cannot start until the ring ends.
20. Imbue Essencebound Breaker Blade; confirm a hung falling smash through the cursor, a linen ribbon, bandage tethers on hit, and that left-click and right-click execution never overlap.
21. Imbue a Handgun into Crux; confirm a cursor-locked X that writes inward and knots, both arms hit once, bullets are consumed, and a second volley cannot start until the first ends.
22. Die with low, medium, and high soul balances; verify capped bloodstain tiers, grounded rendering, hover and Smart Interact response, immediate recovery, collapse and burst feedback, second-death replacement, save/reload persistence, and multiplayer recovery synchronization.

## Near-term implementation backlog

- Add clear menu feedback for insufficient souls and locked progression.
- Reconcile all shrine upgrade costs with measured milestone rewards.
- Create original art and animation for Soulless, the Broken Terra Blade, Terra Shrine, Slime Essence, and Slimebound equipment.
- Add the remaining King Slime weapon imbuements and conventional armor recipes.
- Decide which boss essence follows King Slime and define its equipment identity.
- Add automated or repeatable multiplayer regression checks where practical.
