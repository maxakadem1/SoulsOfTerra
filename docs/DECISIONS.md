# Souls of Terra — Settled Design Decisions

> Internal record. Contains story spoilers. Change a decision deliberately and update this file when the new direction is agreed.

## Core economy

| Topic | Decision | Reason |
|---|---|---|
| Currency representation | Souls are an abstract UI balance. | A central currency should remain readable and avoid inventory clutter. |
| Enemy configuration | Calculate rewards globally from actual scaled NPC data. | Per-NPC configuration is unmaintainable and incompatible with broad mod support. |
| Reward consistency | Rewards are deterministic. | Players can understand and plan around the economy. |
| Segmented enemies | One reward for the complete enemy. | Prevents multiplying rewards by body-part count. |
| World drops | Enemies release collectible soul orbs. | Gives rewards a physical, satisfying journey into the player. |
| Orb ownership | Free-for-all. | Keeps the first multiplayer model simple and preserves shared-world tension. |
| Orb lifetime | Temporary, similar in spirit to Terraria money. | Avoids permanent world clutter; only bloodstains need persistence. |
| Permanent leveling | Excluded. | The mod should expand equipment and choices without invalidating Terraria's progression. |

## Death and recovery

| Topic | Decision | Reason |
|---|---|---|
| Trigger | Every genuine player death. | The risk remains consistent and easy to understand. |
| Amount lost | Entire carried balance. | Establishes souls as a meaningful risk economy. |
| Recovery | Intentional right-click on the bloodstain. | Walking nearby should not erase the deliberate recovery moment. |
| Bloodstain ownership | Free-for-all for now. | Owner-only networking can be added later if testing justifies its complexity. |
| Repeated death | A new death removes the character's previous stain. | Preserves the central recover-or-lose tension. |
| Persistence | Bloodstains save with the world. | Quitting should not be a way to erase or duplicate the risk state. |

## Visual identity

| Topic | Decision | Reason |
|---|---|---|
| Art form | Smooth procedural particle/orb rather than a pixel sprite. | Souls should feel ethereal and distinct from ordinary Terraria drops. |
| Value scale | Continuous size/intensity plus white→green→blue→purple colors. | Communicates relative value without discrete item tiers. |
| Boss color | Orange/yellow is exclusive to bosses. | Boss rewards need an immediate categorical distinction. |
| Trails | One attraction trail per orb. | Multiple trails were visually noisy and were removed after testing. |
| Interior | Mostly transparent core with large rotating wisps and a bright border. | Keeps the internal motion visible instead of becoming a solid glowing disk. |
| Death marker | A grounded void-violet pool with cyan-edged returning wisps. | Separates persistent lost souls from collectible enemy orbs while preserving a shared spectral language. |
| Death marker value | Four capped intensity tiers alter wisp count, brightness, and pulse without changing the footprint. | Larger losses feel more important without producing world clutter or misleading interaction range. |
| Death marker interaction | Hovering brightens the pool and draws its wisps toward the player; recovery collapses them into the player and leaves a short ground burst. | Makes deliberate right-click recovery legible and satisfying without delaying the authoritative balance change. |
| Gain text | Nearby rewards accumulate into one notification. | A small pickup should not overwrite a boss-sized reward before it can be read. |
| Terraforge catalogue | Five-column scrollable grid with self-contained essence cards, hover details, and one contextual Condense button. | Fifteen essences fit at once, the catalogue scales without placeholder cards, and descriptions consume no permanent space. |
| Undiscovered essences | Show generic locked silhouettes without boss names. | Communicates future depth without spoiling encounters or rewards. |

## Soulless and the Terraforge

