using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Gameplay and UI wiring for the RocketQuiz scene (portrait, 1080x1920 reference).
// UI follows the "mission control" design system: RqTheme tokens, PanelGraphic surfaces, TMP type.
// Flow and copy follow Nadide (docs/NADIDE_UYUM_TASARIMI.md): no distances on screen.
public static partial class RocketQuizSceneBuilder
{
    private const float CardWidth = 1080f - 2f * RqTheme.Gutter;

    private static void BuildGameplay(RocketVisual rocket, LaunchCameraRig rig, SpaceJourney journey, Camera nearCamera, Light sun)
    {
        RocketQuizUiAssets.Ensure();

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();

        var bridge = new GameObject("WebBridge").AddComponent<WebBridge>();
        GameScreens screens;
        QuizUI ui = BuildUi(bridge, out screens);

        var gameGo = new GameObject("Game");
        var audioDirector = gameGo.AddComponent<AudioDirector>();
        var juice = gameGo.AddComponent<JuiceDirector>();
        SetReference(juice, "volume", Object.FindFirstObjectByType<Volume>());

        var director = gameGo.AddComponent<LaunchDirector>();
        SetReference(director, "journey", journey);
        SetReference(director, "rig", rig);
        SetReference(director, "rocket", rocket);
        SetReference(director, "juice", juice);
        SetReference(director, "audioDirector", audioDirector);
        SetReference(director, "ui", ui);

        var controller = gameGo.AddComponent<QuizGameController>();
        SetReference(controller, "ui", ui);
        SetReference(controller, "screens", screens);
        SetReference(controller, "director", director);
        SetReference(controller, "audioDirector", audioDirector);
        SetReference(controller, "rig", rig);
    }

