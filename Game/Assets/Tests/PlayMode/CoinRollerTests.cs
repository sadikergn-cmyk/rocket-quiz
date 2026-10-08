using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CoinRollerTests
{
    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        yield return SceneManager.LoadSceneAsync("CoinRoller", LoadSceneMode.Single);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SceneStartsWithEightCoinsAndZeroScore()
    {
        yield return null;
        Assert.AreEqual(8, GameManager.Instance.Total);
        Assert.AreEqual(0, GameManager.Instance.Collected);
        Assert.IsFalse(GameManager.Instance.HasWon);
    }

    [UnityTest]
    public IEnumerator PlayerRestsOnGround()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        yield return new WaitForSeconds(1f);
        Assert.That(player.transform.position.y, Is.InRange(0.4f, 0.6f));
        Assert.AreEqual(0, player.RespawnCount);
    }

    [UnityTest]
    public IEnumerator TouchingCoinCollectsIt()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        Coin coin = Object.FindFirstObjectByType<Coin>();
        Rigidbody body = player.GetComponent<Rigidbody>();

        body.position = coin.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.AreEqual(1, GameManager.Instance.Collected);
        Assert.IsTrue(coin == null, "collected coin should be destroyed");
    }

    [UnityTest]
    public IEnumerator CollectingEveryCoinWins()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        Rigidbody body = player.GetComponent<Rigidbody>();
        body.isKinematic = true;

        foreach (Coin coin in Object.FindObjectsByType<Coin>(FindObjectsSortMode.None))
        {
            body.position = coin.transform.position;
            player.transform.position = coin.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }
        yield return null;

        Assert.AreEqual(8, GameManager.Instance.Collected);
        Assert.IsTrue(GameManager.Instance.HasWon);
        Assert.IsTrue(GameObject.Find("MessageText").activeInHierarchy);
    }

    [UnityTest]
    public IEnumerator FallingOffTheMapRespawnsPlayer()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        Vector3 start = player.transform.position;
        Rigidbody body = player.GetComponent<Rigidbody>();

        body.position = new Vector3(50f, -20f, 50f);
        player.transform.position = body.position;
        yield return null;
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(1, player.RespawnCount);
        Assert.That(Vector3.Distance(player.transform.position, start), Is.LessThan(1f));
    }
}
