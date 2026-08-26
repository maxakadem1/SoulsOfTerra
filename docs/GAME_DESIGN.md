# Souls of Terra — Game Design

> Internal design document. Contains story and progression spoilers.

## Vision

Souls of Terra is intended to grow into a broad content mod in the spirit of mods such as Thorium: new progression, bosses, equipment, characters, and systems that coexist with Terraria's normal structure.

The soul economy is the foundation connecting that future content. It should make ordinary exploration and combat meaningful without replacing Terraria's materials, crafting stations, equipment progression, or class identity.

## Design pillars

1. **Every real fight has value.** Genuine hostile creatures release souls without requiring a hand-authored reward for every vanilla or modded NPC.
2. **Carried wealth creates tension.** Souls are abstract currency that drop on every genuine player death and must be deliberately recovered.
3. **Souls expand choices, not base statistics.** There are no permanent health, damage, defense, or attribute levels purchased with souls.
4. **Progression remains recognizably Terraria.** Souls unlock and condense special materials, while final equipment uses ordinary recipes and appropriately tiered crafting stations.
5. **Compatibility is structural.** Global rules derive from existing NPC data, and ordinary recipes allow systems such as Magic Storage to discover and craft mod equipment normally.

## Soul acquisition

### Enemy rewards

One reward is produced for the complete enemy, not once per body segment. Friendly NPCs, town NPCs, target dummies, statue-spawned enemies, invulnerable entities, and entities without a legitimate reward are excluded.

Rewards are deterministic and calculated from the NPC's actual scaled data. This allows Expert Mode, Master Mode, and stronger modded variants to pay more naturally. The current implementation uses the NPC's monetary value as its principal proxy, with a stat-based fallback for bosses that provide no value.

The calculation must remain global. Per-creature configuration is unsuitable as the primary system, though future exceptions may be used for enemies with misleading data.

### World manifestation

Enemy souls exist briefly as free-for-all world orbs. They do not have permanent ownership. A nearby player attracts and absorbs an orb automatically.

The visual language communicates value continuously:

- Low value: white.
- Increasing value: green, blue, then purple.
- Boss reward: orange/yellow, regardless of numerical interpolation.
- Size, glow, and intensity scale continuously with value.
- A single pleasant trail moves toward the collecting player.
- Large internal wisps rotate within a mostly transparent center bounded by a brighter rim.

Multiple simultaneous attraction trails were tested and rejected because they made the effect noisy without communicating value clearly.

Nearby pickups accumulate into one `+souls` notification instead of immediately replacing a large reward with a smaller one.

### Soul Crystals

Soul Crystals are physical, stackable, tradable vessels for abstract souls. Soulless converts carried souls into three denominations while retaining a 25% thematic fee. Using a crystal requires one deliberate hold-up action and releases its contained value back into the player's abstract balance.

- Faint Soul Crystal: contains 1,000 souls and costs 1,250; available immediately.
- Vivid Soul Crystal: contains 5,000 souls and costs 6,250; requires Terra Shrine tier 1.
- Profound Soul Crystal: contains 25,000 souls and costs 31,250; requires Terra Shrine tier 4.

The conversion loss preserves meaningful death risk while still allowing costly banking and player-to-player trading. Soulless's retained fee is story flavor and is not recorded as a hidden numerical resource.

## Death and recovery

Every genuine player death drops the player's entire carried soul balance into a bloodstain. The balance immediately becomes zero.

Bloodstains are free-for-all in the current design. Recovery requires intentional right-click interaction; walking over a bloodstain does not recover it. This preserves the deliberate retrieval moment associated with Souls games.

Only one bloodstain survives per character identity. Dying again removes the previous bloodstain before creating the new one. Bloodstains persist with the world across save and reload. When the exact death location is unsafe, the stain uses the player's last known grounded, non-hazardous position.

Per-player ownership may be reconsidered later, but it is not required for the initial system.

## Spending souls

Souls are not a permanent-stat currency. Their principal uses are:

- Purchasing access items from Soulless.
- Strengthening the world's Terra Shrines after major progression milestones.
- Condensing souls into boss-specific crafting essences.
- Eventually funding other equipment, utilities, rituals, and content that respect Terraria's normal progression.

Soul costs should provide a reason to fight and farm without forcing one optimal enemy farm. Major shrine upgrades initially target roughly half of the relevant boss's reward; exact values remain balance knobs and must be evaluated in playtesting.

## Soulless

Soulless is a vulnerable town NPC who initially appears near the first active player, similar to beginning a world with the Guide. Afterward he follows ordinary town NPC housing and respawn rules.

His dialogue introduces souls, death recovery, shrines, and new progression in a restrained Dark Souls-inspired voice. He sells the Broken Terra Blade for 100 souls.

### Hidden story direction

Soulless is intended to become an antagonist. He quietly retains the souls paid for Terra Shrine upgrades and uses the player's progress for his own benefit. Early dialogue and mechanics should foreshadow this without immediately exposing it.

His profit is communicated through prices, dialogue, and later story developments rather than a hidden numerical counter.

