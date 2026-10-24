#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Quiz bittiğinde puanı, yanlışları, "akıcı değil" kelimeleri ve seviye atlayanları gösterir.
    /// "Sadece yanlışlarımı tekrar et" → DialogResult.Retry, "Kapat" → DialogResult.OK.
    /// Tamamen kodla kurulur (Designer dosyası gerekmez).
    /// </summary>
    public sealed class OzetFormu : Form
    {
        public OzetFormu(UygulamaServisleri servisler, QuizOturumu oturum, OdulOzeti? odul)
        {
            Text = "Quiz Özeti";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(600, 420);
            Size = new Size(820, 560);

            var yanlislar = oturum.YanlisKayitlari;

            var ozet = new StringBuilder();
            ozet.Append($"Puan: {oturum.Puan}   •   Yanlış: {yanlislar.Count}   •   Doğru ama yavaş: {oturum.YavasKayitlari.Count}   •   Seviye atlayan: {oturum.Terfiler.Count}");

            if (servisler.Ayarlar.RozetGoster && odul != null)
            {
                ozet.AppendLine();
                ozet.Append($"Bu oturumdan +{odul.KazanilanXp} XP");

                foreach (RozetTanimi rozet in odul.YeniRozetler)
                {
                    ozet.AppendLine();
                    ozet.Append($"Yeni rozet: {rozet.Ad} — {rozet.Aciklama}");
                }
            }

            var baslik = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Text = ozet.ToString(),
                TabStop = false
            };

            var liste = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                ShowGroups = true
            };
            liste.Columns.Add("Kelime", 170);
            liste.Columns.Add("Anlam", 240);
            liste.Columns.Add("Bilgi", 330);

            var grupYanlis = new ListViewGroup("Yanlışlar");
            var grupYavas = new ListViewGroup("Doğru ama yavaş — henüz akıcı değil");
            var grupTerfi = new ListViewGroup("Seviye atlayanlar");
            liste.Groups.Add(grupYanlis);
            liste.Groups.Add(grupYavas);
            liste.Groups.Add(grupTerfi);

            foreach (YanlisKaydi kayit in yanlislar)
            {
                string verilen = kayit.VerilenCevaplar.Count > 0
                    ? " • verdiğin: " + string.Join(", ", kayit.VerilenCevaplar.Distinct())
                    : string.Empty;

                liste.Items.Add(new ListViewItem(new[]
                {
                    kayit.Kart.Kelime,
                    kayit.Kart.Anlam,
                    $"{kayit.YanlisDeneme} yanlış deneme{verilen}"
                }) { Group = grupYanlis });
            }

            foreach (YavasKayit yavas in oturum.YavasKayitlari)
            {
                liste.Items.Add(new ListViewItem(new[]
                {
                    yavas.Kart.Kelime,
                    yavas.Kart.Anlam,
                    $"{yavas.SureMs / 1000.0:0.0} sn"
                }) { Group = grupYavas });
            }

            foreach (TerfiKaydi terfi in oturum.Terfiler)
            {
                liste.Items.Add(new ListViewItem(new[]
                {
                    terfi.Kart.Kelime,
                    terfi.Kart.Anlam,
                    $"{UstalikMerdiveni.Ad(terfi.Degisim.Onceki)} → {UstalikMerdiveni.Ad(terfi.Degisim.Yeni)}"
                }) { Group = grupTerfi });
            }

            var btnTekrar = new Button
            {
                Text = "Sadece yanlışlarımı tekrar et",
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                Enabled = yanlislar.Count > 0,
                Tag = TemaUygulayici.Birincil,
                DialogResult = DialogResult.Retry
            };
            var btnKapat = new Button
            {
                Text = "Kapat",
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                DialogResult = DialogResult.OK
            };

            var butonlar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft
            };
            butonlar.Controls.Add(btnKapat);
            butonlar.Controls.Add(btnTekrar);

            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(baslik, 0, 0);
            kok.Controls.Add(liste, 0, 1);
            kok.Controls.Add(butonlar, 0, 2);

            Controls.Add(kok);
            AcceptButton = btnKapat;
            CancelButton = btnKapat;

            new TemaUygulayici(servisler.Ayarlar).Uygula(this);
        }
    }
}
