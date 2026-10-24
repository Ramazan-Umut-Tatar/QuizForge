#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Sık karıştırılan iki kelimeyi yan yana çalıştırır: "Bu anlam hangisinin?" sorusu sorulur,
    /// cevaptan sonra iki kelime farkları vurgulanarak karşılaştırılır. Doğru ayırt edildikçe
    /// karışıklık kaydı azalır. Tamamen kodla kurulur.
    /// </summary>
    public sealed class CiftAntrenmanFormu : Form
    {
        private sealed class Gorev
        {
            public KelimeKarti Hedef { get; }
            public KelimeKarti Diger { get; }
            public KarisikCift Cift { get; }

            public Gorev(KelimeKarti hedef, KelimeKarti diger, KarisikCift cift)
            {
                Hedef = hedef;
                Diger = diger;
                Cift = cift;
            }
        }

        private readonly UygulamaServisleri _s;
        private readonly TemaUygulayici _tema;
        private readonly List<Gorev> _gorevler = new List<Gorev>();
        private readonly Dictionary<KarisikCift, int> _ciftDogru = new Dictionary<KarisikCift, int>();
        private readonly Random _rnd = new Random();

        private readonly Label _lblBaslik = new Label();
        private readonly Label _lblIstem = new Label();
        private readonly Button _btnSol = new Button();
        private readonly Button _btnSag = new Button();
        private readonly RichTextBox _rtbSol = new RichTextBox();
        private readonly RichTextBox _rtbSag = new RichTextBox();
        private readonly Label _lblSonuc = new Label();
        private readonly Button _btnSonraki = new Button();

        private KelimeKarti _solKart = null!;
        private KelimeKarti _sagKart = null!;
        private int _sira = -1;
        private int _dogru;
        private bool _cevaplandi;

        public CiftAntrenmanFormu(UygulamaServisleri servisler, IReadOnlyList<KarisikCift> ciftler)
        {
            _s = servisler;
            _tema = new TemaUygulayici(servisler.Ayarlar);

            Text = "Karıştırdığın Çiftler";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(700, 520);
            Size = new Size(860, 620);

            foreach (KarisikCift cift in ciftler.OrderBy(_ => _rnd.Next()))
            {
                _ciftDogru[cift] = 0;
                _gorevler.Add(new Gorev(cift.A, cift.B, cift));
                _gorevler.Add(new Gorev(cift.B, cift.A, cift));
            }

            ArayuzuKur();
            _tema.Uygula(this);

            SonrakiGorev();
        }

        private void ArayuzuKur()
        {
            _lblBaslik.AutoSize = true;
            _lblBaslik.Margin = new Padding(0, 0, 0, 6);

            _lblIstem.Dock = DockStyle.Fill;
            _lblIstem.TextAlign = ContentAlignment.MiddleCenter;
            _lblIstem.Font = new Font(Font.FontFamily, 16f, FontStyle.Bold);

            foreach (Button dugme in new[] { _btnSol, _btnSag })
            {
                dugme.Dock = DockStyle.Fill;
                dugme.Margin = new Padding(6);
                dugme.Font = new Font(Font.FontFamily, 16f, FontStyle.Bold);
            }
            _btnSol.Click += (sender, args) => Sec(_solKart);
            _btnSag.Click += (sender, args) => Sec(_sagKart);

            var secenekler = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            secenekler.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            secenekler.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            secenekler.Controls.Add(_btnSol, 0, 0);
            secenekler.Controls.Add(_btnSag, 1, 0);

            foreach (RichTextBox kutu in new[] { _rtbSol, _rtbSag })
            {
                kutu.Dock = DockStyle.Fill;
                kutu.ReadOnly = true;
                kutu.BorderStyle = BorderStyle.FixedSingle;
                kutu.Margin = new Padding(6);
                kutu.Font = new Font(Font.FontFamily, 12f);
                kutu.TabStop = false;
            }

            var karsilastirma = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            karsilastirma.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            karsilastirma.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            karsilastirma.Controls.Add(_rtbSol, 0, 0);
            karsilastirma.Controls.Add(_rtbSag, 1, 0);

            _lblSonuc.AutoSize = true;
            _lblSonuc.Anchor = AnchorStyles.Left;

            _btnSonraki.Text = "Sonraki";
            _btnSonraki.AutoSize = true;
            _btnSonraki.Padding = new Padding(14, 5, 14, 5);
            _btnSonraki.Enabled = false;
            _btnSonraki.Tag = TemaUygulayici.Birincil;
            _btnSonraki.Click += SonrakiTiklandi;

            var alt = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
            alt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            alt.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            alt.Controls.Add(_lblSonuc, 0, 0);
            alt.Controls.Add(_btnSonraki, 1, 0);

            var kok = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(12) };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 22));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 22));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(_lblBaslik, 0, 0);
            kok.Controls.Add(_lblIstem, 0, 1);
            kok.Controls.Add(secenekler, 0, 2);
            kok.Controls.Add(karsilastirma, 0, 3);
            kok.Controls.Add(alt, 0, 4);

            Controls.Add(kok);
        }

        // ------------------------------------------------------------------
        //  Akış
        // ------------------------------------------------------------------

        private void SonrakiGorev()
        {
            _sira++;

            if (_sira >= _gorevler.Count)
            {
                Bitir();
                return;
            }

            Gorev gorev = _gorevler[_sira];
            _cevaplandi = false;

            bool solHedef = _rnd.Next(2) == 0;
            _solKart = solHedef ? gorev.Hedef : gorev.Diger;
            _sagKart = solHedef ? gorev.Diger : gorev.Hedef;

            _lblBaslik.Text = $"Soru {_sira + 1}/{_gorevler.Count}";
            _lblIstem.Text = $"“{gorev.Hedef.Anlam}” anlamına gelen kelime hangisi?";

            _btnSol.Text = _solKart.Kelime;
            _btnSag.Text = _sagKart.Kelime;
            DugmeleriSifirla(_btnSol);
            DugmeleriSifirla(_btnSag);
            _btnSol.Enabled = true;
            _btnSag.Enabled = true;

            _rtbSol.Clear();
            _rtbSag.Clear();
            _lblSonuc.Text = string.Empty;
            _btnSonraki.Enabled = false;
            _btnSonraki.Text = _sira == _gorevler.Count - 1 ? "Bitir" : "Sonraki";
        }

        private void DugmeleriSifirla(Button dugme)
        {
            _tema.ButonuSifirla(dugme);
        }

        private void Sec(KelimeKarti secilen)
        {
            if (_cevaplandi) return;
            _cevaplandi = true;

            Gorev gorev = _gorevler[_sira];
            bool dogru = ReferenceEquals(secilen, gorev.Hedef);

            Button hedefDugme = ReferenceEquals(_solKart, gorev.Hedef) ? _btnSol : _btnSag;
            Button digerDugme = hedefDugme == _btnSol ? _btnSag : _btnSol;

            _tema.ButonuDogruYap(hedefDugme);
            if (!dogru)
                _tema.ButonuYanlisYap(digerDugme);

            if (dogru)
            {
                _dogru++;
                _s.Istatistik.KarisikligiAzalt(gorev.Hedef, gorev.Diger.Anlam);

                int sayi = _ciftDogru[gorev.Cift] + 1;
                _ciftDogru[gorev.Cift] = sayi;

                if (sayi == 2)
                {
                    _s.Profil.Say("cift_cozuldu");
                    _s.Profil.XpEkle(8);
                }
            }

            _lblSonuc.Text = dogru ? "Doğru! Farklara bak:" : "Yanlış. Farklara dikkatle bak:";

            KartiYaz(_rtbSol, _solKart, _sagKart);
            KartiYaz(_rtbSag, _sagKart, _solKart);

            _btnSonraki.Enabled = true;
            _btnSonraki.Focus();
        }

        private void Bitir()
        {
            _s.Istatistik.Kaydet();
            _s.Profil.Kaydet();

            _lblIstem.Text = $"Tamamlandı: {_dogru}/{_gorevler.Count} doğru.";
            _lblBaslik.Text = "Antrenman bitti";
            _btnSol.Visible = false;
            _btnSag.Visible = false;
            _lblSonuc.Text = "Doğru ayırt ettiğin çiftlerin karışıklık kaydı azaldı; öğrendikçe liste kısalır.";

            _btnSonraki.Text = "Kapat";
            _btnSonraki.Enabled = true;
            _btnSonraki.Click -= SonrakiTiklandi;
            _btnSonraki.Click += (sender, args) => Close();
        }

        // "Bitir" sonrasında Sonraki yerine Kapat çalışsın diye ayrı metot gerekir.
        private void SonrakiTiklandi(object? sender, EventArgs e) => SonrakiGorev();

        // ------------------------------------------------------------------
        //  Fark vurgulama
        // ------------------------------------------------------------------

        private void KartiYaz(RichTextBox kutu, KelimeKarti kart, KelimeKarti diger)
        {
            kutu.Clear();

            Color notr = kutu.ForeColor;
            Color vurgu = _tema.Palet.Uyari;

            KelimeFarkiYaz(kutu, kart.Kelime, diger.Kelime, notr, vurgu);
            Ekle(kutu, Environment.NewLine + Environment.NewLine, notr, false);

            AnlamFarkiYaz(kutu, kart.Anlam, diger.Anlam, notr, vurgu);

            if (kart.Ornek.Length > 0)
            {
                Ekle(kutu, Environment.NewLine + Environment.NewLine, notr, false);
                Ekle(kutu, kart.Ornek, notr, false);
            }
        }

        /// <summary>İki kelimenin ortak başını ve sonunu normal, farklı orta kısmı vurgulu yazar.</summary>
        private static void KelimeFarkiYaz(RichTextBox kutu, string a, string b, Color notr, Color vurgu)
        {
            int on = 0;
            while (on < a.Length && on < b.Length &&
                   char.ToLowerInvariant(a[on]) == char.ToLowerInvariant(b[on]))
                on++;

            int son = 0;
            while (son < a.Length - on && son < b.Length - on &&
                   char.ToLowerInvariant(a[a.Length - 1 - son]) == char.ToLowerInvariant(b[b.Length - 1 - son]))
                son++;

            Ekle(kutu, a.Substring(0, on), notr, true);
            Ekle(kutu, a.Substring(on, a.Length - on - son), vurgu, true);
            Ekle(kutu, a.Substring(a.Length - son), notr, true);
        }

        /// <summary>Diğer anlamda bulunmayan sözcükleri vurgulu yazar.</summary>
        private static void AnlamFarkiYaz(RichTextBox kutu, string a, string b, Color notr, Color vurgu)
        {
            var digeri = new HashSet<string>(
                Regex.Matches(b, @"\p{L}+").Select(m => MetinYardimcisi.Normalize(m.Value)));

            int konum = 0;
            foreach (Match eslesme in Regex.Matches(a, @"\p{L}+"))
            {
                Ekle(kutu, a.Substring(konum, eslesme.Index - konum), notr, false);

                bool ortak = digeri.Contains(MetinYardimcisi.Normalize(eslesme.Value));
                Ekle(kutu, eslesme.Value, ortak ? notr : vurgu, !ortak);

                konum = eslesme.Index + eslesme.Length;
            }

            Ekle(kutu, a.Substring(konum), notr, false);
        }

        private static void Ekle(RichTextBox kutu, string metin, Color renk, bool kalin)
        {
            if (metin.Length == 0) return;

            kutu.SelectionStart = kutu.TextLength;
            kutu.SelectionLength = 0;
            kutu.SelectionColor = renk;
            kutu.SelectionFont = TemaUygulayici.FontAl(
                kutu.Font.FontFamily.Name, kutu.Font.Size, kalin ? FontStyle.Bold : FontStyle.Regular, kutu.Font.Unit);
            kutu.AppendText(metin);
            kutu.SelectionColor = kutu.ForeColor;
        }
    }
}
