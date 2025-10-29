using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public UiManager uiManager;

    [Header("Game State")]
    public bool heliPlayerWon = false;
    public bool drivingPlayerWon = false;
    public bool heliCrashed = false;
    public bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void HeliWins()
    {
        if (uiManager.IsGameOver()) return;

        heliPlayerWon = true;
        uiManager.ShowHeliWin();
        PauseGame();
    }

    public void HeliCrashed()
    {
        if (uiManager.IsGameOver()) return;

        heliCrashed = true;
        uiManager.ShowHeliCrash();
        PauseGame();
    }

    public void CarEscapes()
    {
        if (uiManager.IsGameOver()) return;

        drivingPlayerWon = true;
        uiManager.ShowCarEscape();
        PauseGame();
    }

    // --- PAUSE/RESUME ---
    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;

        // Freeze all rigidbodies safely
        Rigidbody[] bodies = FindObjectsOfType<Rigidbody>();
        foreach (var rb in bodies)
        {
            rb.isKinematic = true;
        }

        // Freeze physics time but keep Update() for UI working
        Time.timeScale = 0f;

        Debug.Log("GameManager: Game paused.");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        Rigidbody[] bodies = FindObjectsOfType<Rigidbody>();
        foreach (var rb in bodies)
        {
            rb.isKinematic = false;
        }

        Debug.Log("GameManager: Game resumed.");
    }

    public bool GameIsOver()
    {
        return uiManager != null && uiManager.IsGameOver();
    }
}
