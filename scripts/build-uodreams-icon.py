"""Build a multi-size .ico from the golden unicorn logo asset."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "src" / "ClassicUO.Launcher.Custom" / "Resources"
SOURCE = RES / "logo-uod-unicorn.png"
ICON = RES / "uodreams.ico"
ICON_PNG = RES / "uodreams_icon.png"


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"Source logo not found: {SOURCE}")

    logo = Image.open(SOURCE).convert("RGBA")
    width, height = logo.size
    side = min(width, height)

    # Center-crop to square, then pad to 256x256 for crisp downscaling.
    left = (width - side) // 2
    top = (height - side) // 2
    square = logo.crop((left, top, left + side, top + side))

    canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    fitted = square.resize((232, 232), Image.Resampling.LANCZOS)
    offset = ((256 - 232) // 2, (256 - 232) // 2)
    canvas.paste(fitted, offset, fitted)
    canvas.save(ICON_PNG)

    sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    images = [canvas.resize(size, Image.Resampling.LANCZOS) for size in sizes]
    images[0].save(ICON, format="ICO", sizes=sizes, append_images=images[1:])

    print(f"Wrote {ICON}")
    print(f"Wrote {ICON_PNG}")


if __name__ == "__main__":
    main()
