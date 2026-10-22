#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuizForge
{
    public enum SoruModu
    {
        Sikli = 0,
        Yazili = 1
    }

    public enum CevapSonucu
    {
        Yanlis,
        Dogru,
        /// <summary>Kullanıcı "Bilmiyorum" dedi veya yazılı moddaki hakkı bitti; doğru cevap gösterilip soru geçildi.</summary>
        Gecildi
    }

    public sealed class QuizAyarlari
    {
        public SoruModu Mod { get; set; } = SoruModu.Sikli;
        public bool YanlisiTekrarSor { get; set; } = true;
        public int TekrarAraligi { get; set; } = 4;          // yanlıştan kaç soru sonra tekrar sorulsun
        public int MaksimumYazmaDenemesi { get; set; } = 3;   // yazılı modda yanlış hakkı
        public int SoruPuani { get; set; } = 10;
    }

    /// <summary>Ekranda gösterilecek tek bir soru. Yazılı modda Secenekler boştur.</summary>
    public sealed class QuizSorusu
    {
        public KelimeKarti Kart { get; }
        public IReadOnlyList<string> Secenekler { get; }
        public int DogruIndex { get; }
        public bool Tekrar { get; }

        public QuizSorusu(KelimeKarti kart, IReadOnlyList<string> secenekler, int dogruIndex, bool tekrar)
        {
            Kart = kart;
            Secenekler = secenekler;
            DogruIndex = dogruIndex;
            Tekrar = tekrar;
        }
    }

    /// <summary>Oturum boyunca yanlış yapılan bir kelimenin kaydı (özet ekranı için).</summary>
    public sealed class YanlisKaydi
    {
        public KelimeKarti Kart { get; }
        public int YanlisDeneme { get; internal set; }
        public List<string> VerilenCevaplar { get; } = new List<string>();

        public YanlisKaydi(KelimeKarti kart)
        {
            Kart = kart;
        }
    }

    /// <summary>
    /// Bir quiz oturumunun tüm kurallarını yönetir. Windows Forms bağımlılığı yoktur.
    /// </summary>
    public sealed class QuizOturumu
    {
        public const int SecenekSayisi = 4;
        private const int KartBasinaMaksTekrar = 2;

        private readonly QuizAyarlari _ayar;
        private readonly IIstatistikDeposu? _istatistik;
        private readonly Random _rnd = Random.Shared;

        private readonly List<KelimeKarti> _tumKartlar;                         // şık havuzu
        private readonly List<KelimeKarti> _ana;                                // henüz ilk kez çözülmemiş kartlar
        private readonly List<(KelimeKarti Kart, int Zaman)> _tekrar = new List<(KelimeKarti Kart, int Zaman)>();
        private readonly Dictionary<string, int> _tekrarSayisi = new Dictionary<string, int>();
        private readonly Dictionary<string, HashSet<string>> _kelimeAnlamlari;  // kelime → kabul edilen tüm anlamlar
        private readonly Dictionary<string, YanlisKaydi> _yanlislar = new Dictionary<string, YanlisKaydi>();
        private readonly List<string> _karisanSecenekler = new List<string>();

        private int _soruSayaci;
        private int _yanlisDeneme;

        public int ToplamSoru { get; }
        public int Cevaplanan { get; private set; }
        public int Puan { get; private set; }
        public QuizSorusu? MevcutSoru { get; private set; }
        public KelimeKarti? SonAcilanKart { get; private set; }
        public bool Bitti => MevcutSoru is null;
        public int KalanDeneme => Math.Max(0, _ayar.MaksimumYazmaDenemesi - _yanlisDeneme);
        public IReadOnlyList<KelimeKarti> HavuzKartlari => _tumKartlar;

        public IReadOnlyList<YanlisKaydi> YanlisKayitlari =>
            _yanlislar.Values.OrderByDescending(k => k.YanlisDeneme).ToList();

        /// <param name="kartlar">Bu oturumda sorulacak kartlar.</param>
        /// <param name="havuz">Yanlış şık havuzu (verilmezse sadece kartlar kullanılır).</param>
        public QuizOturumu(
            IEnumerable<KelimeKarti> kartlar,
            QuizAyarlari? ayar = null,
            IIstatistikDeposu? istatistik = null,
            IEnumerable<KelimeKarti>? havuz = null)
        {
            _ayar = ayar ?? new QuizAyarlari();
            _istatistik = istatistik;

            _ana = kartlar.ToList();
            ToplamSoru = _ana.Count;

            _tumKartlar = (havuz ?? _ana).Concat(_ana).Distinct().ToList();

            _kelimeAnlamlari = _tumKartlar
                .GroupBy(k => k.KelimeAnahtari)
                .ToDictionary(g => g.Key, g => new HashSet<string>(g.SelectMany(k => k.Anlamlar)));

            SonrakiSoruyuHazirla();
        }

        // ------------------------------------------------------------------
        //  Cevap verme
        // ------------------------------------------------------------------

        /// <summary>Şıklı mod: verilen şıkkı değerlendirir.</summary>
        public CevapSonucu Cevapla(int secenekIndex)
        {
            QuizSorusu soru = MevcutSoruyuAl();

            if (secenekIndex == soru.DogruIndex)
                return DogruCevap();

            string secilen = soru.Secenekler[secenekIndex];
            _karisanSecenekler.Add(secilen);
            return YanlisCevap(soru.Kart, secilen);
        }

        /// <summary>Yazılı mod: yazılan metni değerlendirir (çok anlamlı cevaplarda herhangi biri yeterli).</summary>
        public CevapSonucu CevapYaz(string metin)
        {
            QuizSorusu soru = MevcutSoruyuAl();

            if (YazilanCevapDogruMu(soru.Kart, metin))
                return DogruCevap();

            YanlisCevap(soru.Kart, metin.Trim());

            if (_yanlisDeneme >= _ayar.MaksimumYazmaDenemesi)
                return Gec(soru.Kart);

            return CevapSonucu.Yanlis;
        }

        /// <summary>Doğru cevabı gösterip soruyu yanlış sayarak geçer.</summary>
        public CevapSonucu Bilmiyorum()
        {
            QuizSorusu soru = MevcutSoruyuAl();
            return Gec(soru.Kart);
        }

        // ------------------------------------------------------------------
        //  Dahili akış
        // ------------------------------------------------------------------

        private QuizSorusu MevcutSoruyuAl()
        {
            SonAcilanKart = null;
            return MevcutSoru ?? throw new InvalidOperationException("Cevaplanacak soru kalmadı.");
        }

        private CevapSonucu DogruCevap()
        {
            Tamamla();
            return CevapSonucu.Dogru;
        }

        private CevapSonucu YanlisCevap(KelimeKarti kart, string verilen)
        {
            _yanlisDeneme++;

            YanlisKaydi kayit = KayitAl(kart);
            kayit.YanlisDeneme++;
            if (!string.IsNullOrWhiteSpace(verilen))
                kayit.VerilenCevaplar.Add(verilen);

            return CevapSonucu.Yanlis;
        }

        private CevapSonucu Gec(KelimeKarti kart)
        {
            if (_yanlisDeneme == 0)
            {
                _yanlisDeneme = 1;
                KayitAl(kart).YanlisDeneme++;
            }

            SonAcilanKart = kart;
            Tamamla();
            return CevapSonucu.Gecildi;
        }

        private YanlisKaydi KayitAl(KelimeKarti kart)
        {
            if (!_yanlislar.TryGetValue(kart.Anahtar, out YanlisKaydi? kayit))
            {
                kayit = new YanlisKaydi(kart);
                _yanlislar[kart.Anahtar] = kayit;
            }

            return kayit;
        }

        /// <summary>Mevcut soru çözüldü (doğru bulundu ya da geçildi): puan, istatistik, tekrar kuyruğu, sıradaki soru.</summary>
        private void Tamamla()
        {
            QuizSorusu soru = MevcutSoru!;
            KelimeKarti kart = soru.Kart;
            bool ilkDenemedeDogru = _yanlisDeneme == 0;

            // Yalnızca şıklı modda seçilen yanlış şıklar "karıştırılan cevap" olarak saklanır.
            IEnumerable<string> karisanlar = _ayar.Mod == SoruModu.Sikli
                ? (IEnumerable<string>)_karisanSecenekler
                : Array.Empty<string>();
            _istatistik?.SonucKaydet(kart, ilkDenemedeDogru, karisanlar);

            if (soru.Tekrar)
            {
                _tekrar.RemoveAll(t => ReferenceEquals(t.Kart, kart));
            }
            else
            {
                _ana.Remove(kart);
                Cevaplanan++;
                if (ilkDenemedeDogru) Puan += _ayar.SoruPuani;
            }

            if (!ilkDenemedeDogru && _ayar.YanlisiTekrarSor)
            {
                _tekrarSayisi.TryGetValue(kart.Anahtar, out int simdiki);
                if (simdiki < KartBasinaMaksTekrar)
                {
                    _tekrarSayisi[kart.Anahtar] = simdiki + 1;
                    _tekrar.Add((kart, _soruSayaci + _ayar.TekrarAraligi));
                }
            }

            _soruSayaci++;
            SonrakiSoruyuHazirla();
        }

        private void SonrakiSoruyuHazirla()
        {
            _yanlisDeneme = 0;
            _karisanSecenekler.Clear();

            KelimeKarti? kart = null;
            bool tekrar = false;

            List<(KelimeKarti Kart, int Zaman)> vadesiGelen =
                _tekrar.Where(t => t.Zaman <= _soruSayaci).OrderBy(t => t.Zaman).ToList();

            if (vadesiGelen.Count > 0)
            {
                kart = vadesiGelen[0].Kart;
                tekrar = true;
            }
            else if (_ana.Count > 0)
            {
                kart = _ana[_rnd.Next(_ana.Count)];
            }
            else if (_tekrar.Count > 0)
            {
                kart = _tekrar.OrderBy(t => t.Zaman).First().Kart;
                tekrar = true;
            }

            if (kart is null)
            {
                MevcutSoru = null;
                return;
            }

            if (_ayar.Mod == SoruModu.Yazili)
            {
                MevcutSoru = new QuizSorusu(kart, Array.Empty<string>(), -1, tekrar);
                return;
            }

            List<string> secenekler = YanlisSecenekleriSec(kart);
            int dogruIndex = _rnd.Next(secenekler.Count + 1);
            secenekler.Insert(dogruIndex, kart.Anlam);

            MevcutSoru = new QuizSorusu(kart, secenekler, dogruIndex, tekrar);
        }

        // ------------------------------------------------------------------
        //  Akıllı yanlış şık seçimi
        // ------------------------------------------------------------------

        private List<string> YanlisSecenekleriSec(KelimeKarti dogru)
        {
            // Aynı kelimenin tüm anlamları ve doğru cevapla ortak alternatifi olan anlamlar yasak:
            // aksi halde şıklarda iki doğru cevap çıkabilir.
            var yasak = new HashSet<string>(_kelimeAnlamlari[dogru.KelimeAnahtari]);
            yasak.UnionWith(dogru.Anlamlar);

            IReadOnlyDictionary<string, int>? karisanlar = _istatistik?.KarisanCevaplar(dogru);

            return _tumKartlar
                .Where(k => k.Anlam != dogru.Anlam && !yasak.Overlaps(k.Anlamlar))
                .GroupBy(k => k.Anlam)
                .Select(g => g.First())
                .Select(k => new { k.Anlam, Skor = BenzerlikPuani(dogru, k, karisanlar) })
                .OrderByDescending(x => x.Skor)
                .Take(SecenekSayisi - 1)
                .Select(x => x.Anlam)
                .ToList();
        }

        /// <summary>
        /// Yüksek puan = daha "inandırıcı" yanlış şık:
        /// benzer uzunluk, benzer alternatif sayısı, aynı son ek (-mak/-mek gibi) ve geçmişte karıştırılmış olması.
        /// Küçük rastgele katkı, aynı şıkların sürekli tekrarlanmasını önler.
        /// </summary>
        private double BenzerlikPuani(KelimeKarti dogru, KelimeKarti aday, IReadOnlyDictionary<string, int>? karisanlar)
        {
            double skor = -Math.Abs(dogru.Anlam.Length - aday.Anlam.Length) * 0.5;
            skor -= Math.Abs(dogru.Anlamlar.Count - aday.Anlamlar.Count);

            if (SonEk(dogru.NormAnlam) == SonEk(aday.NormAnlam))
                skor += 3;

            if (karisanlar != null && karisanlar.TryGetValue(aday.Anlam, out int kez))
                skor += 50 + Math.Min(kez, 5);

            return skor + _rnd.NextDouble() * 3;
        }

        private static string SonEk(string metin) =>
            metin.Length >= 3 ? metin.Substring(metin.Length - 3) : metin;

        // ------------------------------------------------------------------
        //  Yazılı cevap kontrolü
        // ------------------------------------------------------------------

        private bool YazilanCevapDogruMu(KelimeKarti kart, string metin)
        {
            string yazilan = MetinYardimcisi.Normalize(metin);
            if (yazilan.Length == 0) return false;

            if (yazilan == kart.NormAnlam) return true;

            // Aynı kelimenin başka bir anlamı da doğru kabul edilir; alternatiflerden biri yeterlidir.
            return _kelimeAnlamlari[kart.KelimeAnahtari].Contains(yazilan);
        }
    }
}
