using System;
using System.Collections.Generic;
using NUnit.Framework;

// Bridge contract v1 (BridgeMessages: share payload, announcements, share result, reduced motion), GET /api/play/state,
// resuming from PlayStartResponse.prompts, and the beta gate's 401 beta_required. JSON is written with ' for ".
public class NadideBridgeTests
{
    private static string J(string single)
    {
        return single.Replace('\'', '"');
    }

    private static DayRow Row(int position, StopId stop, int points)
    {
        return new DayRow { Position = position, Stop = stop, Points = points };
    }

    // ---------- share payload ----------

    [Test]
    public void SharePayload_UsesServerTierIdsAndNullForTheSun()
    {
        var rows = new List<DayRow>
        {
            Row(1, StopId.Neptune, 100), Row(2, StopId.Sun, 0), Row(3, StopId.Moon, 15), Row(4, StopId.Mars, 25), Row(5, StopId.Mercury, 5),
        };
        string json = BridgeMessages.SharePayload("2026-10-15", 42, 145, rows);
        Assert.AreEqual(J("{'date':'2026-10-15','dayNumber':42,'total':145,'tiers':['neptune',null,'trap_moon','mars','mercury']}"), json);
    }

    [Test]
    public void SharePayload_KeepsAtMostFiveTiersAndEscapes()
    {
        var rows = new List<DayRow>();
        for (int i = 1; i <= 7; i++) rows.Add(Row(i, StopId.Earth, 15));
        string json = BridgeMessages.SharePayload("2026-10-15", 1, 105, rows);
        StringAssert.EndsWith(J("'tiers':['earth','earth','earth','earth','earth']}"), json);

        Assert.AreEqual(J("{'date':'a\\'b','dayNumber':0,'total':0,'tiers':[]}"), BridgeMessages.SharePayload("a\"b", 0, 0, new DayRow[0]));
        Assert.AreEqual(J("{'date':'','dayNumber':0,'total':0,'tiers':[]}"), BridgeMessages.SharePayload(null, 0, 0, null));
    }

    [Test]
    public void TierId_EveryStopButTheSun()
    {
        Assert.IsNull(BridgeMessages.TierId(StopId.Sun));
        Assert.AreEqual("mercury", BridgeMessages.TierId(StopId.Mercury));
        Assert.AreEqual("neptune", BridgeMessages.TierId(StopId.Neptune));
        Assert.AreEqual("trap_moon", BridgeMessages.TierId(StopId.Moon));
    }

    // ---------- announcements, share result, reduced motion ----------

    [Test]
    public void Announcements_AreShortTurkishSentencesWithoutDistances()
    {
        Assert.AreEqual("Neptün'e ulaştın, 100 puan.", BridgeMessages.StopAnnouncement(StopId.Neptune, 100, false));
        Assert.AreEqual("Dünya'ya ulaştın, 15 puan.", BridgeMessages.StopAnnouncement(StopId.Earth, 15, false));
        Assert.AreEqual("Mars'a ulaştın, 25 puan. Şu an bunu yazan tek kişi sensin.", BridgeMessages.StopAnnouncement(StopId.Mars, 25, true));
        Assert.AreEqual("Ay tuzağı: Dünya'nın yörüngesinde kaldın, 15 puan.", BridgeMessages.StopAnnouncement(StopId.Moon, 15, false));
        Assert.AreEqual("Süre doldu, Güneş'te kaldın, 0 puan.", BridgeMessages.StopAnnouncement(StopId.Sun, 0, true));
        StringAssert.StartsWith("Günün yolculuğu bitti, toplam 230 puan.", BridgeMessages.ResultAnnouncement(230));

        for (int s = 0; s <= (int)StopId.Moon; s++)
        {
            string text = BridgeMessages.StopAnnouncement((StopId)s, 40, false);
            foreach (string unit in new[] { "km", "AU", "ışık yılı", "irtifa" }) StringAssert.DoesNotContain(unit, text);
        }
    }

    [TestCase("shared", "PAYLAŞILDI")]
    [TestCase("copied", "KOPYALANDI")]
    [TestCase("cancelled", "PAYLAŞ")]
    [TestCase("failed", "PAYLAŞILAMADI")]
    [TestCase("bogus", null)]
    [TestCase(null, null)]
    public void ShareResult_MapsToButtonLabel(string result, string label)
    {
        Assert.AreEqual(label, BridgeMessages.ShareResultLabel(result));
    }

