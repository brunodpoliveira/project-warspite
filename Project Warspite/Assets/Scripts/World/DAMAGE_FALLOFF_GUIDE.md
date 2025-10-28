# Damage Falloff System Guide

## Overview
Projectiles now deal less damage at longer ranges using a configurable falloff curve. This makes close-range combat more dangerous and encourages tactical positioning.

---

## How It Works

### **Distance Tracking**
- Projectile records spawn position
- Calculates distance traveled on impact
- Applies falloff multiplier to base damage

### **Falloff Formula**
```
finalDamage = baseDamage × falloffCurve.Evaluate(distance)
```

### **Default Falloff Curve**
```
Distance (m)    Damage Multiplier    Effective Damage (20 base)
0-10m           100%                 20 damage
10-20m          90%                  18 damage
20-30m          80%                  16 damage
30-40m          65%                  13 damage
40-50m          50%                  10 damage
50m+            30%                  6 damage
```

---

## Configuration

### **In EnemyConfig (Affects All Enemies of That Type)**

**Enable/Disable Falloff:**
```
useDamageFalloff: true/false
```

**Per-Weapon Falloff Profiles:**

#### **Pistol (Standard Falloff)**
- Close: 100% (0-10m)
- Medium: 80% (30m)
- Long: 50% (50m)
- Use: Balanced falloff

#### **Shotgun (Steep Falloff)**
- Close: 100% (0-5m)
- Medium: 40% (15m)
- Long: 10% (30m)
- Use: Punishes long-range use

#### **Assault Rifle (Moderate Falloff)**
- Close: 100% (0-10m)
- Medium: 85% (30m)
- Long: 60% (50m)
- Use: Effective at medium range

#### **Machine Gun (Moderate Falloff)**
- Close: 100% (0-10m)
- Medium: 80% (30m)
- Long: 55% (50m)
- Use: Sustained fire at range

#### **Sniper (Minimal Falloff)**
- Close: 100% (0-10m)
- Medium: 95% (30m)
- Long: 85% (50m)
- Use: Maintains damage at range

#### **Grenadier (No Falloff)**
- useDamageFalloff: false
- Grenades use blast radius instead
- Damage is consistent within radius

---

## In-Game Examples

### **Pistol Infantry (20 base damage)**
```
Point Blank (5m):  20 damage → Kills in 5 hits (100 HP)
Medium Range (25m): 16 damage → Kills in 7 hits
Long Range (45m):   10 damage → Kills in 10 hits
```

### **Shotgun Rusher (80 base damage)**
```
Point Blank (3m):  80 damage → Kills in 2 hits
Medium Range (15m): 32 damage → Kills in 4 hits
Long Range (30m):   8 damage → Kills in 13 hits
```

### **Sniper (120 base damage)**
```
Point Blank (5m):  120 damage → One-shot kill
Medium Range (30m): 114 damage → One-shot kill
Long Range (50m):   102 damage → One-shot kill
```

---

## Customizing Falloff Curves

### **Method 1: Edit in Unity (Visual)**

1. **Select a projectile prefab** (if you have one)
2. **Find Projectile component** in Inspector
3. **Expand "Damage Falloff"** section
4. **Click on Falloff Curve** graph
5. **Edit curve visually:**
   - Add keyframes (click on line)
   - Drag keyframes to adjust
   - Change tangents for smooth/sharp transitions

### **Method 2: Edit in Code (Precise)**

In `EnemyConfig.cs`, you can add custom falloff curves:

```csharp
// Shotgun - steep falloff
AnimationCurve shotgunFalloff = new AnimationCurve(
    new Keyframe(0f, 1.0f),   // 0m = 100%
    new Keyframe(5f, 0.8f),   // 5m = 80%
    new Keyframe(15f, 0.4f),  // 15m = 40%
    new Keyframe(30f, 0.1f)   // 30m = 10%
);

// Sniper - minimal falloff
AnimationCurve sniperFalloff = new AnimationCurve(
    new Keyframe(0f, 1.0f),   // 0m = 100%
    new Keyframe(30f, 0.95f), // 30m = 95%
    new Keyframe(50f, 0.85f), // 50m = 85%
    new Keyframe(100f, 0.75f) // 100m = 75%
);
```

