# Roket ve Fırlatma Alanı: Tasarım Özeti

Tarih: 2026-10-15. Kullanıcıyla soru-cevapla varılan fikir birliği. Uygulama `GELISTIRME_PLANI.md`
fazları F4 (fırlatma sineması), F5 (rampa görselliği) ve F6 (atmosfer/uzay) içinde yapılır.

## 1. Teşhis: şu an neden yapay

- **Roket:** Tek parça döndürülmüş silindir. Panel, malzeme ayrımı ve ölçek ipucu yok (kaynak
  çizgisi, kablo kanalı, kurum, buz). Siluet tanıdık bir roket tipine oturmuyor.
- **Rampa:** Kule birkaç küp, zemin gürültü dokulu düz bir dörtgen. Kompleks hissi yok
  (alev kanalı, su kulesi, tanklar, yollar).
- **Ortam:** Gökyüzü iki renkli geçiş, bulutlar levha, gölge yok.

## 2. Kararlar

| Konu | Karar |
|---|---|
| Siluet | **A · Modern**: tek gövde, 2 kademe, 4 ızgara kanatçık, 4 katlanır iniş bacağı, 9 motorlu 1. kademe, tek motorlu 2. kademe, iki parçalı burun kapağı (fairing). |
| Kimlik | **Kendi markamız**: beyaz gövde, UI ile aynı ateşleme turuncusu vurgu şeridi, RocketQuiz görev arması, seri numarası. Gerçek kurum/şirket logosu yok. |
| Üretim | **Hazır ücretsiz model** (lisansı herkese açık depoya uygun), kendi boyamamızla. Gerekirse parçalar ayrılır/kitbash. |
| Gerçeklik detayları | Buzlanma + soğuk buhar (sıvı oksijen tankı), panel/perçin/kablo kanalı/uyarı etiketleri, hareketli parçalar (ızgara kanatçık açılması, motor gimbal, burun kapağı ayrılması). |
| Saat ve ışık | **Altın saat**: alçak güneş, uzun gölgeler, turuncu-mor gökyüzü, plümde sıcak arka ışık. |
| Konum | **Kıyı kompleksi**: deniz ufku, kumsal şeridi, sazlık; yükseldikçe kıyı çizgisi harita gibi okunur. |
| Kule | **Modern kafes kule**: çelik kafes, mürettebat erişim kolu, besleme kolları, alev kanalı ve saptırıcı, su püskürtme (deluge) kulesi, yıldırım direkleri, küresel yakıt tankları, servis binaları, yollar. |
| Kalkış efektleri | Ateşleme kıvılcımları (motorlar yanmadan hemen önce yanlara fışkıran kıvılcım sağanağı), su baskını + dev buhar bulutu, kolların geri çekilmesi ve kopan kablolar, ısı titreşimi + yere yayılan basınç halkası + sarsıntı. |
| Kamera | **Yayın kurgusu**: rampa kamerası → uzak takip teleobjektifi (ısı titreşimli) → roket üstü kamera (aşağıda küçülen Dünya) → kademe ayrılması. HUD'daki kamera etiketiyle eşleşir. |
| Dünya | Gerçek uydu görüntüsü (NASA Blue Marble, kamu malı), fiziksel atmosfer (saçılım, ince mavi ufuk, kıvrılan Dünya, gün-gece çizgisi), hacimli görünen bulutlar ve gölgeleri, rüzgârla kıvrılan egzoz izi ve bulutlara düşen gölgesi. |

## 3. Kalkış sekansı (rampa, ilk ~8 sn)

| Zaman | Olay | Görsel / ses |
|---|---|---|
| T-10 | Soğuk buhar | Gövdeden süzülen beyaz buhar, buz katmanı; rüzgâr sesi, tank tıkırtıları |
| T-5 | Besleme kolu çekilir | Kol yana döner, kablo sallanır; hidrolik sesi |
| T-3 (oyuncu basılı tutar) | Ateşleme kıvılcımları | Motor çevresinden yanlara kıvılcım sağanağı; çıtırtı |
| T-2 | Su baskını | Alev kanalına su; ilk buhar patlaması |
| T-0 | Motorlar yanar | Turuncu-beyaz plüm, basınç halkası, kamera sarsıntısı, ısı titreşimi, flaş sınırı içinde ışık |
| T+0.5 | Kenetler açılır, kalkış | Buhar bulutu yanlardan taşar, buz parçaları dökülür; "KALKIŞ" anonsu |
| T+3 | Kesme: takip teleobjektifi | Uzaktan kıyı ve kule silueti, ısı titreşimi |
| T+6 | Kesme: roket üstü kamera | Aşağıda küçülen rampa ve kıyı, egzoz izi |

## 4. Teknik notlar (mobil web bütçesine göre)

- **Işık:** Altın saat HDRI'si (Poly Haven, CC0) ortam ve yansıma için; güneş HDRI ile hizalı.
  Rampa yakınında tek kaskad gölge.
