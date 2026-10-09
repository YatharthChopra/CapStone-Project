using UnityEngine;
using UnityEngine.SceneManagement;

// TEMPORARY camera: zooms and centres the camera so each greybox room fills the screen.
// It runs by itself whenever a scene loads (nothing to attach).
// Delete this script when Alice's camera follow is in, since the two would fight each other.
public static class RoomCameraFit
{
    private const float Margin = 0.5f;   // a little space around the room

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Fit();   // the first scene is already loaded by now
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Fit();
    }

    private static void Fit()
    {
        Camera cam = Camera.main;
        GameObject backWall = GameObject.Find("Back Wall");
        if (cam == null || backWall == null) return;   // menu scenes have no room, so skip them

        Bounds room = backWall.GetComponent<SpriteRenderer>().bounds;

        // Pick whichever is bigger: enough height to fit the room, or enough width for the screen shape
        float sizeForHeight = room.extents.y + Margin;
        float sizeForWidth = (room.extents.x + Margin) / cam.aspect;
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

        Vector3 centre = room.center;
        cam.transform.position = new Vector3(centre.x, centre.y, cam.transform.position.z);
    }
}
