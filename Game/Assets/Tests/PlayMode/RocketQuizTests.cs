using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// RocketQuiz with Nadide's rules: 8 planet stops + Moon trap, 5 prompts, 30 s, retries (0018).
public class RocketQuizTests
{
    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("rq.introSeen", 1);
        MockGameSession.FinalDemo = false;
        LaunchDirector.ReducedMotion = false;
        QuizGameController.SessionFactory = () => new MockGameSession(false);
        yield return SceneManager.LoadSceneAsync("RocketQuiz", LoadSceneMode.Single);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator ResetTime()
    {
        Time.timeScale = 1f;
        QuizGameController.SessionFactory = () => new MockGameSession();
        yield return null;
    }

    // ---------- pure logic ----------

    [Test]
    public void MatcherForgivesCaseDiacriticsAndTypos()
    {
        var question = new QuizQuestion
        {
            answers = new[]
            {
                new QuizAnswer { text = "Şanlıurfa", aliases = new[] { "Urfa" }, percent = 0.7f },
                new QuizAnswer { text = "İstanbul", percent = 28f },
                new QuizAnswer { text = "Van", percent = 0.7f },
            },
        };

        Assert.AreEqual("Şanlıurfa", AnswerMatcher.Match(question, "sanliurfa").text);
        Assert.AreEqual("Şanlıurfa", AnswerMatcher.Match(question, "  URFA ").text);
        Assert.AreEqual("İstanbul", AnswerMatcher.Match(question, "ISTANBUL").text);
        Assert.AreEqual("İstanbul", AnswerMatcher.Match(question, "istanbl").text);
        Assert.IsNull(AnswerMatcher.Match(question, "Vam"), "short answers must match exactly");
        Assert.IsNull(AnswerMatcher.Match(question, "Paris"));
        Assert.IsNull(AnswerMatcher.Match(question, ""));

        bool exact;
        Assert.AreEqual("İstanbul", AnswerMatcher.Evaluate(question, "istanbul", out exact).text);
        Assert.IsTrue(exact, "case and dotted İ fold to an exact match");
        Assert.AreEqual("İstanbul", AnswerMatcher.Evaluate(question, "istanbl", out exact).text);
        Assert.IsFalse(exact, "a typo is only suggested");
    }

    [Test]
    public void StopsFollowNadideLadder()
    {
        float[] shares = { 26f, 14f, 7f, 3f, 1.5f, 0.7f, 0.3f, 0.1f };
        int previous = 0;
        for (int i = 0; i < shares.Length; i++)
        {
            StopId stop = RqStops.FromShare(shares[i]);
            Assert.AreEqual((StopId)(i + 1), stop);
            Assert.Greater(RqStops.LadderPoints(stop), previous);
            previous = RqStops.LadderPoints(stop);
        }
        Assert.AreEqual(5, RqStops.LadderPoints(StopId.Mercury));
        Assert.AreEqual(100, RqStops.LadderPoints(StopId.Neptune));
        Assert.AreEqual(StopId.Moon, RqStops.FromId("trap_moon"));
        Assert.AreEqual("neptune", RqStops.Id(StopId.Neptune));
        Assert.AreEqual(RqStops.MapIndex(StopId.Earth), RqStops.MapIndex(StopId.Moon), "the Moon sits beside Earth");
    }

    [Test]
    public void EveryQuestionHasAnExampleACommonAndARareAnswer()
    {
        QuizSet set = QuizLibrary.Load();
        foreach (QuizQuestion question in set.questions)
        {
            Assert.IsFalse(string.IsNullOrEmpty(question.example), question.prompt + " has no example");
            bool common = false;
            bool rare = false;
            foreach (QuizAnswer answer in question.answers)
            {
                StopId stop = RqStops.FromShare(answer.percent);
                if (stop == StopId.Mercury) common = true;
                if ((int)stop >= (int)StopId.Jupiter) rare = true;
            }
            Assert.IsTrue(common, question.prompt + " has no common answer");
            Assert.IsTrue(rare, question.prompt + " has no rare answer");
        }
    }

