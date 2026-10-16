using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// One finished question of today's run, for the result screen and the share payload.
public struct DayRow
{
    public int Position;
    public string Prompt;
    public string Answer;
    public StopId Stop;
    public int Points;
    public bool OnlyYou;
}

// Today's finished questions. After a reload they are rebuilt from play/start (PlayStartResponse.prompts carries
// each answered prompt's stop and points). The answer text is not part of that contract, so the device keeps its
// own copy (AnswerMemo) as a display-only backup; the server stays the authority for stops and points.
public static class DayProgress
{
    public const string DayNumberKey = "rq.dayNumber";
    public const string DayNumberDateKey = "rq.dayNumberDate";

    // Answered prompts -> rows in position order. `answers` may be null.
    public static List<DayRow> RowsFrom(PromptProgress[] prompts, AnswerMemo answers)
    {
        var rows = new List<DayRow>();
        if (prompts == null) return rows;
        foreach (PromptProgress p in prompts)
        {
            if (p == null || p.state != "answered") continue;
            ScoreView score = p.score;
            Put(rows, new DayRow
            {
                Position = p.position,
                Prompt = null,
                Answer = answers != null ? answers.Get(p.position) : null,
                Stop = score != null ? score.Stop : StopId.Sun,
                Points = score != null ? score.points : 0,
                OnlyYou = score != null && score.onlyYou,
            });
        }
        return rows;
    }

    // Adds a row, replacing the one at the same position (a retried or resumed prompt), keeping position order.
    public static void Put(List<DayRow> rows, DayRow row)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Position == row.Position)
            {
                rows[i] = row;
                return;
            }
        }
        int at = rows.Count;
        while (at > 0 && rows[at - 1].Position > row.Position) at--;
        rows.Insert(at, row);
    }

    public static int Total(List<DayRow> rows)
    {
        int total = 0;
        foreach (DayRow row in rows) total += row.Points;
        return total;
    }

    // play/state carries no day number. The Hangar shows the last one play/start gave, moved by whole days (the
    // server numbers consecutive days); 0 (unknown, not shown) before the first game on this device.
    public static int EstimateDayNumber(string date, string knownDate, int knownNumber)
    {
        DateTime day;
        DateTime known;
        if (knownNumber <= 0 || !TryDate(date, out day) || !TryDate(knownDate, out known)) return 0;
        int estimate = knownNumber + (int)Math.Round((day - known).TotalDays);
        return estimate > 0 ? estimate : 0;
    }

    public static int EstimateDayNumber(string date)
    {
        return EstimateDayNumber(date, PlayerPrefs.GetString(DayNumberDateKey, string.Empty), PlayerPrefs.GetInt(DayNumberKey, 0));
    }

    public static void RememberDayNumber(string date, int dayNumber)
    {
        if (dayNumber <= 0 || string.IsNullOrEmpty(date)) return;
        PlayerPrefs.SetString(DayNumberDateKey, date);
        PlayerPrefs.SetInt(DayNumberKey, dayNumber);
        PlayerPrefs.Save();
    }

    private static bool TryDate(string value, out DateTime date)
    {
        return DateTime.TryParseExact(value ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}

// The player's own accepted answers for one game day, by position (PlayerPrefs, display only).
[Serializable]
public class AnswerMemo
{
    public const string PrefsKey = "rq.dayAnswers";

    public string date;
    public int[] positions = new int[0];
    public string[] answers = new string[0];

    public string Get(int position)
    {
        if (positions == null || answers == null) return null;
        for (int i = 0; i < positions.Length && i < answers.Length; i++)
        {
            if (positions[i] == position) return string.IsNullOrEmpty(answers[i]) ? null : answers[i];
        }
        return null;
    }

    public void Set(int position, string answer)
    {
        if (string.IsNullOrEmpty(answer)) return;
        var p = new List<int>(positions ?? new int[0]);
        var a = new List<string>(answers ?? new string[0]);
        int at = p.IndexOf(position);
        if (at >= 0 && at < a.Count)
        {
            a[at] = answer;
        }
        else
        {
            p.Add(position);
            a.Add(answer);
        }
        positions = p.ToArray();
        answers = a.ToArray();
    }

    // The memo for `date`; an empty one when the stored memo belongs to another day (or none is stored).
    public static AnswerMemo FromJson(string json, string date)
    {
        AnswerMemo memo = null;
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                memo = JsonUtility.FromJson<AnswerMemo>(json);
            }
            catch (Exception)
            {
                memo = null;
            }
        }
        if (memo == null || memo.date != date) memo = new AnswerMemo { date = date };
        return memo;
    }

    public static AnswerMemo Load(string date)
    {
        return FromJson(PlayerPrefs.GetString(PrefsKey, string.Empty), date);
    }

    public void Save()
    {
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }
}
