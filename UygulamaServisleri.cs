#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>Formların paylaştığı servisleri tek nesnede toplar (Form1'de bir kez oluşturulur).</summary>
    public sealed class UygulamaServisleri
    {
        public KelimeDeposu Kelimeler { get; }
        public IstatistikDeposu Istatistik { get; }
        public ProfilDeposu Profil { get; }
        public OturumDeposu Oturumlar { get; }
        public UygulamaAyarlari Ayarlar { get; }
        public ClaudeAiServisi Ai { get; }

        public UygulamaServisleri(string kelimeKlasoru)
        {
            Kelimeler = new KelimeDeposu(kelimeKlasoru);
            Istatistik = new IstatistikDeposu();
            Profil = new ProfilDeposu();
            Oturumlar = new OturumDeposu();
            Ayarlar = AyarDeposu.Yukle();
            Ai = new ClaudeAiServisi(Ayarlar);
        }

        public int UstaSayisi => Istatistik.SeviyeSayisi(Seviye.Usta);

        public void HepsiniKaydet()
        {
            AyarDeposu.Kaydet(Ayarlar);
            Istatistik.Kaydet();
            Profil.Kaydet();
        }
    }

    /// <summary>Tek satırlık metin soran küçük modal pencere.</summary>
    public static class YardimciFormlar
    {
        public static string? MetinSor(IWin32Window sahip, UygulamaAyarlari ayarlar, string baslik, string soru, string varsayilan = "")
        {
            using (var form = new Form())
            {
                form.Text = baslik;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(420, 130);

                var etiket = new Label { Text = soru, AutoSize = true, Left = 12, Top = 12 };
                var kutu = new TextBox { Left = 12, Top = 40, Width = 396, Text = varsayilan };
                var tamam = new Button { Text = "Tamam", Left = 232, Top = 84, Width = 84, DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Left = 324, Top = 84, Width = 84, DialogResult = DialogResult.Cancel };

                form.Controls.Add(etiket);
                form.Controls.Add(kutu);
                form.Controls.Add(tamam);
                form.Controls.Add(iptal);
                form.AcceptButton = tamam;
                form.CancelButton = iptal;

                new TemaUygulayici(ayarlar).Uygula(form);

                return form.ShowDialog(sahip) == DialogResult.OK ? kutu.Text.Trim() : null;
            }
        }
    }
}
