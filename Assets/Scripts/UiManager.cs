using UnityEngine;

public class UiManager : MonoBehaviour
{
    [Header("Heli UI Panels")]
    public GameObject HeliWinPanel;
    public GameObject HeliCrashPanel;
    public GameObject HeliCarEscapedPanel;

    [Header("Car UI Panels")]
    public GameObject CarWinPanel;
    public GameObject CarCaughtPanel;
    public GameObject CarHeliCrashedPanel;

    private bool gameOver = false;

    void Start()
    {
        HideAllPanels();
    }

    private void HideAllPanels()
    {
        HeliWinPanel?.SetActive(false);
        HeliCrashPanel?.SetActive(false);
        HeliCarEscapedPanel?.SetActive(false);

        CarWinPanel?.SetActive(false);
        CarCaughtPanel?.SetActive(false);
        CarHeliCrashedPanel?.SetActive(false);
    }

    public void ShowHeliWin()
    {
        if (gameOver) return;
        gameOver = true;

        Debug.Log("UIManager: Helicopter Wins");
        HeliWinPanel?.SetActive(true);
        CarCaughtPanel?.SetActive(true);
    }

    public void ShowHeliCrash()
    {
        if (gameOver) return;
        gameOver = true;

        Debug.Log("UIManager: Helicopter Crashed");
        HeliCrashPanel?.SetActive(true);
        CarHeliCrashedPanel?.SetActive(true);
    }

    public void ShowCarEscape()
    {
        if (gameOver) return;
        gameOver = true;

        Debug.Log("UIManager: Car Escaped");
        HeliCarEscapedPanel?.SetActive(true);
        CarWinPanel?.SetActive(true);
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public void ResetUI()
    {
        gameOver = false;
        HeliWinPanel?.SetActive(false);
        HeliCrashPanel?.SetActive(false);
        HeliCarEscapedPanel?.SetActive(false);
        CarWinPanel?.SetActive(false);
        CarCaughtPanel?.SetActive(false);
        CarHeliCrashedPanel?.SetActive(false);

        GameManager.Instance.ResumeGame();
    }

}
