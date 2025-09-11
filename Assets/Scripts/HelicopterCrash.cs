using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterCrash : MonoBehaviour
{
    public string crashTag = "Buildings";

    public UiManager uiManager;

    private bool hasCrashed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCrashed) return;

        if (collision.gameObject.CompareTag(crashTag))
        {
            hasCrashed = true;
            WhenCrashed();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCrashed) return;

        if (other.CompareTag(crashTag))
        {
            hasCrashed=true;
            WhenCrashed();
        }
    }


    void WhenCrashed()
    {
        uiManager.ShowCrashUI();
        Debug.Log("Heli crashed uh oh stinky");
    }
}
