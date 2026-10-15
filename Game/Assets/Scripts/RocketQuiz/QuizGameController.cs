using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Drives one day of play with Nadide's rules: 5 prompts, 30 s each, retries until time runs out,
// "Bunu mu demek istediniz?" confirmation, a server-decided stop per answer and a provisional total.
// All game state comes from an IGameSession (MockGameSession offline; the Nadide network session in Net/).
public class QuizGameController : MonoBehaviour
{
    public enum GameState
    {
        Boot,
        Final,
        Hangar,
        Question,
        Suggestion,
        Launching,
        Moment,
        Result,
    }

    // Swapped by the Nadide integration (Net/) for the real server session.
    public static Func<IGameSession> SessionFactory = NadideSessions.CreateDefault;

    private const string IntroSeenKey = "rq.introSeen";
    private const string StreakKey = "rq.streak";
    private const string LastDayKey = "rq.lastDay";

    [SerializeField] private QuizUI ui;
    [SerializeField] private GameScreens screens;
    [SerializeField] private LaunchDirector director;
    [SerializeField] private AudioDirector audioDirector;
    [SerializeField] private LaunchCameraRig rig;
    [SerializeField] private bool playIntro = true;

    private IGameSession session;
    private SessionInfo info;
    private PromptView prompt;
    private float deadline;
    private bool frozen;
    private float frozenLeft;
    private bool busy;
    private readonly List<DayRow> rows = new List<DayRow>();

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public StopId LastStop { get; private set; }
    public int LastPoints { get; private set; }
    public PromptView CurrentPrompt { get { return prompt; } }
    public IGameSession Session { get { return session; } }

    private void Start()
    {
        session = SessionFactory();
        LaunchDirector.ReducedMotion = LaunchDirector.ReducedMotion || PlayerPrefs.GetInt("rq.reducedMotion", 0) == 1;
        ui.Submitted += SubmitAnswer;
        screens.StartPressed += BeginDay;
        screens.SuggestionAnswered += ConfirmSuggestion;
        screens.MomentContinue += Continue;
        screens.SharePressed += Share;
        screens.FinalContinue += CloseFinal;
        director.ResetToPad();
        StartCoroutine(Boot());
    }

    private void OnDestroy()
    {
        if (ui != null) ui.Submitted -= SubmitAnswer;
        if (screens != null)
        {
            screens.StartPressed -= BeginDay;
            screens.SuggestionAnswered -= ConfirmSuggestion;
            screens.MomentContinue -= Continue;
            screens.SharePressed -= Share;
            screens.FinalContinue -= CloseFinal;
        }
    }

    private void Update()
    {
        if (State != GameState.Question && State != GameState.Suggestion) return;
        float left = SecondsLeft;
        ui.SetTimer(left, frozen);
        if (!frozen && left <= 0f && !busy)
        {
            StartCoroutine(TimeUp());
        }
    }

    public float SecondsLeft
    {
        get { return frozen ? frozenLeft : Mathf.Max(0f, deadline - Time.realtimeSinceStartup); }
    }

    // ---------- boot, final reveal, hangar ----------

    private IEnumerator Boot()
    {
        State = GameState.Boot;
        if (playIntro && PlayerPrefs.GetInt(IntroSeenKey, 0) == 0)
        {
            PlayerPrefs.SetInt(IntroSeenKey, 1);
            yield return screens.PlayIntro(LaunchDirector.ReducedMotion);
        }

        FinalDay final = null;
        bool waiting = true;
        session.Final(f => { final = f; waiting = false; }, e => waiting = false);
        while (waiting) yield return null;

        if (final != null && final.prompts != null && final.prompts.Length > 0)
        {
            State = GameState.Final;
            screens.ShowFinal(final);
            yield break;
        }
        OpenHangar();
    }

    private void CloseFinal()
    {
        if (State != GameState.Final) return;
        screens.HideFinal();
        OpenHangar();
    }

    private void OpenHangar()
    {
        State = GameState.Hangar;
        session.Start(i =>
        {
            info = i;
            if (i.status == "finished")
            {
                ShowResult();
                return;
            }
            screens.ShowHangar(i, i.progress != null && i.progress.total > 0 ? i.progress.total : MockGameSession.PromptsPerDay, i.secondsPerPrompt);
        }, Fail);
    }

    public void BeginDay()
    {
        if (State != GameState.Hangar) return;
        screens.HideHangar();
        RequestNext();
    }

    // ---------- question loop ----------

    private void RequestNext()
    {
        busy = true;
        session.Next(next =>
        {
            busy = false;
            if (next.status == "finished")
            {
                ShowResult();
                return;
            }
            ShowPrompt(next.prompt);
        }, Fail);
    }

    private void ShowPrompt(PromptView view)
    {
        prompt = view;
        State = view.state == "suggested" ? GameState.Suggestion : GameState.Question;
        frozen = view.frozenSecondsLeft >= 0f;
        frozenLeft = view.frozenSecondsLeft;
        deadline = Time.realtimeSinceStartup + view.secondsLeft;
        ui.ShowQuestion(view, Score, info != null ? info.secondsPerPrompt : MockGameSession.SecondsPerPrompt);
        if (State == GameState.Suggestion)
        {
            ui.SetAnswering(false);
            screens.ShowSuggestion(view.suggestion, view.typed, frozen);
        }
    }

