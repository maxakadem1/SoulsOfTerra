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
| Gain text | Nearby rewards accumulate into one notification. | A small pickup should not overwrite a boss-sized reward before it can be read. |
| Shrine catalogue | Compact selectable essence grid with one detail area and Condense button. | More essences fit on screen without repeating large action controls. |
| Undiscovered essences | Show generic locked silhouettes without boss names. | Communicates future depth without spoiling encounters or rewards. |

## Soulless and shrines

| Topic | Decision | Reason |
|---|---|---|
| Guide character | A town NPC named Soulless appears at the beginning. | Gives the global system an in-world teacher and future narrative anchor. |
| Long-term role | Soulless secretly benefits from the player's payments and becomes a villain. | Turns routine economic progression into narrative setup. |
| Initial access cost | Broken Terra Blade costs 100 souls. | Makes the system available early while teaching the first spending decision. |
| Access item identity | Broken Terra Blade, not a coiled sword. | Preserves the Souls inspiration while grounding the object in Terraria. |
| Shrine creation | Right-click a campfire while holding the blade. | Makes the ritual discoverable and ties it to an existing safe-place object. |
| Shrine name | Terra Shrine. | Connects the mechanic to Terraria's world and avoids direct imitation. |
| Shrine scope | Upgrade level is world-wide. | Avoids forcing every player or every placed shrine through duplicate progression. |
| Upgrade payment | Souls only. | Keeps the core economy central. |
| Intended price target | Approximately 50% of the milestone boss reward. | Encourages some additional combat without demanding excessive farming. |

## Progression and crafting

| Topic | Decision | Reason |
|---|---|---|
| Mandatory milestones | Nine major vanilla progression gates from Eye of Cthulhu through Moon Lord. | Provides a readable backbone without adding a tier for every encounter. |
| Optional bosses | Unlock distinct essences rather than mandatory shrine tiers. | Optional fights remain rewarding without blocking the main path. |
| Essence identity | Each supported boss gets a distinct themed essence. | Bosses can support recognizable equipment families. |
| Essence supply | Unlimited condensation after the boss is defeated. | Avoids requiring one boss kill for every individual equipment piece. |
| Conversion direction | Souls condense irreversibly into physical essences. | Creates a meaningful spending commitment and tradable crafting material. |
| Essence quantity | Roughly one expensive essence per weapon or armor piece. | Each piece has a meaningful soul cost without recipes demanding large essence stacks. |
| Final crafting | Use normal stations appropriate to the equipment tier. | Preserves Terraria's crafting language and Magic Storage compatibility. |
| Initial prototype | King Slime → Slime Essence → Slimebound Blade. | Exercises the entire architecture with an early, easy-to-test boss. |
| Initial essence cost | 2,500 souls. | Starting balance point; explicitly subject to playtesting. |

## Decisions intentionally deferred

- Exact final reward formula and exception policy for unusual modded NPCs.
- Final shrine upgrade costs after measuring real boss payouts.
- Whether bloodstains eventually become owner-only or configurable.
- The complete King Slime equipment set and its mechanical identity.
- The order and content of later boss essence families.
- Rules and denominations for physical consumable soul items.
- How Soulless's hidden hoard changes his story encounter or boss fight.
- Original visual designs and final UI presentation.
