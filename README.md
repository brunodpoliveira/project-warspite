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
- **Catch (L3 only)**: Hold Right Mouse Button near a bullet
- **Throw**: Release RMB or press `F`
- **Restart**: `R`

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

### Visual Feedback (Planned)
1. **Turning Crosshair**: After turret fires, crosshair spins; when locked, turret fires again
2. **Trajectory Indicators**: 
   - Blast radius visualization for grenades
   - Ray visualization for bullets (shows trajectory before firing)
3. **Sonic Boom**: 
   - Stopping at max speed in slowest time dilation creates sonic boom
   - Damages player unless enemy is between player and boom origin
   - Can be used strategically to defeat enemies
   - Requires visual feedback (shockwave effect)
4. **Melee Combat**:
   - Rechargeable "Audio Pulse" hyper strike
   - Charges with movement (faster movement = faster recharge)
5. **Vampire Mechanics**:
   - Health degenerates over time (encourages aggression)
   - "Suck" enemies at critical health to restore health
   - Gibbing enemies gives less health/blood back
   - Encourages precision over brute force
6. **Doomed Enemy Tagging**:
   - When player throws projectile/object that will eventually destroy enemy
   - Enemy is visually tagged (outline, skull icon, color change, etc.)
   - Prevents player from wasting time/resources on already-doomed enemies
   - Applies to: thrown bullets, physics objects, sonic boom victims
   - Tag persists until enemy is destroyed

### Design Philosophy: Push-Forward Combat
Systems 4 and 5 create a "push-forward" mentality:
- Health degeneration prevents camping
- Movement-based recharge rewards aggression
- Vampire mechanics require close-range engagement
- Sonic boom can be weaponized with positioning

---

## Out of Scope (for this prototype)

- Polished UI/UX, audio, VFX
- AI behavior and combat balance
- Save systems, menus, or meta progression
- Networked play
