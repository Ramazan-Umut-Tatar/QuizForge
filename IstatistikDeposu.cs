#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace QuizForge
{
    public enum HataNedeni
    {
        Bilmiyordum = 0,
        Karistirdim = 1,
        Dikkatsizlik = 2
    }

    /// <summary>Bir sorunun sonucu: istatistiğe ve ustalık merdivenine ne yazılacağını belirler.</summary>
    public sealed class SonucBilgisi
    {
        public bool IlkDenemedeDogru { get; set; }
        public SoruTipi Tip { get; set; }
        public long SureMs { get; set; }
        public bool Akici { get; set; }
        public bool CumleVar { get; set; }
        public IReadOnlyList<string> YanlisSecilenler { get; set; } = Array.Empty<string>();
    }

    /// <summary>Quiz oturumunun istatistik kaynağından ihtiyaç duyduğu sözleşme.</summary>
    public interface IIstatistikDeposu
    {
        IReadOnlyDictionary<string, int> KarisanCevaplar(KelimeKarti kart);
        Seviye SeviyeAl(KelimeKarti kart);
        int SeriAl(KelimeKarti kart);
        SeviyeDegisimi SonucKaydet(KelimeKarti kart, SonucBilgisi bilgi);
        void HataNedeniKaydet(KelimeKarti kart, HataNedeni neden);
        void Kaydet();
    }

    public sealed class HataKaydi
    {
        public DateTime Zaman { get; set; }
        public string Anahtar { get; set; } = string.Empty;
        public string Kelime { get; set; } = string.Empty;
        public HataNedeni Neden { get; set; }
    }

    public sealed class KarisikCift
    {
        public KelimeKarti A { get; }
        public KelimeKarti B { get; }
        public int Sayi { get; set; }

        public KarisikCift(KelimeKarti a, KelimeKarti b, int sayi)
        {
            A = a;
            B = b;
            Sayi = sayi;
        }
    }

    public sealed class KelimeIstatistigi
    {
        public string Kelime { get; set; } = string.Empty;
        public string Anlam { get; set; } = string.Empty;
        public int Dogru { get; set; }
        public int Yanlis { get; set; }
        public DateTime SonGorulme { get; set; }
        public Dictionary<string, int> Karistirilanlar { get; set; } = new Dictionary<string, int>();

        // Ustalık merdiveni
        public Seviye Seviye { get; set; } = Seviye.Yeni;
        public int ArdisikDogru { get; set; }
        public DateTime? SonrakiTekrar { get; set; }

        // Cevap süresi (yalnızca ilk denemede doğru cevaplar)
        public long ToplamSureMs { get; set; }
        public int SureSayisi { get; set; }
        public long SonSureMs { get; set; }
        public bool? Akici { get; set; }

        public string? Not { get; set; }
        public Dictionary<string, int> HataNedenleri { get; set; } = new Dictionary<string, int>();

        [JsonIgnore] public int Toplam => Dogru + Yanlis;
        [JsonIgnore] public double BasariOrani => Toplam == 0 ? 0 : (double)Dogru / Toplam;
        [JsonIgnore] public double OrtSureMs => SureSayisi == 0 ? 0 : (double)ToplamSureMs / SureSayisi;

        /// <summary>Az denemeli kelimelerin tesadüfen en üste çıkmaması için yumuşatılmış hata oranı.</summary>
        [JsonIgnore] public double ZorlukPuani => (Yanlis + 1.0) / (Toplam + 2.0);
    }

    public sealed class IstatistikVerisi
    {
        public Dictionary<string, KelimeIstatistigi> Kelimeler { get; set; } = new Dictionary<string, KelimeIstatistigi>();
        public List<HataKaydi> HataGunlugu { get; set; } = new List<HataKaydi>();
    }

    public sealed class IstatistikDeposu : IIstatistikDeposu
    {
        private const int MaksHataKaydi = 2000;
        private static readonly IReadOnlyDictionary<string, int> Bos = new Dictionary<string, int>();

        private readonly string _yol;
        private readonly IstatistikVerisi _veri;
        private bool _degisti;

        public IstatistikDeposu(string? yol = null)
        {
            _yol = yol ?? Path.Combine(JsonDosya.VeriKlasoru, "istatistik.json");
            _veri = JsonDosya.Oku<IstatistikVerisi>(_yol);
        }

        // ------------------------------------------------------------------
        //  Oturumun kullandığı üyeler
        // ------------------------------------------------------------------

        public IReadOnlyDictionary<string, int> KarisanCevaplar(KelimeKarti kart)
        {
            if (_veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s))
                return s.Karistirilanlar;

            return Bos;
        }

        public Seviye SeviyeAl(KelimeKarti kart)
        {
            return _veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s) ? s.Seviye : Seviye.Yeni;
        }

        public int SeriAl(KelimeKarti kart)
        {
            return _veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s) ? s.ArdisikDogru : 0;
        }

        public SeviyeDegisimi SonucKaydet(KelimeKarti kart, SonucBilgisi bilgi)
        {
            KelimeIstatistigi s = Al(kart);

            if (bilgi.IlkDenemedeDogru) s.Dogru++;
            else s.Yanlis++;

            s.SonGorulme = DateTime.Now;

            foreach (string yanlis in bilgi.YanlisSecilenler)
            {
                s.Karistirilanlar.TryGetValue(yanlis, out int sayi);
                s.Karistirilanlar[yanlis] = sayi + 1;
            }

            if (bilgi.IlkDenemedeDogru)
            {
                if (bilgi.SureMs > 0)
                {
                    s.ToplamSureMs += bilgi.SureMs;
                    s.SureSayisi++;
                    s.SonSureMs = bilgi.SureMs;
                }
                s.Akici = bilgi.Akici;
            }
            else
            {
                s.Akici = null;
            }

            SeviyeDegisimi degisim = UstalikMerdiveni.Uygula(s, bilgi.IlkDenemedeDogru, bilgi.Akici, bilgi.Tip, bilgi.CumleVar);
            _degisti = true;
            return degisim;
        }

        public void HataNedeniKaydet(KelimeKarti kart, HataNedeni neden)
        {
            KelimeIstatistigi s = Al(kart);
            string ad = neden.ToString();
            s.HataNedenleri.TryGetValue(ad, out int sayi);
            s.HataNedenleri[ad] = sayi + 1;

            _veri.HataGunlugu.Add(new HataKaydi
            {
                Zaman = DateTime.Now,
                Anahtar = kart.Anahtar,
                Kelime = kart.Kelime,
                Neden = neden
            });

            if (_veri.HataGunlugu.Count > MaksHataKaydi)
                _veri.HataGunlugu.RemoveRange(0, _veri.HataGunlugu.Count - MaksHataKaydi);

            _degisti = true;
        }

        public void Kaydet()
        {
            if (!_degisti) return;
            JsonDosya.Yaz(_yol, _veri);
            _degisti = false;
        }

        // ------------------------------------------------------------------
        //  Panel, çift antrenmanı ve hatırlatıcı için sorgular
        // ------------------------------------------------------------------

        public KelimeIstatistigi? Bul(KelimeKarti kart)
        {
            return _veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s) ? s : null;
        }

        public string NotAl(KelimeKarti kart) => Bul(kart)?.Not ?? string.Empty;

        public void NotAyarla(KelimeKarti kart, string not)
        {
            Al(kart).Not = string.IsNullOrWhiteSpace(not) ? null : not.Trim();
            _degisti = true;
        }

        public bool VadesiGelmis(KelimeKarti kart, DateTime simdi)
        {
            return _veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s)
                   && s.SonrakiTekrar.HasValue
                   && s.SonrakiTekrar.Value <= simdi;
        }

        public List<KelimeKarti> VadesiGelenler(IEnumerable<KelimeKarti> kartlar)
        {
            DateTime simdi = DateTime.Now;
            return kartlar.Where(k => VadesiGelmis(k, simdi)).ToList();
        }

        public Dictionary<Seviye, int> SeviyeDagilimi(IEnumerable<KelimeKarti> kartlar)
        {
            var sonuc = new Dictionary<Seviye, int>
            {
                { Seviye.Yeni, 0 }, { Seviye.Ogreniliyor, 0 }, { Seviye.Pekisti, 0 }, { Seviye.Usta, 0 }
            };

            foreach (KelimeKarti kart in kartlar)
                sonuc[SeviyeAl(kart)]++;

            return sonuc;
        }

        public int AkiciOlmayanSayisi(IEnumerable<KelimeKarti> kartlar)
        {
            return kartlar.Count(k => Bul(k)?.Akici == false);
        }

        public int SeviyeSayisi(Seviye seviye) => _veri.Kelimeler.Values.Count(s => s.Seviye == seviye);

        /// <summary>En az bir kez yanlış yapılmış kelimeleri zorluk sırasına göre döndürür.</summary>
        public List<KelimeIstatistigi> EnZorKelimeler(int adet)
        {
            return _veri.Kelimeler.Values
                        .Where(k => k.Yanlis > 0)
                        .OrderByDescending(k => k.ZorlukPuani)
                        .ThenByDescending(k => k.Yanlis)
                        .Take(adet)
                        .ToList();
        }

        /// <summary>Belirtilen tarihten beri işaretlenen hata nedenlerinin dağılımı.</summary>
        public Dictionary<HataNedeni, int> HataDagilimi(DateTime baslangic)
        {
            var sonuc = new Dictionary<HataNedeni, int>();
            foreach (HataNedeni neden in Enum.GetValues(typeof(HataNedeni)))
                sonuc[neden] = 0;

            foreach (HataKaydi kayit in _veri.HataGunlugu.Where(k => k.Zaman >= baslangic))
                sonuc[kayit.Neden]++;

            return sonuc;
        }

        /// <summary>
        /// Şıklı sorularda birbirine karıştırılan kelime çiftlerini (A'nın yerine B'nin anlamı seçilmiş) bulur.
        /// (A,B) ve (B,A) tek çift olarak toplanır.
        /// </summary>
        public List<KarisikCift> KarisikCiftler(IReadOnlyList<KelimeKarti> kartlar, int adet)
        {
            var anlamIndeksi = new Dictionary<string, KelimeKarti>();
            foreach (KelimeKarti kart in kartlar)
            {
                if (!anlamIndeksi.ContainsKey(kart.Anlam))
                    anlamIndeksi[kart.Anlam] = kart;
            }

            var cift = new Dictionary<string, KarisikCift>();

            foreach (KelimeKarti a in kartlar)
            {
                if (!_veri.Kelimeler.TryGetValue(a.Anahtar, out KelimeIstatistigi? s)) continue;

                foreach (KeyValuePair<string, int> kayit in s.Karistirilanlar)
                {
                    if (kayit.Value <= 0) continue;
                    if (!anlamIndeksi.TryGetValue(kayit.Key, out KelimeKarti? b)) continue;
                    if (b.KelimeAnahtari == a.KelimeAnahtari) continue;

                    string anahtar = string.CompareOrdinal(a.Anahtar, b.Anahtar) < 0
                        ? a.Anahtar + "#" + b.Anahtar
                        : b.Anahtar + "#" + a.Anahtar;

                    if (cift.TryGetValue(anahtar, out KarisikCift? mevcut))
                        mevcut.Sayi += kayit.Value;
                    else
                        cift[anahtar] = new KarisikCift(a, b, kayit.Value);
                }
            }

            return cift.Values.OrderByDescending(c => c.Sayi).Take(adet).ToList();
        }

        /// <summary>Çift antrenmanında doğru ayırt edince karışıklık kaydını bir azaltır.</summary>
        public void KarisikligiAzalt(KelimeKarti kart, string karisanAnlam)
        {
            if (!_veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s)) return;
            if (!s.Karistirilanlar.TryGetValue(karisanAnlam, out int sayi)) return;

            if (sayi <= 1) s.Karistirilanlar.Remove(karisanAnlam);
            else s.Karistirilanlar[karisanAnlam] = sayi - 1;

            _degisti = true;
        }

        public void Sifirla()
        {
            _veri.Kelimeler.Clear();
            _veri.HataGunlugu.Clear();
            _degisti = true;
            Kaydet();
        }

        private KelimeIstatistigi Al(KelimeKarti kart)
        {
            if (!_veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s))
            {
                s = new KelimeIstatistigi { Kelime = kart.Kelime, Anlam = kart.Anlam };
                _veri.Kelimeler[kart.Anahtar] = s;
            }

            return s;
        }
    }
}
