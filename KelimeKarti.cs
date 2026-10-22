#nullable enable
using System.Collections.Generic;

namespace QuizForge
{
    /// <summary>Bir kelime, anlamı ve geldiği dosya (ünite).</summary>
    public sealed class KelimeKarti
    {
        public string Kelime { get; }
        public string Anlam { get; }
        public string Kaynak { get; }

        /// <summary>İstatistik anahtarı: normalize(kelime) | normalize(anlam)</summary>
        public string Anahtar { get; }

        /// <summary>Aynı kelimenin farklı anlamlarını gruplamak için.</summary>
        public string KelimeAnahtari { get; }

        public string NormAnlam { get; }

        /// <summary>"a/b" biçimindeki anlamın normalize edilmiş alternatifleri.</summary>
        public IReadOnlyList<string> Anlamlar { get; }

        public KelimeKarti(string kelime, string anlam, string kaynak = "")
        {
            Kelime = kelime;
            Anlam = anlam;
            Kaynak = kaynak;

            KelimeAnahtari = MetinYardimcisi.Normalize(kelime);
            NormAnlam = MetinYardimcisi.Normalize(anlam);
            Anahtar = KelimeAnahtari + "|" + NormAnlam;
            Anlamlar = MetinYardimcisi.AnlamlariAyir(anlam);
        }
    }
}
