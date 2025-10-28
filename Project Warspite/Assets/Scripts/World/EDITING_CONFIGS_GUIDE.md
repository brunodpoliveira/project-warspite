# Editing Enemy Configs - Quick Guide

## How Config Editing Works

### ✅ **Editing a Config Asset Updates ALL Enemies Using It**

When you edit a ScriptableObject config (like `PistolInfantry.asset`), **every enemy** using that config automatically updates. No need to do anything special!

---

## How to Edit Configs (Affects All Instances)

### **Method 1: Edit in Project Window (Recommended)**

1. **Navigate to:** `Assets/Data/EnemyConfigs/`
2. **Click on a config** (e.g., `PistolInfantry.asset`)
3. **Edit values in Inspector:**
   - Fire Rate: 2 → 5 (all pistol enemies now fire faster)
   - Base Damage: 20 → 30 (all pistol enemies now hit harder)
   - Magazine Size: 15 → 20 (all pistol enemies get more ammo)
4. **Changes apply immediately** to all enemies using that config

### **Method 2: Edit While Enemy Selected**

1. **Select enemy in Hierarchy**
2. **In Inspector**, find "Configuration" section
3. **Click on the config asset** (e.g., "PistolInfantry (Enemy C...)")
4. **Inspector switches to show the config asset**
5. **Edit values** - affects all enemies using this config
6. **Click back on enemy** to return to enemy settings

---

## Understanding the Two Systems

### **Config Editing (Global Changes)**
```
Edit: PistolInfantry.asset → Fire Rate: 5
Result: ALL pistol enemies fire at 5 rounds/sec
Use When: You want to tune the enemy type globally
```

### **Per-Instance Overrides (Local Changes)**
```
Edit: Enemy_Pistol_Boss → Fire Rate Override: 10
Result: ONLY this enemy fires at 10 rounds/sec
Use When: You want one special enemy variant
```

---

## Workflow Examples

### **Scenario 1: Tuning Enemy Types (Use Configs)**

You want all pistol enemies to be more challenging:

1. Open `PistolInfantry.asset`
2. Change Fire Rate: 2 → 4
3. Change Base Damage: 20 → 25
4. **All pistol enemies** in all scenes now use these values

### **Scenario 2: Creating Boss Variant (Use Overrides)**

You want ONE special pistol boss:

1. Place pistol enemy in scene
2. Set Fire Rate Override: 8
3. Set Damage Override: 50
4. Set Magazine Size Override: 30
5. **Only this enemy** uses these values
6. **All other pistol enemies** still use config values

### **Scenario 3: Testing Values (Use Either)**

**Option A: Test with Overrides (Recommended)**
1. Select enemy in scene
2. Set Fire Rate Override: 5
3. Press Play and test
4. If you like it, copy value to config
5. Set Override back to 0

**Option B: Test with Config (Faster)**
1. Open PistolInfantry.asset
2. Change Fire Rate: 5
3. Press Play and test
4. All pistol enemies use new value
5. Adjust until satisfied

---

## Quick Reference Table

| Action | Affects | When to Use |
|--------|---------|-------------|
| Edit Config Asset | ALL enemies using that config | Tuning enemy types globally |
| Set Per-Instance Override | ONLY that enemy instance | Creating special variants |
| Edit Config in Play Mode | ALL enemies (but doesn't save) | Quick testing |
| Edit Override in Play Mode | ONLY that enemy (but doesn't save) | Quick testing |

---

## Important Notes

### **Configs Are Shared**
- One config can be used by 100 enemies
- Editing it updates all 100 instantly
- This is intentional and powerful!

### **Overrides Are Per-Instance**
- Each enemy can have its own overrides
- Overrides don't affect other enemies
- Overrides take priority over config values

### **Testing Workflow**
1. Use **overrides** to test different values
2. Once you find good values, **update the config**
3. Set **overrides back to 0** so they use config
4. Now all enemies of that type use the tuned values

---

## Example: Tuning All Pistol Enemies

### Current State:
```
PistolInfantry.asset:
- Fire Rate: 2
- Damage: 20
- Magazine: 15

10 pistol enemies in scene, all use these values
```

### You Want Them Faster:
```
1. Open PistolInfantry.asset
2. Change Fire Rate: 2 → 4
3. Save (Ctrl+S)
4. Press Play
5. All 10 pistol enemies now fire at 4 rounds/sec
```

### You Want ONE Boss Pistol:
```
1. Select one pistol enemy
2. Set Fire Rate Override: 8
3. Set Damage Override: 40
4. Press Play
5. 9 enemies fire at 4 rounds/sec (config)
6. 1 boss fires at 8 rounds/sec (override)
```

---

## Troubleshooting

### "I edited the config but enemies didn't change"
- ✓ Make sure you saved the config (Ctrl+S)
- ✓ Check enemies are using that config (not a different one)
- ✓ Exit and re-enter Play mode
- ✓ Check no overrides are set (overrides take priority)

### "I want to reset an enemy to config values"
- Set all overrides to 0
- Enemy will now use config values

### "I want to test values without affecting all enemies"
- Use overrides on one enemy
- Test until satisfied
- Copy values to config
- Set overrides back to 0

---

## Best Practices

### **For Prototyping:**
1. Use configs for base values
2. Use overrides to test variations
3. Update configs when you find good values
4. Keep overrides at 0 unless you want special variants

### **For Production:**
1. Configs define enemy archetypes (Pistol, Shotgun, etc.)
2. Overrides create special encounters (Boss, Elite, Weak, etc.)
3. Most enemies use config values (overrides at 0)
4. Only special enemies have overrides

### **For Tuning:**
1. Edit config to tune enemy type globally
2. Test in Play mode
3. Adjust until it feels right
4. All enemies of that type are now tuned
