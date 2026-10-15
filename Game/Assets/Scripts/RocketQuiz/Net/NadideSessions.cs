using UnityEngine;

// Default IGameSession for QuizGameController.SessionFactory. Player builds always talk to the Nadide server (they
// carry no answer data). The Editor plays offline with MockGameSession unless
// "Tools > RocketQuiz > Use Nadide Server" is ticked (server address: NadideConfig, default http://localhost:3000).
public static class NadideSessions
{
    public const string UseServerKey = "RocketQuiz.UseNadideServer";

    public static IGameSession CreateDefault()
    {
#if UNITY_EDITOR
        if (UseServerInEditor) return new NadideGameSession();
        return new MockGameSession();
#else
        return new NadideGameSession();
#endif
    }

#if UNITY_EDITOR
    private const string UseServerMenu = "Tools/RocketQuiz/Use Nadide Server";

    public static bool UseServerInEditor
    {
        get { return UnityEditor.EditorPrefs.GetBool(UseServerKey, false); }
        set { UnityEditor.EditorPrefs.SetBool(UseServerKey, value); }
    }

    [UnityEditor.MenuItem(UseServerMenu, false, 100)]
    private static void ToggleServer()
    {
        UseServerInEditor = !UseServerInEditor;
        Debug.Log(UseServerInEditor
            ? "RocketQuiz: Nadide server session (" + NadideConfig.BaseUrl + ") from the next Play."
            : "RocketQuiz: offline MockGameSession from the next Play.");
    }

    [UnityEditor.MenuItem(UseServerMenu, true)]
    private static bool ToggleServerValidate()
    {
        UnityEditor.Menu.SetChecked(UseServerMenu, UseServerInEditor);
        return true;
    }

    [UnityEditor.MenuItem("Tools/RocketQuiz/Forget Nadide Guest Cookie", false, 101)]
    private static void ForgetGuest()
    {
        NadideTransport.ForgetCookies();
        PlayerPrefs.DeleteKey(NadideGameSession.FinalSeenKey);
        Debug.Log("RocketQuiz: Nadide guest cookie cleared; the next play/start opens a new guest.");
    }
#endif
}
