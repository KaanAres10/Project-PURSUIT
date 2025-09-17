using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject crashPanel;

    private bool gameOver = false;


    public void ShowWinUI()
    {
        if (gameOver) return; gameOver = true;

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (crashPanel != null)
        {
            crashPanel.SetActive(false);
        }
    }


    public void ShowCrashUI()
    {
        if (gameOver) return; gameOver = true;

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (crashPanel != null)
        {
            crashPanel.SetActive(true);
        }
    }


}
