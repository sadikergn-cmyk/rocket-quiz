# RocketQuiz → Nadide (krillion-tr) entegrasyon planı

Durum: taslak, 2026-10-15. Tasarım oturumu bitince uygulamaya geçilecek.
Hedef repo: https://github.com/takatamakata/krillion-tr (özel, Next.js 16 + Supabase, Vercel).

## 1. Alınan kararlar

| # | Karar | Sonuç |
|---|---|---|
| K1 | Entegrasyon modeli | **Unity tüm oyunun yerine geçer.** Next.js yalnızca ince kabuk: `/beta` kapısı, Unity yükleyici sayfası, API, paylaşım kartı (`/kart`). |
| K1b | Görsel kimlik (2026-10-15) | **Nadide görüntüsü kalkar, RocketQuiz görüntüsü gelir.** Nadide'nin intro/menü *akışı* (logo intro, nasıl oynanır, Başla/Devam, günün durumu) korunur ama Unity içinde RocketQuiz tasarım sistemiyle yeniden kurulur (F3 "Hangar" ekranı). Kabukta kalan `/beta` ekranı ve yükleme ekranı da RocketQuiz token/fontlarına geçer. Nadide'nin Canvas gökyüzü, `tokens.css`, Figtree ve `open.tsx` görünümü emekliye ayrılır. |
| K2 | Kurallar | **Nadide kuralları.** 8 gezegen (0017) + Ay tuzak kademesi + "Güneş'te kaldı"; günde 5 soru, 30 sn, tek tahmin, "bunu mu demek istedin" (0018); puan sunucuda (0008); mesafe sayısı yok (0020). |
| K3 | Kimlik | **Mevcut yapı.** Misafir `nadide_device` çerezi + `/beta` kodu. Google/Apple girişi M3'te, ayrı iş. |
| K4 | Süreç | **Önce ADR, sonra kod.** ADR 0021 + bu plan küçük bir doküman PR'ı olarak gider, ekip oylar; onaydan sonra kod PR'ı. |

Değişmeyen kısıt: production yalnızca Dino'nun merge commit'iyle yayına çıkar (Vercel Hobby,
`docs/test-rehberi.md` §0). Bizim işimiz PR'ı merge'e hazır hale getirmek.

## 2. Hedef mimari

```
Tarayıcı (aynı origin: nadide-beta.vercel.app)
├─ Next.js kabuk  ─ /beta kapısı (RocketQuiz görünümü) → "/" = <UnityHost>
│                   └─ <UnityHost>: RocketQuiz yükleme ekranı + Unity loader
│                      (intro, menü, nasıl oynanır artık Unity içinde)
│                        ├─ cevap kutusu HTML (bugünkü window.RQ köprüsü React'e taşınır)
│                        └─ güvenli alan, görünürlük, paylaşım (Web Share) köprüleri
├─ Unity WebGL     ─ soru → cevap → roket sekansı → sonuç ekranı
│                   └─ UnityWebRequest → /api/play/start|next|state, /api/answer, /api/result/[date]
│                      (aynı origin: çerezler kendiliğinden gider, ayrı kimlik aktarımı yok)
└─ Next.js API     ─ puanlama, kademe, sayımlar (değişmez; cevap listesi istemciye inmez)
```

- Unity derlemesi (~27 MB) **git'e girmez** (repo 3 MB, LFS yok). Sürümlü zip olarak bir GitHub
  Release'e konur; Vercel derlemesinde `apps/web/scripts/fetch-unity-build` bunu
  `public/oyun/<sürüm>/` altına indirir. `UNITY_BUILD_VERSION` env değişkeni sürümü sabitler.
- Sıkıştırma: Vercel `Content-Encoding` gönderebildiği için Gzip/Brotli başlıkları `vercel.json`
  `headers` ile verilir, decompression fallback kapatılır (doğrulanacak).