    [Test]
    public void ReducedMotionFlag_ParsesOneAndZero()
    {
        Assert.AreEqual(true, BridgeMessages.Flag("1"));
        Assert.AreEqual(false, BridgeMessages.Flag("0"));
        Assert.AreEqual(true, BridgeMessages.Flag(" true "));
        Assert.IsNull(BridgeMessages.Flag("yes"));
        Assert.IsNull(BridgeMessages.Flag(null));
    }

    // ---------- GET /api/play/state ----------

    [Test]
    public void State_NotStarted_HasNoProgressOrPrompt()
    {
        PlayState state = NadideJson.ParseState(J("{'status':'not_started','lang':'tr','date':'2026-10-15'}"));
        Assert.AreEqual("not_started", state.status);
        Assert.AreEqual("2026-10-15", state.date);
        Assert.IsNull(state.progress);
        Assert.IsNull(state.current);
    }

    [Test]
    public void State_InProgress_CarriesProgressAndTheOpenPrompt()
    {
        PlayState state = NadideJson.ParseState(J(
            "{'status':'in_progress','lang':'tr','date':'2026-10-15','progress':{'answered':2,'total':5,'finished':false,'provisionalTotal':55}," +
            "'current':{'promptId':'p3','position':3,'total':5,'text':'Bir il söyle.','example':'İzmir','state':'suggested'," +
            "'shownAt':'2026-10-15T09:00:00.000Z','deadline':'2026-10-15T09:00:51.000Z','secondsLeft':30,'suggestion':'Iğdır'," +
            "'typed':'ıgdir','frozenSecondsLeft':null,'attempts':['Van'],'attemptsLeft':19}}"));
        Assert.AreEqual("in_progress", state.status);
        Assert.AreEqual(2, state.progress.answered);
        Assert.AreEqual(55, state.progress.provisionalTotal);
        Assert.AreEqual("p3", state.current.promptId);
        Assert.AreEqual("suggested", state.current.state);
        Assert.AreEqual(-1f, state.current.frozenSecondsLeft, "null frozenSecondsLeft = clock running");
        Assert.AreEqual(new[] { "Van" }, state.current.attempts);
    }

    [Test]
    public void State_FinishedWithoutPrompt()
    {
        PlayState state = NadideJson.ParseState(J(
            "{'status':'finished','lang':'tr','date':'2026-10-15','progress':{'answered':5,'total':5,'finished':true,'provisionalTotal':130},'current':null}"));
        Assert.AreEqual("finished", state.status);
        Assert.IsTrue(state.progress.finished);
        Assert.IsNull(state.current);
        Assert.IsNull(NadideJson.ParseState("<html>"), "not JSON");
    }

    [Test]
    public void Session_State_IsAReadOnlyGetWithoutToken()
    {
        var http = new FakeTransport();
        var session = new NadideGameSession(http, "tr");
        http.Reply(200, "{'status':'not_started','lang':'tr','date':'2026-10-15'}");
        PlayState state = null;
        session.State(s => state = s, e => Assert.Fail(e));
        Assert.AreEqual("not_started", state.status);
        Assert.AreEqual(new[] { "GET /api/play/state?lang=tr " }, http.Calls.ToArray());
        Assert.IsNull(session.Token, "play/state opens no game");
    }

    // ---------- resume from PlayStartResponse.prompts ----------

    private const string ResumedStart =
        "{'token':'v1.sess.sig','lang':'tr','date':'2026-10-15','dayNumber':42,'mode':'daily','ladder':'planets'," +
        "'status':'in_progress','resumed':true,'startedAt':'2026-10-15T09:00:00.000Z','expiresAt':'2026-10-15T21:10:00.000Z'," +
        "'secondsPerPrompt':30,'promptIds':['p1','p2','p3','p4','p5'],'prompts':[" +
        "{'promptId':'p2','position':2,'state':'answered','match':{'kind':'timeout'},'score':{'status':'zero','points':0}}," +
        "{'promptId':'p1','position':1,'state':'answered','match':{'kind':'confirmed'}," +
        "'score':{'status':'provisional','ladder':'planets','tier':'neptune','points':100,'onlyYou':true}}," +
        "{'promptId':'p3','position':3,'state':'open','shownAt':'2026-10-15T09:02:00.000Z','deadline':'2026-10-15T09:02:30.000Z','secondsLeft':12}," +
        "{'promptId':'p4','position':4,'state':'pending'},{'promptId':'p5','position':5,'state':'pending'}]," +
        "'progress':{'answered':2,'total':5,'finished':false,'provisionalTotal':100}}";

