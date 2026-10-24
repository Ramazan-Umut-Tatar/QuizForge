#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QuizForge
{
    public enum SoruModu
    {
        Sikli = 0,
        Yazili = 1,
        /// <summary>Her kelimeye seviyesine uygun soru tipi sorulur (şıklı → yazılı → cümle içinde).</summary>
        Ustalik = 2
    }

    public enum CevapSonucu
    {
        Yanlis,
        Dogru,
        /// <summary>"Bilmiyorum" denildi veya yazılı moddaki hak bitti; soru yanlış sayılarak geçildi.</summary>
        Gecildi
    }

    public sealed class QuizAyarlari
    {
        public SoruModu Mod { get; set; } = SoruModu.Ustalik;
        public bool YanlisiTekrarSor { get; set; } = true;
        public int TekrarAraligi { get; set; } = 4;          // yanlıştan kaç soru sonra tekrar sorulsun
        public int MaksimumYazmaDenemesi { get; set; } = 3;   // yazılı/cümle sorularında yanlış hakkı
        public int SoruPuani { get; set; } = 10;
    }

    /// <summary>Ekranda gösterilecek tek bir soru.</summary>
    public sealed class QuizSorusu
    {
        public KelimeKarti Kart { get; }
        public SoruTipi Tip { get; }
        public IReadOnlyList<string> Secenekler { get; }
        public int DogruIndex { get; }
        public bool Tekrar { get; }
        public Seviye Seviye { get; }
        public int Seri { get; }
        public int SeriHedefi { get; }

        /// <summary>Cümle tipinde boşluklu cümle; diğer tiplerde null.</summary>
        public BoslukluCumle? Cumle => Tip == SoruTipi.Cumle ? Kart.Cloze : null;

        public QuizSorusu(
            KelimeKarti kart, SoruTipi tip, IReadOnlyList<string> secenekler, int dogruIndex,
            bool tekrar, Seviye seviye, int seri, int seriHedefi)
        {
            Kart = kart;
            Tip = tip;
            Secenekler = secenekler;
            DogruIndex = dogruIndex;
            Tekrar = tekrar;
            Seviye = seviye;
            Seri = seri;
            SeriHedefi = seriHedefi;
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

    /// <summary>Az önce çözülen sorunun sonucu (arayüzün geri bildirim vermesi için).</summary>
    public sealed class CozumBilgisi
    {
        public KelimeKarti Kart { get; }
        public SoruTipi Tip { get; }
        public bool IlkDenemedeDogru { get; }
        public bool Gecildi { get; }
        public bool Akici { get; }
        public long SureMs { get; }
        public IReadOnlyList<string> VerilenYanlislar { get; }
        public SeviyeDegisimi? Degisim { get; }

        public CozumBilgisi(
            KelimeKarti kart, SoruTipi tip, bool ilkDenemedeDogru, bool gecildi, bool akici,
            long sureMs, IReadOnlyList<string> verilenYanlislar, SeviyeDegisimi? degisim)
        {
            Kart = kart;
            Tip = tip;
            IlkDenemedeDogru = ilkDenemedeDogru;
            Gecildi = gecildi;
            Akici = akici;
            SureMs = sureMs;
            VerilenYanlislar = verilenYanlislar;
            Degisim = degisim;
        }

        /// <summary>Kullanıcıya gösterilecek doğru cevap (cümle tipinde kelime, diğerlerinde anlam).</summary>
        public string DogruCevap => Tip == SoruTipi.Cumle ? Kart.Kelime : Kart.Anlam;
    }

    public sealed class TerfiKaydi
    {
        public KelimeKarti Kart { get; }
        public SeviyeDegisimi Degisim { get; }

        public TerfiKaydi(KelimeKarti kart, SeviyeDegisimi degisim)
        {
            Kart = kart;
            Degisim = degisim;
        }
    }

    public sealed class YavasKayit
    {
        public KelimeKarti Kart { get; }
        public long SureMs { get; }

        public YavasKayit(KelimeKarti kart, long sureMs)
        {
            Kart = kart;
            SureMs = sureMs;
        }
    }

    /// <summary>Bir quiz oturumunun tüm kurallarını yönetir. Windows Forms bağımlılığı yoktur.</summary>
    public sealed class QuizOturumu
    {
        public const int SecenekSayisi = 4;
        private const int KartBasinaMaksTekrar = 2;
        private const int UyariAraligi = 12;

        private readonly QuizAyarlari _ayar;
        private readonly IIstatistikDeposu? _istatistik;
        private readonly Random _rnd = Random.Shared;
        private readonly DateTime _baslangic = DateTime.Now;

        private readonly List<KelimeKarti> _tumKartlar;                         // şık havuzu
        private readonly List<KelimeKarti> _anlamHavuzu;                        // şık adayları: anlamı tekilleştirilmiş (bir kez hesaplanır)
        private readonly List<KelimeKarti> _ana;                                // henüz ilk kez çözülmemiş kartlar
        private readonly List<(KelimeKarti Kart, int Zaman)> _tekrar = new List<(KelimeKarti Kart, int Zaman)>();
        private readonly Dictionary<string, int> _tekrarSayisi = new Dictionary<string, int>();
        private readonly Dictionary<string, HashSet<string>> _kelimeAnlamlari;  // kelime → kabul edilen tüm anlamlar
        private readonly Dictionary<string, YanlisKaydi> _yanlislar = new Dictionary<string, YanlisKaydi>();

        private readonly List<string> _karisanSecenekler = new List<string>();  // bu sorudaki yanlış şıklar
        private readonly List<string> _buSoruYanlislari = new List<string>();   // bu sorudaki tüm yanlış cevaplar

        private readonly List<bool> _dogrular = new List<bool>();
        private readonly List<int> _sureler = new List<int>();
        private readonly List<YavasKayit> _yavaslar = new List<YavasKayit>();
        private readonly List<TerfiKaydi> _terfiler = new List<TerfiKaydi>();

        private readonly Stopwatch _sure = new Stopwatch();
        private long _ilkSureMs = -1;
        private int _soruSayaci;
        private int _yanlisDeneme;
        private int _sonUyariSayisi;

        public int ToplamSoru { get; }
        public int Cevaplanan { get; private set; }
        public int Puan { get; private set; }
        public int YaziliDogru { get; private set; }
        public int HizliDogru { get; private set; }
        public int HataNedeniSayisi { get; private set; }
        public QuizSorusu? MevcutSoru { get; private set; }
        public CozumBilgisi? SonCozum { get; private set; }

        public bool Bitti => MevcutSoru is null;
        public int KalanDeneme => Math.Max(0, _ayar.MaksimumYazmaDenemesi - _yanlisDeneme);
        public int CozulenSoru => _dogrular.Count;
        public int IlkDenemeDogruSayisi => _dogrular.Count(d => d);
        public IReadOnlyList<KelimeKarti> HavuzKartlari => _tumKartlar;
        public IReadOnlyList<YavasKayit> YavasKayitlari => _yavaslar;
        public IReadOnlyList<TerfiKaydi> Terfiler => _terfiler;

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
            _anlamHavuzu = _tumKartlar.GroupBy(k => k.Anlam).Select(g => g.First()).ToList();

            _kelimeAnlamlari = _tumKartlar
                .GroupBy(k => k.KelimeAnahtari)
                .ToDictionary(g => g.Key, g => new HashSet<string>(g.SelectMany(k => k.Anlamlar)));

            SonrakiSoruyuHazirla();
        }

        // ------------------------------------------------------------------
        //  Süre, yorgunluk ve oturum kaydı
        // ------------------------------------------------------------------

        /// <summary>Soru ekranda göründüğünde çağrılır; cevap süresi bu andan ölçülür.</summary>
        public void SureyiBaslat()
        {
            _ilkSureMs = -1;
            _sure.Restart();
        }

        public YorgunlukUyarisi? YorgunlukKontrolu()
        {
            if (_sonUyariSayisi > 0 && _dogrular.Count - _sonUyariSayisi < UyariAraligi)
                return null;

            return YorgunlukAnalizi.Kontrol(_dogrular, _sureler);
        }

        public void UyariGosterildi() => _sonUyariSayisi = Math.Max(1, _dogrular.Count);

        public OturumKaydi KaydiOlustur() => new OturumKaydi
        {
            Baslangic = _baslangic,
            SureDakika = (DateTime.Now - _baslangic).TotalMinutes,
            Dogrular = _dogrular.ToList(),
            Sureler = _sureler.ToList()
        };

        public void HataNedeniKaydet(HataNedeni neden)
        {
            if (SonCozum is null) return;

            _istatistik?.HataNedeniKaydet(SonCozum.Kart, neden);
            HataNedeniSayisi++;
        }

        // ------------------------------------------------------------------
        //  Cevap verme
        // ------------------------------------------------------------------

        /// <summary>Şıklı soru: verilen şıkkı değerlendirir.</summary>
        public CevapSonucu Cevapla(int secenekIndex)
        {
            QuizSorusu soru = MevcutSoruyuAl();

            if (secenekIndex == soru.DogruIndex)
                return DogruCevap();

            string secilen = soru.Secenekler[secenekIndex];
            _karisanSecenekler.Add(secilen);
            return YanlisCevap(soru.Kart, secilen);
        }

        /// <summary>Yazılı veya cümle içinde soru: yazılan metni değerlendirir.</summary>
        public CevapSonucu CevapYaz(string metin)
        {
            QuizSorusu soru = MevcutSoruyuAl();

            bool dogru = soru.Tip == SoruTipi.Cumle
                ? CumleCevabiDogruMu(soru, metin)
                : YazilanCevapDogruMu(soru.Kart, metin);

            if (dogru)
                return DogruCevap();

            YanlisCevap(soru.Kart, metin.Trim());

            if (_yanlisDeneme >= _ayar.MaksimumYazmaDenemesi)
                return Gec(soru);

            return CevapSonucu.Yanlis;
        }

        /// <summary>Doğru cevabı gösterip soruyu yanlış sayarak geçer.</summary>
        public CevapSonucu Bilmiyorum()
        {
            QuizSorusu soru = MevcutSoruyuAl();
            return Gec(soru);
        }

        // ------------------------------------------------------------------
        //  Dahili akış
        // ------------------------------------------------------------------

        private QuizSorusu MevcutSoruyuAl()
        {
            QuizSorusu soru = MevcutSoru ?? throw new InvalidOperationException("Cevaplanacak soru kalmadı.");

            // Süre yalnızca ilk cevap denemesine kadar ölçülür.
            if (_ilkSureMs < 0)
                _ilkSureMs = Math.Min(_sure.ElapsedMilliseconds, Akicilik.AsiriSureMs);

            return soru;
        }

        private CevapSonucu DogruCevap()
        {
            Tamamla(false);
            return CevapSonucu.Dogru;
        }

        private CevapSonucu YanlisCevap(KelimeKarti kart, string verilen)
        {
            _yanlisDeneme++;

            YanlisKaydi kayit = KayitAl(kart);
            kayit.YanlisDeneme++;

            if (!string.IsNullOrWhiteSpace(verilen))
            {
                kayit.VerilenCevaplar.Add(verilen);
                _buSoruYanlislari.Add(verilen);
            }

            return CevapSonucu.Yanlis;
        }

        private CevapSonucu Gec(QuizSorusu soru)
        {
            if (_yanlisDeneme == 0)
            {
                _yanlisDeneme = 1;
                KayitAl(soru.Kart).YanlisDeneme++;
            }

            Tamamla(true);
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

        /// <summary>Soru çözüldü: puan, istatistik, ustalık merdiveni, tekrar kuyruğu, sıradaki soru.</summary>
        private void Tamamla(bool gecildi)
        {
            QuizSorusu soru = MevcutSoru!;
            KelimeKarti kart = soru.Kart;

            bool ilkDenemedeDogru = _yanlisDeneme == 0;
            long sure = _ilkSureMs < 0 ? 0 : _ilkSureMs;
            bool akici = ilkDenemedeDogru && Akicilik.AkiciMi(soru.Tip, kart, sure);

            SeviyeDegisimi? degisim = null;
            if (_istatistik != null)
            {
                degisim = _istatistik.SonucKaydet(kart, new SonucBilgisi
                {
                    IlkDenemedeDogru = ilkDenemedeDogru,
                    Tip = soru.Tip,
                    SureMs = sure,
                    Akici = akici,
                    CumleVar = kart.Cloze != null,
                    YanlisSecilenler = soru.Tip == SoruTipi.Sikli ? _karisanSecenekler.ToList() : new List<string>()
                });

                if (degisim.Terfi)
                    _terfiler.Add(new TerfiKaydi(kart, degisim));
            }

            _dogrular.Add(ilkDenemedeDogru);
            _sureler.Add((int)Math.Min(sure, int.MaxValue));

            if (ilkDenemedeDogru && soru.Tip != SoruTipi.Sikli) YaziliDogru++;
            if (akici) HizliDogru++;
            if (ilkDenemedeDogru && !akici && sure > 0 && _yavaslar.All(y => !ReferenceEquals(y.Kart, kart)))
                _yavaslar.Add(new YavasKayit(kart, sure));

            SonCozum = new CozumBilgisi(kart, soru.Tip, ilkDenemedeDogru, gecildi, akici, sure,
                                        _buSoruYanlislari.ToList(), degisim);

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
            _buSoruYanlislari.Clear();

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

            SoruTipi tip = TipBelirle(kart);
            var secenekler = new List<string>();
            int dogruIndex = -1;

            if (tip == SoruTipi.Sikli)
            {
                secenekler = YanlisSecenekleriSec(kart);

                if (secenekler.Count == 0)
                {
                    tip = SoruTipi.Yazili;   // yanlış şık üretilemiyorsa yazılıya düş
                }
                else
                {
                    dogruIndex = _rnd.Next(secenekler.Count + 1);
                    secenekler.Insert(dogruIndex, kart.Anlam);
                }
            }

            Seviye seviye = _istatistik?.SeviyeAl(kart) ?? Seviye.Yeni;
            int seri = _istatistik?.SeriAl(kart) ?? 0;

            MevcutSoru = new QuizSorusu(kart, tip, secenekler, dogruIndex, tekrar,
                                        seviye, seri, UstalikMerdiveni.GerekliSeri(seviye));
            SureyiBaslat();
        }

        private SoruTipi TipBelirle(KelimeKarti kart)
        {
            switch (_ayar.Mod)
            {
                case SoruModu.Sikli:
                    return SoruTipi.Sikli;
                case SoruModu.Yazili:
                    return SoruTipi.Yazili;
                default:
                    Seviye seviye = _istatistik?.SeviyeAl(kart) ?? Seviye.Yeni;
                    return UstalikMerdiveni.TipSec(seviye, kart.Cloze != null);
            }
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

            return _anlamHavuzu
                .Where(k => k.Anlam != dogru.Anlam && !yasak.Overlaps(k.Anlamlar))
                .Select(k => new { k.Anlam, Skor = BenzerlikPuani(dogru, k, karisanlar) })
                .OrderByDescending(x => x.Skor)
                .Take(SecenekSayisi - 1)
                .Select(x => x.Anlam)
                .ToList();
        }

        /// <summary>
        /// Yüksek puan = daha "inandırıcı" yanlış şık: benzer uzunluk, benzer alternatif sayısı,
        /// aynı son ek ve geçmişte karıştırılmış olması. Küçük rastgele katkı tekrarı önler.
        /// </summary>
        private double BenzerlikPuani(KelimeKarti dogru, KelimeKarti aday, IReadOnlyDictionary<string, int>? karisanlar)
        {
            double skor = -Math.Abs(dogru.Anlam.Length - aday.Anlam.Length) * 0.5;
            skor -= Math.Abs(dogru.Anlamlar.Count - aday.Anlamlar.Count);

            if (dogru.SonEk == aday.SonEk)
                skor += 3;

            if (karisanlar != null && karisanlar.TryGetValue(aday.Anlam, out int kez))
                skor += 50 + Math.Min(kez, 5);

            return skor + _rnd.NextDouble() * 3;
        }

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

        private static bool CumleCevabiDogruMu(QuizSorusu soru, string metin)
        {
            string yazilan = MetinYardimcisi.Normalize(metin);
            if (yazilan.Length == 0) return false;

            if (yazilan == soru.Kart.KelimeAnahtari) return true;

            BoslukluCumle? cumle = soru.Cumle;
            return cumle != null && yazilan == MetinYardimcisi.Normalize(cumle.YuzeyBicim);
        }
    }
}
