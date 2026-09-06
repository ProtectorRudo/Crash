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
    if "SetParent(owner:" in text:
        errors.append(f"invalid Transform.SetParent named argument returned: {p.name}")

for name in ("BotBrain.cs", "CarryableCrate.cs", "ArenaFighter.cs"):
    if name in files and "FindObjectsOfType" in files[name].read_text(encoding="utf-8-sig"):
        errors.append(f"scene-wide scan returned to hot path: {name}")

tuning = files.get("SpaceBashTuning.cs")
if tuning:
    t = tuning.read_text(encoding="utf-8-sig")
    required_tuning = (
        "MatchSeconds = 90f", "TntFuse = 3.0f", "SpeedBootsDuration = 8.0f",
        "SlowZDuration = 8.0f", "ShieldDuration = 10.0f", "WeightDuration = 8.0f",
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

bootstrap = files.get("PrototypeBootstrap.cs")
if bootstrap:
    t = bootstrap.read_text(encoding="utf-8-sig")
    for needle in ("Purple Skyline Backdrop", "Future Tower", "Neon Windows"):
        if needle not in t:
            errors.append(f"visual regression: rooftop skyline element missing {needle}")
    if "Distant Planet" in t or 'star.name = "Star"' in t:
        errors.append("visual regression: outer-space backdrop returned")
    if "cam.fieldOfView = 40f" not in t or "Quaternion.Euler(51.8f" not in t:
        errors.append("visual regression: rooftop camera framing changed")
    if t.count(".sharedMaterial") < 4:
        errors.append("mobile regression: skyline stopped sharing repeated materials")

arena = files.get("PrototypeBootstrapArena.cs")
if arena:
    t = arena.read_text(encoding="utf-8-sig")
    for needle in ("Recessed Deck Face", "Deck Hazard Stripe", "Rail Hazard Stripe"):
        if needle not in t:
            errors.append(f"visual regression: metallic arena detail missing {needle}")
    if "shadow.transform.SetParent(parent.parent, false)" not in t:
        errors.append("visual regression: fighter shadow hierarchy changed unexpectedly")

presentation = files.get("FighterPresentation.cs")
if presentation:
    t = presentation.read_text(encoding="utf-8-sig")
    if "UpdateGroundShadow()" not in t or "ArenaWorld.HasSafeFloor(transform.position, 0.82f)" not in t:
        errors.append("visual regression: fighter shadow no longer stays pinned to safe floor")

match = files.get("MatchManager.cs")
if match:
    t = match.read_text(encoding="utf-8-sig")
    if "float[] xs = { left1, left2, right1, right2 }" not in t:
        errors.append("visual regression: four-player HUD is no longer top-aligned")
    if "new Color(1f, 0.52f, 0.08f, 1f)" not in t:
        errors.append("visual regression: central orange timer styling disappeared")

breakable = files.get("BreakableTile.cs")
if breakable:
    t = breakable.read_text(encoding="utf-8-sig")
    if "GetComponentsInChildren<Collider>(true)" not in t or "GetComponentsInChildren<Renderer>(true)" not in t:
        errors.append("visual/runtime regression: detailed tile children can remain after a blast")

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
    for icon in ('kind == PowerupKind.Wumpa ? "+"', 'kind == PowerupKind.SpeedBoots ? ">>"', 'kind == PowerupKind.SlowZap ? "Z"', 'kind == PowerupKind.Weight ? "500"'):
        if icon not in t:
            errors.append(f"visual regression: readable pickup icon missing {icon}")
    if "CreatePowerupIcon" not in t or t.count(".sharedMaterial") < 5:
        errors.append("visual/mobile regression: pickup readability or material sharing disappeared")

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

print(f"AUDIT OK — {len(files)} gameplay scripts checked + Space Bash gameplay/visual fidelity gates")
