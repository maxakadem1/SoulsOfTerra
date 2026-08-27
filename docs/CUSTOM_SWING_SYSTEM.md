# Custom Swing System Architecture

## Overview
Slimebound Blade now uses a reusable custom melee swing system instead of vanilla Terraria's overhead arc. The system is designed for easy reuse by future melee weapons.

## Components

### 1. BaseCustomSwingProjectile (Abstract Base)
**Location:** `Content/Projectiles/BaseCustomSwingProjectile.cs`

**Purpose:** Reusable swing framework handling all common swing mechanics

**Handles:**
- Player arm positioning and animation
- Hitbox collision (line-based sweeping arc)
- Hitstop freeze on first hit
- Trail rendering during snap phase
- Multiplayer safety (server-authoritative)
- Vanilla swing suppression

**Customization Points (Virtual/Abstract):**
```
Timing Profile:
├─ SwingDuration (total length)
├─ WindupEnd (when windup ends)
├─ SnapStart (when damage starts)
└─ SnapEnd (when damage ends)

Visual Profile:
├─ SwingReach (arc length)
├─ CollisionWidth (hit width)
├─ SwordScale (visual scale)
├─ SwordItemType (which item texture)
└─ TrailLength (afterimage count)

Swing Arc:
├─ GetWindupStartOffset()
├─ GetWindupEndOffset()
├─ GetSnapEndOffset()
└─ GetWindupWobble()

VFX Hooks:
├─ OnSwingTick() - per-frame effects
├─ OnHitstopTick() - during freeze
├─ OnFirstHit() - first contact
├─ OnImpact() - hit splash
└─ GetTrailColor() - trail appearance
```

### 2. SlimeboundBladeSwingProjectile (Implementation)
**Location:** `Content/Projectiles/SlimeboundBladeSwingProjectile.cs`

**Customizations:**
- **Timing:** 45 tick total (22 windup, 13 snap, 10 recovery)
- **Arc:** Diagonal slash with slight overshoot
- **Windup:** Viscous wobble effect (sin wave damping)
- **Trail:** Cyan gel afterimages (60,220,195 RGB)
- **Sounds:** 
  - Windup: Item152 (viscous pull)
  - Snap: Item1 + Item95 (slash + whoosh)
  - Impact: DD2_MonkStaffSwing (punchy hit)
- **Impact:** 12-particle splash (first hit), 4-particle (subsequent)
- **Dust:** BlueCrystalShard with cyan tint during snap phase

### 3. SlimeboundBlade (Weapon Item)
**Location:** `Content/Items/Weapons/Melee/SlimeboundBlade.cs`

**Key Changes:**
```diff
- Item.useStyle = ItemUseStyleID.Swing;
+ Item.useStyle = ItemUseStyleID.Shoot;
- Item.UseSound = SoundID.Item1;
+ Item.UseSound = null;  // Swing projectile handles sounds
+ Item.noMelee = true;
+ Item.noUseGraphic = true;
- Item.shoot = ModContent.ProjectileType<RoyalGelBallProjectile>();
+ Item.shoot = ModContent.ProjectileType<SlimeboundBladeSwingProjectile>();
```

**Preserved:**
- 1-1-3 gel volley cycle (still spawns RoyalGelBallProjectiles)
- swingCounter state tracking
- Tooltip showing volley countdown
- All original stats (damage, knockback, etc.)

## Swing Phases

```
Phase 1: WINDUP (0-22 ticks)
├─ Player slowed (0.75x horizontal velocity)
├─ Blade pulls back with viscous wobble
├─ Sound: viscous pull (Item152)
└─ No damage

Phase 2: SNAP (23-36 ticks)  ← DAMAGE WINDOW
├─ Fast diagonal slash (cubic ease-out)
├─ Cyan dust trail spawns
├─ Sound: slash + whoosh
└─ Can hit enemies

Phase 3: RECOVERY (37-45 ticks)
├─ Blade holds at overshoot position
└─ No damage

On First Hit (during snap):
├─ 3-frame hitstop (time freeze)
├─ Enhanced gel splash (12 particles)
└─ Impact sound
```

## Multiplayer Safety

✅ **Server Authority:**
- Hitbox collision uses `ownerHitCheck` (line-of-sight validation)
- Hit cooldown via `usesLocalNPCImmunity`
- Projectile spawning in Shoot() method (server validates)

✅ **Client Safety:**
- Sounds only play on client (`NetmodeID.Server` check)
- Dust spawning client-side only
- Hitstop via ai[1] (synced automatically)

## Future Weapon Integration

To add custom swing to a new weapon:

1. Create `YourWeaponSwingProjectile : BaseCustomSwingProjectile`
2. Override timing properties and VFX hooks (~30 lines)
3. Update weapon item:
   - `Item.useStyle = ItemUseStyleID.Shoot`
   - `Item.noMelee = true`
   - `Item.noUseGraphic = true`
   - `Item.shoot = ModContent.ProjectileType<YourWeaponSwingProjectile>()`

See `CUSTOM_SWING_USAGE_EXAMPLE.cs` for complete documented example.

## Code Style Compliance

✅ File-scoped namespaces
✅ Tabs for indentation
✅ Short explanatory comments (why, not what)
✅ Proper sealed/virtual/abstract patterns
✅ No bloated documentation

## Unchanged Systems

✅ ImbuementWeaponItem base class
✅ Terra Shrine imbuement registry
✅ RoyalGelBallProjectile (gel volley projectile)
✅ EssenceboundBreakerBlade (still uses vanilla swing)
✅ All other weapons and systems
