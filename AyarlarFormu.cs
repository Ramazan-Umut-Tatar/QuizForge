#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Ayarlar penceresi. Tamam'a basılırsa değerler ayar nesnesine yazılır. Tamamen kodla kurulur.
    /// Her bölüm bir kart; içindeki kontroller dikey akış paneliyle dizilir (üst üste binme olmaz).
    /// </summary>
    public sealed class AyarlarFormu : Form
    {
        private const int NotGenisligi = 500;

        private readonly UygulamaServisleri _s;

        private readonly ComboBox _cmbTema = new ComboBox();
        private readonly NumericUpDown _nudFont = new NumericUpDown();
        private readonly CheckBox _chkTekrar = new CheckBox();
        private readonly CheckBox _chkHataNedeni = new CheckBox();
        private readonly CheckBox _chkYorgunluk = new CheckBox();
        private readonly CheckBox _chkRozet = new CheckBox();
        private readonly CheckBox _chkBildirim = new CheckBox();
        private readonly CheckBox _chkAi = new CheckBox();
        private readonly TextBox _txtAnahtar = new TextBox();
        private readonly CheckBox _chkSakla = new CheckBox();
        private readonly TextBox _txtModel = new TextBox();
        private readonly Button _btnTest = ArayuzYardimcisi.Dugme("Bağlantıyı dene");
        private readonly Label _lblTest = new Label();

        public AyarlarFormu(UygulamaServisleri servisler)
        {
            _s = servisler;

            Text = "Ayarlar";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(600, 520);
            Size = new Size(660, 760);

            ArayuzuKur();
            DegerleriYukle();
            new TemaUygulayici(_s.Ayarlar).Uygula(this);
        }

        private void ArayuzuKur()
        {
            foreach (TemaTuru tur in new[] { TemaTuru.Sakin, TemaTuru.Sepya, TemaTuru.Koyu, TemaTuru.Sistem })
                _cmbTema.Items.Add(TemaPaletleri.Ad(tur));
            _cmbTema.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTema.Width = 300;

            _nudFont.Minimum = 80;
            _nudFont.Maximum = 200;
            _nudFont.Increment = 10;
            _nudFont.Width = 80;

            Metin(_chkTekrar, "Yanlış yapılan kelimeleri oturum içinde tekrar sor");
            Metin(_chkHataNedeni, "Yanlıştan sonra \"neden yanlış yaptın?\" diye sor");
            Metin(_chkYorgunluk, "Yorgunluk uyarısı (doğruluk düşüp süre uzarsa mola öner)");
            Metin(_chkRozet, "XP ve rozetleri göster");
            Metin(_chkBildirim, "\"Bugünkü tekrarın hazır\" bildirimi göster");
            Metin(_chkAi, "Yapay zekâ desteğini aç");
            Metin(_chkSakla, "Anahtarı bu bilgisayarda sakla (düz metin, %AppData% klasöründe)");

            _txtAnahtar.UseSystemPasswordChar = true;
            _txtAnahtar.Width = 340;
            _txtModel.Width = 240;

            _btnTest.Click += async (sender, args) => await BaglantiyiDeneAsync();
            _lblTest.AutoSize = true;
            _lblTest.MaximumSize = new Size(NotGenisligi, 0);
            _lblTest.Tag = TemaUygulayici.Soluk;

            var gorunum = Bolum("Görünüm",
                SatirEtiketi("Tema:", _cmbTema),
                SatirEtiketi("Yazı boyutu (%):", _nudFont));

            var calisma = Bolum("Çalışma",
                _chkTekrar, _chkHataNedeni, _chkYorgunluk, _chkRozet);

            var hatirlatici = Bolum("Hatırlatıcı",
                _chkBildirim,
                NotEtiketi("Bildirim yalnızca uygulama açıkken çalışır. Açıkken pencere küçültülünce uygulama " +
                           "görev çubuğu yerine bildirim alanına (tepsiye) gizlenir; simgeye çift tıklayarak geri açabilirsiniz."));

            var ai = Bolum("Yapay zekâ (isteğe bağlı)",
                _chkAi,
                SatirEtiketi("API anahtarı:", _txtAnahtar),
                _chkSakla,
                SatirEtiketi("Model adı:", _txtModel),
                _btnTest,
                _lblTest,
                NotEtiketi("Açıkken, yalnızca sizin bastığınız AI düğmelerinde ilgili kelime ve cümleler Anthropic'e gönderilir " +
                           "(internet ve API anahtarı gerekir; kullanım ücretlendirilebilir). Anahtar boşsa " +
                           "ANTHROPIC_API_KEY ortam değişkeni kullanılır. Anahtarı saklamazsanız yalnızca bu oturumda geçerlidir."));

            Button tamam = ArayuzYardimcisi.Dugme("Tamam", birincil: true);
            Button iptal = ArayuzYardimcisi.Dugme("İptal");
            tamam.DialogResult = DialogResult.OK;
            iptal.DialogResult = DialogResult.Cancel;
            tamam.Click += (sender, args) => DegerleriKaydet();

            var butonlar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 4, 0, 0)
            };
            butonlar.Controls.Add(iptal);
            butonlar.Controls.Add(tamam);

            var kok = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                Padding = new Padding(14),
                AutoScroll = true
            };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.Controls.Add(gorunum, 0, 0);
            kok.Controls.Add(calisma, 0, 1);
            kok.Controls.Add(hatirlatici, 0, 2);
            kok.Controls.Add(ai, 0, 3);
            kok.Controls.Add(butonlar, 0, 4);

            Controls.Add(kok);
            AcceptButton = tamam;
            CancelButton = iptal;
        }

        /// <summary>Onay kutuları varsayılan olarak AutoSize=false gelir; metin kırpılmasın diye açılır.</summary>
        private static void Metin(CheckBox kutu, string metin)
        {
            kutu.Text = metin;
            kutu.AutoSize = true;
        }

        private static Label NotEtiketi(string metin)
        {
            Label etiket = ArayuzYardimcisi.Etiket(metin, TemaUygulayici.Soluk);
            etiket.MaximumSize = new Size(NotGenisligi, 0);
            return etiket;
        }

        /// <summary>Başlık + dikey akışla dizilen kontroller: tek bir kart.</summary>
        private static Control Bolum(string baslik, params Control[] ogeler)
        {
            TableLayoutPanel kart = ArayuzYardimcisi.Kart();
            kart.RowCount = 2;
            kart.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kart.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kart.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var akis = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            foreach (Control oge in ogeler)
            {
                oge.Margin = new Padding(0, 4, 0, 4);
                akis.Controls.Add(oge);
            }

            kart.Controls.Add(ArayuzYardimcisi.BolumBasligi(baslik), 0, 0);
            kart.Controls.Add(akis, 0, 1);
            return kart;
        }

        private static Control SatirEtiketi(string etiket, Control kontrol)
        {
            var satir = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            satir.Controls.Add(new Label { Text = etiket, AutoSize = true, Margin = new Padding(0, 6, 8, 0) });
            kontrol.Margin = new Padding(0);
            satir.Controls.Add(kontrol);
            return satir;
        }

        private void DegerleriYukle()
        {
            UygulamaAyarlari a = _s.Ayarlar;

            _cmbTema.SelectedIndex = Math.Clamp((int)a.Tema, 0, _cmbTema.Items.Count - 1);
            _nudFont.Value = Math.Clamp(a.FontOlcegiYuzde, 80, 200);
            _chkTekrar.Checked = a.YanlisiTekrarSor;
            _chkHataNedeni.Checked = a.HataNedeniSor;
            _chkYorgunluk.Checked = a.YorgunlukUyarisi;
            _chkRozet.Checked = a.RozetGoster;
            _chkBildirim.Checked = a.BildirimAcik;
            _chkAi.Checked = a.AiAcik;
            _txtAnahtar.Text = a.AiAnahtari;
            _chkSakla.Checked = a.AiAnahtariniSakla;
            _txtModel.Text = a.AiModel;
        }

        private void DegerleriKaydet()
        {
            UygulamaAyarlari a = _s.Ayarlar;

            if (_cmbTema.SelectedIndex >= 0)
                a.Tema = (TemaTuru)_cmbTema.SelectedIndex;

            a.FontOlcegiYuzde = (int)_nudFont.Value;
            a.YanlisiTekrarSor = _chkTekrar.Checked;
            a.HataNedeniSor = _chkHataNedeni.Checked;
            a.YorgunlukUyarisi = _chkYorgunluk.Checked;
            a.RozetGoster = _chkRozet.Checked;
            a.BildirimAcik = _chkBildirim.Checked;
            a.AiAcik = _chkAi.Checked;
            a.AiAnahtari = _txtAnahtar.Text.Trim();
            a.AiAnahtariniSakla = _chkSakla.Checked;
            a.AiModel = string.IsNullOrWhiteSpace(_txtModel.Text) ? "claude-haiku-5-5" : _txtModel.Text.Trim();
        }

        private async System.Threading.Tasks.Task BaglantiyiDeneAsync()
        {
            // Henüz kaydedilmemiş değerlerle geçici bir servis kurulur; ayarlar değişmez.
            var gecici = new ClaudeAiServisi(new UygulamaAyarlari
            {
                AiAcik = true,
                AiAnahtari = _txtAnahtar.Text.Trim(),
                AiModel = string.IsNullOrWhiteSpace(_txtModel.Text) ? "claude-haiku-5-5" : _txtModel.Text.Trim()
            });

            _btnTest.Enabled = false;
            _lblTest.Text = "Deneniyor…";

            AiSonucu sonuc = await gecici.BaglantiTestiAsync();

            if (IsDisposed) return;
            _btnTest.Enabled = true;
            _lblTest.Text = sonuc.Basarili ? "Bağlantı başarılı." : sonuc.Metin;
        }
    }
}
