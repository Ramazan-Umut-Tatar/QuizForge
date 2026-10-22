#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace QuizForge
{
    /// <summary>Quiz oturumunun istatistik kaynağına ihtiyaç duyduğu minimum sözleşme.</summary>
    public interface IIstatistikDeposu
    {
        /// <summary>Bu kartta daha önce yanlışlıkla seçilen şıklar (cevap metni → kaç kez).</summary>
        IReadOnlyDictionary<string, int> KarisanCevaplar(KelimeKarti kart);

        void SonucKaydet(KelimeKarti kart, bool ilkDenemedeDogru, IEnumerable<string> yanlisSecilenler);

        void Kaydet();
    }

    public sealed class KelimeIstatistigi
    {
        public string Kelime { get; set; } = string.Empty;
        public string Anlam { get; set; } = string.Empty;
        public int Dogru { get; set; }
        public int Yanlis { get; set; }
        public DateTime SonGorulme { get; set; }
        public Dictionary<string, int> Karistirilanlar { get; set; } = new Dictionary<string, int>();

        [JsonIgnore] public int Toplam => Dogru + Yanlis;
        [JsonIgnore] public double BasariOrani => Toplam == 0 ? 0 : (double)Dogru / Toplam;

        /// <summary>Az denemeli kelimelerin tesadüfen en üste çıkmaması için yumuşatılmış hata oranı.</summary>
        [JsonIgnore] public double ZorlukPuani => (Yanlis + 1.0) / (Toplam + 2.0);
    }

    public sealed class IstatistikVerisi
    {
        public Dictionary<string, KelimeIstatistigi> Kelimeler { get; set; } = new Dictionary<string, KelimeIstatistigi>();
    }

    public sealed class IstatistikDeposu : IIstatistikDeposu
    {
        private static readonly IReadOnlyDictionary<string, int> Bos = new Dictionary<string, int>();

        private readonly string _yol;
        private readonly IstatistikVerisi _veri;
        private bool _degisti;

        public IstatistikDeposu(string? yol = null)
        {
            _yol = yol ?? Path.Combine(JsonDosya.VeriKlasoru, "istatistik.json");
            _veri = JsonDosya.Oku<IstatistikVerisi>(_yol);
        }

        public IReadOnlyDictionary<string, int> KarisanCevaplar(KelimeKarti kart)
        {
            if (_veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s))
                return s.Karistirilanlar;

            return Bos;
        }

        public void SonucKaydet(KelimeKarti kart, bool ilkDenemedeDogru, IEnumerable<string> yanlisSecilenler)
        {
            if (!_veri.Kelimeler.TryGetValue(kart.Anahtar, out KelimeIstatistigi? s))
            {
                s = new KelimeIstatistigi { Kelime = kart.Kelime, Anlam = kart.Anlam };
                _veri.Kelimeler[kart.Anahtar] = s;
            }

            if (ilkDenemedeDogru) s.Dogru++;
            else s.Yanlis++;

            s.SonGorulme = DateTime.Now;

            foreach (string yanlis in yanlisSecilenler)
            {
                s.Karistirilanlar.TryGetValue(yanlis, out int sayi);
                s.Karistirilanlar[yanlis] = sayi + 1;
            }

            _degisti = true;
        }

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

        public void Sifirla()
        {
            _veri.Kelimeler.Clear();
            _degisti = true;
            Kaydet();
        }

        public void Kaydet()
        {
            if (!_degisti) return;
            JsonDosya.Yaz(_yol, _veri);
            _degisti = false;
        }
    }
}
