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

## Essence imbuement discovery

| Topic | Decision | Reason |
|---|---|---|
| Recipe discovery | An imbuement becomes visible when the boss associated with its essence is defeated. | Makes discoveries world-progression rewards rather than inventory accidents. |
| Temper gating | Discovered recipes remain visible before their required Terraforge Temper, with the missing Temper stated. | Teases future options without bypassing essence progression. |
| Recipe-first flow | Opening Imbuement shows the recipe catalogue; only a recipe with both ingredients present can open the focused ritual screen. | Makes the available binding the player's first decision and removes the redundant inventory picker grid. |
| Ingredient linking | Selecting a ready recipe links the first matching weapon and essence from the player's inventory without consuming them. | Removes inventory-search friction while preserving the ritual as the deliberate confirmation step. |
| Ritual completion | Successful binding returns to the recipe catalogue; the focused screen also provides Back to Recipes. | Keeps repeated bindings and cancellation predictable without exposing an empty ritual screen. |
| Imbuement cost | One valid base weapon and one matching essence, with no additional materials. | The expensive essence already carries the soul and progression cost. |
| Current bindings | Any Copper-through-Platinum broadsword + Slime Essence; Ruby Staff + Eye Essence; Breaker Blade + Wall of Flesh Essence; Imp Staff + Congregation Essence; Muramasa + Congregation Essence; Handgun + Congregation Essence; Magic Missile + Congregation Essence (Stars of Ruin); Diamond Staff + Moon Lord Essence. | Establishes flexible early bases and precise thematic bases where desired. |
| Future enforcement | Every mod weapon inherits `ImbuementWeaponItem`, must be a registry output, and may not have a conventional recipe. | Turns the design rule into a load-time invariant instead of documentation alone. |

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
- Final Terraforge Temper costs after measuring real boss payouts.
- Whether bloodstains eventually become owner-only or configurable.
- The complete King Slime equipment set and its mechanical identity.
- The order and content of later boss essence families.
- How Soul Crystal conversion fees are expressed in Soulless's later story without a tracked hoard counter.
- Original visual designs and final UI presentation.
