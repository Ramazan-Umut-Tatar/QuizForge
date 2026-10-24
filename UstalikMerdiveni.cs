#nullable enable
using System;
using System.Linq;

namespace QuizForge
{
    public enum Seviye
    {
        Yeni = 0,
        Ogreniliyor = 1,
        Pekisti = 2,
        Usta = 3
    }

    public enum SoruTipi
    {
        Sikli = 0,    // tanıma
        Yazili = 1,   // hatırlama
        Cumle = 2     // bağlam içinde hatırlama
    }

    public sealed class SeviyeDegisimi
    {
        public Seviye Onceki { get; }
        public Seviye Yeni { get; }
        public int Seri { get; }

        public SeviyeDegisimi(Seviye onceki, Seviye yeni, int seri)
        {
            Onceki = onceki;
            Yeni = yeni;
            Seri = seri;
        }

        public bool Terfi => Yeni > Onceki;
        public bool Dusus => Yeni < Onceki;
    }

    /// <summary>
    /// Ustalık merdiveninin kuralları:
    /// Yeni → şıklı, Öğreniliyor → yazılı, Pekişti/Usta → cümle içinde (örnek cümle yoksa yazılı).
    /// Üst seviyeye ancak gereken sayıda ÜST ÜSTE, İLK DENEMEDE ve AKICI doğrudan sonra çıkılır.
    /// Yanlış cevap seriyi sıfırlar ve (Yeni hariç) bir seviye düşürür.
    /// Doğru ama yavaş cevap seriyi ilerletmez, sıfırlamaz de.
    /// </summary>
    public static class UstalikMerdiveni
    {
        public static readonly TimeSpan YanlisAraligi = TimeSpan.FromHours(4);

        public static string Ad(Seviye seviye)
        {
            switch (seviye)
            {
                case Seviye.Yeni: return "Yeni";
                case Seviye.Ogreniliyor: return "Öğreniliyor";
                case Seviye.Pekisti: return "Pekişti";
                default: return "Usta";
            }
        }

        public static string TipAdi(SoruTipi tip)
        {
            switch (tip)
            {
                case SoruTipi.Sikli: return "Şıklı";
                case SoruTipi.Yazili: return "Yazılı";
                default: return "Cümlede";
            }
        }

        public static int GerekliSeri(Seviye seviye)
        {
            switch (seviye)
            {
                case Seviye.Yeni: return 2;
                case Seviye.Ogreniliyor: return 2;
                case Seviye.Pekisti: return 3;
                default: return 0;
            }
        }

        public static SoruTipi TipSec(Seviye seviye, bool cumleVar)
        {
            switch (seviye)
            {
                case Seviye.Yeni: return SoruTipi.Sikli;
                case Seviye.Ogreniliyor: return SoruTipi.Yazili;
                default: return cumleVar ? SoruTipi.Cumle : SoruTipi.Yazili;
            }
        }

        /// <summary>Verilen soru tipi, seviyenin istediği tip veya daha zorsa seri ilerleyebilir.</summary>
        public static bool SeriyeSayilir(Seviye seviye, SoruTipi verilen, bool cumleVar) =>
            verilen >= TipSec(seviye, cumleVar);

        public static TimeSpan Aralik(Seviye seviye)
        {
            switch (seviye)
            {
                case Seviye.Yeni: return TimeSpan.FromDays(1);
                case Seviye.Ogreniliyor: return TimeSpan.FromDays(2);
                case Seviye.Pekisti: return TimeSpan.FromDays(5);
                default: return TimeSpan.FromDays(14);
            }
        }

        public static SeviyeDegisimi Uygula(KelimeIstatistigi s, bool dogru, bool akici, SoruTipi tip, bool cumleVar)
        {
            Seviye onceki = s.Seviye;

            if (!dogru)
            {
                s.ArdisikDogru = 0;
                if (s.Seviye > Seviye.Yeni)
                    s.Seviye = (Seviye)((int)s.Seviye - 1);
            }
            else if (akici && SeriyeSayilir(s.Seviye, tip, cumleVar))
            {
                s.ArdisikDogru++;
                if (s.Seviye != Seviye.Usta && s.ArdisikDogru >= GerekliSeri(s.Seviye))
                {
                    s.Seviye = (Seviye)((int)s.Seviye + 1);
                    s.ArdisikDogru = 0;
                }
            }
            // Doğru ama yavaş (ya da daha kolay tipte) → seri değişmez.

            TimeSpan aralik = dogru
                ? (akici ? Aralik(s.Seviye) : TimeSpan.FromTicks(Aralik(s.Seviye).Ticks / 2))
                : YanlisAraligi;
            s.SonrakiTekrar = DateTime.Now + aralik;

            return new SeviyeDegisimi(onceki, s.Seviye, s.ArdisikDogru);
        }
    }

    /// <summary>Cevap süresi eşikleri: doğru ama yavaş bilinen kelime "akıcı değil" sayılır.</summary>
    public static class Akicilik
    {
        /// <summary>Bu süreden uzun bekleme (ör. çay molası) ölçüme dahil edilmez, üst sınırda kesilir.</summary>
        public const long AsiriSureMs = 60000;

        public static long EsikMs(SoruTipi tip, KelimeKarti kart)
        {
            switch (tip)
            {
                case SoruTipi.Sikli:
                    return 6000;
                case SoruTipi.Yazili:
                    int kisa = kart.Anlamlar.Count == 0 ? kart.NormAnlam.Length : kart.Anlamlar.Min(a => a.Length);
                    return 8000 + 300L * kisa;
                default:
                    return 10000 + 300L * kart.Kelime.Length;
            }
        }

        public static bool AkiciMi(SoruTipi tip, KelimeKarti kart, long sureMs) =>
            sureMs > 0 && sureMs <= EsikMs(tip, kart);
    }
}