- 0016'daki 40 KB ilk yükleme bütçesi **kabuk için korunur** (beta + yükleme ekranı); oyun paketi
  ayrı bütçe (hedef ≤30 MB, ADR 0021'de yazılır).
- Unity kaynak kodu `sadikergn-cmyk/rocket-quiz` reposunda kalır (LFS'li dokular, Unity araçları
  orada kurulu). Nadide reposuna yalnızca köprü, indirme script'i ve ADR girer.

## 3. Tasarım oturumuna iletilecek uyum listesi (acil)

Bunlar sürmekte olan tasarım işini doğrudan etkiliyor:

1. Kademeler: 5 kademe (uzayın sınırı, yörünge, Ay, Satürn, Andromeda) → **Merkür, Venüs, Dünya,
   Mars, Jüpiter, Satürn, Uranüs, Neptün** + **Ay tuzağı** + yanlışta **Güneş'te kaldı**.
   Andromeda çıkar. (Sınırsız mod 5 bölge kullanır, sonraya.)
2. **km / AU / ışık yılı sayısı gösterme** (0020 §4). Telemetri hız/süre/irtifa yerine durum,
   gezegen adı, puan göstersin. İçeride ölçek kullanmak serbest, ekranda sayı yok.
3. Okyanus/derinlik teması yok; Krillion'un kademe adı, paleti, paylaşım kartı kopyalanmaz (0007, 0020).
4. Görsel kimlik **RocketQuiz'in kendisi** (K1b): `RqTheme`, Unbounded / Space Grotesk / JetBrains
   Mono, ateşleme turuncusu kalır. Nadide token'larına geçilmez. Gezegen başına bir vurgu rengi
   tanımlamak faydalı (sonuç ekranı, paylaşım kartı).
5. Unity'ye yeni ekranlar: logo intro (ilk açılış), Hangar ana ekranı (Başla/Devam, günün
   durumu, seri), "Nasıl oynanır" sayfası, ertesi gün "final" açıklaması. Nadide'nin bu ekranlardaki
   içerik ve metinleri referans alınır, görünüm RocketQuiz olur.
6. Akış: 30 sn sayaç, tek tahmin, "bunu mu demek istedin" onayı, yanlışta süre içinde tekrar;
   3 yakıt hakkı (fuel pips) kalkar ya da süreye dönüşür.
7. Puan gün içinde geçici, gün sonu cron'u ile kesinleşir; ertesi gün "final" açıklaması var.
   Sonuç ekranı bu iki durumu göstermeli.
8. Metinler Türkçe, `ui/strings/tr.ts` ile aynı dil.
9. Hedef cihaz: orta segment Android'de 60 fps, reduced-motion desteği (0016).

## 4. İş paketleri

### P0. Doküman PR'ı (tasarımla paralel yapılabilir)
- [ ] `docs/decisions/0021-unity-oyun-istemcisi.md`: bağlam (0008/0016'dan sapma), karar (Unity
      oyun istemcisi + Next.js kabuk + sunucu puanı aynen + görsel kimliğin RocketQuiz'e geçmesi,
      GDD 05 marka bölümünün güncellenmesi), bütçeler, geri dönüş planı (eski
      `Game.tsx` bayrakla kalır), açık sorular.
- [ ] Bu planın Nadide repo'suna uyarlanmış özeti (`docs/` altında).
- [ ] Dal `sado/0021-unity-istemci`, PR açılır, ekip oylaması.

