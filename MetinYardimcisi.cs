#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuizForge
{
    /// <summary>Örnek cümlede kelimenin yerine boşluk konmuş hâl ve cümlede geçen gerçek biçim.</summary>
    public sealed class BoslukluCumle
    {
        public string Metin { get; }
        public string YuzeyBicim { get; }

        public BoslukluCumle(string metin, string yuzeyBicim)
        {
            Metin = metin;
            YuzeyBicim = yuzeyBicim;
        }
    }

    public static class MetinYardimcisi
    {
        public const string BoslukIsareti = "_____";

        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private static readonly char[] AnlamAyiricilari = { '/', ';' };
        private static readonly char[] KenarNoktalama = { '.', ',', '!', '?', ':', ';' };
        private static readonly Regex Bosluklar = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex KelimeDeseni = new Regex(@"\p{L}+(?:['’\-]\p{L}+)*", RegexOptions.Compiled);
        private static readonly Regex CumleAyirici = new Regex(@"(?<=[.!?…])\s+|\r?\n+", RegexOptions.Compiled);

        public static string Normalize(string? metin)
        {
            if (string.IsNullOrWhiteSpace(metin)) return string.Empty;
            string temiz = metin.Trim().ToLower(Tr).Trim(KenarNoktalama).Trim();
            return Bosluklar.Replace(temiz, " ");
        }

        /// <summary>"Zorlamak/mecbur bırakmak" → ["zorlamak", "mecbur bırakmak"]</summary>
        public static IReadOnlyList<string> AnlamlariAyir(string? anlam)
        {
            if (anlam is null) return Array.Empty<string>();

            List<string> liste = anlam.Split(AnlamAyiricilari, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(Normalize)
                                      .Where(s => s.Length > 0)
                                      .Distinct()
                                      .ToList();

            if (liste.Count == 0)
            {
                string tam = Normalize(anlam);
                if (tam.Length > 0) liste.Add(tam);
            }

            return liste;
        }

        /// <summary>
        /// Örnek cümlede kelimeyi bulup boşlukla değiştirir. Önce tam kelime, bulunamazsa
        /// 1-4 harflik ek (compel → compelled) denenir. Bulunamazsa null döner.
        /// </summary>
        public static BoslukluCumle? BoslukOlustur(string? ornek, string? kelime)
        {
            if (string.IsNullOrWhiteSpace(ornek) || string.IsNullOrWhiteSpace(kelime)) return null;

            const string Onde = @"(?<![\p{L}\p{N}])";
            const string Sonra = @"(?![\p{L}\p{N}])";
            const RegexOptions Secenek = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

            string kacis = Regex.Escape(kelime.Trim());

            string desen = Onde + kacis + Sonra;
            Match eslesme = Regex.Match(ornek, desen, Secenek);

            if (!eslesme.Success)
            {
                desen = Onde + kacis + @"\p{L}{1,4}" + Sonra;
                eslesme = Regex.Match(ornek, desen, Secenek);
            }

            if (!eslesme.Success) return null;

            return new BoslukluCumle(Regex.Replace(ornek, desen, BoslukIsareti, Secenek), eslesme.Value);
        }

        public static IEnumerable<string> KelimeleriAyir(string? metin)
        {
            if (string.IsNullOrEmpty(metin)) return Enumerable.Empty<string>();
            return KelimeDeseni.Matches(metin).Select(m => m.Value);
        }

        public static List<string> CumleleriAyir(string? metin)
        {
            if (string.IsNullOrWhiteSpace(metin)) return new List<string>();
            return CumleAyirici.Split(metin)
                               .Select(c => c.Trim())
                               .Where(c => c.Length > 0)
                               .ToList();
        }

        public static List<string> EtiketleriAyir(string? metin)
        {
            if (string.IsNullOrWhiteSpace(metin)) return new List<string>();
            return metin.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => e.Length > 0)
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .ToList();
        }
    }
}
