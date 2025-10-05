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

- **Momentum-Based Locomotion**: Movement carries inertia. Direction changes are not instant.

- **Collision Consequences**: Hitting walls at high “relative speed” causes bounce/disruption.

- **Projectile Interaction**: In deepest slow (L3), bullets can be caught and thrown back.

---

## Non-Goals

These are *explicitly excluded* from this prototype:

- No visual effects, sound design, or UI polish
- No enemy AI or health systems
- No animations syncing perfectly — snapping is acceptable
- No stamina system yet (infinite slowmo is fine for test clarity)

---

## Tech Stack

- **Engine**: Unity
- **Template**: `Third Person Controller - Basic Locomotion FREE` by Invector
- **Assets Used**: Only this template, plus primitive cubes/spheres

---

## Success Criteria

✅ If the player can:

1. Build up speed, bounce off walls, and *sometimes* stay in control  
2. Slow time to grab a bullet mid-air  
3. Laugh or panic when flying into a wall

…then the concept is viable.

No polish. Only *proof of chaos*.

