# Souls of Terra — Implementation Status

> This document describes the current prototype, not every intended feature. Last reviewed: 2026-08-31.

## Current playable loop

1. Kill eligible enemies and collect their soul orbs.
2. Risk the complete carried balance on death.
3. Right-click the resulting bloodstain to recover it before dying again.
4. Speak to Soulless and purchase the world's Terra Blade Fragment for 100 souls.
5. Hold the fragment and right-click an Iron or Lead Anvil in a clear, supported 4-by-3 area to form the Terraforge.
6. Defeat King Slime, then spend 2,500 souls at the Terraforge to create Slime Essence.
7. Imbue any Copper-through-Platinum metal broadsword with Slime Essence to create Slimebound Blade.
8. Defeat the Eye of Cthulhu, temper the fragment to Terraforge Temper 1, then spend 5,000 souls to create Eye Essence.
9. Imbue a Ruby Staff with Eye Essence to create Servant's Gaze and release delayed homing servant volleys.
10. In a newly generated world, discover the Buried Court beneath spawn after Skeletron.
11. Purchase a reusable Warden's Fragment for 10,000 souls and use it on the court altar.
12. Break all four seals, defeat The Sealed Congregation, and condense Congregation Essence at Terraforge Temper 3.

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

### Soulspells

- Spellbook keybind (`K`) and Stance keybind (`Left Alt`), rebound in Controls.
- Always band: Soul Skip on by default, double-tap easing surge, preserved vertical momentum, brief i-frames, no ram, chasing equipped-player echoes, converging fragments, reunion impact, and a full-height torn wake.
- Mutually exclusive Soul Flight alternative: horizontal double-tap becomes two seconds of collision-aware free flight as the collectible soul, with ten-tick in/out dissolves, a continuous soul wake, seamless momentum, full damage immunity and outgoing-damage suppression, followed by a shared three-second cooldown.
- Stance band: Soul Light checked by default, Stance off, teal Shine-class light at 1 soul every 5 seconds.
- Live loadout edits, server-authoritative drain, Stance drop at 0 souls or death, Always spells persist.
- Buff-tray icons for Soul Skip, Soul Flight, and Light; right-click toggles the matching book state.

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
- Row-based Soulless transactions and a five-column, three-row-visible Terraforge catalogue with scrolling, self-contained essence cards, hover details, and one contextual Condense action.
- One-time Terra Blade Fragment purchase for 100 souls, contextual active status, and free recall while no Terraforge is active.
- Nine world-wide Terraforge Temper levels tied to major vanilla milestones.
- Two-tab Soulless menu separating Terraforge services from Soul Crystal conversion.
- Three tradable Soul Crystal denominations with 25% conversion loss, tiered unlocks, one-click consumption, and soul-release effects.

### Terraforge and essence prototype

- Right-click formation from a complete vanilla Iron or Lead Anvil while holding the fragment.
- Server validation of range, held item, single-active-forge state, source anvil, clear 4-by-3 space, solid support, and all purchases.
- Four-by-three Terraforge using original art with the Terra Blade Fragment visibly embedded.
- Hidden forge style preserves which anvil material must be returned.
- Condense and Imbue tabs with King Slime and Eye of Cthulhu progression checks.
- Slime Essence condensation for 2,500 souls.
- Eye Essence condensation for 5,000 souls after the Eye is defeated and Terraforge Temper 1 is purchased.
- Successful condensation grants the essence authoritatively, sends seven curved soul wisps into the forge, manifests the result above it, and draws the visual back toward the player.
- Any pickaxe dismantles the Terraforge and returns the fragment plus the original Iron or Lead Anvil; explosions cannot destroy it.
- Formation uses a synchronized fragment flight, green-gold impact burst, layered sound, local camera punch, and restrained idle effects.
- Unified imbuement-only weapon registry with grouped input support, server validation, actual-input ritual rendering, and load-time enforcement against missing entries or conventional weapon recipes.
- Slimebound Blade binding accepts every Copper-through-Platinum metal broadsword plus one Slime Essence.
- The discarded universal soul-swing system, clash state, edge renderer, and ribbon shader have been removed completely.
- Slimebound Blade is restored from commit `f6392b4`: a normal broadsword swing fires bouncing gel balls in a miss-independent 1, 1, 3 cycle.
- Essencebound Breaker Blade throws up to five straight tethered blades; blades lodge in enemies or terrain, auto-return beyond 400 pixels, and all recall on right-click or weapon switch.
- Breaker recalls spin through terrain, hit each non-host enemy once at 70% damage, and extract from a living lodged host for a separate 125% hit.
- Servant's Gaze binding consumes one Ruby Staff and one Eye Essence.
- Servant's Gaze releases three custom-art harmless eye servants that awaken after one second, independently home through terrain, and rupture into small damaging gore bursts.

