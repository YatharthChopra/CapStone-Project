using UnityEngine;

// TEMPORARY - draws Alice's detection meter in the top-left corner so we can see it while testing the enemies.
// Delete this once there's a proper UI for it.
public class DetectionDebug : MonoBehaviour
{
    private HidingScript hiding;

    private void Start()
    {
        hiding = GetComponent<HidingScript>();
    }

    private void OnGUI()
    {
        if (hiding == null) return;

        float percent = Mathf.Clamp01(hiding.Detection / hiding.MaxDetection);
        GUI.Label(new Rect(10, 10, 300, 25), "Detection: " + Mathf.RoundToInt(hiding.Detection) + " / " + hiding.MaxDetection);
        GUI.Box(new Rect(10, 35, 200f * percent, 15), GUIContent.none);
    }
}