    private static QuizUI BuildUi(WebBridge bridge, out GameScreens screens)
    {
        var canvasGo = new GameObject("UI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // PanelGraphic packs its shape and colours into the extra UV channels.
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;
        canvasGo.AddComponent<GraphicRaycaster>();
        Transform root = canvasGo.transform;

        Image veil = CreateFullscreenImage("Cloud Veil", root, new Color(1f, 1f, 1f, 0f));

        var safeGo = new GameObject("Safe Area", typeof(RectTransform));
        safeGo.transform.SetParent(root, false);
        Stretch((RectTransform)safeGo.transform);
        safeGo.AddComponent<SafeAreaFitter>();
        Transform safe = safeGo.transform;

        // --- Header: provisional score left, question count right.
        CanvasGroup header = CreateGroup("Header", safe);
        Stretch((RectTransform)header.transform);
        TMP_Text scoreLabel = CreateTmp("Score Label", header.transform, "PUAN · TAHMİNİ", RocketQuizUiAssets.Mono, 24f, RqTheme.Telemetry, TextAlignmentOptions.TopLeft, RocketQuizUiAssets.MonoShadow);
        scoreLabel.characterSpacing = 14f;
        Place(scoreLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -36f), new Vector2(500f, 34f));
        TMP_Text score = CreateTmp("Score", header.transform, "0", RocketQuizUiAssets.Mono, 54f, RqTheme.TextPrimary, TextAlignmentOptions.TopLeft, RocketQuizUiAssets.MonoShadow);
        Place(score.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -70f), new Vector2(520f, 70f));
        TMP_Text roundLabel = CreateTmp("Round Label", header.transform, "SORU", RocketQuizUiAssets.Mono, 24f, RqTheme.Telemetry, TextAlignmentOptions.TopRight, RocketQuizUiAssets.MonoShadow);
        roundLabel.characterSpacing = 18f;
        Place(roundLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-RqTheme.Gutter, -36f), new Vector2(400f, 34f));
        TMP_Text round = CreateTmp("Round", header.transform, "1/5", RocketQuizUiAssets.Mono, 54f, RqTheme.TextPrimary, TextAlignmentOptions.TopRight, RocketQuizUiAssets.MonoShadow);
        Place(round.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-RqTheme.Gutter, -70f), new Vector2(400f, 70f));

        // --- Question card (top half; the keyboard and answer bar own the bottom).
        CanvasGroup quizPanel = CreateCard("Quiz Panel", safe);
        Place((RectTransform)quizPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(CardWidth, 540f));
        Transform card = quizPanel.transform;

        TMP_Text questionLabel = CreateTmp("Question Label", card, "SORU 1 / 5", RocketQuizUiAssets.Mono, 26f, RqTheme.Telemetry, TextAlignmentOptions.TopLeft, null);
        questionLabel.characterSpacing = 10f;
        Place(questionLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -42f), new Vector2(500f, 36f));
        TMP_Text timerText = CreateTmp("Timer", card, "30", RocketQuizUiAssets.Mono, 30f, RqTheme.Telemetry, TextAlignmentOptions.TopRight, null);
        Place(timerText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -38f), new Vector2(240f, 40f));

        PanelGraphic timerTrack = CreatePanelGraphic("Timer Track", card);
        timerTrack.SetStyle(RqTheme.Hairline, RqTheme.Hairline, 3f, 0f, Color.clear, 0f, Color.clear);
        Place(timerTrack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(CardWidth - 96f, 6f));
        PanelGraphic timerFill = CreatePanelGraphic("Timer Fill", timerTrack.transform);
        timerFill.SetStyle(RqTheme.Telemetry, RqTheme.Telemetry, 3f, 0f, Color.clear, 8f, new Color(0.45f, 0.86f, 1f, 0.35f));
        Place(timerFill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(CardWidth - 96f, 6f));
        timerFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        timerFill.rectTransform.anchoredPosition = Vector2.zero;

        TMP_Text prompt = CreateTmp("Prompt", card, "Soru", RocketQuizUiAssets.Display, 54f, RqTheme.TextPrimary, TextAlignmentOptions.TopLeft, null);
        prompt.enableAutoSizing = true;
        prompt.fontSizeMin = 36f;
        prompt.fontSizeMax = 54f;
        prompt.lineSpacing = -8f;
        Place(prompt.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -128f), new Vector2(CardWidth - 96f, 190f));

        TMP_Text example = CreateTmp("Example", card, "ör. Mars", RocketQuizUiAssets.Body, 30f, RqTheme.TextMuted, TextAlignmentOptions.TopLeft, null);
        Place(example.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -326f), new Vector2(CardWidth - 96f, 40f));
        TMP_Text feedback = CreateTmp("Feedback", card, string.Empty, RocketQuizUiAssets.Body, 30f, RqTheme.TextSecondary, TextAlignmentOptions.TopLeft, null);
        Place(feedback.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -378f), new Vector2(CardWidth - 96f, 80f));
        TMP_Text attempts = CreateTmp("Attempts", card, string.Empty, RocketQuizUiAssets.Body, 26f, RqTheme.TextPrimary, TextAlignmentOptions.TopLeft, null);
        Place(attempts.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -446f), new Vector2(CardWidth - 96f, 80f));

        // --- Answer bar (thumb zone). Hidden on the web, where an HTML field takes its place.
        CanvasGroup answerBar = CreateGroup("Answer Bar", safe);
        Place((RectTransform)answerBar.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(CardWidth, 140f));
        TMP_InputField input = CreateInput("Answer Input", answerBar.transform);
        Place((RectTransform)input.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(CardWidth - 316f, 140f));
        Button submit = CreateButton("Submit Button", answerBar.transform, "FIRLAT", RqTheme.Ignition, RqTheme.IgnitionDeep, true, out TMP_Text submitLabel);
        Place((RectTransform)submit.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(292f, 140f));

        // --- Flight HUD: live badge, status, stop map. No numbers (Nadide 0020).
        CanvasGroup hud = CreateGroup("Flight HUD", safe);
        Stretch((RectTransform)hud.transform);
        TMP_Text hudRound = CreateTmp("HUD Round", hud.transform, "SORU 1 / 5", RocketQuizUiAssets.Mono, 24f, RqTheme.TextSecondary, TextAlignmentOptions.TopLeft, RocketQuizUiAssets.MonoShadow);
        hudRound.characterSpacing = 12f;
        Place(hudRound.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RqTheme.Gutter, -168f), new Vector2(600f, 34f));
        TMP_Text live = CreateTmp("Live", hud.transform, "<color=#FF4D3D>●</color> CANLI", RocketQuizUiAssets.Mono, 24f, RqTheme.TextPrimary, TextAlignmentOptions.TopRight, RocketQuizUiAssets.MonoShadow);
        live.characterSpacing = 12f;
        Place(live.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-RqTheme.Gutter, -168f), new Vector2(300f, 34f));
        OrbitMap hudMap = CreateOrbitMap("HUD Map", hud.transform, CardWidth, false, false, true);
        Place((RectTransform)hudMap.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(CardWidth, 110f));
        TMP_Text status = CreateTmp("Status", hud.transform, "KALKIŞ", RocketQuizUiAssets.Mono, 34f, RqTheme.TextPrimary, TextAlignmentOptions.Top, RocketQuizUiAssets.MonoShadow);
        status.characterSpacing = 16f;
        Place(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -352f), new Vector2(1040f, 50f));
        TMP_Text hint = CreateTmp("Skip Hint", hud.transform, "ATLAMAK İÇİN DOKUN", RocketQuizUiAssets.Mono, 24f, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Bottom, RocketQuizUiAssets.MonoShadow);
        hint.characterSpacing = 12f;
        Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(1000f, 40f));

        TMP_Text milestone = CreateTmp("Milestone", safe, "MARS", RocketQuizUiAssets.DisplayBlack, 92f, Color.white, TextAlignmentOptions.Center, RocketQuizUiAssets.DisplayBlackShadow);
        Place(milestone.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), new Vector2(1040f, 150f));
        TMP_Text stamp = CreateTmp("Stamp", safe, "DOĞRU!", RocketQuizUiAssets.DisplayBlack, 112f, Color.white, TextAlignmentOptions.Center, RocketQuizUiAssets.DisplayBlackShadow);
        Place(stamp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1040f, 180f));

        screens = BuildScreens(safe, root);

        Image flash = CreateFullscreenImage("Flash", root, new Color(1f, 1f, 1f, 0f));

        var ui = canvasGo.AddComponent<QuizUI>();
        SetReference(ui, "header", header);
        SetReference(ui, "quizPanel", quizPanel);
        SetReference(ui, "promptText", prompt);
        SetReference(ui, "questionLabel", questionLabel);
        SetReference(ui, "exampleText", example);
        SetReference(ui, "timerFill", timerFill);
        SetReference(ui, "timerText", timerText);
        SetReference(ui, "answerBar", answerBar);
        SetReference(ui, "answerInput", input);
        SetReference(ui, "submitButton", submit);
        SetReference(ui, "feedbackText", feedback);
        SetReference(ui, "attemptsText", attempts);
        SetReference(ui, "scoreText", score);
        SetReference(ui, "roundText", round);
        SetReference(ui, "hudPanel", hud);
        SetReference(ui, "statusText", status);
        SetReference(ui, "hudRoundText", hudRound);
        SetReference(ui, "hudMap", hudMap);
        SetReference(ui, "skipHint", hint);
        SetReference(ui, "stampText", stamp);
        SetReference(ui, "milestoneText", milestone);
        SetReference(ui, "flashImage", flash);
        SetReference(ui, "veilImage", veil);
        SetReference(ui, "webBridge", bridge);
        return ui;
    }

    // Anchors and pivot share the same point; position is the offset of the element's top edge.
    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x, anchorMin.y);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static CanvasGroup CreateGroup(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<CanvasGroup>();
    }

    private static PanelGraphic CreatePanelGraphic(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<PanelGraphic>();
        panel.material = RocketQuizUiAssets.Panel;
        panel.raycastTarget = false;
        return panel;
    }

    // Glass card: navy gradient, hairline border, soft dark halo that separates it from the scene.
    private static CanvasGroup CreateCard(string name, Transform parent)
    {
        CanvasGroup group = CreateGroup(name, parent);
        var panel = group.gameObject.AddComponent<PanelGraphic>();
        panel.material = RocketQuizUiAssets.Panel;
        panel.SetStyle(RqTheme.PanelTop, RqTheme.PanelBottom, RqTheme.RadiusCard, 2f, RqTheme.PanelBorder, 28f, new Color(0f, 0.01f, 0.04f, 0.35f));
        return group;
    }

    private static Image CreateFullscreenImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateTmp(string name, Transform parent, string content, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment, Material preset)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        if (preset != null)
        {
            text.fontSharedMaterial = preset;
        }
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color top, Color bottom, bool glow, out TMP_Text labelText)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<PanelGraphic>();
        panel.material = RocketQuizUiAssets.Panel;
        panel.SetStyle(top, bottom, RqTheme.RadiusControl, 2f, new Color(1f, 1f, 1f, 0.28f),
            glow ? 26f : 0f, glow ? new Color(top.r, top.g, top.b, 0.5f) : Color.clear);
        var button = go.AddComponent<Button>();
        button.targetGraphic = panel;
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        labelText = CreateTmp("Label", go.transform, label, RocketQuizUiAssets.Display, 40f, Color.white, TextAlignmentOptions.Center, null);
        labelText.characterSpacing = 6f;
        Stretch(labelText.rectTransform);
        return button;
    }

    private static TMP_InputField CreateInput(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<PanelGraphic>();
        panel.material = RocketQuizUiAssets.Panel;
        panel.SetStyle(new Color(0.10f, 0.14f, 0.22f, 0.92f), new Color(0.06f, 0.09f, 0.16f, 0.95f), RqTheme.RadiusControl, 2f,
            new Color(0.62f, 0.78f, 1f, 0.35f), 0f, Color.clear);

        var area = new GameObject("Text Area", typeof(RectTransform));
        area.transform.SetParent(go.transform, false);
        var areaRect = (RectTransform)area.transform;
        Stretch(areaRect);
        areaRect.offsetMin = new Vector2(36f, 10f);
        areaRect.offsetMax = new Vector2(-36f, -10f);
        area.AddComponent<RectMask2D>();

        TMP_Text placeholder = CreateTmp("Placeholder", area.transform, "Tek cevap yaz", RocketQuizUiAssets.Body, 40f, RqTheme.TextMuted, TextAlignmentOptions.Left, null);
        Stretch(placeholder.rectTransform);
        TMP_Text text = CreateTmp("Text", area.transform, string.Empty, RocketQuizUiAssets.Body, 44f, RqTheme.TextPrimary, TextAlignmentOptions.Left, null);
        Stretch(text.rectTransform);
        text.textWrappingMode = TextWrappingModes.NoWrap;

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = panel;
        input.textViewport = areaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.fontAsset = RocketQuizUiAssets.Body;
        input.pointSize = 44f;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 40;
        input.caretColor = RqTheme.Ignition;
        input.customCaretColor = true;
        input.caretWidth = 3;
        input.selectionColor = new Color(1f, 0.46f, 0.12f, 0.35f);
        return input;
    }
}
