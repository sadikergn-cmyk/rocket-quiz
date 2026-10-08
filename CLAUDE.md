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

## Ana oyun: RocketQuiz (mobil, dikey)

- Tasarım: tek soru, çok doğru cevap; oyuncu cevabı yazar. Cevabı ne kadar az kişi verdiyse
  roket o kadar uzağa gider (5 kademe: uzayın sınırı, yörünge, Ay, Satürn, Andromeda).
  Hedef: üst seviye iOS/Android telefon, dikey ekran, fotogerçekçi sinematik stil.
- Sahne: `Game/Assets/Scenes/RocketQuiz.unity`. Koddan üretilir: `Tools > Build RocketQuiz
  Scene` veya `unity command eval 'return RocketQuizSceneBuilder.Build();'`
  (`Assets/Editor/RocketQuizSceneBuilder*.cs`, mesh'ler `RocketQuizMeshes.cs`). Sahne, UI,
  materyal ve mesh değişikliklerini orada yap, yeniden üret (`Assets/Generated/RocketQuiz/`).
- Script'ler `Assets/Scripts/RocketQuiz/`: `QuizGameController` (akış), `LaunchDirector` (ödül
  sekansı ve kamera), `SpaceJourney` + `JourneyMap` (roket sabit, dünya hareket eder; uzay 1
  birim = 6,371 km, ikinci kamera ile yığılır), `RocketVisual`, `JuiceDirector`, `AudioDirector`
  (sesler sentezlenir), `QuizUI`, `AnswerMatcher`.
- Shader'lar `Assets/Shaders/` (elle yazılmış HLSL; VFX Graph ve Shader Graph kullanma, mobil).
- Sorular: `Assets/Resources/RocketQuiz/questions.json`. Yüzdeler şimdilik örnek veridir.
  Kademe eşikleri `RarityTiers.FromPercent` içinde (%20 / %10 / %5 / %2).
- Dokular Solar System Scope, CC BY 4.0; yeni dış dosya eklersen `CREDITS.md`'yi güncelle.
- Ekran görüntüsünden önce `unity command eval 'return GameViewUtil.SetPortrait();'` ile Game
  penceresini 1080x1920 yap; `capture_game_view --width 540 --height 960` kullan.
- Testler: `Assets/Tests/PlayMode/RocketQuizTests.cs`.

## Eski örnek oyun: CoinRoller

- Sahne: `Game/Assets/Scenes/CoinRoller.unity`. Koddan üretilir: `Tools > Build CoinRoller
  Scene` veya `unity command eval 'return CoinRollerSceneBuilder.Build();'`
  (`Assets/Editor/CoinRollerSceneBuilder.cs`). Sahne düzenini orada değiştir, yeniden üret.
- Script'ler `Assets/Scripts/` (`Game` asmdef): PlayerController, Coin, GameManager, CameraFollow.
- Testler: `Assets/Tests/PlayMode/CoinRollerTests.cs`. Çalıştırma:
  `unity command run_tests --mode PlayMode --async_tests true`, sonra `test_status` yokla.
- `unity command simulate_key` W/A/S/D için çalışır ama Space'i iletmez. Tuş testinde
  `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Space))` kullan.
