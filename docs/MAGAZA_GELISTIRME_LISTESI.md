# RocketQuiz: Mağaza Hedefli Geliştirme Listesi

Tarih: 2026-10-17. Hedef: Google Play ve App Store'da üst sıralar (Trivia/Word kategorisi, global).
Kaynak: kod, ayar ve doküman incelemesi (rocket-quiz + krillion-tr) ve pazar araştırması.
Bu belge `GELISTIRME_PLANI.md` fazlarını mağaza hedefiyle yeniden sıralar. F2 zaten
`NADIDE_UYUM_TASARIMI.md` ile geçersiz.

## 0. Sahibin kararları (17.10.2026)

| Konu | Karar |
|---|---|
| Ürün | RocketQuiz ile Nadide **tek oyun**. Mağazada tek uygulama; arka uç Nadide sunucusu |
| Ad | **RocketQuiz** |
| Pazar | **Baştan global** |
| İçerik | **Her dil için ayrı günlük set** (nadirlik her dilin kendi oyuncu havuzundan hesaplanır) |
| Gelir | **Ödüllü reklam + abonelik.** Zorla gösterilen reklam yok |
| Ek modlar | **Serbest.** Günlük 5 soru "resmi" kalır; arşiv, pratik, lig gibi ek modlar eklenebilir |
| Hesap | **Misafir + isteğe bağlı giriş** (Apple/Google) |
| Kaynak | 2–5 kişilik ekip, 3–6 ay |

Bu kararların bir kısmı Nadide ADR'leriyle çelişiyor (bkz. A1).

## 1. Uzman değerlendirmesi: puan kartı

| Alan | Puan (5 üzerinden) | Özet |
|---|---|---|
| Çekirdek mekanik | **4** | Yazarak cevap + nadirlik puanı güçlü ve kısa (günde ~2,5 dk). Krillion'un mekaniğine çok yakın; ayrışma görsel kimlikte |
| Görsel / sinematik | **4** | Kodla üretilen PBR roket, HDRI, yörünge haritası. Mobil seviyesinin üstünde |
| Teknik mimari | **3** | `IGameSession` katmanı temiz, sunucu otoritesi doğru, sözleşme testleri var. Yaşam döngüsü, çevrimdışı durum ve performans eksik |
| İlk deneyim (FTUE) | **2** | 8 durak + tuzak öğrenmek gerekiyor; ilk soru zor olabiliyor; web'de ~31 MB yükleme |
| Elde tutma (meta) | **1** | Seri yalnız cihazda ve yeniden kurulumda kayboluyor. Bildirim, koleksiyon, kozmetik, lig yok |
| Sosyal / viral | **1,5** | Yalnız metin paylaşımı. Arkadaş, lig, davet döngüsü yok |
| Gelir | **0** | Hiçbir şey yok |
| Mağaza hazırlığı | **1** | Uygulama kimliği şablonda kalmış, ikon yok, sunucu `http://localhost`, iOS modülü yok |
| İçerik hattı | **2** | Yalnız Türkçe; günlük setler **4 Kasım'da bitiyor** (18 gün kaldı); 1–2 editör |
| Erişilebilirlik / yerelleştirme | **1,5** | Bütün metinler kodda ve Türkçe; native'de ekran okuyucu ve yazı ölçekleme yok |

**Sonuç:** Çekirdek ve görsel kalite üst sıralara yetecek düzeyde. Eksik olan, oyuncuyu ertesi gün
geri getiren ve arkadaşlarına taşıyan katman (meta, sosyal, bildirim) ve native uygulamanın
kendisi. Mağaza sıralamasını en çok **kurulum hızı + D1/D7 elde tutma + puan + çökme/ANR oranı**
belirler. Liste bu sırayla önceliklendirildi.

## 2. Kritik riskler (önce bunlar)

1. **İçerik pisti:** Türkçe günlük setler 4 Kasım 2026'da bitiyor. Oyun çalışsa bile sorusuz kalır.
2. **Yönetişim çelişkisi:** Native uygulama, reklam/analitik SDK'sı ve ilk günden gelir, Nadide
   ADR'leri 0008 §7.1 (önce web/PWA), 0008 §7.5 (profil çıkaran SDK yok) ve 0009 §8.2 (gelir oyuncu
   eşiğinden sonra) ile çelişiyor. ADR 0010'a göre ekip oylaması gerekiyor.
