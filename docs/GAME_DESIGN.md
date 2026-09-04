# Souls of Terra — Game Design

> Internal design document. Contains story and progression spoilers.

## Vision

Souls of Terra is intended to grow into a broad content mod in the spirit of mods such as Thorium: new progression, bosses, equipment, characters, and systems that coexist with Terraria's normal structure.

The soul economy is the foundation connecting that future content. It should make ordinary exploration and combat meaningful while preserving Terraria's broader equipment progression and class identity.

## Design pillars

1. **Every real fight has value.** Genuine hostile creatures release souls without requiring a hand-authored reward for every vanilla or modded NPC.
2. **Carried wealth creates tension.** Souls are abstract currency that drop on every genuine player death and must be deliberately recovered.
3. **Souls buy equipment, not character levels.** No permanent health, damage, defense, or attribute progression is attached to the player. Power bought with souls lives in gear that can be lost, transferred, or left behind.
4. **Any weapon can stay viable.** Terraria retires a weapon the moment a better one drops. Souls buy the right to refuse that: tempering raises any weapon, vanilla or mod, to the power of the player's current era. Armor, accessories, and supporting equipment continue to use ordinary materials and stations.
5. **One weapon-acquisition language.** Every Souls of Terra weapon is created by essence imbuement at the Terraforge rather than splitting weapons between imbuement and conventional crafting. Mod weapons are distinguished by their movesets, not by their damage.

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

- **Weapon temper.** The primary and effectively unbounded sink. Raising one weapon through its temper levels consumes the majority of a playthrough's souls.
- Condensing souls into boss-specific essences, which weapon temper then consumes level by level.
- Tempering the Terra Blade Fragment, which raises the ceiling every weapon may be tempered toward.
- Purchasing access items from Soulless.
- Transfer and re-infusion rituals, charged at the moment a player wants to change a past commitment.
- Fueling Stance soulspells, as a noticeable ongoing drain.

Soul costs should provide a reason to fight and farm without forcing one optimal enemy farm.

### Why weapon temper carries the economy

An earlier design spread spending across several shallow systems — imbuement, soulspells, mutations, and crystals — each of which a player finished with and never returned to. Every one-time purchase eventually closes, and a currency whose sinks all close becomes a tax paid on a schedule rather than a decision.

Weapon temper does not close. It scales with the player's era, it consumes essences continuously, and it is never fully paid off because a player may always take another weapon further. It also produces the economy's first genuine tradeoff: ride Terraria's loot ladder for free and swap weapons constantly, or pour souls into one weapon and carry it to the end. Both are viable, they cost different things, and choosing between them is the decision the soul balance exists to serve.

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

Temper is not a tollbooth. It is the ceiling: **a weapon may never be tempered beyond the world's current Terraforge Temper**, and the damage a fully tempered weapon converges toward is defined by the tempered era. Buying Temper answers "how far can I take this weapon," which is a question players plan around, rather than "may I continue," which they merely pay.

This also makes the ceiling non-arbitrary. A first-hour weapon cannot outclass its era, because the forge has not been tempered far enough to pull it there.

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

## Weapon temper and essence paths

Weapon temper is the mod's central loop. Tempering and infusion are one system viewed on two axes: **temper is magnitude, the essence path is character.**

### Scope and ownership

Any weapon may be tempered at the Terraforge, vanilla or mod. Restricting temper to the ten imbuement weapons would leave the Terraforge idle for most of a playthrough, which is the failure the system exists to correct.

Temper lives on the **individual item instance**, not the weapon type. The sword in the player's hand is +7; another copy of the same sword is +0. This makes a specific object worth protecting and gives it a history, which a type-wide unlock cannot. It also keeps the sink open, because a type-wide unlock is paid once and then closed forever.

### What a level grants

A temper level grants damage and nothing else. Every other property of the weapon — use time, reach, projectiles, class, prefix — is untouched.

Because any vanilla weapon qualifies, nothing about temper may be authored per weapon. All behaviour is derived systemically from the weapon's own data.

### The damage curve

Temper converges a weapon toward a target defined by the world's Terraforge Temper, rather than scaling the weapon's existing damage.

