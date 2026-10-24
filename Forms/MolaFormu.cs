#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuizForge
{
    public enum MolaKarari
    {
        Devam,
        MolaVerdi,
        Bitir
    }

    /// <summary>Yorgunluk belirtisi görülünce mola önerir ve istenirse 5 dakikalık geri sayım yapar.</summary>
    public sealed class MolaFormu : Form
    {
        private const int MolaSaniyesi = 300;

        private readonly System.Windows.Forms.Timer _sayac = new System.Windows.Forms.Timer { Interval = 1000 };
        private readonly Label _lblGeriSayim = new Label();
        private readonly TableLayoutPanel _oneriPaneli = new TableLayoutPanel();
        private readonly TableLayoutPanel _molaPaneli = new TableLayoutPanel();
        private int _kalanSaniye = MolaSaniyesi;

        public MolaKarari Karar { get; private set; } = MolaKarari.Devam;

        public MolaFormu(YorgunlukUyarisi uyari, UygulamaAyarlari ayarlar)
        {
            Text = "Mola zamanı mı?";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 250);

            // --- Öneri paneli ---
            var mesaj = new Label
            {
                Dock = DockStyle.Fill,
                Text =
                    $"Son sorularda doğruluğun %{uyari.IlkDogruluk * 100:0} → %{uyari.SonDogruluk * 100:0} seviyesine düştü " +
                    $"ve cevap sürelerin {uyari.IlkSureSn:0.#} sn → {uyari.SonSureSn:0.#} sn uzadı." +
                    Environment.NewLine + Environment.NewLine +
                    "Bu yorgunluk belirtisi olabilir. 5 dakikalık bir mola, öğrendiklerinin yerleşmesine yardımcı olur."
            };

            var btnMola = new Button { Text = "5 dakika mola ver", Dock = DockStyle.Fill, Margin = new Padding(4), Tag = TemaUygulayici.Birincil };
            var btnDevam = new Button { Text = "Devam et", Dock = DockStyle.Fill, Margin = new Padding(4) };
            var btnBitir = new Button { Text = "Oturumu bitir", Dock = DockStyle.Fill, Margin = new Padding(4) };

            var dugmeler = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            for (int i = 0; i < 3; i++)
                dugmeler.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            dugmeler.Controls.Add(btnMola, 0, 0);
            dugmeler.Controls.Add(btnDevam, 1, 0);
            dugmeler.Controls.Add(btnBitir, 2, 0);

            _oneriPaneli.Dock = DockStyle.Fill;
            _oneriPaneli.ColumnCount = 1;
            _oneriPaneli.Padding = new Padding(14);
            _oneriPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _oneriPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            _oneriPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            _oneriPaneli.Controls.Add(mesaj, 0, 0);
            _oneriPaneli.Controls.Add(dugmeler, 0, 1);

            // --- Mola paneli (geri sayım) ---
            _lblGeriSayim.Dock = DockStyle.Fill;
            _lblGeriSayim.TextAlign = ContentAlignment.MiddleCenter;
            _lblGeriSayim.Font = new Font(Font.FontFamily, 36f, FontStyle.Bold);
            GeriSayimiYaz();

            var ipucu = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Ekrandan uzaklaş, su iç, gözlerini dinlendir."
            };
            var btnMolaBitir = new Button { Text = "Molayı bitir", Dock = DockStyle.Fill, Margin = new Padding(4) };

            _molaPaneli.Dock = DockStyle.Fill;
            _molaPaneli.ColumnCount = 1;
            _molaPaneli.Padding = new Padding(14);
            _molaPaneli.Visible = false;
            _molaPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _molaPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            _molaPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            _molaPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            _molaPaneli.Controls.Add(_lblGeriSayim, 0, 0);
            _molaPaneli.Controls.Add(ipucu, 0, 1);
            _molaPaneli.Controls.Add(btnMolaBitir, 0, 2);

            Controls.Add(_oneriPaneli);
            Controls.Add(_molaPaneli);

            btnMola.Click += (sender, args) =>
            {
                _oneriPaneli.Visible = false;
                _molaPaneli.Visible = true;
                _sayac.Start();
            };

            btnDevam.Click += (sender, args) =>
            {
                Karar = MolaKarari.Devam;
                DialogResult = DialogResult.OK;
            };

            btnBitir.Click += (sender, args) =>
            {
                Karar = MolaKarari.Bitir;
                DialogResult = DialogResult.OK;
            };

            btnMolaBitir.Click += (sender, args) => MolayiBitir();

            _sayac.Tick += (sender, args) =>
            {
                _kalanSaniye--;
                GeriSayimiYaz();

                if (_kalanSaniye <= 0)
                {
                    System.Media.SystemSounds.Asterisk.Play();
                    MolayiBitir();
                }
            };

            new TemaUygulayici(ayarlar).Uygula(this);
        }

        private void MolayiBitir()
        {
            _sayac.Stop();
            Karar = MolaKarari.MolaVerdi;
            DialogResult = DialogResult.OK;
        }

        private void GeriSayimiYaz()
        {
            _lblGeriSayim.Text = $"{_kalanSaniye / 60:00}:{_kalanSaniye % 60:00}";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _sayac.Stop();
            _sayac.Dispose();
            base.OnFormClosed(e);
        }
    }
}
