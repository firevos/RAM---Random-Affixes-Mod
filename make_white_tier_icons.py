
from PIL import Image, ImageChops
from pathlib import Path
import argparse
import re

OUTPUT_SIZE = 160

# Output white. Change to "#F8F8F8" if you want a slightly softer white.
WHITE_COLOR = "#FFFFFF"

# Pixels brighter than this will become full white.
# Lower this if your source icons are dark and still not white enough.
WHITE_POINT = 180


def hex_to_rgb(hex_color):
    hex_color = hex_color.strip().lstrip("#")
    if len(hex_color) != 6:
        raise ValueError(f"Invalid color: {hex_color}. Use format like #FFFFFF")
    return tuple(int(hex_color[i:i + 2], 16) for i in (0, 2, 4))


def rename_to_1(input_path):
    new_stem = re.sub(r'_\d+$', '_1', input_path.stem)
    return f"{new_stem}{input_path.suffix}"


def levels(value, white_point=180):
    """
    Remaps brightness so the main colored icon becomes white,
    while darker accents stay darker.
    """
    if value <= 0:
        return 0
    if value >= white_point:
        return 255
    return int(value * 255 / white_point)


def recolor_to_white_preserve_shading(image, white_color="#FFFFFF"):
    """
    Converts colored icons back to white while preserving darker accents.

    Uses max(R,G,B) instead of normal grayscale luminance.
    This matters because red icons otherwise become gray, since red has low
    perceived luminance compared to white.
    """
    img = image.convert("RGBA")
    alpha = img.getchannel("A")
    r, g, b = img.split()[:3]

    # Brightness/value channel: red #FF3333 becomes bright instead of gray.
    value = ImageChops.lighter(r, ImageChops.lighter(g, b))
    value = value.point(lambda v: levels(v, WHITE_POINT))

    wr, wg, wb = hex_to_rgb(white_color)

    red = value.point(lambda v: int(v * wr / 255))
    green = value.point(lambda v: int(v * wg / 255))
    blue = value.point(lambda v: int(v * wb / 255))

    return Image.merge("RGBA", (red, green, blue, alpha))


def process_icon(input_path, output_dir):
    original = Image.open(input_path).convert("RGBA")

    resized = original.resize(
        (OUTPUT_SIZE, OUTPUT_SIZE),
        Image.Resampling.LANCZOS
    )

    white_icon = recolor_to_white_preserve_shading(resized, WHITE_COLOR)

    out_name = rename_to_1(input_path)
    white_icon.save(output_dir / out_name)
    print(f"Saved {out_name}")


def main():
    parser = argparse.ArgumentParser(
        description="Convert colored tier icons back to white _1 icons while preserving darker accents."
    )
    parser.add_argument("input", help="Folder containing colored PNG icons, e.g. files ending in _6.png")
    parser.add_argument("output", help="Folder where generated _1 icons will be saved")
    args = parser.parse_args()

    input_dir = Path(args.input)
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)

    png_files = sorted(input_dir.glob("*.png"))

    if not png_files:
        print("No PNG files found in the input folder.")
        return

    for input_path in png_files:
        process_icon(input_path, output_dir)


if __name__ == "__main__":
    main()