| Topic | Decision | Reason |
|---|---|---|
| Guide character | A town NPC named Soulless appears at the beginning. | Gives the global system an in-world teacher and future narrative anchor. |
| Long-term role | Soulless secretly benefits from the player's payments and becomes a villain. | Turns routine economic progression into narrative setup. |
| Initial access cost | Soulless sells one Terra Blade Fragment per world for 100 souls. | Makes the system available early while teaching the first spending decision. |
| Access item identity | A genuine hiltless shard of an ancient Terra Blade. | Grounds the relic in Terraria while making Soulless's possession of it suspicious. |
| Fragment recovery | Soulless recalls it for free whenever no Terraforge is active. | Prevents ordinary item loss from permanently blocking world progression. |
| Forge creation | Right-click an Iron or Lead Anvil while holding the fragment. | Makes formation a deliberate forging ritual grounded in Terraria. |
| Forge footprint | Replace the anvil with a supported 4-by-3 Terraforge if the area is clear. | Reserves space for the embedded fragment and prevents block overlap. |
| Forge uniqueness | Only one Terraforge may be active per world. | Gives the ancient relic narrative weight and matches world-wide progression. |
| Source variants | Iron and Lead Anvils share one appearance but return their original material when dismantled. | Supports both world ores without doubling sprite work. |
| Forge presentation | The fragment remains visibly embedded in the transformed station. | Makes the fragment the legible source of its soul-forging power. |
| Energy language | Teal souls flow into green-gold Terra energy. | Separates soul fuel from the fragment's transformed output at a glance. |
| Formation | A short localized ritual ends in a metallic impact, camera punch, sparks, and soul shockwave. | Makes the unique world station consequential without interrupting player control. |
| Idle presentation | Ambient effects remain subtle and surge only while forming, condensing, or imbuing. | Keeps player bases readable while preserving ritual spectacle. |
| Forge operations | Tabs use the verbs Condense and Imbue. | Keeps the two-step essence loop concise and explicit. |
| Forge name | Terraforge. | Describes its actual condensing and weapon-imbuing function. |
| Forge utility | It does not count as a normal anvil. | The transformation creates a ritual apparatus rather than an upgraded mundane station. |
| Dismantling | Any pickaxe works; explosions do not. | Relocation remains accessible while accidental destruction is prevented. |
| Progression term | Soulless Tempers the Fragment; recipes require Terraforge Temper. | Unifies the forging language across upgrades and requirements. |
| Temper payment | Souls only. | Keeps the core economy central. |
| Temper meaning | Terraforge Temper is the ceiling every weapon may be tempered toward, not a gate on continuing. | Converts a tollbooth with no decision into structure players plan around, and makes the temper ceiling a consequence of progression rather than an arbitrary number. |
| Intended price target | Approximately 50% of the milestone boss reward. | Encourages some additional combat without demanding excessive farming. |

## Progression and crafting

| Topic | Decision | Reason |
|---|---|---|
| Mandatory milestones | Nine major vanilla progression gates from Eye of Cthulhu through Moon Lord. | Provides a readable backbone without adding a tier for every encounter. |
| Optional bosses | Unlock distinct essences rather than mandatory Temper levels. | Optional fights remain rewarding without blocking the main path. |
| Essence identity | Each supported boss gets a distinct themed essence. | Bosses can support recognizable equipment families. |
| Essence supply | Unlimited condensation after the boss is defeated. | Avoids requiring one boss kill for every individual equipment piece. |
| Conversion direction | Souls condense irreversibly into physical essences. | Creates a meaningful spending commitment and tradable crafting material. |
| Essence quantity | Roughly one expensive essence per weapon or armor piece. | Each piece has a meaningful soul cost without recipes demanding large essence stacks. |
| Final equipment acquisition | All mod weapons are created exclusively through Terraforge imbuement; armor, accessories, and non-weapon equipment continue using normal tier-appropriate stations. | Gives weapons one distinctive acquisition language without duplicating essence crafting and imbuement. |
| Initial prototype | King Slime → Slime Essence → Slimebound Blade. | Exercises the entire architecture with an early, easy-to-test boss. |
| Initial essence cost | 2,500 souls. | Starting balance point; explicitly subject to playtesting. |

## Buried Court and Sealed Congregation

