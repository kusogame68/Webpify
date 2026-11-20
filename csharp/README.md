# Webpify — C#

The C# implementation of Webpify. Batch-converts images to WebP with **Magick.NET** (ImageMagick), with configurable parameters directly in the source code.

---

## Features

- **Batch Conversion**: Pass image paths or wildcard patterns (e.g. `*.jpg`) as arguments. Wildcards are expanded in the given folder only, subfolders are not searched.
- **Parallel Processing**: Images are converted in parallel with `Parallel.ForEach`.
- **EXIF Orientation**: Images are auto-rotated based on their EXIF orientation before encoding.
- **Size Guard**: For `.jpg` / `.jpeg` / `.webp` sources, the WebP file is only written if it is actually smaller than the original (can be overridden at startup).

---

## Tech Stack

- **C# (.NET 8)**
- **Magick.NET** (`Magick.NET-Q8-AnyCPU`)

---

## Why Magick.NET

| Library | Reason not chosen |
| --- | --- |
| SixLabors.ImageSharp | Requires a Six Labors license key (v4.0.0). |
| SkiaSharp | Dev fast, but the WebP `method` cannot be adjusted. |

Magick.NET is not as fast as the two above, but it offers better control over encoding settings.

---

## Getting Started

### 1. Clone the Repository

```cmd
git clone https://github.com/kusogame68/Webpify.git
cd Webpify\csharp
```

### 2. Prerequisites

Ensure you have the .NET SDK installed.

- Verify the installation:
  ```cmd
  dotnet --version
  ```

### 3. Install Dependencies

```cmd
dotnet restore
```

### 4. Run

Pass image paths as arguments after `--`:

```cmd
dotnet run -- photo.jpg banner.png
```

Wildcards are supported, so a whole folder can be converted at once:

```cmd
dotnet run -- "D:\website\img\*.jpg" "D:\website\img\*.png"
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

Encoding settings are defined as constants at the top of `Program.cs`:

| Constant | Range | Default | Description |
| --- | --- | --- | --- |
| `SettingQuality` | 0 – 100 | `10` | Lower = smaller file, lower quality. Adjust per use case. |
| `SettingMethod` | 0 – 6 | `6` | Compression effort, 6 gives the best compression but is slower. |

---

## How It Works

```
args (paths / wildcard patterns)
  |
  v
expand patterns with Directory.GetFiles
  |
  v
encode each image to WebP in memory (in parallel)
  |
  v
.jpg / .jpeg / .webp source and not force-convert?
  |-- yes --> WebP smaller than source? -- no --> Skip
  |                   |
  |                  yes
  v                   v
write <name>.webp to output\
```
