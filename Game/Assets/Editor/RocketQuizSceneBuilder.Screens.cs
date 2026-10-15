using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Full-screen and overlay screens (GameScreens) and the horizontal stop map (OrbitMap).
// Copy follows Nadide's tr.ts; layout and type follow RocketQuiz's mission-control system.
public static partial class RocketQuizSceneBuilder
{
    private static GameScreens BuildScreens(Transform safe, Transform root)
    {
        var go = new GameObject("Screens", typeof(RectTransform));
        go.transform.SetParent(safe, false);
        Stretch((RectTransform)go.transform);
        var screens = go.AddComponent<GameScreens>();
        Transform parent = go.transform;

        // ---- Intro: wordmark rising over the dark, once per device.
        CanvasGroup intro = CreateCard("Intro", root);
        Stretch((RectTransform)intro.transform);
        intro.GetComponent<PanelGraphic>().SetStyle(new Color(0.02f, 0.025f, 0.045f, 1f), new Color(0.04f, 0.05f, 0.09f, 1f), 0f, 0f, Color.clear, 0f, Color.clear);
        var mark = new GameObject("Mark", typeof(RectTransform));
        mark.transform.SetParent(intro.transform, false);
        CenterAt((RectTransform)mark.transform, Vector2.zero, new Vector2(1000f, 300f));
        TMP_Text word = CreateTmp("Wordmark", mark.transform, "ROCKETQUIZ", RocketQuizUiAssets.DisplayBlack, 104f, RqTheme.TextPrimary, TextAlignmentOptions.Center, null);
        CenterAt(word.rectTransform, new Vector2(0f, 30f), new Vector2(1000f, 140f));
        PanelGraphic line = CreatePanelGraphic("Line", mark.transform);
        line.SetStyle(RqTheme.Ignition, RqTheme.IgnitionDeep, 3f, 0f, Color.clear, 14f, new Color(1f, 0.46f, 0.12f, 0.5f));
        CenterAt(line.rectTransform, new Vector2(0f, -48f), new Vector2(420f, 6f));
        TMP_Text tag = CreateTmp("Tagline", mark.transform, "GÜNEŞ SİSTEMİ  ·  GÖREV KONTROL", RocketQuizUiAssets.Mono, 24f, RqTheme.Telemetry, TextAlignmentOptions.Center, null);
        tag.characterSpacing = 18f;
        CenterAt(tag.rectTransform, new Vector2(0f, -100f), new Vector2(1000f, 40f));

        // ---- Hangar: the open screen, rocket on the pad behind it.
        CanvasGroup hangar = CreateGroup("Hangar", parent);
        Stretch((RectTransform)hangar.transform);
        CanvasGroup hangarCard = CreateCard("Hangar Card", hangar.transform);
        Place((RectTransform)hangarCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(CardWidth, 900f));
        Transform hc = hangarCard.transform;
        TMP_Text kicker = CreateTmp("Kicker", hc, "#1 · 15 EKİM · 5 SORU · 30 SANİYE", RocketQuizUiAssets.Mono, 24f, RqTheme.Telemetry, TextAlignmentOptions.TopLeft, null);
        kicker.characterSpacing = 6f;
        Place(kicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -46f), new Vector2(CardWidth - 96f, 34f));
        TMP_Text title = CreateTmp("Title", hc, "Herkesin aklına geleni değil, kimsenin aklına gelmeyeni bul.", RocketQuizUiAssets.Display, 50f, RqTheme.TextPrimary, TextAlignmentOptions.TopLeft, null);
        title.lineSpacing = -6f;
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -104f), new Vector2(CardWidth - 96f, 250f));
        OrbitMap hangarMap = CreateOrbitMap("Tutorial Map", hc, CardWidth - 48f, true, false, false);
        Place((RectTransform)hangarMap.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(CardWidth - 48f, 200f));
        TMP_Text explainer = CreateTmp("Explainer", hc, "Cevabın ne kadar nadirse Güneş'ten o kadar uzağa gidersin. Herkesin uzak sandığı cevap seni Ay'da bırakır.", RocketQuizUiAssets.Body, 32f, RqTheme.TextSecondary, TextAlignmentOptions.TopLeft, null);
        explainer.lineSpacing = 6f;
        Place(explainer.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -650f), new Vector2(CardWidth - 96f, 150f));
        TMP_Text rule = CreateTmp("Rule", hc, "Yanlışsa süre bitene kadar yeniden dene. Puanın gün sonunda kesinleşir.", RocketQuizUiAssets.Body, 26f, RqTheme.TextMuted, TextAlignmentOptions.TopLeft, null);
        Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -800f), new Vector2(CardWidth - 96f, 80f));

        Button start = CreateButton("Start Button", hangar.transform, "BAŞLA", RqTheme.Ignition, RqTheme.IgnitionDeep, true, out TMP_Text startLabel);
        Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(CardWidth, 150f));
        startLabel.fontSize = 48f;
        TMP_Text resume = CreateTmp("Resume", hangar.transform, string.Empty, RocketQuizUiAssets.Mono, 26f, RqTheme.TextPrimary, TextAlignmentOptions.Bottom, RocketQuizUiAssets.MonoShadow);
        Place(resume.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 320f), new Vector2(CardWidth, 40f));
        TMP_Text footer = CreateTmp("Footer", hangar.transform, "GÜNDE BİR KEZ  ·  GECE YARISI YENİLENİR", RocketQuizUiAssets.Mono, 22f, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Bottom, RocketQuizUiAssets.MonoShadow);
        footer.characterSpacing = 10f;
        Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(CardWidth, 36f));

        // ---- Suggestion overlay: "Bunu mu demek istediniz?" (0018).
        CanvasGroup suggestion = CreateGroup("Suggestion", parent);
        Stretch((RectTransform)suggestion.transform);
        Image dim = CreateFullscreenImage("Dim", suggestion.transform, new Color(0.01f, 0.015f, 0.03f, 0.6f));
        dim.raycastTarget = true;
        CanvasGroup sCard = CreateCard("Suggestion Card", suggestion.transform);
        CenterAt((RectTransform)sCard.transform, new Vector2(0f, 120f), new Vector2(CardWidth, 560f));
        Transform sc = sCard.transform;
        TMP_Text sTitle = CreateTmp("Title", sc, "Bunu mu demek istediniz?", RocketQuizUiAssets.Display, 40f, RqTheme.TextPrimary, TextAlignmentOptions.Top, null);
        Place(sTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(CardWidth - 96f, 60f));
        TMP_Text sValue = CreateTmp("Value", sc, "Kahramanmaraş", RocketQuizUiAssets.DisplayBlack, 66f, RqTheme.Ignition, TextAlignmentOptions.Top, null);
        sValue.enableAutoSizing = true;
        sValue.fontSizeMin = 40f;
        sValue.fontSizeMax = 66f;
        Place(sValue.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(CardWidth - 96f, 96f));
        TMP_Text sTyped = CreateTmp("Typed", sc, "Yazdığın: ", RocketQuizUiAssets.Body, 30f, RqTheme.TextSecondary, TextAlignmentOptions.Top, null);
        Place(sTyped.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(CardWidth - 96f, 44f));
        TMP_Text sNote = CreateTmp("Note", sc, "Sayaç durdu · hakkın gitmez", RocketQuizUiAssets.Mono, 22f, RqTheme.Telemetry, TextAlignmentOptions.Top, null);
        sNote.characterSpacing = 8f;
        Place(sNote.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -296f), new Vector2(CardWidth - 96f, 36f));
        float half = (CardWidth - 96f - 24f) * 0.5f;
        Button no = CreateButton("No", sc, "HAYIR", new Color(0.14f, 0.18f, 0.27f), new Color(0.09f, 0.12f, 0.19f), false, out TMP_Text noLabel);
        Place((RectTransform)no.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 48f), new Vector2(half, 130f));
        Button yes = CreateButton("Yes", sc, "EVET", RqTheme.Ignition, RqTheme.IgnitionDeep, true, out TMP_Text yesLabel);
        Place((RectTransform)yes.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 48f), new Vector2(half, 130f));

        // ---- Moment card: the stop the answer reached.
        CanvasGroup moment = CreateCard("Moment", parent);
        Place((RectTransform)moment.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(CardWidth, 840f));
        Transform mc = moment.transform;
        PanelGraphic accent = CreatePanelGraphic("Accent", mc);
        accent.SetStyle(RqTheme.Ignition, RqTheme.Ignition, 3f, 0f, Color.clear, 10f, new Color(1f, 1f, 1f, 0.2f));
        Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(CardWidth - 96f, 6f));
        TMP_Text mKicker = CreateTmp("Kicker", mc, "5. DURAK · BÜYÜK KIRMIZI LEKE", RocketQuizUiAssets.Mono, 26f, RqTheme.Telemetry, TextAlignmentOptions.TopLeft, null);
        mKicker.characterSpacing = 6f;
        Place(mKicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -64f), new Vector2(CardWidth - 96f, 36f));
        TMP_Text mName = CreateTmp("Name", mc, "JÜPİTER", RocketQuizUiAssets.DisplayBlack, 104f, Color.white, TextAlignmentOptions.TopLeft, null);
        mName.enableAutoSizing = true;
        mName.fontSizeMin = 56f;
        mName.fontSizeMax = 104f;
        Place(mName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -110f), new Vector2(CardWidth - 96f, 130f));
        TMP_Text mLine = CreateTmp("Line", mc, "Dev gezegen. Bunu az kişi yazdı.", RocketQuizUiAssets.Body, 36f, RqTheme.TextPrimary, TextAlignmentOptions.TopLeft, null);
        Place(mLine.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -258f), new Vector2(CardWidth - 96f, 100f));
        TMP_Text mAnswer = CreateTmp("Answer", mc, "Cevabın · Vaduz", RocketQuizUiAssets.Body, 30f, RqTheme.TextSecondary, TextAlignmentOptions.TopLeft, null);
        Place(mAnswer.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -370f), new Vector2(CardWidth - 96f, 44f));
        TMP_Text mPoints = CreateTmp("Points", mc, "+40", RocketQuizUiAssets.Mono, 120f, RqTheme.Score, TextAlignmentOptions.TopLeft, null);
        Place(mPoints.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -420f), new Vector2(CardWidth - 96f, 140f));
        TMP_Text mStatus = CreateTmp("Status", mc, "TAHMİNİ · GÜN SONUNDA KESİNLEŞİR", RocketQuizUiAssets.Mono, 22f, RqTheme.TextMuted, TextAlignmentOptions.TopLeft, null);
        mStatus.characterSpacing = 6f;
        Place(mStatus.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -570f), new Vector2(CardWidth - 96f, 70f));
        Button mButton = CreateButton("Continue", mc, "DEVAM", new Color(0.30f, 0.62f, 1f), new Color(0.14f, 0.36f, 0.86f), false, out TMP_Text mButtonLabel);
        Place((RectTransform)mButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(CardWidth - 96f, 132f));

        // ---- Result: the day's journey.
        CanvasGroup result = CreateCard("Result", parent);
        Stretch((RectTransform)result.transform);
        result.GetComponent<PanelGraphic>().SetStyle(new Color(0.02f, 0.03f, 0.06f, 0.94f), new Color(0.03f, 0.05f, 0.1f, 0.97f), 0f, 0f, Color.clear, 0f, Color.clear);
        Transform rc = result.transform;
        TMP_Text rHeader = CreateTmp("Header", rc, "#1 · 15 EKİM", RocketQuizUiAssets.Mono, 28f, RqTheme.Telemetry, TextAlignmentOptions.TopLeft, null);
        rHeader.characterSpacing = 12f;
        Place(rHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -60f), new Vector2(CardWidth, 40f));
        TMP_Text rTotal = CreateTmp("Total", rc, "0", RocketQuizUiAssets.Mono, 150f, RqTheme.Score, TextAlignmentOptions.TopLeft, null);
        Place(rTotal.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -110f), new Vector2(CardWidth, 170f));
        TMP_Text rStatus = CreateTmp("Status", rc, "TAHMİNİ · 00:00'DA KESİNLEŞİR", RocketQuizUiAssets.Mono, 24f, RqTheme.TextMuted, TextAlignmentOptions.TopLeft, null);
        rStatus.characterSpacing = 6f;
        Place(rStatus.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -290f), new Vector2(CardWidth, 36f));
        OrbitMap resultMap = CreateOrbitMap("Day Map", rc, CardWidth, false, true, false);
        Place((RectTransform)resultMap.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(CardWidth, 200f));
        var rows = new TMP_Text[5];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = CreateTmp("Row " + (i + 1), rc, string.Empty, RocketQuizUiAssets.Body, 34f, RqTheme.TextPrimary, TextAlignmentOptions.TopLeft, null);
            rows[i].textWrappingMode = TextWrappingModes.NoWrap;
            rows[i].overflowMode = TextOverflowModes.Ellipsis;
            Place(rows[i].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -620f - 78f * i), new Vector2(CardWidth, 60f));
            PanelGraphic divider = CreatePanelGraphic("Divider " + (i + 1), rc);
            divider.SetStyle(RqTheme.Hairline, RqTheme.Hairline, 0f, 0f, Color.clear, 0f, Color.clear);
            Place(divider.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -682f - 78f * i), new Vector2(CardWidth, 2f));
        }
        TMP_Text rStreak = CreateTmp("Streak", rc, "SERİ 1 GÜN", RocketQuizUiAssets.Mono, 28f, RqTheme.TextPrimary, TextAlignmentOptions.TopLeft, null);
        rStreak.characterSpacing = 10f;
        Place(rStreak.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -1036f), new Vector2(CardWidth * 0.5f, 40f));
        TMP_Text rCountdown = CreateTmp("Countdown", rc, "YENİ YOLCULUK 00:00:00", RocketQuizUiAssets.Mono, 28f, RqTheme.Telemetry, TextAlignmentOptions.TopRight, null);
        rCountdown.characterSpacing = 4f;
        Place(rCountdown.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-RqTheme.Gutter, -1036f), new Vector2(CardWidth * 0.6f, 40f));
        Button share = CreateButton("Share", rc, "PAYLAŞ", RqTheme.Ignition, RqTheme.IgnitionDeep, true, out TMP_Text shareLabel);
        Place((RectTransform)share.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(CardWidth, 140f));
        TMP_Text shareNote = CreateTmp("Share Note", rc, "Cevaplar paylaşılmaz; yalnız duraklar ve toplam.", RocketQuizUiAssets.Body, 24f, RqTheme.TextMuted, TextAlignmentOptions.Bottom, null);
        Place(shareNote.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(CardWidth, 36f));

        // ---- Final reveal: yesterday's journey settled.
        CanvasGroup final = CreateCard("Final", parent);
        Stretch((RectTransform)final.transform);
        final.GetComponent<PanelGraphic>().SetStyle(new Color(0.02f, 0.03f, 0.06f, 0.96f), new Color(0.03f, 0.05f, 0.1f, 0.98f), 0f, 0f, Color.clear, 0f, Color.clear);
        Transform fc = final.transform;
        TMP_Text fKicker = CreateTmp("Kicker", fc, "DÜNÜN YOLCULUĞU KESİNLEŞTİ · #1", RocketQuizUiAssets.Mono, 26f, RqTheme.Telemetry, TextAlignmentOptions.TopLeft, null);
        fKicker.characterSpacing = 6f;
        Place(fKicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -60f), new Vector2(CardWidth, 40f));
        TMP_Text fTotal = CreateTmp("Total", fc, "0", RocketQuizUiAssets.Mono, 130f, RqTheme.Score, TextAlignmentOptions.TopLeft, null);
        Place(fTotal.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -110f), new Vector2(CardWidth, 150f));
        TMP_Text fNote = CreateTmp("Note", fc, "Kesin puanın.", RocketQuizUiAssets.Body, 30f, RqTheme.TextSecondary, TextAlignmentOptions.TopLeft, null);
        Place(fNote.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -270f), new Vector2(CardWidth, 44f));
        TMP_Text fBody = CreateTmp("Body", fc, string.Empty, RocketQuizUiAssets.Body, 32f, RqTheme.TextPrimary, TextAlignmentOptions.TopLeft, null);
        fBody.lineSpacing = 10f;
        Place(fBody.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -350f), new Vector2(CardWidth, 900f));
        Button fButton = CreateButton("Continue", fc, "BUGÜNÜN SORULARINA GEÇ", new Color(0.30f, 0.62f, 1f), new Color(0.14f, 0.36f, 0.86f), false, out TMP_Text fLabel);
        fLabel.fontSize = 34f;
        Place((RectTransform)fButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(CardWidth, 140f));

        SetReference(screens, "intro", intro);
        SetReference(screens, "introMark", mark.transform);
        SetReference(screens, "hangar", hangar);
        SetReference(screens, "hangarKicker", kicker);
        SetReference(screens, "hangarMap", hangarMap);
        SetReference(screens, "hangarResume", resume);
        SetReference(screens, "startButton", start);
        SetReference(screens, "startLabel", startLabel);
        SetReference(screens, "suggestion", suggestion);
        SetReference(screens, "suggestionValue", sValue);
        SetReference(screens, "suggestionTyped", sTyped);
        SetReference(screens, "suggestionNote", sNote);
        SetReference(screens, "suggestionNo", no);
        SetReference(screens, "suggestionYes", yes);
        SetReference(screens, "moment", moment);
        SetReference(screens, "momentAccent", accent);
        SetReference(screens, "momentKicker", mKicker);
        SetReference(screens, "momentName", mName);
        SetReference(screens, "momentLine", mLine);
        SetReference(screens, "momentAnswer", mAnswer);
        SetReference(screens, "momentPoints", mPoints);
        SetReference(screens, "momentStatus", mStatus);
        SetReference(screens, "momentButton", mButton);
        SetReference(screens, "momentButtonLabel", mButtonLabel);
        SetReference(screens, "result", result);
        SetReference(screens, "resultHeader", rHeader);
        SetReference(screens, "resultMap", resultMap);
        SetReference(screens, "resultTotal", rTotal);
        SetReference(screens, "resultStatus", rStatus);
        SetReferenceArray(screens, "resultRows", rows);
        SetReference(screens, "resultStreak", rStreak);
        SetReference(screens, "resultCountdown", rCountdown);
        SetReference(screens, "shareButton", share);
        SetReference(screens, "shareLabel", shareLabel);
        SetReference(screens, "final", final);
        SetReference(screens, "finalKicker", fKicker);
        SetReference(screens, "finalTotal", fTotal);
        SetReference(screens, "finalNote", fNote);
        SetReference(screens, "finalBody", fBody);
        SetReference(screens, "finalButton", fButton);
        return screens;
    }

    // Horizontal stop map: Sun at the left end of the track, eight stops outward, Moon beside Earth.
    private static OrbitMap CreateOrbitMap(string name, Transform parent, float width, bool showPoints, bool dayMarks, bool marker)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var map = go.AddComponent<OrbitMap>();
        float trackWidth = width - 120f;

        PanelGraphic track = CreatePanelGraphic("Track", go.transform);
        track.SetStyle(RqTheme.Hairline, RqTheme.Hairline, 1f, 0f, Color.clear, 0f, Color.clear);
        CenterAt(track.rectTransform, new Vector2(0f, 10f), new Vector2(trackWidth, 2f));

        PanelGraphic trail = CreatePanelGraphic("Trail", track.transform);
        Place(trail.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 6f));
        trail.rectTransform.pivot = new Vector2(0f, 0.5f);
        trail.rectTransform.anchoredPosition = Vector2.zero;

        PanelGraphic sun = CreatePanelGraphic("Sun", track.transform);
        Color sunColor = RqStops.Color(StopId.Sun);
        sun.SetStyle(new Color(1f, 0.85f, 0.5f), sunColor, 999f, 0f, Color.clear, 22f, new Color(sunColor.r, sunColor.g, sunColor.b, 0.55f));
        CenterAt(sun.rectTransform, MapPoint(StopId.Sun, trackWidth), new Vector2(46f, 46f));

        var dots = new PanelGraphic[8];
        var labels = new TMP_Text[8];
        for (int i = 0; i < 8; i++)
        {
            StopId stop = (StopId)(i + 1);
            float size = stop == StopId.Jupiter || stop == StopId.Saturn ? 24f : 18f;
            dots[i] = CreatePanelGraphic(RqStops.ShortName(stop), track.transform);
            CenterAt(dots[i].rectTransform, MapPoint(stop, trackWidth), new Vector2(size, size));
            string text = showPoints
                ? RqStops.ShortName(stop) + "\n<size=125%><color=#F5F7FF>" + RqStops.LadderPoints(stop) + "</color></size>"
                : RqStops.ShortName(stop);
            labels[i] = CreateTmp("Label " + (i + 1), track.transform, text, RocketQuizUiAssets.Mono, 19f, RqTheme.TextMuted, TextAlignmentOptions.Top, null);
            labels[i].textWrappingMode = TextWrappingModes.NoWrap;
            CenterAt(labels[i].rectTransform, MapPoint(stop, trackWidth) + new Vector2(0f, -50f), new Vector2(130f, 70f));
        }

        PanelGraphic moon = CreatePanelGraphic("Moon", track.transform);
        CenterAt(moon.rectTransform, MapPoint(StopId.Moon, trackWidth), new Vector2(14f, 14f));
        TMP_Text moonLabel = CreateTmp("Moon Label", track.transform, showPoints ? "Ay · tuzak" : "Ay", RocketQuizUiAssets.Mono, 18f, RqTheme.TextMuted, TextAlignmentOptions.Bottom, null);
        moonLabel.textWrappingMode = TextWrappingModes.NoWrap;
        CenterAt(moonLabel.rectTransform, MapPoint(StopId.Moon, trackWidth) + new Vector2(0f, 26f), new Vector2(150f, 30f));

        PanelGraphic ring = null;
        if (marker)
        {
            ring = CreatePanelGraphic("Marker", track.transform);
            CenterAt(ring.rectTransform, MapPoint(StopId.Earth, trackWidth), new Vector2(42f, 42f));
        }

        PanelGraphic[] marks = null;
        if (dayMarks)
        {
            marks = new PanelGraphic[5];
            for (int i = 0; i < marks.Length; i++)
            {
                marks[i] = CreatePanelGraphic("Day Mark " + (i + 1), track.transform);
                marks[i].SetStyle(Color.white, Color.white, 999f, 0f, Color.clear, 6f, new Color(1f, 1f, 1f, 0.3f));
                CenterAt(marks[i].rectTransform, MapPoint(StopId.Earth, trackWidth), new Vector2(12f, 12f));
            }
        }

        SetReference(map, "track", track.rectTransform);
        SetReference(map, "trail", trail);
        SetReference(map, "sun", sun);
        SetReferenceArray(map, "dots", dots);
        SetReferenceArray(map, "labels", labels);
        SetReference(map, "moonDot", moon);
        SetReference(map, "moonLabel", moonLabel);
        if (ring != null) SetReference(map, "marker", ring);
        if (marks != null) SetReferenceArray(map, "dayMarks", marks);
        return map;
    }

    private static Vector2 MapPoint(StopId stop, float trackWidth)
    {
        float x = -trackWidth * 0.5f + trackWidth * OrbitMap.Position(stop);
        return new Vector2(x, stop == StopId.Moon ? 30f : 0f);
    }

    private static void CenterAt(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
