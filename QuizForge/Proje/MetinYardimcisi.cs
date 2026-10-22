#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuizForge
{
    /// <summary>
    /// Karşılaştırma için metinleri tek biçime getirir (Türkçe küçük harf, boşluk, noktalama)
    /// ve "a/b" biçimindeki çok anlamlı cevapları ayırır.
    /// </summary>
    public static class MetinYardimcisi
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private static readonly char[] AnlamAyiricilari = { '/', ';' };
        private static readonly char[] KenarNoktalama = { '.', ',', '!', '?', ':', ';' };
        private static readonly Regex Bosluklar = new Regex(@"\s+", RegexOptions.Compiled);

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
    }
}