    [Test]
    public void MockSessionRejectsSuggestsAndAccepts()
    {
        var session = new MockGameSession(false);
        NextResult next = null;
        session.Start(info => { }, Assert.Fail);
        session.Next(n => next = n, Assert.Fail);
        Assert.AreEqual("prompt", next.status);
        QuizQuestion question = session.CurrentQuestion;

        AnswerResult result = null;
        session.Answer(next.prompt.promptId, "kesinlikle yanlış bir cevap", r => result = r, Assert.Fail);
        Assert.AreEqual("rejected", result.status);
        int left = result.attemptsLeft;
        session.Answer(next.prompt.promptId, "Kesinlikle yanlış bir cevap", r => result = r, Assert.Fail);
        Assert.AreEqual(left, result.attemptsLeft, "the same form again does not spend an attempt");

        QuizAnswer target = question.answers[0];
        string typo = target.text.Substring(0, target.text.Length - 1) + "x";
        session.Answer(next.prompt.promptId, typo + "xx", r => result = r, Assert.Fail);
        if (target.text.Length >= 9)
        {
            Assert.AreEqual("suggested", result.status);
        }

        session.Answer(next.prompt.promptId, target.text, r => result = r, Assert.Fail);
        if (result.status == "suggested")
        {
            session.Confirm(next.prompt.promptId, true, r => result = r, Assert.Fail);
        }
        Assert.AreEqual("accepted", result.status);
        Assert.AreEqual("provisional", result.score.status);
        Assert.AreEqual(1, result.progress.answered);
    }

    // ---------- play mode ----------

    private static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
    }

    private static IEnumerator StartDay(QuizGameController controller)
    {
        yield return WaitFor(() => controller.State == QuizGameController.GameState.Hangar, 5f);
        Assert.AreEqual(QuizGameController.GameState.Hangar, controller.State);
        controller.BeginDay();
        yield return WaitFor(() => controller.State == QuizGameController.GameState.Question, 5f);
        Assert.AreEqual(QuizGameController.GameState.Question, controller.State);
    }

    [UnityTest]
    public IEnumerator WrongAnswerKeepsQuestionOpen()
    {
        var controller = Object.FindFirstObjectByType<QuizGameController>();
        yield return StartDay(controller);
        controller.SubmitAnswer("kesinlikle yanlış bir cevap");
        yield return null;
        Assert.AreEqual(QuizGameController.GameState.Question, controller.State);
        Assert.Greater(controller.SecondsLeft, 0f, "the clock keeps running after a rejection");
    }

    [UnityTest]
    public IEnumerator CommonAnswerReachesMercuryAndScoresFive()
    {
        var controller = Object.FindFirstObjectByType<QuizGameController>();
        var director = Object.FindFirstObjectByType<LaunchDirector>();
        yield return StartDay(controller);

        var mock = (MockGameSession)controller.Session;
        QuizAnswer common = null;
        foreach (QuizAnswer answer in mock.CurrentQuestion.answers)
        {
            if (RqStops.FromShare(answer.percent) == StopId.Mercury && !answer.trap) common = answer;
        }
        Assert.IsNotNull(common);

        controller.SubmitAnswer(common.text);
        yield return null;
        Assert.AreEqual(QuizGameController.GameState.Launching, controller.State);

        yield return WaitFor(() => controller.State == QuizGameController.GameState.Moment, 20f);
        Assert.AreEqual(QuizGameController.GameState.Moment, controller.State);
        Assert.AreEqual(StopId.Mercury, controller.LastStop);
        Assert.AreEqual(StopId.Mercury, director.ReachedStop);
        Assert.AreEqual(5, controller.Score);
        Assert.AreEqual(JourneyMap.Station(StopId.Mercury), SpaceJourney.Instance.Altitude, 0.5f);

        controller.Continue();
        yield return WaitFor(() => controller.State == QuizGameController.GameState.Question, 5f);
        Assert.AreEqual(QuizGameController.GameState.Question, controller.State);
        Assert.AreEqual(2, controller.CurrentPrompt.position);
    }

    [UnityTest]
    public IEnumerator TimeoutLeavesYouAtTheSun()
    {
        QuizGameController.SessionFactory = () => new MockGameSession(false, 1.5f);
        yield return SceneManager.LoadSceneAsync("RocketQuiz", LoadSceneMode.Single);
        yield return null;
        var controller = Object.FindFirstObjectByType<QuizGameController>();
        yield return StartDay(controller);

        yield return WaitFor(() => controller.State == QuizGameController.GameState.Moment, 8f);
        Assert.AreEqual(QuizGameController.GameState.Moment, controller.State);
        Assert.AreEqual(StopId.Sun, controller.LastStop);
        Assert.AreEqual(0, controller.Score);
    }
}
