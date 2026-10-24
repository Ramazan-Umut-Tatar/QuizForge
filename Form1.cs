#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizForge
{
    public partial class Form1 : Form
    {
        private readonly UygulamaServisleri _s = new UygulamaServisleri(Application.StartupPath);
        private readonly TemaUygulayici _tema = new TemaUygulayici();
        private List<KelimeKarti> _kartlar = new List<KelimeKarti>();
        private bool _doldururken;

        // Kodla eklenen kontroller (Designer dosyasına dokunulmaz)
        private readonly CheckedListBox _dosyaListesi = new CheckedListBox();
        private readonly ComboBox _cmbEtiket = new ComboBox();
        private readonly ComboBox _cmbMod = new ComboBox();
        private readonly Label _lblDurum = ArayuzYardimcisi.Etiket(string.Empty, TemaUygulayici.Soluk);
        private readonly Label _lblIpucu = ArayuzYardimcisi.Etiket(string.Empty, TemaUygulayici.Soluk);
        private readonly Button _btnBugun = ArayuzYardimcisi.Dugme("Bugünkü tekrar");
        private readonly Button _btnCift = ArayuzYardimcisi.Dugme("Karıştırdıklarım");
        private readonly Button _btnDuzenle = ArayuzYardimcisi.Dugme("Kelime düzenleyici");
        private readonly Button _btnMetin = ArayuzYardimcisi.Dugme("Metinden kelime");
        private readonly Button _btnPanel = ArayuzYardimcisi.Dugme("Öğrenme paneli");
        private readonly Button _btnAyarlar = ArayuzYardimcisi.Dugme("Ayarlar");
        private readonly NotifyIcon _tepsi = new NotifyIcon();
        private readonly System.Windows.Forms.Timer _hatirlatma = new System.Windows.Forms.Timer();

        private UygulamaAyarlari Ayarlar => _s.Ayarlar;

        public Form1()
        {
            InitializeComponent();
            ArayuzuKur();
            BildirimiKur();

            FormClosing += (s2, a2) =>
            {
                AyarlariTopla();
                _s.HepsiniKaydet();
                _tepsi.Visible = false;
                _tepsi.Dispose();
                _hatirlatma.Dispose();
            };
        }

        /// <summary>
        /// Designer'daki sabit konumlu yerleşimi, pencereyle birlikte büyüyen bir kart düzenine taşır.
        /// button1/2/3 aynı kontroller olarak kalır; olayları değişmez.
        /// </summary>
        private void ArayuzuKur()
        {
            SuspendLayout();
            Controls.Clear();
            MinimumSize = new Size(720, 680);

            // --- Başlık alanı ---
            var uygulamaAdi = new Label
            {
                Text = "QuizForge",
                AutoSize = true,
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                Tag = TemaUygulayici.Baslik,
                Margin = new Padding(0)
            };
            Label altBaslik = ArayuzYardimcisi.Etiket("Kelime ezber • ustalık merdiveni", TemaUygulayici.Soluk);

            _lblIpucu.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _lblIpucu.TextAlign = ContentAlignment.TopRight;

            var baslikAlani = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            baslikAlani.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            baslikAlani.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            baslikAlani.Controls.Add(uygulamaAdi, 0, 0);
            baslikAlani.Controls.Add(altBaslik, 0, 1);
            baslikAlani.Controls.Add(_lblIpucu, 1, 0);
            baslikAlani.SetRowSpan(_lblIpucu, 2);

            // --- Dosya kartı ---
            _dosyaListesi.Dock = DockStyle.Fill;
            _dosyaListesi.CheckOnClick = true;
            _dosyaListesi.IntegralHeight = false;
            _dosyaListesi.BorderStyle = BorderStyle.None;

            TableLayoutPanel dosyaKarti = ArayuzYardimcisi.Kart(1, false);
            dosyaKarti.RowCount = 2;
            dosyaKarti.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            dosyaKarti.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dosyaKarti.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            dosyaKarti.Controls.Add(ArayuzYardimcisi.BolumBasligi("Çalışılacak dosyalar"), 0, 0);
            dosyaKarti.Controls.Add(_dosyaListesi, 0, 1);

            // --- Seçenek kartı ---
            _cmbEtiket.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbEtiket.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _cmbEtiket.Items.Add("(Tümü)");
            _cmbEtiket.SelectedIndex = 0;

            _cmbMod.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbMod.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _cmbMod.Items.AddRange(new object[]
            {
                "Şıklı (çoktan seçmeli)",
                "Yazarak cevap",
                "Ustalık merdiveni (önerilen)"
            });

            TableLayoutPanel secenekKarti = ArayuzYardimcisi.Kart(2);
            secenekKarti.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            secenekKarti.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            secenekKarti.Controls.Add(Etiket("Etiket filtresi:"), 0, 0);
            secenekKarti.Controls.Add(_cmbEtiket, 1, 0);
            secenekKarti.Controls.Add(Etiket("Soru türü:"), 0, 1);
            secenekKarti.Controls.Add(_cmbMod, 1, 1);

            _lblDurum.Margin = new Padding(2, 0, 0, 8);

            // --- Düğmeler ---
            button2.Tag = TemaUygulayici.Birincil;

            _btnBugun.Click += (s2, a2) => BugunkuTekrar();
            _btnCift.Click += (s2, a2) => CiftAntrenmani();
            _btnDuzenle.Click += (s2, a2) => DuzenleyiciAc();
            _btnMetin.Click += (s2, a2) => MetindenKelimeAc();
            _btnPanel.Click += (s2, a2) => PanelAc();
            _btnAyarlar.Click += (s2, a2) => AyarlariAc();

            var butonlar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                AutoSize = true
            };
            for (int i = 0; i < 4; i++)
                butonlar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            Button[] sirali = { button1, button2, _btnBugun, _btnCift, _btnDuzenle, _btnMetin, _btnPanel, _btnAyarlar };
            for (int i = 0; i < sirali.Length; i++)
            {
                sirali[i].Dock = DockStyle.Fill;
                sirali[i].AutoSize = false;
                sirali[i].Margin = new Padding(4);
                sirali[i].MinimumSize = new Size(0, 46);
                butonlar.Controls.Add(sirali[i], i % 4, i / 4);
            }

            button3.Dock = DockStyle.Fill;
            button3.Margin = new Padding(4);
            button3.MinimumSize = new Size(0, 46);
            butonlar.Controls.Add(button3, 0, 2);
            butonlar.SetColumnSpan(button3, 4);

            // --- Kök düzen ---
            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(18)
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(baslikAlani, 0, 0);
            kok.Controls.Add(dosyaKarti, 0, 1);
            kok.Controls.Add(secenekKarti, 0, 2);
            kok.Controls.Add(_lblDurum, 0, 3);
            kok.Controls.Add(butonlar, 0, 4);

            Controls.Add(kok);

            // Designer olayı (comboBox1_SelectedIndexChanged) bozulmasın diye kontrol korunur ama gizlenir.
            comboBox1.Visible = false;
            Controls.Add(comboBox1);

            ResumeLayout(true);
        }

        private static Label Etiket(string metin)
        {
            Label etiket = ArayuzYardimcisi.Etiket(metin);
            etiket.Anchor = AnchorStyles.Left;
            etiket.Margin = new Padding(0, 6, 10, 6);
            return etiket;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
            this.Text = "Kelime Ezber";
            button1.Text = "Yükle";
            button2.Text = "Başlat";
            button3.Text = "Çıkış";

            DosyaListesiniDoldur(new HashSet<string>(Ayarlar.SonSecilenDosyalar));

            if (_dosyaListesi.Items.Count == 0)
                _lblDurum.Text = "Uygulama klasöründe .txt dosyası bulunamadı.";

            _cmbMod.SelectedIndex = Math.Clamp((int)Ayarlar.Mod, 0, _cmbMod.Items.Count - 1);

            // Değerler yerleştikten sonra olayı bağla (açılışta gereksiz tetiklenmesin)
            _dosyaListesi.ItemCheck += (s2, a2) =>
            {
                if (!_doldururken) DosyalarDegisti();
            };

            GorunumuUygula();
            TekrarSayilariniGuncelle();
            HatirlatmaKontrol();
        }

        // ------------------------------------------------------------------
        //  Yükle / Başlat / Çıkış (Designer'ın bağladığı olaylar)
        // ------------------------------------------------------------------

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

            YuklemeSonucu sonuc = _s.Kelimeler.Yukle(secilenler);
            _kartlar = sonuc.Kartlar;
            Ayarlar.SonSecilenDosyalar = secilenler;
            EtiketleriDoldur();

            if (_kartlar.Count == 0)
            {
                _lblDurum.Text = "Seçilen dosyalarda geçerli kelime bulunamadı.";
                button2.Enabled = false;
            }
            else
            {
                string uyariMetni = sonuc.Uyarilar.Count > 0 ? $"  •  {sonuc.Uyarilar.Count} uyarı" : string.Empty;
                int cumleli = _kartlar.Count(k => k.Cloze != null);
                _lblDurum.Text = $"{_kartlar.Count} kelime yüklendi ({secilenler.Count} dosya, {cumleli} örnek cümleli){uyariMetni}";
                button2.Enabled = true;
            }

            if (sonuc.Uyarilar.Count > 0)
                UyarilariGoster(sonuc.Uyarilar);
        }

        // Başlat
        private void button2_Click(object sender, EventArgs e)
        {
            List<KelimeKarti> secili = SeciliKartlar();
            if (secili.Count < 2)
            {
                MessageBox.Show("Quiz için en az 2 kelime gerekir.");
                return;
            }

            OturumuBaslat(secili, _kartlar);
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
        //  Diğer ekranlar
        // ------------------------------------------------------------------

        private void BugunkuTekrar()
        {
            List<KelimeKarti> tum = _s.Kelimeler.TumKartlariYukle();
            List<KelimeKarti> vadesi = _s.Istatistik.VadesiGelenler(tum);

            if (vadesi.Count == 0)
            {
                MessageBox.Show(
                    "Bugün için vadesi gelen kelime yok. Yeni kelimeler için dosya seçip Yükle → Başlat'ı kullanabilirsiniz.",
                    "Bugünkü tekrar");
                return;
            }

            OturumuBaslat(vadesi, tum);
        }

        private void CiftAntrenmani()
        {
            List<KelimeKarti> tum = _s.Kelimeler.TumKartlariYukle();
            List<KarisikCift> ciftler = _s.Istatistik.KarisikCiftler(tum, 10);

            if (ciftler.Count == 0)
            {
                MessageBox.Show(
                    "Henüz karıştırdığın bir kelime çifti yok. Şıklı sorularda yanlış şıkları işaretledikçe burada birikir.",
                    "Karıştırdıklarım");
                return;
            }

            using (var form = new CiftAntrenmanFormu(_s, ciftler))
            {
                form.ShowDialog(this);
            }

            TekrarSayilariniGuncelle();
        }

        private void DuzenleyiciAc()
        {
            var secili = new HashSet<string>(_dosyaListesi.CheckedItems.Cast<string>());

            using (var form = new DuzenleyiciFormu(_s))
            {
                form.ShowDialog(this);
            }

            DosyaListesiniYenileVeSifirla(secili);
        }

        private void MetindenKelimeAc()
        {
            var secili = new HashSet<string>(_dosyaListesi.CheckedItems.Cast<string>());

            using (var form = new MetindenKelimeFormu(_s))
            {
                form.ShowDialog(this);
            }

            DosyaListesiniYenileVeSifirla(secili);
        }

        private void PanelAc()
        {
            using (var form = new IstatistikFormu(_s))
            {
                form.ShowDialog(this);
            }

            TekrarSayilariniGuncelle();
        }

        private void AyarlariAc()
        {
            using (var form = new AyarlarFormu(_s))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
            }

            GorunumuUygula();
            BildirimDurumunuUygula();
            AyarDeposu.Kaydet(Ayarlar);
            IpucuYaz();
        }

        // ------------------------------------------------------------------
        //  Yardımcılar
        // ------------------------------------------------------------------

        private void OturumuBaslat(IReadOnlyList<KelimeKarti> kartlar, IReadOnlyList<KelimeKarti> havuz)
        {
            AyarlariTopla();

            using (var form2 = new Form2(_s, kartlar, havuz))
            {
                form2.ShowDialog(this);
            }

            TekrarSayilariniGuncelle();
        }

        private List<KelimeKarti> SeciliKartlar()
        {
            if (_cmbEtiket.SelectedIndex <= 0) return _kartlar;

            string etiket = _cmbEtiket.SelectedItem as string ?? string.Empty;
            return _kartlar
                .Where(k => k.Etiketler.Any(e => string.Equals(e, etiket, StringComparison.CurrentCultureIgnoreCase)))
                .ToList();
        }

        private void EtiketleriDoldur()
        {
            List<string> etiketler = _kartlar
                .SelectMany(k => k.Etiketler)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(e => e, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _cmbEtiket.Items.Clear();
            _cmbEtiket.Items.Add("(Tümü)");
            foreach (string etiket in etiketler)
                _cmbEtiket.Items.Add(etiket);
            _cmbEtiket.SelectedIndex = 0;
        }

        private void DosyaListesiniDoldur(ISet<string> secili)
        {
            _doldururken = true;
            try
            {
                _dosyaListesi.Items.Clear();
                foreach (string dosya in _s.Kelimeler.DosyaIsimleriniGetir())
                    _dosyaListesi.Items.Add(dosya, secili.Contains(dosya));

                if (_dosyaListesi.Items.Count > 0 && _dosyaListesi.CheckedItems.Count == 0)
                    _dosyaListesi.SetItemChecked(0, true);
            }
            finally
            {
                _doldururken = false;
            }
        }

        private void DosyaListesiniYenileVeSifirla(ISet<string> secili)
        {
            DosyaListesiniDoldur(secili);
            DosyalarDegisti();
            TekrarSayilariniGuncelle();
        }

        private void DosyalarDegisti()
        {
            button2.Enabled = false;
            _kartlar = new List<KelimeKarti>();
            _lblDurum.Text = "Dosya seçimi değişti. Devam etmek için Yükle'ye basın.";
        }

        private void TekrarSayilariniGuncelle()
        {
            // Dosyalar değişmedikçe KelimeDeposu önbellekten döner; bu çağrı ucuzdur.
            List<KelimeKarti> tum = _s.Kelimeler.TumKartlariYukle();
            int vade = _s.Istatistik.VadesiGelenler(tum).Count;
            int cift = _s.Istatistik.KarisikCiftler(tum, 50).Count;

            _btnBugun.Text = $"Bugünkü tekrar ({vade})";
            _btnCift.Text = $"Karıştırdıklarım ({cift})";
            IpucuYaz();
        }

        private void IpucuYaz()
        {
            var parcalar = new List<string>();

            if (Ayarlar.RozetGoster)
            {
                parcalar.Add($"Seri: {_s.Profil.ArdisikGun()} gün");
                parcalar.Add($"XP: {_s.Profil.Xp}");
            }

            IdealUzunlukSonucu ideal = YorgunlukAnalizi.IdealUzunluk(_s.Oturumlar.Oturumlar);
            if (ideal.SoruSayisi.HasValue)
                parcalar.Add($"İdeal oturum: ~{ideal.SoruSayisi} soru");

            _lblIpucu.Text = string.Join(Environment.NewLine, parcalar);
        }

        private void AyarlariTopla()
        {
            if (_cmbMod.SelectedIndex >= 0)
                Ayarlar.Mod = (SoruModu)_cmbMod.SelectedIndex;

            Ayarlar.SonSecilenDosyalar = _dosyaListesi.CheckedItems.Cast<string>().ToList();
            AyarDeposu.Kaydet(Ayarlar);
        }

        private void GorunumuUygula()
        {
            _tema.Ayarla(Ayarlar.Tema, Ayarlar.FontOlcegiYuzde);
            _tema.Uygula(this);
        }

        private static void UyarilariGoster(List<string> uyarilar)
        {
            const int Maksimum = 12;

            string metin = string.Join(Environment.NewLine, uyarilar.Take(Maksimum).Select(u => "• " + u));
            if (uyarilar.Count > Maksimum)
                metin += Environment.NewLine + $"… ve {uyarilar.Count - Maksimum} uyarı daha.";

            MessageBox.Show(metin, "Veri doğrulama uyarıları", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ------------------------------------------------------------------
        //  "Bugünkü tekrarın hazır" bildirimi
        // ------------------------------------------------------------------

        private void BildirimiKur()
        {
            _tepsi.Icon = this.Icon ?? SystemIcons.Application;
            _tepsi.Text = "QuizForge";
            _tepsi.DoubleClick += (s2, a2) => TepsidenGeriDon();
            _tepsi.BalloonTipClicked += (s2, a2) => TepsidenGeriDon();

            _hatirlatma.Interval = 15 * 60 * 1000;
            _hatirlatma.Tick += (s2, a2) => HatirlatmaKontrol();

            Resize += (s2, a2) =>
            {
                if (WindowState == FormWindowState.Minimized && Ayarlar.BildirimAcik)
                    Hide();
            };

            BildirimDurumunuUygula();
        }

        private void BildirimDurumunuUygula()
        {
            _tepsi.Visible = Ayarlar.BildirimAcik;
            _hatirlatma.Enabled = Ayarlar.BildirimAcik;

            if (!Ayarlar.BildirimAcik && !Visible)
                TepsidenGeriDon();
        }

        private void TepsidenGeriDon()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        /// <summary>Günde en fazla bir kez, vadesi gelen kelime varsa bildirim gösterir.</summary>
        private void HatirlatmaKontrol()
        {
            if (!Ayarlar.BildirimAcik) return;

            DateTime simdi = DateTime.Now;
            if (Ayarlar.SonBildirim.HasValue && Ayarlar.SonBildirim.Value.Date == simdi.Date) return;

            int sayi = _s.Istatistik.VadesiGelenler(_s.Kelimeler.TumKartlariYukle()).Count;
            if (sayi <= 0) return;

            _tepsi.ShowBalloonTip(8000, "QuizForge", $"Bugünkü tekrarın hazır: {sayi} kelime seni bekliyor.", ToolTipIcon.Info);
            Ayarlar.SonBildirim = simdi;
        }
    }
}
