"""
Image Processing Application
Supports: Color conversion, Histogram, Histogram Equalization,
and 12 image filters using OpenCV and Tkinter GUI.
"""

import tkinter as tk
from tkinter import ttk, filedialog, messagebox
import cv2
import numpy as np
from PIL import Image, ImageTk
import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
import os


class ImageProcessorApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Image Processing Application")
        self.root.geometry("1300x820")
        self.root.configure(bg="#1a1a2e")

        self.original_image = None      # BGR (OpenCV)
        self.current_image = None       # BGR (OpenCV)
        self.display_photo = None

        self._build_ui()

    # ─────────────────────────── UI LAYOUT ────────────────────────────

    def _build_ui(self):
        # ── Top bar ──
        top = tk.Frame(self.root, bg="#16213e", pady=8)
        top.pack(fill=tk.X)

        tk.Label(top, text="🖼  Image Processor", font=("Courier", 18, "bold"),
                 fg="#e94560", bg="#16213e").pack(side=tk.LEFT, padx=16)

        tk.Button(top, text="📂  Open Image", command=self.open_image,
                  bg="#e94560", fg="white", font=("Courier", 11, "bold"),
                  relief=tk.FLAT, padx=14, pady=5).pack(side=tk.LEFT, padx=8)
        tk.Button(top, text="💾  Save Image", command=self.save_image,
                  bg="#0f3460", fg="white", font=("Courier", 11),
                  relief=tk.FLAT, padx=14, pady=5).pack(side=tk.LEFT, padx=4)
        tk.Button(top, text="↩  Reset", command=self.reset_image,
                  bg="#533483", fg="white", font=("Courier", 11),
                  relief=tk.FLAT, padx=14, pady=5).pack(side=tk.LEFT, padx=4)

        # ── Main area ──
        main = tk.Frame(self.root, bg="#1a1a2e")
        main.pack(fill=tk.BOTH, expand=True, padx=10, pady=6)

        # Left panel – controls
        left = tk.Frame(main, bg="#16213e", width=280)
        left.pack(side=tk.LEFT, fill=tk.Y, padx=(0, 8))
        left.pack_propagate(False)
        self._build_controls(left)

        # Center – image display
        center = tk.Frame(main, bg="#0f3460")
        center.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        self._build_image_panel(center)

    def _build_controls(self, parent):
        style = {"bg": "#16213e", "fg": "#a8b2d8", "font": ("Courier", 9, "bold")}
        btn_style = {"bg": "#0f3460", "fg": "white", "font": ("Courier", 9),
                     "relief": tk.FLAT, "pady": 4, "anchor": "w", "padx": 8}

        def section(title):
            tk.Label(parent, text=f"  {title}", bg="#e94560", fg="white",
                     font=("Courier", 9, "bold"), anchor="w").pack(fill=tk.X, pady=(10, 2))

        # ── Color Conversion ──
        section("COLOR CONVERSION")
        for label, cmd in [
            ("⬛  Grayscale",         self.to_grayscale),
            ("🔴  Red Channel",        self.to_red),
            ("🟢  Green Channel",      self.to_green),
            ("🔵  Blue Channel",       self.to_blue),
        ]:
            tk.Button(parent, text=label, command=cmd, **btn_style).pack(fill=tk.X, pady=1)

        # ── Histogram ──
        section("HISTOGRAM")
        tk.Button(parent, text="📊  Show Histogram",      command=self.show_histogram,   **btn_style).pack(fill=tk.X, pady=1)
        tk.Button(parent, text="⚖️  Histogram Equalization", command=self.equalize_histogram, **btn_style).pack(fill=tk.X, pady=1)

        # ── Filters ──
        section("FILTERS")
        filters = [
            ("〰️  Gaussian Blur",        self.apply_gaussian_blur),
            ("📐  Sobel Edge Detection",  self.apply_sobel),
            ("☀️  Brightness Adjustment", self.apply_brightness),
            ("🔲  Negative Filter",       self.apply_negative),
            ("🔳  Median Filter",         self.apply_median),
            ("🌀  Laplacian Filter",      self.apply_laplacian),
            ("✨  Sharpen Filter",        self.apply_sharpen),
            ("💨  Motion Blur",           self.apply_motion_blur),
            ("🌊  Bilateral Filter",      self.apply_bilateral),
            ("🪨  Emboss Filter",         self.apply_emboss),
            ("🔍  Canny Edge Detection",  self.apply_canny),
            ("🎚️  Contrast Adjustment",  self.apply_contrast),
        ]
        for label, cmd in filters:
            tk.Button(parent, text=label, command=cmd, **btn_style).pack(fill=tk.X, pady=1)

        # ── Sliders ──
        section("PARAMETERS")
        tk.Label(parent, text="Brightness", **style).pack(anchor="w", padx=8)
        self.brightness_val = tk.IntVar(value=50)
        tk.Scale(parent, from_=0, to=100, orient=tk.HORIZONTAL,
                 variable=self.brightness_val, bg="#16213e", fg="white",
                 highlightthickness=0, troughcolor="#0f3460").pack(fill=tk.X, padx=8)

        tk.Label(parent, text="Contrast", **style).pack(anchor="w", padx=8)
        self.contrast_val = tk.DoubleVar(value=1.0)
        tk.Scale(parent, from_=0.1, to=3.0, resolution=0.1, orient=tk.HORIZONTAL,
                 variable=self.contrast_val, bg="#16213e", fg="white",
                 highlightthickness=0, troughcolor="#0f3460").pack(fill=tk.X, padx=8)

        tk.Label(parent, text="Blur Kernel Size (odd)", **style).pack(anchor="w", padx=8)
        self.blur_size = tk.IntVar(value=5)
        tk.Scale(parent, from_=1, to=31, resolution=2, orient=tk.HORIZONTAL,
                 variable=self.blur_size, bg="#16213e", fg="white",
                 highlightthickness=0, troughcolor="#0f3460").pack(fill=tk.X, padx=8)

        tk.Label(parent, text="Canny Low Threshold", **style).pack(anchor="w", padx=8)
        self.canny_low = tk.IntVar(value=50)
        tk.Scale(parent, from_=0, to=255, orient=tk.HORIZONTAL,
                 variable=self.canny_low, bg="#16213e", fg="white",
                 highlightthickness=0, troughcolor="#0f3460").pack(fill=tk.X, padx=8)

        tk.Label(parent, text="Canny High Threshold", **style).pack(anchor="w", padx=8)
        self.canny_high = tk.IntVar(value=150)
        tk.Scale(parent, from_=0, to=255, orient=tk.HORIZONTAL,
                 variable=self.canny_high, bg="#16213e", fg="white",
                 highlightthickness=0, troughcolor="#0f3460").pack(fill=tk.X, padx=8)

    def _build_image_panel(self, parent):
        tk.Label(parent, text="Original", bg="#0f3460", fg="#a8b2d8",
                 font=("Courier", 9)).place(x=5, y=2)
        tk.Label(parent, text="Processed", bg="#0f3460", fg="#a8b2d8",
                 font=("Courier", 9)).place(x=505, y=2)

        self.canvas_orig = tk.Label(parent, bg="#0a0a1a", relief=tk.FLAT)
        self.canvas_orig.place(x=5, y=20, width=480, height=720)

        self.canvas_proc = tk.Label(parent, bg="#0a0a1a", relief=tk.FLAT)
        self.canvas_proc.place(x=495, y=20, width=480, height=720)

        tk.Label(parent, text="Original", bg="#0f3460", fg="#e94560",
                 font=("Courier", 8)).place(x=5, y=742)
        self.lbl_info = tk.Label(parent, text="No image loaded", bg="#0f3460",
                                 fg="#a8b2d8", font=("Courier", 8))
        self.lbl_info.place(x=495, y=742)

    # ─────────────────────────── HELPERS ──────────────────────────────

    def _show_on_canvas(self, canvas, bgr_img):
        """Display a BGR or grayscale numpy image on a Tkinter Label."""
        if bgr_img is None:
            return
        h, w = bgr_img.shape[:2]
        MAX_W, MAX_H = 480, 720
        scale = min(MAX_W / w, MAX_H / h, 1.0)
        rw, rh = int(w * scale), int(h * scale)

        if len(bgr_img.shape) == 2:
            rgb = cv2.cvtColor(bgr_img, cv2.COLOR_GRAY2RGB)
        else:
            rgb = cv2.cvtColor(bgr_img, cv2.COLOR_BGR2RGB)

        pil = Image.fromarray(cv2.resize(rgb, (rw, rh)))
        photo = ImageTk.PhotoImage(pil)
        canvas.config(image=photo)
        canvas.image = photo  # keep reference

    def _update_processed(self, img):
        self.current_image = img
        self._show_on_canvas(self.canvas_proc, img)
        self.lbl_info.config(text=f"Size: {img.shape[1]}×{img.shape[0]}")

    def _require_image(self):
        if self.original_image is None:
            messagebox.showwarning("No Image", "Please open an image first.")
            return False
        return True

    # ─────────────────────── FILE OPERATIONS ──────────────────────────

    def open_image(self):
        path = filedialog.askopenfilename(
            filetypes=[("Image files", "*.jpg *.jpeg *.png *.bmp *.tiff *.gif")])
        if not path:
            return
        self.original_image = cv2.imread(path)
        if self.original_image is None:
            messagebox.showerror("Error", "Could not read the image.")
            return
        self.current_image = self.original_image.copy()
        self._show_on_canvas(self.canvas_orig, self.original_image)
        self._update_processed(self.current_image)

    def save_image(self):
        if not self._require_image():
            return
        path = filedialog.asksaveasfilename(
            defaultextension=".png",
            filetypes=[("PNG", "*.png"), ("JPEG", "*.jpg"), ("BMP", "*.bmp")])
        if path:
            cv2.imwrite(path, self.current_image)
            messagebox.showinfo("Saved", f"Image saved to:\n{path}")

    def reset_image(self):
        if not self._require_image():
            return
        self._update_processed(self.original_image.copy())

    # ──────────────────── COLOR CONVERSIONS ───────────────────────────

    def to_grayscale(self):
        if not self._require_image(): return
        gray = cv2.cvtColor(self.original_image, cv2.COLOR_BGR2GRAY)
        self._update_processed(gray)

    def to_red(self):
        if not self._require_image(): return
        img = np.zeros_like(self.original_image)
        img[:, :, 2] = self.original_image[:, :, 2]
        self._update_processed(img)

    def to_green(self):
        if not self._require_image(): return
        img = np.zeros_like(self.original_image)
        img[:, :, 1] = self.original_image[:, :, 1]
        self._update_processed(img)

    def to_blue(self):
        if not self._require_image(): return
        img = np.zeros_like(self.original_image)
        img[:, :, 0] = self.original_image[:, :, 0]
        self._update_processed(img)

    # ──────────────────── HISTOGRAM ───────────────────────────────────

    def show_histogram(self):
        if not self._require_image(): return
        fig, axes = plt.subplots(1, 2, figsize=(10, 4))
        fig.patch.set_facecolor("#1a1a2e")

        colors = ("blue", "green", "red")
        for ax, img, title in zip(axes,
                                   [self.original_image, self.current_image],
                                   ["Original", "Processed"]):
            ax.set_facecolor("#0f3460")
            ax.set_title(title, color="white")
            ax.tick_params(colors="white")
            if len(img.shape) == 2:
                ax.plot(cv2.calcHist([img], [0], None, [256], [0, 256]), color="white")
            else:
                for i, c in enumerate(colors):
                    hist = cv2.calcHist([img], [i], None, [256], [0, 256])
                    ax.plot(hist, color=c)

        win = tk.Toplevel(self.root)
        win.title("Histogram")
        win.configure(bg="#1a1a2e")
        canvas = FigureCanvasTkAgg(fig, master=win)
        canvas.draw()
        canvas.get_tk_widget().pack(fill=tk.BOTH, expand=True)

    def equalize_histogram(self):
        if not self._require_image(): return
        src = self.original_image
        if len(src.shape) == 2:
            eq = cv2.equalizeHist(src)
        else:
            yuv = cv2.cvtColor(src, cv2.COLOR_BGR2YUV)
            yuv[:, :, 0] = cv2.equalizeHist(yuv[:, :, 0])
            eq = cv2.cvtColor(yuv, cv2.COLOR_YUV2BGR)
        self._update_processed(eq)

    # ──────────────────── FILTERS ─────────────────────────────────────

    def apply_gaussian_blur(self):
        if not self._require_image(): return
        k = self.blur_size.get()
        k = k if k % 2 == 1 else k + 1
        result = cv2.GaussianBlur(self.original_image, (k, k), 0)
        self._update_processed(result)

    def apply_sobel(self):
        if not self._require_image(): return
        gray = cv2.cvtColor(self.original_image, cv2.COLOR_BGR2GRAY)
        sx = cv2.Sobel(gray, cv2.CV_64F, 1, 0, ksize=3)
        sy = cv2.Sobel(gray, cv2.CV_64F, 0, 1, ksize=3)
        mag = cv2.magnitude(sx, sy)
        result = cv2.convertScaleAbs(mag)
        self._update_processed(result)

    def apply_brightness(self):
        if not self._require_image(): return
        beta = self.brightness_val.get() - 50   # -50 … +50
        result = cv2.convertScaleAbs(self.original_image, alpha=1.0, beta=beta * 2)
        self._update_processed(result)

    def apply_negative(self):
        if not self._require_image(): return
        result = cv2.bitwise_not(self.original_image)
        self._update_processed(result)

    def apply_median(self):
        if not self._require_image(): return
        k = self.blur_size.get()
        k = k if k % 2 == 1 else k + 1
        result = cv2.medianBlur(self.original_image, k)
        self._update_processed(result)

    def apply_laplacian(self):
        if not self._require_image(): return
        gray = cv2.cvtColor(self.original_image, cv2.COLOR_BGR2GRAY)
        lap = cv2.Laplacian(gray, cv2.CV_64F)
        result = cv2.convertScaleAbs(lap)
        self._update_processed(result)

    def apply_sharpen(self):
        if not self._require_image(): return
        kernel = np.array([[0, -1, 0],
                           [-1, 5, -1],
                           [0, -1, 0]], dtype=np.float32)
        result = cv2.filter2D(self.original_image, -1, kernel)
        self._update_processed(result)

    def apply_motion_blur(self):
        if not self._require_image(): return
        k = max(3, self.blur_size.get())
        kernel = np.zeros((k, k), dtype=np.float32)
        kernel[k // 2, :] = 1.0 / k
        result = cv2.filter2D(self.original_image, -1, kernel)
        self._update_processed(result)

    def apply_bilateral(self):
        if not self._require_image(): return
        result = cv2.bilateralFilter(self.original_image, 9, 75, 75)
        self._update_processed(result)

    def apply_emboss(self):
        if not self._require_image(): return
        kernel = np.array([[-2, -1, 0],
                           [-1,  1, 1],
                           [ 0,  1, 2]], dtype=np.float32)
        gray = cv2.cvtColor(self.original_image, cv2.COLOR_BGR2GRAY)
        embossed = cv2.filter2D(gray, -1, kernel) + 128
        result = np.clip(embossed, 0, 255).astype(np.uint8)
        self._update_processed(result)

    def apply_canny(self):
        if not self._require_image(): return
        result = cv2.Canny(self.original_image,
                           self.canny_low.get(),
                           self.canny_high.get())
        self._update_processed(result)

    def apply_contrast(self):
        if not self._require_image(): return
        alpha = self.contrast_val.get()
        result = cv2.convertScaleAbs(self.original_image, alpha=alpha, beta=0)
        self._update_processed(result)


# ─────────────────────────── ENTRY POINT ──────────────────────────────

if __name__ == "__main__":
    root = tk.Tk()
    app = ImageProcessorApp(root)
    root.mainloop()
