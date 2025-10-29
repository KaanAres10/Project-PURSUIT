using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HelicopterCrash : MonoBehaviour
{
    [Header("Crash Settings")]
    public LayerMask crashLayers;
    public UiManager uiManager;
    public int maxLives = 3;
    public float flashDuration = 2f;
    public float recoveryTime = 2f;

    [Header("UI Elements")]
    public Image crashFlash;        // Red full-screen flash image
    public Image[] lifeBars;        // 3 red bars representing lives

    private int currentLives;
    private bool isRecovering = false;
    private bool isGameOver = false;

    private void Start()
    {
        currentLives = maxLives;
        UpdateLivesUI();

        if (crashFlash != null)
            crashFlash.color = new Color(1, 0, 0, 0); // transparent
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isRecovering || isGameOver) return;

        if (IsInCrashLayer(collision.gameObject.layer))
        {
            StartCoroutine(HandleCrash());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRecovering || isGameOver) return;

        if (IsInCrashLayer(other.gameObject.layer))
        {
            StartCoroutine(HandleCrash());
        }
    }

    bool IsInCrashLayer(int objectLayer)
    {
        return (crashLayers.value & (1 << objectLayer)) != 0;
    }

    IEnumerator HandleCrash()
    {
        isRecovering = true;
        currentLives--;
        Debug.Log($"Helicopter crashed! Lives left: {currentLives}");

        UpdateLivesUI();
        StartCoroutine(FlashScreenRed());

        if (currentLives <= 0)
        {
            GameOver();
            yield break;
        }

        // Give a short "recovery" period where player can move out of obstacles
        yield return new WaitForSeconds(recoveryTime);
        isRecovering = false;
    }

    private void GameOver()
    {
        isGameOver = true;
        GameManager.Instance.HeliCrashed();
    }

    private IEnumerator FlashScreenRed()
    {
        float timer = 0f;

        // Fade in red flash
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.7f, timer / flashDuration);
            crashFlash.color = new Color(1, 0, 0, alpha);
            yield return null;
        }

        // Fade out
        timer = 0f;
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0.7f, 0f, timer / flashDuration);
            crashFlash.color = new Color(1, 0, 0, alpha);
            yield return null;
        }
    }

    private void UpdateLivesUI()
    {
        for (int i = 0; i < lifeBars.Length; i++)
        {
            lifeBars[i].enabled = (i < currentLives);
        }
    }
}
