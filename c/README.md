# Webpify — C

The C implementation of Webpify. Batch-converts images to WebP with **libwebp**, decoding input with **Wuffs**, with configurable parameters directly in the source code.

---

## Features

- **Batch Conversion**: Pass image paths or wildcard patterns (e.g. `*.jpg`) as arguments. Wildcards are expanded in the given folder only, subfolders are not searched.
- **Size Guard**: For `.jpg` / `.jpeg` / `.webp` sources, the WebP file is only written if it is actually smaller than the original (can be overridden at startup).
- **Input Safety**: Only JPEG / PNG / WebP are accepted, and images larger than `MAX_IMAGE_PIXELS` are rejected before decoding. Decoders for other formats are not compiled in.
- **Metadata Removal**: Only pixel data is decoded and re-encoded, so EXIF / XMP / ICC metadata are not carried over to the output.

---

## Tech Stack

- **C**
- **libwebp**
- **Wuffs** (`wuffs-v0.4.c` [source](https://github.com/google/wuffs-mirror-release-c/tree/main/release/c))

---

## Why Wuffs

| Library | Reason not chosen |
| --- | --- |
| stb_image | Not designed for untrusted input. |

Wuffs is a memory-safe decoder built for untrusted input, and it detects the format by file content only. It also provides an stb_image-compatible API, so it can be used the same way.

---

## Getting Started

### 1. Clone the Repository

```cmd
git clone https://github.com/kusogame68/Webpify.git
cd Webpify\c
```

### 2. Prerequisites

Ensure you have GCC and `make` installed.

libwebp is required for building:

- **Windows:**
  ```bash
  pacman -S mingw-w64-ucrt-x86_64-libwebp
  ```

- Verify the installation:
  ```bash
  pacman -Q mingw-w64-ucrt-x86_64-libwebp
  ```

### 3. Build

```bash
make
```

This builds both `webpify_o2.exe` and `webpify_o3.exe`. To build only one of them, run `make o2` or `make o3`. To remove the built files, run `make clean`.

### 4. Run

Pass image paths as arguments:

```cmd
webpify_o3.exe photo.jpg banner.png
```

Wildcards are supported, so a whole folder can be converted at once:

```cmd
webpify_o3.exe "D:\website\img\*.jpg" "D:\website\img\*.png"
```

If a path or pattern matches no file, it is reported as `<pattern> is not found.` and skipped.

### 5. Answer the Prompt

On startup, the program asks:

```
If source image size is better than after convert, do you still want to convert? (Y/N):
```

- `N` — For `.jpg` / `.jpeg` / `.webp` sources, skip the file if the WebP result is not smaller. Skipped files are reported as `Skip: <file> (webp) is not smaller.`
- `Y` — Always write the WebP file.

The output is saved in an `output` folder under the current working directory, with the same name and a `.webp` extension (e.g. `photo.jpg` → `output\photo.webp`). An existing `.webp` file with the same name will be overwritten.

---

## Configuration

Encoding settings are defined as macros at the top of `webpify.c`:

| Macro | Range | Default | Description |
| --- | --- | --- | --- |
| `SETTING_QUALITY` | 0 – 100 | `10.0f` | Lower = smaller file, lower quality. Adjust per use case. |
| `SETTING_METHOD` | 0 – 6 | `6` | Compression effort, 6 gives the best compression but is slower. |
| `MAX_IMAGE_PIXELS` | — | `25,000,000` | Maximum pixels per image. Larger images are rejected. |

---

## -O2 vs -O3

| Build | Time | Size |
| --- | --- | --- |
| `webpify_o2.exe` | N + ~10 ms | 1,272 KB |
| `webpify_o3.exe` | N | 1,492 KB |

`-O3` is slightly faster, but the executable is about 17% larger.

---

## How It Works

```
argv (paths / wildcard patterns)
  |
  v
expand patterns with _findfirst (Windows) / glob (Linux)
  |
  v
check format and pixel limit (header only)
  |
  v
decode to RGBA with Wuffs
  |
  v
encode each image to WebP in memory with libwebp
  |
  v
.jpg / .jpeg / .webp source and not force-convert?
  |-- yes --> WebP smaller than source? -- no --> Skip
  |                   |
  |                  yes
  v                   v
write <name>.webp to output\
```