    public void SubmitAnswer(string text)
    {
        if (State != GameState.Question || busy || string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        busy = true;
        session.Answer(prompt.promptId, text, result => { busy = false; HandleAnswer(result); }, Fail);
    }

    private void ConfirmSuggestion(bool yes)
    {
        if (State != GameState.Suggestion || busy) return;
        busy = true;
        screens.HideSuggestion();
        session.Confirm(prompt.promptId, yes, result => { busy = false; HandleAnswer(result); }, Fail);
    }

    private void HandleAnswer(AnswerResult result)
    {
        switch (result.status)
        {
            case "accepted":
                StartCoroutine(Launch(result));
                break;
            case "suggested":
                State = GameState.Suggestion;
                if (result.prompt != null) SyncClock(result.prompt);
                ui.SetAnswering(false);
                screens.ShowSuggestion(result.match.suggestion, result.prompt != null ? result.prompt.typed : string.Empty, frozen);
                break;
            case "rejected":
                State = GameState.Question;
                if (result.prompt != null)
                {
                    SyncClock(result.prompt);
                    prompt = result.prompt;
                }
                ui.SetAnswering(true);
                audioDirector.Wrong();
                rig.AddTrauma(0.15f);
                ui.ShowRejected(prompt.attempts, result.attemptsLeft);
                break;
            default:
                // "closed": the clock ran out on the server side.
                StartCoroutine(TimeUp());
                break;
        }
    }

    private void SyncClock(PromptView view)
    {
        frozen = view.frozenSecondsLeft >= 0f;
        frozenLeft = view.frozenSecondsLeft;
        deadline = Time.realtimeSinceStartup + view.secondsLeft;
    }

    private IEnumerator TimeUp()
    {
        if (State == GameState.Launching || State == GameState.Moment) yield break;
        State = GameState.Launching;
        busy = true;
        screens.HideSuggestion();
        Record(StopId.Sun, null, 0, false);
        yield return director.Scrub();
        busy = false;
        ShowMoment(StopId.Sun, null, 0, false);
    }

    private IEnumerator Launch(AnswerResult result)
    {
        State = GameState.Launching;
        StopId stop = result.score != null ? result.score.Stop : StopId.Sun;
        int points = result.score != null ? result.score.points : 0;
        bool onlyYou = result.score != null && result.score.onlyYou;
        Record(stop, result.answer, points, onlyYou);
        Score = result.progress != null ? result.progress.provisionalTotal : Score + points;
        yield return director.Play(stop, "SORU " + prompt.position + " / " + prompt.total);
        ui.SetScore(Score);
        ShowMoment(stop, result.answer, points, onlyYou);
    }

    private void ShowMoment(StopId stop, string answer, int points, bool onlyYou)
    {
        State = GameState.Moment;
        LastStop = stop;
        LastPoints = points;
        bool last = prompt != null && prompt.position >= prompt.total;
        screens.ShowMoment(stop, answer, points, onlyYou, last);
    }

    public void Continue()
    {
        if (State != GameState.Moment) return;
        screens.HideMoment();
        StartCoroutine(ReturnToPad());
    }

    private IEnumerator ReturnToPad()
    {
        State = GameState.Launching;
        yield return ui.FadeBlack(true, 0.3f);
        director.ResetToPad();
        RequestNext();
        yield return ui.FadeBlack(false, 0.4f);
    }

    private void Record(StopId stop, string answer, int points, bool onlyYou)
    {
        rows.Add(new DayRow
        {
            Position = prompt != null ? prompt.position : rows.Count + 1,
            Prompt = prompt != null ? prompt.text : string.Empty,
            Answer = answer,
            Stop = stop,
            Points = points,
            OnlyYou = onlyYou,
        });
    }

    // ---------- result ----------

    private void ShowResult()
    {
        State = GameState.Result;
        ui.HideQuiz();
        ui.SetHud(false);
        ui.SetHeader(false);
        int streak = UpdateStreak();
        int total = 0;
        foreach (DayRow row in rows) total += row.Points;
        if (info != null && info.progress != null && rows.Count == 0) total = info.progress.provisionalTotal;
        Score = total;
        screens.ShowResult(rows.ToArray(), total, info != null ? info.dayNumber : 0, info != null ? info.date : string.Empty, streak);
    }

    private void Share()
    {
        string text = GameScreens.ShareText(rows.ToArray(), Score, info != null ? info.dayNumber : 0);
        GUIUtility.systemCopyBuffer = text;
        screens.SetShareLabel("KOPYALANDI");
    }

    // Kept on the device (Nadide: the streak continues when today or yesterday was played).
    private int UpdateStreak()
    {
        string today = info != null ? info.date : DateTime.Today.ToString("yyyy-MM-dd");
        string last = PlayerPrefs.GetString(LastDayKey, string.Empty);
        int streak = PlayerPrefs.GetInt(StreakKey, 0);
        if (last == today) return Mathf.Max(streak, 1);
        DateTime todayDate;
        DateTime lastDate;
        bool consecutive = DateTime.TryParse(today, out todayDate) && DateTime.TryParse(last, out lastDate)
            && (todayDate - lastDate).TotalDays <= 1.01;
        streak = consecutive ? streak + 1 : 1;
        PlayerPrefs.SetInt(StreakKey, streak);
        PlayerPrefs.SetString(LastDayKey, today);
        return streak;
    }

    private void Fail(string error)
    {
        busy = false;
        Debug.LogWarning("Game session error: " + error);
    }
}