Proportional scaling was rejected: it preserves the ordering of base damage, so a stronger weapon always stays stronger at equal investment, and reviving an older weapon is never rational.

- **At +0** a weapon deals its ordinary Terraria damage. Untempered vanilla progression is fully intact; parity is something the player buys.
- **As levels are bought** the weapon climbs from its real base toward the era target. A weak weapon climbs a long way; a current-era weapon climbs a short way.
- **At the cap** any weapon lands slightly above the strongest comparable weapon obtainable at that stage.

The ceiling sits *above* era-best rather than level with it. Level with it would make temper a catch-up mechanic that only helps weapons that are behind, so a current-era weapon would gain almost nothing and nobody would invest in one. Sitting slightly above means tempering the player's main weapon is always worthwhile regardless of what that weapon is — the amount gained differs, but the destination does not.

Convergence targets **damage per second, not raw damage**. Terraria prices nothing for attack speed, so normalising raw damage would make the fastest weapon in the game the only correct choice. The damage granted per level is therefore derived from the weapon's use time: slow weapons receive more per level, fast weapons less.

A small override table covers weapons whose real output is not damage times rate — multi-projectile firearms, yoyos, ticking flails, channelled weapons, and minion staves whose listed damage is not the damage dealt. This is an exception list, not per-weapon authoring, and follows the same principle already permitted for NPCs with misleading `value` data.

### Essence paths

Each weapon is raised along one **essence path**, chosen when it is first tempered. Every level consumes one essence of that path.

The path grants an added effect that strengthens with each level — poison, chill, lifesteal, pierce, homing, detonation, and so on — so a weapon's full identity is its base moveset plus the essence it was raised on. "A +8 Queen Bee Muramasa" describes a specific object.

Paths are what keep the nineteen essences distinct. Treating essences as interchangeable fuel would give them separate art, bosses, and names but no separate purpose, and would leave the essences with no imbuement weapon as permanent dead ends. Every essence is a viable path, and each path needs nine of its essence per weapon, so demand is deep rather than one-time.

Changing a weapon's path requires re-infusion: it costs souls and destroys the accumulated essence investment.

### Transfer

A transfer ritual moves a weapon's temper to another weapon for a soul cost scaled to the level moved, losing a small number of levels in the process.

Total loss was rejected. If investment could be stranded permanently, the rational play is to hoard souls until the endgame weapon is certain, which leaves the sink idle through exactly the stretch it is meant to fill. A sink players are afraid to use is not a sink.

Transfer also converts the player's regret into a paid service, which suits a shopkeeper who quietly profits from every decision the player reconsiders.

### Interaction with imbuement

Imbuement **preserves temper**. A +7 Muramasa bound with Congregation Essence becomes a +7 Unison.

Resetting to +0 would make imbuement a punishment after the early game and would turn the mod's own weapons into things players avoid. Preserving temper makes imbuement a moveset change available at any point, which keeps essences desirable far later into a playthrough. It is also the correct fiction: the weapon changes shape, but what was poured into it remains.

## Soulspells

Soulspells are a player-held rite list, not fireballs. A Spellbook keybind opens paged two-page spreads. An Always band holds free spells that stay on without Stance. A Stance keybind turns every checked paid spell on or off together as buffs. Paid spells drain a fixed souls-per-interval that never scales with Temper; the sum of checked paid spells is the price, with no slot cap. Live edits apply immediately.

Soul Skip and Soul Flight are mutually exclusive free dash choices, though both may be disabled. Soul Flight transforms a horizontal double-tap into two seconds of tile-colliding four-direction flight using the collectible-soul appearance. The form preserves momentum into and out of flight, cannot use items, deal damage, or take damage, then enters a shared three-second dash cooldown.

Soul Skip is the first Always spell: a double-tap surge that preserves vertical momentum, eases back into player control, grants brief i-frames, and deals no ram damage. Copies of the player's actual appearance form an abandoned soul and staggered catching-up silhouettes, accompanied by converging fragments and a full-height torn pixelated wake. They reunite precisely when the surge ends in a jagged double ring, radial cuts, soul sparks, and a restrained camera kick; the wake then burns forward from the cast origin. It is free and on by default from spawn. Shine is learned and checked by default, uses the exact vanilla Shine buff, drains 2 souls per second, and teaches Stance before the apparatus is acquired. Stance requires at least 1 soul if any checked paid spell has a cost. Empty balance or death turns Stance off; Always spells are unaffected. Empty categories stay hidden.

