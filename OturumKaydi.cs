#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuizForge
{
    /// <summary>Bir çalışma oturumunun sonuçları (soru sırasıyla).</summary>
    public sealed class OturumKaydi
    {
        public DateTime Baslangic { get; set; }
        public double SureDakika { get; set; }
        public List<bool> Dogrular { get; set; } = new List<bool>();   // ilk denemede doğru mu
        public List<int> Sureler { get; set; } = new List<int>();      // ilk cevap süresi (ms)
    }

    public sealed class OturumGecmisi
    {
        public List<OturumKaydi> Oturumlar { get; set; } = new List<OturumKaydi>();
    }

    public sealed class OturumDeposu
    {
        private const int MaksOturum = 80;

        private readonly string _yol;
        private readonly OturumGecmisi _veri;

        public OturumDeposu(string? yol = null)
        {
            _yol = yol ?? Path.Combine(JsonDosya.VeriKlasoru, "oturumlar.json");
            _veri = JsonDosya.Oku<OturumGecmisi>(_yol);
        }

        public IReadOnlyList<OturumKaydi> Oturumlar => _veri.Oturumlar;

        public void Ekle(OturumKaydi kayit)
        {
            if (kayit.Dogrular.Count == 0) return;

            _veri.Oturumlar.Add(kayit);
            if (_veri.Oturumlar.Count > MaksOturum)
                _veri.Oturumlar.RemoveRange(0, _veri.Oturumlar.Count - MaksOturum);

            JsonDosya.Yaz(_yol, _veri);
        }
    }

    public sealed class YorgunlukUyarisi
    {
        public double IlkDogruluk { get; }
        public double SonDogruluk { get; }
        public double IlkSureSn { get; }
        public double SonSureSn { get; }

        public YorgunlukUyarisi(double ilkDogruluk, double sonDogruluk, double ilkSureSn, double sonSureSn)
        {
            IlkDogruluk = ilkDogruluk;
            SonDogruluk = sonDogruluk;
            IlkSureSn = ilkSureSn;
            SonSureSn = sonSureSn;
        }
    }

    public sealed class IdealUzunlukSonucu
    {
        public int? SoruSayisi { get; }
        public int? Dakika { get; }
        public int OturumSayisi { get; }
        public string Aciklama { get; }

        public IdealUzunlukSonucu(int? soruSayisi, int? dakika, int oturumSayisi, string aciklama)
        {
            SoruSayisi = soruSayisi;
            Dakika = dakika;
            OturumSayisi = oturumSayisi;
            Aciklama = aciklama;
        }
    }

    public static class YorgunlukAnalizi
    {
        private const int Pencere = 8;
        private const int EnAzOturum = 5;

        /// <summary>
        /// Oturumun ilk 8 sorusuyla son 8 sorusunu karşılaştırır: doğruluk en az 25 puan düştüyse
        /// VE ortanca cevap süresi %30 veya daha fazla uzadıysa yorgunluk belirtisi sayılır.
        /// </summary>
        public static YorgunlukUyarisi? Kontrol(IReadOnlyList<bool> dogrular, IReadOnlyList<int> sureler)
        {
            if (dogrular.Count < Pencere * 2 || sureler.Count != dogrular.Count) return null;

            double ilkDogruluk = dogrular.Take(Pencere).Count(d => d) / (double)Pencere;
            double sonDogruluk = dogrular.Skip(dogrular.Count - Pencere).Count(d => d) / (double)Pencere;

            double ilkSure = Medyan(sureler.Take(Pencere));
            double sonSure = Medyan(sureler.Skip(sureler.Count - Pencere));

            bool dogrulukDustu = ilkDogruluk - sonDogruluk >= 0.25;
            bool sureUzadi = ilkSure > 0 && sonSure >= ilkSure * 1.3;

            if (dogrulukDustu && sureUzadi)
                return new YorgunlukUyarisi(ilkDogruluk, sonDogruluk, ilkSure / 1000.0, sonSure / 1000.0);

            return null;
        }

        /// <summary>
        /// Geçmiş oturumlardan, doğruluğun ilk 10 sorudaki seviyesine yakın kaldığı son soru aralığını bulur.
        /// En az 5 oturum (her biri en az 10 soru) gerekir.
        /// </summary>
        public static IdealUzunlukSonucu IdealUzunluk(IReadOnlyList<OturumKaydi> oturumlar)
        {
            const int Kova = 10;
            const int KovaBasinaEnAzSoru = 15;

            List<OturumKaydi> uygun = oturumlar.Where(o => o.Dogrular.Count >= Kova).ToList();

            if (uygun.Count < EnAzOturum)
            {
                return new IdealUzunlukSonucu(null, null, uygun.Count,
                    $"İdeal oturum uzunluğu için yeterli veri yok ({uygun.Count}/{EnAzOturum} oturum).");
            }

            int enUzun = uygun.Max(o => o.Dogrular.Count);
            int kovaSayisi = (enUzun + Kova - 1) / Kova;

            double? ilkOran = null;
            int ideal = Kova;

            for (int k = 0; k < kovaSayisi; k++)
            {
                int toplam = 0;
                int dogru = 0;

                foreach (OturumKaydi o in uygun)
                {
                    int baslangic = k * Kova;
                    int bitis = Math.Min(baslangic + Kova, o.Dogrular.Count);
                    for (int i = baslangic; i < bitis; i++)
                    {
                        toplam++;
                        if (o.Dogrular[i]) dogru++;
                    }
                }

                if (toplam < KovaBasinaEnAzSoru) break;

                double oran = (double)dogru / toplam;
                if (k == 0) ilkOran = oran;
                else if (ilkOran.HasValue && oran < ilkOran.Value - 0.10) break;

                ideal = (k + 1) * Kova;
            }

            double dakikaBasinaSoru = uygun.Average(o => o.SureDakika / o.Dogrular.Count);
            int dakika = Math.Max(1, (int)Math.Round(ideal * dakikaBasinaSoru));

            return new IdealUzunlukSonucu(ideal, dakika, uygun.Count,
                $"Kendi verine göre doğruluğun ilk ~{ideal} soruda yüksek kalıyor (yaklaşık {dakika} dakika). Sonrasında düşme eğilimi var.");
        }

        private static double Medyan(IEnumerable<int> degerler)
        {
            List<int> sirali = degerler.OrderBy(x => x).ToList();
            if (sirali.Count == 0) return 0;

            int orta = sirali.Count / 2;
            return sirali.Count % 2 == 1 ? sirali[orta] : (sirali[orta - 1] + sirali[orta]) / 2.0;
        }
    }
}
