using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Volumetrics/Baked Volumetric Lights")]
public class BakedVolumetricLights : ScriptableObject
{
    [System.Serializable] public struct Point
    {
        public Vector3 positionWS;
        public float   range;        // meters
        public Color   color;        // linear
        [Range(0f,1000f)] public float  intensity; // extra gain
    }

    [System.Serializable] public struct Spot
    {
        public Vector3 positionWS;
        public float   range;
        public Vector3 directionWS;  // normalized
        public float   cosInner;     // cos(inner angle)
        public Color   color;
        [Range(0f,1000f)] public float  intensity;
        public float   cosOuter;     // cos(outer angle)  (<= cosInner)
    }

    public List<Point> points = new();
    public List<Spot>  spots  = new();
}