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

// Sample questions for the offline MockGameSession. The real answer list lives on the Nadide server, so the file sits
// in an Editor folder and never ships in a player build; players only get a placeholder.
public static class QuizLibrary
{
    public const string EditorPath = "Assets/Editor/RocketQuiz/questions.json";

    private const string PlayerSample =
        "{\"questions\":[{\"prompt\":\"Çevrimdışı örnek: bir renk söyle.\",\"example\":\"Mavi\",\"answers\":[" +
        "{\"text\":\"Kırmızı\",\"percent\":30},{\"text\":\"Mavi\",\"percent\":25},{\"text\":\"Turkuaz\",\"percent\":3}]}]}";

    public static QuizSet Load()
    {
#if UNITY_EDITOR
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(EditorPath);
        if (asset == null)
        {
            throw new InvalidOperationException("Quiz data not found at " + EditorPath);
        }
        return JsonUtility.FromJson<QuizSet>(asset.text);
#else
        return JsonUtility.FromJson<QuizSet>(PlayerSample);
#endif
    }
}
