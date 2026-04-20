using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageProcessor
{
    /// <summary>
    /// Pure-C# implementations of all 15 image processing operations.
    /// All methods work on System.Drawing.Bitmap and return a new Bitmap.
    /// </summary>
    public static class ImageFilters
    {
        // ═══════════════════════ COLOR CONVERSIONS ═══════════════════════

        /// <summary>Convert image to grayscale (luminance formula).</summary>
        public static Bitmap ToGrayscale(Bitmap src)
        {
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    data.GetPixel(x, y, out byte r, out byte g, out byte b);
                    byte gray = (byte)(0.2126 * r + 0.7152 * g + 0.0722 * b);
                    outData.SetPixel(x, y, gray, gray, gray);
                }
            return result;
        }

        /// <summary>Keep only the specified color channel (R, G, or B).</summary>
        public static Bitmap ExtractChannel(Bitmap src, char channel)
        {
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    data.GetPixel(x, y, out byte r, out byte g, out byte b);
                    byte nr = channel == 'R' ? r : (byte)0;
                    byte ng = channel == 'G' ? g : (byte)0;
                    byte nb = channel == 'B' ? b : (byte)0;
                    outData.SetPixel(x, y, nr, ng, nb);
                }
            return result;
        }

        // ═══════════════════════ HISTOGRAM ═══════════════════════════════

        /// <summary>Histogram equalization via YCbCr luminance channel.</summary>
        public static Bitmap EqualizeHistogram(Bitmap src)
        {
            // Build grayscale histogram
            int[] hist = new int[256];
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    data.GetPixel(x, y, out byte r, out byte g, out byte b);
                    int luma = (int)(0.2126 * r + 0.7152 * g + 0.0722 * b);
                    hist[luma]++;
                }

            // CDF → lookup table
            int total = src.Width * src.Height;
            double[] cdf = new double[256];
            cdf[0] = hist[0];
            for (int i = 1; i < 256; i++) cdf[i] = cdf[i - 1] + hist[i];
            double cdfMin = 0;
            for (int i = 0; i < 256; i++) if (cdf[i] > 0) { cdfMin = cdf[i]; break; }

            byte[] lut = new byte[256];
            for (int i = 0; i < 256; i++)
                lut[i] = (byte)Math.Round((cdf[i] - cdfMin) / (total - cdfMin) * 255.0);

            // Apply LUT via scaling ratio
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    data.GetPixel(x, y, out byte r, out byte g, out byte b);
                    int luma = (int)(0.2126 * r + 0.7152 * g + 0.0722 * b);
                    double ratio = luma == 0 ? 1.0 : (double)lut[luma] / luma;
                    byte nr = Clamp((int)(r * ratio));
                    byte ng = Clamp((int)(g * ratio));
                    byte nb = Clamp((int)(b * ratio));
                    outData.SetPixel(x, y, nr, ng, nb);
                }
            return result;
        }

        // ═══════════════════════ FILTERS ═════════════════════════════════

        /// <summary>Gaussian blur using a separable kernel.</summary>
        public static Bitmap GaussianBlur(Bitmap src, int kernelSize = 5)
        {
            kernelSize = EnsureOdd(kernelSize);
            double sigma = kernelSize / 6.0;
            double[] kernel = BuildGaussianKernel(kernelSize, sigma);
            var temp = ConvolveHorizontal(src, kernel);
            var result = ConvolveVertical(temp, kernel);
            temp.Dispose();
            return result;
        }

        /// <summary>Sobel edge detection (gradient magnitude).</summary>
        public static Bitmap SobelEdge(Bitmap src)
        {
            int width = src.Width;
            int height = src.Height;

            Bitmap result = new Bitmap(width, height);

            int[,] gx = {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };

            int[,] gy = {
                { -1, -2, -1 },
                {  0,  0,  0 },
                {  1,  2,  1 }
            };

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int sumX = 0;
                    int sumY = 0;

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            Color c = src.GetPixel(x + i, y + j);
                            int gray = (c.R + c.G + c.B) / 3;

                            sumX += gx[j + 1, i + 1] * gray;
                            sumY += gy[j + 1, i + 1] * gray;
                        }
                    }

                    int mag = (int)Math.Sqrt(sumX * sumX + sumY * sumY);
                    mag = Math.Min(255, Math.Max(0, mag));

                    result.SetPixel(x, y, Color.FromArgb(mag, mag, mag));
                }
            }

            return result;
        }

        /// <summary>Add/subtract brightness by a fixed beta value.</summary>
        public static Bitmap AdjustBrightness(Bitmap src, int beta)
        {
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    data.GetPixel(x, y, out byte r, out byte g, out byte b);
                    outData.SetPixel(x, y,
                        Clamp(r + beta), Clamp(g + beta), Clamp(b + beta));
                }
            return result;
        }

        /// <summary>Negative (invert all channels).</summary>
        public static Bitmap Negative(Bitmap src)
        {
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    data.GetPixel(x, y, out byte r, out byte g, out byte b);
                    outData.SetPixel(x, y,
                        (byte)(255 - r), (byte)(255 - g), (byte)(255 - b));
                }
            return result;
        }

        /// <summary>Median filter (per-channel) with given kernel radius.</summary>
        public static Bitmap MedianFilter(Bitmap src, int kernelSize = 3)
        {
            kernelSize = EnsureOdd(kernelSize);
            int half = kernelSize / 2;
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            int count = kernelSize * kernelSize;
            byte[] rs = new byte[count], gs = new byte[count], bs = new byte[count];

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    int idx = 0;
                    for (int ky = -half; ky <= half; ky++)
                        for (int kx = -half; kx <= half; kx++)
                        {
                            int nx = Math.Clamp(x + kx, 0, src.Width - 1);
                            int ny = Math.Clamp(y + ky, 0, src.Height - 1);
                            data.GetPixel(nx, ny, out rs[idx], out gs[idx], out bs[idx]);
                            idx++;
                        }
                    Array.Sort(rs); Array.Sort(gs); Array.Sort(bs);
                    outData.SetPixel(x, y, rs[count / 2], gs[count / 2], bs[count / 2]);
                }
            return result;
        }

        /// <summary>Laplacian edge filter.</summary>
        public static Bitmap LaplacianFilter(Bitmap src)
        {
            double[,] kernel = { { 0, 1, 0 }, { 1, -4, 1 }, { 0, 1, 0 } };
            var gray = ToGrayscale(src);
            var result = Convolve2D(gray, kernel, 1, offset: 128);
            gray.Dispose();
            return result;
        }

        /// <summary>Sharpen filter using unsharp-like kernel.</summary>
        public static Bitmap SharpenFilter(Bitmap src)
        {
            double[,] kernel = { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
            return Convolve2D(src, kernel, 1);
        }

        /// <summary>Horizontal motion blur.</summary>
        public static Bitmap MotionBlur(Bitmap src, int kernelSize = 9)
        {
            kernelSize = Math.Max(3, EnsureOdd(kernelSize));
            double[] row = new double[kernelSize];
            for (int i = 0; i < kernelSize; i++) row[i] = 1.0 / kernelSize;
            return ConvolveHorizontal(src, row);
        }

        /// <summary>Bilateral filter (edge-preserving smoothing).</summary>
        public static Bitmap BilateralFilter(Bitmap src, int radius = 5,
            double sigmaColor = 75, double sigmaSpace = 75)
        {
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            double sigC2 = 2 * sigmaColor * sigmaColor;
            double sigS2 = 2 * sigmaSpace * sigmaSpace;

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    data.GetPixel(x, y, out byte cr, out byte cg, out byte cb);
                    double sumR = 0, sumG = 0, sumB = 0, wSum = 0;

                    for (int ky = -radius; ky <= radius; ky++)
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int nx = Math.Clamp(x + kx, 0, src.Width - 1);
                            int ny = Math.Clamp(y + ky, 0, src.Height - 1);
                            data.GetPixel(nx, ny, out byte nr, out byte ng, out byte nb);

                            double spatialDist = kx * kx + ky * ky;
                            double colorDist = Math.Pow(nr - cr, 2) +
                                               Math.Pow(ng - cg, 2) +
                                               Math.Pow(nb - cb, 2);
                            double w = Math.Exp(-spatialDist / sigS2 - colorDist / sigC2);

                            sumR += nr * w; sumG += ng * w; sumB += nb * w;
                            wSum += w;
                        }

                    outData.SetPixel(x, y,
                        Clamp((int)(sumR / wSum)),
                        Clamp((int)(sumG / wSum)),
                        Clamp((int)(sumB / wSum)));
                }
            return result;
        }

        /// <summary>Emboss filter (relief effect).</summary>
        public static Bitmap EmbossFilter(Bitmap src)
        {
            double[,] kernel = { { -2, -1, 0 }, { -1, 1, 1 }, { 0, 1, 2 } };
            var gray = ToGrayscale(src);
            var result = Convolve2D(gray, kernel, 1, offset: 128);
            gray.Dispose();
            return result;
        }

        /// <summary>Canny edge detection (single-channel result).</summary>
        public static Bitmap CannyEdge(Bitmap src, int lowThresh = 50, int highThresh = 150)
        {
            // 1. Grayscale
            var gray = ToGrayscale(src);
            // 2. Gaussian blur
            var blurred = GaussianBlur(gray, 5);
            gray.Dispose();
            // 3. Sobel gradients
            double[,] kx = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            double[,] ky = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            int w = src.Width, h = src.Height;
            double[,] mag = new double[h, w];
            double[,] dir = new double[h, w];

            using (var bd = new BitmapLocker(blurred, ImageLockMode.ReadOnly))
            {
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                    {
                        double gx = 0, gy = 0;
                        for (int ky2 = -1; ky2 <= 1; ky2++)
                            for (int kx2 = -1; kx2 <= 1; kx2++)
                            {
                                bd.GetPixel(x + kx2, y + ky2, out byte pv, out _, out _);
                                gx += pv * kx[ky2 + 1, kx2 + 1];
                                gy += pv * ky[ky2 + 1, kx2 + 1];
                            }
                        mag[y, x] = Math.Sqrt(gx * gx + gy * gy);
                        dir[y, x] = Math.Atan2(gy, gx) * 180.0 / Math.PI;
                    }
            }
            blurred.Dispose();

            // 4. Non-maximum suppression
            double[,] thin = new double[h, w];
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    double ang = dir[y, x] % 180;
                    if (ang < 0) ang += 180;
                    double q = 255, r = 255;
                    if (ang < 22.5 || ang >= 157.5)         { q = mag[y, x + 1]; r = mag[y, x - 1]; }
                    else if (ang < 67.5)                     { q = mag[y + 1, x - 1]; r = mag[y - 1, x + 1]; }
                    else if (ang < 112.5)                    { q = mag[y + 1, x]; r = mag[y - 1, x]; }
                    else                                     { q = mag[y - 1, x - 1]; r = mag[y + 1, x + 1]; }
                    thin[y, x] = (mag[y, x] >= q && mag[y, x] >= r) ? mag[y, x] : 0;
                }

            // 5. Double threshold + hysteresis
            var result = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    byte val = thin[y, x] >= highThresh ? (byte)255 :
                               thin[y, x] >= lowThresh  ? (byte)128 : (byte)0;
                    // simple hysteresis: keep weak pixel only if next to strong
                    if (val == 128)
                    {
                        bool near = false;
                        for (int ny = -1; ny <= 1 && !near; ny++)
                            for (int nx = -1; nx <= 1 && !near; nx++)
                            {
                                int cy2 = y + ny, cx2 = x + nx;
                                if (cy2 >= 0 && cy2 < h && cx2 >= 0 && cx2 < w)
                                    if (thin[cy2, cx2] >= highThresh) near = true;
                            }
                        val = near ? (byte)255 : (byte)0;
                    }
                    outData.SetPixel(x, y, val, val, val);
                }
            return result;
        }

        /// <summary>Multiply each pixel by alpha (contrast scaling).</summary>
        public static Bitmap AdjustContrast(Bitmap src, double alpha)
        {
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    data.GetPixel(x, y, out byte r, out byte g, out byte b);
                    outData.SetPixel(x, y,
                        Clamp((int)(r * alpha)),
                        Clamp((int)(g * alpha)),
                        Clamp((int)(b * alpha)));
                }
            return result;
        }

        // ═══════════════════════ KERNEL HELPERS ══════════════════════════

        private static double[] BuildGaussianKernel(int size, double sigma)
        {
            double[] k = new double[size];
            int half = size / 2;
            double sum = 0;
            for (int i = -half; i <= half; i++)
            {
                k[i + half] = Math.Exp(-(i * i) / (2 * sigma * sigma));
                sum += k[i + half];
            }
            for (int i = 0; i < size; i++) k[i] /= sum;
            return k;
        }

        private static Bitmap ConvolveHorizontal(Bitmap src, double[] kernel)
        {
            int half = kernel.Length / 2;
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    double sr = 0, sg = 0, sb = 0;
                    for (int k = -half; k <= half; k++)
                    {
                        int nx = Math.Clamp(x + k, 0, src.Width - 1);
                        data.GetPixel(nx, y, out byte r, out byte g, out byte b);
                        double w = kernel[k + half];
                        sr += r * w; sg += g * w; sb += b * w;
                    }
                    outData.SetPixel(x, y, Clamp((int)sr), Clamp((int)sg), Clamp((int)sb));
                }
            return result;
        }

        private static Bitmap ConvolveVertical(Bitmap src, double[] kernel)
        {
            int half = kernel.Length / 2;
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    double sr = 0, sg = 0, sb = 0;
                    for (int k = -half; k <= half; k++)
                    {
                        int ny = Math.Clamp(y + k, 0, src.Height - 1);
                        data.GetPixel(x, ny, out byte r, out byte g, out byte b);
                        double w = kernel[k + half];
                        sr += r * w; sg += g * w; sb += b * w;
                    }
                    outData.SetPixel(x, y, Clamp((int)sr), Clamp((int)sg), Clamp((int)sb));
                }
            return result;
        }

        private static Bitmap Convolve2D(Bitmap src, double[,] kernel, int halfSize, int offset = 0)
        {
            var result = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using var data = new BitmapLocker(src, ImageLockMode.ReadOnly);
            using var outData = new BitmapLocker(result, ImageLockMode.WriteOnly);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    double sr = 0, sg = 0, sb = 0;
                    for (int ky = -halfSize; ky <= halfSize; ky++)
                        for (int kx = -halfSize; kx <= halfSize; kx++)
                        {
                            int nx = Math.Clamp(x + kx, 0, src.Width - 1);
                            int ny = Math.Clamp(y + ky, 0, src.Height - 1);
                            data.GetPixel(nx, ny, out byte r, out byte g, out byte b);
                            double w = kernel[ky + halfSize, kx + halfSize];
                            sr += r * w; sg += g * w; sb += b * w;
                        }
                    outData.SetPixel(x, y,
                        Clamp((int)sr + offset),
                        Clamp((int)sg + offset),
                        Clamp((int)sb + offset));
                }
            return result;
        }

        private static int EnsureOdd(int v) => v % 2 == 0 ? v + 1 : v;
        private static byte Clamp(int v) => (byte)Math.Clamp(v, 0, 255);
    }

    // ═══════════════════════ FAST PIXEL ACCESS ════════════════════════

    /// <summary>
    /// Lock/unlock wrapper for fast unsafe pixel access via Marshal.
    /// Supports 24bpp and 32bpp bitmaps.
    /// </summary>
    internal sealed class BitmapLocker : IDisposable
    {
        private readonly Bitmap _bmp;
        private readonly BitmapData _data;
        private readonly byte[] _bytes;
        private readonly int _stride;
        private readonly int _bpp;
        private bool _disposed;

        public BitmapLocker(Bitmap bmp, ImageLockMode mode)
        {
            _bmp = bmp;
            _data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                  mode, bmp.PixelFormat);
            _stride = Math.Abs(_data.Stride);
            _bpp = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            int byteCount = _stride * bmp.Height;
            _bytes = new byte[byteCount];
            if (mode != ImageLockMode.WriteOnly)
                Marshal.Copy(_data.Scan0, _bytes, 0, byteCount);
        }

        public void GetPixel(int x, int y, out byte r, out byte g, out byte b)
        {
            int idx = y * _stride + x * _bpp;
            b = _bytes[idx];
            g = _bytes[idx + 1];
            r = _bytes[idx + 2];
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            int idx = y * _stride + x * _bpp;
            _bytes[idx] = b;
            _bytes[idx + 1] = g;
            _bytes[idx + 2] = r;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Marshal.Copy(_bytes, 0, _data.Scan0, _bytes.Length);
            _bmp.UnlockBits(_data);
            _disposed = true;
        }
    }
}
