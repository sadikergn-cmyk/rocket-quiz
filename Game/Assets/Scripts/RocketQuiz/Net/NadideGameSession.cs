using System;
using UnityEngine;

// IGameSession over Nadide's HTTP API (daily game, planet ladder):
//   Start   POST /api/play/start {lang}                 -> SessionInfo (token kept here)
//   Next    POST /api/play/next  {token}                -> NextResult
//   Answer  POST /api/answer     {token,promptId,answer}  -> AnswerResult
//   Confirm POST /api/answer     {token,promptId,confirm} -> AnswerResult
//   Final   GET  /api/result/<yesterday>?lang + GET /api/reveal/<yesterday>?lang -> FinalDay or null
// The guest is the `nadide_device` cookie (browser in WebGL, NadideCookieJar elsewhere). All endpoints are safe to
// resend, so dropped connections are retried twice; a lost token (invalid_session) re-runs play/start once.
public sealed class NadideGameSession : IGameSession
{
    public const string FinalSeenKey = "rq.nadide.finalSeen";

    private static readonly float[] NetworkRetryDelays = { 0.4f, 1.2f };
    private const float TooFastDelay = 0.5f;
    // The server closes a prompt 3 s after its deadline (network grace); poll until it does.
    private const float GraceWaitSeconds = 1f;
    private const int MaxGraceWaits = 6;

    private readonly INadideTransport transport;
    private readonly string lang;

    private string token;
    private string date;
    private string openPromptId;
    private string pendingSuggestion;
    private PromptView lastPrompt;
    private ProgressView lastProgress;

    public NadideGameSession() : this(new NadideTransport(), NadideConfig.Lang)
    {
    }

    public NadideGameSession(INadideTransport transport, string lang)
    {
        this.transport = transport;
        this.lang = string.IsNullOrEmpty(lang) ? "tr" : lang;
        UtcNow = () => DateTime.UtcNow;
    }

    // Server error code of the last failed call ("network", "session_expired", ...); null after a success.
    public string LastErrorCode { get; private set; }
    public string Token { get { return token; } }
    public string Date { get { return date; } }
    public Func<DateTime> UtcNow { get; set; }

    public void Start(Action<SessionInfo> done, Action<string> fail)
    {
        Send(new Call
        {
            Method = "POST",
            Path = "/api/play/start",
            Body = () => NadideJson.StartBody(lang),
            Fail = fail,
            Ok = body =>
            {
                string newToken;
                SessionInfo info = NadideJson.ParseStart(body, out newToken);
                if (info == null)
                {
                    fail(NadideErrors.BadResponse);
                    return;
                }
                token = newToken;
                date = info.date;
                lastProgress = info.progress;
                openPromptId = null;
                pendingSuggestion = null;
                done(info);
            },
        });
    }

    public void Next(Action<NextResult> done, Action<string> fail)
    {
        NextAttempt(0, done, fail);
    }

    public void Answer(string promptId, string text, Action<AnswerResult> done, Action<string> fail)
    {
        string typed = text == null ? string.Empty : text.Trim();
        Send(new Call
        {
            Method = "POST",
            Path = "/api/answer",
            Body = () => NadideJson.AnswerBody(token, promptId, typed),
            NeedsToken = true,
            Fail = fail,
            Ok = body => Deliver(NadideJson.ParseAnswer(body), typed, done, fail),
            Handle = error => OutOfAttempts(error, promptId, done),
        });
    }

    public void Confirm(string promptId, bool yes, Action<AnswerResult> done, Action<string> fail)
    {
        Send(new Call
        {
            Method = "POST",
            Path = "/api/answer",
            Body = () => NadideJson.ConfirmBody(token, promptId, yes),
            NeedsToken = true,
            Fail = fail,
            Ok = body => Deliver(NadideJson.ParseAnswer(body), null, done, fail),
            Handle = error => OutOfAttempts(error, promptId, done),
        });
    }

