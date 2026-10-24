#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// .txt kelime dosyalarını tablo üzerinden düzenler: ekleme, silme, örnek cümle, etiket.
    /// Her değişiklikte tekrar, çakışan anlam ve örnek cümle kontrolü anında yapılır.
    /// Tamamen kodla kurulur (Designer dosyası gerekmez).
    /// </summary>
    public sealed class DuzenleyiciFormu : Form
    {
        private readonly UygulamaServisleri _s;
        private readonly ComboBox _cmbDosya = new ComboBox();
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _lblDurum = new Label();
        private readonly Label _lblUyari = new Label();
        private readonly Button _btnAi = new Button();

        // Her hücre düzenlemesinde tüm satırlar yeniden doğrulanır; değişmeyen satırların kartı yeniden üretilmesin.
        private readonly Dictionary<string, KelimeKarti> _kartOnbellegi = new Dictionary<string, KelimeKarti>();
        private const int MaksOnbellekKartSayisi = 5000;

        private string _aktifDosya = string.Empty;
        private bool _degisti;
        private bool _yukleniyor;

        public DuzenleyiciFormu(UygulamaServisleri servisler)
        {
            _s = servisler;

            Text = "Kelime Düzenleyici";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 480);
            Size = new Size(980, 620);

            ArayuzuKur();
            DosyalariListele(null);
            new TemaUygulayici(_s.Ayarlar).Uygula(this);
        }

        private void ArayuzuKur()
        {
            _cmbDosya.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbDosya.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _cmbDosya.SelectedIndexChanged += (sender, args) => DosyaSecildi();

            var btnYeni = new Button { Text = "Yeni dosya", AutoSize = true, Padding = new Padding(8, 3, 8, 3) };
            var btnKaydet = new Button { Text = "Kaydet", AutoSize = true, Padding = new Padding(8, 3, 8, 3), Tag = TemaUygulayici.Birincil };
            var btnSil = new Button { Text = "Seçili satırları sil", AutoSize = true, Padding = new Padding(8, 3, 8, 3) };
            _btnAi.Text = "AI ile örnek cümle";
            _btnAi.AutoSize = true;
            _btnAi.Padding = new Padding(8, 3, 8, 3);

            btnYeni.Click += (sender, args) => YeniDosya();
            btnKaydet.Click += (sender, args) => Kaydet();
            btnSil.Click += (sender, args) => SeciliSatirlariSil();
            _btnAi.Click += async (sender, args) => await AiOrnekCumleAsync();

            var ust = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, AutoSize = true };
            ust.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++)
                ust.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ust.Controls.Add(_cmbDosya, 0, 0);
            ust.Controls.Add(btnYeni, 1, 0);
            ust.Controls.Add(btnSil, 2, 0);
            ust.Controls.Add(_btnAi, 3, 0);
            ust.Controls.Add(btnKaydet, 4, 0);

            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = true;
            _grid.AllowUserToDeleteRows = true;
            _grid.RowHeadersVisible = true;
            _grid.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;
            _grid.MultiSelect = true;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Columns.Add(Kolon("Kelime", 22));
            _grid.Columns.Add(Kolon("Anlam", 28));
            _grid.Columns.Add(Kolon("Örnek cümle", 36));
            _grid.Columns.Add(Kolon("Etiketler", 14));
            _grid.CellEndEdit += (sender, args) => Degisti();
            _grid.UserDeletedRow += (sender, args) => Degisti();

            _lblUyari.AutoSize = true;
            _lblUyari.Margin = new Padding(0, 6, 0, 0);
            _lblDurum.AutoSize = true;
            _lblDurum.Margin = new Padding(0, 4, 0, 0);

            var kok = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
            kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            kok.Controls.Add(ust, 0, 0);
            kok.Controls.Add(_grid, 0, 1);
            kok.Controls.Add(_lblUyari, 0, 2);
            kok.Controls.Add(_lblDurum, 0, 3);

            Controls.Add(kok);
            FormClosing += DuzenleyiciKapaniyor;
        }

        private static DataGridViewTextBoxColumn Kolon(string baslik, float agirlik) => new DataGridViewTextBoxColumn
        {
            HeaderText = baslik,
            FillWeight = agirlik,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        // ------------------------------------------------------------------
        //  Dosya işlemleri
        // ------------------------------------------------------------------

        private void DosyalariListele(string? secilecek)
        {
            _yukleniyor = true;
            _cmbDosya.Items.Clear();
            foreach (string isim in _s.Kelimeler.DosyaIsimleriniGetir())
                _cmbDosya.Items.Add(isim);
            _yukleniyor = false;

            if (_cmbDosya.Items.Count == 0)
            {
                _aktifDosya = string.Empty;
                _grid.Rows.Clear();
                _lblDurum.Text = "Klasörde .txt dosyası yok. \"Yeni dosya\" ile oluşturabilirsiniz.";
                return;
            }

            int index = secilecek != null ? _cmbDosya.Items.IndexOf(secilecek) : 0;
            _cmbDosya.SelectedIndex = index >= 0 ? index : 0;
        }

        private void DosyaSecildi()
        {
            if (_yukleniyor || _cmbDosya.SelectedItem is not string yeni) return;
            if (yeni == _aktifDosya) return;

            if (!DegisiklikleriSor())
            {
                // Kullanıcı vazgeçti: seçimi eski dosyaya geri al.
                _yukleniyor = true;
                _cmbDosya.SelectedItem = _aktifDosya;
                _yukleniyor = false;
                return;
            }

            DosyayiYukle(yeni);
        }

        private void DosyayiYukle(string dosya)
        {
            YuklemeSonucu sonuc = _s.Kelimeler.HamOku(dosya);
            _aktifDosya = dosya;

            _grid.Rows.Clear();
            foreach (KelimeKarti kart in sonuc.Kartlar)
                _grid.Rows.Add(kart.Kelime, kart.Anlam, kart.Ornek, string.Join(", ", kart.Etiketler));

            _degisti = false;
            Dogrula();

            if (sonuc.Uyarilar.Count > 0)
            {
                _lblDurum.Text =
                    $"{sonuc.Uyarilar.Count} satır okunamadı ve kaydedince dosyadan çıkarılacak (önceki sürüm .bak olarak saklanır).";
            }
            else
            {
                _lblDurum.Text = $"{dosya}.txt yüklendi.";
            }
        }

        private void YeniDosya()
        {
            if (!DegisiklikleriSor()) return;

            string? ad = YardimciFormlar.MetinSor(this, _s.Ayarlar, "Yeni dosya", "Dosya adı (uzantısız):");
            if (string.IsNullOrWhiteSpace(ad)) return;

            try
            {
                string temiz = _s.Kelimeler.YeniDosyaOlustur(ad);
                DosyalariListele(temiz);
                DosyayiYukle(temiz);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is System.IO.IOException)
            {
                MessageBox.Show(ex.Message, "Dosya oluşturulamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool Kaydet()
        {
            if (_aktifDosya.Length == 0) return true;

            List<KelimeKarti> kartlar = SatirlardanKartlar(out int eksik, out _);

            if (eksik > 0)
            {
                DialogResult cevap = MessageBox.Show(
                    $"{eksik} satırda kelime veya anlam boş; bu satırlar kaydedilmeyecek. Devam edilsin mi?",
                    "Eksik satırlar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (cevap != DialogResult.Yes) return false;
            }

            int tekrar = KelimeDogrulayici.Analiz(kartlar, false).Count(u => u.Duzey == UyariDuzeyi.Hata);
            if (tekrar > 0)
            {
                DialogResult cevap = MessageBox.Show(
                    $"{tekrar} birebir tekrar kaydı var (kırmızı satırlar). Yine de kaydedilsin mi?",
                    "Tekrar eden kayıtlar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (cevap != DialogResult.Yes) return false;
            }

            try
            {
                _s.Kelimeler.Kaydet(_aktifDosya, kartlar);
                _degisti = false;
                _lblDurum.Text = $"{_aktifDosya}.txt kaydedildi ({kartlar.Count} kelime).";
                return true;
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message, "Kaydedilemedi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>Kaydedilmemiş değişiklik varsa sorar. false → işlem iptal edilmeli.</summary>
        private bool DegisiklikleriSor()
        {
            if (!_degisti) return true;

            DialogResult cevap = MessageBox.Show(
                "Kaydedilmemiş değişiklikler var. Kaydedilsin mi?",
                "Düzenleyici", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (cevap == DialogResult.Cancel) return false;
            if (cevap == DialogResult.Yes) return Kaydet();

            _degisti = false;
            return true;
        }

        private void DuzenleyiciKapaniyor(object? sender, FormClosingEventArgs e)
        {
            if (!DegisiklikleriSor())
                e.Cancel = true;
        }

        // ------------------------------------------------------------------
        //  Satır işlemleri ve anında doğrulama
        // ------------------------------------------------------------------

        private void SeciliSatirlariSil()
        {
            var silinecek = _grid.SelectedRows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            if (silinecek.Count == 0) return;

            foreach (DataGridViewRow satir in silinecek)
                _grid.Rows.Remove(satir);

            Degisti();
        }

        private void Degisti()
        {
            _degisti = true;
            Dogrula();
        }

        private static string Hucre(DataGridViewRow satir, int sutun) =>
            Convert.ToString(satir.Cells[sutun].Value)?.Trim() ?? string.Empty;

        /// <summary>Tablodaki dolu satırları karta çevirir. satirIndeksleri[i] = i. kartın tablo satırı.</summary>
        private List<KelimeKarti> SatirlardanKartlar(out int eksik, out List<int> satirIndeksleri)
        {
            var kartlar = new List<KelimeKarti>();
            satirIndeksleri = new List<int>();
            eksik = 0;

            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                DataGridViewRow satir = _grid.Rows[i];
                if (satir.IsNewRow) continue;

                string kelime = Hucre(satir, 0);
                string anlam = Hucre(satir, 1);
                string ornek = Hucre(satir, 2);
                string etiket = Hucre(satir, 3);

                if (kelime.Length == 0 && anlam.Length == 0 && ornek.Length == 0 && etiket.Length == 0)
                    continue;

                if (kelime.Length == 0 || anlam.Length == 0)
                {
                    eksik++;
                    continue;
                }

                string onbellekAnahtari = string.Join("\t", _aktifDosya, kelime, anlam, ornek, etiket);
                if (!_kartOnbellegi.TryGetValue(onbellekAnahtari, out KelimeKarti? kart))
                {
                    if (_kartOnbellegi.Count >= MaksOnbellekKartSayisi)
                        _kartOnbellegi.Clear();

                    kart = new KelimeKarti(kelime, anlam, _aktifDosya, ornek, MetinYardimcisi.EtiketleriAyir(etiket));
                    _kartOnbellegi[onbellekAnahtari] = kart;
                }

                kartlar.Add(kart);
                satirIndeksleri.Add(i);
            }

            return kartlar;
        }

        private void Dogrula()
        {
            foreach (DataGridViewRow satir in _grid.Rows)
            {
                satir.ErrorText = string.Empty;
                satir.DefaultCellStyle.BackColor = Color.Empty;
                satir.DefaultCellStyle.ForeColor = Color.Empty;
            }

            List<KelimeKarti> kartlar = SatirlardanKartlar(out int eksik, out List<int> indeksler);

            // Yarım doldurulmuş satırları işaretle
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                DataGridViewRow satir = _grid.Rows[i];
                if (satir.IsNewRow || indeksler.Contains(i)) continue;

                bool bos = Hucre(satir, 0).Length == 0 && Hucre(satir, 1).Length == 0
                           && Hucre(satir, 2).Length == 0 && Hucre(satir, 3).Length == 0;
                if (bos) continue;

                Isaretle(satir, "Kelime ve anlam boş bırakılamaz.", UyariDuzeyi.Hata);
            }

            int hata = 0;
            int uyari = 0;

            foreach (DogrulamaUyarisi u in KelimeDogrulayici.Analiz(kartlar))
            {
                if (u.Duzey == UyariDuzeyi.Hata) hata++;
                else if (u.Duzey == UyariDuzeyi.Uyari) uyari++;

                foreach (int k in u.Satirlar)
                    Isaretle(_grid.Rows[indeksler[k]], u.Mesaj, u.Duzey);
            }

            _lblUyari.Text = $"{kartlar.Count} kelime  •  {hata + eksik} hata  •  {uyari} uyarı" +
                             (hata + eksik + uyari > 0 ? "  (satır başındaki simgenin üstüne gelin)" : string.Empty);
        }

        private static void Isaretle(DataGridViewRow satir, string mesaj, UyariDuzeyi duzey)
        {
            satir.ErrorText = string.IsNullOrEmpty(satir.ErrorText) ? mesaj : satir.ErrorText + " | " + mesaj;

            // Açık renkli zemin üzerinde koyu yazı: karanlık temada da okunur.
            satir.DefaultCellStyle.ForeColor = Color.Black;
            if (duzey == UyariDuzeyi.Hata)
                satir.DefaultCellStyle.BackColor = Color.FromArgb(255, 190, 190);
            else if (satir.DefaultCellStyle.BackColor != Color.FromArgb(255, 190, 190))
                satir.DefaultCellStyle.BackColor = duzey == UyariDuzeyi.Uyari
                    ? Color.FromArgb(255, 236, 170)
                    : Color.FromArgb(215, 230, 250);
        }

        // ------------------------------------------------------------------
        //  Yapay zekâ ile örnek cümle (isteğe bağlı)
        // ------------------------------------------------------------------

        private async System.Threading.Tasks.Task AiOrnekCumleAsync()
        {
            if (!_s.Ai.Hazir)
            {
                MessageBox.Show("Yapay zekâ kapalı veya API anahtarı yok. Ayarlar → Yapay zekâ bölümünden açabilirsiniz.",
                    "Yapay zekâ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow? satir = _grid.CurrentRow;
            if (satir == null || satir.IsNewRow || Hucre(satir, 0).Length == 0 || Hucre(satir, 1).Length == 0)
            {
                MessageBox.Show("Önce kelime ve anlamı dolu bir satır seçin.", "Yapay zekâ");
                return;
            }

            _btnAi.Enabled = false;
            _lblDurum.Text = "Yapay zekâdan örnek cümle isteniyor (yalnızca bu kelime gönderilir)…";

            AiSonucu sonuc = await _s.Ai.OrnekCumleAsync(Hucre(satir, 0), Hucre(satir, 1));

            if (IsDisposed) return;
            _btnAi.Enabled = true;

            if (!sonuc.Basarili)
            {
                _lblDurum.Text = sonuc.Metin;
                return;
            }

            satir.Cells[2].Value = sonuc.Metin.Trim().Trim('"');
            _lblDurum.Text = "Örnek cümle eklendi. Lütfen kontrol edin, sonra Kaydet'e basın.";
            Degisti();
        }
    }
}
