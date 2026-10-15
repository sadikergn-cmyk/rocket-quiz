using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Nadide API responses (shapes from apps/web/src/server/contracts.ts and the handlers that build them) parsed into
// the GameSession DTOs, plus the NadideGameSession flow over a scripted transport. JSON is written with ' for ".
public class NadideContractTests
{
    private const string Progress0 = "{'answered':0,'total':5,'finished':false,'provisionalTotal':0}";
    private const string Progress1 = "{'answered':1,'total':5,'finished':false,'provisionalTotal':25}";

    private const string Start =
        "{'token':'v1.sess.sig','lang':'tr','date':'2026-10-15','dayNumber':42,'mode':'daily','ladder':'planets'," +
        "'status':'in_progress','resumed':false,'startedAt':'2026-10-15T09:00:00.000Z','expiresAt':'2026-10-15T21:10:00.000Z'," +
        "'secondsPerPrompt':30,'promptIds':['p1','p2','p3','p4','p5'],'prompts':[{'promptId':'p1','position':1,'state':'pending'}]," +
        "'progress':" + Progress0 + "}";

    private static string OpenPrompt(string id, int position, int secondsLeft)
    {
        return "{'promptId':'" + id + "','position':" + position + ",'total':5,'text':'Bir il söyle.','example':'İzmir'," +
            "'state':'open','shownAt':'2026-10-15T09:00:00.000Z','deadline':'2026-10-15T09:00:30.000Z','secondsLeft':" + secondsLeft +
            ",'suggestion':null,'typed':null,'frozenSecondsLeft':null,'attempts':[],'attemptsLeft':20}";
    }

    private static string Next(string prompt)
    {
        return "{'status':'prompt','prompt':" + prompt + ",'progress':" + Progress0 + "}";
    }

    private static readonly string Suggested =
        "{'promptId':'p1','match':{'kind':'near','suggestion':'Iğdır'},'score':null,'progress':" + Progress0 + "," +
        "'status':'suggested','attemptsLeft':20,'prompt':{'promptId':'p1','position':1,'total':5,'text':'Bir il söyle.'," +
        "'example':'İzmir','state':'suggested','shownAt':'2026-10-15T09:00:00.000Z','deadline':'2026-10-15T09:00:51.000Z'," +
        "'secondsLeft':30,'suggestion':'Iğdır','typed':'ıgdir','frozenSecondsLeft':21.4,'attempts':[],'attemptsLeft':20}}";

    private static readonly string Rejected =
        "{'promptId':'p1','match':{'kind':'rejected'},'score':null,'progress':" + Progress0 + ",'status':'rejected'," +
        "'attemptsLeft':18,'prompt':{'promptId':'p1','position':1,'total':5,'text':'Bir il söyle.','example':'İzmir'," +
        "'state':'open','shownAt':'2026-10-15T09:00:00.000Z','deadline':'2026-10-15T09:00:30.000Z','secondsLeft':17," +
        "'suggestion':null,'typed':null,'frozenSecondsLeft':null,'attempts':['Van','Muş'],'attemptsLeft':18}}";

    private static string Accepted(string kind, string tier, int points, bool onlyYou)
    {
        return "{'promptId':'p1','match':{'kind':'" + kind + "'},'score':{'status':'provisional','ladder':'planets','tier':'" + tier +
            "','points':" + points + ",'onlyYou':" + (onlyYou ? "true" : "false") + "},'progress':" + Progress1 +
            ",'status':'accepted','attemptsLeft':0}";
    }

    private const string Closed =
        "{'promptId':'p1','match':{'kind':'timeout'},'score':{'status':'zero','points':0},'progress':" + Progress1 +
        ",'status':'closed','attemptsLeft':0}";

    private const string Result =
        "{'lang':'tr','date':'2026-10-14','dayNumber':41,'ladder':'planets','prompts':[" +
        "{'promptId':'p1','position':1,'text':'Bir Avrupa başkenti söyle.','answer':'vaduz','matched':'Vaduz'," +
        "'provisional':{'tier':'jupiter','points':40},'final':{'tier':'saturn','points':55},'onlyYou':true}," +
        "{'promptId':'p2','position':2,'text':'Bir takımyıldızı söyle.','answer':null,'matched':null," +
        "'provisional':{'tier':null,'points':0},'final':{'tier':null,'points':0},'onlyYou':false}]," +
        "'provisionalTotal':40,'finalTotal':55,'delta':15}";

