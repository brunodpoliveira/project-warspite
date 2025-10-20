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
- **Audio Pulse**: Middle Mouse Button (rechargeable hyper strike - charges with movement)
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

9. **Audio Pulse (Advanced Melee)**
   - Add `AudioPulse` component to player for rechargeable hyper strike
   - Charges automatically as player moves (faster movement = faster charge)
   - Default: Middle Mouse Button to fire when fully charged
   - Wide cone AOE attack with high damage and knockback
   - Charge meter displayed in DebugHUD (cyan when ready)

10. **Sonic Boom (High-Speed Wake)**
   - Add `SonicBoom` component to player for dangerous speed mechanics
   - Creates persistent trailing wake sphere that follows player at high speed in L3 time dilation
   - Wake continuously damages anything it touches (50 damage/second)
   - If player slows down, wake catches up and damages player
   - **Bleedover period**: Wake persists for 1.5s after dropping below speed threshold (prevents exploit)
   - **Auto-dissipation**: Wake fades and disappears after 3 seconds of existence
   - Visual feedback: Fades to red during bleedover, shrinks as it dissipates
   - Risk/reward: speed = safety, slowing down = danger

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
- **Audio Pulse (Advanced Melee)**: Rechargeable hyper strike that charges with movement
  - Charges automatically as player moves (faster movement = faster charge)
  - Wide cone AOE attack with high damage and knockback
  - Middle Mouse Button to fire when fully charged
  - Charge meter displayed in DebugHUD with color-coded status
  - Encourages aggressive, mobile playstyle
- **Sonic Boom (High-Speed Wake)**: Dangerous trailing wake created at high speeds
  - Activates when moving fast (8+ m/s) in deepest time dilation (L3)
  - Persistent wake sphere follows player, trying to catch up
  - Wake continuously damages anything it touches (50 damage/second in 0.5s ticks)
  - Player must maintain speed or wake catches up and damages them
  - **Bleedover mechanic**: Wake persists 1.5s after speed drops (prevents toggling exploit)
  - **Dissipation system**: Wake auto-fades after 3s max lifetime
  - Visual feedback: Orange → Red (bleedover) → Fades out (dissipation)
  - Semi-transparent sphere shrinks as it dissipates
  - Strategic gameplay: maintain speed, use enemies as obstacles, plan movement carefully
  - Risk/reward: speed = safety, slowing down = danger

### Visual Feedback (Planned)
1. **Wall Walking**
   - When player is in maximum time dilation, player can walk along a wall's surface if they choose to

### Design Philosophy: Push-Forward Combat
The implemented systems create a "push-forward" mentality:
- Health degeneration prevents camping
- Vampire mechanics require close-range engagement with critical enemies
- Melee combat provides immediate damage option
- Audio Pulse rewards constant movement with powerful attacks
- Sonic Boom adds risk/reward to high-speed movement and strategic positioning
- Doomed tagging prevents wasting resources on already-defeated enemies
- Time dilation resource management encourages strategic aggression

---

## Out of Scope (for this prototype)

- Polished UI/UX, audio, VFX
- AI behavior and combat balance
- Save systems, menus, or meta progression
- Networked play

---

## Animation Integration Insights (For Future Iterations)

### Why Third-Party Controllers Fail with Time Dilation
- **Global `Time.timeScale` breaks third-party controllers** that assume normal time flow
- **Root motion animations** become desynchronized when time scales change
- **Built-in animation state machines** don't compensate for time manipulation

### Animation Architecture for Time Dilation Games

#### 1. Separate Animation Time from Game Time
```csharp
// Core principle: Animations run on UNSCALED time
animator.updateMode = AnimatorUpdateMode.UnscaledTime;
```
**Why**: Player animations must stay fluid at real-time speed while world slows down.

#### 2. Manual Animation Speed Control
Instead of relying on `Time.timeScale`:
```csharp
// On player animator
animator.speed = 1.0f; // Always real-time

// On world entities (enemies, props)
animator.speed = currentTimeDilationFactor; // 0.1x to 1.0x
```

