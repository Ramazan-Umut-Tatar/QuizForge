#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace QuizForge
{
    /// <summary>Bir kelime, anlamı, geldiği dosya, isteğe bağlı örnek cümle ve etiketler.</summary>
    public sealed class KelimeKarti
    {
        public string Kelime { get; }
        public string Anlam { get; }
        public string Kaynak { get; }
        public string Ornek { get; }
        public IReadOnlyList<string> Etiketler { get; }

        /// <summary>İstatistik anahtarı: normalize(kelime) | normalize(anlam)</summary>
        public string Anahtar { get; }

        /// <summary>Aynı kelimenin farklı anlamlarını gruplamak için.</summary>
        public string KelimeAnahtari { get; }

        public string NormAnlam { get; }

        /// <summary>Normalize anlamın son 3 harfi (-mak/-mek gibi ekleri eşleştirmek için, bir kez hesaplanır).</summary>
        public string SonEk { get; }

        /// <summary>"a/b" biçimindeki anlamın normalize edilmiş alternatifleri.</summary>
        public IReadOnlyList<string> Anlamlar { get; }

        /// <summary>Örnek cümlede kelime bulunabildiyse boşluklu cümle; yoksa null.</summary>
        public BoslukluCumle? Cloze { get; }

        public KelimeKarti(
            string kelime,
            string anlam,
            string kaynak = "",
            string ornek = "",
            IEnumerable<string>? etiketler = null)
        {
            Kelime = kelime;
            Anlam = anlam;
            Kaynak = kaynak;
            Ornek = ornek ?? string.Empty;
            Etiketler = etiketler?.ToList() ?? new List<string>();

            KelimeAnahtari = MetinYardimcisi.Normalize(kelime);
            NormAnlam = MetinYardimcisi.Normalize(anlam);
            SonEk = NormAnlam.Length >= 3 ? NormAnlam.Substring(NormAnlam.Length - 3) : NormAnlam;
            Anahtar = KelimeAnahtari + "|" + NormAnlam;
            Anlamlar = MetinYardimcisi.AnlamlariAyir(anlam);
            Cloze = MetinYardimcisi.BoslukOlustur(Ornek, kelime);
        }
    }
}