| Topic | Decision | Reason |
|---|---|---|
| Progression position | After Skeletron and before Wall of Flesh. | Establishes an original late-pre-Hardmode encounter. |
| Arena | A 168-by-84 grand vaulted castle hall centered beneath spawn, with a flat 144-by-60 combat chamber. | Creates a readable boss arena and a permanent narrative location reusable for Soulless. |
| Architecture | A restrained custom tileset forms a formally symmetrical, cold blue-gray funerary court. Pointed vaults, columns, chains, and seal reliefs remain background-only; localized damage never enters the combat volume. | Gives the permanent landmark its own identity without compromising attack readability. |
| Arena topology | One uninterrupted floor and a collision-free 144-by-60 chamber, with no side galleries, raised dais, interior columns, or hanging ribs. | Keeps movement and projectile reads predictable while players remain free to place their own platforms. |
| Discovery | A collapsed physical passage descends into a ruined exterior antechamber, then enters through an open floor-level side arch. A passable spectral curtain may warn of an active encounter. | Preserves the staged reveal without placing stairs in combat space or hard-locking multiplayer arrivals. |
| Court materials | Generated brick and unsafe wall variants are permanently unbreakable; safe craftable counterparts unlock after the Congregation. | Preserves the location for repeat encounters and Soulless's later confrontation while making its palette available to builders. |
| Structure authoring | A bundled tile-grid asset is placed by a small internal loader; connection terrain and functional objects remain procedural. | Enables schematic-style iteration without a player-facing dependency. |
| Lighting | Eight Boreal Wood floor lamps, six roof-anchored Boreal chandeliers, and softly glowing mid-wall seal reliefs provide three bands of cool-blue light without solid collision inside the chamber. | Spreads readable illumination across the full height of the large arena while keeping its prison iconography visible. |
| Location reveal | Entering the hall displays a six-second top-center title with restrained teal bloom; it can retrigger only after ten minutes. | Establishes the Court without obscuring combat or becoming noisy near its boundary. |
| Court music | Vanilla Dungeon music plays throughout the hall and yields to boss-priority music. | Gives the location an immediate atmosphere while dedicated original music remains deferred. |
| Existing worlds | Initial arena generation requires a new world. | Silent retroactive generation could destroy player or mod structures. |
| Summoning key | Unlimited non-consumable Warden's Fragment sold by Soulless for 10,000 souls after Skeletron. | Makes access permanent while retaining a soul-economy purchase. |
| Fragment identity | A broken ward-sigil with forged authority, an incomplete ring, teal fracture, and severed chain links. | Connects Soulless, the reliquary, and the Congregation while hiding the full betrayal behind ambiguous clues. |
| Summon cost | No cost beyond holding the key at the dais. | Repeat attempts and farming remain frictionless after access is purchased. |
| Summon presentation | The reliquary gathers rising souls, manifests four spectral seals, implodes, and releases a refractive shockwave before the boss appears. | Gives the permanent monument and first boss reveal a distinct ritual identity instead of spawning the NPC instantly. |
| Phase one | Four simultaneously vulnerable, independently destructible seals protect an invulnerable core. | Makes the seal art mechanically meaningful and supports player target choice. |
| Phase-two identity | A released, highly mobile procedural congregation. | Contrasts ritual order with unstable freedom without requiring more sprite animation. |
| Phase-two beam | Choir of Judgment uses a 1.5-second charge, a locked warning direction, and a 2.5-second 24-degree sweep that can hit each player once. | Replaces overwhelming contact charges with a readable signature attack that still controls arena space. |
| Health bar | One combined core-and-seal bar. | Accurately reports progress while the core is invulnerable. |
| Essence reward | Congregation Essence, Temper 3, 20,000 souls. | Integrates the original boss with established Terraforge progression. |
| Expert accessory | Borrowed Sentence defers 40% of a post-defense hit worth at least 10% maximum life. Dealing 12 times the deferred amount within six seconds erases it; otherwise the exact lethal-capable debt returns, followed by a fixed 14-second cooldown. | Turns the Congregation's judgment into a class-neutral combat trial instead of another passive defensive stat. |
| Summon reward | Imp Staff + Congregation Essence produces Compeditus, a shared core with up to four one-slot seals. | Converts the boss's defining formation into a distinctive summon while respecting imbuement-only weapon acquisition. |
| Melee reward | Muramasa + Congregation Essence produces Unison, a committed two-fist clap that releases a closed Hollow Benediction ring. | Gives Congregation a melee identity without copying the boss's four safe gaps. |
| Unison combat | Hands part and smash; a 360px expanding ring locks at the clap, deals one hit per enemy, and must finish before the next clap. | Reads as the same hymn at player scale, without dragging an arena-clear or overlapping rings. |
| Ranged reward | Handgun + Congregation Essence produces Crux, a cursor-locked crossed sentence. | Dungeon sibling to Unison; uses leftover Crossed Sentence language without full-screen lances. |
| Crux combat | Click locks the cursor; two arms write inward, knot, and hit once per enemy; one volley at a time; consumes bullets. | Fast aimed mark, not another wait-for-hymn special. |
| Magic reward | Magic Missile + Congregation Essence produces Stars of Ruin, a mana-hungry barrage of primeval stars. | Completes the Congregation mage slot with many voices instead of a choir-beam that read as a glowing stick. |
| Stars of Ruin combat | Click locks one visible NPC near the cursor; twelve sapphire-white stars gather at one wand-tip origin, follow six mirrored pairs of nested cubic lanes to form a complete teardrop bouquet, then begin homing. | Reproduces the source spell's two-sided parallel arcs without collapsing the stars into one line or leaving half of the bouquet empty. |
| Compeditus combat | Non-contact seals perform a staggered lance verse followed by a localized implosion judgment; the formation crosses terrain but attacks require line of sight. | Provides reliable summon damage and a coordinated payoff without enabling passive through-wall farming. |

