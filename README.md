# Project Warspite

## Prototype Status: **FEATURE COMPLETE / EVALUATION PHASE**

A momentum-based movement prototype with time dilation, vampire mechanics, and high-speed traversal.
Transitioning from **mechanics prototype** to **controlled demo arena phase** — testing stability, readability, and enemy behavior under extreme time manipulation.

### Core Concept
Play as a hyper-speed character who perceives the world in slow motion while maintaining realistic, momentum-based physics. Deeper time dilation unlocks higher-order abilities: bullet catching, wall walking, and sonic booms that reshape the battlefield.

**30-Second Fantasy:** You are a human projectile moving through frozen time. Droplets hang mid-air, debris and sparks twist around you as you sprint along walls, catch bullets, and smash through obstacles in an instant of chaos.

### Time Dilation System

| Tier   | World Speed | Abilities                                     | Description                            |
| ------ | ----------- | --------------------------------------------- | -------------------------------------- |
| **L1** | 0.5×        | Core locomotion & melee                       | Conventional bullet-time baseline      |
| **L2** | 0.2×        | Wall walking, enhanced air control            | Nearly frozen world; precise traversal |
| **L3** | 0.05×       | Bullet catching, sonic boom, debris shockwave | Full spectacle tier                    |

* **Transition:** Instantaneous snap for responsiveness (no interpolation)
* **Visual cues:** Environmental slowdown — droplets, sparks, flying paper, dust
* **Audio cues:** Layered compression and slowed ambient playback
* **Player time:** Always unscaled for consistent input and control
* **World time:** Scaled per tier; all AI and physics update accordingly

### Implemented Systems
- ✅ 3-level time dilation (L1, L2, L3) with resource management
- ✅ Momentum-based locomotion with inertia and wall bounce
- ✅ Bullet catching/throwing (L3 only)
- ✅ Health degeneration + vampire healing on critical enemies
- ✅ Melee combat with doomed system
- ✅ Audio Pulse (movement-charged hyper strike with exponential cost scaling)
- ✅ Sonic Boom (traveling shockwave with enemy chaining and tunnel ID system)
- ✅ Wall Walking (manual surface traversal in L2/L3)
- ✅ Grenade throwing with trajectory preview
- ✅ Turning crosshair (turret timing feedback)
- ✅ Full-physics destruction — debris, props, and obstacles react to sonic booms and collisions
- ✅ Camera occlusion system (semi-transparent objects between camera and player)

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
- **Constant health drain** enforces forward motion (disabled during Second Wind)
- **Vampire kills** restore health and momentum loop
- **Time dilation resource** recharges slowly; enemies and pickups accelerate recovery
- **Aggression and movement** are rewarded; passivity punished
- **Audio Pulse** rewards constant movement with powerful attacks (exponential cost scaling prevents spam)
- **Sonic Boom** adds risk/reward to high-speed movement and strategic positioning (enemy chaining mechanics)
- **Wall Walking** enables creative positioning and flanking in L2/L3 time dilation
- **Doomed tagging** prevents wasting resources on already-defeated enemies
- **Second Wind** provides one chance to recover from terminal damage
- **Systemic goal:** maintain *flow pressure* and kinetic rhythm

### Second Wind System
A one-time mercy mechanic that prevents instant death:

- **Trigger:** When player takes damage that would reduce health to 0 or below
- **Effect:** Health is set to 1 HP instead of dying
- **Duration:** Until player recovers health above 1 HP
- **Health Drain:** Disabled during Second Wind state
- **Frequency:** Only once per game session
- **Strategic Uses:**
  - **Comeback mechanic:** Allows recovery from critical mistakes
  - **Zero-hit runs:** Can be exploited by intentionally triggering at 1 HP for drain-free gameplay
  - **One-hit wonder mode:** Advanced players can maintain 1 HP for entire run (no health drain)
- **Visual Feedback:** Screen effects, audio cues, UI indicator showing Second Wind is active/used
- **Risk/Reward:** Trading safety net for potential drain-free gameplay

---

## Enemy Archetypes

### Prototype Implementation Targets
These enemies will be implemented in the prototype for testing:

