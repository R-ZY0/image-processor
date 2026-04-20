using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessor
{
    public class HistogramForm : Form
    {
        private Bitmap original, processed;

        public HistogramForm(Bitmap o, Bitmap p)
        {
            original = o;
            processed = p;

            this.Text = "Histogram";
            this.Size = new Size(900, 400);
            this.Paint += OnPaint;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            DrawHistogram(e.Graphics, original, new Rectangle(20, 20, 350, 300));
            DrawHistogram(e.Graphics, processed, new Rectangle(450, 20, 350, 300));
        }

        private void DrawHistogram(Graphics g, Bitmap bmp, Rectangle area)
{
    int[] hr = new int[256];
    int[] hg = new int[256];
    int[] hb = new int[256];

    // حساب القيم
    for (int y = 0; y < bmp.Height; y++)
        for (int x = 0; x < bmp.Width; x++)
        {
            Color c = bmp.GetPixel(x, y);
            hr[c.R]++;
            hg[c.G]++;
            hb[c.B]++;
        }

    int max = 1;
    for (int i = 0; i < 256; i++)
        max = Math.Max(max, Math.Max(hr[i], Math.Max(hg[i], hb[i])));
    
     g.FillRectangle(Brushes.Black, area);
    // رسم القنوات
    for (int i = 0; i < 256; i++)
    {
        float x = area.Left + i * area.Width / 256f;

        float rH = (float)hr[i] / max * area.Height;
        float gH = (float)hg[i] / max * area.Height;
        float bH = (float)hb[i] / max * area.Height;

        // 🔴 Red
        g.DrawLine(new Pen(Color.FromArgb(180, 255, 0, 0)),
                   x, area.Bottom, x, area.Bottom - rH);

        // 🟢 Green
        g.DrawLine(new Pen(Color.FromArgb(180, 0, 255, 0)),
                   x, area.Bottom, x, area.Bottom - gH);

        // 🔵 Blue
        g.DrawLine(new Pen(Color.FromArgb(180, 0, 0, 255)),
                   x, area.Bottom, x, area.Bottom - bH);
    }

    g.DrawRectangle(Pens.Gray, area);
}
    }
}