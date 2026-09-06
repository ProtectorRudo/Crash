# APK de gameplay para teléfono

## Toolchain probado
Crash queda alineado con el mismo editor ya usado para PogoDom/CIVIDOM:
- Unity `6000.3.23f1`
- revisión `09d2ecc7fb28`
- Android Build Support + Android SDK/NDK + OpenJDK instalados desde Unity Hub

## Primer APK: Development truth gate
Para la primera prueba real seguimos el patrón probado de PogoDom: Development APK y build batch desde CMD.

Salida esperada:
`Builds/BoxBash-Development.apk`

Comando batch:
`Unity.exe -batchmode -nographics -force-d3d11 -quit -projectPath <repo> -executeMethod BoxBash.EditorTools.AndroidBuild.BuildDevelopmentApkBatch -logFile <log>`

El builder configura automáticamente:
- versión `0.7.1` / versionCode `8`;
- Android min SDK 24;
- APK, no AAB;
- rotación sólo Landscape Left / Landscape Right;
- escena `Assets/Scenes/Main.unity`;
- identificador `com.protectorrudo.boxbash`.

Para este primer gate no se fuerza scripting backend ni arquitectura desde código: se deja que el toolchain Android instalado por Unity Hub use su combinación compatible, igual que en PogoDom. Cuando el juego ya corra en un teléfono endurecemos backend/arquitecturas de release.

## APK de rendimiento
Después de validar que la Development APK instala y juega correctamente:

**BOX BASH > Build Android APK (Phone Test)**

Genera `Builds/BoxBash-Gameplay.apk` sin `Development Build`, apropiada para evaluar FPS, temperatura y frame pacing reales.

## Controles móviles
- Arrastrar: mover.
- Toque: saltar.
- Doble toque: agarrar caja cercana / lanzar si ya llevás una.
- Flick corto y rápido: patear caja o rival cercano.
- Al terminar: un toque para revancha.

La sensibilidad táctil se escala según la altura de pantalla. El runtime apunta a 60 FPS, física a 60 Hz y HUD dentro de `Screen.safeArea`.

## Editor
- WASD / flechas: mover.
- Space: agarrar / lanzar.
- J: saltar.
- K: patear.