    private const string Reveal =
        "{'lang':'tr','date':'2026-10-14','dayNumber':41,'ladder':'planets','prompts':[" +
        "{'id':'p1','position':1,'text':'Bir Avrupa başkenti söyle.','example':'Paris','class':'A','shortList':false,'answers':[" +
        "{'id':'paris','forms':['Paris'],'tier':'mercury','points':5,'share':0.22,'count':220.5}," +
        "{'id':'vaduz','forms':['Vaduz'],'tier':'saturn','points':55,'share':0.006,'count':6}," +
        "{'id':'londra','forms':['Londra','London'],'tier':'venus','points':10,'share':0.14,'count':140}," +
        "{'id':'roma','forms':['Roma'],'tier':'earth','points':15,'share':0.09,'count':90}," +
        "{'id':'yeni','forms':['Yeni'],'tier':null,'points':null,'share':null,'count':0}]}]}";

    private static string J(string single)
    {
        return single.Replace('\'', '"');
    }

    private FakeTransport http;
    private NadideGameSession session;
    private string savedFinalSeen;

    [SetUp]
    public void SetUp()
    {
        http = new FakeTransport();
        session = new NadideGameSession(http, "tr");
        savedFinalSeen = PlayerPrefs.HasKey(NadideGameSession.FinalSeenKey) ? PlayerPrefs.GetString(NadideGameSession.FinalSeenKey) : null;
        PlayerPrefs.DeleteKey(NadideGameSession.FinalSeenKey);
    }

    [TearDown]
    public void TearDown()
    {
        if (savedFinalSeen != null) PlayerPrefs.SetString(NadideGameSession.FinalSeenKey, savedFinalSeen);
        else PlayerPrefs.DeleteKey(NadideGameSession.FinalSeenKey);
    }

    // ---------- parsing ----------

    [Test]
    public void Start_ParsesSessionInfoAndToken()
    {
        string token;
        SessionInfo info = NadideJson.ParseStart(J(Start), out token);
        Assert.AreEqual("v1.sess.sig", token);
        Assert.AreEqual(42, info.dayNumber);
        Assert.AreEqual("2026-10-15", info.date);
        Assert.AreEqual("in_progress", info.status);
        Assert.IsFalse(info.resumed);
        Assert.AreEqual(30, info.secondsPerPrompt);
        Assert.AreEqual(5, info.progress.total);
    }

    [Test]
    public void Next_OpenPrompt_ContractNullsBecomeDefaults()
    {
        NextResult next = NadideJson.ParseNext(J(Next(OpenPrompt("p1", 1, 30))));
        Assert.AreEqual("prompt", next.status);
        Assert.AreEqual("p1", next.prompt.promptId);
        Assert.AreEqual("open", next.prompt.state);
        Assert.AreEqual(30f, next.prompt.secondsLeft);
        Assert.AreEqual(-1f, next.prompt.frozenSecondsLeft, "null frozenSecondsLeft = clock running");
        Assert.IsNull(next.prompt.suggestion);
        Assert.IsNull(next.prompt.typed);
        Assert.AreEqual("İzmir", next.prompt.example);
        Assert.AreEqual(0, next.prompt.attempts.Length);
        Assert.AreEqual(20, next.prompt.attemptsLeft);
    }

    [Test]
    public void Next_Finished_HasNoPrompt()
    {
        NextResult next = NadideJson.ParseNext(J("{'status':'finished','progress':{'answered':5,'total':5,'finished':true,'provisionalTotal':130}}"));
        Assert.AreEqual("finished", next.status);
        Assert.IsNull(next.prompt);
        Assert.IsTrue(next.progress.finished);
        Assert.AreEqual(130, next.progress.provisionalTotal);
    }

    [TestCase("mercury", StopId.Mercury)]
    [TestCase("venus", StopId.Venus)]
    [TestCase("earth", StopId.Earth)]
    [TestCase("mars", StopId.Mars)]
    [TestCase("jupiter", StopId.Jupiter)]
    [TestCase("saturn", StopId.Saturn)]
    [TestCase("uranus", StopId.Uranus)]
    [TestCase("neptune", StopId.Neptune)]
    [TestCase("trap_moon", StopId.Moon)]
    public void Answer_Accepted_TierIdMapsToStop(string tier, StopId expected)
    {
        AnswerResult result = NadideJson.ParseAnswer(J(Accepted("exact", tier, 25, false)));
        Assert.AreEqual("accepted", result.status);
        Assert.AreEqual("exact", result.match.kind);
        Assert.AreEqual("provisional", result.score.status);
        Assert.AreEqual(tier, result.score.tier);
        Assert.AreEqual(expected, result.score.Stop);
        Assert.IsNull(result.prompt, "a closed prompt has no prompt view");
        Assert.AreEqual(1, result.progress.answered);
    }

