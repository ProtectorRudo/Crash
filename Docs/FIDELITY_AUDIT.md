# Space Bash fidelity audit — v0.4

Goal: reproduce the *game feel and rules* of Crash Bash / Crate Crush / Space Bash as closely as practical for a one-hand mobile game while keeping all characters, branding, artwork, audio and final identity original.

## Differences found in v0.3 and corrections made

| Area | v0.3 | v0.4 fidelity pass |
|---|---|---|
| Round duration | 75 s | 90 s |
| Actions | move + pick/throw | move + jump + pick/throw + kick |
| Mobile mapping | drag + double tap | drag + tap jump + double tap pick/throw + short flick kick |
| Throw assistance | visible trajectory + aim assist | invisible aim assist only |
| Crate set | normal / TNT / Nitro | normal / TNT / Nitro / heavy / gift |
| TNT vs Nitro | mostly same rules | TNT is carryable/fused; Nitro is dangerous to touch and not safely pickable |
| Floor damage | same broad blast rule | TNT removes local panel; Nitro has larger cross-panel footprint |
| Powerups | none | heal, speed, throw boost, shield, slow attack, falling weight |
| Missed solid throws | could remain in arena | consumed after their attack window |
| Crate feel | free rigidbodies | heavier arcade obstacles; kick is meaningful |
| Character action | no jump | jump can clear floor gaps |
| Bot priorities | crate / attack / flee | pickups, danger, kickables, crates, attack, jump escape |
| HUD | human HP + alive count | four-player health presentation + status effects + 90 s clock |
| Camera | generic high 3D | tighter fixed 3/4 arena framing |
| Arena art | dark checkerboard | metallic modular space-station deck, low rails, hazard lights, starfield, planet backdrop |
| Characters | four recolored blobs | four original readable silhouettes/archetypes |
| Audio | none | procedural original pickup/throw/hit/explosion/jump/powerup SFX |
| Regression safety | generic static audit | fidelity gates for 90 s, actions, floor destruction, TNT/Nitro distinction, no throw guide |

## Rules protected by CI

- Four-player arena.
- 90-second round.
- Jump, kick and pick/throw all available.
- TNT and Nitro destroy physical floor colliders, leaving holes.
- TNT and Nitro have different floor footprints.
- Nitro cannot be treated as a normal safe pickup.
- Visible throw trajectory is disabled in the actual bootstrap.
- Hot combat/AI paths may not reintroduce scene-wide `FindObjectsOfType` scans.

## Still requires real-device validation

Static checks cannot certify feel. The first Android playtest must validate:

1. movement acceleration and top speed;
2. single-tap jump latency caused by double-tap disambiguation;
3. crate pickup radius;
4. throw speed / lift / invisible aim-assist strength;
5. kick/flick reliability;
6. TNT fuse timing;
7. Nitro danger/readability;
8. floor-hole creation rate across a 90-second match;
9. bot aggression and avoidance of suicidal pathing;
10. camera scale and visibility on a real 19.5:9 Android screen;
11. 60 FPS stability and thermal behavior.

The next tuning pass should be driven by a phone capture, not guesses.
