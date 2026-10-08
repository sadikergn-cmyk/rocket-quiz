using System.Globalization;
using System.Text;

// Matches typed answers against the accepted list, forgiving case, Turkish diacritics and small typos.
public static class AnswerMatcher
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static QuizAnswer Match(QuizQuestion question, string input)
    {
        string typed = Normalize(input);
        if (typed.Length == 0 || question == null || question.answers == null)
        {
            return null;
        }

        QuizAnswer best = null;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < question.answers.Length; i++)
        {
            QuizAnswer answer = question.answers[i];
            int distance = Distance(typed, Normalize(answer.text));
            if (answer.aliases != null)
            {
                for (int a = 0; a < answer.aliases.Length; a++)
                {
                    int aliasDistance = Distance(typed, Normalize(answer.aliases[a]));
                    if (aliasDistance < distance) distance = aliasDistance;
                }
            }
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = answer;
            }
        }

        return bestDistance <= Tolerance(typed.Length) ? best : null;
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }
        string lower = value.Trim().ToLower(Turkish);
        var builder = new StringBuilder(lower.Length);
        for (int i = 0; i < lower.Length; i++)
        {
            char c = lower[i];
            switch (c)
            {
                case 'ç': c = 'c'; break;
                case 'ğ': c = 'g'; break;
                case 'ı': c = 'i'; break;
                case 'ö': c = 'o'; break;
                case 'ş': c = 's'; break;
                case 'ü': c = 'u'; break;
                case 'â': c = 'a'; break;
                case 'î': c = 'i'; break;
                case 'û': c = 'u'; break;
            }
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    private static int Tolerance(int length)
    {
        if (length >= 9) return 2;
        if (length >= 5) return 1;
        return 0;
    }

    private static int Distance(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) previous[j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                int best = previous[j] + 1;
                if (current[j - 1] + 1 < best) best = current[j - 1] + 1;
                if (previous[j - 1] + cost < best) best = previous[j - 1] + cost;
                current[j] = best;
            }
            int[] swap = previous;
            previous = current;
            current = swap;
        }
        return previous[b.Length];
    }
}
