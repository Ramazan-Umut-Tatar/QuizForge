using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizForge
{
    public partial class Form1 : Form
    {
        private readonly KelimeDeposu _depo = new KelimeDeposu(Application.StartupPath);
        private readonly IstatistikDeposu _istatistik = new IstatistikDeposu();
        private readonly UygulamaAyarlari _ayarlar = AyarDeposu.Yukle();
        private readonly TemaUygulayici _tema = new TemaUygulayici();
        private List<KelimeKarti> _kartlar = new List<KelimeKarti>();

        // Kodla eklenen kontroller (Designer dosyasına dokunulmaz)
        private readonly CheckedListBox _dosyaListesi = new CheckedListBox();
        private readonly Label _lblDurum = new Label();
        private readonly ComboBox _cmbMod = new ComboBox();
        private readonly CheckBox _chkTekrar = new CheckBox();
        private readonly CheckBox _chkKaranlik = new CheckBox();
        private readonly NumericUpDown _nudFont = new NumericUpDown();
        private readonly Button _btnIstatistik = new Button();

        public Form1()
        {
            InitializeComponent();
            ArayuzuKur();
            this.FormClosing += (s, e) =>
            {
                AyarlariTopla();
                _istatistik.Kaydet();
            };
        }

        /// <summary>
        /// Designer'daki sabit konumlu yerleşimi, pencereyle birlikte büyüyen bir TableLayoutPanel düzenine taşır.
        /// button1/2/3 aynı kontroller olarak kalır; olayları değişmez.
        /// </summary>
        private void ArayuzuKur()
        {
            SuspendLayout();
            Controls.Clear();
            MinimumSize = new Size(540, 580);

            var baslik = new Label
            {
                Text = "Çalışılacak dosyalar (birden fazla seçebilirsiniz):",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            _dosyaListesi.Dock = DockStyle.Fill;
            _dosyaListesi.CheckOnClick = true;
            _dosyaListesi.IntegralHeight = false;

            _lblDurum.AutoSize = true;
            _lblDurum.Margin = new Padding(0, 6, 0, 6);

            _cmbMod.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbMod.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _cmbMod.Items.AddRange(new object[] { "Şıklı (çoktan seçmeli)", "Yazarak cevap" });

            _chkTekrar.Text = "Yanlış yapılan kelimeleri oturum içinde tekrar sor";
            _chkTekrar.AutoSize = true;
            _chkKaranlik.Text = "Karanlık tema";
            _chkKaranlik.AutoSize = true;

            _nudFont.Minimum = 80;
            _nudFont.Maximum = 200;
            _nudFont.Increment = 10;
            _nudFont.Value = 100;
            _nudFont.Anchor = AnchorStyles.Left;

            var secenekler = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            secenekler.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            secenekler.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            secenekler.Controls.Add(Etiket("Soru türü:"), 0, 0);
            secenekler.Controls.Add(_cmbMod, 1, 0);
            secenekler.Controls.Add(_chkTekrar, 0, 1);
            secenekler.SetColumnSpan(_chkTekrar, 2);
            secenekler.Controls.Add(_chkKaranlik, 0, 2);
            secenekler.SetColumnSpan(_chkKaranlik, 2);
            secenekler.Controls.Add(Etiket("Yazı boyutu (%):"), 0, 3);
            secenekler.Controls.Add(_nudFont, 1, 3);

            var butonlar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };
            for (int i = 0; i < 4; i++)
                butonlar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            Button[] sirali = { button1, button2, _btnIstatistik, button3 };
            for (int i = 0; i < sirali.Length; i++)
            {
                sirali[i].Dock = DockStyle.Fill;
                sirali[i].Margin = new Padding(4);
                sirali[i].MinimumSize = new Size(0, 44);
                butonlar.Controls.Add(sirali[i], i, 0);
            }

            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12)
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(baslik, 0, 0);
            kok.Controls.Add(_dosyaListesi, 0, 1);
            kok.Controls.Add(_lblDurum, 0, 2);
            kok.Controls.Add(secenekler, 0, 3);
            kok.Controls.Add(butonlar, 0, 4);

            Controls.Add(kok);

            // Designer olayı (comboBox1_SelectedIndexChanged) bozulmasın diye kontrol korunur ama gizlenir.
            comboBox1.Visible = false;
            Controls.Add(comboBox1);

            ResumeLayout(true);
        }

        private static Label Etiket(string metin) => new Label
        {
            Text = metin,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 8, 6)
        };

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
            this.Text = "Kelime Ezber";
            button1.Text = "Yükle";
            button2.Text = "Başlat";
            button3.Text = "Çıkış";
            _btnIstatistik.Text = "İstatistik";

            foreach (string dosya in _depo.DosyaIsimleriniGetir())
                _dosyaListesi.Items.Add(dosya, _ayarlar.SonSecilenDosyalar.Contains(dosya));

            if (_dosyaListesi.Items.Count == 0)
                _lblDurum.Text = "Uygulama klasöründe .txt dosyası bulunamadı.";
            else if (_dosyaListesi.CheckedItems.Count == 0)
                _dosyaListesi.SetItemChecked(0, true);

            _cmbMod.SelectedIndex = Math.Clamp((int)_ayarlar.Mod, 0, _cmbMod.Items.Count - 1);
            _chkTekrar.Checked = _ayarlar.YanlisiTekrarSor;
            _chkKaranlik.Checked = _ayarlar.KaranlikTema;
            _nudFont.Value = Math.Clamp(_ayarlar.FontOlcegiYuzde, 80, 200);

            // Değerler yerleştikten sonra olayları bağla (açılışta gereksiz tetiklenmesin)
            _dosyaListesi.ItemCheck += (s, args) => DosyalarDegisti();
            _chkKaranlik.CheckedChanged += (s, args) => GorunumuGuncelle();
            _nudFont.ValueChanged += (s, args) => GorunumuGuncelle();
            _btnIstatistik.Click += (s, args) => IstatistikGoster();

            _tema.Ayarla(_ayarlar.KaranlikTema, _ayarlar.FontOlcegiYuzde);
            _tema.Uygula(this);
        }

        // Yükle
        private void button1_Click(object sender, EventArgs e)
        {
            List<string> secilenler = _dosyaListesi.CheckedItems.Cast<string>().ToList();
            if (secilenler.Count == 0)
            {
                MessageBox.Show("Lütfen en az bir dosya seçin.");
                button2.Enabled = false;
                return;
            }

            YuklemeSonucu sonuc = _depo.Yukle(secilenler);
            _kartlar = sonuc.Kartlar;
            _ayarlar.SonSecilenDosyalar = secilenler;

            if (_kartlar.Count == 0)
            {
                _lblDurum.Text = "Seçilen dosyalarda geçerli kelime bulunamadı.";
                button2.Enabled = false;
            }
            else
            {
                string uyariMetni = sonuc.Uyarilar.Count > 0 ? $"  •  {sonuc.Uyarilar.Count} uyarı" : string.Empty;
                _lblDurum.Text = $"{_kartlar.Count} kelime yüklendi ({secilenler.Count} dosya){uyariMetni}";
                button2.Enabled = true;
            }

            if (sonuc.Uyarilar.Count > 0)
                UyarilariGoster(sonuc.Uyarilar);
        }

        // Başlat
        private void button2_Click(object sender, EventArgs e)
        {
            if (_kartlar.Count < 2)
            {
                MessageBox.Show("Quiz için en az 2 kelime gerekir.");
                return;
            }

            AyarlariTopla();

            using (var form2 = new Form2(_kartlar, _ayarlar, _istatistik))
            {
                form2.ShowDialog(this);
            }
        }

        // Çıkış
        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Designer'daki eski ComboBox olayı (artık gizli); bozulmasın diye korunuyor.
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Enabled = false;
        }

        // ------------------------------------------------------------------

        private void DosyalarDegisti()
        {
            button2.Enabled = false;
            _kartlar = new List<KelimeKarti>();
            _lblDurum.Text = "Dosya seçimi değişti. Devam etmek için Yükle'ye basın.";
        }

        private void GorunumuGuncelle()
        {
            _ayarlar.KaranlikTema = _chkKaranlik.Checked;
            _ayarlar.FontOlcegiYuzde = (int)_nudFont.Value;
            _tema.Ayarla(_ayarlar.KaranlikTema, _ayarlar.FontOlcegiYuzde);
            _tema.Uygula(this);
        }

        private void AyarlariTopla()
        {
            if (_cmbMod.SelectedIndex >= 0)
                _ayarlar.Mod = (SoruModu)_cmbMod.SelectedIndex;

            _ayarlar.YanlisiTekrarSor = _chkTekrar.Checked;
            _ayarlar.KaranlikTema = _chkKaranlik.Checked;
            _ayarlar.FontOlcegiYuzde = (int)_nudFont.Value;
            AyarDeposu.Kaydet(_ayarlar);
        }

        private void IstatistikGoster()
        {
            using (var form = new IstatistikFormu(_istatistik, _ayarlar))
            {
                form.ShowDialog(this);
            }
        }

        private static void UyarilariGoster(List<string> uyarilar)
        {
            const int Maksimum = 12;

            string metin = string.Join(Environment.NewLine, uyarilar.Take(Maksimum).Select(u => "• " + u));
            if (uyarilar.Count > Maksimum)
                metin += Environment.NewLine + $"… ve {uyarilar.Count - Maksimum} uyarı daha.";

            MessageBox.Show(metin, "Veri doğrulama uyarıları", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
