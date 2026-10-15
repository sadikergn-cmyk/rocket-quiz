# RocketQuiz × Nadide: Kural Uyumu Tasarımı

Tarih: 2026-10-15. Kullanıcı onayı: Nadide kuralları geçerli; ad **RocketQuiz** kalır; cevap sonrası
sinematik **kısa ve atlanabilir**. Kaynak: `NADIDE_ENTEGRASYON_PLANI.md` §3 ve Nadide deposu
(kararlar 0017, 0018, 0020, 0014, 0016; `ui/strings/tr.ts`; `server/contracts.ts`).

Bu belge `GELISTIRME_PLANI.md`'deki şu kararların **yerine geçer**: 5 kademe (uzayın sınırı →
Andromeda), yakıt tankları + hedef bahsi, logaritmik puan, 6 soruluk tur, ekranda mesafe/irtifa.

## 1. Kurallar (Nadide, bağlayıcı)

| Durak | id | Puan | Pay (p̂) | Vurgu rengi (bizim) | Simge |
|---|---|---|---|---|---|
| 1 Merkür | `mercury` | 5 | ≥ %20 (ya da örnek cevap) | `#A89F94` taş grisi | 🟤 |
| 2 Venüs | `venus` | 10 | %10–20 | `#E8C27A` kükürt sarısı | 🟡 |
| 3 Dünya | `earth` | 15 | %5–10 | `#4FA3FF` okyanus mavisi | 🌍 |
| 4 Mars | `mars` | 25 | %2,5–5 | `#E2643B` pas kırmızısı | 🔴 |
| 5 Jüpiter | `jupiter` | 40 | %1–2,5 | `#D9A066` bant turuncusu | 🟠 |
| 6 Satürn | `saturn` | 55 | %0,5–1 | `#F0D58A` halka altını | 🪐 |
| 7 Uranüs | `uranus` | 75 | %0,2–0,5 | `#7FE0D8` buz camgöbeği | 🟢 |
| 8 Neptün | `neptune` | 100 | < %0,2 | `#4C6BFF` derin mavi | 🔵 |
| Tuzak Ay | `trap_moon` | min(15, merdiven) | uydu kuralı (sunucu) | `#C9CCD6` gümüş | 🌙 |
| Güneş'te kaldın | — | 0 | süre kabul edilen cevap olmadan doldu | `#FFB547` | ☀️ |

- Günde **5 soru**, herkese aynı, soru başına **30 sn** (sayaç sunucuda; 3 sn ağ payı).
- Yanlış cevap soruyu kapatmaz: "Listede yok, başka bir şey dene"; alan temizlenir, sayaç işler.
  En çok 20 deneme; aynı biçimi tekrar yazmak deneme harcamaz; denenenler çip olarak görünür.
- Yakın yazımda **"Bunu mu demek istediniz?"** (Yazdığın: …, Hayır / Evet). İlk öneride sayaç durur
  ("Sayaç durdu · hakkın gitmez"), sonrakilerde işler.
- Puan gün içinde **tahmini** ("TAHMİNİ · GÜN SONUNDA KESİNLEŞİR"), Europe/Istanbul 00:15'te
  kesinleşir; ertesi gün **"DÜNÜN YOLCULUĞU KESİNLEŞTİ"** kartı açılıştan önce gelir.
- **Ekranda mesafe sayısı yok** (m, km, AU, ışık yılı, irtifa). Puan, durak adı, durak sırası
  ("5. DURAK") ve kesinleşmiş günlerde cevap payı yüzdesi gösterilebilir.
- Kademeyi sunucu belirler; istemcideki yüzde → durak hesabı yalnızca Editor'deki sahte oturum içindir.
- Kopyalanmaz: okyanus/dalış/derinlik dili, Krillion katman adları ve puan dizileri.

## 2. Anlatı: "Güneş'ten uzaklık" ve rampadan kalkış

Nadide'nin metaforu: cevap ne kadar nadirse **Güneş'ten o kadar uzağa** gidilir. RocketQuiz bunu
görev kontrol diliyle anlatır:

1. **Kalkış (her cevapta, ~2 sn):** Rampadan kalkış, yörüngeye çıkış. İkinci kez görüldüğünde
   kısaltılır. Ekranda sayı yok: durum şeridi "KALKIŞ → YÖRÜNGE → TRANSFER".
