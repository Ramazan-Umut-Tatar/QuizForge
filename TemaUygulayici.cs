#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace QuizForge
{
    public enum TemaTuru
    {
        /// <summary>Koyu mavi-gri, düşük parlaklık: akşam ve uzun çalışmalar için.</summary>
        Sakin = 0,
        /// <summary>Sıcak kâğıt tonu: parlak ortamda beyazdan çok daha az göz yorar.</summary>
        Sepya = 1,
        /// <summary>Nötr koyu gri.</summary>
        Koyu = 2,
        /// <summary>Windows'un açık görünümü (eski varsayılan).</summary>
        Sistem = 3
    }

    /// <summary>Bir temanın tüm renkleri. Ozel=false ise Windows'un yerel görünümü bozulmaz.</summary>
    public sealed class TemaPaleti
    {
        public string Ad { get; set; } = string.Empty;
        public bool Koyu { get; set; }
        public bool Ozel { get; set; } = true;
        public string FontAilesi { get; set; } = "Segoe UI";

        public Color Zemin { get; set; }
        public Color Yuzey { get; set; }          // kartlar, liste başlıkları
        public Color Giris { get; set; }          // metin kutusu, liste, tablo zemini
        public Color Yazi { get; set; }
        public Color SolukYazi { get; set; }
        public Color Kenar { get; set; }
        public Color Vurgu { get; set; }
        public Color VurguYazi { get; set; }
        public Color ButonZemin { get; set; }
        public Color ButonUstunde { get; set; }
        public Color ButonBasili { get; set; }
        public Color Dogru { get; set; }
        public Color Yanlis { get; set; }
        public Color Uyari { get; set; }
        public Color DurumYazi { get; set; }      // Dogru/Yanlis/Uyari zeminleri üzerindeki koyu yazı
        public Color KartZemin { get; set; }      // soru kartı
        public Color KartYazi { get; set; }
        public Color SkorZemin { get; set; }      // üstteki ilerleme çubuğu alanı
        public Color SkorYazi { get; set; }
        public Color SecimZemin { get; set; }
        public Color SecimYazi { get; set; }
    }

    public static class TemaPaletleri
    {
        private static readonly Color Murekkep = Color.FromArgb(18, 24, 30);

        public static readonly TemaPaleti Sakin = new TemaPaleti
        {
            Ad = "Sakin gece (mavi-gri, önerilen)", Koyu = true,
            Zemin = Color.FromArgb(31, 36, 48),
            Yuzey = Color.FromArgb(40, 47, 62),
            Giris = Color.FromArgb(47, 55, 73),
            Yazi = Color.FromArgb(213, 219, 230),
            SolukYazi = Color.FromArgb(143, 155, 179),
            Kenar = Color.FromArgb(62, 72, 93),
            Vurgu = Color.FromArgb(127, 180, 202),
            VurguYazi = Color.FromArgb(14, 26, 36),
            ButonZemin = Color.FromArgb(50, 59, 79),
            ButonUstunde = Color.FromArgb(60, 71, 96),
            ButonBasili = Color.FromArgb(42, 50, 69),
            Dogru = Color.FromArgb(111, 191, 139),
            Yanlis = Color.FromArgb(217, 122, 122),
            Uyari = Color.FromArgb(224, 177, 92),
            DurumYazi = Murekkep,
            KartZemin = Color.FromArgb(38, 52, 74),
            KartYazi = Color.FromArgb(234, 240, 250),
            SkorZemin = Color.FromArgb(33, 44, 62),
            SkorYazi = Color.FromArgb(170, 200, 220),
            SecimZemin = Color.FromArgb(63, 110, 138),
            SecimYazi = Color.FromArgb(234, 240, 250)
        };

        public static readonly TemaPaleti Sepya = new TemaPaleti
        {
            Ad = "Sepya (sıcak kâğıt tonu)", Koyu = false,
            Zemin = Color.FromArgb(237, 230, 214),
            Yuzey = Color.FromArgb(245, 239, 226),
            Giris = Color.FromArgb(250, 246, 236),
            Yazi = Color.FromArgb(58, 54, 46),
            SolukYazi = Color.FromArgb(122, 115, 101),
            Kenar = Color.FromArgb(207, 197, 174),
            Vurgu = Color.FromArgb(94, 140, 106),
            VurguYazi = Color.FromArgb(247, 244, 234),
            ButonZemin = Color.FromArgb(227, 218, 198),
            ButonUstunde = Color.FromArgb(217, 207, 183),
            ButonBasili = Color.FromArgb(207, 196, 170),
            Dogru = Color.FromArgb(124, 184, 138),
            Yanlis = Color.FromArgb(214, 124, 108),
            Uyari = Color.FromArgb(214, 168, 80),
            DurumYazi = Murekkep,
            KartZemin = Color.FromArgb(226, 235, 222),
            KartYazi = Color.FromArgb(47, 59, 48),
            SkorZemin = Color.FromArgb(221, 230, 216),
            SkorYazi = Color.FromArgb(52, 80, 60),
            SecimZemin = Color.FromArgb(191, 211, 190),
            SecimYazi = Color.FromArgb(47, 59, 48)
        };

        public static readonly TemaPaleti Koyu = new TemaPaleti
        {
            Ad = "Koyu gri", Koyu = true,
            Zemin = Color.FromArgb(32, 32, 34),
            Yuzey = Color.FromArgb(40, 40, 43),
            Giris = Color.FromArgb(45, 45, 48),
            Yazi = Color.FromArgb(220, 220, 220),
            SolukYazi = Color.FromArgb(150, 150, 155),
            Kenar = Color.FromArgb(70, 70, 74),
            Vurgu = Color.FromArgb(100, 160, 210),
            VurguYazi = Color.FromArgb(16, 24, 32),
            ButonZemin = Color.FromArgb(58, 58, 62),
            ButonUstunde = Color.FromArgb(70, 70, 75),
            ButonBasili = Color.FromArgb(48, 48, 52),
            Dogru = Color.FromArgb(110, 190, 130),
            Yanlis = Color.FromArgb(215, 100, 100),
            Uyari = Color.FromArgb(225, 175, 80),
            DurumYazi = Murekkep,
            KartZemin = Color.FromArgb(45, 62, 85),
            KartYazi = Color.FromArgb(235, 240, 248),
            SkorZemin = Color.FromArgb(38, 52, 70),
            SkorYazi = Color.FromArgb(190, 210, 230),
            SecimZemin = Color.FromArgb(60, 95, 130),
            SecimYazi = Color.FromArgb(235, 240, 248)
        };

        public static readonly TemaPaleti Sistem = new TemaPaleti
        {
            Ad = "Sistem (açık)", Koyu = false, Ozel = false,
            Zemin = SystemColors.Control,
            Yuzey = SystemColors.Control,
            Giris = SystemColors.Window,
            Yazi = SystemColors.ControlText,
            SolukYazi = SystemColors.GrayText,
            Kenar = SystemColors.ControlDark,
            Vurgu = SystemColors.Highlight,
            VurguYazi = SystemColors.HighlightText,
            ButonZemin = SystemColors.Control,
            ButonUstunde = SystemColors.Control,
            ButonBasili = SystemColors.Control,
            Dogru = Color.LightGreen,
            Yanlis = Color.Red,
            Uyari = Color.DarkOrange,
            DurumYazi = Color.Black,
            KartZemin = SystemColors.Window,
            KartYazi = SystemColors.WindowText,
            SkorZemin = SystemColors.Control,
            SkorYazi = SystemColors.ControlText,
            SecimZemin = SystemColors.Highlight,
            SecimYazi = SystemColors.HighlightText
        };

        public static TemaPaleti Al(TemaTuru tur)
        {
            switch (tur)
            {
                case TemaTuru.Sakin: return Sakin;
                case TemaTuru.Sepya: return Sepya;
                case TemaTuru.Koyu: return Koyu;
                default: return Sistem;
            }
        }

        public static string Ad(TemaTuru tur) => Al(tur).Ad;
    }

    /// <summary>
    /// Bir form ağacına seçili temayı, düz (flat) buton stilini ve yazı boyutu ölçeğini uygular.
    /// Tag değerleri: "tema-yok" (renk dışı bırak), "tema-birincil" (vurgulu buton), "tema-kart" (kart zemini),
    /// "tema-baslik" (vurgu rengi etiket), "tema-soluk" (soluk etiket).
    /// </summary>
    public sealed class TemaUygulayici
    {
        public const string Atla = "tema-yok";
        public const string Birincil = "tema-birincil";
        public const string Kart = "tema-kart";
        public const string Baslik = "tema-baslik";
        public const string Soluk = "tema-soluk";

        // Aynı font tekrar tekrar üretilmesin (GDI nesnesi sızıntısını önler).
        private static readonly Dictionary<string, Font> FontOnbellegi = new Dictionary<string, Font>();

        private readonly Dictionary<Control, Font> _asilFontlar = new Dictionary<Control, Font>();
        private readonly Dictionary<Button, FlatStyle> _asilStiller = new Dictionary<Button, FlatStyle>();
        private readonly HashSet<ListView> _listeler = new HashSet<ListView>();

        public TemaUygulayici()
        {
            Palet = TemaPaletleri.Sistem;
        }

        public TemaUygulayici(UygulamaAyarlari ayarlar)
        {
            Palet = TemaPaletleri.Sistem;
            Ayarla(ayarlar.Tema, ayarlar.FontOlcegiYuzde);
        }

        public TemaPaleti Palet { get; private set; }
        public int FontOlcegiYuzde { get; private set; } = 100;

        public bool Karanlik => Palet.Koyu;
        public Color Zemin => Palet.Zemin;
        public Color Yazi => Palet.Yazi;
        public Color ButonZemin => Palet.ButonZemin;
        public Color GirisZemin => Palet.Giris;

        public void Ayarla(TemaTuru tur, int fontOlcegiYuzde)
        {
            Palet = TemaPaletleri.Al(tur);
            FontOlcegiYuzde = Math.Clamp(fontOlcegiYuzde, 80, 200);
        }

        public static Font FontAl(string aile, float boyut, FontStyle stil, GraphicsUnit birim)
        {
            string anahtar = aile + "|" + boyut.ToString("0.##", CultureInfo.InvariantCulture) + "|" + (int)stil + "|" + (int)birim;

            if (!FontOnbellegi.TryGetValue(anahtar, out Font? font))
            {
                font = new Font(aile, boyut, stil, birim);
                FontOnbellegi[anahtar] = font;
            }

            return font;
        }

        public void Uygula(Control kok)
        {
            // Önce tüm orijinal değerleri kaydet (ebeveynin fontu değişmeden), sonra uygula.
            Kaydet(kok);
            Isle(kok, false);
        }

        // ------------------------------------------------------------------
        //  Durum renkleri (doğru / yanlış) ve özel alanlar
        // ------------------------------------------------------------------

        /// <summary>Geçici durum renginden sonra butonu tema stiline döndürür.</summary>
        public void ButonuSifirla(Button buton) => ButonStil(buton);

        public void ButonuDogruYap(Button buton)
        {
            buton.UseVisualStyleBackColor = false;
            buton.BackColor = Palet.Dogru;
            buton.ForeColor = Palet.DurumYazi;
        }

        public void ButonuYanlisYap(Button buton)
        {
            buton.UseVisualStyleBackColor = false;
            buton.BackColor = Palet.Yanlis;
            buton.ForeColor = Palet.DurumYazi;
        }

        /// <summary>Soru kartının (kelime kutusu) renklerini paletten verir. Sistem temasında tasarımdaki renkler korunur.</summary>
        public void KartRenkleriUygula(Control kart)
        {
            if (!Palet.Ozel) return;
            kart.BackColor = Palet.KartZemin;
            kart.ForeColor = Palet.KartYazi;
        }

        public void SkorRenkleriUygula(Control panel, params Label[] etiketler)
        {
            if (!Palet.Ozel) return;

            panel.BackColor = Palet.SkorZemin;
            foreach (Label etiket in etiketler)
                etiket.ForeColor = Palet.SkorYazi;
        }

        // ------------------------------------------------------------------
        //  Ağaç dolaşımı
        // ------------------------------------------------------------------

        private void Kaydet(Control kontrol)
        {
            if (!_asilFontlar.ContainsKey(kontrol))
                _asilFontlar[kontrol] = kontrol.Font;

            if (kontrol is Button buton && !_asilStiller.ContainsKey(buton))
                _asilStiller[buton] = buton.FlatStyle;

            foreach (Control alt in kontrol.Controls)
                Kaydet(alt);
        }

        private void Isle(Control kontrol, bool ustAtla)
        {
            bool renkAtla = ustAtla || Equals(kontrol.Tag, Atla);

            if (!renkAtla)
                RenkUygula(kontrol);

            FontUygula(kontrol);

            foreach (Control alt in kontrol.Controls)
                Isle(alt, renkAtla);
        }

        private void RenkUygula(Control kontrol)
        {
            switch (kontrol)
            {
                case Form form:
                    form.BackColor = Palet.Zemin;
                    form.ForeColor = Palet.Yazi;
                    break;

                case TabPage sayfa:
                    sayfa.UseVisualStyleBackColor = !Palet.Ozel;
                    sayfa.BackColor = Palet.Zemin;
                    sayfa.ForeColor = Palet.Yazi;
                    break;

                case Button buton:
                    ButonStil(buton);
                    break;

                case DataGridView tablo:
                    TabloStil(tablo);
                    break;

                case RichTextBox zengin:
                    GirisRenkleri(zengin);
                    if (zengin.BorderStyle != BorderStyle.None) zengin.BorderStyle = KenarlikStili();
                    break;

                case TextBox kutu:
                    GirisRenkleri(kutu);
                    if (kutu.BorderStyle != BorderStyle.None) kutu.BorderStyle = KenarlikStili();
                    break;

                case ListBox kutuListe:
                    GirisRenkleri(kutuListe);
                    if (kutuListe.BorderStyle != BorderStyle.None) kutuListe.BorderStyle = KenarlikStili();
                    break;

                case ListView gorunum:
                    ListeStil(gorunum);
                    break;

                case ComboBox acilir:
                    GirisRenkleri(acilir);
                    acilir.FlatStyle = Palet.Ozel ? FlatStyle.Flat : FlatStyle.Standard;
                    break;

                case NumericUpDown sayi:
                    GirisRenkleri(sayi);
                    if (sayi.BorderStyle != BorderStyle.None) sayi.BorderStyle = KenarlikStili();
                    break;

                case Label etiket:
                    if (Equals(etiket.Tag, Baslik)) etiket.ForeColor = Palet.Vurgu;
                    else if (Equals(etiket.Tag, Soluk)) etiket.ForeColor = Palet.SolukYazi;
                    else etiket.ForeColor = Palet.Yazi;
                    break;

                case CheckBox _:
                case RadioButton _:
                case GroupBox _:
                    kontrol.ForeColor = Palet.Yazi;
                    break;

                case Bar bar:
                    bar.BackColor = Palet.Kenar;
                    bar.DolguRengi = Palet.Vurgu;
                    break;

                case Panel panel when Equals(panel.Tag, Kart):
                    panel.BackColor = Palet.Ozel ? Palet.Yuzey : Color.Empty;
                    break;
            }
        }

        private void GirisRenkleri(Control kontrol)
        {
            kontrol.BackColor = Palet.Giris;
            kontrol.ForeColor = Palet.Yazi;
        }

        private BorderStyle KenarlikStili() => Palet.Ozel ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;

        private void ButonStil(Button buton)
        {
            if (!Palet.Ozel)
            {
                if (_asilStiller.TryGetValue(buton, out FlatStyle asil))
                    buton.FlatStyle = asil;

                buton.BackColor = Palet.ButonZemin;
                buton.ForeColor = Palet.Yazi;
                buton.UseVisualStyleBackColor = true;
                return;
            }

            bool birincil = Equals(buton.Tag, Birincil);

            buton.FlatStyle = FlatStyle.Flat;
            buton.UseVisualStyleBackColor = false;
            buton.BackColor = birincil ? Palet.Vurgu : Palet.ButonZemin;
            buton.ForeColor = birincil ? Palet.VurguYazi : Palet.Yazi;
            buton.FlatAppearance.BorderSize = birincil ? 0 : 1;
            buton.FlatAppearance.BorderColor = Palet.Kenar;
            buton.FlatAppearance.MouseOverBackColor = birincil ? Aydinlat(Palet.Vurgu, 0.12f) : Palet.ButonUstunde;
            buton.FlatAppearance.MouseDownBackColor = birincil ? Koyulastir(Palet.Vurgu, 0.12f) : Palet.ButonBasili;
            buton.Cursor = Cursors.Hand;
        }

        private void TabloStil(DataGridView tablo)
        {
            bool ozel = Palet.Ozel;

            tablo.BackgroundColor = ozel ? Palet.Zemin : SystemColors.AppWorkspace;
            tablo.GridColor = Palet.Kenar;
            tablo.BorderStyle = ozel ? BorderStyle.None : BorderStyle.FixedSingle;
            tablo.CellBorderStyle = ozel ? DataGridViewCellBorderStyle.SingleHorizontal : DataGridViewCellBorderStyle.Single;
            tablo.EnableHeadersVisualStyles = !ozel;
            tablo.ColumnHeadersBorderStyle = ozel ? DataGridViewHeaderBorderStyle.Single : DataGridViewHeaderBorderStyle.Raised;
            tablo.RowHeadersBorderStyle = ozel ? DataGridViewHeaderBorderStyle.Single : DataGridViewHeaderBorderStyle.Raised;

            tablo.DefaultCellStyle.BackColor = Palet.Giris;
            tablo.DefaultCellStyle.ForeColor = Palet.Yazi;
            tablo.DefaultCellStyle.SelectionBackColor = Palet.SecimZemin;
            tablo.DefaultCellStyle.SelectionForeColor = Palet.SecimYazi;
            tablo.AlternatingRowsDefaultCellStyle.BackColor = ozel ? Palet.Yuzey : Color.Empty;

            tablo.ColumnHeadersDefaultCellStyle.BackColor = Palet.Yuzey;
            tablo.ColumnHeadersDefaultCellStyle.ForeColor = ozel ? Palet.SolukYazi : Palet.Yazi;
            tablo.ColumnHeadersDefaultCellStyle.SelectionBackColor = Palet.Yuzey;
            tablo.RowHeadersDefaultCellStyle.BackColor = Palet.Yuzey;
            tablo.RowHeadersDefaultCellStyle.ForeColor = Palet.Yazi;
            tablo.RowHeadersDefaultCellStyle.SelectionBackColor = Palet.Yuzey;
        }

        private void ListeStil(ListView liste)
        {
            GirisRenkleri(liste);
            if (liste.BorderStyle != BorderStyle.None) liste.BorderStyle = KenarlikStili();

            if (_listeler.Add(liste))
            {
                liste.DrawColumnHeader += ListeBasligiCiz;
                liste.DrawItem += (s, e) => e.DrawDefault = true;
                liste.DrawSubItem += (s, e) => e.DrawDefault = true;
            }

            // Özel sütun başlığı yalnızca renkli temalarda çizilir; Sistem temasında yerel görünüm korunur.
            liste.OwnerDraw = Palet.Ozel;
        }

        private void ListeBasligiCiz(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var arka = new SolidBrush(Palet.Yuzey))
                e.Graphics.FillRectangle(arka, e.Bounds);

            using (var kalem = new Pen(Palet.Kenar))
                e.Graphics.DrawLine(kalem, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            Rectangle metinAlani = Rectangle.Inflate(e.Bounds, -6, 0);
            TextRenderer.DrawText(
                e.Graphics,
                e.Header?.Text ?? string.Empty,
                e.Font,
                metinAlani,
                Palet.SolukYazi,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void FontUygula(Control kontrol)
        {
            // Form/ContainerControl fontunu değiştirmek AutoScaleMode.Font ile formu yeniden ölçeklendirir.
            if (kontrol is ContainerControl) return;
            if (!_asilFontlar.TryGetValue(kontrol, out Font? asil)) return;

            string aile = Palet.Ozel ? Palet.FontAilesi : asil.FontFamily.Name;
            float boyut = asil.Size * FontOlcegiYuzde / 100f;
            Font hedef = FontAl(aile, boyut, asil.Style, asil.Unit);

            if (!ReferenceEquals(kontrol.Font, hedef) && !kontrol.Font.Equals(hedef))
                kontrol.Font = hedef;
        }

        private static Color Aydinlat(Color renk, float oran) => Color.FromArgb(
            renk.A,
            renk.R + (int)((255 - renk.R) * oran),
            renk.G + (int)((255 - renk.G) * oran),
            renk.B + (int)((255 - renk.B) * oran));

        private static Color Koyulastir(Color renk, float oran) => Color.FromArgb(
            renk.A,
            (int)(renk.R * (1 - oran)),
            (int)(renk.G * (1 - oran)),
            (int)(renk.B * (1 - oran)));
    }
}
