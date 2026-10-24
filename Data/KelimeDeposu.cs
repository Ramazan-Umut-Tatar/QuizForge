#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuizForge
{
    public sealed class YuklemeSonucu
    {
        public List<KelimeKarti> Kartlar { get; } = new List<KelimeKarti>();
        public List<string> Uyarilar { get; } = new List<string>();
    }

    /// <summary>
    /// Klasördeki .txt kelime dosyalarını okur, doğrular ve yazar.
    /// Satır biçimi: kelime[TAB]anlam[TAB]örnek cümle(isteğe bağlı)[TAB]etiketler(isteğe bağlı, virgülle)
    /// Okunan dosyalar son değişiklik zamanı ve boyutuyla önbelleğe alınır; dosya değişmedikçe diskten
    /// yeniden okunmaz (ana ekran sayaçları, hatırlatıcı ve düzenleyiciler sık sık tüm desteyi ister).
    /// </summary>
    public sealed class KelimeDeposu
    {
        private const string Uzanti = ".txt";

        private sealed class DosyaOnbellegi
        {
            public DateTime Zaman { get; set; }
            public long Boyut { get; set; }
            public List<KelimeKarti> Kartlar { get; set; } = new List<KelimeKarti>();
            public List<string> Uyarilar { get; set; } = new List<string>();
        }

        private readonly string _klasorYolu;
        private readonly Dictionary<string, DosyaOnbellegi> _onbellek =
            new Dictionary<string, DosyaOnbellegi>(StringComparer.OrdinalIgnoreCase);

        public KelimeDeposu(string klasorYolu)
        {
            _klasorYolu = klasorYolu;
        }

        public List<string> DosyaIsimleriniGetir()
        {
            return Directory.GetFiles(_klasorYolu, "*" + Uzanti)
                            .Select(yol => Path.GetFileNameWithoutExtension(yol) ?? string.Empty)
                            .OrderBy(isim => isim, StringComparer.CurrentCultureIgnoreCase)
                            .ToList();
        }

        public string DosyaYolu(string dosyaIsmi) => Path.Combine(_klasorYolu, dosyaIsmi + Uzanti);

        /// <summary>Birden çok dosyayı birleştirir; birebir tekrarları eler ve uyarıları toplar.</summary>
        public YuklemeSonucu Yukle(IEnumerable<string> dosyaIsimleri)
        {
            var sonuc = new YuklemeSonucu();

            foreach (string isim in dosyaIsimleri)
                DosyayiOku(isim, sonuc);

            Dogrula(sonuc);
            return sonuc;
        }

        /// <summary>Düzenleyici için: tekrarları elemeden, dosyada ne varsa olduğu gibi okur.</summary>
        public YuklemeSonucu HamOku(string dosyaIsmi)
        {
            var sonuc = new YuklemeSonucu();
            DosyayiOku(dosyaIsmi, sonuc);
            return sonuc;
        }

        public List<KelimeKarti> TumKartlariYukle() => Yukle(DosyaIsimleriniGetir()).Kartlar;

        /// <summary>Dosyanın tamamını verilen kartlarla yeniden yazar (önceki sürüm .bak olarak saklanır).</summary>
        public void Kaydet(string dosyaIsmi, IEnumerable<KelimeKarti> kartlar)
        {
            string yol = DosyaYolu(dosyaIsmi);
            Directory.CreateDirectory(_klasorYolu);

            if (File.Exists(yol))
                File.Copy(yol, yol + ".bak", true);

            File.WriteAllLines(yol, kartlar.Select(SatirOlustur), new UTF8Encoding(false));
            _onbellek.Remove(dosyaIsmi);
        }

        /// <summary>Mevcut dosyaya dokunmadan sonuna yeni kartlar ekler.</summary>
        public void Ekle(string dosyaIsmi, IEnumerable<KelimeKarti> yeniKartlar)
        {
            string yol = DosyaYolu(dosyaIsmi);
            var metin = new StringBuilder(File.Exists(yol) ? File.ReadAllText(yol) : string.Empty);

            if (metin.Length > 0 && metin[metin.Length - 1] != '\n')
                metin.AppendLine();

            foreach (KelimeKarti kart in yeniKartlar)
                metin.AppendLine(SatirOlustur(kart));

            File.WriteAllText(yol, metin.ToString(), new UTF8Encoding(false));
            _onbellek.Remove(dosyaIsmi);
        }

        /// <summary>Boş bir kelime dosyası oluşturur ve temizlenmiş dosya adını döndürür.</summary>
        public string YeniDosyaOlustur(string istenenIsim)
        {
            char[] gecersiz = Path.GetInvalidFileNameChars();
            string temiz = new string((istenenIsim ?? string.Empty).Trim().Where(c => Array.IndexOf(gecersiz, c) < 0).ToArray());

            if (temiz.Length == 0)
                throw new ArgumentException("Geçersiz dosya adı.");

            string yol = DosyaYolu(temiz);
            if (!File.Exists(yol))
                File.WriteAllText(yol, string.Empty, new UTF8Encoding(false));

            _onbellek.Remove(temiz);
            return temiz;
        }

        // ------------------------------------------------------------------

        private void DosyayiOku(string dosyaIsmi, YuklemeSonucu sonuc)
        {
            string yol = DosyaYolu(dosyaIsmi);

            if (!File.Exists(yol))
            {
                _onbellek.Remove(dosyaIsmi);
                sonuc.Uyarilar.Add($"{dosyaIsmi}{Uzanti} bulunamadı.");
                return;
            }

            var bilgi = new FileInfo(yol);

            if (_onbellek.TryGetValue(dosyaIsmi, out DosyaOnbellegi? kayit)
                && kayit.Zaman == bilgi.LastWriteTimeUtc
                && kayit.Boyut == bilgi.Length)
            {
                // Kartlar değişmezdir; yalnızca listeler kopyalanır.
                sonuc.Kartlar.AddRange(kayit.Kartlar);
                sonuc.Uyarilar.AddRange(kayit.Uyarilar);
                return;
            }

            var kartlar = new List<KelimeKarti>();
            var uyarilar = new List<string>();

            if (!SatirlariOku(dosyaIsmi, yol, kartlar, uyarilar))
            {
                sonuc.Uyarilar.AddRange(uyarilar);
                return;
            }

            _onbellek[dosyaIsmi] = new DosyaOnbellegi
            {
                Zaman = bilgi.LastWriteTimeUtc,
                Boyut = bilgi.Length,
                Kartlar = kartlar,
                Uyarilar = uyarilar
            };

            sonuc.Kartlar.AddRange(kartlar);
            sonuc.Uyarilar.AddRange(uyarilar);
        }

        /// <summary>Dosyayı ayrıştırır. Okunamazsa false döner (uyarı listesine neden yazılır).</summary>
        private static bool SatirlariOku(string dosyaIsmi, string yol, List<KelimeKarti> kartlar, List<string> uyarilar)
        {
            string[] satirlar;
            try
            {
                satirlar = File.ReadAllLines(yol);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                uyarilar.Add($"{dosyaIsmi}{Uzanti} okunamadı: {ex.Message}");
                return false;
            }

            for (int i = 0; i < satirlar.Length; i++)
            {
                string satir = satirlar[i];
                if (string.IsNullOrWhiteSpace(satir)) continue;

                string[] parcalar = satir.Split('\t');
                if (parcalar.Length < 2 || parcalar.Length > 4)
                {
                    uyarilar.Add($"{dosyaIsmi}, satır {i + 1}: 2 ile 4 arasında TAB ile ayrılmış sütun bekleniyordu, satır atlandı.");
                    continue;
                }

                string kelime = parcalar[0].Trim();
                string anlam = parcalar[1].Trim();

                if (kelime.Length == 0 || anlam.Length == 0)
                {
                    uyarilar.Add($"{dosyaIsmi}, satır {i + 1}: kelime veya anlam boş, satır atlandı.");
                    continue;
                }

                string ornek = parcalar.Length > 2 ? parcalar[2].Trim() : string.Empty;
                List<string> etiketler = parcalar.Length > 3
                    ? MetinYardimcisi.EtiketleriAyir(parcalar[3])
                    : new List<string>();

                kartlar.Add(new KelimeKarti(kelime, anlam, dosyaIsmi, ornek, etiketler));
            }

            return true;
        }

        private static void Dogrula(YuklemeSonucu sonuc)
        {
            var gorulen = new Dictionary<string, KelimeKarti>();
            var temiz = new List<KelimeKarti>();

            foreach (KelimeKarti kart in sonuc.Kartlar)
            {
                if (gorulen.TryGetValue(kart.Anahtar, out KelimeKarti? ilk))
                {
                    string kaynak = ilk.Kaynak == kart.Kaynak ? ilk.Kaynak : ilk.Kaynak + ", " + kart.Kaynak;
                    sonuc.Uyarilar.Add($"Tekrar eden kayıt atlandı: \"{kart.Kelime}\" = \"{kart.Anlam}\" ({kaynak})");
                    continue;
                }

                gorulen[kart.Anahtar] = kart;
                temiz.Add(kart);
            }

            sonuc.Kartlar.Clear();
            sonuc.Kartlar.AddRange(temiz);

            foreach (DogrulamaUyarisi uyari in KelimeDogrulayici.Analiz(temiz))
                sonuc.Uyarilar.Add(uyari.Mesaj);
        }

        private static string Temizle(string? metin) =>
            (metin ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();

        private static string SatirOlustur(KelimeKarti kart)
        {
            var alanlar = new List<string>
            {
                Temizle(kart.Kelime),
                Temizle(kart.Anlam),
                Temizle(kart.Ornek),
                Temizle(string.Join(", ", kart.Etiketler))
            };

            // Boş kalan son sütunları yazma: eski 2 sütunlu dosyalar bozulmasın.
            while (alanlar.Count > 2 && alanlar[alanlar.Count - 1].Length == 0)
                alanlar.RemoveAt(alanlar.Count - 1);

            return string.Join("\t", alanlar);
        }
    }
}