#### **Pistol Infantry**
- **Weapon:** Semi-automatic pistol
- **Magazine:** 15 rounds
- **Damage:** Weak (15-20 per hit)
- **Fire Rate:** ~2 rounds/sec
- **Behavior:** Basic patrol, takes cover, medium accuracy
- **Role:** Cannon fodder, teaches basic mechanics

#### **Shotgun Rusher**
- **Weapon:** Pump-action shotgun
- **Magazine:** 7 rounds
- **Damage:** Strong at close range (60-80 per hit, falls off with distance)
- **Fire Rate:** ~1 round/sec (pump delay)
- **Behavior:** Aggressive advance, tries to close distance
- **Role:** Forces player movement, punishes camping

#### **Grenadier**
- **Weapon:** Timed grenades
- **Damage:** Blast + shrapnel (80 blast, 40 shrapnel)
- **Telegraph:** 
  - Blast radius circle visible on ground
  - Timer displayed on grenade (like crosshair)
  - Shrapnel spread pattern shown
- **Counterplay:** Player can throw grenades back at any time dilation level
- **Role:** Area denial, forces repositioning

#### **Assault Rifle Soldier**
- **Weapon:** 3-round burst rifle
- **Magazine:** 30 rounds (10 bursts)
- **Damage:** Medium (25-30 per hit)
- **Fire Rate:** ~2 bursts/sec
- **Behavior:** Tactical positioning, suppressive fire
- **Role:** Mid-range threat, tests player's ability to close gaps

#### **Machine Gunner**
- **Weapon:** Belt-fed machine gun
- **Magazine:** 150 rounds
- **Damage:** High (35-40 per hit)
- **Fire Rate:** ~10 rounds/sec sustained
- **Behavior:** Suppressive fire, area control, slow movement when firing
- **Role:** High-priority target, creates danger zones

#### **Sniper**
- **Weapon:** Single-shot rifle with laser telegraph
- **Magazine:** 1 round (reload after each shot)
- **Damage:** Heavy (100+ per hit)
- **Telegraph:** Red laser pointer shows aim direction 1-2 seconds before firing
- **Behavior:** Stationary or slow-moving, long-range threat
- **Role:** Area denial, encourages use of time dilation to dodge

### Advanced Archetypes (Design Only - Not in Prototype)

#### **Melee Berserker**
- **Weapon:** Melee weapon (blade/club)
- **Damage:** Strong (70-90 per hit)
- **Behavior:** Sprint directly at player, ignores cover
- **Role:** Forces player to engage or evade, tests melee counter-play

#### **Counter-Speedster**
- **Weapon:** Melee + enhanced movement
- **Damage:** Strong (80-100 per hit)
- **Behavior:** Matches player speed partially, can pursue through time dilation
- **Special:** Moves at 0.3× speed even in L3 (vs normal 0.05×)
- **Role:** Anti-camping, forces strategic time dilation usage

#### **Laser Gunner**
- **Weapon:** Continuous beam laser (hitscan)
- **Damage:** Ramps up the longer player is hit (10/sec → 50/sec over 3 seconds)
- **Behavior:** Tracking beam, requires line of sight
- **Role:** Only true hitscan weapon, forces immediate evasion
- **Counterplay:** Break line of sight or use cover

#### **Rocket Launcher**
- **Weapon:** Lock-on missiles
- **Damage:** Thermobaric warhead (see below)
- **Arming Distance:** ~3 meters from launch point
  - Rockets will not detonate if they hit targets within arming distance
  - Creates safe zone for advanced close-quarters counterplay
  - Allows skilled players to rush rocket troops before missiles arm
- **Lock-on System:**
  - Player hears RWR (Radar Warning Receiver) beeping when being tracked
  - Solid tone when locked
  - Visual indicator shows lock status
- **Counterplay (L3 only):** 
  - Advanced players can approach from side/back
  - Detach warhead and throw it back
  - **Cannot** approach from front (proximity fuse)
  - **Rush strategy:** Close distance before rocket arms (~3m)
