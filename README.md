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

---

## Controls

- **Time Dilation**: `Q` (slower) / `E` (faster) - 3 levels: L1, L2, L3
- **Move**: `WASD`
- **Look**: Mouse
- **Melee**: Left Mouse Button
- **Audio Pulse**: Middle Mouse Button (charges with movement)
- **Vampire Suck**: `F` (on critical enemies - pink pulsing indicator)
- **Wall Walking**: `Enter` (L2/L3 only - press near walls to activate/deactivate)
- **Catch Bullet**: Hold Right Mouse Button (L3 only, near bullet)
- **Throw**: Release RMB while holding projectile

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
- Wall Walking enables creative positioning and flanking in L2/L3 time dilation
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