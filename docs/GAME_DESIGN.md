# Souls of Terra — Game Design

> Internal design document. Contains story and progression spoilers.

## Vision

Souls of Terra is intended to grow into a broad content mod in the spirit of mods such as Thorium: new progression, bosses, equipment, characters, and systems that coexist with Terraria's normal structure.

The soul economy is the foundation connecting that future content. It should make ordinary exploration and combat meaningful while preserving Terraria's broader equipment progression and class identity.

## Design pillars

1. **Every real fight has value.** Genuine hostile creatures release souls without requiring a hand-authored reward for every vanilla or modded NPC.
2. **Carried wealth creates tension.** Souls are abstract currency that drop on every genuine player death and must be deliberately recovered.
3. **Souls expand choices, not base statistics.** There are no permanent health, damage, defense, or attribute levels purchased with souls.
4. **Progression remains recognizably Terraria.** Souls unlock and condense special materials; weapons transform from familiar vanilla bases, while armor, accessories, and supporting equipment retain ordinary materials and stations.
5. **One weapon-acquisition language.** Every Souls of Terra weapon is created by essence imbuement at the Terraforge rather than splitting weapons between imbuement and conventional crafting.

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
- Vivid Soul Crystal: contains 5,000 souls and costs 6,250; requires Terraforge Temper 1.
- Profound Soul Crystal: contains 25,000 souls and costs 31,250; requires Terraforge Temper 4.

The conversion loss preserves meaningful death risk while still allowing costly banking and player-to-player trading. Soulless's retained fee is story flavor and is not recorded as a hidden numerical resource.

## Death and recovery

Every genuine player death drops the player's entire carried soul balance into a bloodstain. The balance immediately becomes zero.

Bloodstains are free-for-all in the current design. Recovery requires intentional right-click interaction; walking over a bloodstain does not recover it. This preserves the deliberate retrieval moment associated with Souls games.

Only one bloodstain survives per character identity. Dying again removes the previous bloodstain before creating the new one. Bloodstains persist with the world across save and reload. When the exact death location is unsafe, the stain uses the player's last known grounded, non-hazardous position.

Per-player ownership may be reconsidered later, but it is not required for the initial system.

## Spending souls

Souls are not a permanent-stat currency. Their principal uses are:

- Purchasing access items from Soulless.
- Tempering the Terra Blade Fragment after major progression milestones.
- Condensing souls into boss-specific crafting essences.
- Eventually funding other equipment, utilities, rituals, and content that respect Terraria's normal progression.

Soul costs should provide a reason to fight and farm without forcing one optimal enemy farm. Major Temper increases initially target roughly half of the relevant boss's reward; exact values remain balance knobs and must be evaluated in playtesting.

## Soulless

Soulless is a vulnerable town NPC who initially appears near the first active player, similar to beginning a world with the Guide. Afterward he follows ordinary town NPC housing and respawn rules.

His dialogue introduces souls, death recovery, the Terraforge, and new progression in a restrained Dark Souls-inspired voice. He sells one Terra Blade Fragment per world for 100 souls and recalls it for free when no Terraforge is active.

### Hidden story direction

Soulless is intended to become an antagonist. He quietly retains the souls paid to temper the fragment and uses the player's progress for his own benefit. Early dialogue and mechanics should foreshadow this without immediately exposing it.

His profit is communicated through prices, dialogue, and later story developments rather than a hidden numerical counter.

## Terraforge

The Terra Blade Fragment is a genuine, hiltless shard of an ancient Terra Blade. Soulless sells it once for 100 souls without explaining how he obtained it. Holding the fragment and right-clicking an Iron or Lead Anvil forms the Terraforge when a clear, fully supported 4-by-3 area surrounds it.

The Terraforge:

- Is limited to one active instance per world.
- Occupies a symmetrical 4-by-3 footprint centered around the original anvil.
- Uses one transformed appearance regardless of the source anvil material.
- Keeps the fragment visibly embedded as its green-gold core.
- Condenses teal soul energy into boss essences and imbues vanilla weapons with those essences.
- No longer functions as a normal Iron or Lead Anvil.
- Can be dismantled with any pickaxe but cannot be destroyed by explosions.
- Drops the Terra Blade Fragment and the original Iron or Lead Anvil when dismantled.

Terraforge Temper is world-wide. Players return to Soulless after each major milestone and pay souls to temper the fragment. Its ambient light and effects intensify across four broad visual states.

### Main Temper milestones

1. Eye of Cthulhu.
2. Eater of Worlds or Brain of Cthulhu.
3. Skeletron.
4. Wall of Flesh.
5. All three mechanical bosses.
6. Plantera.
7. Golem.
8. Lunatic Cultist.
9. Moon Lord.

Optional bosses do not add mandatory Temper levels. Instead, they unlock their own themed essence or related content.

## Boss essences and crafting

Each supported boss should eventually unlock a distinct essence. After its boss is defeated, the Terraforge can irreversibly condense abstract souls into that essence. Defeating a boss unlocks unlimited condensation; the boss does not need to be killed once per essence.

Essences should be expensive enough that one is a meaningful purchase. Every mod weapon consumes exactly one valid vanilla base weapon and one matching essence through the Terraforge's Imbue page. No bars, fragments, lenses, gel, or adjacent crafting station are added to weapon bindings. Armor, accessories, and other non-weapon equipment continue using ordinary Terraria materials and tier-appropriate stations so those categories remain compatible with normal crafting integrations.