2. **Yörünge haritası (0,8–3 sn):** Yatay güneş sistemi haritası (Güneş solda, 8 durak sağa,
   Dünya'nın yanında Ay). Parlak bir iz Güneş'ten dışa akar, geçtiği her yörünge kendi rengiyle
   yanar ve adı yazılır ("DEVAM EDİYOR"). İz cevabın durağında durur. **Ay tuzağı:** iz Mars'a doğru
   hızla uzanır, sonra kopup Dünya'nın yanındaki Ay'a geri çekilir.
3. **Varış (~1,5 sn):** Kesme; roketin (sondanın) durak gezegenine yaklaşma çekimi. Gezegen kadrajı
   doldurur.
4. **Durak kartı:** Üst satır ("5. DURAK · BÜYÜK KIRMIZI LEKE"), gezegen adı (vurgu renginde),
   durak cümlesi, "Cevabın · …", "+40", "TAHMİNİ · GÜN SONUNDA KESİNLEŞİR", "Devam" /
   son soruda "Yolculuğunu gör".

- **Dokununca atla:** Sekansın herhangi bir anında dokunmak doğrudan durak kartına geçer.
- **Hareketi azalt:** Kalkış ve harita atlanır; yalnız saydamlıkla açılan durak kartı.
- **Güneş'te kaldın:** Kalkış olmaz. Rampada geri sayım durur ("FIRLATMA İPTAL"), harita Güneş'te
  kalır, kart "SÜRE DOLDU · Süre doldu. Sıradaki soru."

## 3. Ekranlar (RocketQuiz görünümü, Nadide akışı)

| Ekran | İçerik |
|---|---|
| Logo intro (ilk açılış) | RocketQuiz kelime markası + roket silueti, 1,2 sn; sonraki açılışlarda atlanır. |
| Hangar (açılış) | Üst satır "#N · 15 EKİM · 5 SORU · 30 SANİYE"; başlık "Herkesin aklına geleni değil, kimsenin aklına gelmeyeni bul."; öğretici yörünge haritası (8 durak + puanlar + "Ay · tuzak"); açıklama satırı; **Başla** ya da **Devam** + "Kaldığın yer: soru 2 / 5"; alt not "Günde bir kez · gece yarısı yenilenir"; arka planda rampadaki roket. Bugün oynandıysa doğrudan sonuç. |
| Soru | "SORU 2 / 5", soru, "ör. …", 30 sn sayaç çubuğu (son 5 sn turuncu nabız, "Son 5 saniye"), "Denediklerin: …" çipleri, ret satırı, deneme uyarıları, alttaki cevap çubuğu (web'de HTML). Yakıt göstergesi kalkar. |
| Öneri | Kart üstünde katman: "Bunu mu demek istediniz?", öneri, "Yazdığın: …", Hayır / Evet, not. |
| Uçuş HUD'u | CANLI rozeti, "SORU 2 / 5", durum şeridi, mini durak merdiveni (sayısız). |
| Durak kartı | Bölüm 2.4. |
| Sonuç | "#N · 15 EKİM", günün haritası (5 ışın, her soru bir durak), puan sayımı, soru soru satırlar (cevap · durak · puan), "Sadece sen ×N", "TAHMİNİ · 00:00'DA KESİNLEŞİR", seri, "YENİ YOLCULUK" geri sayımı, **Paylaş**. |
| Kesinleşti (ertesi gün) | "DÜNÜN YOLCULUĞU KESİNLEŞTİ · #N", kesin puan + fark, değişen duraklar (iz ileri/geri), "Sadece sen · …", "EN YAYGIN 3 CEVAP · SORU n" (%'lerle), "Bugünün sorularına geç". |

Paylaşım metni (cevap yok): `RocketQuiz #42 · 230 ✦ (tahmini)` + 5 simge + adres.

## 4. Mimari

- `RqStops`: durak kimlikleri, adlar, puanlar, renkler, simgeler, üst satırlar, durak cümleleri.
- `IGameSession`: `Start`, `Next`, `Answer`, `Confirm` (Nadide `/api/play/*` ve `/api/answer`
  sözleşmesinin C# karşılığı). `MockGameSession` Editor'de ve çevrimdışıyken yerel
  `questions.json` ile çalışır; **`Net/` altındaki gerçek oturum diğer oturumun işi** (P1).
- `QuizGameController` yalnız arayüz üzerinden konuşur; kademe sunucudan gelir.
- Uzay dünyası: 8 gezegen + Güneş + Ay, her biri kendi "varış istasyonunda"; Andromeda/galaksi çıkar.

## 5. Bu turda yapılmayanlar

Gerçek ağ oturumu (P1), web kabuğunun React'e taşınması (P2), Atlas/rozetler, pratik modları,
paylaşım kartı görseli.
