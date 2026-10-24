#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Yapıştırılan metinden kelime listesi çıkarır; seçilen kelimeler metindeki cümleyle birlikte
    /// (boşluk doldurma için hazır) bir kelime dosyasına eklenir. Tamamen kodla kurulur.
    /// </summary>
    public sealed class MetindenKelimeFormu : Form
    {
        private const int MaksMetinUzunlugu = 50000;
        private const int MaksSatir = 400;
        private const int AiToplu = 25;

        // Çok sık geçen İngilizce işlev sözcükleri (listeden gizlenir; kapatılabilir).
        private static readonly HashSet<string> SikKelimeler = new HashSet<string>(
            ("the a an and or but if then so of to in on at by for with from as is are was were be been being am do does did done " +
             "have has had having it its this that these those i you he she we they me him her us them my your his our their mine yours " +
             "not no yes can could will would shall should may might must there here when where what which who whom whose why how " +
             "all any both each few more most other some such than too very just also about into over after before between through " +
             "during out up down off again further once only own same because while until against above below under").Split(' '),
            StringComparer.OrdinalIgnoreCase);

        private readonly UygulamaServisleri _s;
        private readonly TextBox _txtMetin = new TextBox();
        private readonly NumericUpDown _nudMin = new NumericUpDown();
        private readonly CheckBox _chkSik = new CheckBox();
        private readonly CheckBox _chkBilinen = new CheckBox();
        private readonly DataGridView _grid = new DataGridView();
        private readonly ComboBox _cmbHedef = new ComboBox();
        private readonly TextBox _txtEtiket = new TextBox();
        private readonly Button _btnAi = new Button();
        private readonly Label _lblDurum = new Label();

        public MetindenKelimeFormu(UygulamaServisleri servisler)
        {
            _s = servisler;

            Text = "Metinden Kelime Çıkar";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(800, 560);
            Size = new Size(1020, 700);

            ArayuzuKur();
            HedefDosyalariListele(null);
            new TemaUygulayici(_s.Ayarlar).Uygula(this);
        }

        private void ArayuzuKur()
        {
            var baslik = new Label
            {
                Text = "Bir makale, e-posta veya metin yapıştırın:",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            _txtMetin.Multiline = true;
            _txtMetin.ScrollBars = ScrollBars.Vertical;
            _txtMetin.AcceptsReturn = true;
            _txtMetin.Dock = DockStyle.Fill;

            _nudMin.Minimum = 2;
            _nudMin.Maximum = 15;
            _nudMin.Value = 4;
            _nudMin.Anchor = AnchorStyles.Left;

            _chkSik.Text = "Sık İngilizce kelimeleri gizle";
            _chkSik.Checked = true;
            _chkSik.AutoSize = true;
            _chkBilinen.Text = "Zaten destede olanları gizle";
            _chkBilinen.Checked = true;
            _chkBilinen.AutoSize = true;

            var btnCikar = new Button { Text = "Kelimeleri çıkar", AutoSize = true, Padding = new Padding(8, 3, 8, 3), Tag = TemaUygulayici.Birincil };
            btnCikar.Click += (sender, args) => Cikar();

            var secenekler = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            secenekler.Controls.Add(btnCikar);
            secenekler.Controls.Add(new Label { Text = "En az harf:", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
            secenekler.Controls.Add(_nudMin);
            secenekler.Controls.Add(_chkSik);
            secenekler.Controls.Add(_chkBilinen);

            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Ekle", FillWeight = 6 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kelime", FillWeight = 16, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sıklık", FillWeight = 7, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Anlam (siz yazın veya AI)", FillWeight = 28 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Metindeki cümle", FillWeight = 43 });

            _cmbHedef.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbHedef.Width = 180;

            var btnYeniDosya = new Button { Text = "Yeni dosya", AutoSize = true, Padding = new Padding(8, 3, 8, 3) };
            var btnEkle = new Button { Text = "İşaretlileri desteye ekle", AutoSize = true, Padding = new Padding(8, 3, 8, 3), Tag = TemaUygulayici.Birincil };
            _btnAi.Text = "İşaretlilerin anlamını AI ile doldur";
            _btnAi.AutoSize = true;
            _btnAi.Padding = new Padding(8, 3, 8, 3);

            btnYeniDosya.Click += (sender, args) => YeniDosya();
            btnEkle.Click += (sender, args) => Ekle();
            _btnAi.Click += async (sender, args) => await AiDoldurAsync();

            _txtEtiket.Width = 140;

            var alt = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            alt.Controls.Add(_btnAi);
            alt.Controls.Add(new Label { Text = "Hedef dosya:", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
            alt.Controls.Add(_cmbHedef);
            alt.Controls.Add(btnYeniDosya);
            alt.Controls.Add(new Label { Text = "Etiket:", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
            alt.Controls.Add(_txtEtiket);
            alt.Controls.Add(btnEkle);

            _lblDurum.AutoSize = true;
            _lblDurum.Margin = new Padding(0, 4, 0, 0);

            var kok = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(12) };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(baslik, 0, 0);
            kok.Controls.Add(_txtMetin, 0, 1);
            kok.Controls.Add(secenekler, 0, 2);
            kok.Controls.Add(_grid, 0, 3);
            kok.Controls.Add(alt, 0, 4);
            kok.Controls.Add(_lblDurum, 0, 5);

            Controls.Add(kok);
        }

        // ------------------------------------------------------------------
        //  Kelime çıkarma
        // ------------------------------------------------------------------

        private void Cikar()
        {
            string metin = _txtMetin.Text;
            if (string.IsNullOrWhiteSpace(metin))
            {
                MessageBox.Show("Önce bir metin yapıştırın.", "Metinden kelime çıkar");
                return;
            }

            if (metin.Length > MaksMetinUzunlugu)
                metin = metin.Substring(0, MaksMetinUzunlugu);

            int enAz = (int)_nudMin.Value;

            var bilinen = new HashSet<string>();
            if (_chkBilinen.Checked)
            {
                foreach (KelimeKarti kart in _s.Kelimeler.TumKartlariYukle())
                {
                    bilinen.Add(kart.KelimeAnahtari);
                    bilinen.Add(kart.Kelime.ToLowerInvariant());
                }
            }

            var sayac = new Dictionary<string, int>();
            foreach (string parca in MetinYardimcisi.KelimeleriAyir(metin))
            {
                string kelime = parca.ToLowerInvariant();

                if (kelime.Length < enAz) continue;
                if (_chkSik.Checked && SikKelimeler.Contains(kelime)) continue;
                if (bilinen.Contains(kelime) || bilinen.Contains(MetinYardimcisi.Normalize(parca))) continue;

                sayac.TryGetValue(kelime, out int n);
                sayac[kelime] = n + 1;
            }

            List<string> cumleler = MetinYardimcisi.CumleleriAyir(metin);

            _grid.SuspendLayout();
            _grid.Rows.Clear();
            foreach (KeyValuePair<string, int> kayit in sayac
                         .OrderByDescending(x => x.Value)
                         .ThenBy(x => x.Key, StringComparer.CurrentCultureIgnoreCase)
                         .Take(MaksSatir))
            {
                _grid.Rows.Add(false, kayit.Key, kayit.Value, string.Empty, CumleBul(cumleler, kayit.Key));
            }

            _grid.ResumeLayout();

            _lblDurum.Text = _grid.Rows.Count == 0
                ? "Uygun kelime bulunamadı (filtreleri gevşetmeyi deneyin)."
                : $"{_grid.Rows.Count} aday kelime listelendi. Öğrenmek istediklerinizi işaretleyip anlamlarını yazın.";
        }

        private static string CumleBul(List<string> cumleler, string kelime)
        {
            var desen = new Regex(@"(?<![\p{L}\p{N}])" + Regex.Escape(kelime) + @"(?![\p{L}\p{N}])",
                                  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            foreach (string cumle in cumleler)
            {
                if (desen.IsMatch(cumle))
                    return cumle.Length > 300 ? cumle.Substring(0, 300) : cumle;
            }

            return string.Empty;
        }

        // ------------------------------------------------------------------
        //  Hedef dosya ve ekleme
        // ------------------------------------------------------------------

        private void HedefDosyalariListele(string? secilecek)
        {
            _cmbHedef.Items.Clear();
            foreach (string isim in _s.Kelimeler.DosyaIsimleriniGetir())
                _cmbHedef.Items.Add(isim);

            if (_cmbHedef.Items.Count == 0) return;

            int index = secilecek != null ? _cmbHedef.Items.IndexOf(secilecek) : 0;
            _cmbHedef.SelectedIndex = index >= 0 ? index : 0;
        }

        private void YeniDosya()
        {
            string? ad = YardimciFormlar.MetinSor(this, _s.Ayarlar, "Yeni dosya", "Dosya adı (uzantısız):");
            if (string.IsNullOrWhiteSpace(ad)) return;

            try
            {
                string temiz = _s.Kelimeler.YeniDosyaOlustur(ad);
                HedefDosyalariListele(temiz);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is System.IO.IOException)
            {
                MessageBox.Show(ex.Message, "Dosya oluşturulamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Ekle()
        {
            if (_cmbHedef.SelectedItem is not string hedef)
            {
                MessageBox.Show("Önce bir hedef dosya seçin veya \"Yeni dosya\" oluşturun.", "Desteye ekle");
                return;
            }

            List<string> etiketler = MetinYardimcisi.EtiketleriAyir(_txtEtiket.Text);
            if (!etiketler.Contains("metinden", StringComparer.CurrentCultureIgnoreCase))
                etiketler.Add("metinden");

            var mevcut = _s.Kelimeler.HamOku(hedef).Kartlar;
            var mevcutAnahtarlar = new HashSet<string>(mevcut.Select(k => k.Anahtar));

            var eklenecek = new List<KelimeKarti>();
            var satirlar = new List<DataGridViewRow>();
            int anlamsiz = 0;
            int zatenVar = 0;

            foreach (DataGridViewRow satir in _grid.Rows)
            {
                if (!(satir.Cells[0].Value is bool secili) || !secili) continue;

                string kelime = Convert.ToString(satir.Cells[1].Value)?.Trim() ?? string.Empty;
                string anlam = Convert.ToString(satir.Cells[3].Value)?.Trim() ?? string.Empty;
                string ornek = Convert.ToString(satir.Cells[4].Value)?.Trim() ?? string.Empty;

                if (anlam.Length == 0)
                {
                    anlamsiz++;
                    continue;
                }

                var kart = new KelimeKarti(kelime, anlam, hedef, ornek, etiketler);
                if (mevcutAnahtarlar.Contains(kart.Anahtar))
                {
                    zatenVar++;
                    continue;
                }

                eklenecek.Add(kart);
                satirlar.Add(satir);
            }

            if (eklenecek.Count == 0)
            {
                MessageBox.Show(
                    anlamsiz > 0
                        ? $"İşaretli {anlamsiz} kelimenin anlamı boş. Anlam yazın veya AI ile doldurun."
                        : "Eklenecek işaretli kelime yok.",
                    "Desteye ekle");
                return;
            }

            try
            {
                _s.Kelimeler.Ekle(hedef, eklenecek);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message, "Eklenemedi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (DataGridViewRow satir in satirlar)
                _grid.Rows.Remove(satir);

            _lblDurum.Text =
                $"{eklenecek.Count} kelime {hedef}.txt dosyasına eklendi" +
                (anlamsiz > 0 ? $" • {anlamsiz} kelime anlamsız olduğu için atlandı" : string.Empty) +
                (zatenVar > 0 ? $" • {zatenVar} kelime zaten vardı" : string.Empty) + ".";
        }

        // ------------------------------------------------------------------
        //  Yapay zekâ ile anlam doldurma (isteğe bağlı)
        // ------------------------------------------------------------------

        private async System.Threading.Tasks.Task AiDoldurAsync()
        {
            if (!_s.Ai.Hazir)
            {
                MessageBox.Show("Yapay zekâ kapalı veya API anahtarı yok. Ayarlar → Yapay zekâ bölümünden açabilirsiniz.",
                    "Yapay zekâ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var istek = new List<(string Kelime, string Cumle)>();
            var satirlar = new List<DataGridViewRow>();

            foreach (DataGridViewRow satir in _grid.Rows)
            {
                if (!(satir.Cells[0].Value is bool secili) || !secili) continue;
                if (!string.IsNullOrWhiteSpace(Convert.ToString(satir.Cells[3].Value))) continue;

                istek.Add((Convert.ToString(satir.Cells[1].Value) ?? string.Empty,
                           Convert.ToString(satir.Cells[4].Value) ?? string.Empty));
                satirlar.Add(satir);

                if (istek.Count >= AiToplu) break;
            }

            if (istek.Count == 0)
            {
                MessageBox.Show("Anlamı boş olan işaretli kelime yok. Önce doldurmak istediğiniz kelimeleri işaretleyin.", "Yapay zekâ");
                return;
            }

            var onay = MessageBox.Show(
                $"{istek.Count} kelime ve bunların metindeki cümleleri Anthropic'e gönderilecek. Devam edilsin mi?",
                "Yapay zekâ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (onay != DialogResult.Yes) return;

            _btnAi.Enabled = false;
            _lblDurum.Text = "Yapay zekâ anlamları hazırlıyor…";

            AiSonucu sonuc = await _s.Ai.AnlamlariDoldurAsync(istek);

            if (IsDisposed) return;
            _btnAi.Enabled = true;

            if (!sonuc.Basarili)
            {
                _lblDurum.Text = sonuc.Metin;
                return;
            }

            Dictionary<string, string> anlamlar = ClaudeAiServisi.AnlamlariAyristir(sonuc.Metin);
            int doldurulan = 0;

            foreach (DataGridViewRow satir in satirlar)
            {
                string kelime = Convert.ToString(satir.Cells[1].Value) ?? string.Empty;
                if (anlamlar.TryGetValue(kelime, out string? anlam))
                {
                    satir.Cells[3].Value = anlam;
                    doldurulan++;
                }
            }

            _lblDurum.Text = $"{doldurulan}/{istek.Count} anlam dolduruldu. Yapay zekâ hata yapabilir: eklemeden önce kontrol edin.";
        }
    }
}
