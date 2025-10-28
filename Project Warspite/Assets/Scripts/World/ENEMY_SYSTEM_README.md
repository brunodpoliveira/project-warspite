# Enemy System - Phase 1 Implementation

## Overview
Data-driven enemy system using ScriptableObjects for easy configuration. Based on the existing SimpleTurret but refactored for flexibility.

## Components

### 1. **EnemyConfig.cs** (ScriptableObject)
Defines all stats for an enemy type:
- Health
- Weapon stats (magazine, fire rate, burst, reload)
- Damage and falloff
- Projectile properties
- Accuracy cone settings
- Special behaviors (grenades, laser telegraph, shotgun pellets)

### 2. **EnemyLogic.cs** (MonoBehaviour)
Main enemy behavior script:
- Reads stats from EnemyConfig
- Handles firing, reloading, bursts
- Implements accuracy cone system
- Supports sniper laser telegraph
- Shotgun multi-pellet firing
- Auto-creates projectiles if no prefab assigned

### 3. **EnemyConfigCreator.cs** (Editor Utility)
Creates all 6 enemy config assets with proper stats:
- Pistol Infantry
- Shotgun Rusher
- Grenadier
- Assault Rifle Soldier
- Machine Gunner
- Sniper

## Quick Start

### Step 1: Create Enemy Configs
1. In Unity menu: **Warspite > Create All Enemy Configs**
2. This creates 6 ScriptableObject assets in `Assets/Data/EnemyConfigs/`

### Step 2: Create Enemy Prefab
1. Create a GameObject (e.g., Cube or your enemy model)
2. Add `EnemyLogic` component
3. Add `Health` component (from existing Core namespace)
4. Add `TurretHealth` component (or create new EnemyHealth)
5. Assign an `EnemyConfig` asset to the EnemyLogic component
6. (Optional) Assign a Transform for `muzzlePoint`
7. (Optional) For Sniper: Add LineRenderer component for laser

### Step 3: Drag & Drop
- Save as prefab
- Drag into scene
- Enemy will auto-target player and fire based on config

## Configuration Examples

### Pistol Infantry
```
Magazine: 15 rounds
Fire Rate: 2 rounds/sec
Damage: 20
Spread: 2° base + 0.05° per meter
```

### Shotgun Rusher
```
Magazine: 7 rounds
Fire Rate: 1 round/sec
Damage: 80 (close range)
Pellets: 8 per shot
Spread: 8° wide pattern
```

### Sniper
```
Magazine: 1 round
Fire Rate: 0.5 rounds/sec (2 sec between shots)
Damage: 120
Telegraph: 1.5 sec laser warning
Spread: 0.5° (very accurate)
```

## Updating All Enemies

When you update an `EnemyConfig` asset, **all enemies using that config** will automatically update. This makes it easy to:
- Tweak damage values
- Adjust fire rates
- Change accuracy
- Modify projectile appearance

No need to update individual prefabs!

## Adding Accuracy Cone (Future)

The system is already set up for accuracy cone:
- `baseSpreadAngle`: Starting cone angle
- `spreadMultiplier`: Additional spread per meter distance
- Formula: `totalSpread = baseSpread + (distance × spreadMultiplier)`

The cone is applied in `EnemyLogic.FireSingleProjectile()` and visualized in the Scene view gizmos.

## Adding Damage Falloff (Future)

Config has `useDamageFalloff` flag. Implementation will be in Phase 2 when we enhance the Projectile script to:
- Track spawn distance
- Calculate damage based on distance traveled
- Use weapon-specific falloff curves

## Next Steps (Phase 2)

1. Enhance `Projectile.cs` to use damage from EnemyConfig
2. Implement damage falloff system
3. Add grenade explosion mechanics
4. Improve visual feedback (muzzle flash, tracers)
5. Add proper enemy health/death handling

## Notes

- Enemies use `Time.deltaTime` so they slow down with time dilation (world-scaled)
- Player uses `Time.unscaledDeltaTime` to stay responsive
- Ballistic trajectory calculation included for projectile arc
- System is "bodged" for prototype - clean implementation comes later
