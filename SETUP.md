# WARSPITE - Scene Setup Guide

## Quick Setup (5 minutes)

### 1. GameSystems Object
Create empty GameObject named `GameSystems`

**Add components:**
- `TimeDilationController` (leave defaults)
- `DebugHUD`
  - Will auto-find references at runtime if left empty

---

### 2. Player Object
Create empty GameObject named `Player` at position (0, 1, 0)

**Add components:**
- `CharacterController`
  - Height: 2
  - Radius: 0.5
  - Center: (0, 1, 0)
- `MomentumLocomotion` (leave defaults)
- `PlayerTimeCompensator` (no settings needed)
- `CatchAndThrow`
  - Leave `handAnchor` empty (auto-creates)

**Tag the Player:**
- Set GameObject tag to "Player" (create if needed)

---

### 3. Camera Setup
Find the **Main Camera** in the scene

**Option A: Use existing camera**
- Remove or disable any Invector camera scripts
- Add `ThirdPersonOrbitCamera`
  - Drag `Player` into the `Target` field
  - Distance: 5
  - Height: 2
  - Mouse Sensitivity: 3

**Option B: Create new camera**
- Create new Camera GameObject
- Tag as "MainCamera"
- Add `ThirdPersonOrbitCamera` with Player as target

---

### 4. Test Turret
Create a **Cube** GameObject (or cylinder) named `Turret`

**Transform:**
- Position: (5, 1, 5) - or anywhere in view
- Scale: (1, 2, 1) to make it visible

**Add component:**
- `SimpleTurret`
  - Leave `Projectile Prefab` **EMPTY** (auto-creates cubes)
  - Interval: 1.0
  - Muzzle Speed: 20
  - Burst Count: 1
  - Track Target: ✓ (checked)
  - Drag `Player` into `Target` field

---

### 5. Link DebugHUD (Optional but Recommended)
Go back to `GameSystems` → `DebugHUD` component

**Drag references:**
- `Time Controller`: Drag `GameSystems` (itself)
- `Momentum`: Drag `Player`

This displays time level and velocity in the HUD.

---

## Controls

| Key | Action |
|-----|--------|
| `WASD` | Move (momentum-based) |
| `Mouse` | Look around |
| `Q` | Slower time |
| `E` | Faster time |
| `RMB` (Hold) | Catch bullet (L3 only) |
| `RMB` (Release) or `F` | Throw bullet |
| `ESC` | Toggle cursor lock |
| `F1` | Toggle debug HUD |

---

## What to Test

### Success Criteria from README:

1. **Player Speed Consistency**
   - Hold W and watch the HUD velocity
   - Press Q to enter slow-motion
   - Player speed should stay ~10 m/s regardless of time level

2. **Camera Consistency**  
   - Mouse look should feel identical in all time levels
   - No sluggishness in deep slow

3. **World Slowdown**  
   - Turret shoots slower as you press Q
   - Projectiles move slower
   - Should be obvious visual difference

4. **Catch/Throw (L3)**  
   - Press Q three times to reach Near-Freeze
   - Hold RMB near a bullet to catch it
   - Release RMB to throw it back
   - Should feel readable and functional

5. **Stability**  
   - Switch time levels rapidly (Q/E spam)
   - Move while switching
   - Should be smooth, no pops or tunneling

---

## Troubleshooting

**Player falls through floor:**
- Check CharacterController is enabled
- Ensure ground has a collider
- Check Player position starts at Y > 0

**Camera not following:**
- Verify Main Camera has `ThirdPersonOrbitCamera` script
- Check Target field is assigned to Player
- Press ESC if cursor is unlocked

**Turret not shooting:**
- Check SimpleTurret component is enabled
- Projectile Prefab should be EMPTY for auto-cubes
- Interval = 1 means one shot per second (at normal time)

**Time dilation not working:**
- GameSystems should have `TimeDilationController`
- Press Q/E (not held, tap to change levels)
- Check console for errors

**HUD not showing anything:**
- Press F1 to toggle HUD
- Drag references to DebugHUD component
- Check GameSystems is active

---

## Next Steps

Once validated:
- Adjust speeds in `MomentumLocomotion` (maxSpeed, acceleration)
- Tune time levels in `TimeDilationController` (slowLevel1, 2, 3)
- Add more turrets at different angles
- Build actual environment geometry
- Polish camera feel (distance, smoothing)

**Remember:** This is about validating the fantasy, not building final gameplay.
