using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Gameplay and UI wiring for the RocketQuiz scene (portrait, 1080x1920 reference).
public static partial class RocketQuizSceneBuilder
{
    private static Font uiFont;
    private static Sprite panelSprite;

    private static void BuildGameplay(RocketVisual rocket, LaunchCameraRig rig, SpaceJourney journey, Camera nearCamera, Light sun)
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();

        QuizUI ui = BuildUi();

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
        SetReference(controller, "director", director);
        SetReference(controller, "audioDirector", audioDirector);
        SetReference(controller, "rig", rig);
    }

    private static QuizUI BuildUi()
    {
        var canvasGo = new GameObject("UI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;
        canvasGo.AddComponent<GraphicRaycaster>();
        Transform root = canvasGo.transform;

        Image veil = CreateFullscreenImage("Cloud Veil", root, new Color(1f, 1f, 1f, 0f));

        Text score = CreateText("Score", root, "PUAN 0", 44, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, true);
        Place(score.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -60f), new Vector2(520f, 70f));
        Text round = CreateText("Round", root, "SORU 1 / 6", 44, FontStyle.Bold, TextAnchor.UpperRight, new Color(0.8f, 0.88f, 1f), true);
        Place(round.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -60f), new Vector2(520f, 70f));

        // Question card sits in the top half so the mobile keyboard never covers it.
        CanvasGroup quizPanel = CreatePanel("Quiz Panel", root, new Color(0.03f, 0.05f, 0.10f, 0.80f));
        Place((RectTransform)quizPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(980f, 640f));
        Text prompt = CreateText("Prompt", quizPanel.transform, "Soru", 58, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, false);
        Place(prompt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 190f));
        InputField input = CreateInput("Answer Input", quizPanel.transform);
        Place((RectTransform)input.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(880f, 120f));
        Button submit = CreateButton("Submit Button", quizPanel.transform, "FIRLAT", new Color(1f, 0.42f, 0.10f), out Text submitLabel);
        Place((RectTransform)submit.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -395f), new Vector2(880f, 120f));
        Text feedback = CreateText("Feedback", quizPanel.transform, string.Empty, 32, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 1f), false);
        Place(feedback.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -535f), new Vector2(900f, 90f));

        CanvasGroup hud = CreateGroup("Flight HUD", root);
        Stretch((RectTransform)hud.transform);
        Text altitude = CreateText("Altitude", hud.transform, "0 m", 84, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, true);
        Place(altitude.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1040f, 120f));
        Text hint = CreateText("Speed Hint", hud.transform, "Hızlandırmak için ekrana basılı tut", 30, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f), true);
        Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1000f, 60f));

        Text milestone = CreateText("Milestone", root, "YÖRÜNGE", 100, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, true);
        Place(milestone.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 560f), new Vector2(1060f, 150f));
        Text stamp = CreateText("Stamp", root, "DOĞRU!", 130, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, true);
        Place(stamp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 380f), new Vector2(1060f, 190f));

        CanvasGroup result = CreatePanel("Result Panel", root, new Color(0.03f, 0.05f, 0.10f, 0.84f));
        Place((RectTransform)result.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(990f, 760f));
        Text resultTitle = CreateText("Title", result.transform, "NADİR CEVAP", 72, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, false);
        Place(resultTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(930f, 100f));
        Text resultDestination = CreateText("Destination", result.transform, "Cevap", 40, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.82f, 0.9f, 1f), false);
        Place(resultDestination.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(930f, 120f));
        Text resultRarity = CreateText("Rarity", result.transform, "Yalnızca %3 bu cevabı verdi", 50, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, false);
        Place(resultRarity.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -272f), new Vector2(930f, 80f));
        Text resultScore = CreateText("Points", result.transform, "+0", 120, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.25f), false);
        Place(resultScore.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(930f, 150f));
        resultScore.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        resultScore.rectTransform.anchoredPosition = new Vector2(0f, -435f);
        Text nearMiss = CreateText("Near Miss", result.transform, string.Empty, 32, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.7f, 0.76f, 0.86f), false);
        Place(nearMiss.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -520f), new Vector2(930f, 60f));
        Button continueButton = CreateButton("Continue Button", result.transform, "SIRADAKİ SORU", new Color(0.16f, 0.52f, 1f), out Text continueLabel);
        Place((RectTransform)continueButton.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -605f), new Vector2(880f, 120f));

        Image flash = CreateFullscreenImage("Flash", root, new Color(1f, 1f, 1f, 0f));

        var ui = canvasGo.AddComponent<QuizUI>();
        SetReference(ui, "quizPanel", quizPanel);
        SetReference(ui, "promptText", prompt);
        SetReference(ui, "answerInput", input);
        SetReference(ui, "submitButton", submit);
        SetReference(ui, "feedbackText", feedback);
        SetReference(ui, "scoreText", score);
        SetReference(ui, "roundText", round);
        SetReference(ui, "hudPanel", hud);
        SetReference(ui, "altitudeText", altitude);
        SetReference(ui, "stampText", stamp);
        SetReference(ui, "milestoneText", milestone);
        SetReference(ui, "resultPanel", result);
        SetReference(ui, "resultTitle", resultTitle);
        SetReference(ui, "resultDestination", resultDestination);
        SetReference(ui, "resultRarity", resultRarity);
        SetReference(ui, "resultScore", resultScore);
        SetReference(ui, "resultNearMiss", nearMiss);
        SetReference(ui, "continueButton", continueButton);
        SetReference(ui, "continueLabel", continueLabel);
        SetReference(ui, "flashImage", flash);
        SetReference(ui, "veilImage", veil);
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

    private static CanvasGroup CreatePanel(string name, Transform parent, Color color)
    {
        CanvasGroup group = CreateGroup(name, parent);
        var image = group.gameObject.AddComponent<Image>();
        image.sprite = panelSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
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

    private static Text CreateText(string name, Transform parent, string content, int size, FontStyle style, TextAnchor anchor, Color color, bool outline)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (outline)
        {
            var shadow = go.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color, out Text labelText)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = panelSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        button.colors = colors;

        labelText = CreateText("Label", go.transform, label, 56, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, false);
        Stretch(labelText.rectTransform);
        return button;
    }

    private static InputField CreateInput(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = panelSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.96f, 0.97f, 1f, 1f);

        Text text = CreateText("Text", go.transform, string.Empty, 54, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.06f, 0.08f, 0.14f), false);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(24f, 8f);
        text.rectTransform.offsetMax = new Vector2(-24f, -8f);
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        Text placeholder = CreateText("Placeholder", go.transform, "Cevabını yaz...", 48, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.06f, 0.08f, 0.14f, 0.45f), false);
        Stretch(placeholder.rectTransform);

        var input = go.AddComponent<InputField>();
        input.targetGraphic = image;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = InputField.LineType.SingleLine;
        input.characterLimit = 40;
        return input;
    }
}
