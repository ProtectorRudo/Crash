# APK de gameplay para teléfono

## Requisitos
- Unity Hub
- Unity 2022.3.7f1
- Android Build Support + Android SDK/NDK + OpenJDK

## La build que hay que probar primero
1. Abrir la carpeta raíz del proyecto con Unity Hub.
2. Esperar la importación/compilación inicial.
3. Abrir `Assets/Scenes/Main.unity` y probar con **Play**.
4. Menú superior: **BOX BASH > Build Android APK (Phone Test)**.
5. Instalar `Builds/BoxBash-Gameplay.apk` en el teléfono.

`Phone Test` NO usa Development Build. Es la build correcta para evaluar FPS, temperatura, respuesta táctil y sensación real del juego.

## Build de diagnóstico
Si necesitamos logs/debug de Unity:

**BOX BASH > Build Android APK (Development)**

Genera `Builds/BoxBash-Development.apk`. No usar su rendimiento como referencia final porque incorpora overhead de desarrollo.

## Configuración automatizada
El builder fija:
- versión 0.7.0 / versionCode 7;
- IL2CPP + ARM64;
- Android min SDK 24;
- APK (no AAB);
- rotación sólo entre Landscape Left / Landscape Right;
- escena `Assets/Scenes/Main.unity`.

El runtime apunta a 60 FPS, física a 60 Hz, un solo touch activo y HUD dentro del `Screen.safeArea`.

## Controles móviles
- Arrastrar: mover.
- Toque: saltar.
- Doble toque: agarrar caja cercana / lanzar si ya llevás una.
- Flick corto y rápido: patear caja o rival cercano.
- Al terminar: un toque para revancha.

La sensibilidad táctil se escala según la altura de pantalla para conservar una sensación parecida entre resoluciones distintas.

## Editor
- WASD / flechas: mover.
- Space: agarrar / lanzar.
- J: saltar.
- K: patear.
