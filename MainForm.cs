using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ImageProcessor
{
    public class MainForm : Form
    {
        PictureBox picOriginal, picProcessed;
        Panel panel;
        ProgressBar progress;
        Button btnCancel;

        Bitmap original, processed;

        bool isProcessing = false;
        bool cancelRequested = false;

        public MainForm()
        {
            Text = "Image Processor";
            Size = new Size(1100, 650);
            BackColor = Color.FromArgb(20, 20, 40);

            // الصور
            picOriginal = new PictureBox
            {
                Dock = DockStyle.Left,
                Width = 500,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            picProcessed = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            // Sidebar
            panel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(30, 30, 60),
                AutoScroll = true
            };

            // Progress bar
            progress = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            // Cancel button
            btnCancel = new Button
            {
                Text = "❌ Cancel",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.DarkRed,
                ForeColor = Color.White,
                Visible = false
            };
            btnCancel.Click += (s, e) => cancelRequested = true;

            // أزرار
            AddButton("📂 Open", BtnOpen_Click);
            AddButton("🎨 Grayscale", (s,e)=>ApplyFilter(ImageFilters.ToGrayscale));
            AddButton("📊 Histogram", BtnHist_Click);

            AddButton("🌫 Gaussian", (s,e)=>ApplyFilter(img => ImageFilters.GaussianBlur(img, 15)));
            AddButton("📐 Sobel", async (s,e)=>await SafeFilter(img => ImageFilters.SobelEdge(img)));
            AddButton("🔍 Canny", (s,e)=>ApplyFilter(img => ImageFilters.CannyEdge(img, 50, 150)));
            AddButton("🔳 Median", (s,e)=>ApplyFilter(img => ImageFilters.MedianFilter(img, 3)));
            AddButton("✨ Sharpen", (s,e)=>ApplyFilter(ImageFilters.SharpenFilter));
            AddButton("🌀 Laplacian", (s,e)=>ApplyFilter(ImageFilters.LaplacianFilter));
            AddButton("🌊 Bilateral", (s,e)=>ApplyFilter(img => ImageFilters.BilateralFilter(img)));
            AddButton("🎚 Contrast", (s,e)=>ApplyFilter(img => ImageFilters.AdjustContrast(img, 1.5)));
            AddButton("☀ Brightness", (s,e)=>ApplyFilter(img => ImageFilters.AdjustBrightness(img, 30)));
            AddButton("🔲 Negative", (s,e)=>ApplyFilter(ImageFilters.Negative));

            Controls.Add(picProcessed);
            Controls.Add(picOriginal);
            Controls.Add(panel);
            Controls.Add(progress);
            Controls.Add(btnCancel);
        }

        // زرار جاهز
        void AddButton(string text, EventHandler action)
        {
            Button btn = new Button
            {
                Text = text,
                Height = 45,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 90),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(80, 80, 130);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(50, 50, 90);

            btn.Click += action;
            panel.Controls.Add(btn);
        }

        // 🔥 تصغير الصورة (سرعة أعلى)
        Bitmap Resize(Bitmap img, int max = 800)
        {
            if (img.Width <= max && img.Height <= max)
                return (Bitmap)img.Clone();

            float scale = Math.Min((float)max / img.Width, (float)max / img.Height);

            return new Bitmap(img, new Size(
                (int)(img.Width * scale),
                (int)(img.Height * scale)
            ));
        }

        // 🔥 تطبيق فلتر (سريع + آمن)
        async void ApplyFilter(Func<Bitmap, Bitmap> filter)
        {
            if (original == null || isProcessing) return;

            isProcessing = true;
            cancelRequested = false;

            progress.Visible = true;
            btnCancel.Visible = true;

            Cursor = Cursors.WaitCursor;

            try
            {
                Bitmap src = Resize(original); // 👈 السرعة هنا

                Bitmap result = await Task.Run(() =>
                {
                    if (cancelRequested) return null;
                    return filter(src);
                });

                if (!cancelRequested && result != null)
                {
                    processed?.Dispose();
                    processed = result;
                    picProcessed.Image = processed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            Cursor = Cursors.Default;
            progress.Visible = false;
            btnCancel.Visible = false;
            isProcessing = false;
        }
        async Task SafeFilter(Func<Bitmap, Bitmap> filter)
        {
            if (original == null || isProcessing) return;

            isProcessing = true;
            cancelRequested = false;

            progress.Visible = true;
            btnCancel.Visible = true;
            Cursor = Cursors.WaitCursor;

            try
            {
                // ⚡ تصغير أكتر عشان Sobel
                Bitmap src = Resize(original, 500);

                Bitmap result = await Task.Run(() =>
                {
                    try
                    {
                        if (cancelRequested) return null;
                        return filter(src);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Filter Error: " + ex.Message);
                        return null;
                    }
                });

                if (!cancelRequested && result != null)
                {
                    processed?.Dispose();
                    processed = result;
                    picProcessed.Image = processed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Crash: " + ex.Message);
            }

            Cursor = Cursors.Default;
            progress.Visible = false;
            btnCancel.Visible = false;
            isProcessing = false;
        }

        // Events
        private void BtnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                original = new Bitmap(dlg.FileName);
                processed = (Bitmap)original.Clone();

                picOriginal.Image = original;
                picProcessed.Image = processed;
            }
        }

        private void BtnHist_Click(object sender, EventArgs e)
        {
            if (original == null || processed == null) return;
            new HistogramForm(original, processed).Show();
        }
    }
}