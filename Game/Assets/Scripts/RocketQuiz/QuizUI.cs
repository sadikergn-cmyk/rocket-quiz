using System;
using System.Collections;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// View layer for the question card and the flight HUD (Nadide flow, RocketQuiz look).
// On the web the answer field is an HTML overlay (WebBridge); elsewhere it is the in-canvas bar.
// No distances anywhere on screen (Nadide 0020): the HUD shows status, question and the stop map.
public class QuizUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup header;
    [SerializeField] private CanvasGroup quizPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text questionLabel;
    [SerializeField] private TMP_Text exampleText;
    [SerializeField] private PanelGraphic timerFill;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private CanvasGroup answerBar;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private CanvasGroup hudPanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text hudRoundText;
    [SerializeField] private OrbitMap hudMap;
    [SerializeField] private TMP_Text skipHint;
    [SerializeField] private TMP_Text stampText;
    [SerializeField] private TMP_Text milestoneText;
    [SerializeField] private Image flashImage;
    [SerializeField] private Image veilImage;
    [SerializeField] private WebBridge webBridge;

    private const string Placeholder = "Tek cevap yaz";
    private const string Hint = "Kimsenin aklına gelmeyeni bul. Yanlışsa süre içinde yeniden dene.";

    private static readonly System.Globalization.CultureInfo Turkish = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    private Coroutine stampRoutine;
    private Coroutine milestoneRoutine;
    private Coroutine flashRoutine;
    private MotionHandle cardMotion;
    private MotionHandle shakeMotion;
    private Vector2 quizPanelHome;
    private bool answering;
    private float timerTotal = 30f;

    public event Action<string> Submitted;

    public OrbitMap HudMap { get { return hudMap; } }

    private void Awake()
    {
        quizPanelHome = ((RectTransform)quizPanel.transform).anchoredPosition;
        submitButton.onClick.AddListener(OnSubmitClicked);
        answerInput.onSubmit.AddListener(OnInputSubmitted);
        if (webBridge != null) webBridge.AnswerSubmitted += OnInputSubmitted;

        // The HTML overlay replaces the in-canvas answer bar on the web.
        answerBar.gameObject.SetActive(!WebBridge.UsesHtmlInput);

        SetGroup(quizPanel, false);
        SetGroup(answerBar, false);
        SetGroup(hudPanel, false);
        SetGroup(header, false);
        stampText.gameObject.SetActive(false);
        milestoneText.gameObject.SetActive(false);
        SetAlpha(flashImage, 0f);
        SetAlpha(veilImage, 0f);
    }

    private void OnDestroy()
    {
        submitButton.onClick.RemoveListener(OnSubmitClicked);
        answerInput.onSubmit.RemoveListener(OnInputSubmitted);
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

    public void SetHeader(bool visible)
    {
        SetGroup(header, visible);
    }

    public void ShowQuestion(PromptView prompt, int score, float totalSeconds)
    {
        timerTotal = Mathf.Max(1f, totalSeconds);
        promptText.text = prompt.text;
        questionLabel.text = "SORU " + prompt.position + " / " + prompt.total;
        exampleText.text = string.IsNullOrEmpty(prompt.example) ? string.Empty : "ör. " + prompt.example;
        roundText.text = prompt.position + "/" + prompt.total;
        SetScore(score);
        answerInput.text = string.Empty;
        SetFeedback(Hint, RqTheme.TextSecondary);
        SetAttempts(prompt.attempts, prompt.attemptsLeft);
        SetTimer(prompt.secondsLeft, false);
        SetHeader(true);
        SetGroup(quizPanel, true);
        SetGroup(answerBar, true);
        SetGroup(hudPanel, false);
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

    // Draining bar; the last five seconds pulse in ignition orange (Nadide: "Son 5 saniye").
    public void SetTimer(float secondsLeft, bool frozen)
    {
        float left = Mathf.Max(0f, secondsLeft);
        float t = Mathf.Clamp01(left / timerTotal);
        RectTransform bar = timerFill.rectTransform;
        RectTransform track = (RectTransform)bar.parent;
        bar.sizeDelta = new Vector2(track.rect.width * t, bar.sizeDelta.y);
        bool warning = left <= 5f && !frozen;
        float pulse = warning ? 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 1f;
        Color c = frozen ? RqTheme.TextMuted : warning ? RqTheme.Ignition : RqTheme.Telemetry;
        timerFill.SetFill(new Color(c.r, c.g, c.b, pulse), new Color(c.r, c.g, c.b, pulse * 0.8f));
        timerText.text = frozen ? "DURDU" : warning ? "SON " + Mathf.CeilToInt(left) : Mathf.CeilToInt(left).ToString();
        timerText.color = c;
    }

    public void FocusInput()
    {
        // Mobile keeps the keyboard closed until the player taps the field.
        if (!Application.isMobilePlatform && !WebBridge.UsesHtmlInput)
        {
            answerInput.ActivateInputField();
        }
    }

    // Rejection (0018): the reason and the right answer are never shown; tried forms become chips.
    public void ShowRejected(string[] attempts, int attemptsLeft)
    {
        SetFeedback(attemptsLeft <= 0 ? "Bu soruda deneme hakkın bitti." : "Listede yok, başka bir şey dene.", RqTheme.Danger);
        SetAttempts(attempts, attemptsLeft);
        answerInput.text = string.Empty;
        if (webBridge != null) webBridge.ClearAnswer();
        submitButton.interactable = attemptsLeft > 0;
        ShakeCard();
        FocusInput();
    }

    public void SetAnswering(bool value)
    {
        answering = value;
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

    public void SetSkipHint(bool visible)
    {
        if (skipHint != null) skipHint.gameObject.SetActive(visible);
    }

    public void SetHudRound(string text)
    {
        hudRoundText.text = text;
    }

    public void SetStatus(string value)
    {
        statusText.text = value;
    }

    // Soft white veil while the rocket punches through the cloud decks (a fade, never a strobe).
    public void SetVeil(float alpha)
    {
        SetAlpha(veilImage, alpha);
    }

    public void Stamp(string message, Color color)
    {
        if (stampRoutine != null) StopCoroutine(stampRoutine);
        stampRoutine = StartCoroutine(Pop(stampText, message, color, 1.8f, 0.7f));
    }

    public void Milestone(string message, Color color)
    {
        if (milestoneRoutine != null) StopCoroutine(milestoneRoutine);
        milestoneRoutine = StartCoroutine(Pop(milestoneText, message, color, 1.3f, 0.5f));
    }

    public void ClearPops()
    {
        if (stampRoutine != null) StopCoroutine(stampRoutine);
        if (milestoneRoutine != null) StopCoroutine(milestoneRoutine);
        stampText.gameObject.SetActive(false);
        milestoneText.gameObject.SetActive(false);
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

    private void SetFeedback(string message, Color color)
    {
        feedbackText.text = message;
        feedbackText.color = color;
    }

    private void SetAttempts(string[] attempts, int attemptsLeft)
    {
        string text = string.Empty;
        if (attempts != null && attempts.Length > 0)
        {
            text = "<color=#7F8EA8>Denediklerin:</color>  " + string.Join("  ·  ", attempts);
        }
        if (attemptsLeft > 0 && attemptsLeft <= 3)
        {
            text += (text.Length > 0 ? "\n" : string.Empty) + "<color=#FF7620>" + (attemptsLeft == 1 ? "Son deneme" : "Son " + attemptsLeft + " deneme") + "</color>";
        }
        attemptsText.text = text;
        submitButton.interactable = attemptsLeft > 0;
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

    private IEnumerator Pop(TMP_Text label, string message, Color color, float startScale, float hold)
    {
        label.text = message;
        label.gameObject.SetActive(true);
        RectTransform rect = label.rectTransform;

        float elapsed = 0f;
        const float popIn = 0.2f;
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
        const float fadeOut = 0.3f;
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
