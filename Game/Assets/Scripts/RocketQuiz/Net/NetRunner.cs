using System;
using System.Collections;
using UnityEngine;

// Hidden coroutine host for the network layer's short waits (retry back-off, timeout grace). Uses real
// time so a paused timeScale never stalls a request.
public class NetRunner : MonoBehaviour
{
    private static NetRunner instance;

    public static void After(float seconds, Action then)
    {
        if (!Application.isPlaying || seconds <= 0f)
        {
            then();
            return;
        }
        if (instance == null)
        {
            var host = new GameObject("RocketQuiz.NetRunner");
            host.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(host);
            instance = host.AddComponent<NetRunner>();
        }
        instance.StartCoroutine(instance.Wait(seconds, then));
    }

    private IEnumerator Wait(float seconds, Action then)
    {
        yield return new WaitForSecondsRealtime(seconds);
        then();
    }
}
