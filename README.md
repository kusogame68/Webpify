# Webpify

A lightweight CLI script collection for batch-converting images to WebP, built for preparing web assets. Includes two standalone scripts using **Pillow** and **libwebp** (`cwebp`), with configurable parameters directly in the source code.

---

## Features

- **Batch Conversion**: Pass image paths or wildcard patterns (e.g. `*.jpg`) as arguments; each image is converted to a `.webp` file next to the original.
- **Two Scripts**:
  - `pillow_convert.py` — pure Python conversion via the Pillow library.
  - `libwebp_convert.py` — invokes Google's official `cwebp` CLI binary via `subprocess`.
- **Size Guard for JPEG**: For `.jpg` / `.jpeg` sources, the WebP file is only written if it is actually smaller than the original (can be overridden at startup).

---

## Tech Stack

- **Python 3.12**
- **Pillow**
- **libwebp (`cwebp`)** — only required for `libwebp_convert.py`

---

## Getting Started

### 1. Clone the Repository

```cmd
git clone https://github.com/kusogame68/Webpify.git
cd Webpify
```

### 2. Prerequisites

Ensure you have `uv` installed for dependency management.

- **Windows:**
  ```powershell
  irm https://astral.sh/uv/install.ps1 | iex
  ```

If you plan to use `libwebp_convert.py`, `cwebp` must also be available on your `PATH`:

- **Windows:** Download the precompiled libwebp package from Google's [WebP downloads page](https://developers.google.com/speed/webp/download), extract it, and add its `bin` folder to `PATH`. Or install directly via `winget`:
  ```cmd
  winget install Google.Libwebp
  ```

- Verify the installation:
  ```cmd
  cwebp -version
  ```

### 3. Install Dependencies

```cmd
uv sync
```

### 4. Run

Pass image paths as arguments:

```cmd
:: Using Pillow
uv run pillow_convert.py photo.jpg banner.png

:: Using libwebp
uv run libwebp_convert.py photo.jpg banner.png
```

Wildcards are supported, so a whole folder can be converted at once:

```cmd
uv run pillow_convert.py "D:\website\img\*.jpg" "D:\website\img\*.png"
```

If a path or pattern matches no file, it is reported as `<pattern> is not found.` and skipped.

### 5. Answer the Prompt

On startup, the script asks:

```
If source image size is better than after convert, do you still want to convert? (Y/N):
```

- `N` — For JPEG sources, skip the file if the WebP result is not smaller. Skipped files are reported as `Skip: <file> is better than before convert.`
- `Y` — Always write the WebP file.

The output is saved in the same folder as the source, with the same name and a `.webp` extension (e.g. `photo.jpg` → `photo.webp`). An existing `.webp` file with the same name will be overwritten.

---

## Configuration

Encoding settings are defined as constants at the top of each script:

| Constant | Range | Default | Description |
| --- | --- | --- | --- |
| `SETTING_QUALITY` | 0 – 100 | `10` | Lower = smaller file, lower quality. Adjust per use case. |
| `SETTING_METHOD` | 0 – 6 | `6` | Compression effort, 6 gives the best compression but is slower. |

---

## How It Works

```
argv (paths / wildcard patterns)
  |
  v
expand patterns with glob
  |
  v
encode each image to WebP in memory
  |
  v
JPEG source and not force-convert?
  |-- yes --> WebP smaller than source? -- no --> Skip
  |                   |
  |                  yes
  v                   v
write <name>.webp next to the source
```