- **Thermobaric Mechanics:**
  1. **First Stage:** Explosive charge disperses aerosol cloud (gas/liquid/powder)
  2. **Second Stage:** Cloud ignites, creating:
     - Fireball reaching 3000°C
     - Massive shockwave (1.5-2× conventional explosives)
     - Vacuum effect (oxygen sucked in)
  3. **Damage Zones:**
     - **Immediate (0-5m):** Bodies pulverized, instant death
     - **Close (5-15m):** Ruptured organs, severe concussion, blindness
     - **Outer (15-25m):** Ruptured eardrums, severe concussion
- **Role:** Ultimate threat, encourages mastery of L3 mechanics

#### **Flying Drones**
- **Type:** Aerial reconnaissance and attack drones
- **Weapon:** Light machine gun or single rocket
- **Damage:** Medium (20-30 per hit for MG variant)
- **Behavior:** 
  - Flies at medium altitude, circles player
  - Strafing runs with weapons
  - Evasive maneuvers when targeted
- **Mobility:** Full 3D movement, can navigate around obstacles
- **Counterplay:** 
  - Bullet catching works on drone projectiles
  - Can be destroyed with melee/pulse if player reaches them (wall walking)
  - Sonic boom can knock them out of the air
- **Role:** Vertical threat, forces player to look up, tests 3D awareness

#### **Kamikaze Drones**
- **Type:** Explosive suicide drones
- **Weapon:** Self-destruct payload
- **Damage:** High explosive (60-80 in blast radius)
- **Behavior:**
  - Beelines directly toward player at high speed
  - Detonates on contact or proximity
  - Audio warning (high-pitched whine increases as it approaches)
- **Telegraph:**
  - Visible approach trajectory
  - Audio cue intensity scales with distance
  - Red blinking light on drone
- **Counterplay:**
  - Destroy before impact (shoot, melee, pulse)
  - Dodge at last moment (time dilation helps)
  - Catch and redirect in L3 (treat as projectile)
  - Use environment/enemies as shields
- **Role:** High-pressure threat, forces immediate reaction, punishes tunnel vision

#### **Vehicles** (Design Only)
- **Types:** APCs, tanks, helicopters
- **Features:** Multiple weapon hardpoints, armor zones, crew positions
- **Role:** Boss-tier encounters, multi-phase fights

---

## Projectile System

### Accuracy Cone Mechanics
All enemy projectiles use a **cone of fire** system for realistic inaccuracy:

- **Base Cone Angle:** Each weapon has a base spread (e.g., pistol: 2°, rifle: 1°, shotgun: 8°)
- **Distance Scaling:** Cone expands with distance
  - Formula: `actualSpread = baseSpread + (distance * spreadMultiplier)`
  - Example: Pistol at 50m might have 2° + (50 × 0.05°) = 4.5° total spread
- **Random Distribution:** Each shot randomly deviates within the cone
- **Weapon-Specific Modifiers:**
  - **Pistol:** Medium spread, increases moderately with distance
  - **Shotgun:** Wide spread, pellet pattern
  - **Assault Rifle:** Low spread, tight grouping
  - **Machine Gun:** Increases with sustained fire (recoil accumulation)
  - **Sniper:** Minimal spread, laser-telegraphed

### Projectile Types

#### **Ballistic (Standard Bullets)**
- Physical projectiles with travel time
- Affected by gravity (slight drop over distance)
- Can be caught in L3
- Visible tracers in time dilation
- **Damage Falloff:** Damage decreases with distance
  - Formula: `actualDamage = baseDamage × falloffCurve(distance)`
  - Close range (0-10m): 100% damage
  - Medium range (10-30m): 80-100% damage (gradual falloff)
  - Long range (30-50m): 50-80% damage
  - Extreme range (50m+): 30-50% damage
  - Weapon-specific falloff curves (shotgun drops faster, sniper maintains better)

#### **Hitscan (Laser Only)**
- Instant hit, no travel time
- Cannot be caught
- Continuous beam, damage ramps over time
- Requires sustained line of sight

#### **Explosive (Grenades, Rockets)**
- Timed or impact detonation
- Area of effect damage
- Visible telegraph (blast radius, timer)
- Can be thrown back by player

### Visual Feedback
- **Tracers:** Visible bullet trails (enhanced in time dilation)
- **Impact Effects:** Sparks, debris, decals
- **Near-Miss Indicators:** Audio/visual cues when bullets pass close to player
- **Telegraphs:** Laser pointers, grenade timers, lock-on warnings

