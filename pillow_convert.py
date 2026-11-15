import glob
import io
import os
import sys
from PIL import Image

# SETTING_QUALITY: 0 - 100, depends on the use case (lower = higher compression).
# SETTING_METHOD: 0 - 6, 6 is maximum compression, but takes more time (~100 ms).

SETTING_QUALITY: int | float = 10.0
SETTING_METHOD: int = 6
IS_FORCE_CONVERT: bool = input("If source image size is better than after convert, do you still want to convert? (Y/N): ").strip().lower() == "y"

def pillow_convert() -> None:
    for arg in argv:
        file_name: str
        ext: str

        file_name, ext = os.path.splitext(arg)
        ext_lower: str = ext.lower()
        webp_path: str = f"{file_name}.webp"

        buffer: io.BytesIO = io.BytesIO()
        with Image.open(arg) as im:
            im.convert("RGB").save(buffer, "WEBP", quality = SETTING_QUALITY, method = SETTING_METHOD)

        if ext_lower in (".jpg", ".jpeg") and not IS_FORCE_CONVERT:
            source_size: int = os.path.getsize(arg)
            new_size: int = buffer.tell()

            if new_size >= source_size:
                print(f"Skip: {arg} is better than before convert.")
                continue

        with open(webp_path, "wb") as f:
            f.write(buffer.getvalue())

if __name__ == "__main__":
    argv: list[str] = []
    for pattern in sys.argv[1:]:
        matched: list[str] = glob.glob(pattern)
        if not matched:
            print(f"{pattern} is not found.")
        argv.extend(matched)

    pillow_convert()