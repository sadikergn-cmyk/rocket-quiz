# RocketQuiz: Görsel ve UX Geliştirme Planı

Tarih: 2026-10-15. Kaynak: oyunun mevcut WebGL sürümünün aşama aşama ekran görüntüleri, render ve UI
kod denetimi, 6 web araştırması (grafik teknikleri, UI teknolojisi, çekirdek döngü, elde tutma,
mobil giriş/erişilebilirlik, ödül sineması).

## 1. Uzman değerlendirmesi

### 1.1 Kritik hatalar (her şeyden önce)

Not: İlk incelemede "mobilde görüntü %75 küçük çiziliyor" diye bir bulgu vardı; tarayıcı panelinin
mobil benzetim ekran görüntüsünden kaynaklanan bir yanılgı olduğu anlaşıldı ve listeden çıkarıldı.

| # | Sorun | Etki |
|---|---|---|
| K1 | Unity'nin WebGL metin kutusu iOS'ta klavyeyi güvenilir açmıyor (Unity hata kaydı açık). | Mobilde cevap girişi kırılgan. |
| K2 | Güvenli alan (çentik, alt çubuk) yok; ekran yalnızca genişliğe göre ölçekleniyor. | Başlıklar çentiğin altında kalıyor, kısa ekranlarda kartlar çakışıyor. |

### 1.2 Görsel: neden "yapay" duruyor (etki sırasıyla)

1. **Gölge ve ortam kapatma (AO) yok.** Roket rampaya "yapıştırılmış" gibi duruyor; temas hissi yok.
2. **Düz renk materyaller.** Doku, normal haritası, panel çizgisi, is, buz yok. Işık modeli Phong +
   sahte gökyüzü yansıması.
3. **Kule birkaç küpten ibaret.** Kafes yapı yok; ölçek ve detay hissi kayboluyor.
4. **Duman ışıksız ve küçük.** Prosedürel daireler, flipbook yok, yumuşak parçacık yok, zemine sert
   kesiliyor. Gerçek fırlatmanın imzası olan dev buhar bulutu eksik.
5. **Zemin ve bulutlar düz düzlemler.** Gürültü dokulu tek dörtgen zemin, yandan bakınca levha gibi
   görünen bulut katmanları, sabit renkli sis.
6. **Gökyüzü fiziksel değil.** İki renkli geçiş; yüksekliğe ve güneşe göre değişen saçılım yok.
7. **Kenar yumuşatma (AA) yok.** İnce direkler ve roket kenarları titriyor.
8. **Uzayda doku yoğunluğu düşük.** Dünya 2K doku (yakında bulanık), yıldızlar büyütülmüş ve yumuşak,
   Ay'da kabartma yok, Satürn halkası gölge/saçılımsız, Andromeda prosedürel bir girdap.
9. **Kompozisyon hataları.** 2 milyon km'de Jüpiter ile Mars üst üste, "MARS" yazısı Jüpiter'in
   üstünde; fırlatma anında ekranın yarısını kaplayan koyu dikdörtgen.
10. **Post işleme "oyunsu".** Çizgi efektleri, renk sapması darbeleri, +12 kontrast; film greni,
    hareket bulanıklığı, lens dili yok.

### 1.3 Arayüz ve UX

1. **Tipografi ve stil:** Unity'nin gömülü Arial benzeri fontu, sahte kalın, stok panel görseli. Marka
   kimliği yok.
2. **Akış:** Başlık/tanıtım yok, doğrudan soru. Tur sonunda özet yok, "tekrar oyna" aynı sırayı
   oynatıyor.
3. **Puanlama baskın stratejisi:** Yanlış cevabın bedeli yok; her soruda en bilinmedik cevabı yazmak
   her zaman ~5000 puan getiriyor.
4. **Cevaplar hiç gösterilmiyor:** "Diğerleri ne dedi?" anı (oyunun en tatmin edici anı olabilecek
   yer) yok.
5. **Meta döngü yok:** En iyi skor, seri, kilit açma, günlük içerik yok. Toplam 6 soru.
6. **Yazma sürtünmesi:** Yanlış cevaptan sonra klavye kapanıyor, yazım hatası toleransı yok, kalan
   hak görünmüyor.
7. **Ölü zaman:** Sayım ve geçişler atlanamıyor; 6 fırlatmanın hepsi aynı uzunlukta (tekrar
   yorgunluğu).
8. **Erişilebilirlik:** Ses/flaş/titreşim/azaltılmış hareket ayarları yok; kademeler yalnızca renkle
   ayırt ediliyor.

## 2. Alınan kararlar

