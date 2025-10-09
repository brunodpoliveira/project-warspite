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

## Out of Scope (for this prototype)

- Polished UI/UX, audio, VFX
- AI behavior and combat balance
- Save systems, menus, or meta progression
- Networked play
