# BOX BASH — Frankenstein Prototype 0.2

Unity mobile arena prototype focused on **one-hand play, instant readability, short rematch loops and a destructible-floor viral hook**.

## 0.2 gameplay slice

- 1 human player vs 3 autonomous bots.
- One-finger drag movement.
- Double tap contextual action: pick up / throw crate.
- Keyboard fallback in Editor: WASD/arrows + Space.
- Invisible aim assist that softly leads moving targets.
- Visible throw arc while carrying a crate.
- Normal, TNT and Nitro crates.
- Thrown-crate damage and knockback.
- Explosive chain reactions.
- TNT/Nitro permanently destroy arena floor tiles during the round.
- Falling through a hole eliminates the fighter.
- Bots reserve crates so they do not all chase the same pickup.
- Bots evaluate missing floor and live explosive threat before moving.
- Bots have mild target/personality variation to reduce human dog-piling.
- Crate Director continuously repopulates safe floor to avoid dead time.
- 3-2-1 countdown, 75-second round, alive counter and health HUD.
- One-tap instant rematch after victory/defeat.
- Winner celebration.
- Procedural stylized blob characters with separate visual and physics roots.
- Squash/stretch reactions for pickup, throw, hit and elimination.
- Procedural crate accents, particles, screen shake and Android vibration.
- Empty-scene autostart: open the project, create/open an empty scene, press Play.

## Unity

Target editor: **Unity 2022.3.7f1 LTS**.

## First run

1. Open this folder as a Unity project.
2. Open or create an empty scene.
3. Press Play. `BoxBashRuntime` auto-creates the whole prototype.
4. Drag anywhere to move.
5. Double-tap to grab a nearby crate; double-tap again to throw it.
6. After the result screen, tap once for an immediate rematch.

## Core product loop

**move instantly → grab instantly → assisted throw → impact → floor disappears → near-fall tension → winner celebration → one-tap rematch**

The arena changing permanently during every round is the primary short-form-video hook: late-round fights naturally create tiny safe islands, near-falls and last-tile moments.

## Architecture choices made in 0.2

- `ArenaWorld` keeps lightweight registries for fighters/crates/tiles instead of repeated scene-wide searches.
- `FighterPresentation` owns squash/stretch and celebrations separately from the Rigidbody/collider root.
- `CrateDirector` maintains combat density without spawning onto destroyed floor or directly under fighters.
- `ThrowGuide` renders the same assisted trajectory used by the throw logic.
- Explosions use a reusable `OverlapSphereNonAlloc` buffer.

## Next production pass

1. Playtest/tune movement, throw speed, aim-assist cone and match duration on a real Android phone.
2. Add original power-ups: heal, speed burst, shield and heavy throw.
3. Add near-miss / last-safe-tile feedback and stronger endgame presentation.
4. Replace runtime VFX allocation with pooling.
5. Add authored sound/haptic layers.
6. Move from generated prototype geometry to final original character/environment art after the core loop proves fun.

## IP / source policy

No Crash Bandicoot assets, characters, names, music or original game code are included in this package. The implementation is original and uses arena-game mechanical concepts as design reference.

## APK de gameplay
La escena `Assets/Scenes/Main.unity` ya está incluida en Build Settings. Para sacar el primer APK: abrí Unity 2022.3.7f1 y usá **BOX BASH > Build Android APK**. El archivo sale en `Builds/BoxBash-Gameplay.apk`. Ver `Docs/APK.md`.
