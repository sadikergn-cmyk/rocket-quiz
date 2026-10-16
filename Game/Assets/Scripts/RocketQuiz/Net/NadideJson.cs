using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

// JSON <-> DTO for the Nadide API (apps/web/src/server/contracts.ts). JsonUtility cannot express null for numbers,
// strings or nested objects, so every parse is followed by a normalisation that restores the contract's nulls:
// `frozenSecondsLeft: null` -> -1, empty optional strings -> null, absent objects -> null by status.
public static class NadideJson
{
    private static readonly Regex FrozenNull = new Regex("\"frozenSecondsLeft\"\\s*:\\s*null");

    // ---------- requests ----------

    public static string StartBody(string lang)
    {
        return JsonUtility.ToJson(new StartRequestWire { lang = lang });
    }

    public static string TokenBody(string token)
    {
        return JsonUtility.ToJson(new TokenRequestWire { token = token });
    }

    public static string AnswerBody(string token, string promptId, string answer)
    {
        return JsonUtility.ToJson(new AnswerRequestWire { token = token, promptId = promptId, answer = answer });
    }

    public static string ConfirmBody(string token, string promptId, bool confirm)
    {
        return JsonUtility.ToJson(new ConfirmRequestWire { token = token, promptId = promptId, confirm = confirm });
    }

    // ---------- responses (null when the body is not the expected JSON) ----------

    // POST /api/play/start -> SessionInfo; the session token stays with the caller.
    public static SessionInfo ParseStart(string json, out string token)
    {
        token = null;
        SessionInfo info = Read<SessionInfo>(json);
        StartWire wire = Read<StartWire>(json);
        if (info == null || wire == null || string.IsNullOrEmpty(wire.token)) return null;
        token = wire.token;
        info.prompts = NormalizePrompts(info.prompts);
        return info;
    }

    // GET /api/play/state -> PlayState (read-only; no token). `current` is the same view play/next returns.
    public static PlayState ParseState(string json)
    {
        PlayState state = Read<PlayState>(Prepare(json));
        if (state == null || string.IsNullOrEmpty(state.status)) return null;
        if (state.status == "not_started") state.progress = null;
        if (state.current == null || string.IsNullOrEmpty(state.current.promptId)) state.current = null;
        Normalize(state.current);
        return state;
    }

    // POST /api/play/next -> NextResult (`timedOut` is filled by the session, the server does not send it).
    public static NextResult ParseNext(string json)
    {
        NextResult result = Read<NextResult>(Prepare(json));
        if (result == null) return null;
        if (result.status != "prompt" || result.prompt == null || string.IsNullOrEmpty(result.prompt.promptId))
        {
            result.prompt = null;
        }
        Normalize(result.prompt);
        return result;
    }

    // POST /api/answer -> AnswerResult (`answer` is filled by the session).
    public static AnswerResult ParseAnswer(string json)
    {
        AnswerResult result = Read<AnswerResult>(Prepare(json));
        if (result == null) return null;
        // Not part of the server response; the session fills it for accepted answers.
        result.answer = null;
        bool open = result.status == "suggested" || result.status == "rejected";
        // Contract: score is null while the prompt is still open; prompt is only sent while it is open.
        if (open || result.score == null || string.IsNullOrEmpty(result.score.status)) result.score = null;
        if (!open || result.prompt == null || string.IsNullOrEmpty(result.prompt.promptId)) result.prompt = null;
        if (result.score != null && string.IsNullOrEmpty(result.score.tier)) result.score.tier = null;
        if (result.match != null) result.match.suggestion = Optional(result.match.suggestion);
        Normalize(result.prompt);
        return result;
    }

    // GET /api/result/:date (+ optional GET /api/reveal/:date for the common answers) -> FinalDay.
    public static FinalDay BuildFinal(string resultJson, string revealJson)
    {
        ResultWire result = Read<ResultWire>(resultJson);
        if (result == null || result.prompts == null) return null;
        RevealWire reveal = string.IsNullOrEmpty(revealJson) ? null : Read<RevealWire>(revealJson);

        var prompts = new FinalPrompt[result.prompts.Length];
        for (int i = 0; i < prompts.Length; i++)
        {
            ResultPromptWire p = result.prompts[i];
            var prompt = new FinalPrompt
            {
                position = p.position,
                text = p.text,
                // Display form of the matched answer; the raw typed text if there is none.
                answer = Optional(p.matched) ?? Optional(p.answer),
                provisionalTier = p.provisional != null ? Optional(p.provisional.tier) : null,
                finalTier = p.final != null ? Optional(p.final.tier) : null,
                provisionalPoints = p.provisional != null ? p.provisional.points : 0,
                finalPoints = p.final != null ? p.final.points : 0,
                onlyYou = p.onlyYou,
            };
            FillCommon(prompt, FindReveal(reveal, p), 3);
            prompts[i] = prompt;
        }
        return new FinalDay
        {
            dayNumber = result.dayNumber,
            date = result.date,
            provisionalTotal = result.provisionalTotal,
            finalTotal = result.finalTotal,
            prompts = prompts,
        };
    }

