using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FuzzyEffectController : MonoBehaviour
{
    [Header("References")]
    public Transform trackerScaled;
    public GameObject fuzzyOverlay;
    public Image speedBar;

    [Header("Settings")]
    public float maxSpeed = 20f;
    public float fuzzyCooldown = 1f;
    public Color normalColor = Color.green;
    public Color dangerColor = Color.red;

    private bool isFuzzy = false;
    private Vector3 lastPosition;
    private float fuzzyTimer = 0f;
    private float currentSpeed = 0f;

    // Start is called before the first frame update
    void Start()
    {

        lastPosition = trackerScaled.position;

        SetFuzzy(false);  

        if(speedBar != null)
        {
            speedBar.fillAmount = 0f;
            speedBar.color = normalColor;
        }
    }


    private void Update()
    {
        currentSpeed = (trackerScaled.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = trackerScaled.position;
        Debug.Log(currentSpeed);

        if(speedBar != null)
        {
            float normalized = Mathf.Clamp01(currentSpeed/maxSpeed);
            speedBar.fillAmount = normalized;
            speedBar.color = Color.Lerp(normalColor, dangerColor, normalized);

            if(currentSpeed > maxSpeed)
            {
                EnterFuzzy();
            }
            else if (isFuzzy)
            {
                fuzzyTimer = Time.deltaTime;
                if (fuzzyTimer < 0f)
                    ExitFuzzy();
            }
        }
    }

    private void EnterFuzzy()
    {
        if (!isFuzzy)
        {
            Debug.Log("Helicopter too fast. view fuzzing");
            SetFuzzy(true);
        }
        fuzzyTimer = fuzzyCooldown;
    }

    private void ExitFuzzy()
    {
        Debug.Log("Heli slowed down, view clearing");
        SetFuzzy(false);
    }


    public void SetFuzzy(bool fuzzy)
    {
        if (isFuzzy == fuzzy) return;
        isFuzzy = fuzzy;

        if(fuzzyOverlay != null)
        {
            fuzzyOverlay.SetActive(fuzzy);
        }

        Debug.Log(fuzzy ? "Projector view Fuzzy" : "Projector view clear");

    }


    public bool IsFuzzy() {  return isFuzzy; }

    public bool CanDetectCar() { return !isFuzzy; }

}
