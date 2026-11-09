"""Compose native sun artwork on Plant Library's daylight lawn, without starting the game."""
import hashlib
import json
from pathlib import Path
import shutil

import UnityPy
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
LIBRARY = ROOT.parent / "plant-library"
GAME = Path("/mnt/FastGaming1/SteamLibrary/steamapps/common/PVZ Replanted")
BUNDLE = GAME / "Replanted_Data/StreamingAssets/aa/StandaloneWindows64/defaultlocalgroup_assets_coinsprites_1567b864fe644461aad8d5ae5eaa9c05.bundle"
ATLAS = BUNDLE.parent / "gameplayatlases_assets_all.bundle"
SUN_ID = 4336683481468266449
SUN_CENTER_ID = 7504776209017734150


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export():
    environment = UnityPy.load(str(BUNDLE), str(ATLAS))
    sun = next(obj.read() for obj in environment.objects
               if obj.type.name == "Sprite" and obj.path_id == SUN_ID)
    if sun.m_Name != "Sun2":
        raise ValueError("Unexpected native sun sprite")
    center = next(obj.read() for obj in environment.objects
                  if obj.type.name == "Sprite" and obj.path_id == SUN_CENTER_ID)
    if center.m_Name != "Sun1":
        raise ValueError("Unexpected native sun center")
    assets = ROOT / "assets"
    assets.mkdir(exist_ok=True)
    shutil.copy2(LIBRARY / "assets/lawn.png", assets / "lawn.png")
    portrait = sun.image.convert("RGBA")
    # Native Sun1 is the round center; Sun2 supplies the surrounding rays.
    # Both native transforms use unit scale and the same center.
    core = center.image.convert("RGBA")
    portrait.alpha_composite(core, ((portrait.width - core.width) // 2, (portrait.height - core.height) // 2))
    portrait.save(assets / "sun.png")
    with Image.open(assets / "lawn.png") as lawn:
        icon = ImageOps.fit(lawn.convert("RGBA"), (512, 512), method=Image.Resampling.LANCZOS)
    icon.putalpha(255)
    foreground = ImageOps.contain(portrait, (440, 440), method=Image.Resampling.LANCZOS)
    icon.alpha_composite(foreground, ((512 - foreground.width) // 2, 512 - foreground.height - 28))
    icon.save(assets / "menu-icon.png")
    proof = {
        "reference": "../plant-library/tools/export_menu_assets.py",
        "bundle": str(BUNDLE), "bundle_sha256": sha256(BUNDLE),
        "atlas": str(ATLAS), "atlas_sha256": sha256(ATLAS),
        "sprite": sun.m_Name, "path_id": SUN_ID,
        "center_sprite": center.m_Name, "center_path_id": SUN_CENTER_ID,
        "lawn_source": str(LIBRARY / "assets/lawn.png"),
        "lawn_provenance": json.loads((LIBRARY / "assets/sources.json").read_text()),
        "icon_size": [512, 512], "foreground_bounds": [440, 440], "foreground_bottom": 28,
        "assets": {name: sha256(assets / name) for name in ("lawn.png", "sun.png", "menu-icon.png")},
        "game_started": False,
    }
    (assets / "sources.json").write_text(json.dumps(proof, indent=2) + "\n")
    print(assets / "menu-icon.png")


if __name__ == "__main__":
    export()
