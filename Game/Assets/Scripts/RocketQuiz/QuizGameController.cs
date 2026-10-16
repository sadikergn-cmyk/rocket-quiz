using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Drives one day of play with Nadide's rules: 5 prompts, 30 s each, retries until time runs out,
// "Bunu mu demek istediniz?" confirmation, a server-decided stop per answer and a provisional total.
// All game state comes from an IGameSession (MockGameSession offline; the Nadide network session in Net/).
// The Hangar reads the day with State (play/state, creates nothing); "Başla"/"Devam" opens it with Start
// (play/start), whose prompt list restores the finished questions after a reload. A failed call shows the
// error card; "Tekrar dene" resends it (or reopens the day when the session is gone).
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
        Error,
    }

    // Swapped by the Nadide integration (Net/) for the real server session.
    public static Func<IGameSession> SessionFactory = NadideSessions.CreateDefault;

    private const string IntroSeenKey = "rq.introSeen";
    private const string StreakKey = "rq.streak";
    private const string LastDayKey = "rq.lastDay";
    private const float ShareLabelSeconds = 2.5f;

    [SerializeField] private QuizUI ui;
    [SerializeField] private GameScreens screens;
    [SerializeField] private LaunchDirector director;
    [SerializeField] private AudioDirector audioDirector;
    [SerializeField] private LaunchCameraRig rig;
    [SerializeField] private bool playIntro = true;

    private IGameSession session;
    private WebBridge bridge;
    private SessionInfo info;
    private PromptView prompt;
    private AnswerMemo memo;
    private float deadline;
    private bool frozen;
    private float frozenLeft;
    private bool busy;
    private Action retry;
    private readonly List<DayRow> rows = new List<DayRow>();

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public StopId LastStop { get; private set; }
    public int LastPoints { get; private set; }
    public PromptView CurrentPrompt { get { return prompt; } }
    public IGameSession Session { get { return session; } }
    public SessionFailure LastFailure { get; private set; }

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
        screens.RetryPressed += Retry;
        bridge = WebBridge.Instance;
        if (bridge != null) bridge.ShareResult += OnShareResult;
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
            screens.RetryPressed -= Retry;
        }
        if (bridge != null) bridge.ShareResult -= OnShareResult;
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

    // play/state only: the game is opened when the player presses Başla/Devam.
    private void OpenHangar()
    {
        State = GameState.Hangar;
        busy = true;
        session.State(day =>
        {
            busy = false;
            if (day.status == "finished")
            {
                // play/start hands back the finished game: its stops for the result and the day number to share.
                StartSession(ShowHangarFromInfo);
                return;
            }
            int total = day.progress != null && day.progress.total > 0 ? day.progress.total : MockGameSession.PromptsPerDay;
            DayProgress.RememberDayNumber(day.date, day.dayNumber);
            int dayNumber = day.dayNumber > 0 ? day.dayNumber : DayProgress.EstimateDayNumber(day.date);
            screens.ShowHangar(dayNumber, day.date, day.progress, total, Mathf.RoundToInt(MockGameSession.SecondsPerPrompt));
        }, FailThen(OpenHangar));
    }

    private void ShowHangarFromInfo()
    {
        State = GameState.Hangar;
        int total = info.progress != null && info.progress.total > 0 ? info.progress.total : MockGameSession.PromptsPerDay;
        screens.ShowHangar(info.dayNumber, info.date, info.progress, total, info.secondsPerPrompt);
    }

    public void BeginDay()
    {
        if (State != GameState.Hangar || busy) return;
        screens.HideHangar();
        StartSession(RequestNext);
    }

    // play/start opens today's game or resumes it. Its prompt list is the server's word on the finished
    // questions, so the rows are rebuilt from it (answer texts from this device's memo, display only).
    private void StartSession(Action then)
    {
        busy = true;
        session.Start(i =>
        {
            busy = false;
            info = i;
            DayProgress.RememberDayNumber(i.date, i.dayNumber);
            memo = AnswerMemo.Load(i.date);
            rows.Clear();
            rows.AddRange(DayProgress.RowsFrom(i.prompts, memo));
            Score = i.progress != null ? i.progress.provisionalTotal : DayProgress.Total(rows);
            if (i.status == "finished")
            {
                ShowResult();
                return;
            }
            then();
        }, FailThen(() => StartSession(then)));
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
        }, FailThen(RequestNext));
    }

    private void ShowPrompt(PromptView view)
    {
        prompt = view;
        State = view.state == "suggested" ? GameState.Suggestion : GameState.Question;
        frozen = view.frozenSecondsLeft >= 0f;
        frozenLeft = view.frozenSecondsLeft;
        deadline = Time.realtimeSinceStartup + view.secondsLeft;
        ui.ShowQuestion(view, Score, SecondsPerPrompt);
        if (State == GameState.Suggestion)
        {
            ui.SetAnswering(false);
            screens.ShowSuggestion(view.suggestion, view.typed, frozen);
        }
    }

    private float SecondsPerPrompt
    {
        get { return info != null ? info.secondsPerPrompt : MockGameSession.SecondsPerPrompt; }
    }

    public void SubmitAnswer(string text)
    {
        if (State != GameState.Question || busy || string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        SendAnswer(text);
    }

    private void SendAnswer(string text)
    {
        busy = true;
        session.Answer(prompt.promptId, text, result => { busy = false; HandleAnswer(result); }, FailThen(() =>
        {
            ReopenQuestion();
            SendAnswer(text);
        }));
    }

    private void ConfirmSuggestion(bool yes)
    {
        if (State != GameState.Suggestion || busy) return;
        screens.HideSuggestion();
        SendConfirm(yes);
    }

    private void SendConfirm(bool yes)
    {
        busy = true;
        session.Confirm(prompt.promptId, yes, result => { busy = false; HandleAnswer(result); }, FailThen(() =>
        {
            ReopenQuestion();
            State = GameState.Suggestion;
            ui.SetAnswering(false);
            SendConfirm(yes);
        }));
    }

    // After the error card: the question card and the answer bar come back with the clock where it was.
    private void ReopenQuestion()
    {
        State = GameState.Question;
        ui.ShowQuestion(prompt, Score, SecondsPerPrompt);
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
        Announce(BridgeMessages.StopAnnouncement(stop, points, onlyYou));
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
        int position = prompt != null ? prompt.position : rows.Count + 1;
        DayProgress.Put(rows, new DayRow
        {
            Position = position,
            Prompt = prompt != null ? prompt.text : string.Empty,
            Answer = answer,
            Stop = stop,
            Points = points,
            OnlyYou = onlyYou,
        });
        if (!string.IsNullOrEmpty(answer) && info != null)
        {
            if (memo == null) memo = AnswerMemo.Load(info.date);
            memo.Set(position, answer);
            memo.Save();
        }
    }

    // ---------- result ----------

    private void ShowResult()
    {
        State = GameState.Result;
        ui.HideQuiz();
        ui.SetHud(false);
        ui.SetHeader(false);
        int streak = UpdateStreak();
        int total = DayProgress.Total(rows);
        if (info != null && info.progress != null && rows.Count == 0) total = info.progress.provisionalTotal;
        Score = total;
        screens.ShowResult(rows.ToArray(), total, info != null ? info.dayNumber : 0, info != null ? info.date : string.Empty, streak);
        Announce(BridgeMessages.ResultAnnouncement(total));
    }

    // The page (Nadide host or the template) writes the share text and card; without one, the old copy text.
    private void Share()
    {
        DayRow[] day = rows.ToArray();
        int dayNumber = info != null ? info.dayNumber : 0;
        string payload = BridgeMessages.SharePayload(info != null ? info.date : string.Empty, dayNumber, Score, day);
        if (bridge != null && bridge.Share(payload)) return;
        GUIUtility.systemCopyBuffer = GameScreens.ShareText(day, Score, dayNumber);
        screens.SetShareLabel("KOPYALANDI");
    }

    private void OnShareResult(string result)
    {
        string label = BridgeMessages.ShareResultLabel(result);
        if (label != null) screens.FlashShareLabel(label, ShareLabelSeconds);
    }

    private void Announce(string text)
    {
        if (bridge != null) bridge.Announce(text);
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

    // ---------- errors ----------

    private Action<string> FailThen(Action again)
    {
        return message => ShowError(message, again);
    }

    private void ShowError(string message, Action again)
    {
        busy = false;
        var nadide = session as NadideGameSession;
        LastFailure = NadideErrors.Classify(nadide != null ? nadide.LastErrorCode : null);
        Debug.LogWarning("Game session error (" + LastFailure + "): " + message);
        retry = LastFailure == SessionFailure.Restart ? (Action)RestartDay : again;
        if (LastFailure == SessionFailure.BetaRequired && bridge != null) bridge.BetaRequired();

        State = GameState.Error;
        ui.SetAnswering(false);
        if (bridge != null) bridge.HideAnswer();
        screens.HideSuggestion();
        string title = LastFailure == SessionFailure.BetaRequired ? "KAPALI BETA"
            : LastFailure == SessionFailure.Restart ? "OTURUM KAPANDI" : "BAĞLANTI KOPTU";
        screens.ShowError(title, message);
    }

    private void Retry()
    {
        if (State != GameState.Error) return;
        screens.HideMoment();
        Action again = retry;
        retry = null;
        State = GameState.Boot;
        if (again != null) again();
        else OpenHangar();
    }

    // The session is gone (expired, finished elsewhere, a new day): back to the Hangar with a fresh read.
    private void RestartDay()
    {
        info = null;
        prompt = null;
        memo = null;
        rows.Clear();
        Score = 0;
        ui.HideQuiz();
        ui.SetHud(false);
        ui.SetHeader(false);
        screens.HideAll();
        director.ResetToPad();
        OpenHangar();
    }
}
