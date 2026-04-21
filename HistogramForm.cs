using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessor
{
    public class HistogramForm : Form
    {
        private Bitmap original;
        private Bitmap processed;
        private Bitmap equalized;

        public HistogramForm(Bitmap orig, Bitmap proc)
        {
            original = orig;
            processed = proc;

            // نعمل Equalization أول ما الفورم يفتح
            equalized = HistogramEqualization(proc);

            this.Text = "Histogram Viewer";
            this.Size = new Size(1100, 650);
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;

            g.Clear(Color.FromArgb(18, 18, 30));

            // ───── Grayscale Histogram ─────
            DrawGrayHistogram(g, processed, "Grayscale Histogram",
                new Rectangle(20, 30, 320, 200));

            // ───── Equalized Histogram ─────
            DrawGrayHistogram(g, equalized, "Equalized Histogram",
                new Rectangle(380, 30, 320, 200));

            // ───── RGB ─────
            DrawChannel(g, processed, "Red", new Rectangle(20, 270, 300, 250), 0);
            DrawChannel(g, processed, "Green", new Rectangle(360, 270, 300, 250), 1);
            DrawChannel(g, processed, "Blue", new Rectangle(700, 270, 300, 250), 2);
        }

        // ───── Gray Histogram ─────
        void DrawGrayHistogram(Graphics g, Bitmap bmp, string title, Rectangle area)
        {
            int[] hist = new int[256];

            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    int gray = (c.R + c.G + c.B) / 3;
                    hist[gray]++;
                }

            int max = 1;
            foreach (var v in hist)
                if (v > max) max = v;

            g.FillRectangle(new SolidBrush(Color.FromArgb(25, 25, 45)), area);
            g.DrawRectangle(Pens.Gray, area);

            g.DrawString(title, new Font("Segoe UI", 10, FontStyle.Bold),
                Brushes.White, area.Left, area.Top - 20);

            using (Pen pen = new Pen(Color.White))
            {
                for (int i = 0; i < 256; i++)
                {
                    float x = area.Left + i * area.Width / 256f;
                    float h = (float)hist[i] / max * area.Height;

                    g.DrawLine(pen, x, area.Bottom, x, area.Bottom - h);
                }
            }
        }

        // ───── RGB Channels ─────
        void DrawChannel(Graphics g, Bitmap bmp, string title, Rectangle area, int ch)
        {
            if (bmp == null) return;

            // 🔥 الحل هنا (يمنع التهنيج)
            bmp = new Bitmap(bmp, new Size(300, 300));

            int[] hist = new int[256];

            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    int val = ch == 0 ? c.R : ch == 1 ? c.G : c.B;
                    hist[val]++;
                }

            int max = 1;
            foreach (var v in hist)
                if (v > max) max = v;

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(25, 25, 45)))
                g.FillRectangle(bg, area);

            g.DrawRectangle(Pens.Gray, area);

            g.DrawString(title, new Font("Segoe UI", 10, FontStyle.Bold),
                Brushes.White, area.Left, area.Top - 20);

            Color color = ch == 0 ? Color.Red : ch == 1 ? Color.Lime : Color.Blue;

            using (Pen pen = new Pen(color))
            {
                for (int i = 0; i < 256; i++)
                {
                    float x = area.Left + i * area.Width / 256f;
                    float h = (float)hist[i] / max * area.Height;

                    g.DrawLine(pen, x, area.Bottom, x, area.Bottom - h);
                }
            }
        }

        // ───── Equalization ─────
        Bitmap HistogramEqualization(Bitmap img)
        {
            Bitmap gray = ToGray(img);

            int width = gray.Width;
            int height = gray.Height;

            int[] hist = new int[256];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    hist[gray.GetPixel(x, y).R]++;

            float[] cdf = new float[256];
            cdf[0] = hist[0];

            for (int i = 1; i < 256; i++)
                cdf[i] = cdf[i - 1] + hist[i];

            float total = width * height;

            Bitmap result = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int val = gray.GetPixel(x, y).R;
                    int newVal = (int)(cdf[val] / total * 255);
                    result.SetPixel(x, y, Color.FromArgb(newVal, newVal, newVal));
                }

            return result;
        }

        Bitmap ToGray(Bitmap img)
        {
            Bitmap res = new Bitmap(img.Width, img.Height);

            for (int y = 0; y < img.Height; y++)
                for (int x = 0; x < img.Width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int g = (c.R + c.G + c.B) / 3;
                    res.SetPixel(x, y, Color.FromArgb(g, g, g));
                }

            return res;
        }
    }
}