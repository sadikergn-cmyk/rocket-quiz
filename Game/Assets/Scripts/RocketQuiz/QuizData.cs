using System;
using UnityEngine;

[Serializable]
public class QuizAnswer
{
    public string text;
    public string[] aliases;
    // Share of players who gave this answer (0-100). Lower = rarer = further.
    public float percent;
    // Offline mock only: an answer that feels rare but lands on the Moon trap.
    public bool trap;
}

[Serializable]
public class QuizQuestion
{
    public string prompt;
    public string example;
    public QuizAnswer[] answers;

    public QuizAnswer RarestAnswer()
    {
        QuizAnswer rarest = null;
        for (int i = 0; i < answers.Length; i++)
        {
            if (rarest == null || answers[i].percent < rarest.percent)
            {
                rarest = answers[i];
            }
        }
        return rarest;
    }
}

[Serializable]
public class QuizSet
{
    public QuizQuestion[] questions;
}

public static class QuizLibrary
{
    public const string ResourcePath = "RocketQuiz/questions";

    public static QuizSet Load()
    {
        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            throw new InvalidOperationException("Quiz data not found at Resources/" + ResourcePath);
        }
        return JsonUtility.FromJson<QuizSet>(asset.text);
    }
}
