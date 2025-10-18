# project-warspite

# WARSPITE — Movement & Time Dilation Prototype

## Purpose

This prototype exists solely to **validate the core fantasy** of playing as a grounded super-speed character who perceives time in slow motion while maintaining **momentum-based physical vulnerability**.

We are **not** building a full combat system or polished experience.  
We only want to answer one question:

> *"Is improvisational chaos fun when movement is inertia-driven and time dilation is under the player's control?"*

---
## Core Mechanics to Validate

- **Time States**: Player can toggle between 4 time dilation levels  
  `Normal → Slow (L1) → Deep Slow (L2) → Near-Freeze (L3)`

- **Player stays real-time**: Player movement and camera responsiveness remain constant across time levels.

- **World slows**: Projectiles, spawners, AI, and physics props slow according to the selected time level.

- **Momentum-Based Locomotion**: Movement carries inertia. Direction changes are not instant.

- **Collision Consequences**: Hitting walls at high relative speed causes bounce/disruption.

- **Projectile Interaction**: In deepest slow (L3), bullets can be caught and thrown back.

---

## Controls (current test mapping)

- **Time**: `Q` (slower) / `E` (faster)
- **Move**: `WASD`
- **Mouse**: Look
- **Catch (L3 only)**: Hold Right Mouse Button near a bullet (must be in Near-Freeze L3!)
- **Throw**: Release RMB while holding projectile
- **Punch**: Left Mouse Button (melee attack)
- **Suck**: `F` (vampire mechanic on critical enemies - look for pink pulsing indicator)
- **Restart**: `R`

**Note**: Catch only works when time shows "Near-Freeze L3" - press Q twice to reach it!

---

## Minimal Scene Setup (for a clean test)

1. **Time Controller**  
   - Add `TimeDilationController` to an empty GameObject (e.g., `GameSystems/`).  
   - Leave Input Action fields empty to use fallback keys `Q/E` and `R`.

2. **Debug HUD (optional but useful)**  
   - Add `DebugHUD` to any always-active object (camera or `GameSystems/`).  
   - Optional references: `timeController` (drag the `TimeDilationController`), `momentum` (player's `MomentumLocomotion`).

3. **Projectile Source for Visual Validation**  
   - Add `SimpleTurret` (drag-and-drop) to any object (a cylinder works).  
   - Leave `Projectile Prefab` empty to auto-use a runtime cube projectile.  
   - Tune: `interval`, `muzzleSpeed`, `burstCount`, `spreadAngle`.

4. **Health Bars (optional)**
   - Add `AutoHealthBar` component to turrets/enemies to automatically create world-space health bars.
   - Health bars fade when full and show color-coded health status (green/yellow/red).

5. **Melee Combat**
   - Add `MeleeCombat` component to player for punch attacks.
   - Default: Left Mouse Button to punch, configurable range and damage.

6. **Doomed Enemy Tagging & Critical Status**
   - Add `DoomedTag` component to turrets/enemies to enable visual feedback.
   - **Orange glow**: Enemy will be destroyed by incoming projectile/melee attack
   - **Pink pulsing sphere**: Enemy is at critical health and can be drained for HP
   - Automatically updates based on health percentage

7. **Trajectory Visualization**
   - Automatically created by `CatchAndThrow` component when holding projectiles
   - Shows predicted path with impact point marker
   - Helps plan throws in slow-motion

8. **Turning Crosshair**
   - Add `TurningCrosshair` component to turrets for firing cadence feedback
   - Automatically syncs with turret firing intervals
   - Color-coded visual feedback for timing

---

## Time Dilation Implementation Options 

- Global TimeScale + Player Compensator
  - Set `Time.timeScale` and `Time.fixedDeltaTime` to slow everything.  
  - Add a `PlayerTimeCompensator` to keep the player moving in real-time by adding missing displacement after normal movement.  
  - Pros: Simple world slowdown; minimal world code.  
  - Cons: Can be brittle with third-party controllers and root motion.

---

## Success Criteria

- **Player Speed Consistency**: Player `CharacterController.velocity` magnitude remains roughly constant across time levels while holding forward.
- **Camera Consistency**: Look responsiveness feels the same in deep slow and normal time.
- **World Slowdown**: Turret shot cadence and projectile motion clearly slow with lower time levels.
- **Interactions**: Catch/Throw in deepest slow remains functional and readable.
- **Stability**: No tunneling or large pops when switching time levels.

---

## Known Pitfalls (from testing)

- Global `Time.timeScale` affects third-party character controllers; compensation must run late and avoid double movement.  
- Velocity-based rescaling on rigidbodies can cause spikes; prefer damping/gravity scaling or per-world timers.  
- Input must be read with real-time deltas if using global slowmo (to avoid sluggish feel).

---

## Vertical Slice - Additional Systems

### Implemented
- **Time Dilation Bar**: Fills linearly over time, drains when time dilation active (faster drain at deeper slow levels)
- **Player Health**: Degenerates over time, recharged by "sucking" enemies at critical health
- **Turret Health**: Turrets can be destroyed by thrown projectiles
- **Health Bar HUD**: World-space health bars for turrets and enemies that fade when full/dead
- **Melee Combat**: Simple punch system (Left Mouse Button) with range detection, damage, and knockback
  - Marks enemies as doomed when dealing lethal damage
- **Doomed Enemy Tagging**: Enemies marked with visual feedback (orange glow) when targeted by lethal attacks
  - Works with thrown projectiles and melee attacks
  - Prevents wasting resources on already-doomed enemies
- **Critical Status Indicators**: Enemies show pulsing pink indicator when at critical health (drainable)
  - Helps players identify which enemies can be drained for HP
- **Trajectory Indicators**: Ray visualization showing projectile path when holding caught projectiles
  - Shows predicted impact point
  - Helps plan throws in slow-motion
- **Turning Crosshair**: Visual turret firing cadence feedback
  - Spins while charging, locks when ready to fire
  - Color-coded: Red (just fired) → Yellow (charging) → Green (ready)

### Visual Feedback (Planned)
1. **Sonic Boom**: 
   - moving at max speed in slowest time dilation creates sonic boom "wake" behind player
   - Damages player when player stops moving unless enemy is between player and boom wake or the player decelerates gently
   - Can be used strategically to defeat enemies
   - Requires visual feedback (shockwave effect)
4. **Advanced Melee Combat**:
   - Rechargeable "Audio Pulse" hyper strike
   - Charges with movement (faster movement = faster recharge)
   - Could enhance existing punch system
5. **Wall Walking**
   - When player is in maximum time dilation, player can walk along a wall's surface if they choose to

### Design Philosophy: Push-Forward Combat
The implemented systems create a "push-forward" mentality:
- Health degeneration prevents camping
- Vampire mechanics require close-range engagement with critical enemies
- Melee combat provides immediate damage option
- Doomed tagging prevents wasting resources on already-defeated enemies
- Time dilation resource management encourages strategic aggression

---

## Out of Scope (for this prototype)

- Polished UI/UX, audio, VFX
- AI behavior and combat balance
- Save systems, menus, or meta progression
- Networked play
