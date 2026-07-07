"""Build a multi-size .ico from the UODreams logo asset."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "src" / "ClassicUO.Launcher.Custom" / "Resources"
SOURCE = RES / "uodreams-logo-source.png"
ICON = RES / "uodreams.ico"
ICON_PNG = RES / "uodreams_icon.png"
MASTER_SIZE = 256
ICON_FILL_RATIO = 0.92


def prepare_logo(source: Path) -> Image.Image:
    logo = Image.open(source).convert("RGBA")
    width, height = logo.size
    side = max(width, height)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    offset = ((side - width) // 2, (side - height) // 2)
    canvas.paste(logo, offset, logo)
    return canvas


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"Source logo not found: {SOURCE}")

    square = prepare_logo(SOURCE)

    fitted_size = int(MASTER_SIZE * ICON_FILL_RATIO)
    canvas = Image.new("RGBA", (MASTER_SIZE, MASTER_SIZE), (0, 0, 0, 0))
    fitted = square.resize((fitted_size, fitted_size), Image.Resampling.LANCZOS)
    offset = ((MASTER_SIZE - fitted_size) // 2, (MASTER_SIZE - fitted_size) // 2)
    canvas.paste(fitted, offset, fitted)
    canvas.save(ICON_PNG)

    sizes = [
        (256, 256),
        (128, 128),
        (64, 64),
        (48, 48),
        (32, 32),
        (24, 24),
        (16, 16),
    ]
    images = [canvas.resize(size, Image.Resampling.LANCZOS) for size in sizes]
    images[0].save(ICON, format="ICO", sizes=sizes, append_images=images[1:])

    print(f"Source: {SOURCE} ({square.width}x{square.height})")
    print(f"Master: {MASTER_SIZE}x{MASTER_SIZE} fill={ICON_FILL_RATIO:.0%} transparent")
    print(f"Wrote {ICON}")
    print(f"Wrote {ICON_PNG}")


if __name__ == "__main__":
    main()
