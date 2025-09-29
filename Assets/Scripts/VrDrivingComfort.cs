using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VrDrivingComfort : MonoBehaviour
{

    public float vignetteIntensity = 0.5f;


    [Header("References")]
    public Transform playerRig;
    public Transform cockpit;
    public Volume postProcessVolume;

    private Vignette vignette;

    // Start is called before the first frame update
    void Start()
    {
        if(postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
        }
    }

    // Update is called once per frame
    void Update()
    {
        vignette.intensity.value = vignetteIntensity;
    }
}