    [Test]
    public void Answer_ClosedByTimeout_IsZeroAtTheSun()
    {
        AnswerResult result = NadideJson.ParseAnswer(J(Closed));
        Assert.AreEqual("closed", result.status);
        Assert.AreEqual("timeout", result.match.kind);
        Assert.AreEqual("zero", result.score.status);
        Assert.IsNull(result.score.tier);
        Assert.AreEqual(0, result.score.points);
        Assert.AreEqual(StopId.Sun, result.score.Stop);
        Assert.IsNull(result.prompt);
    }

    [Test]
    public void Answer_Rejected_KeepsPromptOpenWithAttempts()
    {
        AnswerResult result = NadideJson.ParseAnswer(J(Rejected));
        Assert.AreEqual("rejected", result.status);
        Assert.IsNull(result.score);
        Assert.AreEqual(18, result.attemptsLeft);
        Assert.AreEqual(new[] { "Van", "Muş" }, result.prompt.attempts);
        Assert.AreEqual(-1f, result.prompt.frozenSecondsLeft);
    }

    [Test]
    public void Answer_Suggested_CarriesSuggestionAndFrozenClock()
    {
        AnswerResult result = NadideJson.ParseAnswer(J(Suggested));
        Assert.AreEqual("suggested", result.status);
        Assert.AreEqual("near", result.match.kind);
        Assert.AreEqual("Iğdır", result.match.suggestion);
        Assert.IsNull(result.score);
        Assert.AreEqual("suggested", result.prompt.state);
        Assert.AreEqual("ıgdir", result.prompt.typed);
        Assert.AreEqual(21.4f, result.prompt.frozenSecondsLeft, 0.001f);
    }

    [Test]
    public void Final_BuiltFromResultAndReveal()
    {
        FinalDay day = NadideJson.BuildFinal(J(Result), J(Reveal));
        Assert.AreEqual(41, day.dayNumber);
        Assert.AreEqual("2026-10-14", day.date);
        Assert.AreEqual(40, day.provisionalTotal);
        Assert.AreEqual(55, day.finalTotal);
        Assert.AreEqual(2, day.prompts.Length);

        FinalPrompt first = day.prompts[0];
        Assert.AreEqual("Vaduz", first.answer, "matched display form wins over the raw text");
        Assert.AreEqual("jupiter", first.provisionalTier);
        Assert.AreEqual("saturn", first.finalTier);
        Assert.AreEqual(55, first.finalPoints);
        Assert.IsTrue(first.onlyYou);
        Assert.AreEqual(new[] { "Paris", "Londra", "Roma" }, first.commonAnswers);
        Assert.AreEqual(22f, first.commonShares[0], 0.01f);
        Assert.AreEqual(14f, first.commonShares[1], 0.01f);

        FinalPrompt second = day.prompts[1];
        Assert.IsNull(second.answer);
        Assert.IsNull(second.finalTier);
        Assert.AreEqual(StopId.Sun, RqStops.FromId(second.finalTier));
        Assert.AreEqual(0, second.commonAnswers.Length);
    }

    [Test]
    public void Error_BodiesMapToCodesAndTurkishMessages()
    {
        NadideError expired = NadideJson.ToError(new NadideResponse { status = 401, body = J("{'error':{'code':'session_expired','message':'Oturumun süresi doldu.'}}") });
        Assert.AreEqual("session_expired", expired.code);
        Assert.AreEqual(NadideErrors.SessionExpired, NadideErrors.UserMessage(expired));

        NadideError html = NadideJson.ToError(new NadideResponse { status = 404, body = "<html>Not Found</html>" });
        Assert.AreEqual("unknown", html.code);
        Assert.AreEqual("Oyun sunucusu bulunamadı.", NadideErrors.UserMessage(html));

        Assert.IsTrue(NadideJson.ToError(new NadideResponse { status = 503 }).network);
        Assert.IsTrue(NadideJson.ToError(NadideResponse.Offline()).network);
        Assert.IsNull(NadideJson.ToError(new NadideResponse { status = 201, body = "{}" }));
    }

