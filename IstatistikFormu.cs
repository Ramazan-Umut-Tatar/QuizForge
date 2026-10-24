#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Öğrenme paneli: solda gezinme menüsü, sağda kart sayfaları (en zor kelimeler, seviyeler,
    /// hata türleri, oturumlar / ideal uzunluk, XP ve rozetler). Tamamen kodla kurulur.
    /// Sayfalar her gösterimde yeniden üretilir, böylece veri her zaman güncel olur.
    /// </summary>
    public sealed class IstatistikFormu : Form
    {
        private readonly UygulamaServisleri _s;
        private readonly TemaUygulayici _tema;
        private readonly Panel _icerik = new Panel();
        private readonly List<Button> _navDugmeleri = new List<Button>();
        private readonly List<string> _basliklar = new List<string>();
        private readonly List<Func<Control>> _sayfalar = new List<Func<Control>>();
        private int _aktif;

        public IstatistikFormu(UygulamaServisleri servisler)
        {
            _s = servisler;
            _tema = new TemaUygulayici(_s.Ayarlar);

            Text = "Öğrenme Paneli";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 520);
            Size = new Size(1020, 640);

            ArayuzuKur();
            _tema.Uygula(this);
            SayfaGoster(0);
        }

        private void ArayuzuKur()
        {
            SayfaEkle("En zor kelimeler", ZorKelimelerSayfasi);
            SayfaEkle("Seviyeler", SeviyeSayfasi);
            SayfaEkle("Hata türleri", HataTuruSayfasi);
            SayfaEkle("Oturumlar", OturumSayfasi);
            SayfaEkle("XP ve rozetler", RozetSayfasi);

            var menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 12, 0)
            };

            for (int i = 0; i < _basliklar.Count; i++)
            {
                int index = i;
                Button dugme = ArayuzYardimcisi.Dugme(_basliklar[i]);
                dugme.AutoSize = false;
                dugme.Size = new Size(200, 44);
                dugme.Margin = new Padding(0, 0, 0, 6);
                dugme.TextAlign = ContentAlignment.MiddleLeft;
                dugme.Padding = new Padding(12, 0, 0, 0);
                dugme.Click += (sender, args) => SayfaGoster(index);

                _navDugmeleri.Add(dugme);
                menu.Controls.Add(dugme);
            }

            _icerik.Dock = DockStyle.Fill;

            Button btnSifirla = ArayuzYardimcisi.Dugme("İstatistikleri sıfırla");
            Button btnKapat = ArayuzYardimcisi.Dugme("Kapat", birincil: true);
            btnKapat.DialogResult = DialogResult.OK;
            btnSifirla.Click += (sender, args) => Sifirla();

            var butonlar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 10, 0, 0)
            };
            butonlar.Controls.Add(btnKapat);
            butonlar.Controls.Add(btnSifirla);

            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(16)
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 212));
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(menu, 0, 0);
            kok.Controls.Add(_icerik, 1, 0);
            kok.Controls.Add(butonlar, 0, 1);
            kok.SetColumnSpan(butonlar, 2);

            Controls.Add(kok);
            CancelButton = btnKapat;
        }

        private void SayfaEkle(string baslik, Func<Control> olusturucu)
        {
            _basliklar.Add(baslik);
            _sayfalar.Add(olusturucu);
        }

        private void SayfaGoster(int index)
        {
            _aktif = index;

            for (int i = 0; i < _navDugmeleri.Count; i++)
            {
                bool secili = i == index;
                _navDugmeleri[i].Text = (secili ? "▸  " : "    ") + _basliklar[i];
                _navDugmeleri[i].Tag = secili ? TemaUygulayici.Birincil : null;
                _tema.ButonuSifirla(_navDugmeleri[i]);
            }

            // Sayfa, forma eklenmeden önce temalanır: eklendikten sonra ebeveynin ölçekli fontunu
            // "orijinal font" sanıp ikinci kez ölçeklememesi için.
            Control sayfa = _sayfalar[index]();
            sayfa.Dock = DockStyle.Fill;
            _tema.Uygula(sayfa);

            _icerik.SuspendLayout();
            while (_icerik.Controls.Count > 0)
            {
                Control eski = _icerik.Controls[0];
                _icerik.Controls.RemoveAt(0);
                eski.Dispose();
            }
            _icerik.Controls.Add(sayfa);
            _icerik.ResumeLayout(true);
        }

        // ------------------------------------------------------------------
        //  Sayfalar
        // ------------------------------------------------------------------

        private Control ZorKelimelerSayfasi()
        {
            ListView liste = ListeOlustur();
            liste.Columns.Add("#", 40);
            liste.Columns.Add("Kelime", 150);
            liste.Columns.Add("Anlam", 200);
            liste.Columns.Add("Seviye", 90);
            liste.Columns.Add("Doğru", 55);
            liste.Columns.Add("Yanlış", 55);
            liste.Columns.Add("Başarı", 60);
            liste.Columns.Add("Ort. süre", 70);
            liste.Columns.Add("Akıcı", 60);
            liste.Columns.Add("Son görülme", 120);

            int sira = 1;
            liste.BeginUpdate();
            foreach (KelimeIstatistigi k in _s.Istatistik.EnZorKelimeler(20))
            {
                string akici = k.Akici == true ? "evet" : (k.Akici == false ? "hayır" : "—");
                string sure = k.SureSayisi > 0 ? $"{k.OrtSureMs / 1000.0:0.0} sn" : "—";

                liste.Items.Add(new ListViewItem(new[]
                {
                    (sira++).ToString(),
                    k.Kelime,
                    k.Anlam,
                    UstalikMerdiveni.Ad(k.Seviye),
                    k.Dogru.ToString(),
                    k.Yanlis.ToString(),
                    "%" + Math.Round(k.BasariOrani * 100),
                    sure,
                    akici,
                    k.SonGorulme.ToString("dd.MM.yyyy HH:mm")
                }));
            }
            liste.EndUpdate();

            string aciklama = liste.Items.Count == 0
                ? "Henüz yanlış yapılan kelime yok."
                : "Yanlış oranı yüksek olanlar üstte (en fazla 20 kelime).";

            return ListeSayfasi("En zor kelimeler", aciklama, liste);
        }

        private Control SeviyeSayfasi()
        {
            List<KelimeKarti> kartlar = _s.Kelimeler.TumKartlariYukle();
            Dictionary<Seviye, int> dagilim = _s.Istatistik.SeviyeDagilimi(kartlar);
            int toplam = Math.Max(1, kartlar.Count);

            TableLayoutPanel tablo = BarTablosu();
            foreach (Seviye seviye in new[] { Seviye.Yeni, Seviye.Ogreniliyor, Seviye.Pekisti, Seviye.Usta })
                BarEkle(tablo, UstalikMerdiveni.Ad(seviye), dagilim[seviye], toplam);

            int vadesi = _s.Istatistik.VadesiGelenler(kartlar).Count;
            int akiciDegil = _s.Istatistik.AkiciOlmayanSayisi(kartlar);

            string bilgi =
                $"Toplam {kartlar.Count} kelime (tüm dosyalar)." + Environment.NewLine +
                $"Bugün tekrar vadesi gelen: {vadesi}" + Environment.NewLine +
                $"Doğru bilinen ama henüz akıcı olmayan: {akiciDegil}" + Environment.NewLine + Environment.NewLine +
                "Yeni → şıklı, Öğreniliyor → yazılı, Pekişti ve Usta → cümle içinde sorulur. " +
                "Üst seviyeye ancak üst üste, ilk denemede ve hızlı doğrulardan sonra çıkılır.";

            return BarSayfasi("Ustalık seviyeleri", tablo, bilgi);
        }

        private Control HataTuruSayfasi()
        {
            Dictionary<HataNedeni, int> hafta = _s.Istatistik.HataDagilimi(DateTime.Now.AddDays(-7));
            Dictionary<HataNedeni, int> tumu = _s.Istatistik.HataDagilimi(DateTime.MinValue);
            int haftaToplam = hafta.Values.Sum();
            int tumToplam = tumu.Values.Sum();

            TableLayoutPanel tablo = BarTablosu();
            foreach (HataNedeni neden in Enum.GetValues(typeof(HataNedeni)))
                BarEkle(tablo, NedenAdi(neden), hafta[neden], Math.Max(1, haftaToplam));

            string aciklama;
            if (haftaToplam < 5)
            {
                aciklama =
                    $"Son 7 günde {haftaToplam} hata nedeni işaretlendi (tüm zamanlar: {tumToplam})." + Environment.NewLine +
                    "Bir hafta boyunca yanlış sonrası nedeni işaretledikçe burada anlamlı bir dağılım oluşur.";
            }
            else
            {
                HataNedeni enSik = hafta.OrderByDescending(x => x.Value).First().Key;
                aciklama =
                    $"Son 7 günde {haftaToplam} hata işaretlendi; en sık neden: {NedenAdi(enSik)}." + Environment.NewLine +
                    Oneri(enSik);
            }

            return BarSayfasi("Hata türleri — son 7 gün", tablo, aciklama);
        }

        private Control OturumSayfasi()
        {
            IdealUzunlukSonucu ideal = YorgunlukAnalizi.IdealUzunluk(_s.Oturumlar.Oturumlar);

            ListView liste = ListeOlustur();
            liste.Columns.Add("Tarih", 140);
            liste.Columns.Add("Soru", 60);
            liste.Columns.Add("Doğruluk", 80);
            liste.Columns.Add("Süre (dk)", 80);
            liste.Columns.Add("Ort. cevap süresi", 120);

            liste.BeginUpdate();
            foreach (OturumKaydi o in _s.Oturumlar.Oturumlar.Reverse().Take(20))
            {
                double dogruluk = o.Dogrular.Count == 0 ? 0 : o.Dogrular.Count(d => d) * 100.0 / o.Dogrular.Count;
                double ortSure = o.Sureler.Count == 0 ? 0 : o.Sureler.Average() / 1000.0;

                liste.Items.Add(new ListViewItem(new[]
                {
                    o.Baslangic.ToString("dd.MM.yyyy HH:mm"),
                    o.Dogrular.Count.ToString(),
                    $"%{dogruluk:0}",
                    o.SureDakika.ToString("0.0"),
                    $"{ortSure:0.0} sn"
                }));
            }
            liste.EndUpdate();

            return ListeSayfasi("Oturumlar", ideal.Aciklama, liste);
        }

        private Control RozetSayfasi()
        {
            if (!_s.Ayarlar.RozetGoster)
            {
                TableLayoutPanel kapali = ArayuzYardimcisi.Kart(1, false);
                kapali.Controls.Add(ArayuzYardimcisi.BolumBasligi("XP ve rozetler"), 0, 0);
                kapali.Controls.Add(
                    ArayuzYardimcisi.Etiket("Rozet ve XP gösterimi kapalı (Ayarlar'dan açılabilir).", TemaUygulayici.Soluk), 0, 1);
                return kapali;
            }

            ListView liste = ListeOlustur();
            liste.Columns.Add("Rozet", 160);
            liste.Columns.Add("Açıklama", 420);
            liste.Columns.Add("Durum", 150);

            foreach (RozetTanimi rozet in Rozetler.Hepsi)
            {
                bool kazanildi = _s.Profil.KazanilanRozetler.TryGetValue(rozet.Id, out DateTime tarih);
                var oge = new ListViewItem(new[]
                {
                    rozet.Ad,
                    rozet.Aciklama,
                    kazanildi ? "✔ " + tarih.ToString("dd.MM.yyyy") : "—"
                });
                if (!kazanildi) oge.ForeColor = SystemColors.GrayText;
                liste.Items.Add(oge);
            }

            string ozet =
                $"Toplam XP: {_s.Profil.Xp}   •   Seri: {_s.Profil.ArdisikGun()} gün   •   " +
                $"Rozet: {_s.Profil.KazanilanRozetler.Count}/{Rozetler.Hepsi.Count}";

            return ListeSayfasi("XP ve rozetler", ozet, liste);
        }

        // ------------------------------------------------------------------
        //  Sayfa iskeletleri ve yardımcılar
        // ------------------------------------------------------------------

        private static ListView ListeOlustur() => new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            Margin = new Padding(0)
        };

        /// <summary>Başlık + açıklama + liste içeren kart sayfası.</summary>
        private static Control ListeSayfasi(string baslik, string aciklama, ListView liste)
        {
            TableLayoutPanel kart = ArayuzYardimcisi.Kart(1, false);
            kart.RowCount = 3;
            kart.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kart.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kart.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kart.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label aciklamaEtiketi = ArayuzYardimcisi.Etiket(aciklama, TemaUygulayici.Soluk);
            aciklamaEtiketi.Margin = new Padding(0, 0, 0, 10);

            kart.Controls.Add(ArayuzYardimcisi.BolumBasligi(baslik), 0, 0);
            kart.Controls.Add(aciklamaEtiketi, 0, 1);
            kart.Controls.Add(liste, 0, 2);
            return kart;
        }

        /// <summary>Başlık + çubuk tablosu + açıklama içeren kart sayfası.</summary>
        private static Control BarSayfasi(string baslik, TableLayoutPanel tablo, string aciklama)
        {
            TableLayoutPanel kart = ArayuzYardimcisi.Kart(1, false);
            kart.RowCount = 3;
            kart.AutoScroll = true;
            kart.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kart.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kart.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kart.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label aciklamaEtiketi = ArayuzYardimcisi.Etiket(aciklama, TemaUygulayici.Soluk);
            aciklamaEtiketi.Margin = new Padding(0, 14, 0, 0);
            aciklamaEtiketi.MaximumSize = new Size(640, 0);

            kart.Controls.Add(ArayuzYardimcisi.BolumBasligi(baslik), 0, 0);
            kart.Controls.Add(tablo, 0, 1);
            kart.Controls.Add(aciklamaEtiketi, 0, 2);
            return kart;
        }

        private static TableLayoutPanel BarTablosu()
        {
            var tablo = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, Dock = DockStyle.Top };
            tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            return tablo;
        }

        private static void BarEkle(TableLayoutPanel tablo, string ad, int deger, int en)
        {
            int satir = tablo.RowCount;
            tablo.RowCount = satir + 1;
            tablo.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var etiket = new Label { Text = ad, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 8) };
            var bar = new Bar
            {
                Maks = Math.Max(1, en),
                Deger = deger,
                Height = 14,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 12, 8)
            };
            var sayi = new Label { Text = deger.ToString(), AutoSize = true, Anchor = AnchorStyles.Left };

            tablo.Controls.Add(etiket, 0, satir);
            tablo.Controls.Add(bar, 1, satir);
            tablo.Controls.Add(sayi, 2, satir);
        }

        private static string NedenAdi(HataNedeni neden)
        {
            switch (neden)
            {
                case HataNedeni.Bilmiyordum: return "Bilmiyordum";
                case HataNedeni.Karistirdim: return "Karıştırdım";
                default: return "Dikkatsizlik";
            }
        }

        private static string Oneri(HataNedeni neden)
        {
            switch (neden)
            {
                case HataNedeni.Bilmiyordum:
                    return "Öneri: Yeni kelimeleri daha sık ve kısa oturumlarla tekrar et; çağrışım notları (AI) yardımcı olabilir.";
                case HataNedeni.Karistirdim:
                    return "Öneri: Ana ekrandaki \"Karıştırdıklarım\" çift antrenmanını dene; benzer kelimeleri yan yana ayırt etmek en iyi çözüm.";
                default:
                    return "Öneri: Dikkat hataları genelde acele ve yorgunluktan gelir; oturumları kısalt, molaları ihmal etme.";
            }
        }

        private void Sifirla()
        {
            DialogResult cevap = MessageBox.Show(
                "Tüm kelime istatistikleri, seviyeler ve hata kayıtları silinecek. (XP ve rozetler silinmez.) Emin misiniz?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (cevap != DialogResult.Yes) return;

            _s.Istatistik.Sifirla();
            SayfaGoster(_aktif);
        }
    }
}