    [Test]
    public void Start_ParsesPromptProgressForResume()
    {
        string token;
        SessionInfo info = NadideJson.ParseStart(J(ResumedStart), out token);
        Assert.IsTrue(info.resumed);
        Assert.AreEqual(5, info.prompts.Length);
        Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, Array.ConvertAll(info.prompts, p => p.position), "sorted by position");

        PromptProgress first = info.prompts[0];
        Assert.AreEqual("answered", first.state);
        Assert.AreEqual("confirmed", first.match.kind);
        Assert.AreEqual(StopId.Neptune, first.score.Stop);
        Assert.AreEqual(100, first.score.points);
        Assert.IsTrue(first.score.onlyYou);

        PromptProgress timedOut = info.prompts[1];
        Assert.AreEqual("zero", timedOut.score.status);
        Assert.IsNull(timedOut.score.tier);
        Assert.AreEqual(StopId.Sun, timedOut.score.Stop);

        Assert.AreEqual("open", info.prompts[2].state);
        Assert.IsNull(info.prompts[2].score, "only answered prompts carry a score");
        Assert.IsNull(info.prompts[2].match);
        Assert.IsNull(info.prompts[4].score);
    }

    [Test]
    public void Start_WithoutPrompts_GivesAnEmptyList()
    {
        string token;
        SessionInfo info = NadideJson.ParseStart(J("{'token':'t','date':'2026-10-15','dayNumber':1,'status':'in_progress'}"), out token);
        Assert.IsNotNull(info.prompts);
        Assert.AreEqual(0, info.prompts.Length);
    }

    [Test]
    public void Resume_RebuildsRowsWithRememberedAnswers()
    {
        string token;
        SessionInfo info = NadideJson.ParseStart(J(ResumedStart), out token);
        var memo = new AnswerMemo { date = "2026-10-15" };
        memo.Set(1, "Iğdır");
        List<DayRow> rows = DayProgress.RowsFrom(info.prompts, memo);

        Assert.AreEqual(2, rows.Count, "only the answered prompts");
        Assert.AreEqual(1, rows[0].Position);
        Assert.AreEqual("Iğdır", rows[0].Answer);
        Assert.AreEqual(StopId.Neptune, rows[0].Stop);
        Assert.AreEqual(100, rows[0].Points);
        Assert.IsTrue(rows[0].OnlyYou);
        Assert.AreEqual(2, rows[1].Position);
        Assert.IsNull(rows[1].Answer);
        Assert.AreEqual(StopId.Sun, rows[1].Stop);
        Assert.AreEqual(100, DayProgress.Total(rows));
        Assert.AreEqual(J("{'date':'2026-10-15','dayNumber':42,'total':100,'tiers':['neptune',null]}"),
            BridgeMessages.SharePayload(info.date, info.dayNumber, DayProgress.Total(rows), rows));
    }

    [Test]
    public void Rows_PutReplacesSamePositionAndKeepsOrder()
    {
        var rows = new List<DayRow>();
        DayProgress.Put(rows, Row(3, StopId.Mars, 25));
        DayProgress.Put(rows, Row(1, StopId.Venus, 10));
        DayProgress.Put(rows, Row(3, StopId.Saturn, 55));
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual(1, rows[0].Position);
        Assert.AreEqual(StopId.Saturn, rows[1].Stop);
        Assert.AreEqual(65, DayProgress.Total(rows));
    }

    [Test]
    public void AnswerMemo_IsPerDay()
    {
        var memo = new AnswerMemo { date = "2026-10-15" };
        memo.Set(2, "Vaduz");
        memo.Set(2, "Vaduz!");
        memo.Set(3, null);
        string json = UnityEngine.JsonUtility.ToJson(memo);
        Assert.AreEqual("Vaduz!", AnswerMemo.FromJson(json, "2026-10-15").Get(2));
        Assert.IsNull(AnswerMemo.FromJson(json, "2026-10-15").Get(3));
        Assert.IsNull(AnswerMemo.FromJson(json, "2026-10-16").Get(2), "another day starts empty");
        Assert.IsNull(AnswerMemo.FromJson("not json", "2026-10-15").Get(2));
    }

    [Test]
    public void DayNumber_EstimatedFromTheLastKnownDay()
    {
        Assert.AreEqual(43, DayProgress.EstimateDayNumber("2026-10-16", "2026-10-15", 42));
        Assert.AreEqual(42, DayProgress.EstimateDayNumber("2026-10-15", "2026-10-15", 42));
        Assert.AreEqual(0, DayProgress.EstimateDayNumber("2026-10-16", string.Empty, 0), "unknown before the first game");
        Assert.AreEqual(0, DayProgress.EstimateDayNumber(null, "2026-10-15", 42));
    }

    [Test]
    public void MockSession_StateAndResumeFollowTheContract()
    {
        var session = new MockGameSession(false);
        PlayState state = null;
        session.State(s => state = s, e => Assert.Fail(e));
        Assert.AreEqual("not_started", state.status);
        Assert.IsNull(state.progress);

        NextResult next = null;
        session.Start(i => { }, e => Assert.Fail(e));
        session.Next(n => next = n, e => Assert.Fail(e));
        session.State(s => state = s, e => Assert.Fail(e));
        Assert.AreEqual("in_progress", state.status);
        Assert.AreEqual(next.prompt.promptId, state.current.promptId);

        AnswerResult result = null;
        session.Answer(next.prompt.promptId, session.CurrentQuestion.answers[0].text, r => result = r, e => Assert.Fail(e));
        if (result.status == "suggested") session.Confirm(next.prompt.promptId, true, r => result = r, e => Assert.Fail(e));
        Assert.AreEqual("accepted", result.status);

        SessionInfo info = null;
        session.Start(i => info = i, e => Assert.Fail(e));
        Assert.AreEqual("answered", info.prompts[0].state);
        Assert.AreEqual(result.score.tier, info.prompts[0].score.tier);
        Assert.AreEqual(1, DayProgress.RowsFrom(info.prompts, null).Count);
    }

    // ---------- closed beta ----------

    private const string BetaRequired = "{'error':{'code':'beta_required','message':'Kapalı beta: önce erişim kodunu gir.'}}";

    [Test]
    public void BetaGate401_MapsToBetaRequired()
    {
        NadideError error = NadideJson.ToError(new NadideResponse { status = 401, body = J(BetaRequired) });
        Assert.IsFalse(error.network);
        Assert.AreEqual("beta_required", error.code);
        Assert.AreEqual(NadideErrors.BetaRequired, NadideErrors.UserMessage(error));
        Assert.AreEqual(SessionFailure.BetaRequired, NadideErrors.Classify(error.code));
    }

    [Test]
    public void Session_BetaRequired_FailsOnceWithTheCode()
    {
        var http = new FakeTransport();
        var session = new NadideGameSession(http, "tr");
        http.Reply(401, BetaRequired);
        string message = null;
        session.State(s => Assert.Fail("no state expected"), e => message = e);
        Assert.AreEqual(NadideErrors.BetaRequired, message);
        Assert.AreEqual("beta_required", session.LastErrorCode);
        Assert.AreEqual(1, http.Calls.Count, "not retried");
        Assert.AreEqual(0, http.Delays.Count);
    }

    [TestCase("network", SessionFailure.Retry)]
    [TestCase("internal_error", SessionFailure.Retry)]
    [TestCase(null, SessionFailure.Retry)]
    [TestCase("session_expired", SessionFailure.Restart)]
    [TestCase("invalid_session", SessionFailure.Restart)]
    [TestCase("play_finished", SessionFailure.Restart)]
    [TestCase("beta_required", SessionFailure.BetaRequired)]
    public void Failures_ClassifiedForTheRetryButton(string code, SessionFailure expected)
    {
        Assert.AreEqual(expected, NadideErrors.Classify(code));
    }

    private sealed class FakeTransport : INadideTransport
    {
        public readonly List<string> Calls = new List<string>();
        public readonly List<float> Delays = new List<float>();
        private readonly Queue<NadideResponse> responses = new Queue<NadideResponse>();

        public void Reply(int status, string json)
        {
            responses.Enqueue(new NadideResponse { status = status, body = J(json) });
        }

        public void Send(string method, string path, string jsonBody, Action<NadideResponse> done)
        {
            Calls.Add(method + " " + path + " " + jsonBody);
            done(responses.Count > 0 ? responses.Dequeue() : NadideResponse.Offline());
        }

        public void Delay(float seconds, Action then)
        {
            Delays.Add(seconds);
            then();
        }
    }
}