---

## Environment & Setting

### Roguelite Arena Structure
- **Modular interiors:** Labs, mess halls, offices, barracks, tunnels
- **Each run:** Short chain of arenas (1-2 min each) → recharge node → next area
- **Success metric:** Arenas/loops survived

### Material Rules

| Element         | Behavior                              |
| --------------- | ------------------------------------- |
| **Floors**      | Always walkable                       |
| **Walls**       | Walkable in L2/L3                     |
| **Columns**     | Climbable anchors                     |
| **Ceilings**    | Mostly decorative                     |
| **Decoratives** | Destructible (L3) or auto-vaulted (L1/L2) |
| **Cover**       | Some resist shockwave; others scatter |

**Performance Note:** Large debris fields must be sub-stepped or pooled to avoid instability during snap transitions.

### Resource Economy

Three interconnected resources form the "push-forward triangle":

| Resource | Source | Recharge Mechanism | Purpose |
| -------- | ------ | ------------------ | ------- |
| **Time Dilation Energy** | Finite pool | Very slow passive regen; restored faster via kills or pickups | Controls pacing |
| **Health** | Degenerates over time | Restored via vampire kills | Encourages forward pressure |
| **Audio Pulse Charge** | Movement | Builds with sustained velocity | Rewards traversal mastery |

**Triangle Loop:** Movement fuels power → power fuels aggression → aggression restores survival

---

## Current Prototype Goals

### Immediate Focus
1. **Build static demo arena** — single interior environment for controlled testing
2. **Implement prototype enemies**
3. **Implement projectile cone system** with distance-based spread
4. **Test physics stability** across snap transitions
5. **Evaluate AI tick logic** under scaled time
6. **Performance benchmark:** Target ≥60 fps with debris active

### Implementation Plan (Priority Order)

#### **Phase 1: Enemy System Foundation**
Implement the 6 prototype enemy archetypes:

1. **Pistol Infantry**
   - Semi-automatic pistol (15 rounds)
   - 15-20 damage per hit, ~2 rounds/sec
   - Basic patrol, cover-taking, medium accuracy
   - Role: Cannon fodder, teaches basic mechanics

2. **Shotgun Rusher**
   - Pump-action shotgun (7 rounds)
   - 60-80 damage close range, ~1 round/sec
   - Aggressive advance, closes distance
   - Role: Forces movement, punishes camping

3. **Grenadier**
   - Timed grenades with blast + shrapnel (80/40 damage)
   - Telegraph: blast radius circle, timer display, shrapnel pattern
   - Player can throw grenades back at any time dilation level
   - Role: Area denial, forces repositioning

4. **Assault Rifle Soldier**
   - 3-round burst rifle (30 rounds, 10 bursts)
   - 25-30 damage per hit, ~2 bursts/sec
   - Tactical positioning, suppressive fire
   - Role: Mid-range threat, tests gap-closing ability

5. **Machine Gunner**
   - Belt-fed machine gun (150 rounds)
   - 35-40 damage per hit, ~10 rounds/sec sustained
   - Suppressive fire, area control, slow when firing
   - Role: High-priority target, creates danger zones

6. **Sniper**
   - Single-shot rifle with laser telegraph
   - 100+ damage per hit, 1 round (reload after each shot)
   - Red laser pointer shows aim 1-2 seconds before firing
   - Role: Area denial, encourages time dilation dodging

#### **Phase 2: Projectile Systems**

1. **Accuracy Cone Mechanics**
   - Base cone angle per weapon type (pistol: 2°, rifle: 1°, shotgun: 8°)
   - Distance scaling: `actualSpread = baseSpread + (distance × spreadMultiplier)`
   - Random distribution within cone
   - Weapon-specific modifiers (MG recoil accumulation, sniper minimal spread)

2. **Damage Falloff System**
   - Close range (0-10m): 100% damage
   - Medium range (10-30m): 80-100% damage (gradual falloff)
   - Long range (30-50m): 50-80% damage
   - Extreme range (50m+): 30-50% damage
   - Weapon-specific curves (shotgun drops faster, sniper maintains better)

