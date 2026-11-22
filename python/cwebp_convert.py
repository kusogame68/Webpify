import glob
import os
import subprocess
import sys
import warnings
from PIL import Image

# SETTING_QUALITY: 0 - 100, depends on the use case (lower = higher compression).
# SETTING_METHOD: 0 - 6, 6 is maximum compression, but takes more time (~100 ms).
# MAX_IMAGE_PIXELS: images larger than this are rejected before decoding.

SETTING_QUALITY: str = "10"
SETTING_METHOD: str = "6"
OUTPUT_FOLDER = "output"
ALLOWED_FORMATS: list[str] = ["JPEG", "PNG", "WEBP"]
Image.MAX_IMAGE_PIXELS = 25000000
warnings.simplefilter("error", Image.DecompressionBombWarning)
is_force_convert: bool = input("If source image size is better than after convert, do you still want to convert? (Y/N): ").strip().lower() == "y"

def cwebp_convert() -> None:
    os.makedirs(OUTPUT_FOLDER, exist_ok = True)

    for arg in argv:
        file_name: str
        ext: str

        file_name, ext = os.path.splitext(os.path.basename(arg))
        webp_path: str = os.path.join(OUTPUT_FOLDER, f"{file_name}.webp")

        try:
            Image.open(arg, formats = ALLOWED_FORMATS).close()

            result = subprocess.run(
                ["cwebp", "-q", SETTING_QUALITY, "-m", SETTING_METHOD, "-mt", "-quiet", os.path.abspath(arg), "-o", "-"],
                capture_output = True, check = True
            )
        except Exception as e:
            print(f"Failed: {arg} ({e})")
            continue

        webp_bytes: bytes = result.stdout

        if ext.lower() in (".jpg", ".jpeg", ".webp") and not is_force_convert:
            source_size: int = os.path.getsize(arg)
            new_size: int = len(webp_bytes)

            if new_size >= source_size:
                print(f"Skip: {arg} (webp) is not smaller.")
                continue

        with open(webp_path, "wb") as f:
            f.write(webp_bytes)

if __name__ == "__main__":
    argv: list[str] = []
    for pattern in sys.argv[1:]:
        matched: list[str] = [pattern] if os.path.isfile(pattern) else glob.glob(pattern)
        if not matched:
            print(f"{pattern} is not found.")
        argv.extend(matched)

    cwebp_convert()