## Eater of Worlds

| Topic | Decision | Reason |
|---|---|---|
| Ranged reward | Musket + Eater of Worlds Essence produces Carrion Call, a slow thrown meal that calls a boss-scale Eater dive. | Uses the Shadow Orb gun as the sacrifice while the held identity is Worm Food bait. |
| Carrion Call combat | Bait sticks to the first enemy or sits on a tile; after a rumble a frozen rise-and-dive comes from below, through the meal, then buries. Contact hits in air and tiles. One scrape per enemy per worm. A still-tagged host takes a 1.75× chomp; a floor meal makes a smaller burst. Misses get no bonus and the bait still vanishes. No ammo. Multiple hunts are allowed; throw speed is very slow. | Turns Worm Food into a readable hunt instead of a homing worm or a delayed javelin. |

## Essence imbuement catalogue

| Topic | Decision | Reason |
|---|---|---|
| Recipe catalogue | Every registered imbuement is listed from the first Imbue visit, including later-progression bindings. | Lets players inspect the full reward set they can work toward. |
| Progression gating | Locked rows stay visible and unselectable, stating the missing boss, Temper, or ingredients. | Teases future options without bypassing essence progression. |
| Recipe-first flow | Opening Imbuement shows the recipe catalogue; only a recipe with both ingredients present can open the focused ritual screen. | Makes the available binding the player's first decision and removes the redundant inventory picker grid. |
| Ingredient linking | Selecting a ready recipe links the first matching weapon and essence from the player's inventory without consuming them. | Removes inventory-search friction while preserving the ritual as the deliberate confirmation step. |
| Ritual presentation | The ready recipe opens a centered, mostly frameless canvas over the visible world, with an authored weapon frame, orbiting pickup-soul visuals, pixel-grid soul currents, and compact Back and Bind controls. | Reuses the soul economy's strongest visual identity while keeping the Terraforge and surrounding world visible. |
| Ritual completion | Successful binding returns to the recipe catalogue; the focused screen also provides Back to Recipes. | Keeps repeated bindings and cancellation predictable without exposing an empty ritual screen. |
| Imbuement cost | One valid base weapon and one matching essence, with no additional materials. | The expensive essence already carries the soul and progression cost. |
| Current bindings | Any Copper-through-Platinum broadsword + Slime Essence; Ruby Staff + Eye Essence; Musket + Eater of Worlds Essence (Carrion Call); Breaker Blade + Wall of Flesh Essence; Imp Staff + Congregation Essence; Muramasa + Congregation Essence; Handgun + Congregation Essence; Magic Missile + Congregation Essence (Stars of Ruin); Diamond Staff + Moon Lord Essence; Venus Magnum + Moon Lord Essence. | Establishes flexible early bases and precise thematic bases where desired. |
| Future enforcement | Every mod weapon inherits `ImbuementWeaponItem`, must be a registry output, and may not have a conventional recipe. | Turns the design rule into a load-time invariant instead of documentation alone. |

