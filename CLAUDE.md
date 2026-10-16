# Unity + Claude: prompt ile oyun geliştirme ortamı

Kullanıcı Unity'de yeni; oyunu yalnızca prompt yazarak geliştirmek istiyor. Editor'de elle
tıklama gerektiren adımları en aza indir, gerektiğinde tam olarak nereye tıklanacağını söyle.
Kullanıcıyla Türkçe konuş.

## Ortam

- Unity projesi: `Game/` (Unity **6000.3.25f1**, 6.3 LTS). Sürümü değiştirme.
- Render pipeline: **URP**. Built-in pipeline shader/ayarı kullanma.
- Girdi: **yeni Input System** (`UnityEngine.InputSystem`). Eski `Input.GetKey...` kullanma.
- Aynı projede hem 3D hem 2D sahne yapılabilir (2D paketleri kurulu).
- Unity CLI: `%LOCALAPPDATA%\Unity\bin\unity.exe` (`unity` PATH'te). Ayrıntı: `unity-cli` skill'i.

## Editor'ü sürme yolları

1. **Unity CLI + Pipeline paketi** (birincil, her zaman çalışır):
   - `unity status --project-path Game` — Editor bağlı mı (`ready`)?
   - `unity command --project-path Game` — Editor'ün sunduğu komutlar.
   - `unity command eval '<C#>' --project-path Game` — keyfi C# çalıştır.
   - `unity recompile --project-path Game` — derle, hataları raporla.
   - `unity test Game --mode EditMode` — testler (Editor kapalıyken batch modda).
2. **UnityMCP** (CoplayDev v10.3.0, `http://127.0.0.1:8080/mcp`): sahne, GameObject, prefab,
   script, konsol, kamera/ekran görüntüsü, test, `execute_code` araçları. Sunucu, Editor
   açılınca kendiliğinden başlar (`MCPForUnity.AutoStartOnLoad` açık; ilk bağlantı ~1 dk
   sürebilir). Başlamadıysa `Window > MCP for Unity > Start Server`; o da olmazsa CLI yolu.
3. Editor kapalıysa: `unity open Game`, sonra `unity status --until-ready --project-path Game`.

Pratik notlar:
- `unity command eval` çağrılarını **Bash** aracıyla yap (PowerShell 5.1 tırnakları bozar).
  Kod tek tırnak içinde, `return ...;` ile biter.
- "Main thread operation timed out" = Editor içe aktarma/derleme yapıyor; `unity status
  --until-ready` ile bekleyip yeniden dene.
- Ekran görüntüsü: `unity command capture_game_view --save_path Screenshots/x.png` dosyayı
  `Game/Assets/Screenshots/` altına yazar (gitignore'da). `Read` ile aç ve bak.
- Paket ekleme: `unity command package_add --identifier <ad> --confirm true`, sonra
  `package_status`. Kurulu ekstra paketler: Cinemachine 3.1.7, 2D paket seti. ProBuilder,
  DOTween vb. gerektiğinde eklenir.
- Konsoldaki `InfluenceWindowStyle.uss ... Unsupported selector format` hataları 2D Animation
  paketinden gelir, zararsızdır; yok say.
- Build modülleri kurulu: Windows (Mono + IL2CPP), WebGL, Android.

## Kurallar

- `*.unity`, `*.prefab`, `*.asset`, `*.meta` dosyalarını metin olarak düzenleme. Sahne ve
  prefab değişikliklerini Editor üzerinden (CLI `eval` veya MCP) yap.
- Dosya taşıma/yeniden adlandırma/silme işlemini Editor API'siyle (`AssetDatabase`) yap;
  `.meta` dosyası asset'iyle birlikte kalmalı, yoksa referanslar sessizce kopar.
- C# script'leri `Game/Assets/Scripts/` altına yaz. Dosya adı = MonoBehaviour sınıf adı.
- Inspector alanları için `[SerializeField] private`. `UnityEngine.Object` üzerinde `?.` ve
  `??` kullanma. Abone olunan event'lerden `OnDestroy`/`OnDisable` içinde çık.
- Her script değişikliğinden sonra derle ve konsolu oku; derleme hatası varken yeni işe geçme.
  (Derleme hatasıyla açılan Editor Safe Mode'a düşer ve bağlantı kopar.)
- Görsel sonucu doğrula: Play moduna gir, ekran görüntüsü al, konsolda hata olmadığını gör.
  Oyunun gerçekten ilerlediğini kontrol et (odaksız Editor 1. karede donabilir).
- Çalışan her adımdan sonra git commit at. Kullanıcı istemeden push yapma.
- Sahneleri küçük tut; büyük değişiklikleri küçük, doğrulanabilir adımlara böl.
- Görsel/ses için önce ücretsiz CC0 kaynaklar: Kenney.nl, Quaternius, Poly Haven. Mixamo ve
  Asset Store dosyalarını herkese açık depoya koyma. 3D model gerekiyorsa `blender` MCP.
- Unity API'sinden emin değilsen `context7` MCP veya `unity docs <konu>` ile doğrula.

## Ana oyun: RocketQuiz (mobil, dikey) — Nadide kurallarıyla

- **Kurallar Nadide'nin** (bağlayıcı, `docs/NADIDE_UYUM_TASARIMI.md`): günde 5 soru, soru başına
  30 sn, yanlış cevapta süre içinde yeniden deneme, "Bunu mu demek istediniz?" onayı, 8 gezegen
  durağı (Merkür 5 … Neptün 100) + Ay tuzağı + süre dolunca "Güneş'te kaldın" (0). Puan gün içinde
  tahmini, gece yarısı kesinleşir; ertesi gün "kesinleşti" kartı. **Ekranda mesafe sayısı yok**
  (km, AU, ışık yılı, irtifa). Ad ve görünüm RocketQuiz. Okyanus/derinlik/Krillion dili yok.
- Kademeyi sunucu belirler. İstemci yalnız `IGameSession` ile konuşur
  (`Assets/Scripts/RocketQuiz/Game/`): `MockGameSession` çevrimdışı/Editor/testler için yerel
  `questions.json`'dan oynar; gerçek Nadide oturumu `Assets/Scripts/RocketQuiz/Net/` altında
  (**"Unity oyunu GitHub senkronizasyonu" oturumunun alanı**, `QuizGameController.SessionFactory`
  ile takılır).
- Duraklar, renkler, metinler: `RqStops`. Durak haritası (Güneş solda): `OrbitMap`. Ekranlar
  (intro, Hangar, öneri, durak kartı, sonuç, kesinleşti): `GameScreens`. Soru kartı + HUD: `QuizUI`.
  Uçuş (kalkış → harita → varış istasyonu, dokununca atla, hareketi azalt): `LaunchDirector`.
  İstasyonlar ve gezegen konumları: `JourneyMap` (iç ölçek; ekranda gösterilmez).
- Sahne: `Game/Assets/Scenes/RocketQuiz.unity`. Koddan üretilir: `Tools > Build RocketQuiz
  Scene` veya `unity command eval 'return RocketQuizSceneBuilder.Build();'`
  (`Assets/Editor/RocketQuizSceneBuilder*.cs`; UI `.Gameplay.cs` + `.Screens.cs`, roket `.Rocket.cs`).
- Sorular: `Assets/Editor/RocketQuiz/questions.json` (yalnızca Editor'deki sahte oturum; build'e
  girmez. `example`, sahte oturum için `trap`). Canlı sunucu: `Tools > RocketQuiz > Use Nadide Server`.
  Yüzdeler örnek veridir; yalnız `MockGameSession` kullanır.
- Hata ayıklama: PlayerPrefs `rq.introSeen` (intro), `rq.finalDemo`=1 (örnek kesinleşti kartı),
  `rq.reducedMotion`=1.
- Dokular Solar System Scope, CC BY 4.0; yeni dış dosya eklersen `CREDITS.md`'yi güncelle.
- Ekran görüntüsünden önce `unity command eval 'return GameViewUtil.SetPortrait();'` ile Game
  penceresini 1080x1920 yap; `capture_game_view --width 540 --height 960` kullan.
- Testler: `Assets/Tests/PlayMode/RocketQuizTests.cs`.

## RocketQuiz arayüzü (tasarım sistemi)

- Yol haritası: `docs/GELISTIRME_PLANI.md` (fazlar F0–F10, kararlar, bütçeler). Yeni işe başlamadan oku.
- uGUI + TextMeshPro. Eski `UnityEngine.UI.Text` kullanma. Renk/süre token'ları `RqTheme`.
- Fontlar: Unbounded (başlık), Space Grotesk (gövde, dinamik), JetBrains Mono (telemetri).
  TMP asset'leri `RocketQuizUiAssets.Ensure()` üretir (`Assets/Generated/RocketQuiz/UI/`); var olanı
  yeniden üretmez, değiştirmek için asset'i AssetDatabase ile silip sahneyi yeniden üret.
- Paneller/butonlar: `PanelGraphic` + `RocketQuiz/UI Panel` shader'ı (SDF köşe, çerçeve, parıltı).
  Canvas'ta TexCoord1-3 kanalları açık olmalı (builder açıyor).
- Animasyon: LitMotion (`LMotion.Create(...).Bind(...).AddTo(this)`), UI'da
  `MotionScheduler.UpdateIgnoreTimeScale`.
- Web'de cevap kutusu HTML'dir (`WebBridge` + `Plugins/WebGL/RocketQuizWeb.jslib` + şablondaki
  `window.RQ`). Unity içindeki cevap çubuğu yalnızca Editor/uygulamada görünür. Güvenli alan web'de
  şablondan `OnWebSafeArea` ile gelir (`SafeAreaFitter`).
- Köprü sözleşmesi v1 (Nadide Next.js kabuğu da aynısını uygular; değiştirme): Unity → sayfa `ready`,
  `showAnswer`, `hideAnswer`, `clearAnswer`, `share(json)`, `announce(metin)`, `betaRequired()`; sayfa →
  Unity `OnWebAnswer`, `OnWebSafeArea`, `OnWebVisibility`, `OnWebReducedMotion("1"|"0")`,
  `OnWebShareResult(...)`. Yük ve metinler: `Game/BridgeMessages.cs`.
- Mobil web testi: tarayıcı panelinde mobil görünüm; `type` eylemi Unity'ye tuş göndermez, `key` kullan.

## Roket (görsel)

- Tasarım kararları: `docs/ROKET_VE_RAMPA_TASARIMI.md`.
- Roket kodla üretilir: `Assets/Editor/RocketQuizSceneBuilder.Rocket.cs` (geometri, malzemeler,
  livery). Dokular `RocketQuizRocketTextures` (gürültü dokuları önbellekli; livery her derlemede
  yeniden çizilir) → `Assets/Generated/RocketQuiz/Rocket/`.
- Shader `RocketQuiz/Hull`: GGX + HDRI ışığı (`EnvironmentLighting` global `_EnvCube`), URP güneş
  gölgesi, nesne uzayında prosedürel dikiş/buz/kurum/livery. Modlar: 0 gövde, 1 düz, 2 karbon,
  3 motor çanı. Livery önü `FrontDegrees` (-56°, varsayılan fırlatma kamerasına bakar).
- Bakış geliştirme sahnesi: `unity command eval 'return RocketQuizSceneBuilder.BuildShowcase();'`,
  sonra `...CaptureShowcase(720, 1280);` → `Assets/Screenshots/rocket_*.png`. Build'e girmez.
- HDRI: Poly Haven `blouberg_sunrise_2` (CC0), küp harita 256, specular konvolüsyon.

## GitHub ve web sürümü

- Depo: https://github.com/sadikergn-cmyk/rocket-quiz (herkese açık). Oyun:
  https://sadikergn-cmyk.github.io/rocket-quiz/
- `main`'e push → `.github/workflows/webgl-pages.yml` WebGL derler (game-ci, Docker) ve `gh-pages` dalına
  yayınlar. Sadece `Game/Assets`, `Game/Packages`, `Game/ProjectSettings` değişince çalışır;
  elle: `gh workflow run "WebGL to Pages"`. Durum: `gh run list`, hata: `gh run view --log-failed`.
- Derleme yöntemi `WebGLBuilder.Build` (`Assets/Editor/WebGLBuilder.cs`): yalnızca
  `RocketQuiz.unity`, Gzip + Decompression Fallback (Pages `Content-Encoding` gönderemez),
  şablon `Assets/WebGLTemplates/RocketQuiz/`. Yerelde: `Tools > Build RocketQuiz WebGL`, sonra
  `python tools/serve_webgl.py` (http://localhost:8090).
- Nadide için: derleme `Build/` yanına `rq-build.json` yazar (sürüm + dosya yolları). Paket:
  `powershell -ExecutionPolicy Bypass -File tools\package_webgl.ps1` → `rocketquiz-webgl-<sürüm>.zip` ve
  ekrana yazdığı `gh release create ...` komutu (elle çalıştırılır).
- CI sahneyi yeniden üretmez; derlenen şey depodaki `RocketQuiz.unity`'dir. Builder kodunu
  değiştirdiysen sahneyi Editor'de yeniden üretip onu da commit'le.
- **Bulut oturumu (Unity Editor yok):** `unity` CLI ve MCP çalışmaz. Yalnızca C#/shader/JSON
  düzenle, `*.unity`/`*.mat`/`*.asset` dosyalarına dokunma. Derleme kontrolü = CI çalışması;
  push'tan sonra `gh run watch` ile bekle, hata varsa logdan düzelt. Görsel doğrulamayı Pages
  linkinde yap. Sahne/materyal gerektiren işleri yerel oturuma bırak.
- WebGL'de URP Lit görünmüyor; materyaller `RocketQuiz/Surface` shader'ını kullanır
  (`Assets/Shaders/Surface.shader`).
- Secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` (kullanıcı ekler). Bu makinede
  `.ulf` yok (yalnızca `%LOCALAPPDATA%\Unity\licenses\UnityEntitlementLicense.xml`); CI lisansı
  alamazsa yedek yol: yerelde derle, `bash tools/publish_webgl.sh` ile `gh-pages`'e gönder.

## Eski örnek oyun: CoinRoller

- Sahne: `Game/Assets/Scenes/CoinRoller.unity`. Koddan üretilir: `Tools > Build CoinRoller
  Scene` veya `unity command eval 'return CoinRollerSceneBuilder.Build();'`
  (`Assets/Editor/CoinRollerSceneBuilder.cs`). Sahne düzenini orada değiştir, yeniden üret.
- Script'ler `Assets/Scripts/` (`Game` asmdef): PlayerController, Coin, GameManager, CameraFollow.
- Testler: `Assets/Tests/PlayMode/CoinRollerTests.cs`. Çalıştırma:
  `unity command run_tests --mode PlayMode --async_tests true`, sonra `test_status` yokla.
- `unity command simulate_key` W/A/S/D için çalışır ama Space'i iletmez. Tuş testinde
  `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Space))` kullan.
