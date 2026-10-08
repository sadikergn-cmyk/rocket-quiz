using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// View layer for the quiz: question card, flight HUD, stamps and the result card.
public class QuizUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup quizPanel;
    [SerializeField] private Text promptText;
    [SerializeField] private InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text roundText;
    [SerializeField] private CanvasGroup hudPanel;
    [SerializeField] private Text altitudeText;
    [SerializeField] private Text stampText;
    [SerializeField] private Text milestoneText;
    [SerializeField] private CanvasGroup resultPanel;
    [SerializeField] private Text resultTitle;
    [SerializeField] private Text resultDestination;
    [SerializeField] private Text resultRarity;
    [SerializeField] private Text resultScore;
    [SerializeField] private Text resultNearMiss;
    [SerializeField] private Button continueButton;
    [SerializeField] private Text continueLabel;
    [SerializeField] private Image flashImage;
    [SerializeField] private Image veilImage;

    private Coroutine stampRoutine;
    private Coroutine milestoneRoutine;
    private Coroutine flashRoutine;
    private Coroutine resultRoutine;
    private Coroutine shakeRoutine;
    private Vector2 quizPanelHome;

    public event Action<string> Submitted;
    public event Action Continued;

    private void Awake()
    {
        quizPanelHome = ((RectTransform)quizPanel.transform).anchoredPosition;
        submitButton.onClick.AddListener(OnSubmitClicked);
        answerInput.onSubmit.AddListener(OnInputSubmitted);
        continueButton.onClick.AddListener(OnContinueClicked);

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
    }

    private void OnSubmitClicked()
    {
        if (Submitted != null) Submitted(answerInput.text);
    }

    private void OnInputSubmitted(string value)
    {
        if (Submitted != null) Submitted(value);
    }

    private void OnContinueClicked()
    {
        if (Continued != null) Continued();
    }

    public void ShowQuestion(string prompt, int round, int total, int score, int attempts)
    {
        promptText.text = prompt;
        roundText.text = "SORU " + round + " / " + total;
        SetScore(score);
        answerInput.text = string.Empty;
        feedbackText.color = new Color(0.75f, 0.85f, 1f, 0.9f);
        feedbackText.text = "Ne kadar az bilinen bir cevap verirsen o kadar uzağa gidersin";
        SetGroup(quizPanel, true);
        SetGroup(hudPanel, false);
        SetGroup(resultPanel, false);
        FocusInput();
    }

    public void SetScore(int score)
    {
        scoreText.text = "PUAN " + score.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
    }

    public void FocusInput()
    {
        // Mobile keeps the keyboard closed until the player taps the field.
        if (!Application.isMobilePlatform)
        {
            answerInput.ActivateInputField();
        }
    }

    public void ShowWrong(string message)
    {
        feedbackText.color = new Color(1f, 0.45f, 0.4f);
        feedbackText.text = message;
        answerInput.text = string.Empty;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakePanel());
        FocusInput();
    }

    public void HideQuiz()
    {
        answerInput.DeactivateInputField();
        SetGroup(quizPanel, false);
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
        stampRoutine = StartCoroutine(Pop(stampText, message, color, 2.2f, 0.9f));
    }

    public void Milestone(string message, Color color)
    {
        if (milestoneRoutine != null) StopCoroutine(milestoneRoutine);
        milestoneRoutine = StartCoroutine(Pop(milestoneText, message, color, 1.6f, 1.5f));
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

    private IEnumerator ResultRoutine(int points, Action<float> onTick)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        RectTransform panel = (RectTransform)resultPanel.transform;
        continueButton.gameObject.SetActive(false);
        resultScore.text = "+0";

        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.35f);
            panel.localScale = Vector3.one * EaseOutBack(t);
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
            resultScore.text = "+" + Mathf.RoundToInt(points * eased).ToString("N0", culture);
            if (elapsed >= nextTick)
            {
                nextTick = elapsed + Mathf.Lerp(0.09f, 0.035f, t);
                if (onTick != null) onTick(t);
            }
            yield return null;
        }
        resultScore.text = "+" + points.ToString("N0", culture);

        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.25f);
            resultScore.rectTransform.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        resultScore.rectTransform.localScale = Vector3.one;
        continueButton.gameObject.SetActive(true);
    }

    private IEnumerator Pop(Text label, string message, Color color, float startScale, float hold)
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
        const float fadeOut = 0.35f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOut);
            label.color = new Color(color.r, color.g, color.b, 1f - t);
            rect.localScale = Vector3.one * (1f + 0.15f * t);
            yield return null;
        }
        label.gameObject.SetActive(false);
    }

    private IEnumerator ShakePanel()
    {
        RectTransform rect = (RectTransform)quizPanel.transform;
        float elapsed = 0f;
        const float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float offset = Mathf.Sin(t * Mathf.PI * 8f) * 26f * (1f - t);
            rect.anchoredPosition = quizPanelHome + new Vector2(offset, 0f);
            yield return null;
        }
        rect.anchoredPosition = quizPanelHome;
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
