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
| `csharp/` | C# (.NET 8) | Magick.NET (ImageMagick) | [csharp/README.md](csharp/README.md) |
| `c/` | C | libwebp (decoded by Wuffs) | [c/README.md](c/README.md) |

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
├── csharp/
│   ├── README.md
│   ├── Webpify.csproj
│   └── Program.cs
└── c/
    ├── README.md
    ├── Makefile
    ├── webpify.c
    └── wuffs-v0.4.c
```

---

## Shared Behavior

All implementations follow the same workflow:

- Pass image paths or wildcard patterns (e.g. `*.jpg`) as arguments. Subfolders are not searched.
- Ask once at startup whether to force conversion.
- For `.jpg` / `.jpeg` / `.webp` sources, skip the file if the WebP result is not smaller (unless forced).
- Write results to an `output` folder.
- Only JPEG / PNG / WebP are accepted, and images larger than the pixel limit are rejected before decoding.
- EXIF / XMP / ICC metadata are removed from the output.

Encoding settings are defined as constants at the top of each source file:

| Setting | Range | Default | Description |
| --- | --- | --- | --- |
| Quality | 0 – 100 | `10` | Lower = smaller file, lower quality. Adjust per use case. |
| Method | 0 – 6 | `6` | Compression effort, 6 gives the best compression but is slower. |
| Max image pixels | — | `25,000,000` | Maximum pixels per image. Larger images are rejected. |

---

## Differences

| | `pillow_convert.py` | `cwebp_convert.py` | C# (`Program.cs`) | C (`webpify.c`) |
| --- | --- | --- | --- | --- |
| Encoder | Pillow | cwebp | Magick.NET | libwebp |
| EXIF orientation | Yes | No | Yes | No |
| Speed | Slowest | Fast | Medium | Fastest |
| ICC → sRGB conversion | Yes | No | Yes | No |

---

## References

- [Wuffs](https://github.com/google/wuffs-mirror-release-c/tree/main/release/c) — Used by the C implementation for decoding untrusted image input. Its approach of treating image files as untrusted also inspired the input safety checks in the other implementations.
- [List of file signatures (Wikipedia)](https://en.wikipedia.org/wiki/List_of_file_signatures) — Used for the magic bytes check of JPEG / PNG / WebP.
