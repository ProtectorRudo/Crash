#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "Assets" / "BoxBash" / "Scripts"
REQUIRED = {
    "ArenaFighter.cs", "ArenaWorld.cs", "BotBrain.cs", "BoxBashRuntime.cs",
    "BreakableTile.cs", "CarryableCrate.cs", "CrateDirector.cs", "FallingWeight.cs",
    "FighterPresentation.cs", "GameFeel.cs", "MatchManager.cs", "ArenaPickup.cs",
    "PlayerTouchController.cs", "PrototypeBootstrap.cs", "PrototypeBootstrapArena.cs",
    "PrototypeBootstrapContent.cs", "SpaceBashTuning.cs", "ThrowGuide.cs",
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

tuning = files.get("SpaceBashTuning.cs")
if tuning:
    t = tuning.read_text(encoding="utf-8-sig")
    required_tuning = (
        "MatchSeconds = 90f",
        "TntFuse = 3.0f",
        "SpeedBootsDuration = 8.0f",
        "SlowZDuration = 8.0f",
        "ShieldDuration = 10.0f",
        "WeightDuration = 8.0f",
    )
    for needle in required_tuning:
        if needle not in t:
            errors.append(f"fidelity regression: missing tuning {needle}")

controls = files.get("PlayerTouchController.cs")
if controls:
    t = controls.read_text(encoding="utf-8-sig")
    if "fighter.Jump()" not in t or "fighter.Kick()" not in t or "fighter.ContextAction()" not in t:
        errors.append("fidelity regression: jump/kick/pick-throw control set incomplete")

bootstrap_parts = [files.get(n) for n in ("PrototypeBootstrap.cs", "PrototypeBootstrapArena.cs", "PrototypeBootstrapContent.cs")]
bootstrap_parts = [p for p in bootstrap_parts if p is not None]
if bootstrap_parts:
    t = "\n".join(p.read_text(encoding="utf-8-sig") for p in bootstrap_parts)
    if "AddComponent<ThrowGuide>" in t:
        errors.append("fidelity regression: visible throw guide re-enabled")
    if "AddComponent<AudioListener>" not in t:
        errors.append("runtime regression: generated camera has no AudioListener")

content = files.get("PrototypeBootstrapContent.cs")
if content:
    t = content.read_text(encoding="utf-8-sig")
    if "CrateKind.Gift," in t or "CrateKind.Heavy," in t:
        errors.append("fidelity regression: non-Space-Bash gift/heavy crates are spawned")
    for kind in ("CrateKind.Normal", "CrateKind.TNT", "CrateKind.Nitro"):
        if kind not in t:
            errors.append(f"fidelity regression: missing runtime crate kind {kind}")
    if "PowerupKind.ThrowBoost :" in t:
        errors.append("fidelity regression: throw boost returned to the Space Bash spawn pool")
    if "PowerupKind.SlowZap" not in t or "PowerupKind.Weight" not in t or "PowerupKind.Shield" not in t:
        errors.append("fidelity regression: Space Bash special item pool incomplete")

crate = files.get("CarryableCrate.cs")
if crate:
    t = crate.read_text(encoding="utf-8-sig")
    if "tile.Blast()" not in t:
        errors.append("fidelity regression: explosive crates no longer break floor tiles")
    if "tileBlastRadius = 0.98f" not in t or "tileBlastRadius = 1.55f" not in t:
        errors.append("fidelity regression: TNT/Nitro floor footprints are no longer distinct")
    if "kind != CrateKind.Nitro" not in t:
        errors.append("fidelity regression: Nitro became safely pickable/kickable")
    if "fuseDeadline" not in t or "PrimeTntIfNeeded" not in t or "FuseRemaining" not in t:
        errors.append("fidelity regression: TNT countdown continuity disappeared")
    if 'labelMesh.text = Mathf.CeilToInt(remaining).ToString()' not in t:
        errors.append("fidelity regression: TNT visible countdown disappeared")

pickup = files.get("ArenaPickup.cs")
if pickup:
    t = pickup.read_text(encoding="utf-8-sig")
    if "fighter.ApplySlow(0.52f, SpaceBashTuning.SlowZDuration)" not in t:
        errors.append("fidelity regression: Z no longer slows its collector")
    if "fighter.GiveWeight(SpaceBashTuning.WeightDuration)" not in t:
        errors.append("fidelity regression: weight is no longer a transferable possession")
    if "fighter.ApplyShield(SpaceBashTuning.ShieldDuration)" not in t:
        errors.append("fidelity regression: one-hit shield item missing")

fighter = files.get("ArenaFighter.cs")
if fighter:
    t = fighter.read_text(encoding="utf-8-sig")
    for needle in (
        "shieldCharges = 1", "PassWeightTo", "ReceiveWeight", "DropCrushingWeight",
        "speedBoostUntil = 0f", "NearestKickableOpponent", "opponent.ApplyDamage(8f",
        "bootJumpMultiplier = Time.time < speedBoostUntil ? 1.18f : 1f",
    ):
        if needle not in t:
            errors.append(f"fidelity regression: fighter rule missing {needle}")

bot = files.get("BotBrain.cs")
if bot:
    t = bot.read_text(encoding="utf-8-sig")
    if "fighter.HasWeight" not in t or "ChaseToPassWeight" not in t:
        errors.append("fidelity regression: bots no longer try to pass lethal weight")
    if "fighter.CarriedCrate.FuseRemaining" not in t:
        errors.append("fidelity regression: bots ignore TNT countdown")

manifest = ROOT / "Packages" / "manifest.json"
if manifest.exists():
    mt = manifest.read_text(encoding="utf-8")
    for module in ("com.unity.modules.physics", "com.unity.modules.particlesystem", "com.unity.modules.androidjni"):
        if module not in mt:
            errors.append(f"missing runtime module: {module}")

if errors:
    print("AUDIT FAIL")
    for error in errors:
        print("-", error)
    sys.exit(1)

print(f"AUDIT OK — {len(files)} gameplay scripts checked + Space Bash fidelity gates")
