#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QuizForge
{
    public sealed class ProfilVerisi
    {
        public int Xp { get; set; }
        public Dictionary<string, DateTime> Rozetler { get; set; } = new Dictionary<string, DateTime>();
        public List<string> CalismaGunleri { get; set; } = new List<string>();   // "yyyy-MM-dd"
        public Dictionary<string, int> Sayaclar { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>Rozet koşullarının değerlendirildiği anlık durum.</summary>
    public sealed class RozetBaglami
    {
        public ProfilVerisi Profil { get; }
        public int UstaSayisi { get; }
        public int ArdisikGun { get; }

        public RozetBaglami(ProfilVerisi profil, int ustaSayisi, int ardisikGun)
        {
            Profil = profil;
            UstaSayisi = ustaSayisi;
            ArdisikGun = ardisikGun;
        }

        public int Say(string ad) => Profil.Sayaclar.TryGetValue(ad, out int n) ? n : 0;
    }

    public sealed class RozetTanimi
    {
        public string Id { get; }
        public string Ad { get; }
        public string Aciklama { get; }
        public Func<RozetBaglami, bool> Kosul { get; }

        public RozetTanimi(string id, string ad, string aciklama, Func<RozetBaglami, bool> kosul)
        {
            Id = id;
            Ad = ad;
            Aciklama = aciklama;
            Kosul = kosul;
        }
    }

    public sealed class OdulOzeti
    {
        public int KazanilanXp { get; }
        public IReadOnlyList<RozetTanimi> YeniRozetler { get; }

        public OdulOzeti(int kazanilanXp, IReadOnlyList<RozetTanimi> yeniRozetler)
        {
            KazanilanXp = kazanilanXp;
            YeniRozetler = yeniRozetler;
        }
    }

    /// <summary>
    /// Rozetler bilerek hacmi değil öğrenme davranışlarını ödüllendirir
    /// (yansıtma, hata analizi, mola, ustalık); sıralama veya rekabet yoktur.
    /// </summary>
    public static class Rozetler
    {
        public static readonly IReadOnlyList<RozetTanimi> Hepsi = new List<RozetTanimi>
        {
            new RozetTanimi("ilk-oturum", "İlk Adım", "İlk oturumunu tamamla.",
                b => b.Say("oturum") >= 1),
            new RozetTanimi("seri-3", "Üç Gün", "3 gün üst üste çalış.",
                b => b.ArdisikGun >= 3),
            new RozetTanimi("seri-7", "Bir Hafta", "7 gün üst üste çalış.",
                b => b.ArdisikGun >= 7),
            new RozetTanimi("ilk-usta", "İlk Usta Kelime", "Bir kelimeyi Usta seviyesine çıkar.",
                b => b.UstaSayisi >= 1),
            new RozetTanimi("usta-10", "On Usta", "10 kelimeyi Usta seviyesine çıkar.",
                b => b.UstaSayisi >= 10),
            new RozetTanimi("hatirlayan", "Hatırlayan", "Yazarak veya cümle içinde ilk denemede 25 doğru cevap ver.",
                b => b.Say("yazili_dogru") >= 25),
            new RozetTanimi("dusunen", "Düşünen", "10 hatanın nedenini işaretle.",
                b => b.Say("hata_nedeni") >= 10),
            new RozetTanimi("cift-avcisi", "Çift Avcısı", "Karıştırdığın 5 kelime çiftini ayırt etmeyi öğren.",
                b => b.Say("cift_cozuldu") >= 5),
            new RozetTanimi("akici", "Akıcı", "50 cevabı hızlı ve doğru ver.",
                b => b.Say("hizli_dogru") >= 50),
            new RozetTanimi("kendini-taniyan", "Kendini Tanıyan", "Yorgunluk uyarısını dinleyip mola ver.",
                b => b.Say("mola") >= 1)
        };
    }

    public sealed class ProfilDeposu
    {
        private readonly string _yol;
        private readonly ProfilVerisi _veri;

        public ProfilDeposu(string? yol = null)
        {
            _yol = yol ?? Path.Combine(JsonDosya.VeriKlasoru, "profil.json");
            _veri = JsonDosya.Oku<ProfilVerisi>(_yol);
        }

        public int Xp => _veri.Xp;
        public IReadOnlyDictionary<string, DateTime> KazanilanRozetler => _veri.Rozetler;

        public void XpEkle(int xp)
        {
            if (xp > 0) _veri.Xp += xp;
        }

        public void Say(string ad, int adet = 1)
        {
            if (adet <= 0) return;
            _veri.Sayaclar.TryGetValue(ad, out int simdiki);
            _veri.Sayaclar[ad] = simdiki + adet;
        }

        public void BugunuIsaretle()
        {
            string bugun = GunAnahtari(DateTime.Today);
            if (_veri.CalismaGunleri.Contains(bugun)) return;

            _veri.CalismaGunleri.Add(bugun);
            if (_veri.CalismaGunleri.Count > 400)
                _veri.CalismaGunleri.RemoveRange(0, _veri.CalismaGunleri.Count - 400);
        }

        /// <summary>Bugün veya dün biten, kesintisiz çalışma günü sayısı.</summary>
        public int ArdisikGun()
        {
            var gunler = new HashSet<string>(_veri.CalismaGunleri);
            DateTime gun = DateTime.Today;

            if (!gunler.Contains(GunAnahtari(gun)))
                gun = gun.AddDays(-1);

            int sayi = 0;
            while (gunler.Contains(GunAnahtari(gun)))
            {
                sayi++;
                gun = gun.AddDays(-1);
            }

            return sayi;
        }

        /// <summary>Bitmiş bir oturumun XP'sini ve rozetlerini işler.</summary>
        public OdulOzeti OturumuIsle(QuizOturumu oturum, int ustaSayisi)
        {
            if (oturum.CozulenSoru == 0)
                return new OdulOzeti(0, new List<RozetTanimi>());

            BugunuIsaretle();
            Say("oturum");
            Say("yazili_dogru", oturum.YaziliDogru);
            Say("hizli_dogru", oturum.HizliDogru);
            Say("hata_nedeni", oturum.HataNedeniSayisi);

            // Çaba (+2), ilk denemede doğru (+5), seviye atlama (+20), hata nedeni yansıtma (+3), oturum bonusu (+10)
            int xp = oturum.CozulenSoru * 2
                     + oturum.IlkDenemeDogruSayisi * 5
                     + oturum.Terfiler.Count * 20
                     + oturum.HataNedeniSayisi * 3;
            if (oturum.CozulenSoru >= 5) xp += 10;

            XpEkle(xp);
            IReadOnlyList<RozetTanimi> yeni = RozetleriDegerlendir(ustaSayisi);
            Kaydet();

            return new OdulOzeti(xp, yeni);
        }

        public IReadOnlyList<RozetTanimi> RozetleriDegerlendir(int ustaSayisi)
        {
            var baglam = new RozetBaglami(_veri, ustaSayisi, ArdisikGun());
            var yeni = new List<RozetTanimi>();

            foreach (RozetTanimi rozet in Rozetler.Hepsi)
            {
                if (_veri.Rozetler.ContainsKey(rozet.Id)) continue;
                if (!rozet.Kosul(baglam)) continue;

                _veri.Rozetler[rozet.Id] = DateTime.Now;
                yeni.Add(rozet);
            }

            return yeni;
        }

        public void Kaydet() => JsonDosya.Yaz(_yol, _veri);

        private static string GunAnahtari(DateTime gun) =>
            gun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
