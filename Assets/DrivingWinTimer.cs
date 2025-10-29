using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DrivingWinTimer : MonoBehaviour
{
    [Header("References")]
    public SpotlightDetector spotlightDetector;
    public UiManager uiManager;
    public TextMeshProUGUI timerText; // Assign Text on the world-space canvas

    [Header("Win Condition Settings")]
    public float survivalTime = 180f; // 3 minutes
   

    private float timer = 0f;
    private bool gameOver = false;

    void Start()
    {
        timer = survivalTime;
        UpdateTimerUI();
    }

    void Update()
    {
        if (gameOver || GameManager.Instance.GameIsOver())
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            DrivingPlayerWins();
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }


    private void DrivingPlayerWins()
    {
        gameOver = true;
        if (GameManager.Instance.GameIsOver()) return;

        GameManager.Instance.CarEscapes();

    }
}