The imbuement registry is the authoritative weapon catalogue. It supports precise inputs such as Ruby Staff and grouped inputs such as any Copper-through-Platinum metal broadsword. Every mod weapon must inherit the imbuement-only base type, appear as a registry output, and have no conventional crafting recipe; load-time validation enforces these invariants for future content.

### Initial vertical slice

- King Slime unlocks Slime Essence.
- One Slime Essence costs 2,500 souls.
- Any Copper-through-Platinum metal broadsword plus one Slime Essence binds into Slimebound Blade.

Current costs and equipment statistics are prototypes, not final balance.

## The Buried Court and The Sealed Congregation

The Buried Court is a grand, vaulted underground castle hall generated beneath world spawn in new worlds. A collapsed passage leads down a ruined side staircase into an upper gallery, revealing a broad flat combat floor, pointed wall bays, fractured vaults, and a throne-like central dais. Damage is concentrated around the outer galleries and floor edges so the architecture feels ancient without compromising boss readability. Entering the hall fades in its authored title near the top of the screen while Dungeon music establishes the location; the title lasts six seconds and cannot retrigger for ten minutes. Players can discover the Court immediately, but its central altar remains dormant until Skeletron is defeated. The arena is a permanent narrative location intended for The Sealed Congregation, repeat encounters, and Soulless's eventual confrontation.

After Skeletron, Soulless sells unlimited Warden's Fragments for 10,000 souls. The fragment is a broken ward-sigil carrying forged, borrowed authority rather than a conventional key. Soulless claims the reliquary remembers the warden's office rather than the hand presenting its mark, quietly foreshadowing his knowledge of the prison and his interest in the souls within it. Holding the fragment and right-clicking the court's Warden's Reliquary begins a short summoning ritual without an additional soul cost. The dormant socket bends nearby light and emits a restrained teal pulse. Once activated, rising souls assemble four spectral seals above the court, collapse into the future core, and release a refractive shockwave as The Sealed Congregation manifests.

The reliquary's acceptance message implies that the fragment's authority is false. The Congregation's final voices describe the unseen hand behind the mark as hollow, creating an early clue about Soulless without explicitly revealing his future role.

The encounter has two stages:

- Four simultaneously vulnerable seals orbit an invulnerable procedural soul core. The seals coordinate geometric lance, curved-bolt, and chain-sweep formations. Each uses a broken visual below 35% health and snaps its chain when destroyed.
- Once every seal breaks, the released congregation becomes vulnerable and highly mobile. It charges Choir of Judgment above the player, commits to a warned direction, and slowly sweeps the spectral beam toward the player's prior movement. Delayed confessions and a major implosion-to-radial-eruption complete the phase.

The core, chains, internal faces, glow, telegraphs, and trails are code-driven. One clean seal sprite and one broken variant provide the authored boss art. The combined boss bar includes core and seal health so phase-one damage is always represented accurately.

Defeating the boss sets a world-wide progression flag and unlocks Congregation Essence at Terraforge Temper 3 for a prototype cost of 20,000 souls.

Its Expert treasure bag contains **Borrowed Sentence**, a fractured court seal that postpones 40% of any post-defense hit dealing at least 10% of the wearer's maximum life. The wearer has six seconds to deal twelve times the borrowed amount to hostile enemies. Success earns absolution and erases the damage; failure returns the exact stored damage and can kill. Further hits neither enter nor alter the active sentence, and a fixed fourteen-second cooldown begins after either outcome.

The Congregation's first equipment reward is **Compeditus**, created by binding Congregation Essence into an Imp Staff. It summons one shared spectral core surrounded by up to four seals, each consuming one minion slot. The formation passes through terrain but only attacks with clear line of sight. Seals deal no contact damage; instead, they fire restrained-homing lances in a staggered verse. Completing a verse expands and arrests the formation before its threads collapse into a localized judgment burst. The initial balance target is 22 summon damage, low knockback, a four-seal maximum, and one lance per seal per roughly 60-tick verse. **Stars of Ruin** binds Congregation Essence into a Magic Missile. Twelve sapphire shooting stars gather at its tip, launch in a rapid interleaved cascade, follow separate nested cubic lanes into a broad teardrop bouquet, and only then home toward one cursor-selected enemy.

The Terraforge's Imbue page opens directly into its recipe catalogue. Each binding is revealed when the boss associated with its essence is defeated, even if the forge still lacks the Temper required to condense that essence. Revealed rows show framed input, essence, and result slots alongside their requirements. A row becomes selectable only when both ingredients are present and the required Temper is available; selection links both inventory items and opens the focused ritual screen. That screen contains no secondary inventory grid, offers Back to Recipes, and returns to the catalogue after a binding begins.

## Multiplayer model

- Enemy orbs and bloodstains are free-for-all.
- Soul balances belong to player characters.
- Terraforge Temper and boss unlocks belong to the world.
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
- Does 100 souls make the Terra Blade Fragment accessible at the right time?
- Does 2,500 souls per Slime Essence create useful early farming without excessive repetition?
- Should Temper prices actually equal 50% of milestone boss rewards after the reward formula stabilizes?
- Does free-for-all recovery create enjoyable multiplayer tension or unacceptable griefing?