After the Eye of Cthulhu, Soulless sells unlimited Soul Apparatuses for 1,000 souls. Its catalogue shows every supported potion recipe as Potion + 200 souls → buff-named soulspell. Owning the bottle is the only progression gate; later potions are expensive because they drain more, not because they hide behind an essence. A ready recipe opens a focused dissolution ritual; the potion and souls are consumed immediately and the spell is permanently learned, unchecked, for that character. Learned rows disable permanently. Recipes cover vanilla drinkable timed positive buff potions and exclude recovery potions, food, flasks, teleportation, permanent upgrades, and thrown potions. The resulting soulspell and an ordinary potion share the same vanilla buff ID, so their effects cannot stack.

## Mutations

Mutations are the body's counterpart to essence paths. A path gives a weapon a character; a mutation gives the player one. Both are permanent, both are bought with an essence, and both express which bosses the player chose to carry with them.

Mutations are explicitly **not** an economy sink. Three permanent purchases close faster than any other system in the mod, and asking them to absorb souls was what made them expensive, unrefundable, and shallow at the same time. Weapon temper carries the economy; mutations are freed to be cheap in souls and expensive in commitment.

### Every mutation carries a drawback

A graft grants a strong, distinctive benefit and a real, permanent cost — reduced defense, knockback vulnerability, slower movement, an exploitable weakness fitting the boss it came from.

This is the requirement that makes the system work. Without a drawback a mutation is a free permanent stat, which contradicts pillar 3, benefits every class identically, and therefore homogenises builds instead of differentiating them. With one, the interesting question stops being "can I afford this" and becomes "what am I willing to give up," which is the only question a permanent irreversible choice can meaningfully ask.

It also occupies design space Terraria barely uses. Vanilla has almost no permanent-downside mechanics, so grafting is the mod's most distinctive surface.

### Required corrections to the current prototype

- Mutation pets must consume minion slots, as `Compeditus` already does. Free permanent minions for every class devalue the summoner.
- The third slot is gated on `Player.extraAccessory`, which only a Demon Heart sets and which is Expert-only. Classic worlds currently have a permanently dead slot with no explanation. The gate needs a Classic-mode equivalent.
- Mutation damage is computed once when the projectile spawns and never refreshed, so a maximum-life increase after grafting has no effect until something kills the projectile. This creates a hidden re-graft ritual and must be recalculated live.
- Purging returns nothing. Some refund is required, or experimentation never happens.

## The Buried Court and The Sealed Congregation

The Buried Court is a grand, vaulted underground castle hall generated beneath world spawn in new worlds. A collapsed passage descends into a ruined exterior antechamber and enters through a floor-level side arch, revealing an uninterrupted 144-by-60 combat chamber. Its formally symmetrical cold blue-gray architecture uses background-only pointed vaults, columns, chains, and seal reliefs around a flush central reliquary. Localized cracks and missing reliefs suggest age without adding combat collision. Boreal Wood floor lamps, roof-anchored chandeliers, and softly glowing seal reliefs spread cool-blue light across the lower, upper, and middle chamber. Entering the hall fades in its authored title near the top of the screen while Dungeon music establishes the location; the title lasts six seconds and cannot retrigger for ten minutes. Players can discover the Court immediately, but its central altar remains dormant until Skeletron is defeated. The arena is a permanent narrative location intended for The Sealed Congregation, repeat encounters, and Soulless's eventual confrontation.

After Skeletron, Soulless sells unlimited Warden's Fragments for 10,000 souls. The fragment is a broken ward-sigil carrying forged, borrowed authority rather than a conventional key. Soulless claims the reliquary remembers the warden's office rather than the hand presenting its mark, quietly foreshadowing his knowledge of the prison and his interest in the souls within it. Holding the fragment and right-clicking the court's Warden's Reliquary begins a short summoning ritual without an additional soul cost. The dormant socket bends nearby light and emits a restrained teal pulse. Once activated, rising souls assemble four spectral seals above the court, collapse into the future core, and release a refractive shockwave as The Sealed Congregation manifests.

