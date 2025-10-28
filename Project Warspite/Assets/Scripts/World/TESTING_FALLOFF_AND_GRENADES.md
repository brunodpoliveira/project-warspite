# Testing Damage Falloff & Grenades

## Testing Damage Falloff

### **Enable Debug Logging**

**Option 1: On Projectile Prefab (if you have one)**
1. Select projectile prefab in Project window
2. Find Projectile component
3. Check **"Debug Damage Falloff"** ✓

**Option 2: On Auto-Created Projectiles**
Since projectiles are auto-created, you need to enable logging in code temporarily.

**Quick Test Method:**
1. Place 3 enemies at different distances:
   - **Close:** 5 meters from player
   - **Medium:** 25 meters from player
   - **Far:** 45 meters from player
2. Press Play
3. Watch Console for damage logs

### **What You'll See in Console:**

```
[Projectile] Hit PlayerObject | Distance: 5.2m | Base: 20.0 | Multiplier: 1.00x | Final: 20.0
[Projectile] Hit PlayerObject | Distance: 24.8m | Base: 20.0 | Multiplier: 0.82x | Final: 16.4
[Projectile] Hit PlayerObject | Distance: 44.3m | Base: 20.0 | Multiplier: 0.51x | Final: 10.2
```

### **Expected Results:**

**Pistol Infantry (20 base damage):**
- 5m: ~20 damage (100%)
- 25m: ~16 damage (80%)
- 45m: ~10 damage (50%)

**Shotgun Rusher (80 base damage):**
- 5m: ~80 damage (100%)
- 15m: ~32 damage (40%)
- 30m: ~8 damage (10%)

**Sniper (120 base damage):**
- 5m: ~120 damage (100%)
- 25m: ~114 damage (95%)
- 45m: ~102 damage (85%)

---

## Testing Grenades

### **Setup Grenadier Enemy**

1. **Create or select enemy prefab**
2. **In EnemyLogic component:**
   - Config: **Grenadier.asset**
3. **Place in scene**
4. **Press Play**

### **What You'll See:**

1. **Green sphere projectile** (grenade) instead of cube
2. **Blinking green light** that speeds up as timer runs out
3. **After 3 seconds:** Explosion!
4. **Damage in radius:** Blast + shrapnel damage

### **Enable Grenade Debug Logging:**

**Method 1: In Scene (Temporary)**
1. Press Play
2. Find grenade GameObject in Hierarchy (appears when fired)
3. Select it quickly before it explodes
4. Check **"Debug Explosion"** ✓

**Method 2: In Code (Permanent)**
Edit `Grenade.cs`:
```csharp
[SerializeField] private bool debugExplosion = true; // Change to true
```

### **What You'll See in Console:**

```
[Grenade] Exploded at (10.2, 1.5, 5.3) | Blast: 80 | Shrapnel: 40 | Radius: 5.0m
[Grenade] Hit PlayerObject | Distance: 2.3m | Blast: 68.0 | Shrapnel: 38.4 | Total: 106.4
[Grenade] Hit enemy_pistol | Distance: 4.8m | Blast: 8.0 | Shrapnel: 20.8 | Total: 28.8
```

### **Expected Grenade Behavior:**

**At Center (0m):**
- Blast: 80 damage (100%)
- Shrapnel: 40 damage (100%)
- Total: 120 damage

**At 2.5m (half radius):**
- Blast: 40 damage (50%)
- Shrapnel: 30 damage (75%)
- Total: 70 damage

**At 5m (edge of radius):**
- Blast: 0 damage (0%)
- Shrapnel: 20 damage (50%)
- Total: 20 damage

---

## Quick Test Scenarios

### **Scenario 1: Falloff Verification**

**Setup:**
```
Enemy_Pistol_Close   (5m from player)
Enemy_Pistol_Medium  (25m from player)
Enemy_Pistol_Far     (45m from player)
```

**Expected:**
- Close hits for ~20 damage
- Medium hits for ~16 damage
- Far hits for ~10 damage

**Verify:** Check player health after each hit

---

### **Scenario 2: Grenade Blast Radius**

**Setup:**
```
Grenadier (10m from player)
Player standing still
```

**Expected:**
1. Grenade arcs toward player
2. Lands near player
3. Blinks faster and faster
4. Explodes after 3 seconds
5. Player takes 70-120 damage (depending on distance from center)

**Verify:** Check Console for explosion logs

---

### **Scenario 3: Multiple Enemy Types**

**Setup:**
```
Enemy_Pistol    (20m from player)
Enemy_Shotgun   (10m from player)
Enemy_Sniper    (40m from player)
Enemy_Grenadier (15m from player)
```

**Expected:**
- Pistol: ~17 damage per hit
- Shotgun: ~50 damage per hit (8 pellets × ~6 damage each)
- Sniper: ~110 damage per hit
- Grenadier: ~80 damage per explosion

---

## Troubleshooting

### **"No damage logs appearing"**

**For Projectiles:**
- Check `debugDamageFalloff` is true
- Make sure projectiles are hitting something with Health component
- Check Console isn't filtered (click "Clear" and try again)

**For Grenades:**
- Check `debugExplosion` is true
- Make sure grenade explodes (wait 3 seconds)
- Check enemies/player have Health component

### **"Damage seems wrong"**

**Check:**
1. Enemy config has correct base damage
2. `useDamageFalloff` is enabled (or disabled if testing)
3. Distance is measured correctly (use Console logs)
4. Falloff curve is configured properly

### **"Grenades don't explode"**

**Check:**
1. Grenade has Rigidbody (auto-added)
2. Timer is set (default 3 seconds)
3. Grenade isn't destroyed on impact (it shouldn't be)
4. Time.timeScale is not 0 (game isn't paused)

### **"Grenades explode instantly"**

**Check:**
1. Timer value in config (should be 3.0, not 0.3)
2. Time.time is working correctly
3. Grenade script is properly initialized

---

## Visual Indicators

### **Projectile Damage Falloff:**
- No visual indicator (check Console logs)
- Or add colored tracers (red = full damage, yellow = medium, blue = low)

### **Grenade Timer:**
- **Slow blink:** Just thrown (3s remaining)
- **Medium blink:** Half time (1.5s remaining)
- **Fast blink:** About to explode (<0.5s remaining)
- **Explosion:** Grenade disappears, damage applied

### **Blast Radius:**
- In Scene view: Orange sphere gizmo (5m radius)
- In Game view: No indicator (add VFX later)

---

## Performance Notes

### **Damage Falloff:**
- Zero overhead when disabled
- One distance calculation per hit
- One curve evaluation per hit
- Negligible performance impact

### **Grenades:**
- One OverlapSphere per explosion
- One distance calculation per target
- Runs once per grenade (on explosion)
- Safe for dozens of simultaneous grenades

---

## Next Steps After Testing

1. **Verify falloff works** - Check Console logs match expected values
2. **Tune configs** - Adjust falloff curves if needed
3. **Test grenades** - Verify blast radius and damage
4. **Add VFX** - Explosion particles, muzzle flash, etc.
5. **Add SFX** - Explosion sound, grenade bounce, etc.
6. **Build demo arena** - Phase 3!
