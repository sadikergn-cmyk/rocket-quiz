using System.Collections;
using UnityEngine;

public class QuizGameController : MonoBehaviour
{
    public enum GameState
    {
        Question,
        Launching,
        Result,
        Finished,
    }

    [SerializeField] private QuizUI ui;
    [SerializeField] private LaunchDirector director;
    [SerializeField] private AudioDirector audioDirector;
    [SerializeField] private LaunchCameraRig rig;
    [SerializeField] private int attemptsPerQuestion = 3;
    [SerializeField] private bool shuffleQuestions = true;

    private QuizQuestion[] questions;
    private int questionIndex;
    private int attemptsLeft;

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public int LastTier { get; private set; }
    public int LastPoints { get; private set; }

    public QuizQuestion CurrentQuestion
    {
        get { return questions[questionIndex]; }
    }

    private void Start()
    {
        questions = QuizLibrary.Load().questions;
        if (shuffleQuestions)
        {
            for (int i = questions.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                QuizQuestion swap = questions[i];
                questions[i] = questions[j];
                questions[j] = swap;
            }
        }

        ui.Submitted += SubmitAnswer;
        ui.Continued += Continue;
        LastTier = -1;
        director.ResetToPad();
        ShowQuestion();
    }

    private void OnDestroy()
    {
        if (ui != null)
        {
            ui.Submitted -= SubmitAnswer;
            ui.Continued -= Continue;
        }
    }

    private void ShowQuestion()
    {
        State = GameState.Question;
        attemptsLeft = attemptsPerQuestion;
        ui.ShowQuestion(CurrentQuestion.prompt, questionIndex + 1, questions.Length, Score, attemptsLeft);
    }

    public void SubmitAnswer(string text)
    {
        if (State != GameState.Question || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        QuizAnswer answer = AnswerMatcher.Match(CurrentQuestion, text);
        if (answer == null)
        {
            attemptsLeft--;
            audioDirector.Wrong();
            rig.AddTrauma(0.2f);
            ui.SetFuel(attemptsLeft);
            if (attemptsLeft > 0)
            {
                ui.ShowWrong("“" + text.Trim() + "” listede yok. Bir yakıt tankı yandı.");
            }
            else
            {
                StartCoroutine(SkipQuestion());
            }
            return;
        }

        StartCoroutine(LaunchFlow(answer));
    }

    private IEnumerator SkipQuestion()
    {
        State = GameState.Launching;
        ui.ShowWrong("Hakkın bitti. Roket rampada kaldı.");
        yield return new WaitForSecondsRealtime(1.6f);
        NextQuestion();
    }

    private IEnumerator LaunchFlow(QuizAnswer answer)
    {
        State = GameState.Launching;
        int tier = RarityTiers.FromPercent(answer.percent);
        int points = RarityTiers.Score(answer.percent);
        LastTier = tier;
        LastPoints = points;

        yield return director.Play(tier);

        Score += points;
        State = GameState.Result;
        ui.SetHud(false);

        // Rarity is the reward, so it gets its own stamped line; a better answer is teased below it.
        string rarity = "Yalnızca " + RarityTiers.FormatPercent(answer.percent) + " bu cevabı verdi";
        if (tier == 0)
        {
            rarity = RarityTiers.FormatPercent(answer.percent) + " bu cevabı verdi";
        }
        QuizAnswer rarest = CurrentQuestion.RarestAnswer();
        string nearMiss = tier < JourneyMap.TierCount - 1
            ? "En nadir cevap: " + rarest.text + " (" + RarityTiers.FormatPercent(rarest.percent) + ")"
            : "Daha uzağı yok. Bunu çok az kişi bilir!";
        bool last = questionIndex >= questions.Length - 1;
        string destination = answer.text + "  →  " + RarityTiers.Destination[tier]
            + "\n" + RarityTiers.FormatDistance(RarityTiers.DistanceKm[tier]);

        ui.ShowResult(RarityTiers.Title[tier], RarityTiers.Tint[tier], destination, rarity, nearMiss, points,
            last ? "SONUCU GÖR" : "SIRADAKİ SORU", audioDirector.Tick);
        ui.SetScore(Score);
    }

    public void Continue()
    {
        if (State == GameState.Result)
        {
            StartCoroutine(ReturnToPad());
        }
        else if (State == GameState.Finished)
        {
            Score = 0;
            questionIndex = 0;
            StartCoroutine(ReturnToPad(false));
        }
    }

    private IEnumerator ReturnToPad(bool advance = true)
    {
        State = GameState.Launching;
        ui.HideResult();
        yield return ui.FadeBlack(true, 0.35f);
        director.ResetToPad();
        if (advance)
        {
            NextQuestion();
        }
        else
        {
            ShowQuestion();
        }
        yield return ui.FadeBlack(false, 0.45f);
    }

    private void NextQuestion()
    {
        if (questionIndex >= questions.Length - 1)
        {
            State = GameState.Finished;
            ui.HideQuiz();
            ui.ShowResult("OYUN BİTTİ", Color.white, "Toplam puanın", "Tüm sorular tamamlandı", string.Empty, Score,
                "TEKRAR OYNA", audioDirector.Tick);
            return;
        }
        questionIndex++;
        ShowQuestion();
    }
}