### Buried Court and Sealed Congregation

- New-world generation of a protected 168-by-84 vaulted Court beneath spawn through a bundled dependency-free structure asset, with a flat collision-free 144-by-60 combat interior, background reliefs, a flush reliquary, and an exterior stair-and-antechamber entrance.
- Unbreakable graybox Court brick, masonry wall, relief wall, and brass-accent wall types temporarily reuse vanilla art; Court walls suppress ordinary ambient spawns while eight Boreal floor lamps, six Boreal chandeliers, and glowing mid-wall seals provide three bands of cool-blue light.
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
- Expert and Master victories award a standard pre-Hardmode treasure bag containing healing potions and Borrowed Sentence.
- Arena retreat behavior with a three-second grace period and encounter-projectile cleanup.
- Congregation Essence unlock at Terraforge Temper 3 for 20,000 souls.
- Compeditus summon prototype: Imp Staff imbuement, placeholder item/buff art, one code-drawn controller core, up to four reduced Congregation seals, standard minion targeting, tile-aware restrained-homing lances, and a delayed localized implosion judgment.
- Unison melee prototype: Muramasa imbuement, committed two-fist clap, and a closed Hollow Benediction ring (~360px, no gaps) locked at the smash; one hit per enemy per ring, next clap waits until the hymn finishes.
- Crux ranged prototype: Handgun imbuement, cursor-locked crossed sentence (~240px X, dedicated write-and-knot shader), both arms hit once per enemy per volley, one volley at a time, consumes bullets.
- Stars of Ruin mage prototype: Magic Missile imbuement, mana-hungry verse of twelve sapphire-white stars with white-hot heads, narrow blue ribbons, procedural cobalt-violet cosmic mist, and stellar motes; they lock one visible cursor-selected NPC, share one wand-tip origin, form a complete two-sided bouquet on six mirrored pairs of cubic lanes, collide with terrain throughout their flight, and begin homing only after the opening curves finish.
- Borrowed Sentence Expert accessory: qualifying wounds defer 40% of final damage into a six-second seal, hostile damage repays its 12x requirement, failed judgment returns exact lethal-capable damage, and both outcomes begin a fixed fourteen-second cooldown.
- Recipe-first Imbue flow with a full always-visible catalogue, Temper and boss requirement copy, missing-inventory feedback, scalable scrolling, framed ingredient/result slots, and ready-only selection.
- Centered, mostly frameless Imbue ritual canvas with eased entry, authored weapon focus, depth-layered pickup souls orbiting the weapon, two-pixel-snapped soul currents, compact controls, and an unobscured world backdrop.

## Current Terraforge Temper table

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
- Terra Blade Fragment uses original 40-by-48 hiltless shard art.
- Terraforge uses original 62-by-32 art aligned within its 4-by-3 interaction footprint, with Temper-dependent light and ambient effects.
- Slime Essence has its first original 24-by-24 sprite with a royal crown core.
- Slimebound Blade has its first original 40-by-40 sprite and retains prototype combat values.
- Menu text is functional but not yet visually themed.
- The Buried Court uses layered vanilla masonry with an original custom-drawn Warden's Reliquary monument while its broader architecture is validated before original court tiles are authored.
- Congregation Essence temporarily reuses existing placeholder essence art.
- Soul Skip, Soul Flight, and Soul Light buff icons temporarily reuse vanilla Swiftness, Featherfall, and Shine icons.

## Known limitations and risks

- King Slime and Eye of Cthulhu have implemented essences and one equipment demonstration each.
- Terraforge Tempers currently unlock progression but do not yet gate additional implemented recipes beyond the initial set.
- Upgrade costs require balance testing against actual soul payouts.
- Terraforge formation, single-instance enforcement, clearance checks, recall, and dismantling need multiplayer playtesting.
- Free-for-all bloodstains and orbs permit another player to take them by design.
- Modded NPCs with unusual or misleading `value` data may need fallback logic or exceptions.
- The complete `.tmod` package cannot be replaced externally while tModLoader has it loaded; compile-only validation still succeeds.
- Existing worlds do not receive a Buried Court; the first implementation requires a newly generated world.
- The redesigned arena layout, structure asset packaging, entrance traversal, multi-tile reliquary framing, seal hitboxes, attack timings, and procedural collision visuals require in-game validation.
- The Sealed Congregation currently has four unique class weapons through Congregation Essence imbuement (Compeditus, Unison, Crux, and Stars of Ruin) plus Borrowed Sentence; it still lacks a broader equipment pool, trophy, relic, and dedicated music.

