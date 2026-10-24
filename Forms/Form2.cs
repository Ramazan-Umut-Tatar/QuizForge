#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizForge
{
    public partial class Form2 : Form
    {
        private readonly UygulamaServisleri _s;
        private readonly UygulamaAyarlari _ayarlar;
        private readonly QuizAyarlari _quizAyar;
        private readonly TemaUygulayici _tema;
        private readonly Button[] _butonlar;
        private readonly string _baslik;
        private readonly HashSet<int> _yanlisIsaretli = new HashSet<int>();
        private QuizOturumu _oturum;
        private bool _tamamlandi;
        private OdulOzeti? _odul;

        // Kodla eklenen kontroller (Designer dosyasına dokunulmaz)
        private readonly Label _lblIlerleme = new Label();
        private readonly Label _lblSeviye = new Label();
        private readonly Label _lblPuan = new Label();
        private readonly Label _lblBilgi = new Label();
        private readonly Bar _barIlerleme = new Bar();
        private readonly TableLayoutPanel _sikPaneli = new TableLayoutPanel();
        private readonly TableLayoutPanel _yazPaneli = new TableLayoutPanel();
        private readonly TextBox _txtCevap = new TextBox();
        private readonly Button _btnKontrol = ArayuzYardimcisi.Dugme("Kontrol Et", birincil: true);
        private readonly Button _btnBilmiyorum = ArayuzYardimcisi.Dugme("Bilmiyorum / Geç (cevabı göster)");

        public Form2(UygulamaServisleri servisler, IEnumerable<KelimeKarti> kartlar, IEnumerable<KelimeKarti>? havuz = null)
        {
            InitializeComponent();

            _s = servisler;
            _ayarlar = servisler.Ayarlar;
            _quizAyar = _ayarlar.QuizAyarlariOlustur();
            _tema = new TemaUygulayici(_ayarlar);
            _butonlar = new[] { button1, button2, button3, button4 };

            switch (_quizAyar.Mod)
            {
                case SoruModu.Sikli: _baslik = "Çoktan Seçmeli Sorular"; break;
                case SoruModu.Yazili: _baslik = "Yazarak Cevap"; break;
                default: _baslik = "Ustalık Merdiveni"; break;
            }

            _oturum = new QuizOturumu(kartlar, _quizAyar, servisler.Istatistik, havuz);

            ArayuzuKur();

            this.Load += new EventHandler(Form2_Load);
            this.KeyPreview = true;
            this.KeyDown += Form2_KeyDown;
        }

        /// <summary>
        /// Designer'daki sabit konumlu kontrolleri (button1-4, listBox1-2) pencereyle büyüyen
        /// bir TableLayoutPanel düzenine taşır ve yazılı/cümle sorusu kontrollerini ekler.
        /// Hangi panelin görüneceği her soruda soru tipine göre belirlenir.
        /// </summary>
        private void ArayuzuKur()
        {
            SuspendLayout();
            Controls.Clear();

            MinimumSize = new Size(640, 500);
            Text = _baslik;

            // --- Skor çubuğu (eski listBox1'in yerini alır): ilerleme | seviye | puan ---
            var skorPaneli = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = listBox1.BackColor,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(6, 0, 6, 0),
                Tag = TemaUygulayici.Atla
            };
            skorPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            skorPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            skorPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            skorPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            SkorEtiketiHazirla(_lblIlerleme, ContentAlignment.MiddleLeft, 1f);
            SkorEtiketiHazirla(_lblSeviye, ContentAlignment.MiddleCenter, 0.55f);
            SkorEtiketiHazirla(_lblPuan, ContentAlignment.MiddleRight, 1f);
            skorPaneli.Controls.Add(_lblIlerleme, 0, 0);
            skorPaneli.Controls.Add(_lblSeviye, 1, 0);
            skorPaneli.Controls.Add(_lblPuan, 2, 0);

            // Eski listBox1 Designer olayları için korunur ama gösterilmez.
            listBox1.Visible = false;

            // --- İnce ilerleme çubuğu ---
            _barIlerleme.Dock = DockStyle.Fill;
            _barIlerleme.Height = 10;
            _barIlerleme.Margin = new Padding(0, 0, 0, 10);

            // --- Kelime / cümle kartı ---
            listBox2.Dock = DockStyle.Fill;
            listBox2.Margin = new Padding(0, 0, 0, 8);
            listBox2.IntegralHeight = false;
            listBox2.BorderStyle = BorderStyle.None;
            listBox2.DrawMode = DrawMode.OwnerDrawFixed;
            listBox2.DrawItem += listBox2_DrawItem;
            listBox2.Resize += (s2, a2) => KelimeKutusuYuksekligiAyarla();
            listBox2.Tag = TemaUygulayici.Atla;

            // --- Bilgi satırı ---
            _lblBilgi.Dock = DockStyle.Fill;
            _lblBilgi.TextAlign = ContentAlignment.MiddleCenter;
            _lblBilgi.AutoEllipsis = true;

            // --- Şıklı soru paneli (2x2) ---
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
                _butonlar[i].AutoSize = false;
                _butonlar[i].Margin = new Padding(5);
                _sikPaneli.Controls.Add(_butonlar[i], i % 2, i / 2);
            }

            // --- Yazılı / cümle içinde soru paneli ---
            _yazPaneli.Dock = DockStyle.Fill;
            _yazPaneli.ColumnCount = 2;
            _yazPaneli.RowCount = 1;
            _yazPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            _yazPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _yazPaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtCevap.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _txtCevap.Margin = new Padding(5);
            _txtCevap.Font = new Font("Segoe UI", 16f);
            _txtCevap.KeyDown += (s2, a2) =>
            {
                if (a2.KeyCode == Keys.Enter)
                {
                    a2.SuppressKeyPress = true;
                    YazilanCevabiKontrolEt();
                }
            };
            _txtCevap.TextChanged += (s2, a2) =>
            {
                _txtCevap.BackColor = _tema.GirisZemin;
                _txtCevap.ForeColor = _tema.Yazi;
            };

            _btnKontrol.Dock = DockStyle.Fill;
            _btnKontrol.AutoSize = false;
            _btnKontrol.Margin = new Padding(5);
            _btnKontrol.Click += (s2, a2) => YazilanCevabiKontrolEt();

            _yazPaneli.Controls.Add(_txtCevap, 0, 0);
            _yazPaneli.Controls.Add(_btnKontrol, 1, 0);
            _yazPaneli.Visible = false;

            var cevapAlani = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            cevapAlani.Controls.Add(_sikPaneli);
            cevapAlani.Controls.Add(_yazPaneli);

            // --- Bilmiyorum butonu ---
            _btnBilmiyorum.Dock = DockStyle.Fill;
            _btnBilmiyorum.AutoSize = false;
            _btnBilmiyorum.Margin = new Padding(5);
            _btnBilmiyorum.Click += (s2, a2) => CevapSonrasiIsle(_oturum.Bilmiyorum());

            // --- Kök düzen ---
            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(14)
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 12));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 22));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 8));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 12));
            kok.Controls.Add(skorPaneli, 0, 0);
            kok.Controls.Add(_barIlerleme, 0, 1);
            kok.Controls.Add(listBox2, 0, 2);
            kok.Controls.Add(_lblBilgi, 0, 3);
            kok.Controls.Add(cevapAlani, 0, 4);
            kok.Controls.Add(_btnBilmiyorum, 0, 5);

            Controls.Add(kok);
            Controls.Add(listBox1);

            _tema.Uygula(this);

            // Tema dışı bırakılan iki alanın renkleri paletten verilir (Sistem temasında tasarım renkleri korunur).
            _tema.KartRenkleriUygula(listBox2);
            _tema.SkorRenkleriUygula(skorPaneli, _lblIlerleme, _lblSeviye, _lblPuan);

            ResumeLayout(true);
        }

        private void SkorEtiketiHazirla(Label etiket, ContentAlignment hiza, float olcek)
        {
            etiket.Dock = DockStyle.Fill;
            etiket.TextAlign = hiza;
            etiket.Font = new Font(listBox1.Font.FontFamily, Math.Max(8f, listBox1.Font.Size * olcek), listBox1.Font.Style);
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
            QuizSorusu? soru = _oturum.MevcutSoru;
            if (soru == null) return;

            Text = soru.Tekrar ? _baslik + " — Tekrar" : _baslik;

            bool sikli = soru.Tip == SoruTipi.Sikli;
            _sikPaneli.Visible = sikli;
            _yazPaneli.Visible = !sikli;
            _yanlisIsaretli.Clear();

            listBox2.Items.Clear();
            listBox2.Items.Add(soru.Cumle != null ? soru.Cumle.Metin : soru.Kart.Kelime);

            _lblSeviye.Text = SeviyeMetni(soru);

            string bilgi;
            if (soru.Tip == SoruTipi.Cumle)
                bilgi = $"Boşluğa gelecek kelimeyi yazın — anlamı: {soru.Kart.Anlam}";
            else if (soru.Tekrar)
                bilgi = "Tekrar sorusu";
            else
                bilgi = string.Empty;

            string not = _s.Istatistik.NotAl(soru.Kart);
            if (not.Length > 0 && soru.Tekrar)
                bilgi += (bilgi.Length > 0 ? " • " : string.Empty) + "Notun: " + not;

            Bilgi(bilgi, _tema.Palet.SolukYazi);

            if (sikli)
            {
                for (int i = 0; i < _butonlar.Length; i++)
                {
                    bool kullanilacak = i < soru.Secenekler.Count;
                    _butonlar[i].Visible = kullanilacak;
                    _butonlar[i].Enabled = true;
                    _butonlar[i].Text = kullanilacak ? soru.Secenekler[i] : string.Empty;
                    _tema.ButonuSifirla(_butonlar[i]);
                }
            }
            else
            {
                _txtCevap.PlaceholderText = soru.Tip == SoruTipi.Cumle
                    ? "Boşluğa gelecek kelimeyi yazın"
                    : "Anlamını yazın (alternatiflerden biri yeterli)";
                _txtCevap.Clear();
                _txtCevap.Focus();
            }

            SkoruYaz();
            _oturum.SureyiBaslat();
        }

        private static string SeviyeMetni(QuizSorusu soru)
        {
            string ad = UstalikMerdiveni.Ad(soru.Seviye);
            string tip = UstalikMerdiveni.TipAdi(soru.Tip);

            return soru.SeriHedefi > 0
                ? $"{ad} • {tip} • seri {soru.Seri}/{soru.SeriHedefi}"
                : $"{ad} • {tip}";
        }

        private void SkoruYaz()
        {
            _lblIlerleme.Text = $"{_oturum.Cevaplanan}/{_oturum.ToplamSoru}";
            _lblPuan.Text = _oturum.Puan.ToString();
            _barIlerleme.Maks = Math.Max(1, _oturum.ToplamSoru);
            _barIlerleme.Deger = _oturum.Cevaplanan;
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
            // Yanlış işaretlenen şık devre dışı bırakılmaz (düz stil devre dışı rengi okunaksızdır);
            // bunun yerine tekrar tıklanması yok sayılır.
            if (_yanlisIsaretli.Contains(index)) return;

            if (_oturum.Cevapla(index) == CevapSonucu.Yanlis)
            {
                _yanlisIsaretli.Add(index);
                _tema.ButonuYanlisYap(_butonlar[index]);

                string not = _oturum.MevcutSoru != null ? _s.Istatistik.NotAl(_oturum.MevcutSoru.Kart) : string.Empty;
                if (not.Length > 0)
                    Bilgi("Notun: " + not, _tema.Palet.Uyari);
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
                _txtCevap.BackColor = _tema.Palet.Yanlis;
                _txtCevap.ForeColor = _tema.Palet.DurumYazi;
                Bilgi($"Yanlış. Kalan hak: {_oturum.KalanDeneme}", _tema.Palet.Yanlis);
                _txtCevap.SelectAll();
                _txtCevap.Focus();
                return;
            }

            CevapSonrasiIsle(sonuc);
        }

        /// <summary>Soru çözüldükten (doğru ya da geçildi) sonra hata nedenini sorar, yorgunluğu kontrol eder, ilerler.</summary>
        private void CevapSonrasiIsle(CevapSonucu sonuc)
        {
            CozumBilgisi? cozum = _oturum.SonCozum;

            if (cozum != null && !cozum.IlkDenemedeDogru && _ayarlar.HataNedeniSor)
                HataNedeniniSor(cozum);

            bool erkenBitir = false;
            if (!_oturum.Bitti && _ayarlar.YorgunlukUyarisi)
                erkenBitir = YorgunlukKontrolu();

            SkoruYaz();

            if (_oturum.Bitti || erkenBitir)
            {
                OturumuBitir();
                return;
            }

            SoruyuGoster();

            if (cozum != null)
                GeriBildirimGoster(cozum);
        }

        private void GeriBildirimGoster(CozumBilgisi cozum)
        {
            if (cozum.Gecildi)
            {
                Bilgi($"Doğru cevap ({cozum.Kart.Kelime}): {cozum.DogruCevap}", _tema.Palet.Uyari);
            }
            else if (cozum.Degisim != null && cozum.Degisim.Terfi)
            {
                Bilgi($"Seviye atladı: {UstalikMerdiveni.Ad(cozum.Degisim.Onceki)} → {UstalikMerdiveni.Ad(cozum.Degisim.Yeni)}",
                      _tema.Palet.Dogru);
            }
            else if (cozum.IlkDenemedeDogru && !cozum.Akici)
            {
                Bilgi("Doğru, ama yavaş: henüz akıcı değil. Bu kelime daha sık sorulacak.", _tema.Palet.Uyari);
            }
        }

        private void HataNedeniniSor(CozumBilgisi cozum)
        {
            using (var form = new HataNedeniFormu(_s, cozum))
            {
                form.ShowDialog(this);

                if (form.Secilen.HasValue)
                {
                    _oturum.HataNedeniKaydet(form.Secilen.Value);
                    _s.Profil.Say("hata_nedeni");
                }
            }
        }

        /// <summary>true dönerse kullanıcı oturumu bitirmek istemiştir.</summary>
        private bool YorgunlukKontrolu()
        {
            YorgunlukUyarisi? uyari = _oturum.YorgunlukKontrolu();
            if (uyari == null) return false;

            _oturum.UyariGosterildi();

            using (var form = new MolaFormu(uyari, _ayarlar))
            {
                form.ShowDialog(this);

                if (form.Karar == MolaKarari.MolaVerdi)
                    _s.Profil.Say("mola");

                return form.Karar == MolaKarari.Bitir;
            }
        }

        // ------------------------------------------------------------------
        //  Oturum sonu
        // ------------------------------------------------------------------

        /// <summary>Oturum kaydını, XP'yi ve rozetleri bir kez işler (pencere kapansa bile).</summary>
        private void OturumuTamamla()
        {
            if (_tamamlandi) return;
            _tamamlandi = true;

            if (_oturum.CozulenSoru == 0) return;

            _s.Oturumlar.Ekle(_oturum.KaydiOlustur());
            _odul = _s.Profil.OturumuIsle(_oturum, _s.UstaSayisi);
            _s.HepsiniKaydet();
        }

        private void OturumuBitir()
        {
            OturumuTamamla();

            DialogResult sonuc;
            using (var ozet = new OzetFormu(_s, _oturum, _odul))
            {
                sonuc = ozet.ShowDialog(this);
            }

            if (sonuc == DialogResult.Retry && _oturum.YanlisKayitlari.Count > 0)
            {
                List<KelimeKarti> yanlisKartlar = _oturum.YanlisKayitlari.Select(k => k.Kart).ToList();
                _oturum = new QuizOturumu(yanlisKartlar, _quizAyar, _s.Istatistik, _oturum.HavuzKartlari);
                _tamamlandi = false;
                _odul = null;
                SoruyuGoster();
                return;
            }

            this.Close();
        }

        private void Form2_KeyDown(object? sender, KeyEventArgs e)
        {
            // 1-4 tuşları şıkları seçer; yalnızca şıklı sorularda (yazarken rakam girilebilsin).
            QuizSorusu? soru = _oturum.MevcutSoru;
            if (soru == null || soru.Tip != SoruTipi.Sikli) return;

            int index;
            switch (e.KeyCode)
            {
                case Keys.D1: case Keys.NumPad1: index = 0; break;
                case Keys.D2: case Keys.NumPad2: index = 1; break;
                case Keys.D3: case Keys.NumPad3: index = 2; break;
                case Keys.D4: case Keys.NumPad4: index = 3; break;
                default: return;
            }

            if (_butonlar[index].Visible)
            {
                e.Handled = true;
                _butonlar[index].PerformClick();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            OturumuTamamla();
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

            // Boşluk bırakılan kenarlar: metin kartın kenarlarına yapışmasın.
            Rectangle alan = Rectangle.Inflate(e.Bounds, -16, -8);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                listBox2.Font,
                alan,
                listBox2.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
    }
}
