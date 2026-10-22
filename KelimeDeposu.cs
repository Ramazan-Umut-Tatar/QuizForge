#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuizForge
{
    public sealed class YuklemeSonucu
    {
        public List<KelimeKarti> Kartlar { get; } = new List<KelimeKarti>();
        public List<string> Uyarilar { get; } = new List<string>();
    }

    /// <summary>
    /// Klasördeki .txt dosyalarını bulur, birden çok dosyayı birleştirerek okur ve doğrular.
    /// Dosya biçimi: her satırda "kelime[TAB]anlam".
    /// </summary>
    public sealed class KelimeDeposu
    {
        private const string Uzanti = ".txt";
        private readonly string _klasorYolu;

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

        public YuklemeSonucu Yukle(IEnumerable<string> dosyaIsimleri)
        {
            var sonuc = new YuklemeSonucu();

            foreach (string isim in dosyaIsimleri)
                DosyayiOku(isim, sonuc);

            Dogrula(sonuc);
            return sonuc;
        }

        private void DosyayiOku(string dosyaIsmi, YuklemeSonucu sonuc)
        {
            string yol = Path.Combine(_klasorYolu, dosyaIsmi + Uzanti);

            if (!File.Exists(yol))
            {
                sonuc.Uyarilar.Add($"{dosyaIsmi}{Uzanti} bulunamadı.");
                return;
            }

            string[] satirlar;
            try
            {
                satirlar = File.ReadAllLines(yol);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                sonuc.Uyarilar.Add($"{dosyaIsmi}{Uzanti} okunamadı: {ex.Message}");
                return;
            }

            for (int i = 0; i < satirlar.Length; i++)
            {
                string satir = satirlar[i];
                if (string.IsNullOrWhiteSpace(satir)) continue;

                string[] parcalar = satir.Split('\t');
                if (parcalar.Length != 2)
                {
                    sonuc.Uyarilar.Add($"{dosyaIsmi}, satır {i + 1}: TAB ile ayrılmış 2 sütun bekleniyordu, satır atlandı.");
                    continue;
                }

                string kelime = parcalar[0].Trim();
                string anlam = parcalar[1].Trim();

                if (kelime.Length == 0 || anlam.Length == 0)
                {
                    sonuc.Uyarilar.Add($"{dosyaIsmi}, satır {i + 1}: kelime veya anlam boş, satır atlandı.");
                    continue;
                }

                sonuc.Kartlar.Add(new KelimeKarti(kelime, anlam, dosyaIsmi));
            }
        }

        /// <summary>
        /// 1) Birebir aynı kayıtları (kelime + anlam) eler.
        /// 2) Aynı kelimenin farklı anlamlarını ve aynı anlamı paylaşan farklı kelimeleri uyarı olarak bildirir
        ///    (şıklarda iki doğru cevap çıkmasına yol açabilecek durumlar).
        /// </summary>
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

            // Aynı kelime, farklı anlam
            foreach (var grup in temiz.GroupBy(k => k.KelimeAnahtari).Where(g => g.Count() > 1))
            {
                sonuc.Uyarilar.Add(
                    $"\"{grup.First().Kelime}\" birden fazla anlamla tanımlı: {string.Join(" | ", grup.Select(k => k.Anlam))}");
            }

            // Aynı anlam, farklı kelime
            var ortakAnlamlar = temiz
                .SelectMany(k => k.Anlamlar.Select(a => new { Anlam = a, Kart = k }))
                .GroupBy(x => x.Anlam)
                .Where(g => g.Select(x => x.Kart.KelimeAnahtari).Distinct().Count() > 1);

            foreach (var grup in ortakAnlamlar)
            {
                string kelimeler = string.Join(", ", grup.Select(x => x.Kart.Kelime).Distinct());
                sonuc.Uyarilar.Add($"Aynı anlam birden fazla kelimede: \"{grup.Key}\" → {kelimeler}");
            }
        }
    }
}