    // Yesterday's closed day, once per day. "Not closed yet", "did not play" and "no content" all mean null.
    public void Final(Action<FinalDay> done, Action<string> fail)
    {
        string day = NadideJson.PreviousDay(date, UtcNow());
        if (PlayerPrefs.GetString(FinalSeenKey, string.Empty) == day)
        {
            done(null);
            return;
        }
        string query = "?lang=" + Uri.EscapeDataString(lang);
        Send(new Call
        {
            Method = "GET",
            Path = "/api/result/" + day + query,
            Fail = fail,
            Handle = error =>
            {
                done(null);
                return true;
            },
            Ok = resultBody => Send(new Call
            {
                Method = "GET",
                Path = "/api/reveal/" + day + query,
                Fail = fail,
                Ok = revealBody => DeliverFinal(day, resultBody, revealBody, done, fail),
                // Without the reveal the card still shows the player's own day.
                Handle = error =>
                {
                    DeliverFinal(day, resultBody, null, done, fail);
                    return true;
                },
            }),
        });
    }

    // ---------- flow ----------

    private void NextAttempt(int waits, Action<NextResult> done, Action<string> fail)
    {
        Send(new Call
        {
            Method = "POST",
            Path = "/api/play/next",
            Body = () => NadideJson.TokenBody(token),
            NeedsToken = true,
            Fail = fail,
            Ok = body =>
            {
                NextResult result = NadideJson.ParseNext(body);
                if (result == null)
                {
                    fail(NadideErrors.BadResponse);
                    return;
                }
                bool samePrompt = result.prompt != null && openPromptId != null && result.prompt.promptId == openPromptId;
                if (samePrompt && waits < MaxGraceWaits && ClockRanOut(result.prompt))
                {
                    // The local timer hit 0 but the server is still inside its grace window.
                    transport.Delay(GraceWaitSeconds, () => NextAttempt(waits + 1, done, fail));
                    return;
                }
                // The server does not say it; a prompt we still had open that is gone now closed by timeout.
                result.timedOut = openPromptId != null && !samePrompt;
                if (result.progress != null) lastProgress = result.progress;
                if (result.prompt != null)
                {
                    Track(result.prompt);
                }
                else
                {
                    openPromptId = null;
                    pendingSuggestion = null;
                }
                done(result);
            },
        });
    }

    private void Deliver(AnswerResult result, string typed, Action<AnswerResult> done, Action<string> fail)
    {
        if (result == null)
        {
            fail(NadideErrors.BadResponse);
            return;
        }
        switch (result.status)
        {
            case "accepted":
                bool confirmed = result.match != null && result.match.kind == "confirmed";
                string promptTyped = lastPrompt != null ? lastPrompt.typed : null;
                // Shown on the stop card: the confirmed spelling, else what the player typed.
                result.answer = confirmed
                    ? FirstOf(pendingSuggestion, typed, promptTyped)
                    : FirstOf(typed, pendingSuggestion, promptTyped);
                openPromptId = null;
                pendingSuggestion = null;
                break;
            case "suggested":
                if (result.prompt != null) Track(result.prompt);
                if (result.match != null && result.match.suggestion != null) pendingSuggestion = result.match.suggestion;
                break;
            case "rejected":
                pendingSuggestion = null;
                if (result.prompt != null) Track(result.prompt);
                break;
            default:
                // "closed": the prompt ended without an accepted answer (timeout).
                openPromptId = null;
                pendingSuggestion = null;
                break;
        }
        if (result.progress != null) lastProgress = result.progress;
        done(result);
    }

    private void DeliverFinal(string day, string resultBody, string revealBody, Action<FinalDay> done, Action<string> fail)
    {
        FinalDay final = NadideJson.BuildFinal(resultBody, revealBody);
        if (final == null)
        {
            fail(NadideErrors.BadResponse);
            return;
        }
        PlayerPrefs.SetString(FinalSeenKey, day);
        PlayerPrefs.Save();
        done(final);
    }

