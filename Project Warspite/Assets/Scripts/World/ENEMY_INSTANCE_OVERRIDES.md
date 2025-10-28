# Enemy Instance Overrides Guide

## Overview
You can now override individual enemy stats without changing the ScriptableObject config or creating new configs!

## How to Use Per-Instance Overrides

### Step 1: Select Enemy Instance in Scene
1. Click on the enemy GameObject in Hierarchy
2. Look at the Inspector

### Step 2: Expand "Per-Instance Overrides (Optional)"
You'll see these fields:
- **Fire Rate Override** (rounds per second)
- **Damage Override** (damage per hit)
- **Magazine Size Override** (rounds before reload)
- **Reload Time Override** (seconds to reload)

### Step 3: Set Override Values
- **Leave at 0** to use the config value
- **Set to any value > 0** to override

## Examples

### Make a Pistol Enemy Fire Faster
```
Config: PistolInfantry.asset (default 2 rounds/sec)
Fire Rate Override: 5
Result: This enemy fires at 5 rounds/sec instead of 2
```

### Make a Sniper Deal Less Damage
```
Config: Sniper.asset (default 120 damage)
Damage Override: 80
Result: This enemy deals 80 damage instead of 120
```

### Make a Machine Gunner Reload Faster
```
Config: MachineGunner.asset (default 4 sec reload)
Reload Time Override: 2
Result: This enemy reloads in 2 seconds instead of 4
```

### Give a Shotgun More Ammo
```
Config: ShotgunRusher.asset (default 7 rounds)
Magazine Size Override: 12
Result: This enemy has 12 rounds before reload
```

## Use Cases

### 1. Boss Variants
Create a tougher version of an enemy:
```
Config: AssaultRifleSoldier.asset
Fire Rate Override: 10 (faster)
Damage Override: 50 (higher)
Magazine Size Override: 60 (more ammo)
→ Creates a "Heavy Assault" boss enemy
```

### 2. Tutorial/Easy Enemies
Make enemies easier for testing:
```
Config: MachineGunner.asset
Fire Rate Override: 2 (much slower)
Damage Override: 10 (weaker)
→ Creates a non-threatening training dummy
```

### 3. Difficulty Scaling
Place different strength enemies in different areas:
```
Early Area: Fire Rate Override: 1 (slow)
Mid Area: Fire Rate Override: 0 (use config default)
Late Area: Fire Rate Override: 5 (fast)
```

### 4. Special Encounters
Create unique enemy variants:
```
"Sniper Pistol" - Pistol with sniper damage
Config: PistolInfantry.asset
Damage Override: 120
```

## Important Notes

### Override Priority
Overrides always take priority over config values:
```
If Override > 0: Use Override
If Override = 0: Use Config Value
```

### Config Still Controls Everything Else
Overrides only affect:
- Fire Rate
- Damage
- Magazine Size
- Reload Time

Everything else comes from the config:
- Projectile speed
- Accuracy cone
- Burst count
- Special behaviors (laser, grenades, etc.)

### Prefab Variants vs Overrides

**Use Prefab Variants when:**
- You want to change the config (different weapon type)
- You want to reuse the variant multiple times
- You want to save the configuration

**Use Overrides when:**
- You want a one-off unique enemy
- You're testing/tuning values
- You want quick tweaks without creating assets

## Workflow Examples

### Scenario 1: Testing Fire Rate Values
1. Place enemy in scene
2. Set Fire Rate Override to different values (1, 2, 5, 10)
3. Test in Play mode
4. Once you find the right value, update the config asset
5. Set Override back to 0

### Scenario 2: Creating Enemy Variants
1. Create base enemy prefab with PistolInfantry config
2. Right-click prefab → Create Prefab Variant
3. Name it "Enemy_Pistol_Fast"
4. Open variant, set Fire Rate Override: 4
5. Save variant
6. Now you have two pistol variants to place in levels

### Scenario 3: Difficulty Progression
```
Zone 1 Enemies:
- Fire Rate Override: 1 (slow)
- Damage Override: 15 (weak)

Zone 2 Enemies:
- Fire Rate Override: 0 (default)
- Damage Override: 0 (default)

Zone 3 Enemies:
- Fire Rate Override: 5 (fast)
- Damage Override: 50 (strong)
```

## Debugging

### Check Active Values in Play Mode
In Play mode, you can see which values are being used:
- **Fire Interval** property shows actual fire rate (with overrides)
- **Magazine Size** property shows actual magazine size
- **Reload Progress** uses override reload time

### Override Not Working?
- ✓ Check override value is > 0
- ✓ Check config is assigned
- ✓ Check enemy is enabled
- ✓ Try entering/exiting Play mode

## Performance Note
Overrides have zero performance cost - they're just simple checks at startup and during firing.

---

## Quick Reference

| Override Field | Config Default | Good Test Values |
|----------------|----------------|------------------|
| Fire Rate Override | 0.5 - 10 | 1 (slow), 5 (fast), 10 (rapid) |
| Damage Override | 15 - 120 | 10 (weak), 50 (medium), 100 (strong) |
| Magazine Size Override | 7 - 150 | 5 (low), 30 (medium), 100 (high) |
| Reload Time Override | 2 - 4 | 1 (fast), 3 (medium), 5 (slow) |
