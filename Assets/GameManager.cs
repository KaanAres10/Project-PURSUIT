using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool heliPlayerWon = false;
    public bool drivingPlayerWon = false;

    [Header("UI References")]
    public Canvas heliPlayerUICanvas;     // 2D monitor UI
    public Canvas drivingPlayerUICanvas;  // VR world-space canvas

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

    public void EndGame(bool heliWon)
    {
        if (heliWon)
        {
            heliPlayerWon = true;
        }
        else
        {
            drivingPlayerWon = true;
        }

        // Show UIs for both players
        if (heliPlayerUICanvas != null)
            heliPlayerUICanvas.gameObject.SetActive(true);

        if (drivingPlayerUICanvas != null)
            drivingPlayerUICanvas.gameObject.SetActive(true);
    }

    public bool GameIsOver()
    {
        return heliPlayerWon || drivingPlayerWon;
    }
}
