using System;
using System.Collections.Generic;
using UnityEngine;

// Offline stand-in for Nadide's server (Editor, tests): today's 5 prompts from the Editor-only
// Assets/Editor/RocketQuiz/questions.json (players get a placeholder), a 30 s clock, rejection with retries (0018), one frozen
// "Bunu mu demek istediniz?" per prompt, and a provisional stop from the sample shares.
public class MockGameSession : IGameSession
{
    public const int PromptsPerDay = 5;
    public const float SecondsPerPrompt = 30f;
    public const int MaxAttempts = 20;

    // Editor/debug switch (or PlayerPrefs "rq.finalDemo" = 1): serve a sample "yesterday" for the final reveal.
    public static bool FinalDemo;

    private readonly QuizQuestion[] prompts;
    private readonly float seconds;
    private readonly DateTime today;
    private readonly ProgressView progress = new ProgressView();
    private readonly List<string> attempts = new List<string>();
    private readonly HashSet<string> tried = new HashSet<string>();
    private readonly HashSet<string> declined = new HashSet<string>();
    // Settled prompts in order (score + match kind), for the resume list Start returns.
    private readonly List<PromptProgress> settled = new List<PromptProgress>();

    private bool open;
    private float shownAt;
    private bool frozen;
    private bool freezeUsed;
    private float frozenLeft;
    private QuizAnswer pending;
    private string pendingTyped;