    // Any non-2xx (or missing) response -> error; null for success.
    public static NadideError ToError(NadideResponse response)
    {
        if (response == null || response.network) return new NadideError { network = true, code = "network" };
        long status = response.status;
        if (status >= 200 && status < 300) return null;
        // Gateway errors behave like a dropped connection (the endpoints are safe to resend).
        if (status == 502 || status == 503 || status == 504) return new NadideError { network = true, status = status, code = "network" };
        var error = new NadideError { status = status, code = "unknown" };
        ErrorWire wire = Read<ErrorWire>(response.body);
        if (wire != null && wire.error != null && !string.IsNullOrEmpty(wire.error.code))
        {
            error.code = wire.error.code;
            error.message = wire.error.message;
        }
        return error;
    }

    // Day before `gameDate` (yyyy-MM-dd), or before today in Europe/Istanbul (UTC+3, no DST) when unknown.
    public static string PreviousDay(string gameDate, DateTime utcNow)
    {
        DateTime day;
        if (string.IsNullOrEmpty(gameDate)
            || !DateTime.TryParseExact(gameDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
        {
            day = utcNow.AddHours(3).Date;
        }
        return day.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    // ---------- internals ----------

    private static string Prepare(string json)
    {
        return string.IsNullOrEmpty(json) ? json : FrozenNull.Replace(json, "\"frozenSecondsLeft\":-1");
    }

    private static T Read<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void Normalize(PromptView prompt)
    {
        if (prompt == null) return;
        prompt.example = Optional(prompt.example);
        prompt.suggestion = Optional(prompt.suggestion);
        prompt.typed = Optional(prompt.typed);
        if (prompt.attempts == null) prompt.attempts = new string[0];
        if (prompt.state != "suggested") prompt.frozenSecondsLeft = -1f;
    }

    // JsonUtility fills absent objects with empty instances; only answered prompts keep match and score.
    private static PromptProgress[] NormalizePrompts(PromptProgress[] prompts)
    {
        if (prompts == null) return new PromptProgress[0];
        var list = new List<PromptProgress>();
        foreach (PromptProgress p in prompts)
        {
            if (p == null || string.IsNullOrEmpty(p.promptId)) continue;
            bool answered = p.state == "answered";
            if (!answered || p.score == null || string.IsNullOrEmpty(p.score.status)) p.score = null;
            if (!answered || p.match == null || string.IsNullOrEmpty(p.match.kind)) p.match = null;
            if (p.score != null && string.IsNullOrEmpty(p.score.tier)) p.score.tier = null;
            if (p.match != null) p.match.suggestion = Optional(p.match.suggestion);
            list.Add(p);
        }
        list.Sort((a, b) => a.position.CompareTo(b.position));
        return list.ToArray();
    }

    private static string Optional(string value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static RevealPromptWire FindReveal(RevealWire reveal, ResultPromptWire prompt)
    {
        if (reveal == null || reveal.prompts == null) return null;
        foreach (RevealPromptWire candidate in reveal.prompts)
        {
            if (candidate.id == prompt.promptId) return candidate;
        }
        foreach (RevealPromptWire candidate in reveal.prompts)
        {
            if (candidate.position == prompt.position) return candidate;
        }
        return null;
    }

    // Most common answers with their final share as a percentage (server `share` is a 0-1 fraction).
    private static void FillCommon(FinalPrompt prompt, RevealPromptWire reveal, int count)
    {
        var list = new List<RevealAnswerWire>();
        if (reveal != null && reveal.answers != null)
        {
            foreach (RevealAnswerWire answer in reveal.answers)
            {
                if (answer.share > 0f || answer.count > 0f) list.Add(answer);
            }
        }
        list.Sort((a, b) => a.share != b.share ? b.share.CompareTo(a.share) : b.count.CompareTo(a.count));
        int n = Mathf.Min(count, list.Count);
        prompt.commonAnswers = new string[n];
        prompt.commonShares = new float[n];
        for (int i = 0; i < n; i++)
        {
            RevealAnswerWire answer = list[i];
            prompt.commonAnswers[i] = answer.forms != null && answer.forms.Length > 0 ? answer.forms[0] : answer.id;
            prompt.commonShares[i] = (float)Math.Round(answer.share * 100f, 1);
        }
    }
}

public sealed class NadideError
{
    public bool network;
    public long status;
    // Server ErrorCode (contracts.ts), "beta_required" from the beta gate, "network" or "unknown".
    public string code;
    public string message;
}

// What "Tekrar dene" does after a failed call: resend the same call, reopen the day from the Hangar, or (closed
// beta) ask the web shell for the access code first and then resend.
public enum SessionFailure
{
    Retry,
    Restart,
    BetaRequired,
}

// Turkish, player-facing texts for the session's fail callback.
public static class NadideErrors
{
    public const string Network = "Bağlantı kurulamadı. İnternet bağlantını kontrol edip yeniden dene.";
    public const string Server = "Sunucuda bir sorun var. Biraz sonra yeniden dene.";
    public const string BadResponse = "Sunucunun yanıtı okunamadı. Biraz sonra yeniden dene.";
    public const string SessionExpired = "Oturumun süresi doldu. Günün oyununu yeniden başlat.";
    public const string InvalidSession = "Oturum bulunamadı. Oyunu yeniden başlat.";
    public const string BetaRequired = "Kapalı beta: önce erişim kodunu gir.";

    // Error code (NadideGameSession.LastErrorCode; null for the offline mock) -> what the retry button does.
    // 401 `beta_required` comes from the beta gate (apps/web/src/app/beta/gate.ts) on every /api route.
    public static SessionFailure Classify(string code)
    {
        switch (code)
        {
            case "beta_required":
                return SessionFailure.BetaRequired;
            case "session_expired":
            case "invalid_session":
            case "play_finished":
            case "unknown_prompt":
            case "prompt_not_shown":
            case "no_suggestion":
                return SessionFailure.Restart;
            default:
                return SessionFailure.Retry;
        }
    }

    public static string UserMessage(NadideError error)
    {
        if (error == null) return BadResponse;
        if (error.network) return Network;
        switch (error.code)
        {
            case "session_expired": return SessionExpired;
            case "invalid_session": return InvalidSession;
            case "beta_required": return BetaRequired;
            case "play_finished": return "Bugünün oyunu bitti. Yeni sorular gece yarısı geliyor.";
            case "no_content": return "Bugün için soru bulunamadı.";
            case "prompt_not_shown": return "Soru henüz açılmadı. Yeniden dene.";
            case "no_suggestion": return "Onaylanacak bir öneri kalmadı.";
            case "unknown_prompt": return "Bu soru bu oyunda yok. Oyunu yeniden başlat.";
            case "too_many_attempts": return "Bu soruda deneme hakkın bitti.";
            case "attempt_too_fast": return "Biraz yavaş: bir saniye sonra yeniden dene.";
        }
        if (error.status >= 500) return Server;
        if (error.code == "unknown") return error.status == 404 ? "Oyun sunucusu bulunamadı." : BadResponse;
        // Server messages are already Turkish (e.g. "Cevap en çok 80 karakter olabilir.").
        return !string.IsNullOrEmpty(error.message) ? error.message : "Beklenmeyen bir hata oluştu.";
    }
}

// ---------- wire shapes that have no counterpart in GameSession.cs ----------

[Serializable]
public class StartRequestWire
{
    public string lang;
}

[Serializable]
public class TokenRequestWire
{
    public string token;
}

[Serializable]
public class AnswerRequestWire
{
    public string token;
    public string promptId;
    public string answer;
}

[Serializable]
public class ConfirmRequestWire
{
    public string token;
    public string promptId;
    public bool confirm;
}

// PlayStartResponse fields SessionInfo does not carry.
[Serializable]
public class StartWire
{
    public string token;
    public string expiresAt;
    public string ladder;
}

[Serializable]
public class ErrorWire
{
    public ErrorDetailWire error;
}

[Serializable]
public class ErrorDetailWire
{
    public string code;
    public string message;
}

// GET /api/result/:date (ResultResponse).
[Serializable]
public class ResultWire
{
    public string date;
    public int dayNumber;
    public ResultPromptWire[] prompts;
    public int provisionalTotal;
    public int finalTotal;
    public int delta;
}

[Serializable]
public class ResultPromptWire
{
    public string promptId;
    public int position;
    public string text;
    public string answer;
    public string matched;
    public TierPointsWire provisional;
    public TierPointsWire final;
    public bool onlyYou;
}

[Serializable]
public class TierPointsWire
{
    public string tier;
    public int points;
}

// GET /api/reveal/:date (RevealResponse).
[Serializable]
public class RevealWire
{
    public string date;
    public int dayNumber;
    public RevealPromptWire[] prompts;
}

[Serializable]
public class RevealPromptWire
{
    public string id;
    public int position;
    public string text;
    public RevealAnswerWire[] answers;
}

[Serializable]
public class RevealAnswerWire
{
    public string id;
    public string[] forms;
    public string tier;
    public float share;
    public float count;
}
