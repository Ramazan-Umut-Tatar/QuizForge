#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace QuizForge
{
    public sealed class UygulamaAyarlari
    {
        public bool KaranlikTema { get; set; }
        public int FontOlcegiYuzde { get; set; } = 100;
        public SoruModu Mod { get; set; } = SoruModu.Sikli;
        public bool YanlisiTekrarSor { get; set; } = true;
        public List<string> SonSecilenDosyalar { get; set; } = new List<string>();

        public QuizAyarlari QuizAyarlariOlustur() => new QuizAyarlari
        {
            Mod = Mod,
            YanlisiTekrarSor = YanlisiTekrarSor
        };
    }

    public static class AyarDeposu
    {
        private static string Yol => Path.Combine(JsonDosya.VeriKlasoru, "ayarlar.json");

        public static UygulamaAyarlari Yukle()
        {
            UygulamaAyarlari ayarlar = JsonDosya.Oku<UygulamaAyarlari>(Yol);
            ayarlar.FontOlcegiYuzde = Math.Clamp(ayarlar.FontOlcegiYuzde, 80, 200);
            if (!Enum.IsDefined(typeof(SoruModu), ayarlar.Mod)) ayarlar.Mod = SoruModu.Sikli;
            return ayarlar;
        }

        public static void Kaydet(UygulamaAyarlari ayarlar) => JsonDosya.Yaz(Yol, ayarlar);
    }
}
