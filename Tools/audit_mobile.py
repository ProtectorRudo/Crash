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
    'PlayerSettings.bundleVersion = "0.7.0"',
    "PlayerSettings.Android.bundleVersionCode = 7",
    "PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation",
    "PlayerSettings.allowedAutorotateToPortrait = false",
    "PlayerSettings.allowedAutorotateToPortraitUpsideDown = false",
    "PlayerSettings.allowedAutorotateToLandscapeLeft = true",
    "PlayerSettings.allowedAutorotateToLandscapeRight = true",
    "BuildPhoneTestApk() => BuildApk(PhoneTestOutput, BuildOptions.None)",
    "BuildDevelopmentApk() => BuildApk(DevelopmentOutput, BuildOptions.Development)",
    "ScriptingImplementation.IL2CPP",
    "AndroidArchitecture.ARM64",
):
    if needle not in builder:
        errors.append(f"Android build regression: missing {needle}")

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

print("MOBILE AUDIT OK — resolution-scaled touch, gesture exclusivity, safe-area HUD, 60 Hz physics and Android builders checked")
