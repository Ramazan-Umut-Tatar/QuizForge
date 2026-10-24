#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Tema renklerini alan, köşeleri yuvarlak ilerleme çubuğu (ProgressBar yerel yeşil rengini
    /// değiştirmeye izin vermediği için). BackColor = iz, DolguRengi = dolgu.
    /// </summary>
    public sealed class Bar : Control
    {
        private int _deger;
        private int _maks = 100;
        private Color _dolgu = Color.SteelBlue;

        public Bar()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            TabStop = false;
            BackColor = Color.Gray;
            Height = 14;
        }

        public int Deger
        {
            get => _deger;
            set { _deger = Math.Max(0, value); Invalidate(); }
        }

        public int Maks
        {
            get => _maks;
            set { _maks = Math.Max(1, value); Invalidate(); }
        }

        public Color DolguRengi
        {
            get => _dolgu;
            set { _dolgu = value; Invalidate(); }
        }

        protected override Size DefaultSize => new Size(200, 14);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var alan = new Rectangle(0, 0, Width - 1, Height - 1);
            if (alan.Width < 2 || alan.Height < 2) return;

            int yaricap = alan.Height / 2;

            using (GraphicsPath iz = Yuvarlak(alan, yaricap))
            using (var firca = new SolidBrush(BackColor))
                e.Graphics.FillPath(firca, iz);

            if (_deger <= 0) return;

            int genislik = Math.Max(alan.Height, (int)((long)alan.Width * Math.Min(_deger, _maks) / _maks));
            var dolgu = new Rectangle(alan.X, alan.Y, Math.Min(genislik, alan.Width), alan.Height);

            using (GraphicsPath yol = Yuvarlak(dolgu, yaricap))
            using (var firca = new SolidBrush(_dolgu))
                e.Graphics.FillPath(firca, yol);
        }

        private static GraphicsPath Yuvarlak(Rectangle r, int yaricap)
        {
            int cap = Math.Max(1, yaricap * 2);
            var yol = new GraphicsPath();

            yol.AddArc(r.X, r.Y, cap, cap, 180, 90);
            yol.AddArc(r.Right - cap, r.Y, cap, cap, 270, 90);
            yol.AddArc(r.Right - cap, r.Bottom - cap, cap, cap, 0, 90);
            yol.AddArc(r.X, r.Bottom - cap, cap, cap, 90, 90);
            yol.CloseFigure();

            return yol;
        }
    }

    /// <summary>Formlarda tekrar eden buton, kart ve etiket oluşturma kodunu tek yerde toplar.</summary>
    public static class ArayuzYardimcisi
    {
        public static Button Dugme(string metin, bool birincil = false)
        {
            return new Button
            {
                Text = metin,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 5, 12, 5),
                Margin = new Padding(4),
                Tag = birincil ? TemaUygulayici.Birincil : null
            };
        }

        /// <summary>Tema tarafından yüzey rengine boyanan kart paneli.</summary>
        public static TableLayoutPanel Kart(int sutun = 1, bool otomatikBoyut = true)
        {
            return new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = sutun,
                Padding = new Padding(14),
                Margin = new Padding(0, 0, 0, 10),
                AutoSize = otomatikBoyut,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Tag = TemaUygulayici.Kart
            };
        }

        public static Label Etiket(string metin, string? tur = null)
        {
            return new Label
            {
                Text = metin,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
                Tag = tur
            };
        }

        /// <summary>Bölüm başlığı (vurgu rengi, kalın).</summary>
        public static Label BolumBasligi(string metin)
        {
            Label etiket = Etiket(metin, TemaUygulayici.Baslik);
            etiket.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            etiket.Margin = new Padding(0, 0, 0, 8);
            return etiket;
        }
    }
}
