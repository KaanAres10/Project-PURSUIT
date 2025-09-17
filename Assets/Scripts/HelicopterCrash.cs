using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterCrash : MonoBehaviour
{
    [Header("Crash Layers")]
    public LayerMask crashLayers;

    public UiManager uiManager;

    private bool hasCrashed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCrashed) return;

        if (IsInCrashLayer(collision.gameObject.layer))
        {
            hasCrashed = true;
            WhenCrashed();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCrashed) return;

        if (IsInCrashLayer(other.gameObject.layer))
        {
            hasCrashed=true;
            WhenCrashed();
        }
    }

    bool IsInCrashLayer(int objectLayer)
    {
        return (crashLayers.value & (1 << objectLayer)) != 0;
    }


    void WhenCrashed()
    {
        uiManager.ShowCrashUI();
        Debug.Log("Heli crashed uh oh stinky");
    }
}