## Verification status

- `dotnet build SoulsOfTerra.csproj --no-restore -p:BuildMod=false -p:TargetFramework=net8.0`: passes with zero warnings and zero errors.
- Full `.tmod` packaging passes with zero warnings and zero errors.
- In-game testing is still required for the complete Soulless-to-Terraforge vertical slice.

## Recommended next test

1. Build and reload from inside tModLoader.
2. Confirm Soulless appears in both a new world and an existing test world.
3. Purchase the Terra Blade Fragment once; verify the entry changes state and further purchases are impossible.
4. Form the Terraforge from both Iron and Lead Anvils on clear ground; verify blocked, unsupported, and second-forge attempts report the correct failure.
5. Confirm any pickaxe dismantles it, explosions do not, and the fragment plus correct source anvil drop exactly once.
6. Defeat King Slime and condense several essences.
7. Confirm every metal broadsword variant appears as a valid Slimebound Blade input and that selecting a ready recipe links an owned variant, opens the ritual, and returns to the browser after binding.
8. Repeat purchase, formation, condensation, recall, and dismantling with a multiplayer client.
9. Save and reload; verify Terraforge position, Temper, fragment purchase state, Soulless state, player balance, and bloodstains.
10. Generate a new world and verify the Buried Court is centered beneath spawn in the Underground layer without damaging the surface spawn.
11. Confirm the collapsed passage, castle framing, floor, and temporary altar render and collide correctly.
12. Defeat Skeletron, buy multiple Warden's Fragments, and confirm none are consumed by summoning.
13. Test all phase-one formations, damage every seal into its broken state, and confirm the combined boss bar falls correctly.
14. Verify Choir of Judgment's warning, 24-degree sweep, single-hit rule, camera feedback, and terrain piercing at several resolutions.
15. Complete phase two, verify Congregation Essence unlocks at Terraforge Temper 3, then save and reload the defeat flag.
16. Repeat summoning, seal synchronization, retreat, projectile cleanup, and victory with a multiplayer client.
17. Imbue an Imp Staff into Compeditus; summon one through four seals and verify slot use, targeting priority, terrain traversal, line-of-sight attacks, lance collision, judgment cadence, recall at four seals, dismissal, and multiplayer synchronization.
18. Imbue Slimebound Blade and confirm ordinary broadsword swings fire bouncing gel balls in a miss-independent 1, 1, 3 volley cycle.
19. Imbue Muramasa into Unison; confirm the clap animation, a closed expanding ring with no safe gaps, one hit per enemy, knockback away from the smash, and that a second clap cannot start until the ring ends.
20. Imbue Essencebound Breaker Blade; throw five straight blades, lodge several in one enemy and terrain, recall all at once, and verify extraction, one return hit per enemy per blade, terrain piercing, distance recall, death recall, and weapon-switch recall.
21. Imbue a Handgun into Crux; confirm a cursor-locked X that writes inward and knots, both arms hit once, bullets are consumed, and a second volley cannot start until the first ends.
22. Imbue a Magic Missile into Stars of Ruin; confirm the staff waves, twelve stars gather at its tip, lock one visible NPC near the cursor, launch rapidly from that shared origin into six mirrored curve pairs filling both sides of the aim line, begin homing only after the bouquet forms, collide with terrain, and continue without retargeting if their target dies; mana is consumed once per verse, and a second verse cannot start until the conductor ends.
23. Die with low, medium, and high soul balances; verify capped bloodstain tiers, grounded rendering, hover and Smart Interact response, immediate recovery, collapse and burst feedback, second-death replacement, save/reload persistence, and multiplayer recovery synchronization.
24. Open the spellbook, confirm Soul Skip on and Soul Light checked, double-tap to surge without ram damage, press Stance with 0 souls and confirm it fails, collect souls, hold Stance for more than 5 seconds, and confirm 1 soul is spent and the teal light matches Shine range.
25. Uncheck Light while Stance is on, confirm drain stops, die with Stance on, and confirm Soul Skip remains available after respawn.
26. Select Soul Flight and confirm Soul Skip unchecks; double-tap left or right while running, steer with movement or Jump, collide with tiles, take and deal no damage for two seconds, preserve exit momentum, then verify neither dash can activate during the shared three-second cooldown.

## Near-term implementation backlog

- Add clear menu feedback for insufficient souls and locked progression.
- Reconcile all Terraforge Temper costs with measured milestone rewards.
- Continue original art and animation work for Soulless, boss essences, and equipment.
- Add the remaining King Slime weapon imbuements and conventional armor recipes.
- Decide which boss essence follows King Slime and define its equipment identity.
- Add automated or repeatable multiplayer regression checks where practical.