The reliquary's acceptance message implies that the fragment's authority is false. The Congregation's final voices describe the unseen hand behind the mark as hollow, creating an early clue about Soulless without explicitly revealing his future role.

The encounter has two stages:

- Four simultaneously vulnerable seals orbit an invulnerable procedural soul core. The seals coordinate geometric lance, curved-bolt, and chain-sweep formations. Each uses a broken visual below 35% health and snaps its chain when destroyed.
- Once every seal breaks, the released congregation becomes vulnerable and highly mobile. It charges Choir of Judgment above the player, commits to a warned direction, and slowly sweeps the spectral beam toward the player's prior movement. Delayed confessions and a major implosion-to-radial-eruption complete the phase.

The core, chains, internal faces, glow, telegraphs, and trails are code-driven. One clean seal sprite and one broken variant provide the authored boss art. The combined boss bar includes core and seal health so phase-one damage is always represented accurately.

Defeating the boss sets a world-wide progression flag and unlocks Congregation Essence at Terraforge Temper 3 for a prototype cost of 20,000 souls.

Its Expert treasure bag contains **Borrowed Sentence**, a fractured court seal that postpones 40% of any post-defense hit dealing at least 10% of the wearer's maximum life. The wearer has six seconds to deal twelve times the borrowed amount to hostile enemies. Success earns absolution and erases the damage; failure returns the exact stored damage and can kill. Further hits neither enter nor alter the active sentence, and a fixed fourteen-second cooldown begins after either outcome.

The Congregation's first equipment reward is **Compeditus**, created by binding Congregation Essence into an Imp Staff. It summons one shared spectral core surrounded by up to four seals, each consuming one minion slot. The formation passes through terrain but only attacks with clear line of sight. Seals deal no contact damage; instead, they fire restrained-homing lances in a staggered verse. Completing a verse expands and arrests the formation before its threads collapse into a localized judgment burst. The initial balance target is 22 summon damage, low knockback, a four-seal maximum, and one lance per seal per roughly 60-tick verse. **Stars of Ruin** binds Congregation Essence into a Magic Missile. Twelve sapphire shooting stars gather at its tip, launch in a rapid interleaved cascade, follow separate nested cubic lanes into a broad teardrop bouquet, and only then home toward one cursor-selected enemy.

The Terraforge's Imbue page opens directly into its full recipe catalogue. Every registered binding is listed from the first visit, including later-progression weapons, with framed input, essence, and result slots alongside their requirements. A row becomes selectable only when both ingredients are present and the required Temper is available; locked rows stay visible and name the missing boss, Temper, or items. Selection links both inventory items and opens the focused ritual screen. That screen contains no secondary inventory grid, offers Back to Recipes, and returns to the catalogue after a binding begins.

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
- No per-weapon authored temper behaviour; temper is derived systemically, with a short override list for weapons whose output is not damage times rate.
- No temper effect other than damage. Everything expressive belongs to the essence path.
- No temper level that exceeds the world's Terraforge Temper.

## Balance questions for playtesting

- Does the global reward formula pay fairly across common enemies, segmented enemies, events, bosses, and modded NPCs? In particular, do Pumpkin Moon, Frost Moon, and other event farms pay so far beyond ordinary play that every fixed price becomes meaningless?
- Does the damage-per-second convergence target hold up across melee, ranged, magic, and summon, and how large does the override list actually need to be?
- Is one weapon's full temper path affordable within the era it belongs to, or does it demand farming that outlasts the era?
- Does the transfer ritual's level loss feel like a fair rescue or a second punishment?
- Do players understand that +0 is unchanged and parity is purchased, or do they expect a newly found weapon to already be competitive?
- Are boss reward colors and continuous value differences readable during combat?
- Is bloodstain interaction reliable under combat pressure?
- Does 100 souls make the Terra Blade Fragment accessible at the right time?
- Does 2,500 souls per Slime Essence create useful early farming without excessive repetition?
- Should Temper prices actually equal 50% of milestone boss rewards after the reward formula stabilizes?
- Does free-for-all recovery create enjoyable multiplayer tension or unacceptable griefing?