3. **Grenade Telegraph System**
   - Blast radius circle visible on ground
   - Timer displayed on grenade (like crosshair)
   - Shrapnel spread pattern visualization
   - Throwback mechanic at any time dilation level

#### **Phase 3: Demo Arena Construction**

1. **Static Test Environment**
   - Single enclosed interior space
   - Strategic cover placement (some destructible, some resistant)
   - Enemy spawn points for controlled testing
   - Clear sightlines for sniper positioning
   - Close-quarters areas for shotgun/melee testing

2. **Arena Features**
   - Walkable walls (L2/L3 testing)
   - Climbable columns
   - Destructible props for sonic boom interaction
   - Cover variety (resistant vs. scatter)

#### **Phase 4: Quality of Life Systems**

1. **Second Wind Mechanic**
   - Trigger: Damage that would reduce HP to ≤0
   - Effect: Set HP to 1 instead of death
   - Disable health drain during Second Wind state
   - Visual feedback: Screen effects, UI indicator
   - Audio feedback: Distinct sound cue
   - Frequency: Once per game session
   - Track usage state for UI display

2. **Enemy Feedback Enhancements**
   - Distinct visual feedback under slow motion
   - Hit reactions scaled to time dilation
   - Death animations that work at 0.05× speed
   - Clear critical state indicators (pink highlight)

#### **Phase 5: Testing & Tuning**

1. **Physics Stability**
   - Test all enemies across snap transitions (L1 ↔ L2 ↔ L3)
   - Verify projectile behavior at different time scales
   - Validate sonic boom interactions with enemies
   - Check debris field performance

2. **AI Tick Logic**
   - Ensure AI updates correctly under scaled time
   - Verify pathfinding at different time scales
   - Test enemy reactions to player abilities
   - Validate grenade throwback timing

3. **Performance Benchmark**
   - Target: ≥60 fps with full debris active
   - Test with all 6 enemy types spawned
   - Stress test with multiple sonic booms
   - Profile and optimize bottlenecks

### Short-Term Deliverables
- Stable full-physics destruction at all dilation levels
- Distinct enemy feedback under slow motion
- Consistent control feel through dilation snaps
- Core loop validation: "Enter → Engage → Feed → Exit" within 90 seconds
- All 6 prototype enemies functional and balanced
- Projectile cone and damage falloff systems working
- Second Wind mechanic implemented and tested

---

## Post-Demo Plan

Once demo arena and enemy prototypes are validated:

1. **Clean implementation** of full game:
   - Modular arena stitching system
   - Data-driven time-scale parameters (avoid global `Time.timeScale`)
   - Clear subsystem separation (player physics, AI, time scaling, FX)
2. **Expand to procedural roguelite loop** (arena chaining, upgrade/recharge nodes)
3. **Introduce new environments** and aesthetic layer (audio, VFX, UI polish)

---

## Out of Scope (for this prototype)

- Polished UI/UX, audio, VFX
- Full AI behavior trees and advanced combat balance
- Save systems, menus, or meta progression
- Networked play
- Advanced enemies: Laser Gunner, Rocket Launcher, Flying Drones, Kamikaze Drones, Vehicles

---

## Known Technical Considerations

- **Time scaling:** Global `Time.timeScale` causes double-movement issues; prefer per-system timers
- **Velocity rescaling:** Spikes possible; use damping or gravity scaling instead
- **Animation:** Keep physics authoritative; animation follows velocity
- **Destruction:** Full physics increases unpredictability — add sub-stepped collision limits
- **Input:** Always use unscaled delta for responsiveness in slow motion

---

## Critical Lessons from This Prototype

1. **Separate animation from physics**: `CharacterController` velocity should drive animation, not vice versa
2. **Unscaled time for player, scaled for world**: Maintains responsiveness during time dilation
3. **IK is optional**: For fast-paced games, full-body IK might be overkill
4. **Procedural animation**: Consider procedural head-look, aim offset, or foot IK only if it adds to the fantasy
5. **Snap transitions feel more responsive** than smooth fades; responsiveness beats spectacle
6. **Exponential cost scaling** (Audio Pulse) prevents ability spam while maintaining strategic depth
7. **Enemy chaining mechanics** (Sonic Boom) create tactical decision points: shield vs. chain

