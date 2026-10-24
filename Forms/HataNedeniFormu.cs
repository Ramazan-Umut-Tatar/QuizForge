#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Yanlıştan sonra "neden karıştırdın?" sorar (Bilmiyordum / Karıştırdım / Dikkatsizlik).
    /// Yapay zekâ açıksa nedenini açıklatma ve çağrışım önerme düğmeleri de görünür.
    /// Tamamen kodla kurulur (Designer dosyası gerekmez).
    /// </summary>
    public sealed class HataNedeniFormu : Form
    {
        private readonly UygulamaServisleri _s;
        private readonly CozumBilgisi _cozum;

        private readonly TextBox _aiCikti = new TextBox();
        private readonly Button _btnAcikla = new Button();
        private readonly Button _btnCagrisim = new Button();
        private readonly Button _btnNotKaydet = new Button();
        private string? _sonCagrisim;

        public HataNedeni? Secilen { get; private set; }

        public HataNedeniFormu(UygulamaServisleri servisler, CozumBilgisi cozum)
        {
            _s = servisler;
            _cozum = cozum;

            Text = "Hatadan öğren";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(520, 360);
            Size = new Size(640, _s.Ai.Hazir ? 560 : 340);

            string verilen = cozum.VerilenYanlislar.Count > 0
                ? string.Join(", ", cozum.VerilenYanlislar.Distinct())
                : "(cevap verilmedi)";

            string ozet =
                $"Kelime: {cozum.Kart.Kelime}" + Environment.NewLine +
                $"Doğru cevap: {cozum.DogruCevap}" + Environment.NewLine +
                $"Verdiğin cevap(lar): {verilen}";

            string not = _s.Istatistik.NotAl(cozum.Kart);
            if (not.Length > 0)
                ozet += Environment.NewLine + $"Notun: {not}";

            var etiketOzet = new Label { Text = ozet, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            var etiketSoru = new Label
            {
                Text = "Neden yanlış yaptın?",
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            };

            var neden = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            neden.Controls.Add(NedenDugmesi("Bilmiyordum", HataNedeni.Bilmiyordum));
            neden.Controls.Add(NedenDugmesi("Karıştırdım", HataNedeni.Karistirdim));
            neden.Controls.Add(NedenDugmesi("Dikkatsizlik", HataNedeni.Dikkatsizlik));

            var atla = new Button
            {
                Text = "Atla",
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                DialogResult = DialogResult.Cancel
            };
            neden.Controls.Add(atla);
            CancelButton = atla;

            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                Padding = new Padding(14)
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            kok.Controls.Add(etiketOzet, 0, 0);
            kok.Controls.Add(etiketSoru, 0, 1);
            kok.Controls.Add(neden, 0, 2);

            if (_s.Ai.Hazir)
                AiBolumunuEkle(kok);

            Controls.Add(kok);
            new TemaUygulayici(_s.Ayarlar).Uygula(this);
        }

        private Button NedenDugmesi(string metin, HataNedeni deger)
        {
            var dugme = new Button { Text = metin, AutoSize = true, Padding = new Padding(10, 4, 10, 4) };
            dugme.Click += (sender, args) =>
            {
                Secilen = deger;
                DialogResult = DialogResult.OK;
            };
            return dugme;
        }

        private void AiBolumunuEkle(TableLayoutPanel kok)
        {
            var baslik = new Label
            {
                Text = "Yapay zekâ (isteğe bağlı — yalnızca bu kelime Anthropic'e gönderilir):",
                AutoSize = true,
                Margin = new Padding(0, 14, 0, 6)
            };

            _btnAcikla.Text = "Neden yanlış? (AI)";
            _btnAcikla.AutoSize = true;
            _btnAcikla.Padding = new Padding(10, 4, 10, 4);

            _btnCagrisim.Text = "Çağrışım öner (AI)";
            _btnCagrisim.AutoSize = true;
            _btnCagrisim.Padding = new Padding(10, 4, 10, 4);

            _btnNotKaydet.Text = "Çağrışımı not olarak kaydet";
            _btnNotKaydet.AutoSize = true;
            _btnNotKaydet.Padding = new Padding(10, 4, 10, 4);
            _btnNotKaydet.Enabled = false;

            var dugmeler = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            dugmeler.Controls.Add(_btnAcikla);
            dugmeler.Controls.Add(_btnCagrisim);
            dugmeler.Controls.Add(_btnNotKaydet);

            _aiCikti.Multiline = true;
            _aiCikti.ReadOnly = true;
            _aiCikti.ScrollBars = ScrollBars.Vertical;
            _aiCikti.Dock = DockStyle.Fill;
            _aiCikti.MinimumSize = new Size(0, 110);

            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kok.Controls.Add(baslik, 0, 3);
            kok.Controls.Add(dugmeler, 0, 4);
            kok.Controls.Add(_aiCikti, 0, 5);

            _btnAcikla.Click += async (sender, args) =>
            {
                await AiCalistirAsync(
                    () => _s.Ai.HataAciklaAsync(_cozum.Kart.Kelime, _cozum.DogruCevap, _cozum.VerilenYanlislar),
                    false);
            };

            _btnCagrisim.Click += async (sender, args) =>
            {
                await AiCalistirAsync(
                    () => _s.Ai.CagrisimAsync(_cozum.Kart.Kelime, _cozum.Kart.Anlam),
                    true);
            };

            _btnNotKaydet.Click += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(_sonCagrisim)) return;
                _s.Istatistik.NotAyarla(_cozum.Kart, _sonCagrisim);
                _btnNotKaydet.Enabled = false;
                _btnNotKaydet.Text = "Not kaydedildi";
            };
        }

        private async System.Threading.Tasks.Task AiCalistirAsync(Func<System.Threading.Tasks.Task<AiSonucu>> islem, bool cagrisimMi)
        {
            _btnAcikla.Enabled = false;
            _btnCagrisim.Enabled = false;
            _aiCikti.Text = "Yapay zekâ düşünüyor…";

            AiSonucu sonuc = await islem();

            if (IsDisposed) return;

            _btnAcikla.Enabled = true;
            _btnCagrisim.Enabled = true;
            _aiCikti.Text = sonuc.Metin;

            if (sonuc.Basarili && cagrisimMi)
            {
                _sonCagrisim = sonuc.Metin;
                _btnNotKaydet.Enabled = true;
                _btnNotKaydet.Text = "Çağrışımı not olarak kaydet";
            }
        }
    }
}
