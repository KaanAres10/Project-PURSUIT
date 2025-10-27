using UnityEngine;

public class RadarScript : MonoBehaviour
{
    [Header("Radar Settings")]
    public float radarRange = 100f;    // Max world distance displayed
    public float radarScale = 7.0f;    // Max distance the dot moves from center

    [Header("Radar Dots")]
    public Transform targetDot;       // HeliDot (now a sibling of CarDot)
    public float targetZOffset = -0.1f; // small offset to render on top of green dot

    private Transform player;         // Sports Car
    private Transform target;         // Helicopter's TrackerScaled

    void Start()
    {
        // Find the player
        player = GameObject.Find("Sports Car")?.transform;
        if (player == null) Debug.LogError("Sports Car not found!");

        // Find the helicopter's tracker
        GameObject trackerObj = GameObject.Find("Helicopter/TrackerScaled");
        if (trackerObj != null)
            target = trackerObj.transform;
        else
            Debug.LogError("Helicopter/TrackerScaled not found!");

        if (targetDot == null)
            Debug.LogError("HeliDot not assigned!");
    }

    void Update()
    {
        if (player == null || target == null || targetDot == null)
            return;

        // Offset in world space
        Vector3 offset = target.position - player.position;

        // Only XZ plane
        Vector2 offset2D = new Vector2(offset.x, offset.z);

        // Scale and clamp
        Vector2 radarPos = offset2D / radarRange * radarScale;
        if (radarPos.magnitude > radarScale)
            radarPos = radarPos.normalized * radarScale;

        // Move dot relative to radar origin (green dot center)
        // Apply tiny Z offset so red dot renders on top
        targetDot.localPosition = new Vector3(radarPos.x, radarPos.y, targetZOffset);
    }
}