    [Test]
    public void PreviousDay_UsesGameDateOrIstanbulToday()
    {
        Assert.AreEqual("2026-10-14", NadideJson.PreviousDay("2026-10-15", DateTime.UtcNow));
        // 22:30 UTC is already the next day in Istanbul (UTC+3).
        Assert.AreEqual("2026-10-15", NadideJson.PreviousDay(null, new DateTime(2026, 10, 15, 22, 30, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void CookieJar_KeepsDeviceCookieAndHonoursMaxAge()
    {
        var jar = new NadideCookieJar(false);
        jar.Absorb("nadide_device=d1.abc.sig; Path=/; Max-Age=31536000; HttpOnly; SameSite=Lax");
        Assert.AreEqual("nadide_device=d1.abc.sig", jar.Header());
        jar.Absorb("nadide_beta=x; Path=/, other=y; Path=/");
        Assert.AreEqual("d1.abc.sig", jar.Get("nadide_device"));
        Assert.AreEqual("x", jar.Get("nadide_beta"));
        Assert.AreEqual("y", jar.Get("other"));
        jar.Absorb("other=; Max-Age=0");
        Assert.IsNull(jar.Get("other"));
    }

    // ---------- session flow ----------

    [Test]
    public void Session_RequestsMatchTheContract()
    {
        http.Reply(201, Start);
        http.Reply(200, Next(OpenPrompt("p1", 1, 30)));
        http.Reply(200, Accepted("exact", "mars", 25, false));

        SessionInfo info = null;
        session.Start(i => info = i, Unexpected);
        session.Next(n => { }, Unexpected);
        session.Answer("p1", "  Iğdır ", r => { }, Unexpected);

        Assert.AreEqual(42, info.dayNumber);
        Assert.AreEqual("v1.sess.sig", session.Token);
        Assert.AreEqual("POST /api/play/start {\"lang\":\"tr\"}", http.Calls[0]);
        Assert.AreEqual("POST /api/play/next {\"token\":\"v1.sess.sig\"}", http.Calls[1]);
        Assert.AreEqual("POST /api/answer {\"token\":\"v1.sess.sig\",\"promptId\":\"p1\",\"answer\":\"Iğdır\"}", http.Calls[2]);
    }

    [Test]
    public void Session_ExactAnswer_ShowsTypedText()
    {
        StartAndOpen();
        http.Reply(200, Accepted("exact", "mars", 25, false));
        AnswerResult result = null;
        session.Answer("p1", "Muş", r => result = r, Unexpected);
        Assert.AreEqual("accepted", result.status);
        Assert.AreEqual("Muş", result.answer);
        Assert.AreEqual(StopId.Mars, result.score.Stop);
    }

    [Test]
    public void Session_SuggestionConfirmed_CreditsSuggestedSpelling()
    {
        StartAndOpen();
        http.Reply(200, Suggested);
        http.Reply(200, Accepted("confirmed", "neptune", 100, true));

        AnswerResult suggested = null;
        AnswerResult accepted = null;
        session.Answer("p1", "ıgdir", r => suggested = r, Unexpected);
        session.Confirm("p1", true, r => accepted = r, Unexpected);

        Assert.AreEqual("suggested", suggested.status);
        Assert.AreEqual("Iğdır", suggested.match.suggestion);
        Assert.AreEqual("POST /api/answer {\"token\":\"v1.sess.sig\",\"promptId\":\"p1\",\"confirm\":true}", http.Calls[http.Calls.Count - 1]);
        Assert.AreEqual("accepted", accepted.status);
        Assert.AreEqual("confirmed", accepted.match.kind);
        Assert.AreEqual("Iğdır", accepted.answer);
        Assert.AreEqual(StopId.Neptune, accepted.score.Stop);
        Assert.IsTrue(accepted.score.onlyYou);
    }

    [Test]
    public void Session_SuggestionDeclined_IsRejectedAndPromptStaysOpen()
    {
        StartAndOpen();
        http.Reply(200, Suggested);
        http.Reply(200, Rejected);
        AnswerResult declined = null;
        session.Answer("p1", "ıgdir", r => { }, Unexpected);
        session.Confirm("p1", false, r => declined = r, Unexpected);
        StringAssert.EndsWith("\"confirm\":false}", http.Calls[http.Calls.Count - 1]);
        Assert.AreEqual("rejected", declined.status);
        Assert.IsNotNull(declined.prompt);
        Assert.IsNull(declined.answer);
    }

    [Test]
    public void Session_NextAfterLocalTimeout_WaitsForServerGraceAndFlagsTimedOut()
    {
        StartAndOpen();
        // Local clock hit 0; the server keeps the prompt open for its 3 s grace, then moves on.
        http.Reply(200, Next(OpenPrompt("p1", 1, 0)));
        http.Reply(200, Next(OpenPrompt("p1", 1, 0)));
        http.Reply(200, Next(OpenPrompt("p2", 2, 30)));
        NextResult next = null;
        session.Next(n => next = n, Unexpected);
        Assert.AreEqual("p2", next.prompt.promptId);
        Assert.IsTrue(next.timedOut);
        Assert.AreEqual(2, http.Delays.Count);
    }

    [Test]
    public void Session_NextAfterAcceptedAnswer_IsNotATimeout()
    {
        StartAndOpen();
        http.Reply(200, Accepted("exact", "earth", 15, false));
        http.Reply(200, Next(OpenPrompt("p2", 2, 30)));
        NextResult next = null;
        session.Answer("p1", "Van", r => { }, Unexpected);
        session.Next(n => next = n, Unexpected);
        Assert.IsFalse(next.timedOut);
        Assert.AreEqual(0, http.Delays.Count);
    }

    [Test]
    public void Session_NetworkErrors_RetriedThenReportedInTurkish()
    {
        StartAndOpen();
        http.Offline();
        http.Offline();
        http.Offline();
        string error = null;
        session.Next(n => Assert.Fail("no response expected"), e => error = e);
        Assert.AreEqual(NadideErrors.Network, error);
        Assert.AreEqual(2, http.Delays.Count);
        Assert.AreEqual("network", session.LastErrorCode);
    }

    [Test]
    public void Session_NetworkBlip_RecoversOnRetry()
    {
        StartAndOpen();
        http.Offline();
        http.Reply(200, Accepted("exact", "venus", 10, false));
        AnswerResult result = null;
        session.Answer("p1", "Van", r => result = r, Unexpected);
        Assert.AreEqual("accepted", result.status);
    }

    [Test]
    public void Session_Expired_FailsWithTurkishMessage()
    {
        StartAndOpen();
        http.Reply(401, "{'error':{'code':'session_expired','message':'Oturumun süresi doldu.'}}");
        string error = null;
        session.Next(n => Assert.Fail("no response expected"), e => error = e);
        Assert.AreEqual(NadideErrors.SessionExpired, error);
        Assert.IsNull(session.Token);
    }

    [Test]
    public void Session_InvalidSession_RestartsOnceAndRetries()
    {
        StartAndOpen();
        http.Reply(401, "{'error':{'code':'invalid_session','message':'Oturum belirteci geçersiz.'}}");
        http.Reply(200, Start.Replace("v1.sess.sig", "v1.new.sig"));
        http.Reply(200, Accepted("exact", "uranus", 75, false));
        AnswerResult result = null;
        session.Answer("p1", "Van", r => result = r, Unexpected);
        Assert.AreEqual("accepted", result.status);
        Assert.AreEqual("v1.new.sig", session.Token);
        StringAssert.Contains("v1.new.sig", http.Calls[http.Calls.Count - 1]);
    }

    [Test]
    public void Session_TooManyAttempts_IsARejectionWithNoTriesLeft()
    {
        StartAndOpen();
        http.Reply(429, "{'error':{'code':'too_many_attempts','message':'Deneme hakkın bitti.'}}");
        AnswerResult result = null;
        session.Answer("p1", "Van", r => result = r, Unexpected);
        Assert.AreEqual("rejected", result.status);
        Assert.AreEqual(0, result.attemptsLeft);
        Assert.AreEqual("p1", result.prompt.promptId);
    }

    [Test]
    public void Session_Final_ReturnsYesterdayOnceThenNull()
    {
        session.UtcNow = () => new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc);
        http.Reply(200, Result);
        http.Reply(200, Reveal);
        FinalDay final = null;
        session.Final(f => final = f, Unexpected);
        Assert.AreEqual("GET /api/result/2026-10-14?lang=tr ", http.Calls[0]);
        Assert.AreEqual("GET /api/reveal/2026-10-14?lang=tr ", http.Calls[1]);
        Assert.AreEqual(55, final.finalTotal);

        bool called = false;
        session.Final(f => { called = true; Assert.IsNull(f); }, Unexpected);
        Assert.IsTrue(called);
        Assert.AreEqual(2, http.Calls.Count, "already shown: no request");
    }

    [Test]
    public void Session_Final_DayNotClosedIsNull()
    {
        http.Reply(403, "{'error':{'code':'day_not_closed','message':'Kesin sonuç gün kapanınca açıklanır.'}}");
        bool called = false;
        session.Final(f => { called = true; Assert.IsNull(f); }, Unexpected);
        Assert.IsTrue(called);
    }

    // ---------- helpers ----------

    private void StartAndOpen()
    {
        http.Reply(201, Start);
        http.Reply(200, Next(OpenPrompt("p1", 1, 30)));
        session.Start(i => { }, Unexpected);
        session.Next(n => { }, Unexpected);
    }

    private static void Unexpected(string error)
    {
        Assert.Fail("unexpected failure: " + error);
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

        public void Offline()
        {
            responses.Enqueue(NadideResponse.Offline());
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
