# Project Warspite

## Prototype Status: **FEATURE COMPLETE**

A momentum-based movement prototype with time dilation, vampire mechanics, and high-speed traversal.

### Core Concept
Play as a super-speed character who perceives time in slow motion while maintaining momentum-based physics. The deeper the time dilation, the more abilities unlock (bullet catching, wall walking, sonic boom).

### Implemented Systems
- ✅ 3-level time dilation (L1, L2, L3) with resource management
- ✅ Momentum-based locomotion with inertia and wall bounce
- ✅ Bullet catching/throwing (L3 only)
- ✅ Health degeneration + vampire healing on critical enemies
- ✅ Melee combat with doomed system
- ✅ Audio Pulse (movement-charged hyper strike)
- ✅ Sonic Boom (traveling shockwave on high-speed trails)
- ✅ Wall Walking (manual surface traversal in L3)
- ✅ Grenade throwing with trajectory preview
- ✅ Turning crosshair (turret timing feedback)

### Known Issue: L3 Movement Lag
Movement in L3 time dilation feels laggy/stuttery. This affects all movement including wall walking. **Priority for next iteration: smooth out L3 locomotion.**

---

## Controls

- **Time Dilation**: `Q` (slower) / `E` (faster) - 3 levels: L1, L2, L3
- **Move**: `WASD`
- **Look**: Mouse
- **Melee**: Left Mouse Button
- **Audio Pulse**: Middle Mouse Button (charges with movement)
- **Vampire Suck**: `F` (on critical enemies - pink pulsing indicator)
- **Wall Walking**: `Enter` (L3 only - press near walls to activate/deactivate)
- **Catch Bullet**: Hold Right Mouse Button (L3 only, near bullet)
- **Throw**: Release RMB while holding projectile
- **Restart**: `R`

---

## Known Pitfalls (from testing)

- Global `Time.timeScale` affects third-party character controllers; compensation must run late and avoid double movement.  
- Velocity-based rescaling on rigidbodies can cause spikes; prefer damping/gravity scaling or per-world timers.  
- Input must be read with real-time deltas if using global slowmo (to avoid sluggish feel).

---

### Design Philosophy: Push-Forward Combat
The implemented systems create a "push-forward" mentality:
- Health degeneration prevents camping
- Vampire mechanics require close-range engagement with critical enemies
- Melee combat provides immediate damage option
- Audio Pulse rewards constant movement with powerful attacks
- Sonic Boom adds risk/reward to high-speed movement and strategic positioning
- Wall Walking enables creative positioning and flanking in L3 time dilation
- Doomed tagging prevents wasting resources on already-defeated enemies
- Time dilation resource management encourages strategic aggression

---

## Out of Scope (for this prototype)

- Polished UI/UX, audio, VFX
- AI behavior and combat balance
- Save systems, menus, or meta progression
- Networked play

---

## Critical Lessons from This Prototype

1. **Keep animation separate from physics**: `CharacterController` velocity should drive animation, not vice versa
2. **Unscaled time for player, scaled for world**: Maintains responsiveness
3. **IK is optional**: For fast-paced games, full-body IK might be overkill
4. **Procedural animation**: Consider procedural head-look, aim offset, or foot IK only if it adds to the fantasy

---

## Next Steps: Fixing L3 Movement Lag

### The Problem
Movement in L3 (deepest time dilation) feels laggy and stuttery. This affects:
- Basic WASD movement
- Wall walking transitions
- Overall responsiveness

### Likely Causes
1. **Time.timeScale conflicts**: Global time scaling may interfere with player movement
2. **FixedUpdate timing**: Physics updates may be out of sync at extreme time scales
3. **Input sampling**: Input may be polled at wrong delta time
4. **Animation sync**: If animations are added, they need unscaled time for player

### Recommended Fixes (Priority Order)

#### 1. Use Unscaled Delta Time for Player Movement
```csharp
// In MomentumLocomotion.cs and WallWalking.cs
// Replace Time.deltaTime with Time.unscaledDeltaTime for player-specific calculations
float deltaTime = Time.unscaledDeltaTime;
```

#### 2. Separate Player Physics from World Physics
```csharp
// Option A: Move player in Update() instead of FixedUpdate()
// Option B: Use CharacterController.Move() which bypasses physics time scaling
```

#### 3. Input Smoothing
```csharp
// Smooth input over multiple frames to reduce stutter
Vector3 smoothedInput = Vector3.Lerp(lastInput, currentInput, smoothingFactor);
```

#### 4. Animation Considerations (If Added Later)
```csharp
// Player animator must use unscaled time
animator.updateMode = AnimatorUpdateMode.UnscaledTime;
animator.speed = 1.0f; // Always real-time

// World entities use scaled time
worldAnimator.speed = currentTimeDilationFactor; // 0.1x to 1.0x
```

### Testing Checklist
- [ ] Player movement feels smooth in L3
- [ ] Wall walking transitions are responsive
- [ ] Input feels immediate, not delayed
- [ ] Camera rotation is smooth
- [ ] No stuttering when changing time levels
