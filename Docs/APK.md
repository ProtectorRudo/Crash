# Primer APK de gameplay

## Requisitos
- Unity Hub
- Unity 2022.3.7f1
- módulo Android Build Support + Android SDK/NDK + OpenJDK

## Camino corto
1. Abrir la carpeta raíz del proyecto con Unity Hub.
2. Esperar la primera importación/compilación.
3. Abrir `Assets/Scenes/Main.unity`.
4. Probar con **Play**.
5. Menú superior: **BOX BASH > Build Android APK**.
6. El APK queda en `Builds/BoxBash-Gameplay.apk`.

El build de esta vertical slice es Development para priorizar velocidad de prueba. El juego fuerza 60 FPS, orientación horizontal y touch de un dedo.

## Controles
- Arrastrar: mover.
- Doble toque: agarrar caja cercana.
- Doble toque sosteniendo caja: lanzar con asistencia de puntería.
- Al terminar: un toque para revancha.

En editor: WASD/flechas + Space.
