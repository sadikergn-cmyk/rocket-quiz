using System;
using System.Collections;
using System.Globalization;
using System.Text;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Full-screen and overlay screens around the question card: logo intro, Hangar (open screen),
// "Bunu mu demek istediniz?", stop moment card, day result and yesterday's final reveal.
// The moment card doubles as the error card ("Tekrar dene"), so errors need no extra scene objects.
// Copy follows Nadide's tr.ts; the look is RocketQuiz's mission-control system.
public class GameScreens : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private CanvasGroup intro;
    [SerializeField] private RectTransform introMark;

    [Header("Hangar")]
    [SerializeField] private CanvasGroup hangar;
    [SerializeField] private TMP_Text hangarKicker;
    [SerializeField] private OrbitMap hangarMap;
    [SerializeField] private TMP_Text hangarResume;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startLabel;

    [Header("Suggestion")]
    [SerializeField] private CanvasGroup suggestion;
    [SerializeField] private TMP_Text suggestionValue;
    [SerializeField] private TMP_Text suggestionTyped;
    [SerializeField] private TMP_Text suggestionNote;
    [SerializeField] private Button suggestionNo;
    [SerializeField] private Button suggestionYes;

    [Header("Moment")]
    [SerializeField] private CanvasGroup moment;
    [SerializeField] private PanelGraphic momentAccent;
    [SerializeField] private TMP_Text momentKicker;
    [SerializeField] private TMP_Text momentName;
    [SerializeField] private TMP_Text momentLine;
    [SerializeField] private TMP_Text momentAnswer;
    [SerializeField] private TMP_Text momentPoints;
    [SerializeField] private TMP_Text momentStatus;
    [SerializeField] private Button momentButton;
    [SerializeField] private TMP_Text momentButtonLabel;

    [Header("Result")]
    [SerializeField] private CanvasGroup result;
    [SerializeField] private TMP_Text resultHeader;
    [SerializeField] private OrbitMap resultMap;
    [SerializeField] private TMP_Text resultTotal;
    [SerializeField] private TMP_Text resultStatus;
    [SerializeField] private TMP_Text[] resultRows;
    [SerializeField] private TMP_Text resultStreak;
    [SerializeField] private TMP_Text resultCountdown;
    [SerializeField] private Button shareButton;
    [SerializeField] private TMP_Text shareLabel;

    [Header("Final")]
    [SerializeField] private CanvasGroup final;
    [SerializeField] private TMP_Text finalKicker;
    [SerializeField] private TMP_Text finalTotal;
    [SerializeField] private TMP_Text finalNote;
    [SerializeField] private TMP_Text finalBody;
    [SerializeField] private Button finalButton;

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private MotionHandle countMotion;
    private MotionHandle fadeMotion;
    private bool resultVisible;
    private bool errorMode;
    private Coroutine shareLabelRoutine;

    public event Action StartPressed;
    public event Action<bool> SuggestionAnswered;
    public event Action MomentContinue;
    public event Action RetryPressed;
    public event Action SharePressed;
    public event Action FinalContinue;

    public OrbitMap HangarMap { get { return hangarMap; } }

    private void Awake()
    {
        startButton.onClick.AddListener(OnStart);
        suggestionNo.onClick.AddListener(OnNo);
        suggestionYes.onClick.AddListener(OnYes);
        momentButton.onClick.AddListener(OnMoment);
        shareButton.onClick.AddListener(OnShare);
        finalButton.onClick.AddListener(OnFinal);
        HideAll();
    }

    private void OnDestroy()
    {
        startButton.onClick.RemoveListener(OnStart);
        suggestionNo.onClick.RemoveListener(OnNo);
        suggestionYes.onClick.RemoveListener(OnYes);
        momentButton.onClick.RemoveListener(OnMoment);
        shareButton.onClick.RemoveListener(OnShare);
        finalButton.onClick.RemoveListener(OnFinal);
        countMotion.TryCancel();
        fadeMotion.TryCancel();
    }

    private void Update()
    {
        if (resultVisible)
        {
            resultCountdown.text = "YENİ YOLCULUK  " + Countdown();
        }
    }

    public void HideAll()
    {
        Set(intro, false);
        Set(hangar, false);
        Set(suggestion, false);
        Set(moment, false);
        Set(result, false);
        Set(final, false);
        resultVisible = false;
        errorMode = false;
    }

    // ---------- intro ----------

    public IEnumerator PlayIntro(bool reducedMotion)
    {
        Set(intro, true);
        if (reducedMotion)
        {
            yield return new WaitForSecondsRealtime(0.6f);
        }
        else
        {
            float elapsed = 0f;
            const float duration = 1.3f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float rise = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.6f), 3f);
                introMark.anchoredPosition = new Vector2(0f, Mathf.Lerp(-60f, 0f, rise));
                intro.alpha = Mathf.Clamp01(t / 0.25f);
                yield return null;
            }
        }
        yield return Fade(intro, false, 0.35f);
    }

    // ---------- hangar ----------

    // dayNumber 0 = unknown (play/state does not send it): the kicker then starts with the date.
    public void ShowHangar(int dayNumber, string date, ProgressView progress, int total, int seconds)
    {
        string day = dayNumber > 0 ? "#" + dayNumber + "  ·  " : string.Empty;
        hangarKicker.text = day + DayLabel(date) + "  ·  " + total + " SORU  ·  " + seconds + " SANİYE";
        bool resume = progress != null && progress.answered > 0 && !progress.finished;
        startLabel.text = resume ? "DEVAM" : "BAŞLA";
        hangarResume.text = resume ? "Kaldığın yer: soru " + (progress.answered + 1) + " / " + total : string.Empty;
        hangarMap.ResetMap(true);
        Set(hangar, true);
        StartCoroutine(Fade(hangar, true, 0.4f));
    }

    public void HideHangar()
    {
        StartCoroutine(Fade(hangar, false, 0.25f));
    }

    // ---------- suggestion ----------

    public void ShowSuggestion(string value, string typed, bool frozen)
    {
        suggestionValue.text = value;
        suggestionTyped.text = "Yazdığın: " + typed;
        suggestionNote.text = frozen ? "Sayaç durdu  ·  hakkın gitmez" : "Sayaç işliyor";
        Set(suggestion, true);
        StartCoroutine(Fade(suggestion, true, RqTheme.Fast));
    }

    public void HideSuggestion()
    {
        Set(suggestion, false);
    }

    public bool SuggestionVisible
    {
        get { return suggestion.blocksRaycasts; }
    }

    // ---------- moment ----------

    public void ShowMoment(StopId stop, string answer, int points, bool onlyYou, bool last)
    {
        errorMode = false;
        Color c = RqStops.Color(stop);
        momentAccent.SetFill(c, new Color(c.r, c.g, c.b, 0.35f));
        momentKicker.text = RqStops.Kicker(stop);
        momentKicker.color = c;
        momentName.text = stop == StopId.Sun ? "GÜNEŞ'TE KALDIN" : RqStops.ShortName(stop).ToUpper(Turkish);
        momentName.color = stop == StopId.Sun ? RqTheme.TextPrimary : c;
        momentLine.text = RqStops.Line(stop);
        momentAnswer.text = string.IsNullOrEmpty(answer) ? string.Empty : "Cevabın  ·  " + answer;
        momentStatus.text = stop == StopId.Sun ? string.Empty
            : (onlyYou ? "Şu an bunu yazan tek kişi sensin\n" : string.Empty) + "TAHMİNİ  ·  GÜN SONUNDA KESİNLEŞİR";
        momentButtonLabel.text = last ? "YOLCULUĞUNU GÖR" : "DEVAM";
        momentButton.gameObject.SetActive(false);

        Set(moment, true);
        StartCoroutine(Fade(moment, true, RqTheme.Medium));
        countMotion.TryCancel();
        momentPoints.text = "+0";
        countMotion = LMotion.Create(0f, points, points > 0 ? 0.6f : 0.01f)
            .WithEase(Ease.OutCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithOnComplete(() => momentButton.gameObject.SetActive(true))
            .Bind(v => momentPoints.text = "+" + Mathf.RoundToInt(v).ToString("N0", Turkish))
            .AddTo(this);
    }

    public void HideMoment()
    {
        errorMode = false;
        Set(moment, false);
    }

    // ---------- error (the moment card with a "Tekrar dene" button) ----------

    public void ShowError(string title, string message)
    {
        errorMode = true;
        countMotion.TryCancel();
        Color c = RqTheme.Danger;
        momentAccent.SetFill(c, new Color(c.r, c.g, c.b, 0.35f));
        momentKicker.text = "GÖREV KONTROL";
        momentKicker.color = c;
        momentName.text = title;
        momentName.color = RqTheme.TextPrimary;
        momentLine.text = string.IsNullOrEmpty(message) ? NadideErrors.Server : message;
        momentAnswer.text = string.Empty;
        momentPoints.text = string.Empty;
        momentStatus.text = string.Empty;
        momentButtonLabel.text = "TEKRAR DENE";
        momentButton.gameObject.SetActive(true);
        Set(moment, true);
        StartCoroutine(Fade(moment, true, RqTheme.Medium));
    }

    // ---------- result ----------

    public void ShowResult(DayRow[] rows, int total, int dayNumber, string date, int streak)
    {
        resultHeader.text = "#" + dayNumber + "  ·  " + DayLabel(date);
        var stops = new StopId[rows.Length];
        int onlyYou = 0;
        for (int i = 0; i < resultRows.Length; i++)
        {
            if (i >= rows.Length)
            {
                resultRows[i].text = string.Empty;
                continue;
            }
            DayRow row = rows[i];
            stops[i] = row.Stop;
            if (row.OnlyYou) onlyYou++;
            string hex = ColorUtility.ToHtmlStringRGB(RqStops.Color(row.Stop));
            string answer = string.IsNullOrEmpty(row.Answer) ? "—" : row.Answer;
            resultRows[i].text = "<color=#7F8EA8>" + row.Position + "</color>   " + answer
                + "<pos=62%><color=#" + hex + ">" + RqStops.Name(row.Stop) + "</color><pos=90%>" + row.Points;
        }
        resultMap.ShowDay(stops);
        resultStatus.text = (onlyYou > 0 ? "Sadece sen ×" + onlyYou + "    " : string.Empty) + "TAHMİNİ  ·  00:00'DA KESİNLEŞİR";
        resultStreak.text = "SERİ  " + streak + " GÜN";
        shareLabel.text = "PAYLAŞ";
        resultVisible = true;
        Set(result, true);
        StartCoroutine(Fade(result, true, RqTheme.Slow));
        countMotion.TryCancel();
        countMotion = LMotion.Create(0f, total, 0.9f)
            .WithEase(Ease.OutCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(v => resultTotal.text = Mathf.RoundToInt(v).ToString("N0", Turkish))
            .AddTo(this);
    }

    public void SetShareLabel(string text)
    {
        if (shareLabelRoutine != null) StopCoroutine(shareLabelRoutine);
        shareLabelRoutine = null;
        shareLabel.text = text;
    }

    // Brief feedback on the share button (the host's share result), then back to "PAYLAŞ".
    public void FlashShareLabel(string text, float seconds)
    {
        SetShareLabel(text);
        if (text != "PAYLAŞ") shareLabelRoutine = StartCoroutine(RestoreShareLabel(seconds));
    }

    private IEnumerator RestoreShareLabel(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        shareLabelRoutine = null;
        shareLabel.text = "PAYLAŞ";
    }

    public static string ShareText(DayRow[] rows, int total, int dayNumber)
    {
        var marks = new StringBuilder();
        for (int i = 0; i < rows.Length; i++)
        {
            if (i > 0) marks.Append(' ');
            marks.Append(RqStops.Mark(rows[i].Stop));
        }
        return "RocketQuiz #" + dayNumber + " · " + total.ToString("N0", Turkish) + " ✦ (tahmini)\n" + marks
            + "\nhttps://sadikergn-cmyk.github.io/rocket-quiz/";
    }

    // ---------- final ----------

    public void ShowFinal(FinalDay day)
    {
        finalKicker.text = "DÜNÜN YOLCULUĞU KESİNLEŞTİ  ·  #" + day.dayNumber;
        int delta = day.finalTotal - day.provisionalTotal;
        string deltaText = delta > 0 ? "+" + delta : delta < 0 ? "−" + (-delta) : "±0";
        finalTotal.text = day.finalTotal.ToString("N0", Turkish) + "  <size=60%><color=#7F8EA8>" + deltaText + "</color></size>";
        finalNote.text = "Kesin puanın. Tahminin " + day.provisionalTotal.ToString("N0", Turkish) + " idi.";

        var body = new StringBuilder();
        bool changed = false;
        foreach (FinalPrompt p in day.prompts)
        {
            if (p.provisionalTier != p.finalTier) changed = true;
        }
        if (changed)
        {
            body.Append("<color=#73DBFF>DEĞİŞEN DURAKLAR</color>\n");
            foreach (FinalPrompt p in day.prompts)
            {
                if (p.provisionalTier == p.finalTier) continue;
                StopId from = RqStops.FromId(p.provisionalTier);
                StopId to = RqStops.FromId(p.finalTier);
                body.Append(p.answer).Append(":  ").Append(Colored(from)).Append("  →  ").Append(Colored(to)).Append('\n');
            }
        }
        else
        {
            body.Append(delta == 0 ? "Tahminin tuttu.\n<size=80%>Hiçbir durak değişmedi, puanın kesinleşti.</size>\n"
                : "Duraklar yerinde kaldı.\n<size=80%>Puanın kesinleşirken biraz oynadı.</size>\n");
        }
        foreach (FinalPrompt p in day.prompts)
        {
            if (p.onlyYou) body.Append("\nSadece sen  ·  ").Append(p.answer);
        }
        if (day.prompts.Length > 0 && day.prompts[0].commonAnswers != null)
        {
            FinalPrompt first = day.prompts[0];
            body.Append("\n\n<color=#73DBFF>EN YAYGIN 3 CEVAP  ·  SORU ").Append(first.position).Append("</color>\n");
            for (int i = 0; i < first.commonAnswers.Length; i++)
            {
                body.Append(first.commonAnswers[i]).Append("  <color=#7F8EA8>%").Append(first.commonShares[i].ToString("0.#", Turkish)).Append("</color>\n");
            }
        }
        finalBody.text = body.ToString();
        Set(final, true);
        StartCoroutine(Fade(final, true, RqTheme.Slow));
    }

    public void HideFinal()
    {
        StartCoroutine(Fade(final, false, 0.25f));
    }

    public void HideResult()
    {
        resultVisible = false;
        Set(result, false);
    }

    // ---------- helpers ----------

    private static string Colored(StopId stop)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(RqStops.Color(stop)) + ">" + RqStops.Name(stop) + "</color>";
    }

    public static string DayLabel(string date)
    {
        DateTime d;
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
        {
            return date;
        }
        return d.ToString("d MMMM", Turkish).ToUpper(Turkish);
    }

    // Time until the next day opens (Europe/Istanbul midnight, UTC+3, no DST).
    private static string Countdown()
    {
        DateTime nowTr = DateTime.UtcNow.AddHours(3);
        TimeSpan left = nowTr.Date.AddDays(1) - nowTr;
        return left.Hours.ToString("00") + ":" + left.Minutes.ToString("00") + ":" + left.Seconds.ToString("00");
    }

    private IEnumerator Fade(CanvasGroup group, bool show, float duration)
    {
        if (show) Set(group, true);
        float from = show ? 0f : group.alpha;
        float to = show ? 1f : 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
        if (!show) Set(group, false);
    }

    private static void Set(CanvasGroup group, bool visible)
    {
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void OnStart() { if (StartPressed != null) StartPressed(); }
    private void OnNo() { if (SuggestionAnswered != null) SuggestionAnswered(false); }
    private void OnYes() { if (SuggestionAnswered != null) SuggestionAnswered(true); }
    private void OnMoment()
    {
        if (errorMode)
        {
            if (RetryPressed != null) RetryPressed();
            return;
        }
        if (MomentContinue != null) MomentContinue();
    }
    private void OnShare() { if (SharePressed != null) SharePressed(); }
    private void OnFinal() { if (FinalContinue != null) FinalContinue(); }
}