## Terra Shrine

The Broken Terra Blade is an access item resembling a ruined Terra Blade rather than a direct copy of a coiled sword. Holding it and right-clicking an Iron or Lead Anvil transforms that anvil into a Terra Shrine when a clear, fully supported 4-by-2 area surrounds it. The blade acts as a ritual catalyst but is not visibly embedded in the completed station.

A Terra Shrine:

- Occupies a symmetrical 4-by-2 footprint centered around the original anvil.
- Uses one dark, blackened-anvil appearance regardless of the source anvil material.
- Renders only the Soul Anvil texture so the crafting station retains one cohesive silhouette.
- Provides the interface for condensing souls into unlocked essences.
- Drops both the Broken Terra Blade and the original Iron or Lead Anvil when destroyed.

Shrine strength is world-wide. Players return to Soulless after each major milestone and pay souls to strengthen every Terra Shrine in that world.

### Main shrine milestones

1. Eye of Cthulhu.
2. Eater of Worlds or Brain of Cthulhu.
3. Skeletron.
4. Wall of Flesh.
5. All three mechanical bosses.
6. Plantera.
7. Golem.
8. Lunatic Cultist.
9. Moon Lord.

Optional bosses do not add mandatory shrine tiers. Instead, they unlock their own themed essence or related content.

## Boss essences and crafting

Each supported boss should eventually unlock a distinct essence. After its boss is defeated, a Terra Shrine can irreversibly condense abstract souls into that essence. Defeating a boss unlocks unlimited condensation; the boss does not need to be killed once per essence.

Essences should be expensive enough that one is a meaningful purchase. The baseline recipe language is approximately one essence per weapon or armor piece, with ordinary Terraria materials supplying the rest of the recipe.

Final equipment is crafted at a normal station appropriate to its tier. This deliberately keeps equipment recipes visible to Magic Storage and other crafting integrations. A Terra Shrine is not a required adjacent station for final equipment crafting.

### Initial vertical slice

- King Slime unlocks Slime Essence.
- One Slime Essence costs 2,500 souls.
- Slimebound Blade demonstrates a normal anvil recipe using one essence, Gel, and Iron or Lead Bars.

Current costs and equipment statistics are prototypes, not final balance.

## The Buried Court and The Sealed Congregation

The Buried Court is a ruined underground castle arena generated beneath world spawn in new worlds. Players can discover it immediately through a collapsed physical passage, but its central altar remains dormant until Skeletron is defeated. The arena is a permanent narrative location intended for The Sealed Congregation, repeat encounters, and Soulless's eventual confrontation.

After Skeletron, Soulless sells unlimited Warden's Fragments for 10,000 souls. The fragment is a non-consumable key: holding it and right-clicking the court's altar summons The Sealed Congregation without an additional soul cost.

The encounter has two stages:

- Four simultaneously vulnerable seals orbit an invulnerable procedural soul core. The seals coordinate geometric lance, curved-bolt, and chain-sweep formations. Each uses a broken visual below 35% health and snaps its chain when destroyed.
- Once every seal breaks, the released congregation becomes vulnerable and highly mobile. It charges Choir of Judgment above the player, commits to a warned direction, and slowly sweeps the spectral beam toward the player's prior movement. Delayed confessions and a major implosion-to-radial-eruption complete the phase.

The core, chains, internal faces, glow, telegraphs, and trails are code-driven. One clean seal sprite and one broken variant provide the authored boss art. The combined boss bar includes core and seal health so phase-one damage is always represented accurately.

Defeating the boss sets a world-wide progression flag and unlocks Congregation Essence at Terra Shrine tier 3 for a prototype cost of 20,000 souls.

## Multiplayer model

- Enemy orbs and bloodstains are free-for-all.
- Soul balances belong to player characters.
- Shrine strength and boss unlocks belong to the world.
- Purchases, spending, condensation, recovery, and tile transformations must be validated by the server.
- A complete enemy produces one shared reward rather than one full reward per player.

This model intentionally permits competition or cooperation around dropped souls. Owner-only recovery can be explored later if multiplayer testing shows griefing outweighs the intended tension.

## Explicit non-goals

- No permanent attribute-leveling screen.
- No manually maintained reward entry for every creature.
- No separate final-equipment crafting ecosystem that bypasses normal Terraria stations.
- No mandatory progression tier for every optional vanilla boss.
- No automatic bloodstain recovery by contact.
- No multiple visual trails from a single soul orb.

## Balance questions for playtesting

- Does the global reward formula pay fairly across common enemies, segmented enemies, events, bosses, and modded NPCs?
- Are boss reward colors and continuous value differences readable during combat?
- Is bloodstain interaction reliable under combat pressure?
- Does 100 souls make the first shrine accessible at the right time?
- Does 2,500 souls per Slime Essence create useful early farming without excessive repetition?
- Should shrine upgrade prices actually equal 50% of milestone boss rewards after the reward formula stabilizes?
- Does free-for-all recovery create enjoyable multiplayer tension or unacceptable griefing?