3. **Krillion ile doğrudan rekabet ve klon algısı:** Global İngilizce sürüm Krillion'la aynı pazarda,
   aynı sıfırlanma saatinde (ADR 0012). R5 hukuki risk notu ve 0020 ayrışma korkulukları
   global mağazada daha da önemli; ad/marka/ticari marka taraması yapılmadı.
4. **Kritik kütle:** Nadirlik puanı her dilde yeterli oyuncu ister. Yeni dilde ilk haftalarda
   yüzdeler gürültülü olur. Kalibrasyonlu eşikler (ADR 0017) yardımcı olur ama dil açılışları sıralı yapılmalı.
5. **Türkiye'de 15 yaş düzenlemesi:** Raporlara göre 1 Kasım 2026'da yürürlüğe giriyor; oyun platformları
   için yaş derecelendirmesi ve ebeveyn denetimi öngörüyor olabilir. Arkadaş/lig özellikleri "sosyal"
   sayılabilir. **Doğrulanmadı, hukukçuya sorulmalı.**

## 3. Geliştirme listesi

Simgeler: ☁️ bulut oturumunda yapılabilir (C#/JSON) · 🖥️ yerel Editor gerekir (sahne, ProjectSettings)
· 🌐 Nadide sunucusu (krillion-tr) · 👥 ekip kararı / iş dışı · **P0** lansmanı engeller ·
**P1** sıralama için gerekli · **P2** büyüme.

### A. Kararlar ve hukuk (Faz 0, 1. hafta)

| # | İş | Öncelik | Tür |
|---|---|---|---|
| A1 | Yeni ADR: "RocketQuiz mağaza uygulaması". Kapsamı: 0008 §7.1 ve §7.5'i, 0009 §8.2'yi ve GDD 02'deki "seri dondurma yok" kuralını değiştirmek; ürün adını RocketQuiz yapmak (0022 açık nokta 2). Ekip oylaması (0010) | P0 | 👥 |
| A2 | Ad, alan adı ve mağaza adı taraması; TR/EU/US ticari marka başvurusu (R5 notu) | P0 | 👥 |
| A3 | Geliştirici hesapları: Google Play **kurumsal** hesap (kişisel hesapta üretime çıkmadan önce 12 test kullanıcısıyla 14 günlük kapalı test zorunlu), Apple Developer Program, D-U-N-S numarası | P0 | 👥 |
| A4 | KVKK + GDPR + CCPA: aydınlatma metni, gizlilik politikası URL'si, hesap silme akışı (iki mağazada da zorunlu), yurt dışına veri aktarımı için standart sözleşmeler (Firebase/AdMob/Supabase bölgesi) | P0 | 👥🌐 |
| A5 | 15 yaş düzenlemesinin arkadaş/lig özelliklerine etkisi; yaş derecelendirmesi (IARC, App Store) | P1 | 👥 |
| A6 | Varlık sahipliği ve gelir paylaşımı (ADR 0010'da K7 ertelenmiş). Reklam ve abonelik gelirinden önce kapanmalı | P0 | 👥 |

### B. İçerik hattı (Faz 0'dan itibaren sürekli)

| # | İş | Öncelik | Tür |
|---|---|---|---|
| B1 | **Acil:** 5 Kasım'dan sonraki 60 günlük Türkçe set (en az 2 aylık stok, her zaman ≥30 gün ileride) | P0 | 🌐👥 |
| B2 | İngilizce içerik hattı: ayrı EN günlük setler + kabul listeleri. ADR 0006 yalnız LLM'le içerik öngörüyor; mağaza kalitesi için en az bir anadili İngilizce editör. Öneri: önce EN, ES/DE sonra | P0 | 🌐👥 |
| B3 | İçerik aracı: LLM taslak → Wikidata → editör onayı → otomatik çakışma ve kapsam denetimi (0019'daki 600 kimliğe kadar). Haftada 35+ soru/dil hızına ölçeklenmeli | P1 | 🌐 |
| B4 | Oyuncu itirazı: "Bu cevap kabul edilmeliydi" bildirimi (0005 §4.6'da yok). Kabul listesi kalitesi yorum puanını doğrudan etkiler | P1 | ☁️🌐 |
| B5 | Tema haftaları / etkinlik takvimi (Cumhuriyet Bayramı, derbi haftası, Dünya Kupası, Noel). Mağaza etkinlikleri (G6) için hammadde | P2 | 👥🌐 |

### C. Native uygulama temeli (Faz 1, 1–6. hafta): mağazaya çıkabilir bir derleme

| # | İş | Öncelik | Tür |
|---|---|---|---|
| C1 | Player Settings: gerçek uygulama kimliği (`com.<stüdyo>.rocketquiz`), şirket adı, sürüm/yapı numarası şeması, **yalnız dikey**, Android hedef API 35+ (16 KB sayfa desteğini doğrula), IL2CPP ARM64, minify açık, Metal API doğrulaması kapalı | P0 | 🖥️ |
| C2 | İkon seti (Android adaptive dahil), açılış ekranı, iOS launch storyboard. Unity açılış logosu kapalı (Unity 6'da serbest) | P0 | 🖥️ |
| C3 | Derleme listesinden CoinRoller, SampleScene, RocketShowcase ve TutorialInfo'yu çıkar. Kullanılmayan paketleri kaldır: `ai.inference` + `SENTIS_ANALYTICS_ENABLED`, `ai.assistant` (ön sürüm), `com.unity.pipeline` (deneysel), visualscripting, unity-mcp (yalnız Editor'de tut). LitMotion sürümünü sabitle | P0 | 🖥️ |
| C4 | Ortam yapılandırması: platform başına üretim HTTPS adresi; `localhost` yalnız Editor'de (şu an varsayılan `http://localhost:3000`, NadideConfig.cs:9) | P0 | ☁️ |
| C5 | Kimlik: cihaz kimliğiyle misafir hesabı (çerez yerine token) + Sign in with Apple / Google ile bağlama; seri ve istatistik sunucuda. Native'de kapalı beta kodu ekranı ya da mağaza derlemesinde kapının kaldırılması (şu an "KAPALI BETA" ekranında kalınıyor) | P0 | ☁️🌐 |
| C6 | **Native cevap çubuğu:** soru açılınca ve ret sonrası klavye kendiliğinden açılsın; işletim sisteminin ek metin kutusunu gizle; `TouchScreenKeyboard.area` (iOS) / görünür alan (Android JNI) ile yerleşimi kaydır. 30 sn'lik oyunda her dokunuş puan demek | P0 | ☁️🖥️ |
| C7 | Yaşam döngüsü: `OnApplicationPause` ile geri dönüşte durumu yenile; gece yarısında Hangar'a dön; Android geri tuşu; ağ yok / yükleniyor göstergesi (şu an açılışta bir dakikadan uzun boş ekran olabiliyor) | P0 | ☁️ |
| C8 | Native paylaşım sayfası (eklenti) + deep link / universal link (şu an yalnız panoya kopyalıyor) | P0 | ☁️🖥️ |
| C9 | Hata ve analitik: Crashlytics (ya da Sentry) + olay analitiği (Firebase / GameAnalytics), KVKK onay akışıyla. Huni: kurulum → ilk soru → 5. soru → paylaşım → D1 | P0 | ☁️🖥️ |
| C10 | Performans: mobilde `targetFrameRate = 60`; zamanlayıcı metnini yalnız saniye değişince yenile (QuizUI.cs:144, GameScreens.cs:112); gökyüzü/bulut/galaksi gürültü oktavlarını kalite kademesine bağla; Hull gölge varyantlarını azalt. Hedef: orta-alt Android'de 60 fps, ANR %0,47'nin, çökme %1,09'un çok altında (Play'in resmi görünürlük cezası eşikleri) | P0 | ☁️ |
| C11 | iPad: ya yalnız iPhone (targetDevice) ya da CanvasScaler eşleşmesi 0,5 + yerleşim testi. Android tabletlerde API 36 yön kilidini yok sayıyor, test et | P1 | 🖥️ |
| C12 | Ses: `ignoreListenerPause` hatası (AudioDirector.cs:115), iOS sessiz anahtarı, ses ve müzik ayarı | P1 | ☁️ |
| C13 | Doğru haptik (`UIImpactFeedbackGenerator` / `VibrationEffect`) ve aç/kapa; şu an tek uzun `Handheld.Vibrate` | P1 | ☁️ |
| C14 | CI: game-ci Android AAB işi (keystore gizli anahtarlarla) + EditMode/PlayMode test işi; iOS için macOS runner ya da Unity Build Automation + fastlane. iOS modülünü yükle | P0 | ☁️🖥️ |
| C15 | Hata düzeltmeleri: `ShowResult` sunucu toplamını yerel toplamla eziyor (QuizGameController.cs:327); geri sayım UTC+3'e sabit (GameScreens.cs:404); `DateTime.TryParse` cihaz kültürüne bağlı (QuizGameController.cs:439); boş cevap listesinde "EN YAYGIN 3 CEVAP" başlığı; TMP atlasında `− → ✦ ×` glifleri | P1 | ☁️ |
| C16 | Uygulama içi Künye/Hakkında ekranı: CC BY 4.0 dokular (lisans şartı, şu an uygulamada yok), OFL fontlar, gizlilik politikası bağlantısı | P0 | ☁️🖥️ |

### D. Yerelleştirme ve global hazırlık (Faz 1–2)

| # | İş | Öncelik | Tür |
|---|---|---|---|
| D1 | Unity Localization paketi; `RqStops`, `GameScreens`, `QuizUI`, `NadideErrors` ve `BridgeMessages` metinlerini tablolara taşı. `NadideConfig.Lang` ve tr-TR kültür sabitlemesini kaldır | P0 | ☁️🖥️ |
| D2 | Dil başına sıfırlanma saati (TR İstanbul, EN New York …; ADR 0012) ve istemcide dil seçimi/algılama. Oyuncu hangi dilin havuzunda oynuyor, açıkça göster | P0 | ☁️🌐 |
| D3 | Font kapsamı: Unbounded / Space Grotesk / JetBrains Mono için Latin Extended (ES/DE); fallback atlasları | P1 | 🖥️ |
| D4 | Mağaza sayfası metinleri, ekran görüntüleri ve tanıtım videosu her dilde (G1–G3) | P1 | 👥 |

### E. Elde tutma ve meta (Faz 2, 4–10. hafta). D1/D7'yi taşıyan katman

| # | İş | Öncelik | Tür |
|---|---|---|---|
| E1 | **Bildirimler:** günlük hatırlatma (oyuncunun oynadığı saatte), akşam "serin tehlikede" ve sabah "dünün yolculuğu kesinleşti" (kesinleşti kartı ikinci açılış nedeni). Mobile Notifications (yerel) + FCM/APNs (uzak); izin, ilk günün sonunda istenmeli, ilk açılışta değil | P0 | ☁️🌐 |
| E2 | **Sunucuda seri:** cihazlar arası; **seri kurtarma** (ödüllü reklam ya da abone hakkı). Duolingo'nun en büyük elde tutma kazancı seri koruma bildiriminden geldi | P0 | ☁️🌐 |
| E3 | **Gezegen Atlası / Seyir Defteri:** her durağa ilk ulaşış, Ay tuzağına düşüş, Neptün sayısı; rozetler (UYUM'da planlı, Unity'de yok) | P1 | ☁️ |
| E4 | **Kozmetik roket kaplamaları ve iz renkleri:** roket kodla üretildiği için ucuz. Kilit açma yolları: seri günleri, rozetler, abonelik, ön kayıt ödülü. Puana etkisi yok | P1 | ☁️🖥️ |
| E5 | **Yıllık / aylık özet** ("2026 Yolculuğun"): paylaşılabilir. NYT "Year in Games" modeli | P2 | ☁️🌐 |
| E6 | FTUE: ilk günün ilk sorusu kolay (Merkür/Venüs garantili); 8 durak + tuzak, ilk cevap sinematiğinde sırayla öğretilsin; tanıtımda yalnız bir ekran. Hedef: açılıştan ilk cevaba ≤ 60 sn | P0 | ☁️🌐 |
| E7 | Mağaza içi değerlendirme isteği (Play In-App Review / `SKStoreReviewController`); yalnız iyi bir anda iste (Neptün'e ulaşma, 7 günlük seri) | P1 | ☁️ |
| E8 | Ayarlar ekranı: ses, haptik, hareketi azalt (işletim sistemi ayarından başlat), flaş, dil, bildirim saati, hesap bağlama/silme | P0 | ☁️🖥️ |

### F. Sosyal ve viral (Faz 2–3). Kurulum hızının motoru

| # | İş | Öncelik | Tür |
|---|---|---|---|
| F1 | **Görsel paylaşım kartı:** spoiler'sız rota (Güneş → durak emojileri, Ay tuzağı), gün numarası, deep link. WhatsApp ve Instagram Story'ye uygun boyutlar; `/kart` OG görselinin RocketQuiz kaplaması (P3) | P0 | ☁️🌐 |
| F2 | **Arkadaş ligi** (2–50 kişi, haftalık; Kelimelik Süper Lig modeli), davet bağlantısıyla katılma. ADR 0012'de planlı, M3'te login'e bağlı | P1 | ☁️🌐 |
| F3 | Global günlük sıralama (kesinleşmiş puanla) + ülke/şehir filtresi | P1 | ☁️🌐 |
| F4 | Davet / referans ödülü: davet edilen kişi ilk gününü bitirince iki tarafa kaplama | P2 | ☁️🌐 |
| F5 | Ertesi gün "Sadece sen" ve "En yaygın 3 cevap" anları (kesinleşti kartı) paylaşılabilir olsun | P1 | ☁️ |
| F6 | Play Games Services / Game Center: başarımlar ve lig tablosu (keşfedilebilirliğe katkı) | P2 | ☁️🖥️ |

### G. Ek modlar ve gelir (Faz 3, 8–14. hafta)

| # | İş | Öncelik | Tür |
|---|---|---|---|
| G1 | **Arşiv:** geçmiş günleri resmi puan olmadan oyna. Ücretsiz: günde 1 arşiv günü ödüllü reklamla; abonede sınırsız (NYT Wordle arşivi modeli) | P1 | ☁️🌐 |
| G2 | **Sınırsız pratik** (0015'te bayrak arkasında; Unity'ye taşınmalı) | P1 | ☁️🌐 |
| G3 | **Ödüllü reklam** (AdMob + arabuluculuk): yalnız oyuncu isteyince (seri kurtarma, ekstra arşiv günü, ipucu yok: adalet bozulur). ATT / UMP onay akışı; çocuk kitle bayrakları | P1 | ☁️🖥️ |
| G4 | **Abonelik "RocketQuiz Pro":** reklamsız, sınırsız arşiv ve pratik, ayda 1 seri kurtarma, özel kaplamalar, ayrıntılı istatistik. Aylık + yıllık; TL'de yerel fiyat. Pazar notu: haftalık abonelik pahalı algılanıyor; tek seferlik "reklamları kaldır" seçeneği de test edilmeli. Play Billing + StoreKit 2 (Unity IAP v5); sunucu tarafı makbuz doğrulama | P1 | ☁️🌐🖥️ |
| G5 | Eşzamanlı oda modu (arkadaşlarla canlı 5 soru; ADR 0012). Viral ama pahalı; lansman sonrası | P2 | ☁️🌐 |
| G6 | Sponsorlu tema paketleri (0009'da var) | P2 | 👥🌐 |

### H. Mağaza, ASO ve lansman (Faz 4–5, 12–24. hafta)

| # | İş | Öncelik | Tür |
|---|---|---|---|
| H1 | ASO: ad + alt başlık + anahtar kelimeler her dilde ("daily trivia", "quiz", "word game"; TR: "bilgi yarışması", "günlük bilgi oyunu", "kelime"). Apple'ın LLM etiketleri metadatadan ve ekran görüntülerinden üretiliyor; görüntülerdeki metin de anahtar kelime | P0 | 👥 |
| H2 | Ekran görüntüleri: sinematik ve nadirlik anı (Neptün varışı), "herkes aynı 5 soru", "nadir cevap = uzak gezegen". 15–30 sn tanıtım videosu. Custom Product Pages / Store Listing Experiments ile A/B | P0 | 👥🖥️ |
| H3 | Google Play **ön kayıt** (4–6 hafta, en çok 90 gün) + ön kayıt ödülü (özel kaplama) | P1 | 👥 |
| H4 | **Soft launch:** 2–3 pazar (ör. Türkiye + bir İngilizce pazar: Kanada/Avustralya/Filipinler), 4–6 hafta, metrik kapıları (§4). Kapıları geçmeden kullanıcı edinimine para harcama | P0 | 👥 |
| H5 | Apple Featuring Nomination (lansmandan 6–8 hafta önce) + Google Play Editors' Choice / Indie Corner başvurusu | P1 | 👥 |
| H6 | LiveOps takvimi: App Store In-App Events + Play Promotional Content (tema haftaları, B5); ayda en az 2 etkinlik, iki haftada bir güncelleme | P1 | 👥 |
| H7 | Yorumlara yanıt rutini; 1–2 yıldızlı yorumlardan haftalık hata listesi | P1 | 👥 |
| H8 | Web/PWA ile mağaza uygulaması arasında köprü: web oyuncusuna "uygulamayı indir, serin korunsun" (hesap bağlamayla seri taşınır) | P2 | ☁️🌐 |

### I. Erişilebilirlik ve cila (Faz 2–4, sürekli)

| # | İş | Öncelik | Tür |
|---|---|---|---|
| I1 | Unity 6 erişilebilirlik hiyerarşisi ile TalkBack / VoiceOver (soru, sayaç, durak kartı) | P1 | ☁️ |
| I2 | Yazı ölçeği (Dynamic Type / Android font scale), kontrast ≥4,5:1, 44 px dokunma hedefleri, renk körü dostu durak simgeleri | P1 | ☁️🖥️ |
| I3 | F4 sinematik + F6 atmosfer: yalnız ekran görüntüsü ve video için en güçlü 3 kare (Ay tuzağı, Satürn, Neptün) önce | P2 | 🖥️ |
| I4 | Denetleyici testleri (hata/yeniden deneme/devam), CI'da her PR'da | P1 | ☁️ |

## 4. Fazlar ve metrik kapıları

| Faz | Hafta | Çıktı | Geçiş kapısı |
|---|---|---|---|
| 0 Karar + acil | 0–1 | A1–A6, B1 başladı | ADR kabul edildi, hesaplar açıldı, ≥30 gün TR içerik |
| 1 Native temel | 1–6 | C1–C16, D1–D2, E6, E8, F1 | İç test (Play Internal + TestFlight): uçtan uca 5 soru, çökme yok |
| 2 Elde tutma | 4–10 | E1–E4, E7, F2–F3, F5, I1–I2 | Kapalı test (≥50 kişi, 14 gün): D1 ≥ %40, D7 ≥ %18 |
| 3 Gelir + modlar | 8–14 | G1–G4, B2 (EN içerik ≥60 gün) | Abonelik ve reklam akışı test pazarında çalışıyor |
| 4 Soft launch | 12–18 | H1–H4, H7 | **D1 ≥ %40, D7 ≥ %18, D30 ≥ %8, puan ≥ 4,5, çökmesiz oturum ≥ %99,5, ANR < %0,3, paylaşım oranı ≥ %10 (biten günlerin)** |
| 5 Global lansman | 18–24 | H5–H6, D3–D4, F4, F6, G5 | Haftalık kurulum hızı artıyor, D7 korunuyor |

Kapı değerleri günlük bulmaca oyunları için hedef değerlerdir, sektör ortalaması değildir.
Play'in resmi eşikleri: çökme %1,09, ANR %0,47 (aşılırsa görünürlük düşer).

Önerilen iş kolları (2–5 kişi):
- **İstemci (Unity):** C, D1, E, F1, G1–G4'ün istemci tarafı.
- **Sunucu (Nadide):** C5, D2, E1–E2 arka ucu, F2–F3, G4 makbuz doğrulama, B3 aracı.
- **İçerik:** B1, B2, B4, B5 (dil başına en az bir editör).
- **Büyüme/hukuk:** A, H.

## 5. Açık kararlar

1. A1 için ekip oylamasının sonucu. Özellikle reklam/analitik SDK'larına izin (0008 §7.5) ve ilk günden gelir (0009 §8.2).
2. Seri dondurma / kurtarma (GDD 02'deki "kaçan gün seriyi sıfırlar" kuralıyla çelişiyor).
3. Global lansmanda dil sırası: EN ile birlikte mi, yoksa önce TR+EN, sonra ES/DE mi? (Öneri: TR+EN, ES/DE soft launch verisinden sonra.)
4. Krillion'la aynı İngilizce pazarda ayrışma stratejisi: sıfırlanma saati, görsel kimlik, ek modlar.
5. Abonelik fiyatı ve tek seferlik "reklamları kaldır" seçeneği: soft launch'ta A/B.
6. Web (Nadide sitesi) mağaza uygulamasıyla birlikte yaşayacak mı, yoksa uygulamaya mı yönlendirecek? 31 MB WebGL indirmesi web FTUE'sini zorluyor.