---

## Progression and Difficulty Framework

### Percentage-Based Scaling
Difficulty is expressed as a **percentage** rather than categorical levels:

| Parameter | Description |
| --------- | ----------- |
| **Base Difficulty** | 100% — standard baseline |
| **Cap** | 500% — unlocked only after clearing the Initiation Run |
| **Modules** | Each cleared sector offers three random difficulty modifiers; player must pick one before continuing |
| **Scaling** | Each module increases global difficulty; combinations persist until run end |
| **Goal** | Player-directed escalation; difficulty grows by choice, not fiat |

**Module Examples:**
- +20% enemy projectile speed
- +15% damage
- Resource scarcity
- Enemy vampirism
- Unstable physics / debris hazard

Modules are data-driven for easy extension.

---

## Opening, Audio, and Visual Direction

### The "Initiation" — Opening Combat Gauntlet
- **Replaces tutorial:** First playable section introduces player through live combat, not text
- **Calibration:** Completion calibrates starting difficulty and unlocks 100–500% range
- **Replayable as "Danger Room":** Benchmark, build testing, and score challenge mode

### Tutorial Room (Optional)
Separate, optional space:
- No enemies; modular training modules for controls, mechanics, and traversal
- Accessible from main menu

### Audio Direction
**Concept:** Orchestral fragments remixed like a DJ set

- **Influences:** Heitor Villa-Lobos, Prokofiev, Drakengard OST
- **Implementation:**
  - Time dilation manipulates playback: slicing, looping, reversing
  - As dilation deepens, audio fragments degrade and fragment
  - Normal time restores full instrumentation
- **Goal:** Music mirrors the game's manipulation of time and motion

### Visual Direction
**Stylized realism** for clarity and performance:

- **Distinct silhouettes** for all enemies and props
- **Simplified materials:** Limited palette per environment
- **Clear iconography** for states:
  - Critical (pink)
  - Active ability (cyan)
  - Resource (amber)
- **Visual effects:** Emphasize legibility under chaos — visible shockwaves, readable debris, minimal noise
- **Target platform:** Modest PCs; style > fidelity

---

## Narrative and Thematic Justification

### Role of Story
**Minimal narrative.** Exists to justify mechanics, set tone, and anchor art direction.
Comparable approach: F.E.A.R., SUPERHOT, Ruiner.

#### **1. Why Interiors Only**
**Containment Facility or Megastructure:**
- Combat occurs inside a sealed testing complex or collapsing arcology
- Time manipulation is contained to modular sectors
- Supports the procedural, enclosed design

#### **2. Why the Player Has Powers**
**Experimental Subject / Temporal Parasite Hybrid:**
- Bioengineered human fused with a time-manipulating organism or technology
- Health decay represents temporal instability; feeding restores stability
- Explains unscaled movement and vampire mechanics

#### **3. Enemy Factions**
- Automated security drones defending containment
- Rival experiments or failed subjects
- External cleanup forces sent to neutralize all anomalies
- All can coexist; visually distinct archetypes justify gameplay diversity

#### **4. Final Objective**
- Reach and either consume or shut down the temporal core
- Could be escape, transcendence, or endless loop awareness
- Supports cyclical roguelite structure: reaching the end resets time

#### **5. Tonal & Delivery Guidelines**
- **Mood:** Sterile, tragic, rhythmic — "scientific apocalypse"
- **Dialogue:** Minimal; logs or terminals only
- **Environment:** Communicates collapse through architecture and debris
- **Lore Delivery:** Purely environmental; readable but ignorable
- **Story Function:** Tone scaffolding, not progression gating

---

## Development Outlook

**Short term:** Empirical refinement. One arena, a handful of enemies, aggressive telemetry.
**Long term:** Modular roguelite with layered powers, dynamic arenas, and replay-oriented runs — *a super-speed combat sandbox with the readability of a classic FPS and the pacing of an arcade game.*

---

## Repository Use

This document serves as a **living design record** — a single reference for mechanics, structure, style, and rationale.
Future changes (new systems, AI types, modules, or lore) should append directly here or in specialized `.md` subfiles referenced from this one.