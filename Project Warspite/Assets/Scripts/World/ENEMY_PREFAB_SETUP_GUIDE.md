# Enemy Prefab Setup Guide

## Quick Start: Creating Your First Enemy

### Step 1: Create the Base GameObject

1. **In Unity Hierarchy:** Right-click → 3D Object → Capsule (or Cylinder)
2. **Rename it:** "Enemy_Pistol" (or whatever enemy type you're making)
3. **Position:** Set Y position to 1 (so it sits on the ground)
4. **Scale:** Adjust to reasonable size (e.g., X=1, Y=2, Z=1 for human-sized)

### Step 2: Add Required Components

Add these components in order (click "Add Component" in Inspector):

#### **1. Health Component** (from Warspite.Core)
- **Max Health:** 100 (adjust per enemy type)
- This handles damage and death

#### **2. EnemyLogic Component** (from Warspite.World)
This is the main enemy script. Configure:

**Configuration:**
- **Config:** Drag one of the EnemyConfig assets from `Assets/Data/EnemyConfigs/`
  - Example: `PistolInfantry.asset` for pistol enemy

**Targeting:**
- **Target:** Leave empty (auto-finds player)
- **Track Target:** ✓ Checked
- **Muzzle Point:** Create a child empty GameObject (see Step 3)
- **Min Spawn Distance:** 1.5

**UI:**
- **Crosshair:** Leave empty for now (optional)

**Sniper Laser (if using Sniper config):**
- **Laser Renderer:** Add LineRenderer component (see Step 4)

### Step 3: Create Muzzle Point (Recommended)

1. **Right-click on enemy GameObject** → Create Empty
2. **Rename:** "MuzzlePoint"
3. **Position:** Move it to where bullets should spawn (front of enemy, gun height)
   - Example: X=0, Y=1.5, Z=0.5 (in front and at chest height)
4. **Drag MuzzlePoint** into the "Muzzle Point" field in EnemyLogic

### Step 4: Add LineRenderer (Sniper Only)

If using Sniper config:
1. **Add Component:** LineRenderer
2. **Settings:**
   - Positions: 2
   - Width: 0.05
   - Material: Default-Line (or create a glowing material)
   - Color: Red
3. **Drag LineRenderer** into "Laser Renderer" field in EnemyLogic

### Step 5: Add Collider (if not already present)

- Capsule/Cylinder should have collider by default
- Make sure it's **NOT** set to Trigger
- Adjust size to fit your enemy model

### Step 6: Add Rigidbody (Optional - for physics)

If you want enemies to be affected by physics:
1. **Add Component:** Rigidbody
2. **Settings:**
   - Mass: 70 (human weight)
   - Drag: 0
   - Angular Drag: 0.05
   - Use Gravity: ✓ Checked
   - Is Kinematic: ☐ Unchecked (unless you want to control movement manually)
   - Constraints: Freeze Rotation X, Y, Z (prevents tipping over)

### Step 7: Set Layer (Important for targeting)

1. **In Inspector:** Set Layer to "Default" or create an "Enemy" layer
2. If you create "Enemy" layer, make sure player projectiles can hit it

### Step 8: Save as Prefab

1. **Drag the enemy GameObject** from Hierarchy into Project window
2. **Save location:** `Assets/Prefabs/Enemies/`
3. **Name it:** "Enemy_Pistol" (or appropriate name)

### Step 9: Test in Scene

1. **Drag prefab** into scene
2. **Press Play**
3. Enemy should:
   - Auto-target player
   - Fire projectiles at configured rate
   - Reload when magazine empty
   - Take damage when hit

---

## Component Checklist

For a complete enemy prefab, you need:

### ✅ **Required Components:**
- [ ] **Transform** (automatic)
- [ ] **Mesh Renderer** (for visual)
- [ ] **Collider** (Capsule/Box/Mesh)
- [ ] **Health** (Warspite.Core)
- [ ] **EnemyLogic** (Warspite.World)
- [ ] **EnemyConfig assigned** in EnemyLogic

### ✅ **Recommended Components:**
- [ ] **Rigidbody** (for physics interactions)
- [ ] **MuzzlePoint** (child Transform for bullet spawn)

### ✅ **Optional Components:**
- [ ] **TurningCrosshair** (visual firing indicator)
- [ ] **LineRenderer** (for Sniper laser telegraph)
- [ ] **NavMeshAgent** (for AI movement - Phase 5)

---

## Enemy Type Specific Setup

### **Pistol Infantry**
```
Config: PistolInfantry.asset
Health: 100
Muzzle Point: Yes (chest height, front)
Special: None
```

### **Shotgun Rusher**
```
Config: ShotgunRusher.asset
Health: 120
Muzzle Point: Yes (chest height, front)
Special: Fires 8 pellets per shot
```

### **Grenadier**
```
Config: Grenadier.asset
Health: 100
Muzzle Point: Yes (shoulder height for grenade throw)
Special: Grenades arc in trajectory
Note: Grenade explosion not implemented yet (Phase 2)
```

### **Assault Rifle Soldier**
```
Config: AssaultRifleSoldier.asset
Health: 100
Muzzle Point: Yes (chest height, front)
Special: 3-round burst fire
```

### **Machine Gunner**
```
Config: MachineGunner.asset
Health: 150 (tougher)
Muzzle Point: Yes (chest height, front)
Special: High fire rate, large magazine
```

### **Sniper**
```
Config: Sniper.asset
Health: 80 (fragile)
Muzzle Point: Yes (head height for scope)
LineRenderer: Required for laser telegraph
Special: 1.5 second laser warning before shot
```

---

## Troubleshooting

### "Enemy doesn't fire"
- ✓ Check EnemyConfig is assigned
- ✓ Check Target is assigned (or player has "Player" tag)
- ✓ Check enemy is not reloading (CurrentAmmo > 0)

### "Projectiles spawn inside enemy"
- ✓ Increase Min Spawn Distance (try 2.0)
- ✓ Create and assign MuzzlePoint in front of enemy

### "Enemy takes no damage"
- ✓ Check Health component is attached
- ✓ Check Collider is not set to Trigger
- ✓ Check projectiles have Projectile.cs script

### "Laser doesn't show (Sniper)"
- ✓ Check LineRenderer is assigned
- ✓ Check config has usesLaserTelegraph = true
- ✓ Check LineRenderer material is visible

### "Friendly fire between enemies"
- This is prevented by default in Projectile.cs
- Enemy projectiles won't damage other enemies
- Player-thrown projectiles (IsCaught=true) will damage enemies

---

## Quick Copy-Paste Setup

For fastest setup, duplicate an existing enemy and just change the Config:

1. **Duplicate prefab** in Project window (Ctrl+D)
2. **Rename** to new enemy type
3. **Open prefab** (double-click)
4. **Change EnemyConfig** in EnemyLogic component
5. **Adjust Health** if needed
6. **Save prefab** (Ctrl+S)

Done! All stats come from the config.

---

## Visual Customization (Optional)

### Change Enemy Color
1. Select enemy prefab
2. In Mesh Renderer → Materials
3. Create new material or modify existing
4. Change Albedo color

### Add Muzzle Flash (Future)
- Will be added in Phase 2
- Particle system at MuzzlePoint

### Add Death Effects (Future)
- Will be added in Phase 2
- Explosion, ragdoll, etc.

---

## Next Steps After Creating Prefab

1. **Test single enemy** in scene
2. **Spawn multiple enemies** of same type
3. **Mix enemy types** for variety
4. **Adjust configs** to tune difficulty
5. **Build demo arena** (Phase 3)

Remember: Updating the EnemyConfig asset will update ALL enemies using that config!
