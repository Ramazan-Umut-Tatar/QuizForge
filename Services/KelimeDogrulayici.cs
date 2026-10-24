#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace QuizForge
{
    public enum UyariDuzeyi
    {
        Bilgi,
        Uyari,
        Hata
    }

    public sealed class DogrulamaUyarisi
    {
        /// <summary>Uyarıyla ilgili kartların listedeki sıra numaraları.</summary>
        public IReadOnlyList<int> Satirlar { get; }
        public string Mesaj { get; }
        public UyariDuzeyi Duzey { get; }

        public DogrulamaUyarisi(IReadOnlyList<int> satirlar, string mesaj, UyariDuzeyi duzey)
        {
            Satirlar = satirlar;
            Mesaj = mesaj;
            Duzey = duzey;
        }
    }

    /// <summary>Kelime kartlarını tarar; şıklarda iki doğru cevap çıkarabilecek durumları bulur.</summary>
    public static class KelimeDogrulayici
    {
        public static List<DogrulamaUyarisi> Analiz(IReadOnlyList<KelimeKarti> kartlar, bool ornekKontrolu = true)
        {
            var sonuc = new List<DogrulamaUyarisi>();
            var indeksli = kartlar.Select((kart, index) => new { Kart = kart, Index = index }).ToList();

            // 1) Birebir aynı kayıt
            foreach (var grup in indeksli.GroupBy(x => x.Kart.Anahtar).Where(g => g.Count() > 1))
            {
                var ilk = grup.First().Kart;
                sonuc.Add(new DogrulamaUyarisi(
                    grup.Select(x => x.Index).ToList(),
                    $"Birebir tekrar: \"{ilk.Kelime}\" = \"{ilk.Anlam}\"",
                    UyariDuzeyi.Hata));
            }

            // 2) Aynı kelime, farklı anlam
            foreach (var grup in indeksli
                         .GroupBy(x => x.Kart.KelimeAnahtari)
                         .Where(g => g.Select(x => x.Kart.Anahtar).Distinct().Count() > 1))
            {
                sonuc.Add(new DogrulamaUyarisi(
                    grup.Select(x => x.Index).ToList(),
                    $"\"{grup.First().Kart.Kelime}\" birden fazla anlamla tanımlı: {string.Join(" | ", grup.Select(x => x.Kart.Anlam).Distinct())}",
                    UyariDuzeyi.Uyari));
            }

            // 3) Aynı anlam, farklı kelime
            var ortakAnlamlar = indeksli
                .SelectMany(x => x.Kart.Anlamlar.Select(a => new { Anlam = a, x.Kart, x.Index }))
                .GroupBy(x => x.Anlam)
                .Where(g => g.Select(x => x.Kart.KelimeAnahtari).Distinct().Count() > 1);

            foreach (var grup in ortakAnlamlar)
            {
                sonuc.Add(new DogrulamaUyarisi(
                    grup.Select(x => x.Index).Distinct().ToList(),
                    $"Aynı anlam birden fazla kelimede: \"{grup.Key}\" → {string.Join(", ", grup.Select(x => x.Kart.Kelime).Distinct())}",
                    UyariDuzeyi.Uyari));
            }

            // 4) Örnek cümlede kelime bulunamıyor
            if (ornekKontrolu)
            {
                foreach (var x in indeksli.Where(x => x.Kart.Ornek.Length > 0 && x.Kart.Cloze == null))
                {
                    sonuc.Add(new DogrulamaUyarisi(
                        new[] { x.Index },
                        $"\"{x.Kart.Kelime}\": örnek cümlede kelime bulunamadı, boşluk doldurma bu kelime için kullanılmayacak.",
                        UyariDuzeyi.Bilgi));
                }
            }

            return sonuc;
        }
    }
}
