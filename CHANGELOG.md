# Değişiklik Günlüğü

## [Yayınlanmamış] — 2026-10-24

### Eklendi
- **Ustalık merdiveni:** Yeni → Öğreniliyor → Pekişti → Usta. Soru tipi seviyeye göre değişir
  (şıklı → yazılı → cümle içinde). Seviye atlamak için üst üste, ilk denemede ve hızlı doğru gerekir.
- **Cümle içinde boşluk doldurma:** kelime dosyalarında isteğe bağlı 3. sütun (örnek cümle) ve 4. sütun (etiketler).
  Eski 2 sütunlu dosyalar aynen çalışır.
- **Cevap süresi takibi:** doğru ama yavaş cevap "akıcı değil" işaretlenir.
- **Karıştırdığın çiftler antrenmanı:** benzer iki kelime yan yana sorulur, farklar vurgulanır.
- **Hatadan öğrenme:** yanlıştan sonra neden sorusu; son 7 günlük hata türü dağılımı.
- **Yorgunluk uyarısı** ve geçmiş oturumlardan **ideal oturum uzunluğu**.
- **Bugünkü tekrar** oturumu ve günde bir kez bildirim (uygulama açıkken / tepsideyken).
- **Rozet ve XP** (öğrenme davranışlarını ödüllendirir, kapatılabilir).
- **Kelime düzenleyici** ve **metinden kelime çıkarma**.
- **İsteğe bağlı yapay zekâ desteği** (varsayılan kapalı).
- Çoklu dosya seçimi, etiket filtresi, otomatik veri doğrulama.
- **Dört tema:** Sakin gece (varsayılan), Sepya, Koyu gri, Sistem (açık).
- Tema renklerini alan `Bar` ilerleme çubuğu; `ArayuzYardimcisi`.

### Değişti
- Tüm formlar kart tabanlı, düz stilli ve pencereyle birlikte büyüyen yeni düzene taşındı.
- Öğrenme paneli yan menülü sayfalara dönüştürüldü.
- `KaranlikTema` ayarı kaldırıldı, yerine `Tema` geldi (eski değer yok sayılır).
- Proje ve ad alanı `Odev2` → `QuizForge`.

### Düzeltildi
- Ayarlar penceresinde yazıların kayması (üst üste binen kontroller, AutoSize olmayan onay kutuları).
- Yanlış işaretlenen şıkların okunaksız gri yazıyla devre dışı kalması.
- Çift antrenmanında "Bitir" sonrası düğmenin çift tetiklenmesi.
- Kelime seçiminde listenin son bölümünü neredeyse hiç sormayan dağılım.

### Optimizasyon
- Kelime dosyaları için değişiklik zamanına dayalı önbellek (`KelimeDeposu`).
- Değişmeyen içerik için gereksiz disk yazımının atlanması (`JsonDosya`).
- Şık havuzu ve son ekin önceden hesaplanması (`QuizOturumu`, `KelimeKarti`).
- Düzenleyicide kart önbelleği; panelde yalnızca açık sayfanın üretilmesi.
- Font önbelleği (GDI nesnesi sızıntısı önlendi); toplu satır eklemede düzen hesaplamasının askıya alınması.
