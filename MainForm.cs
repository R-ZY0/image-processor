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
            Size = new Size(1200, 700);
            BackColor = Color.FromArgb(20, 20, 40);

            // 🔝 Top Bar
            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(30, 30, 60)
            };

            Label title = new Label
            {
                Text = "🖼 Image Processor",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 10)
            };

            topBar.Controls.Add(title);

            // الصور
            picOriginal = new PictureBox
            {
                Dock = DockStyle.Left,
                Width = 500,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            picProcessed = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Sidebar
            panel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Color.FromArgb(25, 25, 50),
                AutoScroll = true,
                Padding = new Padding(8)
            };

            // Progress
            progress = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            // Cancel
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

            // 🔹 Sections
            AddSection("📂 FILE");
            AddButton("Open Image", BtnOpen_Click);
            AddButton("Histogram", BtnHist_Click);

            AddSection("🎨 COLORS");
            AddButton("Red", (s, e) => ApplyFilter(ImageFilters.ToRed));
            AddButton("Green", (s, e) => ApplyFilter(ImageFilters.ToGreen));
            AddButton("Blue", (s, e) => ApplyFilter(ImageFilters.ToBlue));
            AddButton("Grayscale", (s, e) => ApplyFilter(ImageFilters.ToGrayscale));

            AddSection("✨ FILTERS");
            AddButton("Gaussian", (s, e) => ApplyFilter(img => ImageFilters.GaussianBlur(img, 15)));
            AddButton("Sobel", async (s, e) => await SafeFilter(img => ImageFilters.SobelEdge(img)));
            AddButton("Canny", (s, e) => ApplyFilter(img => ImageFilters.CannyEdge(img, 50, 150)));
            AddButton("Median", (s, e) => ApplyFilter(img => ImageFilters.MedianFilter(img, 3)));
            AddButton("Sharpen", (s, e) => ApplyFilter(ImageFilters.SharpenFilter));
            AddButton("Laplacian", (s, e) => ApplyFilter(ImageFilters.LaplacianFilter));
            AddButton("Bilateral", (s, e) => ApplyFilter(img => ImageFilters.BilateralFilter(img)));

            AddSection("🎚 ADJUST");
            AddButton("Contrast", (s, e) => ApplyFilter(img => ImageFilters.AdjustContrast(img, 1.5)));
            AddButton("Brightness", (s, e) => ApplyFilter(img => ImageFilters.AdjustBrightness(img, 30)));
            AddButton("Negative", (s, e) => ApplyFilter(ImageFilters.Negative));

            Controls.Add(picProcessed);
            Controls.Add(picOriginal);
            Controls.Add(panel);
            Controls.Add(topBar);
            Controls.Add(progress);
            Controls.Add(btnCancel);
        }

        // 🔹 Section Title
        void AddSection(string text)
        {
            Label lbl = new Label
            {
                Text = text,
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Height = 25,
                Dock = DockStyle.Top
            };

            panel.Controls.Add(lbl);
        }

        // 🔹 Button Style
        void AddButton(string text, EventHandler action)
        {
            Button btn = new Button
            {
                Text = text,
                Height = 40,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 80),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(5)
            };

            btn.FlatAppearance.BorderSize = 0;

            btn.MouseEnter += (s, e) =>
                btn.BackColor = Color.FromArgb(80, 80, 130);

            btn.MouseLeave += (s, e) =>
                btn.BackColor = Color.FromArgb(45, 45, 80);

            btn.Click += action;

            panel.Controls.Add(btn);
        }

        // باقي الكود زي ما هو (ApplyFilter / SafeFilter / Resize / Events)

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


            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Open Image",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };

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