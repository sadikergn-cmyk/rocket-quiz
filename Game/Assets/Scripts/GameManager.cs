using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text messageText;

    public static GameManager Instance { get; private set; }

    public int Collected { get; private set; }
    public int Total { get; private set; }
    public bool HasWon { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Total = FindObjectsByType<Coin>(FindObjectsSortMode.None).Length;
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
        RefreshScore();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void CollectCoin()
    {
        Collected++;
        RefreshScore();

        if (!HasWon && Collected >= Total)
        {
            HasWon = true;
            if (messageText != null)
            {
                messageText.text = "KAZANDIN!\nYeniden başlamak için R";
                messageText.gameObject.SetActive(true);
            }
        }
    }

    private void RefreshScore()
    {
        if (scoreText != null)
        {
            scoreText.text = "Altın: " + Collected + " / " + Total;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