- **Roket materyali:** PBR (albedo/normal/ORM) + ayrı bir "decal/livery" katmanı (arma, şerit,
  yazılar) + buz maskesi (sıvı oksijen bölgesi, kalkıştan sonra azalır) + kurum maskesi.
- **Duman/buhar:** 6 yönlü ışıklı flipbook, yumuşak parçacık (derinlik dokusu), az sayıda büyük
  parçacık. Kıvılcımlar: uzatılmış (stretched) toplanır parçacıklar + kısa ışık.
- **Deniz:** Ucuz Gerstner benzeri dalga normali + gökyüzü yansıması + güneş parıltısı.
- **Dünya:** Editor'de önceden hesaplanmış atmosfer LUT'ları, 4K (web) / 8K (uygulama) doku,
  bulut katmanı için gölge ofseti, ince ufuk çizgisi.
- **Kamera:** Cinemachine 3 kameraları + Impulse ile sarsıntı; kesmeler LaunchDirector olaylarına
  bağlı.

## 5. Açık konular

- Lisansı uygun hazır model adayları araştırılıyor (bkz. bölüm 6). Uygun aday yoksa: hazır parça
  kitleri + kendi parçalarımızla birleştirme (kitbash).
- Gerçek marka/şirket görünümünden kaçınılır (SpaceX/NASA logoları, birebir Falcon 9 boyası).

## 6. Model adayları

Lisanslar Sketchfab API kayıtlarından doğrulandı (2026-10-15). Hepsi CC BY 4.0: herkese açık depoya
konabilir, `CREDITS.md`'ye yazar/başlık/URL/lisans + "değiştirildi" notu zorunlu. Hepsinde SpaceX
logoları var → kaldırılıp kendi boyamamız uygulanacak; oyunda "Falcon/SpaceX" adı geçmeyecek.

### Roket

| # | Model | Yazar | Üçgen | Not |
|---|---|---|---|---|
| 1 (öneri) | [SpaceX Falcon 9 V4](https://sketchfab.com/3d-models/33e974795a21416eaac38d536ae1ceb9) | clarence365 | 105k | 14 hareketli parça: açılır bacaklar, ızgara kanatçıklar, yönlenen motorlar. Animasyon kararlarımıza en uygun. |
| 2 | [Falcon 9 – SpaceX](https://sketchfab.com/3d-models/394f7cf52d124bbd9db69f24d1ff2f08) | Stanley_Creative | 158k | Görsel kalitesi en yüksek; parçaların ayrılabilirliği belirsiz. |
| 3 | [SpaceX Falcon 9 and Dragon 2](https://sketchfab.com/3d-models/f709fc945bb2413faf2878aa613cde3d) | ForestKatsch | 50k | Temiz ve hafif; burun kapağını (fairing) kendimiz yaparız. |

Web için hedef 40–60k üçgen (sadeleştirme). Kademeler, burun kapağı yarıları, 4 kanatçık ve 4 bacak
ayrı nesne olmalı.

### Fırlatma alanı (birleştirme / kitbash)

- Kule tabanı: [NASA Mobile Launcher](https://science.nasa.gov/3d-resources/mobile-launcher/) (kamu
  malı, çok hafif; dokularda NASA logosu varsa temizlenir) + [Falcon 9 Strongback](https://sketchfab.com/3d-models/854921c608784624ad2af042a24fded4)
  (captaintokra, CC BY, 12k).
- Kendi prosedürel kafes kulemiz ve yıldırım direklerimiz; Poly Haven (CC0) borular, tanklar
  (büyütülüp küresel tanka çevrilir), lambalar, çitler, bariyerler.
- Zemin: Poly Haven `concrete_floor_worn_001` + `burned_ground_01` (is lekeleri), `asphalt_floor`,
  `coast_sand_01`, `grass_ground`.
- HDRI (altın saat): `blouberg_sunrise_1/2` (kıyı), `grasslands_sunset`, `aarfontein_pan`.
- Kıvılcımlar: kendi parçacık sistemi + shader; duman için Kenney Smoke Particles (CC0) başlangıç.

### Kaçınılacaklar

CGTrader / TurboSquid / BlenderKit "Royalty Free" (herkese açık depoda dağıtım yasak), Sketchfab
CC BY-NC, atıfsız yeniden yüklemeler, yapay zekâ ile üretilmiş ağır ağlar.

### Gereken kullanıcı adımı

Sketchfab indirmesi için ücretsiz bir Sketchfab hesabıyla giriş gerekir (Claude hesap açamaz/giriş
yapamaz). Seçilen modelin "Download 3D Model" → **glTF (.glb)** sürümünü indirip
`Game/Assets/ThirdParty/Rocket/` klasörüne koymak yeterli; gerisini Claude yapar (Unity'de glTF içe
aktarma paketi, parça ayırma, sadeleştirme, boyama).
