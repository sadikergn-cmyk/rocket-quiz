using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RocketQuizTests
{
    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("RocketQuiz", LoadSceneMode.Single);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator ResetTime()
    {
        Time.timeScale = 1f;
        yield return null;
    }

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
    }

    [Test]
    public void RarerAnswersReachFurtherTiersAndScoreMore()
    {
        float[] percents = { 30f, 12f, 6f, 3f, 0.5f };
        int previousScore = 0;
        for (int i = 0; i < percents.Length; i++)
        {
            Assert.AreEqual(i, RarityTiers.FromPercent(percents[i]));
            int score = RarityTiers.Score(percents[i]);
            Assert.Greater(score, previousScore);
            previousScore = score;
        }
        Assert.AreEqual("%1,5", RarityTiers.FormatPercent(1.5f));
    }

    [Test]
    public void EveryQuestionHasACommonAndARareAnswer()
    {
        QuizSet set = QuizLibrary.Load();
        Assert.GreaterOrEqual(set.questions.Length, 5);
        foreach (QuizQuestion question in set.questions)
        {
            bool common = false;
            bool rare = false;
            foreach (QuizAnswer answer in question.answers)
            {
                if (RarityTiers.FromPercent(answer.percent) == 0) common = true;
                if (RarityTiers.FromPercent(answer.percent) == JourneyMap.TierCount - 1) rare = true;
                Assert.AreSame(answer, AnswerMatcher.Match(question, answer.text), question.prompt + ": " + answer.text);
            }
            Assert.IsTrue(common, question.prompt + " has no common answer");
            Assert.IsTrue(rare, question.prompt + " has no top-tier answer");
        }
    }

    [UnityTest]
    public IEnumerator WrongAnswerKeepsRocketOnPad()
    {
        var controller = Object.FindFirstObjectByType<QuizGameController>();
        controller.SubmitAnswer("kesinlikle yanlış bir cevap");
        yield return null;
        Assert.AreEqual(QuizGameController.GameState.Question, controller.State);
        Assert.AreEqual(0f, SpaceJourney.Instance.Altitude);
    }

    [UnityTest]
    public IEnumerator CommonAnswerFliesToFirstTierAndScores()
    {
        var controller = Object.FindFirstObjectByType<QuizGameController>();
        var director = Object.FindFirstObjectByType<LaunchDirector>();
        QuizQuestion question = controller.CurrentQuestion;
        QuizAnswer common = question.answers[0];
        foreach (QuizAnswer answer in question.answers)
        {
            if (answer.percent > common.percent) common = answer;
        }

        controller.SubmitAnswer(common.text);
        Assert.AreEqual(QuizGameController.GameState.Launching, controller.State);

        float deadline = Time.realtimeSinceStartup + 30f;
        while (controller.State != QuizGameController.GameState.Result && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.AreEqual(QuizGameController.GameState.Result, controller.State);
        Assert.AreEqual(0, controller.LastTier);
        Assert.AreEqual(0, director.ReachedTier);
        Assert.AreEqual(JourneyMap.TierAltitude[0], SpaceJourney.Instance.Altitude, 0.01f);
        Assert.AreEqual(RarityTiers.Score(common.percent), controller.Score);

        controller.Continue();
        deadline = Time.realtimeSinceStartup + 10f;
        while (controller.State != QuizGameController.GameState.Question && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
        Assert.AreEqual(QuizGameController.GameState.Question, controller.State);
        Assert.AreEqual(0f, SpaceJourney.Instance.Altitude);
    }
}