## Weapon temper and essence paths

| Topic | Decision | Reason |
|---|---|---|
| Role | Weapon temper is the primary soul sink; essence paths are the primary essence sink. | Every previous sink was a one-time purchase that closed, leaving souls as a scheduled tax rather than a decision. Temper never closes. |
| System count | Temper and infusion are one system, not two. Temper is magnitude, the essence path is character. | Two systems competing for the same currency produced four shallow mechanics instead of one deep one. |
| Scope | Any weapon may be tempered, vanilla or mod. | Limiting temper to ten imbuement weapons leaves the Terraforge idle for most of a playthrough, recreating the shallow-sink problem. |
| Storage | Temper is stored on the individual item instance, not the weapon type. | A specific object becomes worth protecting and acquires a history; a type-wide unlock is paid once and closes forever. |
| Level effect | Damage only. Use time, reach, projectiles, class, and prefix are untouched. | Anything expressive belongs to the essence path; two systems granting character would collide. |
| Authoring | Behaviour is derived systemically from weapon data, with a short override list for weapons whose output is not damage times rate. | Any vanilla weapon qualifies, so per-weapon authoring is impossible. |
| Damage curve | Temper converges the weapon toward an era target rather than scaling its existing damage. | Proportional scaling preserves the ordering of base damage, so a stronger weapon always wins at equal investment and reviving an old weapon is never rational. |
| Level +0 | A weapon deals its ordinary Terraria damage until tempered. | Parity must be purchased. Free parity would erase Terraria's loot progression and empty the sink at the same time. |
| Ceiling | A fully tempered weapon lands slightly above the strongest comparable weapon obtainable at that stage. | A ceiling level with era-best makes temper a catch-up mechanic that current-era weapons gain nothing from, so nobody invests. |
| Normalisation unit | Convergence targets damage per second, derived from use time, not raw damage. | Terraria prices nothing for attack speed; normalising raw damage would make the fastest weapon the only correct choice. |
| Ceiling authority | A weapon may never exceed the world's Terraforge Temper. | Prevents an early weapon from outclassing its era, using progression rather than an arbitrary cap. |
| Essence consumption | One essence per temper level. | Keeps essences in continuous demand instead of being condensed once per weapon. |
| Essence choice | Each weapon is raised along one essence path chosen at first temper; every level feeds that same essence. | Interchangeable fuel would leave nineteen essences with distinct art and no distinct purpose, and would leave essences without an imbuement weapon as permanent dead ends. |
| Path effect | The path grants an added effect that strengthens with each level. | Weapon identity becomes base moveset plus path, making a specific tempered weapon describable and memorable. |
| Path change | Re-infusion costs souls and destroys the accumulated essence investment. | Keeps the initial path choice meaningful while charging for reconsideration. |
| Outgrown weapons | A transfer ritual moves temper to another weapon for souls, losing a small number of levels. | Total loss makes hoarding rational and leaves the sink idle exactly when it should be busiest. A sink players fear to use is not a sink. |
| Transfer framing | Charging for regret suits Soulless. | Expresses his quiet profit through mechanics rather than a hidden counter. |
| Imbuement interaction | Imbuement preserves temper; a +7 Muramasa becomes a +7 Unison. | Resetting would punish imbuing after the early game and turn the mod's own weapons into things players avoid. |
| Mod weapon identity | Mod weapons are distinguished by moveset, not damage. | Under full parity no weapon is stronger than another at equal temper; ten distinctive movesets is a sound offering where ten "strong weapons" never was. |

