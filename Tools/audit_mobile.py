#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

def read(path):
    p = ROOT / path
    if not p.exists():
        errors.append(f"missing file: {path}")
        return ""
    return p.read_text(encoding="utf-8-sig")

project_version = read("ProjectSettings/ProjectVersion.txt")
if "m_EditorVersion: 6000.3.23f1" not in project_version:
    errors.append("Unity toolchain regression: Crash is not pinned to 6000.3.23f1")

manifest = read("Packages/manifest.json")
for needle in (
    '"com.unity.test-framework": "1.6.0"',
    '"com.unity.modules.androidjni": "1.0.0"',
    '"com.unity.modules.audio": "1.0.0"',
    '"com.unity.modules.imgui": "1.0.0"',
    '"com.unity.modules.particlesystem": "1.0.0"',
    '"com.unity.modules.physics": "1.0.0"',
):
    if needle not in manifest:
        errors.append(f"Unity 6 package regression: missing {needle}")
for legacy in ("com.unity.collab-proxy", "com.unity.textmeshpro", "com.unity.timeline"):
    if legacy in manifest:
        errors.append(f"Unity 6 package regression: legacy package returned {legacy}")

bootstrap = read("Assets/BoxBash/Scripts/PrototypeBootstrap.cs")
for needle in (
    "Time.fixedDeltaTime = 1f / 60f",
    "Screen.autorotateToPortrait = false",
    "Screen.autorotateToPortraitUpsideDown = false",
    "Screen.autorotateToLandscapeLeft = true",
    "Screen.autorotateToLandscapeRight = true",
    "Screen.orientation = ScreenOrientation.AutoRotation",
):
    if needle not in bootstrap:
        errors.append(f"mobile runtime regression: missing {needle}")

controls = read("Assets/BoxBash/Scripts/PlayerTouchController.cs")
for needle in (
    "Screen.height / 1080f",
    "dragDeadZonePixels * scale",
    "maxDragPixels * scale",
    "tapMoveCancelPixels * scale",
    "flickMinPixels * scale",
    "flickMaxPixels * scale",
    "heldFor >= 0.045f",
    "contextActionThisPointer = true",
    "!contextActionThisPointer && heldFor",
):
    if needle not in controls:
        errors.append(f"touch regression: missing {needle}")

hud = read("Assets/BoxBash/Scripts/MatchManager.cs")
for needle in (
    "Rect safe = Screen.safeArea",
    "Screen.height / 1080f",
    "safe.xMin",
    "safe.xMax",
    "safe.yMin",
):
    if needle not in hud:
        errors.append(f"safe-area regression: missing {needle}")

builder = read("Assets/BoxBash/Editor/AndroidBuild.cs")
for needle in (
    'PlayerSettings.bundleVersion = "0.7.1"',
    "PlayerSettings.Android.bundleVersionCode = 8",
    "PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation",
    "PlayerSettings.allowedAutorotateToPortrait = false",
    "PlayerSettings.allowedAutorotateToPortraitUpsideDown = false",
    "PlayerSettings.allowedAutorotateToLandscapeLeft = true",
    "PlayerSettings.allowedAutorotateToLandscapeRight = true",
    "BuildPhoneTestApkBatch() => BuildPhoneTestApk()",
    "BuildDevelopmentApkBatch() => BuildDevelopmentApk()",
    "BuildDevelopmentApk() => BuildApk(DevelopmentOutput, BuildOptions.Development)",
    "EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android)",
):
    if needle not in builder:
        errors.append(f"Android build regression: missing {needle}")

if "SetScriptingBackend" in builder or "targetArchitectures" in builder:
    errors.append("first-device build regression: backend/architectures are being forced instead of using the proven PogoDom toolchain approach")

if 'BuildApk(PhoneTestOutput, BuildOptions.Development)' in builder:
    errors.append("Android build regression: phone-test APK became a Development Build")

apk_doc = read("Docs/APK.md")
if "Build Android APK (Phone Test)" not in apk_doc or "BoxBash-Development.apk" not in apk_doc:
    errors.append("documentation regression: APK workflow no longer distinguishes phone test vs development")

if errors:
    print("MOBILE AUDIT FAIL")
    for error in errors:
        print("-", error)
    sys.exit(1)

print("MOBILE AUDIT OK — Unity 6000.3.23f1, PogoDom-style batch build, gesture exclusivity, safe-area HUD and 60 Hz runtime checked")
