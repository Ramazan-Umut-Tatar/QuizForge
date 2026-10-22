using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuizForge
{
    /// <summary>
    /// Bir form ağacına karanlık/açık tema ve yazı boyutu ölçeği uygular.
    /// Tag = "tema-yok" olan kontroller (ve çocukları) renk temasından muaf tutulur;
    /// yazı boyutu ölçeği yine uygulanır.
    /// </summary>
    public sealed class TemaUygulayici
    {
        public const string Atla = "tema-yok";

        private readonly Dictionary<Control, float> _asilBoyutlar = new Dictionary<Control, float>();
        private readonly Dictionary<Button, FlatStyle> _asilStiller = new Dictionary<Button, FlatStyle>();

        public TemaUygulayici()
        {
        }

        public TemaUygulayici(UygulamaAyarlari ayarlar)
        {
            Ayarla(ayarlar.KaranlikTema, ayarlar.FontOlcegiYuzde);
        }

        public bool Karanlik { get; private set; }
        public int FontOlcegiYuzde { get; private set; } = 100;

        public Color Zemin => Karanlik ? Color.FromArgb(32, 32, 34) : SystemColors.Control;
        public Color Yazi => Karanlik ? Color.Gainsboro : SystemColors.ControlText;
        public Color ButonZemin => Karanlik ? Color.FromArgb(58, 58, 62) : SystemColors.Control;
        public Color GirisZemin => Karanlik ? Color.FromArgb(45, 45, 48) : SystemColors.Window;

        public void Ayarla(bool karanlik, int fontOlcegiYuzde)
        {
            Karanlik = karanlik;
            FontOlcegiYuzde = Math.Clamp(fontOlcegiYuzde, 80, 200);
        }

        public void Uygula(Control kok)
        {
            // Önce tüm orijinal değerleri kaydet (ebeveynin fontu değişmeden), sonra uygula.
            Kaydet(kok);
            Isle(kok, false);
        }

        /// <summary>Kırmızı/gri gibi geçici durum renklerinden sonra butonu tema rengine döndürür.</summary>
        public void ButonuSifirla(Button buton)
        {
            buton.BackColor = ButonZemin;
            buton.ForeColor = Yazi;
            if (!Karanlik)
                buton.UseVisualStyleBackColor = true;
        }

        private void Kaydet(Control kontrol)
        {
            if (!_asilBoyutlar.ContainsKey(kontrol))
                _asilBoyutlar[kontrol] = kontrol.Font.Size;

            if (kontrol is Button buton && !_asilStiller.ContainsKey(buton))
                _asilStiller[buton] = buton.FlatStyle;

            foreach (Control alt in kontrol.Controls)
                Kaydet(alt);
        }

        private void Isle(Control kontrol, bool ustAtla)
        {
            bool renkAtla = ustAtla || Equals(kontrol.Tag, Atla);

            if (!renkAtla)
                RenkUygula(kontrol);

            FontUygula(kontrol);

            foreach (Control alt in kontrol.Controls)
                Isle(alt, renkAtla);
        }

        private void RenkUygula(Control kontrol)
        {
            switch (kontrol)
            {
                case Form form:
                    form.BackColor = Zemin;
                    form.ForeColor = Yazi;
                    break;

                case Button buton:
                    buton.BackColor = ButonZemin;
                    buton.ForeColor = Yazi;
                    if (Karanlik)
                    {
                        buton.FlatStyle = FlatStyle.Flat;
                        buton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 105);
                    }
                    else
                    {
                        if (_asilStiller.TryGetValue(buton, out FlatStyle asil))
                            buton.FlatStyle = asil;
                        buton.UseVisualStyleBackColor = true;
                    }
                    break;

                case TextBox _:
                case ComboBox _:
                case ListBox _:
                case ListView _:
                case NumericUpDown _:
                    kontrol.BackColor = GirisZemin;
                    kontrol.ForeColor = Yazi;
                    break;

                case Label _:
                case CheckBox _:
                case RadioButton _:
                case GroupBox _:
                    kontrol.ForeColor = Yazi;
                    break;
            }
        }

        private void FontUygula(Control kontrol)
        {
            // Form/ContainerControl fontunu değiştirmek AutoScaleMode.Font ile formu yeniden ölçeklendirir.
            if (kontrol is ContainerControl) return;
            if (!_asilBoyutlar.TryGetValue(kontrol, out float asil)) return;

            float hedef = asil * FontOlcegiYuzde / 100f;
            if (Math.Abs(kontrol.Font.Size - hedef) < 0.05f) return;

            kontrol.Font = new Font(kontrol.Font.FontFamily, hedef, kontrol.Font.Style, kontrol.Font.Unit);
        }
    }
}