### P1. Unity: sunucu güdümlü oyun akışı
- [ ] `claude/admiring-thompson-s6qrbj` dalındaki `apps/unity` C# API istemcisini ve DTO'ları
      incele, `Assets/Scripts/RocketQuiz/Net/` altına uyarla (çerez kavanozu WebGL'de gereksiz).
- [ ] `QuizGameController`: yerel `questions.json` yerine `/api/play/start` → soru → `/api/answer`
      → sunucunun döndürdüğü kademe. Editor'de çevrimdışı test için sahte (mock) sağlayıcı.
- [ ] `RarityTiers`/`JourneyMap`: 8 gezegen + Ay + Güneş; yüzdeye göre kademe hesabı istemciden kalkar.
- [ ] 30 sn sayaç, tek tahmin, "bunu mu demek istedin", hata durumları (ağ, oturum süresi dolması).
- [ ] Build'den cevap verisini çıkar (Resources'taki `questions.json` yalnızca Editor mock'u).

### P2. Web kabuğu (Nadide `apps/web`)
- [ ] `ui/unity/UnityHost.tsx`: RocketQuiz yükleme ekranı, `window.RQ` köprüsü, cevap kutusu,
      güvenli alan, görünürlük; `/` rotasında `Game.tsx` yerine geçer (geri dönüş için bayrak kalır).
- [ ] `/beta` ekranını RocketQuiz görünümüne çevir (Unbounded/Space Grotesk, `RqTheme` renkleri).
- [ ] Pratik modları (`?mod=sinirsiz|arsiv`) ilk sürümde gizlenir; Unity'ye taşınınca açılır.
- [ ] `scripts/fetch-unity-build` + `vercel.json` başlıkları + `proxy.ts`'te `/oyun/*` davranışı.
- [ ] Unity şablonu (`WebGLTemplates/RocketQuiz`) sadeleşir; HTML arayüzü React'e taşınır.
- [ ] Paylaşım: Unity → jslib → Web Share API + mevcut `/kart/[gun]/[katmanlar]`.

### P3. Görsel uyum ve yeni ekranlar (tasarım oturumu çıktısına bağlı)
- [ ] 8 gezegen varış sahneleri, Ay tuzağı ve Güneş sahnesi, mesafe sayılarının kaldırılması.
- [ ] Logo intro, Hangar menüsü, "Nasıl oynanır", final açıklama ekranı (Unity, RocketQuiz stili).
- [ ] Paylaşım kartı `/kart` görselinin RocketQuiz stiline çevrilmesi.

### P4. Doğrulama
- [ ] Yerel: `npm run dev` (bellek repository'si) + `public/oyun/` içinde yerel Unity derlemesi,
      mobil görünümde uçtan uca 5 soruluk gün.
- [ ] Nadide testleri (`npm test`, `typecheck`, `layout:check`), Unity PlayMode testleri.
- [ ] Performans: indirme boyutu, orta segment Android fps, iOS Safari bellek.

### P5. Kod PR'ı ve yayın
- [ ] ADR onayından sonra kod PR'ı; Dino "Create a merge commit" ile birleştirir → production.
- [ ] Release zip'i ve `UNITY_BUILD_VERSION` Vercel ortamına Dino tarafından girilir.

## 5. Riskler

- **Ekip onayı:** 0008/0016'dan büyük sapma; ADR reddedilirse P1–P5 değişir.
- **Mobil performans:** Unity WebGL'in orta segment Android'de 60 fps ve iOS Safari bellek limiti
  kanıtlanmalı; ADR'ye ölçüm eklemek ikna için şart.
- **Önizleme engeli:** Son commit'i Dino'ya ait olmayan dallar Vercel önizlemesi almıyor; doğrulama
  yerelde yapılır. `Co-Authored-By` satırlı commit'lerin engellendiğine dair rapor var; bu repo
  için commit mesajında bu satır kullanılmayabilir (S3).
- **Unity CI lisansı:** `UNITY_LICENSE` yok; derleme yerelde alınıp Release'e yüklenir.
- **İki istemci:** Geçişte `Game.tsx` ve Unity yan yana yaşar; sunucu API'si ortak olduğu için
  risk düşük.

## 6. Kapanan sorular (2026-10-15, kullanıcı karar yetkisini devretti)

- S1 Unity kaynağı: **`rocket-quiz` reposunda kalır.** Unity araçları, LFS ve CLAUDE.md düzeni
  orada; Nadide'nin 3 MB'lık reposu şişmez. `claude/admiring-thompson-s6qrbj` dalındaki mobil
  prototip silinmez, C# API istemcisi referans alınır; ADR'de "yerini alır" diye belirtilir.
- S2 Release zip'leri: **`rocket-quiz` Releases (herkese açık).** Vercel token'sız indirir. Puan ve
  cevaplar sunucuda kaldığı için derlemede gizli veri yok; kaynak zaten herkese açık.
- S3 `Co-Authored-By`: **Kalır.** Production'ı tetikleyen commit Dino'nun merge commit'idir (yazar
  Dino, mesajında bu satır yok); PR içi commit'ler önizleme alamasa da yayını engellemez.
- S4 İlk sürüm: **Günlük oyun (5 soru) + ertesi gün final açıklaması.** Pratik modları gizli,
  ikinci sürümde Unity'ye taşınır.
