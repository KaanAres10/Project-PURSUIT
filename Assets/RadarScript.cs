using TMPro;
using UnityEngine;

public class RadarScript : MonoBehaviour
{
    [Header("Radar Settings")]
    public float radarRange = 100f;    // Max world distance displayed
    public float radarScale = 7.0f;    // Max distance the dot moves from center

    [Header("Radar Dots")]
    public Transform targetDot;       // HeliDot
    public float targetZOffset = -0.1f; // small offset to render on top of green dot

    [Header("UI Display")]
    public TMP_Text distanceText;      // Assign in the Inspector (TextMeshPro text)


    private Transform player;         // Sports Car
    private Transform target;         // Helicopter's TrackerScaled

    void Start()
    {
        // Find the player
        player = GameObject.Find("Sports Car 2")?.transform;
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

        // World-space offset
        Vector3 offset = target.position - player.position;

        // Convert to player's local space (so it rotates with the player)
        Vector3 localOffset = player.InverseTransformDirection(offset);

        // Only XZ plane
        Vector2 offset2D = new Vector2(localOffset.x, localOffset.z);
        //Debug.Log($"[Radar] Distance to target: {offset2D.magnitude / 10.0f:F1} meters");
        // Scale and clamp
        Vector2 radarPos = offset2D / radarRange * radarScale;
        if (radarPos.magnitude > radarScale)
            radarPos = radarPos.normalized * radarScale;

        // Apply to radar dot (relative to radar center)
        targetDot.localPosition = new Vector3(radarPos.x, radarPos.y, targetZOffset);

        // Show in UI 
        if (distanceText != null)
            distanceText.text = $" {offset2D.magnitude / 10.0f:F1} m";
    }
}
