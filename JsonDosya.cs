#nullable enable
using System;
using System.IO;
using System.Text.Json;

namespace QuizForge
{
    /// <summary>Ayar ve istatistik dosyaları için küçük JSON okuma/yazma yardımcısı.</summary>
    internal static class JsonDosya
    {
        private static readonly JsonSerializerOptions Secenekler = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>Yazma izni her zaman olan kullanıcı klasörü: %AppData%\KelimeEzber</summary>
        public static string VeriKlasoru =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KelimeEzber");

        public static T Oku<T>(string yol) where T : new()
        {
            try
            {
                if (File.Exists(yol))
                    return JsonSerializer.Deserialize<T>(File.ReadAllText(yol)) ?? new T();
            }
            catch (JsonException)
            {
                // Bozuk dosyayı silmeden yedekle, temiz başla.
                try { File.Move(yol, yol + ".bozuk", overwrite: true); } catch (IOException) { }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }

            return new T();
        }

        public static void Yaz<T>(string yol, T veri)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(yol)!);

                // Önce geçici dosyaya yaz: yazma yarıda kesilirse eski veri bozulmaz.
                string gecici = yol + ".tmp";
                File.WriteAllText(gecici, JsonSerializer.Serialize(veri, Secenekler));
                File.Move(gecici, yol, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Kaydedilemezse uygulama çalışmaya devam etsin.
            }
        }
    }
}
