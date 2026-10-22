using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>Kalıcı istatistiklerden "en zor 20 kelime" listesini gösterir. Tamamen kodla kurulur.</summary>
    public sealed class IstatistikFormu : Form
    {
        private const int Adet = 20;

        private readonly IstatistikDeposu _depo;
        private readonly Label _baslik = new Label();
        private readonly ListView _liste = new ListView();

        public IstatistikFormu(IstatistikDeposu depo, UygulamaAyarlari ayarlar)
        {
            _depo = depo;

            Text = "Kelime İstatistikleri";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(620, 420);
            Size = new Size(860, 540);

            _baslik.AutoSize = true;
            _baslik.Margin = new Padding(0, 0, 0, 8);
            _baslik.Font = new Font(Font, FontStyle.Bold);

            _liste.Dock = DockStyle.Fill;
            _liste.View = View.Details;
            _liste.FullRowSelect = true;
            _liste.GridLines = true;
            _liste.Columns.Add("#", 40);
            _liste.Columns.Add("Kelime", 170);
            _liste.Columns.Add("Anlam", 230);
            _liste.Columns.Add("Doğru", 60);
            _liste.Columns.Add("Yanlış", 60);
            _liste.Columns.Add("Başarı", 70);
            _liste.Columns.Add("Son görülme", 140);

            var btnSifirla = new Button { Text = "İstatistikleri sıfırla", AutoSize = true, Padding = new Padding(10, 4, 10, 4) };
            var btnKapat = new Button { Text = "Kapat", AutoSize = true, Padding = new Padding(10, 4, 10, 4), DialogResult = DialogResult.OK };
            btnSifirla.Click += (s, e) => Sifirla();

            var butonlar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            butonlar.Controls.Add(btnKapat);
            butonlar.Controls.Add(btnSifirla);

            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(_baslik, 0, 0);
            kok.Controls.Add(_liste, 0, 1);
            kok.Controls.Add(butonlar, 0, 2);

            Controls.Add(kok);
            CancelButton = btnKapat;

            Doldur();
            new TemaUygulayici(ayarlar).Uygula(this);
        }

        private void Doldur()
        {
            _liste.Items.Clear();

            var kelimeler = _depo.EnZorKelimeler(Adet);
            _baslik.Text = kelimeler.Count == 0
                ? "Henüz yanlış yapılan kelime yok."
                : $"En zor {kelimeler.Count} kelime (yanlış oranı yüksek olanlar üstte)";

            int sira = 1;
            foreach (KelimeIstatistigi k in kelimeler)
            {
                _liste.Items.Add(new ListViewItem(new[]
                {
                    sira++.ToString(),
                    k.Kelime,
                    k.Anlam,
                    k.Dogru.ToString(),
                    k.Yanlis.ToString(),
                    "%" + Math.Round(k.BasariOrani * 100).ToString(),
                    k.SonGorulme.ToString("dd.MM.yyyy HH:mm")
                }));
            }
        }

        private void Sifirla()
        {
            DialogResult cevap = MessageBox.Show(
                "Tüm kelime istatistikleri silinecek. Emin misiniz?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (cevap != DialogResult.Yes) return;

            _depo.Sifirla();
            Doldur();
        }
    }
}