#### 3. Velocity-Driven Animation (Not Time-Driven)
```csharp
// Blend tree parameters based on actual movement
float velocityMagnitude = characterController.velocity.magnitude;
animator.SetFloat("Speed", velocityMagnitude);
animator.SetFloat("DirectionX", localVelocity.x);
animator.SetFloat("DirectionZ", localVelocity.z);
```
**Benefit**: Animations automatically match momentum-based locomotion.

### Recommended Animation Setup

#### Player Character
1. **Blend Tree Structure**:
   - Idle (0 velocity)
   - Walk (low velocity)
   - Run (medium velocity)
   - Sprint (high velocity)
   - Directional strafing (based on local velocity X/Z)

2. **Layer Setup**:
   - **Base Layer**: Locomotion blend tree
   - **Upper Body Layer**: Aiming/shooting (additive)
   - **Action Layer**: Melee attacks, catching projectiles (override)

3. **Key Parameters**:
   ```csharp
   animator.SetFloat("VelocityMagnitude", velocity.magnitude);
   animator.SetFloat("StrafeX", localVelocity.x);
   animator.SetFloat("StrafeZ", localVelocity.z);
   animator.SetBool("IsGrounded", isGrounded);
   animator.SetTrigger("Punch"); // For melee
   animator.SetTrigger("Catch"); // For projectile catch
   ```

#### Enemy/Turret Animations
```csharp
// Turrets: Simple rotation + firing animation
animator.speed = TimeDilationController.CurrentTimeFactor;
animator.SetTrigger("Fire");

// Enemies: Full locomotion affected by time
animator.speed = TimeDilationController.CurrentTimeFactor;
```

### Integration with Current Systems

#### MomentumLocomotion + Animation
```csharp
// In MomentumLocomotion.cs (hypothetical addition)
void UpdateAnimator()
{
    if (animator == null) return;
    
    Vector3 localVel = transform.InverseTransformDirection(velocity);
    
    animator.SetFloat("VelocityMagnitude", velocity.magnitude);
    animator.SetFloat("VelocityX", localVel.x);
    animator.SetFloat("VelocityZ", localVel.z);
}
```

#### MeleeCombat + Animation
```csharp
// In MeleeCombat.cs
void PerformPunch()
{
    animator?.SetTrigger("Punch");
    // Existing punch logic...
}
```

#### CatchAndThrow + Animation
```csharp
// Catching
animator?.SetBool("HoldingProjectile", true);
animator?.SetTrigger("Catch");

// Throwing
animator?.SetTrigger("Throw");
animator?.SetBool("HoldingProjectile", false);
```

### Asset Recommendations

#### Free Options
- **Mixamo**: Humanoid animations, auto-rigging
- **Unity Asset Store**: "Basic Motions FREE" by Kevin Iglesias
- **Quaternius**: Low-poly characters with animations

#### Paid (High Quality)
- **Motion Matching**: For ultra-responsive movement
- **Kinematic Character Controller** by Philippe St-Amand (supports custom time)

### Critical Lessons from This Prototype

1. **Keep animation separate from physics**: `CharacterController` velocity should drive animation, not vice versa
2. **Unscaled time for player, scaled for world**: Maintains responsiveness
3. **IK is optional**: For fast-paced games, full-body IK might be overkill
4. **Procedural animation**: Consider procedural head-look, aim offset, or foot IK only if it adds to the fantasy

### Migration Path for Next Iteration

1. Start with capsule + basic animator (like current project)
2. Add simple idle/walk/run blend tree
3. Hook velocity to blend tree parameters
4. Test time dilation with `animator.speed` scaling
5. Add action layers (melee, catch, throw)
6. Polish with IK/procedural if needed

**Key Takeaway**: Momentum-based systems are ideal for animation because velocity naturally drives blend trees. The main challenge is keeping player animations unscaled while world animations scale with time dilation.
