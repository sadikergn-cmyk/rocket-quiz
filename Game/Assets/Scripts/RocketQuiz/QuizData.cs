using System;
using UnityEngine;

[Serializable]
public class QuizAnswer
{
    public string text;
    public string[] aliases;
    // Share of players who gave this answer (0-100). Lower = rarer = further.
    public float percent;
}

[Serializable]
public class QuizQuestion
{
    public string prompt;
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

// Maps how rare an answer is to how far the rocket flies.
public static class RarityTiers
{
    public static readonly string[] Destination =
    {
        "UZAYIN SINIRI", "DÜNYA YÖRÜNGESİ", "AY", "SATÜRN", "ANDROMEDA GALAKSİSİ",
    };

    public static readonly string[] Title =
    {
        "YAYGIN CEVAP", "İYİ CEVAP", "NADİR CEVAP", "ÇOK NADİR CEVAP", "EFSANE CEVAP",
    };

    // Real-world distance shown for each destination, in kilometres.
    public static readonly double[] DistanceKm = { 100, 400, 384400, 1.43e9, 2.4e19 };

    // Cool to warm to white: the colour itself tells the player how rare the answer was.
    public static readonly Color[] Tint =
    {
        new Color(0.62f, 0.78f, 0.95f),
        new Color(0.35f, 0.92f, 1.00f),
        new Color(0.76f, 0.56f, 1.00f),
        new Color(1.00f, 0.78f, 0.22f),
        new Color(1.00f, 1.00f, 1.00f),
    };

    public static int FromPercent(float percent)
    {
        if (percent >= 20f) return 0;
        if (percent >= 10f) return 1;
        if (percent >= 5f) return 2;
        if (percent >= 2f) return 3;
        return 4;
    }

    public static int Score(float percent)
    {
        int tier = FromPercent(percent);
        return Mathf.RoundToInt((100f - percent) * 10f) * (tier + 1);
    }

    public static string FormatPercent(float percent)
    {
        if (percent >= 1f)
        {
            return "%" + Mathf.RoundToInt(percent);
        }
        return "%" + percent.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
    }

    public static string FormatDistance(double km)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        const double lightYearKm = 9.461e12;
        if (km < 1.0) return Math.Round(km * 1000.0).ToString("N0", culture) + " m";
        if (km < 1e6) return Math.Round(km).ToString("N0", culture) + " km";
        if (km < 1e9) return (km / 1e6).ToString("0.0", culture) + " milyon km";
        if (km < lightYearKm * 0.1) return (km / 1e9).ToString("0.00", culture) + " milyar km";
        double lightYears = km / lightYearKm;
        if (lightYears < 1000.0) return lightYears.ToString("0.0", culture) + " ışık yılı";
        if (lightYears < 1e6) return (lightYears / 1000.0).ToString("0.0", culture) + " bin ışık yılı";
        return (lightYears / 1e6).ToString("0.00", culture) + " milyon ışık yılı";
    }
}