---

## Testing Falloff

### **Debug Information**

The Projectile script has debug methods:

```csharp
projectile.GetDistanceTraveled() // Returns distance in meters
projectile.GetCurrentDamage()    // Returns damage at current position
```

### **Visual Testing**

1. **Place enemy at different distances** from player
2. **Fire at player**
3. **Check damage in console** or health bar
4. **Adjust falloff curve** until it feels right

### **Quick Test Setup**

1. Place 3 enemies:
   - Close (5m from player)
   - Medium (25m from player)
   - Far (45m from player)
2. Let them all fire at player
3. Observe damage differences
4. Tune configs as needed

---

## Weapon-Specific Recommendations

### **Close-Range Weapons (Shotgun, Melee)**
```
Steep falloff curve
Max effective range: 15m
Damage drops to 10% beyond 30m
```

### **Medium-Range Weapons (Pistol, AR, MG)**
```
Moderate falloff curve
Max effective range: 30m
Damage drops to 50% at 50m
```

### **Long-Range Weapons (Sniper)**
```
Minimal falloff curve
Max effective range: 100m+
Damage stays above 75% at all ranges
```

### **Explosive Weapons (Grenade, Rocket)**
```
No falloff (useDamageFalloff = false)
Use blast radius for damage calculation
Consistent damage within radius
```

---

## Performance Notes

- **Zero overhead** when `useDamageFalloff = false`
- **Minimal cost** when enabled (one distance calculation + curve evaluation)
- **No per-frame cost** (only calculated on impact)
- **Safe for hundreds of projectiles**

---

## Balancing Tips

### **Make Shotguns Feel Right**
- High base damage (60-80)
- Steep falloff (drops to 10% at 30m)
- Forces close-range engagement

### **Make Snipers Feel Powerful**
- Very high base damage (100-120)
- Minimal falloff (stays above 80% at all ranges)
- Rewards accuracy and positioning

### **Make Pistols Versatile**
- Medium base damage (15-25)
- Moderate falloff (50% at 50m)
- Effective at most ranges but not dominant

### **Make Machine Guns Suppressive**
- Medium-high base damage (35-45)
- Moderate falloff (55% at 50m)
- High fire rate compensates for falloff

---

## Common Issues

### **"Shotgun still effective at long range"**
- Increase spreadMultiplier in config
- Steepen falloff curve (drop to 10% faster)
- Reduce pellet count at range

### **"Sniper feels weak at close range"**
- Snipers should maintain damage at all ranges
- Check useDamageFalloff is true but curve is flat
- Consider adding laser telegraph delay as balance

### **"All weapons feel the same"**
- Differentiate falloff curves more
- Shotgun: Steep (10% at 30m)
- Pistol: Moderate (50% at 50m)
- Sniper: Flat (85% at 50m)

---

## Future Enhancements (Not Implemented Yet)

- **Headshot multipliers** (2x damage)
- **Armor penetration** (ignores % of falloff)
- **Critical hits** (random damage boost)
- **Elemental damage** (fire, ice, etc.)
- **Damage over time** (poison, bleed)

---

## Quick Reference

| Enemy Type | Base Damage | Falloff @ 30m | Falloff @ 50m | Use Case |
|------------|-------------|---------------|---------------|----------|
| Pistol     | 20          | 80%           | 50%           | Balanced |
| Shotgun    | 80          | 20%           | 10%           | Close range |
| AR         | 30          | 85%           | 60%           | Medium range |
| MG         | 40          | 80%           | 55%           | Suppression |
| Sniper     | 120         | 95%           | 85%           | Long range |
| Grenadier  | 80          | N/A           | N/A           | Blast radius |
