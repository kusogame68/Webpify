# Webpify

A lightweight CLI script collection for batch-converting images to WebP, built for preparing web assets. Each language implementation lives in its own folder with its own README.

---

## Motivation

This project started when I found that converting images to WebP brings great benefits to web performance. So I tried implementing it myself, so that future projects can adopt it as well.

---

## Implementations

| Folder | Language | Encoder | Docs |
| --- | --- | --- | --- |
| `python/` | Python 3.12 | Pillow / cwebp | [python/README.md](python/README.md) |
| `csharp/` | C# (.NET) | Magick.NET (ImageMagick) | [csharp/README.md](csharp/README.md) |

---

## Project Structure

```
Webpify/
├── README.md
├── .gitignore
├── python/
│   ├── README.md
│   ├── pyproject.toml
│   ├── uv.lock
│   ├── .python-version
│   ├── pillow_convert.py
│   └── cwebp_convert.py
└── csharp/
    ├── README.md
    ├── Webpify.csproj
    └── Program.cs
```

---

## Shared Behavior

All implementations follow the same workflow:

- Pass image paths or wildcard patterns (e.g. `*.jpg`) as arguments. Subfolders are not searched.
- Ask once at startup whether to force conversion.
- For `.jpg` / `.jpeg` / `.webp` sources, skip the file if the WebP result is not smaller (unless forced).
- Write results to an `output` folder.

Encoding settings are defined as constants at the top of each source file:

| Setting | Range | Default | Description |
| --- | --- | --- | --- |
| Quality | 0 – 100 | `10` | Lower = smaller file, lower quality. Adjust per use case. |
| Method | 0 – 6 | `6` | Compression effort, 6 gives the best compression but is slower. |

---

## Differences

| | `pillow_convert.py` | `cwebp_convert.py` | C# (`Program.cs`) |
| --- | --- | --- | --- |
| Encoder | Pillow | cwebp | Magick.NET |
| EXIF orientation | Yes | No | Yes |
| Speed | Slowest | Fastest | Medium |