## Mutations

| Topic | Decision | Reason |
|---|---|---|
| Role | The body's counterpart to essence paths: a path gives a weapon character, a mutation gives the player one. | Provides a clear identity distinct from accessories and from weapon temper. |
| Economic role | Explicitly not a soul sink. | Three permanent purchases close faster than any other system; asking them to absorb souls made them expensive, unrefundable, and shallow simultaneously. |
| Pricing | Cheap in souls, expensive in commitment. | Weapon temper carries the economy, freeing mutations to be rare and memorable rather than a toll. |
| Drawbacks | Every mutation grants a strong benefit and a real permanent cost. | Without a drawback a mutation is a free permanent stat that contradicts pillar 3, benefits every class identically, and homogenises builds. |
| Design space | Permanent downsides are near-absent from vanilla Terraria. | Makes grafting the mod's most distinctive surface. |
| Minion slots | Mutation pets consume minion slots, as Compeditus already does. | Free permanent minions for every class devalue the summoner. |
| Third slot | Needs a Classic-mode equivalent to the Demon Heart gate. | `Player.extraAccessory` is Expert-only, so Classic worlds currently have a permanently dead slot with no explanation. |
| Damage refresh | Mutation damage must recalculate live rather than being fixed at projectile spawn. | The current snapshot creates a hidden re-graft ritual whenever maximum life changes. |
| Purging | Must refund something. | A total loss means players never experiment, which is fatal for a system whose point is choosing. |

## Soulspells

| Topic | Decision | Reason |
|---|---|---|
| Role | Repeatable throughout-game sink via toggleable buffs, not projectiles. | Gives ordinary play a reason to spend souls between Temper and essence purchases. |
| Book | Two-page spreads with external page arrows; Always begins the first spread and learned Stance spells continue by category. | Supports the full potion catalogue without shrinking icons or showing unlearned entries. |
| Always spells | Toggled only in the book (or by right-clicking their buff). No Stance key, no drain. | Soul Skip can teach the book and stay up for corpse runs. |
| Stance | One keybind activates every checked paid spell as buffs; press again to stop drain. | One mix, one power switch. |
| Loadout cap | None. Drain is the cap. | Checking every box is a valid, expensive choice. |
| Cost model | Flat souls per interval per spell, no Temper scaling. | The printed number never silently changes. |
| First Always spell | Soul Skip: a double-tap easing surge that preserves vertical momentum, grants brief i-frames, and has no ram damage. A soul echo chases the player through a full-height torn wake and merges in a landing snap. | Expressive exploration movement without competing with combat accessories. |
| Alternate dash | Soul Flight is mutually exclusive with Soul Skip but both may be off. A horizontal double-tap gives two seconds of tile-colliding, four-direction flight at 9 px/tick as a 1.35x pickup soul, then a shared three-second cooldown. Ten-tick dissolves exchange the player and orb around a continuous soul wake. It preserves entry/exit momentum, releases grapples, blocks items, incoming damage, and all player-owned damage, but does not cleanse debuffs. | Creates seamless precision traversal with an explicit invulnerability tradeoff and no input conflict with vertical double-tap equipment. |
| First paid spell | Shine, 2 souls per second, learned and checked from spawn. Stance starts off. | Uses the exact vanilla effect to teach the same system used by dissolved potions. No starter discount, so the first toggle shows Stance is a spend. |
| Spawn defaults | Soul Skip on, Shine checked, Stance off. | They can skip immediately; Stance is a deliberate press. |
| Empty balance | Stance cannot start if paid drain is active and the pile is 0; Stance drops when a charge cannot be paid. | Closes a toggle exploit and keeps Always spells up. |
| Live-edit | Checks change while Stance is on. | The book is the mixer; Stance is the switch. |
| Potion-dissolve | Soulless sells unlimited Soul Apparatuses for 1,000 souls after the Eye of Cthulhu. One registered vanilla buff potion plus 200 souls permanently teaches that character the matching unchecked Stance spell. | Learning is a cheap rite. The bottle is the only gate; a second lock would recreate the grind. |
| Potion scope | Timed positive vanilla drinkable buff potions only; recovery, food, flasks, teleportation, permanent upgrades, and thrown Love/Stink potions are excluded. | Keeps every learned result compatible with sustained Stance behavior. |
| Potion pricing | Keep the existing relative drain ranking, multiplied by 25. Learning is a flat 200 souls. | Unlock stays cheap; Stance is the real cost. A utility lasts a cave trip, a combat mix lasts an expedition, and the full book is a short luxury. |
| Potion coexistence | Soulspells apply the vanilla buff ID; ordinary potions can still be consumed but never stack their stats. Right-clicking an active spell buff unchecks it. | Preserves Quick Buff behavior and prevents double-dipping. |