    // 429 too_many_attempts keeps the prompt open; report it like the mock does, as a rejection with no tries left.
    private bool OutOfAttempts(NadideError error, string promptId, Action<AnswerResult> done)
    {
        if (error.code != "too_many_attempts") return false;
        if (lastPrompt != null) lastPrompt.attemptsLeft = 0;
        done(new AnswerResult
        {
            promptId = promptId,
            status = "rejected",
            match = new MatchView { kind = "rejected" },
            score = null,
            progress = lastProgress,
            attemptsLeft = 0,
            prompt = lastPrompt,
        });
        return true;
    }

    private void Track(PromptView prompt)
    {
        lastPrompt = prompt;
        openPromptId = prompt.promptId;
        if (prompt.state == "suggested" && prompt.suggestion != null) pendingSuggestion = prompt.suggestion;
    }

    private static bool ClockRanOut(PromptView prompt)
    {
        return prompt.secondsLeft <= 0f && prompt.frozenSecondsLeft < 0f;
    }

    private static string FirstOf(string a, string b, string c)
    {
        if (!string.IsNullOrEmpty(a)) return a;
        if (!string.IsNullOrEmpty(b)) return b;
        return c;
    }

    // ---------- HTTP with retries ----------

    private sealed class Call
    {
        public string Method;
        public string Path;
        public Func<string> Body;
        public bool NeedsToken;
        public Action<string> Ok;
        // Returns true when it took care of the error (the fail callback is then skipped).
        public Func<NadideError, bool> Handle;
        public Action<string> Fail;
        public int NetworkTries;
        public bool Restarted;
        public bool RetriedTooFast;
    }

    private void Send(Call call)
    {
        if (call.NeedsToken && string.IsNullOrEmpty(token) && !call.Restarted)
        {
            call.Restarted = true;
            Restart(() => Send(call), call.Fail);
            return;
        }
        transport.Send(call.Method, call.Path, call.Body != null ? call.Body() : null, response =>
        {
            NadideError error = NadideJson.ToError(response);
            if (error == null)
            {
                LastErrorCode = null;
                call.Ok(response.body);
                return;
            }
            if (error.network && call.NetworkTries < NetworkRetryDelays.Length)
            {
                float delay = NetworkRetryDelays[call.NetworkTries];
                call.NetworkTries++;
                transport.Delay(delay, () => Send(call));
                return;
            }
            if (error.code == "attempt_too_fast" && !call.RetriedTooFast)
            {
                call.RetriedTooFast = true;
                transport.Delay(TooFastDelay, () => Send(call));
                return;
            }
            if (call.NeedsToken && error.code == "invalid_session" && !call.Restarted)
            {
                call.Restarted = true;
                call.NetworkTries = 0;
                Restart(() => Send(call), call.Fail);
                return;
            }
            if (error.code == "session_expired") token = null;
            LastErrorCode = error.code;
            if (call.Handle != null && !error.network && call.Handle(error)) return;
            Debug.LogWarning("[nadide] " + call.Method + " " + call.Path + " -> " + error.status + " " + error.code);
            call.Fail(NadideErrors.UserMessage(error));
        });
    }

    // New token for the same game (play/start resumes this device's game). If the game day has moved on, the old
    // session is over: report expiry instead of silently switching to a new day's prompts.
    private void Restart(Action then, Action<string> fail)
    {
        string previousDate = date;
        Send(new Call
        {
            Method = "POST",
            Path = "/api/play/start",
            Body = () => NadideJson.StartBody(lang),
            Fail = fail,
            Ok = body =>
            {
                string newToken;
                SessionInfo info = NadideJson.ParseStart(body, out newToken);
                if (info == null)
                {
                    fail(NadideErrors.BadResponse);
                    return;
                }
                if (!string.IsNullOrEmpty(previousDate) && info.date != previousDate)
                {
                    token = null;
                    LastErrorCode = "session_expired";
                    fail(NadideErrors.SessionExpired);
                    return;
                }
                token = newToken;
                date = info.date;
                then();
            },
        });
    }
}
