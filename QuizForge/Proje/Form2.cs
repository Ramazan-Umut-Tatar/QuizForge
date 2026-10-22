using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizForge
{
    public partial class Form2 : Form
    {
        private readonly UygulamaAyarlari _ayarlar;
        private readonly QuizAyarlari _quizAyar;
        private readonly IstatistikDeposu _istatistik;
        private readonly TemaUygulayici _tema;
        private readonly Button[] _butonlar;
        private readonly string _baslik;
        private QuizOturumu _oturum;

        // Kodla eklenen kontroller (Designer dosyasına dokunulmaz)
        private readonly Label _lblIlerleme = new Label();
        private readonly Label _lblPuan = new Label();
        private readonly Label _lblBilgi = new Label();
        private readonly TableLayoutPanel _sikPaneli = new TableLayoutPanel();
        private readonly TableLayoutPanel _yazPaneli = new TableLayoutPanel();
        private readonly TextBox _txtCevap = new TextBox();
        private readonly Button _btnKontrol = new Button();
        private readonly Button _btnBilmiyorum = new Button();

        public Form2(IEnumerable<KelimeKarti> kartlar, UygulamaAyarlari ayarlar, IstatistikDeposu istatistik)
        {
            InitializeComponent();

            _ayarlar = ayarlar;
            _istatistik = istatistik;
            _quizAyar = ayarlar.QuizAyarlariOlustur();
            _tema = new TemaUygulayici(ayarlar);
            _butonlar = new[] { button1, button2, button3, button4 };
            _baslik = _quizAyar.Mod == SoruModu.Sikli ? "Çoktan Seçmeli Sorular" : "Yazarak Cevap";
            _oturum = new QuizOturumu(kartlar, _quizAyar, istatistik);

            ArayuzuKur();

            this.Load += new EventHandler(Form2_Load);
            this.KeyPreview = true;
            this.KeyDown += Form2_KeyDown;
        }

        /// <summary>
        /// Designer'daki sabit konumlu kontrolleri (button1-4, listBox1-2) pencereyle büyüyen
        /// bir TableLayoutPanel düzenine taşır ve yazılı mod kontrollerini ekler.
        /// </summary>
        private void ArayuzuKur()
        {
            SuspendLayout();
            Controls.Clear();

            MinimumSize = new Size(560, 420);
            Text = _baslik;

            // --- Skor çubuğu (eski listBox1'in yerini alır; sekmelerle hizalama yerine iki etiket) ---
            var skorPaneli = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = listBox1.BackColor,
                Margin = new Padding(0, 0, 0, 8),
                Tag = TemaUygulayici.Atla
            };
            skorPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            skorPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            skorPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            SkorEtiketiHazirla(_lblIlerleme, ContentAlignment.MiddleLeft);
            SkorEtiketiHazirla(_lblPuan, ContentAlignment.MiddleRight);
            skorPaneli.Controls.Add(_lblIlerleme, 0, 0);
            skorPaneli.Controls.Add(_lblPuan, 1, 0);

            // Eski listBox1 Designer olayları için korunur ama gösterilmez.
            listBox1.Visible = false;

            // --- Kelime kutusu ---
            listBox2.Dock = DockStyle.Fill;
            listBox2.Margin = new Padding(0, 0, 0, 8);
            listBox2.IntegralHeight = false;
            listBox2.DrawMode = DrawMode.OwnerDrawFixed;
            listBox2.DrawItem += listBox2_DrawItem;
            listBox2.Resize += (s, e) => KelimeKutusuYuksekligiAyarla();
            listBox2.Tag = TemaUygulayici.Atla;

            // --- Bilgi satırı ---
            _lblBilgi.Dock = DockStyle.Fill;
            _lblBilgi.TextAlign = ContentAlignment.MiddleCenter;
            _lblBilgi.AutoEllipsis = true;

            // --- Şıklı mod paneli (2x2) ---
            _sikPaneli.Dock = DockStyle.Fill;
            _sikPaneli.ColumnCount = 2;
            _sikPaneli.RowCount = 2;
            _sikPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _sikPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _sikPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            _sikPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            for (int i = 0; i < _butonlar.Length; i++)
            {
                _butonlar[i].Dock = DockStyle.Fill;
                _butonlar[i].Margin = new Padding(4);
                _sikPaneli.Controls.Add(_butonlar[i], i % 2, i / 2);
            }

            // --- Yazılı mod paneli ---
            _yazPaneli.Dock = DockStyle.Fill;
            _yazPaneli.ColumnCount = 2;
            _yazPaneli.RowCount = 1;
            _yazPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            _yazPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _yazPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtCevap.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _txtCevap.Margin = new Padding(4);
            _txtCevap.Font = new Font("Segoe UI", 16f);
            _txtCevap.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    YazilanCevabiKontrolEt();
                }
            };
            _txtCevap.TextChanged += (s, e) =>
            {
                _txtCevap.BackColor = _tema.GirisZemin;
                _txtCevap.ForeColor = _tema.Yazi;
            };

            _btnKontrol.Text = "Kontrol Et";
            _btnKontrol.Dock = DockStyle.Fill;
            _btnKontrol.Margin = new Padding(4);
            _btnKontrol.Click += (s, e) => YazilanCevabiKontrolEt();

            _yazPaneli.Controls.Add(_txtCevap, 0, 0);
            _yazPaneli.Controls.Add(_btnKontrol, 1, 0);

            bool sikli = _quizAyar.Mod == SoruModu.Sikli;
            _sikPaneli.Visible = sikli;
            _yazPaneli.Visible = !sikli;

            var cevapAlani = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            cevapAlani.Controls.Add(_sikPaneli);
            cevapAlani.Controls.Add(_yazPaneli);

            // --- Bilmiyorum butonu ---
            _btnBilmiyorum.Text = "Bilmiyorum / Geç (cevabı göster)";
            _btnBilmiyorum.Dock = DockStyle.Fill;
            _btnBilmiyorum.Margin = new Padding(4);
            _btnBilmiyorum.Click += (s, e) => CevapSonrasiIsle(_oturum.Bilmiyorum());

            // --- Kök düzen ---
            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10)
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 12));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 22));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 8));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 12));
            kok.Controls.Add(skorPaneli, 0, 0);
            kok.Controls.Add(listBox2, 0, 1);
            kok.Controls.Add(_lblBilgi, 0, 2);
            kok.Controls.Add(cevapAlani, 0, 3);
            kok.Controls.Add(_btnBilmiyorum, 0, 4);

            Controls.Add(kok);
            Controls.Add(listBox1);

            _tema.Uygula(this);
            ResumeLayout(true);
        }

        private void SkorEtiketiHazirla(Label etiket, ContentAlignment hiza)
        {
            etiket.Dock = DockStyle.Fill;
            etiket.TextAlign = hiza;
            etiket.Font = new Font(listBox1.Font, listBox1.Font.Style);
            etiket.ForeColor = Color.Black;
            etiket.BackColor = Color.Transparent;
            etiket.Padding = new Padding(8, 0, 8, 0);
        }

        private void KelimeKutusuYuksekligiAyarla()
        {
            // Tek satırlık kutuda öğe, kutunun tamamını kaplasın; metin DrawItem'da ortalanır.
            int yukseklik = Math.Max(20, Math.Min(255, listBox2.ClientSize.Height));
            if (listBox2.ItemHeight != yukseklik)
                listBox2.ItemHeight = yukseklik;
        }

        public void Form2_Load(object? sender, EventArgs e)
        {
            KelimeKutusuYuksekligiAyarla();
            SoruyuGoster();
        }

        // ------------------------------------------------------------------
        //  Gösterim
        // ------------------------------------------------------------------

        private void SoruyuGoster()
        {
            var soru = _oturum.MevcutSoru;
            if (soru == null) return;

            Text = soru.Tekrar ? _baslik + " — Tekrar" : _baslik;
            listBox2.Items.Clear();
            listBox2.Items.Add(soru.Kart.Kelime);

            Bilgi(soru.Tekrar ? "Tekrar sorusu" : string.Empty, _tema.Yazi);

            if (_quizAyar.Mod == SoruModu.Sikli)
            {
                for (int i = 0; i < _butonlar.Length; i++)
                {
                    bool kullanilacak = i < soru.Secenekler.Count;
                    _butonlar[i].Visible = kullanilacak;
                    _butonlar[i].Enabled = kullanilacak;
                    _butonlar[i].Text = kullanilacak ? soru.Secenekler[i] : string.Empty;
                    _tema.ButonuSifirla(_butonlar[i]);
                }
            }
            else
            {
                _txtCevap.Clear();
                _txtCevap.Focus();
            }

            SkoruYaz();
        }

        private void SkoruYaz()
        {
            _lblIlerleme.Text = $"{_oturum.Cevaplanan}/{_oturum.ToplamSoru}";
            _lblPuan.Text = _oturum.Puan.ToString();
        }

        private void Bilgi(string metin, Color renk)
        {
            _lblBilgi.Text = metin;
            _lblBilgi.ForeColor = renk;
        }

        // ------------------------------------------------------------------
        //  Cevap akışı
        // ------------------------------------------------------------------

        private void SecenekTiklandi(int index)
        {
            if (_oturum.Cevapla(index) == CevapSonucu.Yanlis)
            {
                _butonlar[index].Enabled = false;
                _butonlar[index].BackColor = Color.Red;
                return;
            }

            CevapSonrasiIsle(CevapSonucu.Dogru);
        }

        private void YazilanCevabiKontrolEt()
        {
            if (_oturum.MevcutSoru == null) return;

            if (string.IsNullOrWhiteSpace(_txtCevap.Text))
            {
                _txtCevap.Focus();
                return;
            }

            CevapSonucu sonuc = _oturum.CevapYaz(_txtCevap.Text);

            if (sonuc == CevapSonucu.Yanlis)
            {
                _txtCevap.BackColor = Color.FromArgb(255, 190, 190);
                _txtCevap.ForeColor = Color.Black;
                Bilgi($"Yanlış. Kalan hak: {_oturum.KalanDeneme}", Color.IndianRed);
                _txtCevap.SelectAll();
                _txtCevap.Focus();
                return;
            }

            CevapSonrasiIsle(sonuc);
        }

        /// <summary>Soru çözüldükten (doğru ya da geçildi) sonra ekranı günceller veya oturumu bitirir.</summary>
        private void CevapSonrasiIsle(CevapSonucu sonuc)
        {
            KelimeKarti? acilan = _oturum.SonAcilanKart;

            if (_oturum.Bitti)
            {
                SkoruYaz();
                OturumuBitir();
                return;
            }

            SoruyuGoster();

            if (sonuc == CevapSonucu.Gecildi && acilan != null)
                Bilgi($"Doğru cevap ({acilan.Kelime}): {acilan.Anlam}", Color.DarkOrange);
        }

        private void OturumuBitir()
        {
            _istatistik.Kaydet();

            DialogResult sonuc;
            using (var ozet = new OzetFormu(_oturum.YanlisKayitlari, _oturum.Puan, _ayarlar))
            {
                sonuc = ozet.ShowDialog(this);
            }

            if (sonuc == DialogResult.Retry)
            {
                List<KelimeKarti> yanlisKartlar = _oturum.YanlisKayitlari.Select(k => k.Kart).ToList();
                _oturum = new QuizOturumu(yanlisKartlar, _quizAyar, _istatistik, _oturum.HavuzKartlari);
                SoruyuGoster();
                return;
            }

            this.Close();
        }

        private void Form2_KeyDown(object? sender, KeyEventArgs e)
        {
            // 1-4 tuşları şıkları seçer (yazılı modda rakam yazabilmek için devre dışı).
            if (_quizAyar.Mod != SoruModu.Sikli) return;

            int index;
            switch (e.KeyCode)
            {
                case Keys.D1: case Keys.NumPad1: index = 0; break;
                case Keys.D2: case Keys.NumPad2: index = 1; break;
                case Keys.D3: case Keys.NumPad3: index = 2; break;
                case Keys.D4: case Keys.NumPad4: index = 3; break;
                default: return;
            }

            if (_butonlar[index].Visible && _butonlar[index].Enabled)
            {
                e.Handled = true;
                _butonlar[index].PerformClick();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _istatistik.Kaydet();
            base.OnFormClosed(e);
        }

        // ------------------------------------------------------------------
        //  Designer'ın bağladığı olaylar (isimler değişmedi)
        // ------------------------------------------------------------------

        private void button1_Click(object sender, EventArgs e) => SecenekTiklandi(0);
        private void button2_Click(object sender, EventArgs e) => SecenekTiklandi(1);
        private void button3_Click(object sender, EventArgs e) => SecenekTiklandi(2);
        private void button4_Click(object sender, EventArgs e) => SecenekTiklandi(3);

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e) { }

        private void listBox2_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            string text = listBox2.Items[e.Index].ToString() ?? string.Empty;

            using (var arkaPlan = new SolidBrush(listBox2.BackColor))
            {
                e.Graphics.FillRectangle(arkaPlan, e.Bounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                text,
                listBox2.Font,
                e.Bounds,
                Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
    }
}