## Ranged melee

| Topic | Decision | Reason |
|---|---|---|
| Class direction | Melee weapons gain practical ranged engagement through weapon-specific attack shapes. | Melee identity comes from arcs, throws, impacts, momentum, and techniques rather than mandatory contact range. |
| Shared system | No universal true-melee swing, clash, or soul-edge framework. | Each weapon can choose the ranged behavior that best expresses its physical identity. |
| Slimebound | Restore the historical broadsword swing with a 1, 1, 3 bouncing gel-ball cycle. | Keeps the weapon functional while its eventual ranged-melee rewrite is designed independently. |
| Breaker throw | Left-click throws a straight point-first blade; up to five may lodge in enemies or terrain. | Embedding a heavy cleaver creates ranged pressure without making it resemble a gun. |
| Breaker recall | Right-click recalls every blade; returned blades spin, pass through terrain, and hit each enemy once. | Converging return paths provide crowd control and a deliberate setup/payoff rhythm. |
| Breaker extraction | Lodged hosts take a 125% extraction hit; return-path hits deal 70%, excluding that host. | Accurate embedding is the strongest payoff without double-hitting the host during recall. |
| Breaker tether | Bandages are visual only; missed or overly distant blades return automatically. | Tethers communicate ownership and range without unpredictable enemy pulling. |

## Decisions intentionally deferred

- Exact final reward formula and exception policy for unusual modded NPCs.
- Whether event farming (Pumpkin Moon, Frost Moon, invasions) needs its own reward treatment. `npc.value` throughput there exceeds ordinary play by orders of magnitude, which makes every fixed price meaningless for players who build an arena and punishing for those who do not. No price in the mod can be set honestly until this is resolved.
- The soul cost curve per temper level, and how many levels sit within each Terraforge Temper.
- The exact convergence formula and its era targets, plus the override list for weapons whose output is not damage times rate.
- The nineteen essence path effects and how strongly each scales with level.
- The specific drawback attached to each mutation.
- Whether the 25× Stance drain scale needs easing after play. The starting numbers make a combat mix last about 15 minutes and the full book about a minute off a 10,000-soul Eye-tier pile.
- Whether Soul Crystals should remain a 25% opt-out from the death penalty. Items do not drop on death in Classic or Softcore, so banking before a fight makes bloodstains irrelevant for a modest fee.
- Final Terraforge Temper costs after measuring real boss payouts.
- Whether bloodstains eventually become owner-only or configurable.
- The complete King Slime equipment set and its mechanical identity.
- The order and content of later boss essence families.
- How Soul Crystal conversion fees are expressed in Soulless's later story without a tracked hoard counter.
- Original visual designs and final UI presentation.
