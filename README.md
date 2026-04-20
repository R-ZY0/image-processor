# Image Processing Application

A full-featured image processing desktop application implemented in **Python (Tkinter + OpenCV)** and **C# (WinForms)**.

---

## Features

| # | Feature | Python | C# |
|---|---------|--------|----|
| 1 | Open image via file dialog | ✅ | ✅ |
| 2 | Save processed image | ✅ | ✅ |
| 3 | Reset to original | ✅ | ✅ |
| **Color** | | | |
| 4 | Grayscale conversion | ✅ | ✅ |
| 5 | Red channel extraction | ✅ | ✅ |
| 6 | Green channel extraction | ✅ | ✅ |
| 7 | Blue channel extraction | ✅ | ✅ |
| **Histogram** | | | |
| 8 | Show histogram (per-channel) | ✅ | ✅ |
| 9 | Histogram equalization | ✅ | ✅ |
| **Filters** | | | |
| 10 | Gaussian Blur | ✅ | ✅ |
| 11 | Sobel Edge Detection | ✅ | ✅ |
| 12 | Brightness Adjustment | ✅ | ✅ |
| 13 | Negative Filter | ✅ | ✅ |
| 14 | Median Filter | ✅ | ✅ |
| 15 | Laplacian Filter | ✅ | ✅ |
| 16 | Sharpen Filter | ✅ | ✅ |
| 17 | Motion Blur | ✅ | ✅ |
| 18 | Bilateral Filter | ✅ | ✅ |
| 19 | Emboss Filter | ✅ | ✅ |
| 20 | Canny Edge Detection | ✅ | ✅ |
| 21 | Contrast Adjustment | ✅ | ✅ |

---

## Python Version

### Requirements
- Python 3.9+
- Libraries: `opencv-python`, `numpy`, `Pillow`, `matplotlib`

### Setup & Run
```bash
cd python/
pip install -r requirements.txt
python image_processor.py
```

### Architecture
```
image_processor.py
├── ImageProcessorApp          ← Main Tkinter app class
│   ├── _build_ui()            ← Layout: top bar, left panel, image panels
│   ├── _build_controls()      ← All buttons + sliders
│   ├── _build_image_panel()   ← Dual PictureBox (Original / Processed)
│   ├── open_image / save_image / reset_image
│   ├── to_grayscale / to_red / to_green / to_blue
│   ├── show_histogram / equalize_histogram
│   └── apply_* (12 filter methods)
```

### How Each Filter Works (Python / OpenCV)

| Filter | Method |
|--------|--------|
| Gaussian Blur | `cv2.GaussianBlur(img, (k,k), 0)` |
| Sobel | `cv2.Sobel` on X+Y, magnitude via `cv2.magnitude` |
| Brightness | `cv2.convertScaleAbs(alpha=1, beta=Δ)` |
| Negative | `cv2.bitwise_not` |
| Median | `cv2.medianBlur` |
| Laplacian | `cv2.Laplacian` on grayscale |
| Sharpen | Custom kernel `[0,-1,0; -1,5,-1; 0,-1,0]` via `cv2.filter2D` |
| Motion Blur | Horizontal kernel via `cv2.filter2D` |
| Bilateral | `cv2.bilateralFilter(d=9, sigmaColor=75, sigmaSpace=75)` |
| Emboss | Custom kernel `[-2,-1,0; -1,1,1; 0,1,2]` + offset 128 |
| Canny | `cv2.Canny(low, high)` |
| Contrast | `cv2.convertScaleAbs(alpha=α, beta=0)` |

---

## C# Version

### Requirements
- .NET 8 SDK  
- Windows (WinForms)

### Setup & Run
```bash
cd csharp/
dotnet run
# or build:
dotnet build -c Release
# then run:
./bin/Release/net8.0-windows/ImageProcessor.exe
```

### Architecture
```
csharp/
├── Program.cs              ← Entry point (STAThread)
├── MainForm.cs             ← Event handlers for all buttons
├── MainForm.Designer.cs    ← Full UI layout (no .resx needed)
├── ImageFilters.cs         ← All 21 image processing algorithms
└── HistogramForm.cs        ← GDI+ histogram popup window
```

### Key Classes

#### `ImageFilters` (static)
Pure C# implementations using `BitmapLocker` for fast pixel access.  
All methods take a `Bitmap` and return a **new** `Bitmap` — the original is never mutated.

#### `BitmapLocker`
Wraps `Bitmap.LockBits` / `Marshal.Copy` for O(1) pixel access instead of slow `GetPixel`/`SetPixel`.

#### `HistogramForm`
Draws per-channel (R/G/B) bar histograms using pure GDI+ — no third-party chart libraries needed.

### How Each Filter Works (C#)

| Filter | Algorithm |
|--------|-----------|
| Gaussian Blur | Separable 1-D kernel applied H then V |
| Sobel | Gradient magnitude from Gx + Gy convolutions |
| Brightness | `pixel = Clamp(pixel + β)` |
| Negative | `pixel = 255 − pixel` |
| Median | Sort neighborhood pixels, pick middle value |
| Laplacian | `[0,1,0; 1,-4,1; 0,1,0]` + offset 128 |
| Sharpen | `[0,-1,0; -1,5,-1; 0,-1,0]` convolution |
| Motion Blur | Row-only box filter |
| Bilateral | Per-pixel weighted average (spatial + color Gaussian) |
| Emboss | `[-2,-1,0; -1,1,1; 0,1,2]` + offset 128 |
| Canny | Gaussian → Sobel → NMS → double threshold + hysteresis |
| Contrast | `pixel = Clamp(pixel × α)` |
| Hist. Equalization | CDF-based LUT applied to luminance channel |

---

## UI Layout (both versions)

```
┌─────────────────────────────────────────────────────────────────┐
│  🖼 Image Processor   [📂 Open]  [💾 Save]  [↩ Reset]           │
├────────────┬────────────────────────┬───────────────────────────┤
│ COLOR      │                        │                            │
│ [Grayscale]│   ORIGINAL IMAGE       │   PROCESSED IMAGE          │
│ [Red]      │                        │                            │
│ [Green]    │                        │                            │
│ [Blue]     │                        │                            │
│ HISTOGRAM  │                        │                            │
│ [Show]     │                        │                            │
│ [Equalize] │                        │                            │
│ FILTERS    │                        │                            │
│ [Gaussian] │                        │                            │
│ [Sobel]    │                        │                            │
│  ... × 10  │                        │                            │
│ PARAMETERS │                        │                            │
│ [Sliders]  │                        │                            │
└────────────┴────────────────────────┴───────────────────────────┘
```

---

## Supported Image Formats
JPG, JPEG, PNG, BMP, TIFF, GIF

---

## Notes
- All filters operate on the **original** image, not the previously filtered result (non-destructive editing).
- Use the sliders/spinner to tune kernel size, brightness, contrast, and Canny thresholds before applying a filter.
- The Bilateral filter is the slowest (O(n²) per pixel) — use on smaller images for real-time feel.
