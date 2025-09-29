using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VrDrivingComfort : MonoBehaviour, ITunnelingVignetteProvider
{
    [SerializeField] VignetteParameters _params = new VignetteParameters();

    public VignetteParameters vignetteParameters => _params;
}
