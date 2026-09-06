#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "Assets" / "BoxBash" / "Scripts"
REQUIRED = {
    "ArenaFighter.cs", "ArenaWorld.cs", "BotBrain.cs", "BoxBashRuntime.cs",
    "BreakableTile.cs", "CarryableCrate.cs", "CrateDirector.cs",
    "FighterPresentation.cs", "GameFeel.cs", "MatchManager.cs",
    "PlayerTouchController.cs", "PrototypeBootstrap.cs", "ThrowGuide.cs",
}

errors = []
files = {p.name: p for p in SCRIPTS.glob("*.cs")}
missing = REQUIRED - files.keys()
if missing:
    errors.append(f"missing scripts: {sorted(missing)}")

for p in files.values():
    text = p.read_text(encoding="utf-8-sig")
    stripped = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    stripped = re.sub(r"//.*", "", stripped)
    stripped = re.sub(r'@"(?:""|[^"])*"', '""', stripped)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)
    if stripped.count("{") != stripped.count("}"):
        errors.append(f"brace mismatch: {p.name}")
    if stripped.count("(") != stripped.count(")"):
        errors.append(f"paren mismatch: {p.name}")

for name in ("BotBrain.cs", "CarryableCrate.cs", "ArenaFighter.cs"):
    if name in files and "FindObjectsOfType" in files[name].read_text(encoding="utf-8-sig"):
        errors.append(f"scene-wide scan returned to hot path: {name}")

if errors:
    print("AUDIT FAIL")
    for error in errors:
        print("-", error)
    sys.exit(1)

print(f"AUDIT OK — {len(files)} gameplay scripts checked")
