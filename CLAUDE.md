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
2. **unityMCP** (CoplayDev, `http://localhost:8080/mcp`): sahne, GameObject, prefab, script,
   konsol, ekran görüntüsü araçları. Sunucu Editor içinden başlar
   (`Window > MCP for Unity`). Bağlı değilse CLI yolunu kullan.
3. Editor kapalıysa: `unity open Game`, sonra `unity status --until-ready --project-path Game`.

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
