# BOX BASH — Space Bash fidelity prototype v0.4

Unity mobile arena prototype aimed at the **Crate Crush / Space Bash game feel**: four fighters, short arcade rounds, crates as weapons, TNT/Nitro, disappearing floor, powerups, last-player-standing tension and a fixed readable arena camera — with completely original characters, branding, art and generated audio.

## v0.4 gameplay

- 1 human vs 3 autonomous bots.
- 90-second rounds.
- Drag anywhere to move.
- Single tap to jump.
- Double tap to pick up / throw.
- Short directional flick to kick.
- Editor controls: WASD/arrows, Space pick/throw, J jump, K kick.
- Invisible mobile aim assist; no visible trajectory line in gameplay.
- Normal, TNT, Nitro, heavy and gift crates.
- TNT is carryable, fused and opens a permanent hole in the floor panel it blasts.
- Nitro is dangerous to touch, is not a safe pickup and opens a larger floor footprint.
- Explosive chain reactions.
- Physical floor colliders disappear after blasts; falling through a hole eliminates the fighter.
- Powerups: heal, speed, throw boost, shield, slow attack and 500-weight attack.
- Jumping can clear damaged-floor gaps.
- Four-player health HUD and status effects.
- Fixed 3/4 space-station presentation with starfield/hazard lighting.
- Four original procedural character silhouettes.
- Procedurally generated original SFX, VFX, shake and Android haptics.
- Bots evaluate holes, explosives, pickups and kickable crates.
- One-tap rematch.

## Unity

Target editor: **Unity 2022.3.7f1 LTS**.

## First run

1. Open the repository as a Unity project.
2. Open `Assets/Scenes/Main.unity`.
3. Press Play. The arena builds itself at runtime.

## Android APK

Use **BOX BASH > Build Android APK**. The development APK is generated at:

`Builds/BoxBash-Gameplay.apk`

The builder selects Android, IL2CPP, ARM64, landscape orientation and min SDK 24.

## Fidelity audit

See `Docs/FIDELITY_AUDIT.md`. The repository static audit includes explicit fidelity gates so future work cannot silently remove 90-second rounds, jump/kick, floor destruction or the TNT/Nitro distinction.

## IP policy

No Crash Bandicoot characters, logos, music, sounds, textures or original retail-game source/assets are included. The goal is functional/game-feel fidelity with an original commercial identity.
