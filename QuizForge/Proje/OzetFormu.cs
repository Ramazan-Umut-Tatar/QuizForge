using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Quiz bittiğinde puanı ve yanlış yapılan kelimeleri gösterir.
    /// "Sadece yanlışlarımı tekrar et" → DialogResult.Retry, "Kapat" → DialogResult.OK.
    /// Tamamen kodla kurulur (Designer dosyası gerekmez).
    /// </summary>
    public sealed class OzetFormu : Form
    {
        public OzetFormu(IReadOnlyList<YanlisKaydi> yanlislar, int puan, UygulamaAyarlari ayarlar)
        {
            Text = "Quiz Özeti";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(560, 400);
            Size = new Size(780, 520);

            var baslik = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                Font = new Font(Font, FontStyle.Bold),
                Text = yanlislar.Count == 0
                    ? $"Tebrikler, hiç yanlışın yok! Puan: {puan}"
                    : $"Puan: {puan}   •   Yanlış yapılan kelime: {yanlislar.Count}"
            };

            var liste = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false
            };
            liste.Columns.Add("Kelime", 170);
            liste.Columns.Add("Doğru anlam", 230);
            liste.Columns.Add("Yanlış", 60);
            liste.Columns.Add("Verdiğin cevaplar", 240);

            foreach (YanlisKaydi kayit in yanlislar)
            {
                liste.Items.Add(new ListViewItem(new[]
                {
                    kayit.Kart.Kelime,
                    kayit.Kart.Anlam,
                    kayit.YanlisDeneme.ToString(),
                    string.Join(", ", kayit.VerilenCevaplar.Distinct())
                }));
            }

            var btnTekrar = new Button
            {
                Text = "Sadece yanlışlarımı tekrar et",
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                Enabled = yanlislar.Count > 0,
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
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(baslik, 0, 0);
            kok.Controls.Add(liste, 0, 1);
            kok.Controls.Add(butonlar, 0, 2);

            Controls.Add(kok);
            AcceptButton = btnKapat;
            CancelButton = btnKapat;

            new TemaUygulayici(ayarlar).Uygula(this);
        }
    }
}
