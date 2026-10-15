# RocketQuiz

Tek soru, çok doğru cevap: oyuncu cevabı yazar, cevabı ne kadar az kişi verdiyse roket o kadar
uzağa gider (uzayın sınırı, yörünge, Ay, Satürn, Andromeda). Mobil, dikey ekran.

**Tarayıcıda oyna:** https://sadikergn-cmyk.github.io/rocket-quiz/

## Proje

- Unity 6000.3.25f1 (6.3 LTS), URP, yeni Input System. Unity projesi `Game/` klasöründe.
- Oyun sahnesi koddan üretilir: `Game/Assets/Editor/RocketQuizSceneBuilder*.cs`.
- Web derlemesi: `Game/Assets/Editor/WebGLBuilder.cs`, şablon `Game/Assets/WebGLTemplates/RocketQuiz/`.
- `main` dalına yapılan her push, GitHub Actions ile WebGL olarak derlenir ve GitHub Pages'e
  yayınlanır (`.github/workflows/webgl-pages.yml`, `gh-pages` dalı).

Geliştirme notları ve kurallar: [CLAUDE.md](CLAUDE.md). Atıflar: [CREDITS.md](CREDITS.md).
