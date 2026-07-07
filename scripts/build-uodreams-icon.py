"""Crop the golden unicorn from uodreams_logo.png and build a multi-size .ico."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "src" / "ClassicUO.Launcher.Custom" / "Resources"
LOGO = RES / "uodreams_logo.png"
ICON = RES / "uodreams.ico"
ICON_PNG = RES / "uodreams_icon.png"


def content_bbox(img: Image.Image, max_x: int | None = None) -> tuple[int, int, int, int]:
    pixels = img.load()
    width, height = img.size
    limit_x = max_x if max_x is not None else width

    min_x, min_y, max_x2, max_y2 = width, height, 0, 0
    found = False

    for y in range(height):
        for x in range(limit_x):
            r, g, b, a = pixels[x, y]
            if a < 16:
                continue
            if r + g + b < 40:
                continue

            found = True
            min_x = min(min_x, x)
            min_y = min(min_y, y)
            max_x2 = max(max_x2, x)
            max_y2 = max(max_y2, y)

    if not found:
        side = min(width, height)
        return 0, 0, side, side

    return min_x, min_y, max_x2 + 1, max_y2 + 1


def square_crop(box: tuple[int, int, int, int], max_width: int, max_height: int) -> tuple[int, int, int, int]:
    left, top, right, bottom = box
    width = right - left
    height = bottom - top
    side = max(width, height)
    side = min(side, max_width, max_height)

    cx = (left + right) // 2
    cy = (top + bottom) // 2

    left = max(0, cx - side // 2)
    top = max(0, cy - side // 2)
    right = min(max_width, left + side)
    bottom = min(max_height, top + side)

    # re-center if clamped
    if right - left < side:
        left = max(0, right - side)
    if bottom - top < side:
        top = max(0, bottom - side)

    return left, top, right, bottom


def main() -> None:
    logo = Image.open(LOGO).convert("RGBA")
    width, height = logo.size

    # Unicorn lives on the left side of the banner — stop before the text.
    bbox = content_bbox(logo, max_x=int(width * 0.38))
    crop_box = square_crop(bbox, width, height)
    unicorn = logo.crop(crop_box)

    # Transparent padding for a clean taskbar icon.
    canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    unicorn = unicorn.resize((220, 220), Image.Resampling.LANCZOS)
    offset = ((256 - 220) // 2, (256 - 220) // 2)
    canvas.paste(unicorn, offset, unicorn)
    canvas.save(ICON_PNG)

    sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    images = [canvas.resize(size, Image.Resampling.LANCZOS) for size in sizes]
    images[0].save(ICON, format="ICO", sizes=sizes, append_images=images[1:])

    print(f"Wrote {ICON}")
    print(f"Wrote {ICON_PNG}")


if __name__ == "__main__":
    main()
