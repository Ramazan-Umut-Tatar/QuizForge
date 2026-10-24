#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace QuizForge
{
    public sealed class UygulamaAyarlari
    {
        // Görünüm
        public TemaTuru Tema { get; set; } = TemaTuru.Sakin;
        public int FontOlcegiYuzde { get; set; } = 100;

        // Çalışma
        public SoruModu Mod { get; set; } = SoruModu.Ustalik;
        public bool YanlisiTekrarSor { get; set; } = true;
        public bool HataNedeniSor { get; set; } = true;
        public bool YorgunlukUyarisi { get; set; } = true;
        public bool RozetGoster { get; set; } = true;
        public List<string> SonSecilenDosyalar { get; set; } = new List<string>();

        // Hatırlatıcı (uygulama açıkken / tepsideyken çalışır)
        public bool BildirimAcik { get; set; }
        public DateTime? SonBildirim { get; set; }

        // Yapay zekâ (isteğe bağlı, varsayılan kapalı)
        public bool AiAcik { get; set; }
        public string AiAnahtari { get; set; } = string.Empty;
        public bool AiAnahtariniSakla { get; set; }
        public string AiModel { get; set; } = "claude-haiku-5-5";

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
            if (!Enum.IsDefined(typeof(SoruModu), ayarlar.Mod)) ayarlar.Mod = SoruModu.Ustalik;
            if (!Enum.IsDefined(typeof(TemaTuru), ayarlar.Tema)) ayarlar.Tema = TemaTuru.Sakin;
            if (string.IsNullOrWhiteSpace(ayarlar.AiModel)) ayarlar.AiModel = "claude-haiku-5-5";
            return ayarlar;
        }

        /// <summary>API anahtarı yalnızca kullanıcı "sakla" dediyse diske yazılır.</summary>
        public static void Kaydet(UygulamaAyarlari ayarlar)
        {
            string anahtar = ayarlar.AiAnahtari;
            if (!ayarlar.AiAnahtariniSakla)
                ayarlar.AiAnahtari = string.Empty;

            try
            {
                JsonDosya.Yaz(Yol, ayarlar);
            }
            finally
            {
                ayarlar.AiAnahtari = anahtar;
            }
        }
    }
}
