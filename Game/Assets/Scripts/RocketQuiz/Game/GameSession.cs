using System;

// C# view of Nadide's play contract (apps/web/src/server/contracts.ts). Field names follow the
// JSON so a network session can deserialize responses straight into these classes.

[Serializable]
public class ProgressView
{
    public int answered;
    public int total;
    public bool finished;
    public int provisionalTotal;
}

[Serializable]
public class PromptView
{
    public string promptId;
    public int position;
    public int total;
    public string text;
    public string example;
    // "open" | "suggested"
    public string state;
    public float secondsLeft;
    public string suggestion;
    public string typed;
    // < 0 when the clock is running (only the first suggestion of a prompt freezes it).
    public float frozenSecondsLeft = -1f;
    public string[] attempts;
    public int attemptsLeft = 20;
}

[Serializable]
public class MatchView
{
    // "exact" | "near" | "confirmed" | "rejected" | "none" | "timeout"
    public string kind;
    public string suggestion;
}

[Serializable]
public class ScoreView
{
    // "provisional" | "zero" | "final"
    public string status;
    // Stop id: "mercury" ... "neptune", "trap_moon"; empty for zero.
    public string tier;
    public int points;
    public bool onlyYou;

    public StopId Stop
    {
        get { return status == "zero" || string.IsNullOrEmpty(tier) ? StopId.Sun : RqStops.FromId(tier); }
    }
}

[Serializable]
public class AnswerResult
{
    public string promptId;
    // "accepted" | "suggested" | "rejected" | "closed"
    public string status;
    public MatchView match;
    public ScoreView score;
    public ProgressView progress;
    public int attemptsLeft;
    public PromptView prompt;
    // Canonical answer the player is credited with (client-side display only; never a hidden answer).
    public string answer;
}

[Serializable]
public class SessionInfo
{
    public int dayNumber;
    public string date;
    // "in_progress" | "finished"
    public string status;
    public bool resumed;
    public int secondsPerPrompt = 30;
    public ProgressView progress;
}

[Serializable]
public class NextResult
{
    // "prompt" | "finished"
    public string status;
    public PromptView prompt;
    public ProgressView progress;
    // Set when the previous prompt closed by timeout on the server.
    public bool timedOut;
}

// Yesterday's closed day ("DÜNÜN YOLCULUĞU KESİNLEŞTİ").
[Serializable]
public class FinalDay
{
    public int dayNumber;
    public string date;
    public int provisionalTotal;
    public int finalTotal;
    public FinalPrompt[] prompts;
}

[Serializable]
public class FinalPrompt
{
    public int position;
    public string text;
    public string answer;
    public string provisionalTier;
    public string finalTier;
    public int provisionalPoints;
    public int finalPoints;
    public bool onlyYou;
    public string[] commonAnswers;
    public float[] commonShares;
}

// The game controller only talks to this. The Nadide network session lives in Net/ (P1);
// MockGameSession plays offline from the Editor-only Assets/Editor/RocketQuiz/questions.json.
public interface IGameSession
{
    void Start(Action<SessionInfo> done, Action<string> fail);
    void Next(Action<NextResult> done, Action<string> fail);
    void Answer(string promptId, string text, Action<AnswerResult> done, Action<string> fail);
    void Confirm(string promptId, bool yes, Action<AnswerResult> done, Action<string> fail);
    // Yesterday's final reveal, or null when there is none to show.
    void Final(Action<FinalDay> done, Action<string> fail);
}