| Konu | Karar |
|---|---|
| Platform | Önce web (WebGL2, GitHub Pages), sonra iOS/Android uygulaması. İki kademeli mimari. |
| Sanat yönü | Sinematik gerçekçi (SpaceX/NASA canlı yayını, belgesel). |
| Arayüz | "Görev kontrol": cam paneller, ince çizgiler, telemetri sayıları, yayın grafikleri. |
| Kurallar | 3 yakıt tankı + 3. sorudan itibaren hedef bahsi + logaritmik nadirlik puanı. Süre yok. |
| Mod | Günlük görev (herkese aynı 6 soru) + serbest alıştırma; seri, yıldız haritası, rütbeler. |
| İçerik | 60+ soru, tahmini yüzdeler (işaretli); gerçek veri sonra. |
| Sunucu | Sonraki faz. Şimdilik sunucusuz: günlük tohum, link ile meydan okuma, cihazda kayıt. |

## 3. Hedef mimari

### 3.1 Render: iki kademe, tek içerik

`QualityTier` (açılışta cihaz/GPU'ya göre seçilir, ayarlardan değiştirilebilir):

| Özellik | Web (WebGL2) | Uygulama (Metal/Vulkan) |
|---|---|---|
| Yol | Forward (Forward+ Chrome'da Lit'i bozuyor) | Forward+ |
| Çözünürlük | Render scale 1, cihaz piksel oranı ≤2 (HTML tarafında) | Render scale 1, STP/TAA sonraki sürümde |
| Gölge | Ana ışık, 1 kaskad, 1024, rampa yakınında | 2 kaskad, 2048, yumuşak |
| AO | Prosedürel temas AO (köşe/kap) | SSAO |
| AA | FXAA veya 2x MSAA | MSAA 4x / TAA |
| Duman | 6 yönlü ışıklı flipbook, yumuşak parçacık | + daha fazla parçacık, ısı dalgası |
| Atmosfer | Editor'de önceden hesaplanmış LUT'lar | Aynı LUT'lar, daha yüksek örnekleme |
| Doku | 4K Dünya, ASTC/ETC2 | 8K Dünya |
| Post | Dual bloom ¼, LUT, gren, lens flare (SRP) | + hareket bulanıklığı, DOF |

- **Kamera:** "Dünya hareket eder, roket sabit" şeması kalır (hassasiyet için doğru tercih). Yakın
  sahne derinliğine ihtiyaç duyan efektler (yumuşak parçacık, ısı dalgası) için yakın kamera
  derinlik dokusu açılır; post işlem tek noktada (son kamera) uygulanır.
- **Materyal:** `RocketQuiz/Surface` → GGX + Smith PBR, albedo/normal/ORM doku yuvaları, HDRI'dan
  önceden filtrelenmiş yansıma, gölge alma/düşürme pasları. Alternatif: WebGL'de Forward yolunda
  URP Lit test edilir; çalışırsa standart Lit'e dönülür (daha az bakım).
- **Işık:** Poly Haven açık gökyüzü HDRI (CC0) ile ortam ve yansıma; güneş yönü HDRI ile eşleşir.
- **Shader kuralı:** Elle HLSL kalır (VFX Graph/Shader Graph yok). Sinüs-hash gürültüleri gürültü
  dokularına çevrilir (mobilde çok daha ucuz).

### 3.2 Arayüz

- **Teknoloji:** uGUI + TextMeshPro (Unity 6'da `com.unity.ugui` içinde). UI Toolkit'in arka plan
  bulanıklığı ve mobil web klavye desteği henüz yeterli değil.
- **Tasarım sistemi:** `UITheme` (renk, tipografi, boşluk, köşe, hareket süreleri), tek bir SDF
  shader (yuvarlak köşe, ince çerçeve, gradyan, parıltı, cam), LitMotion ile hareket (MIT).
- **Fontlar (OFL, Türkçe tam):** Unbounded (başlık), Space Grotesk (gövde), JetBrains Mono
  (telemetri sayıları). `Assets/Fonts/`.
- **Simgeler:** Phosphor (MIT), 2x PNG + Sprite Atlas.
- **Web cevap girişi:** HTML `<input>` katmanı + `SendMessage` köprüsü (iOS klavye sorunu çözümü),
  `visualViewport` ile klavyenin üstünde kalan giriş çubuğu. Uygulamada TMP_InputField.
- **Kabuk:** 100dvh, `env(safe-area-inset-*)`, yatay ekranda "telefonu dik tut" katmanı, "Başla"
  dokunuşunda ses kilidini açma, sekme gizlenince duraklatma, PWA manifest'i.

### 3.3 Oyun kuralları v2

- **Nadirlik puanı:** `R = min(150, round(20·log2(100 / max(p, 0.5))))`
  (%50→20, %20→46, %10→66, %5→86, %2→113, %1→133, listede olmayan nadir→150).
- **Yakıt:** 3 tank; doğru cevap 1./2./3. denemede ×1,0 / ×0,6 / ×0,3. Üç yanlış = fırlatma iptal, 0.
  Aynı yanlış cevabı tekrar yazmak tank yakmaz.
- **Hedef bahsi (3. sorudan itibaren, isteğe bağlı):** ilan edilen kademeye ulaşırsa ×1,5, ulaşamazsa
  ×0,5.
- **Eşleştirme:** Türkçe küçük harf + ASCII katlama (ı/i, ş/s…), takma adlar, Damerau-Levenshtein
  yazım toleransı (≤4 harf: 0, 5–8: 1, ≥9: 2), "Bunu mu demek istedin?" çipi, otomatik tamamlama yok.
- **Kademe eşikleri** aynı kalır (%20 / %10 / %5 / %2); fırlatma sırasında "% sayacı" 100'den aşağı
  akar, her eşik bir kademe kapısıdır (Pointless sütunu gerilimi).

### 3.4 Akış ve ekranlar

1. **Yükleme:** markalı yükleyici, ipuçları, tekrar ziyarette "önbellekten".
2. **Hangar (ana ekran):** günlük görev kartı (seri, geri sayım), serbest mod, yıldız haritası,
   rütbe, ayarlar. İlk açılışta doğrudan ilk soruya gider (önce oyna, sonra açıkla).
3. **Soru:** görev başlığı, soru, 3 yakıt göstergesi, denenen yanlış cevaplar, alttaki giriş çubuğu.
4. **Hedef seçimi (3. sorudan):** 5 kademelik merdiven, risk/ödül.
5. **Fırlatma:** T-3 basılı tut ateşleme, yayın HUD'u (CANLI, T+ saat, irtifa, hız, olay şeridi,
   kademe merdiveni, kamera etiketi), basılı tut ×3, ilk izlemeden sonra kısaltılmış sürüm.
6. **Sonuç + cevap tablosu:** kademe, "sadece %X", bir sonraki kademeye ne gerektiği (dürüst
   yakın kaçış), en popüler 5 cevap çubuklarla, oyuncunun cevabı vurgulu, kaçırılan 2-3 nadir cevap,
   puan sayımı (dokununca atlar).
7. **Tur özeti:** 6 satır, toplam, en iyi, spoiler'sız paylaşım kartı (🌍🛰️🌕🪐🌌) + meydan okuma
   linki.
8. **Ayarlar:** ses, titreşim, flaş azaltma, azaltılmış hareket, yazı boyutu.

### 3.5 Ödül sineması (ilk izleme süreleri, tekrarlarda ×0,6)

| Kademe | Süre | Doruk anı |
|---|---|---|
| Uzayın sınırı | ~6 sn | MAX-Q sarsıntısı, Kármán hattı |
| Yörünge | ~8 sn | MECO, kademe ayrılması (120 ms sessizlik), Dünya'nın eğriliği |
| Ay | ~10 sn | Ay'a transfer, Ay kadrajı doldurur |
| Satürn | ~13 sn | "Ay'ı geçti!", halkalarda 0,4 sn ağır çekim |
| Andromeda | ~17 sn | Davul, ışık yılı birimi, warp, 0,8 sn sessizlik, galaksi |

Kurallar: sarsıntı "travma" değeriyle, yalnızca dönme, ≤1,5°; saniyede ≤3 flaş, doygun kırmızı flaş
yok; titreşim seyrek ve keskin; kademe renkleri renk körü dostu (Okabe-Ito) ve her biri simgeli.

## 4. Fazlar

Her faz: derlenir, testler geçer, WebGL'de mobil görünümde doğrulanır, commit + push.

| Faz | İçerik | Kabul ölçütü |
|---|---|---|
| **F0 Temel düzeltmeler** | HTML cevap girişi, web kabuğu (dvh, güvenli alan, ses kilidi, yatay uyarı, sekme duraklatma) | Telefon görünümünde tam ekran, dokunma doğru, iOS benzetiminde cevap yazılabiliyor |
| **F1 Tasarım sistemi** | UITheme, fontlar (TMP), SDF panel shader'ı, LitMotion yardımcıları, SafeArea, yeni soru kartı ve HUD iskeleti | Yeni soru ekranı mobilde, eski Text bileşenleri kaldırılmış |
| **F2 Kurallar v2 + içerik** | Yakıt, hedef bahsi, log puan, yazım toleransı, 60+ soru, günlük tohum, cihazda kayıt | Testler: puan formülü, eşleştirme, tohum determinizmi |
| **F3 Ekranlar** | Hangar, sonuç + cevap tablosu, tur özeti, paylaşım kartı/linki, ayarlar, tanıtım | Tüm akış uçtan uca oynanıyor |
| **F4 Fırlatma sineması** | Beat sheet, yayın HUD'u, kamera kesmeleri (Cinemachine), sarsıntı/ titreşim/ses katmanları, atla/hızlandır, kısaltılmış tekrar | 5 kademe süreleri hedefte, flaş sınırı sağlanıyor |
| **F5 Rampa görselliği** | PBR shader veya Lit testi, gölge, HDRI, AA, kafes kule, rampa/roket dokuları, 6 yönlü duman + buhar bulutu, ısı dalgası | Önce/sonra ekran görüntüsü, mobil web ≥30 fps |
| **F6 Atmosfer ve uzay** | LUT atmosfer + hava perspektifi, 4K/8K Dünya (NASA, kamu malı), şehir ışıkları, bulut gölgesi, keskin yıldızlar, Ay kabartması, Satürn halka gölgesi, Andromeda fotoğrafı, lens flare, kompozisyon düzeltmeleri | Her kademe varış karesi "kapak görseli" kalitesinde |
| **F7 Meta** | Seri (dondurma ile), yıldız haritası/seyir defteri, rütbeler, 3'lü görevler, kozmetik kilitler | Cihazda kalıcı ilerleme |
| **F8 Cila + erişilebilirlik** | Renk derecelendirme LUT'u, gren, PEAT flaş testi, kontrast ≥4.5:1, yazı ölçeği, aria-live yansıtma | Erişilebilirlik kontrol listesi tamam |
| **F9 Uygulama kademesi** | iOS/Android derlemesi, yüksek kademe ayarları, yerel bildirim, haptik | Cihazda test |
| **F10 Sunucu (sonra)** | Supabase anonim cevap kaydı, günlük istatistik → statik JSON, canlı yüzdeler, arkadaş tablosu, KVKK metni | Ayrı karar |

## 5. Bütçeler

- İlk indirme ≤ 30 MB (şu an ~26 MB); ağır dokular sonra Addressables ile.
- Mobil web: orta-üst telefonda ≥ 30 fps, üst seviyede 60 fps; çizim çağrısı ≤ 120; ekran başı
  parçacık örtüşmesi en büyük risk (az sayıda büyük parçacık).
- Ana ekrana ulaşma ≤ 20 sn (ilk ziyaret, iyi bağlantı); ilk eğlence anı ≤ 90 sn.

## 6. Asset ve lisanslar

| Asset | Kaynak | Lisans |
|---|---|---|
| Fontlar | Unbounded, Space Grotesk, JetBrains Mono | OFL |
| Simgeler | Phosphor | MIT |
| Hareket | LitMotion | MIT |
| HDRI, zemin dokuları | Poly Haven | CC0 |
| Duman flipbook'ları | Unity Labs ücretsiz flipbook'lar; 6 yönlü örnek: peeweek/Unity-URP-SmokeLighting | CC0 / MIT |
| Dünya | NASA Blue Marble / Black Marble | Kamu malı |
| Gezegenler 8K | Solar System Scope | CC BY 4.0 (CREDITS.md) |
| Roket/rampa referansı | NASA 3D Resources (Saturn V, Mobile Launcher) | NASA kuralları; NASA/SpaceX logoları kullanılmaz |
| Ses | Kenney Sci-fi + Interface Sounds; mevcut sentez | CC0 |

## 7. Riskler

- Chrome'un Forward+ hatası geri gelebilir → web'de Forward yolu sabit.
- iOS Safari'de HDR float hedefleri garanti değil → gerçek cihaz testi.
- WebGPU, Unity 6.3'te deneysel; üretim kalitesi 6.6+ gerektirir (motor sürümü değişikliği, ayrı
  karar).
- Doldurma oranı (3x piksel oranlı telefonlar, büyük duman, bloom) → piksel oranı ≤2, ¼ bloom.
- Rakip: Krillion (nadir cevap = derin dalış). Ayrışma: sinematik roket, Türkçe içerik, hedef bahsi.
- KVKK: hesap yok, profil çıkaran SDK yok, çerezsiz analitik; serbest metin cevaplar başka
  oyunculara asla ham gösterilmez.