    public MockGameSession(bool shuffle = true, float secondsPerPrompt = SecondsPerPrompt)
    {
        seconds = secondsPerPrompt;
        today = DateTime.Today;
        QuizQuestion[] all = QuizLibrary.Load().questions;
        var pool = new List<QuizQuestion>(all);
        if (shuffle)
        {
            // Same set for the whole day, like the real game.
            var random = new System.Random(today.Year * 1000 + today.DayOfYear);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                QuizQuestion swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }
        }
        prompts = pool.GetRange(0, Mathf.Min(PromptsPerDay, pool.Count)).ToArray();
        progress.total = prompts.Length;
    }

    public QuizQuestion CurrentQuestion
    {
        get { return progress.answered < prompts.Length ? prompts[progress.answered] : null; }
    }

    public float TimeLeft
    {
        get { return frozen ? frozenLeft : Mathf.Max(0f, seconds - (Time.realtimeSinceStartup - shownAt)); }
    }

    private int DayNumber
    {
        get { return (today - new DateTime(2026, 1, 1)).Days + 1; }
    }

    public void State(Action<PlayState> done, Action<string> fail)
    {
        bool started = open || progress.answered > 0;
        done(new PlayState
        {
            status = !started ? "not_started" : progress.finished ? "finished" : "in_progress",
            date = today.ToString("yyyy-MM-dd"),
            dayNumber = DayNumber,
            progress = started ? Copy(progress) : null,
            current = open ? View() : null,
        });
    }

    public void Start(Action<SessionInfo> done, Action<string> fail)
    {
        var list = new List<PromptProgress>(settled);
        for (int i = settled.Count; i < prompts.Length; i++)
        {
            string state = i == settled.Count && open ? (pending != null ? "suggested" : "open") : "pending";
            list.Add(new PromptProgress { promptId = (i + 1).ToString(), position = i + 1, state = state });
        }
        done(new SessionInfo
        {
            dayNumber = DayNumber,
            date = today.ToString("yyyy-MM-dd"),
            status = progress.finished ? "finished" : "in_progress",
            resumed = progress.answered > 0,
            secondsPerPrompt = Mathf.RoundToInt(seconds),
            progress = Copy(progress),
            prompts = list.ToArray(),
        });
    }

    public void Next(Action<NextResult> done, Action<string> fail)
    {
        bool timedOut = false;
        if (open && TimeLeft <= 0f)
        {
            CloseTimeout();
            timedOut = true;
        }
        if (!open)
        {
            if (progress.answered >= prompts.Length)
            {
                progress.finished = true;
                done(new NextResult { status = "finished", progress = Copy(progress), timedOut = timedOut });
                return;
            }
            OpenPrompt();
        }
        done(new NextResult { status = "prompt", prompt = View(), progress = Copy(progress), timedOut = timedOut });
    }

    public void Answer(string promptId, string text, Action<AnswerResult> done, Action<string> fail)
    {
        if (!open)
        {
            done(Closed());
            return;
        }
        if (TimeLeft <= 0f)
        {
            CloseTimeout();
            done(Result("closed", "timeout", new ScoreView { status = "zero" }, null));
            return;
        }

        string key = AnswerMatcher.Normalize(text);
        if (key.Length == 0 || tried.Contains(key))
        {
            // Same form again does not spend an attempt.
            done(Result("rejected", "rejected", null, null));
            return;
        }
        if (MaxAttempts - attempts.Count <= 0)
        {
            done(Result("rejected", "rejected", null, null));
            return;
        }

        bool exact;
        QuizQuestion question = CurrentQuestion;
        QuizAnswer answer = AnswerMatcher.Evaluate(question, text, out exact);
        if (answer != null && exact)
        {
            done(Accept(answer, "exact"));
            return;
        }
        if (answer != null && !declined.Contains(key))
        {
            pending = answer;
            pendingTyped = text.Trim();
            if (!freezeUsed)
            {
                freezeUsed = true;
                frozenLeft = TimeLeft;
                frozen = true;
            }
            AnswerResult suggested = Result("suggested", "near", null, null);
            suggested.match.suggestion = answer.text;
            done(suggested);
            return;
        }

        attempts.Add(text.Trim());
        tried.Add(key);
        done(Result("rejected", "rejected", null, null));
    }

    public void Confirm(string promptId, bool yes, Action<AnswerResult> done, Action<string> fail)
    {
        if (!open || pending == null)
        {
            done(Closed());
            return;
        }
        QuizAnswer answer = pending;
        string typed = pendingTyped;
        pending = null;
        Unfreeze();
        if (yes)
        {
            done(Accept(answer, "confirmed"));
            return;
        }
        string key = AnswerMatcher.Normalize(typed);
        declined.Add(key);
        tried.Add(key);
        attempts.Add(typed);
        done(Result("rejected", "rejected", null, null));
    }

    public void Final(Action<FinalDay> done, Action<string> fail)
    {
        if (!FinalDemo && PlayerPrefs.GetInt("rq.finalDemo", 0) == 0)
        {
            done(null);
            return;
        }
        DateTime yesterday = today.AddDays(-1);
        done(new FinalDay
        {
            dayNumber = (yesterday - new DateTime(2026, 1, 1)).Days + 1,
            date = yesterday.ToString("yyyy-MM-dd"),
            provisionalTotal = 185,
            finalTotal = 200,
            prompts = new[]
            {
                new FinalPrompt { position = 1, text = "Bir Avrupa başkenti söyle.", answer = "Vaduz", provisionalTier = "jupiter", finalTier = "saturn", provisionalPoints = 40, finalPoints = 55, onlyYou = true,
                    commonAnswers = new[] { "Paris", "Londra", "Roma" }, commonShares = new[] { 22f, 14f, 9f } },
                new FinalPrompt { position = 2, text = "Bir takımyıldızı söyle.", answer = "Avcı", provisionalTier = "earth", finalTier = "earth", provisionalPoints = 15, finalPoints = 15,
                    commonAnswers = new[] { "Büyükayı", "Avcı", "Kuğu" }, commonShares = new[] { 31f, 8f, 6f } },
            },
        });
    }

    // ---------- internals ----------

    private void OpenPrompt()
    {
        open = true;
        shownAt = Time.realtimeSinceStartup;
        frozen = false;
        freezeUsed = false;
        pending = null;
        attempts.Clear();
        tried.Clear();
        declined.Clear();
    }

    private void Unfreeze()
    {
        if (!frozen) return;
        frozen = false;
        shownAt = Time.realtimeSinceStartup - (seconds - frozenLeft);
    }

    private void CloseTimeout()
    {
        open = false;
        pending = null;
        Settle("timeout", new ScoreView { status = "zero" });
        progress.answered++;
    }

    private void Settle(string kind, ScoreView score)
    {
        int position = settled.Count + 1;
        settled.Add(new PromptProgress
        {
            promptId = position.ToString(),
            position = position,
            state = "answered",
            match = new MatchView { kind = kind },
            score = new ScoreView { status = score.status, tier = score.tier, points = score.points, onlyYou = score.onlyYou },
        });
    }

    private AnswerResult Accept(QuizAnswer answer, string kind)
    {
        StopId ladder = RqStops.FromShare(answer.percent);
        StopId stop = answer.trap ? StopId.Moon : ladder;
        int points = stop == StopId.Moon ? Mathf.Min(15, RqStops.LadderPoints(ladder)) : RqStops.LadderPoints(stop);
        open = false;
        progress.answered++;
        progress.provisionalTotal += points;
        progress.finished = progress.answered >= prompts.Length;
        var score = new ScoreView { status = "provisional", tier = RqStops.Id(stop), points = points, onlyYou = answer.percent < 0.2f };
        Settle(kind, score);
        AnswerResult result = Result("accepted", kind, score, answer.text);
        return result;
    }

    private AnswerResult Closed()
    {
        return Result("closed", "timeout", new ScoreView { status = "zero" }, null);
    }

    private AnswerResult Result(string status, string kind, ScoreView score, string answer)
    {
        return new AnswerResult
        {
            promptId = (progress.answered + (open ? 1 : 0)).ToString(),
            status = status,
            match = new MatchView { kind = kind },
            score = score,
            progress = Copy(progress),
            attemptsLeft = MaxAttempts - attempts.Count,
            prompt = open ? View() : null,
            answer = answer,
        };
    }

    private PromptView View()
    {
        QuizQuestion question = CurrentQuestion;
        return new PromptView
        {
            promptId = (progress.answered + 1).ToString(),
            position = progress.answered + 1,
            total = prompts.Length,
            text = question.prompt,
            example = string.IsNullOrEmpty(question.example) ? null : question.example,
            state = pending != null ? "suggested" : "open",
            secondsLeft = TimeLeft,
            suggestion = pending != null ? pending.text : null,
            typed = pendingTyped,
            frozenSecondsLeft = frozen ? frozenLeft : -1f,
            attempts = attempts.ToArray(),
            attemptsLeft = MaxAttempts - attempts.Count,
        };
    }

    private static ProgressView Copy(ProgressView p)
    {
        return new ProgressView { answered = p.answered, total = p.total, finished = p.finished, provisionalTotal = p.provisionalTotal };
    }
}
