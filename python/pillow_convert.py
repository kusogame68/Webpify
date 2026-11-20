import glob
import io
import os
import sys
from PIL import Image, ImageOps

# SETTING_QUALITY: 0 - 100, depends on the use case (lower = higher compression).
# SETTING_METHOD: 0 - 6, 6 is maximum compression, but takes more time (~100 ms).

SETTING_QUALITY: int = 10
SETTING_METHOD: int = 6
OUTPUT_FOLDER = "output"
is_force_convert: bool = input("If source image size is better than after convert, do you still want to convert? (Y/N): ").strip().lower() == "y"

def pillow_convert() -> None:
    os.makedirs(OUTPUT_FOLDER, exist_ok = True)

    for arg in argv:
        file_name: str
        ext: str

        file_name, ext = os.path.splitext(os.path.basename(arg))
        webp_path: str = os.path.join(OUTPUT_FOLDER, f"{file_name}.webp")
        buffer: io.BytesIO = io.BytesIO()

        try:
            with Image.open(arg) as im:
                ImageOps.exif_transpose(im).save(buffer, "WEBP", quality = SETTING_QUALITY, method = SETTING_METHOD)
        except Exception as e:
            print(f"Failed: {arg} ({e})")
            continue

        if ext.lower() in (".jpg", ".jpeg", ".webp") and not is_force_convert:
            source_size: int = os.path.getsize(arg)
            new_size: int = buffer.tell()

            if new_size >= source_size:
                print(f"Skip: {arg} (webp) is not smaller.")
                continue

        with open(webp_path, "wb") as f:
            f.write(buffer.getvalue())

if __name__ == "__main__":
    argv: list[str] = []
    for pattern in sys.argv[1:]:
        matched: list[str] = [pattern] if os.path.isfile(pattern) else glob.glob(pattern)
        if not matched:
            print(f"{pattern} is not found.")
        argv.extend(matched)

    pillow_convert()