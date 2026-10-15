using System;
using System.Collections;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// View layer for the quiz: header, question card, answer bar, flight HUD, stamps and result card.
// On the web the answer field is an HTML overlay (WebBridge); elsewhere it is the in-canvas bar.
public class QuizUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup quizPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text questionLabel;
    [SerializeField] private PanelGraphic[] fuelPips;
    [SerializeField] private CanvasGroup answerBar;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private CanvasGroup hudPanel;
    [SerializeField] private TMP_Text altitudeText;
    [SerializeField] private TMP_Text stampText;
    [SerializeField] private TMP_Text milestoneText;
    [SerializeField] private CanvasGroup resultPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultDestination;
    [SerializeField] private TMP_Text resultRarity;
    [SerializeField] private TMP_Text resultScore;
    [SerializeField] private TMP_Text resultNearMiss;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueLabel;
    [SerializeField] private Image flashImage;
    [SerializeField] private Image veilImage;
    [SerializeField] private WebBridge webBridge;

    private const string Placeholder = "Cevabını yaz…";
    private const string Hint = "Ne kadar az kişinin bildiği bir cevap verirsen o kadar uzağa gidersin.";

    private static readonly System.Globalization.CultureInfo Turkish = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    private Coroutine stampRoutine;
    private Coroutine milestoneRoutine;
    private Coroutine flashRoutine;
    private Coroutine resultRoutine;
    private MotionHandle cardMotion;
    private MotionHandle shakeMotion;
    private Vector2 quizPanelHome;
    private bool answering;

    public event Action<string> Submitted;
    public event Action Continued;

    private void Awake()
    {
        quizPanelHome = ((RectTransform)quizPanel.transform).anchoredPosition;
        submitButton.onClick.AddListener(OnSubmitClicked);
        answerInput.onSubmit.AddListener(OnInputSubmitted);
        continueButton.onClick.AddListener(OnContinueClicked);
        if (webBridge != null) webBridge.AnswerSubmitted += OnInputSubmitted;

        // The HTML overlay replaces the in-canvas answer bar on the web.
        answerBar.gameObject.SetActive(!WebBridge.UsesHtmlInput);

        SetGroup(hudPanel, false);
        SetGroup(resultPanel, false);
        stampText.gameObject.SetActive(false);
        milestoneText.gameObject.SetActive(false);
        SetAlpha(flashImage, 0f);
        SetAlpha(veilImage, 0f);
    }

    private void OnDestroy()
    {
        submitButton.onClick.RemoveListener(OnSubmitClicked);
        answerInput.onSubmit.RemoveListener(OnInputSubmitted);
        continueButton.onClick.RemoveListener(OnContinueClicked);
        if (webBridge != null) webBridge.AnswerSubmitted -= OnInputSubmitted;
        cardMotion.TryCancel();
        shakeMotion.TryCancel();
    }

    private void OnSubmitClicked()
    {
        OnInputSubmitted(answerInput.text);
    }

    private void OnInputSubmitted(string value)
    {
        if (!answering) return;
        if (Submitted != null) Submitted(value);
    }

    private void OnContinueClicked()
    {
        if (Continued != null) Continued();
    }

    public void ShowQuestion(string prompt, int round, int total, int score, int attempts)
    {
        promptText.text = prompt;
        questionLabel.text = "SORU " + round.ToString("00") + "  ·  " + total.ToString("00");
        roundText.text = round + "/" + total;
        SetScore(score);
        SetFuel(attempts);
        answerInput.text = string.Empty;
        SetFeedback(Hint, RqTheme.TextSecondary);
        SetGroup(quizPanel, true);
        SetGroup(answerBar, true);
        SetGroup(hudPanel, false);
        SetGroup(resultPanel, false);
        answering = true;
        AnimateCardIn();

        if (webBridge != null && WebBridge.UsesHtmlInput)
        {
            webBridge.ShowAnswer(Placeholder);
        }
        FocusInput();
    }

    public void SetScore(int score)
    {
        scoreText.text = score.ToString("N0", Turkish);
    }

    public void SetFuel(int left)
    {
        for (int i = 0; i < fuelPips.Length; i++)
        {
            bool full = i < left;
            fuelPips[i].SetFill(full ? RqTheme.Ignition : RqTheme.FuelEmpty, full ? RqTheme.IgnitionDeep : RqTheme.FuelEmpty);
            fuelPips[i].SetGlow(full ? new Color(RqTheme.Ignition.r, RqTheme.Ignition.g, RqTheme.Ignition.b, 0.45f) : Color.clear);
        }
    }

    public void FocusInput()
    {
        // Mobile keeps the keyboard closed until the player taps the field.
        if (!Application.isMobilePlatform && !WebBridge.UsesHtmlInput)
        {
            answerInput.ActivateInputField();
        }
    }

    public void ShowWrong(string message)
    {
        SetFeedback(message, RqTheme.Danger);
        answerInput.text = string.Empty;
        if (webBridge != null) webBridge.ClearAnswer();
        ShakeCard();
        FocusInput();
    }

    public void HideQuiz()
    {
        answering = false;
        answerInput.DeactivateInputField();
        if (webBridge != null) webBridge.HideAnswer();
        SetGroup(quizPanel, false);
        SetGroup(answerBar, false);
    }

    public void SetHud(bool visible)
    {
        SetGroup(hudPanel, visible);
    }

    public void SetAltitude(string value)
    {
        altitudeText.text = value;
    }

    // Soft white veil while the rocket punches through the cloud decks (a fade, never a strobe).
    public void SetVeil(float alpha)
    {
        SetAlpha(veilImage, alpha);
    }

    public void Stamp(string message, Color color)
    {
        if (stampRoutine != null) StopCoroutine(stampRoutine);
        stampRoutine = StartCoroutine(Pop(stampText, message, color, 1.8f, 0.9f));
    }

    public void Milestone(string message, Color color)
    {
        if (milestoneRoutine != null) StopCoroutine(milestoneRoutine);
        milestoneRoutine = StartCoroutine(Pop(milestoneText, message, color, 1.4f, 1.5f));
    }

    public void Flash(Color color, float peakAlpha, float duration)
    {
        if (!JuiceDirector.FlashEnabled) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(color, peakAlpha, duration));
    }

    public Coroutine FadeBlack(bool toBlack, float duration)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FadeRoutine(toBlack, duration));
        return flashRoutine;
    }

    public void ShowResult(string title, Color tint, string destination, string rarityLine, string nearMiss, int points, string buttonLabel, Action<float> onTick)
    {
        resultTitle.text = title;
        resultTitle.color = tint;
        resultDestination.text = destination;
        resultRarity.text = rarityLine;
        resultNearMiss.text = nearMiss;
        continueLabel.text = buttonLabel;
        SetGroup(resultPanel, true);
        if (resultRoutine != null) StopCoroutine(resultRoutine);
        resultRoutine = StartCoroutine(ResultRoutine(points, onTick));
    }

    public void HideResult()
    {
        SetGroup(resultPanel, false);
    }

    private void SetFeedback(string message, Color color)
    {
        feedbackText.text = message;
        feedbackText.color = color;
    }

    private void AnimateCardIn()
    {
        cardMotion.TryCancel();
        RectTransform rect = (RectTransform)quizPanel.transform;
        cardMotion = LMotion.Create(0f, 1f, RqTheme.Slow)
            .WithEase(Ease.OutCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(t =>
            {
                quizPanel.alpha = t;
                rect.anchoredPosition = quizPanelHome + new Vector2(0f, 60f * (1f - t));
            })
            .AddTo(this);
    }

    private void ShakeCard()
    {
        shakeMotion.TryCancel();
        cardMotion.TryCancel();
        quizPanel.alpha = 1f;
        RectTransform rect = (RectTransform)quizPanel.transform;
        shakeMotion = LMotion.Create(0f, 1f, 0.35f)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithOnComplete(() => rect.anchoredPosition = quizPanelHome)
            .Bind(t => rect.anchoredPosition = quizPanelHome + new Vector2(Mathf.Sin(t * Mathf.PI * 8f) * 22f * (1f - t), 0f))
            .AddTo(this);
    }

    private IEnumerator ResultRoutine(int points, Action<float> onTick)
    {
        RectTransform panel = (RectTransform)resultPanel.transform;
        continueButton.gameObject.SetActive(false);
        resultScore.text = "+0";

        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.35f);
            panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, EaseOutBack(t));
            resultPanel.alpha = t;
            yield return null;
        }
        panel.localScale = Vector3.one;

        // Count-up with ticks that speed up and rise in pitch.
        float duration = Mathf.Lerp(0.7f, 1.6f, Mathf.InverseLerp(500f, 5000f, points));
        float nextTick = 0f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            resultScore.text = "+" + Mathf.RoundToInt(points * eased).ToString("N0", Turkish);
            if (elapsed >= nextTick)
            {
                nextTick = elapsed + Mathf.Lerp(0.09f, 0.035f, t);
                if (onTick != null) onTick(t);
            }
            yield return null;
        }
        resultScore.text = "+" + points.ToString("N0", Turkish);

        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.25f);
            resultScore.rectTransform.localScale = Vector3.one * (1f + 0.2f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        resultScore.rectTransform.localScale = Vector3.one;
        continueButton.gameObject.SetActive(true);
    }

    private IEnumerator Pop(TMP_Text label, string message, Color color, float startScale, float hold)
    {
        label.text = message;
        label.gameObject.SetActive(true);
        RectTransform rect = label.rectTransform;

        float elapsed = 0f;
        const float popIn = 0.22f;
        while (elapsed < popIn)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popIn);
            rect.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, EaseOutBack(t));
            label.color = new Color(color.r, color.g, color.b, t);
            yield return null;
        }
        rect.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(hold);

        elapsed = 0f;
        const float fadeOut = 0.35f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOut);
            label.color = new Color(color.r, color.g, color.b, 1f - t);
            rect.localScale = Vector3.one * (1f + 0.08f * t);
            yield return null;
        }
        label.gameObject.SetActive(false);
    }

    private IEnumerator FlashRoutine(Color color, float peakAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            flashImage.color = new Color(color.r, color.g, color.b, peakAlpha * (1f - t) * (1f - t));
            yield return null;
        }
        SetAlpha(flashImage, 0f);
    }

    private IEnumerator FadeRoutine(bool toBlack, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            flashImage.color = new Color(0f, 0f, 0f, toBlack ? t : 1f - t);
            yield return null;
        }
        flashImage.color = new Color(0f, 0f, 0f, toBlack ? 1f : 0f);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static void SetGroup(CanvasGroup group, bool visible)
    {
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
