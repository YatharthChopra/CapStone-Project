using UnityEngine;

// Gives the Glaring Eyes their drawings. The pictures are loaded from Assets/Resources/GlaringEye, so nothing has
// to be set up in the prefab (GlaringEyes adds this script to itself).
//   Asleep      -> a closed eye while the player is far away
//   Activating  -> the eye opens when the player comes within the sense distance, and closes again when they leave
//   Open        -> an empty eye with a separate pupil, which looks the same way as the vision cone
//                  (so while staring it stays focused on Nin)
//   Staring     -> the same, with the veins coming out around the eye
// The eye itself stays still and is only flipped when the vision cone points left.
public class GlaringEyeAnimator : MonoBehaviour
{
    private const string Folder = "GlaringEye/";
    private const int ActivatingFrameCount = 10;
    private const int VeinsFrameCount = 8;

    private const float SpriteScale = 4.5f;               // the pictures are 64 pixels, so they are scaled up
    private const float SenseDistance = 10f;              // the eye wakes up when the player is closer than this
    private const float ActivatingFramesPerSecond = 10f;
    private const float VeinsFramesPerSecond = 10f;

    // Where the pupil goes, measured in pixels of the 64 pixel picture
    private const float PixelsPerUnit = 100f;
    private const float PupilCentreX = 5.5f;              // moves the pupil picture to the middle of the eye
    private const float PupilCentreY = 4f;
    private const float PupilReachX = 7f;                 // how far it can travel left and right from the middle
    private const float PupilReachY = 2f;                 // and up and down (the eye is much wider than it is tall)

    private GlaringEyes eyes;
    private SpriteRenderer eyeRenderer;
    private SpriteRenderer pupilRenderer;                 // drawn on top of the open eye
    private SpriteRenderer veinsRenderer;                 // drawn around the eye while staring
    private Transform visual;

    private Sprite openEye;
    private Sprite pupil;
    private Sprite[] activating = new Sprite[ActivatingFrameCount];
    private Sprite[] veins = new Sprite[VeinsFrameCount];

    private float openTime;                               // seconds into the opening animation (0 = closed)

    private void Awake()
    {
        eyes = GetComponent<GlaringEyes>();

        visual = transform.Find("Visual");
        if (visual == null) { enabled = false; return; }
        eyeRenderer = visual.GetComponent<SpriteRenderer>();
        if (eyeRenderer == null) { enabled = false; return; }

        LoadFrames();

        if (openEye == null || pupil == null || activating[0] == null)
        {
            // The pictures are missing, so leave the old placeholder as it was
            enabled = false;
            return;
        }

        // Replace the blue placeholder box
        eyeRenderer.color = Color.white;
        visual.localScale = new Vector3(SpriteScale, SpriteScale, 1f);

        // The extra pictures are made in code, so the prefab doesn't change
        pupilRenderer = CreateLayer("Pupil", 1);        // just in front of the eye
        pupilRenderer.sprite = pupil;
        veinsRenderer = CreateLayer("Veins", -1);       // just behind it, so they stay around the outside
    }

    private void LoadFrames()
    {
        openEye = Resources.Load<Sprite>(Folder + "Glaring_Eye_Blank");
        pupil = Resources.Load<Sprite>(Folder + "Glaring_Eye_Pupil");

        for (int i = 0; i < ActivatingFrameCount; i++)
        {
            activating[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Activating" + (i + 1));
        }
        for (int i = 0; i < VeinsFrameCount; i++)
        {
            veins[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Veins" + (i + 1));
        }
    }

    // Makes an extra picture on the eye, drawn in front of it (+1) or behind it (-1). It starts hidden.
    private SpriteRenderer CreateLayer(string layerName, int orderFromEye)
    {
        GameObject layerObject = new GameObject(layerName);
        layerObject.transform.SetParent(visual, false);

        SpriteRenderer layer = layerObject.AddComponent<SpriteRenderer>();
        layer.sortingLayerID = eyeRenderer.sortingLayerID;
        layer.sortingOrder = eyeRenderer.sortingOrder + orderFromEye;
        layer.enabled = false;
        return layer;
    }

    private void LateUpdate()
    {
        FlipToFaceCone();

        // Open the eye while the player is near, close it when they are gone
        float openDuration = ActivatingFrameCount / ActivatingFramesPerSecond;
        openTime = Mathf.MoveTowards(openTime, PlayerIsNear() ? openDuration : 0f, Time.deltaTime);
        eyes.SetEyeOpen(openTime >= openDuration);   // the eye only sees while fully open

        if (openTime < openDuration)
        {
            ShowOpening();
            return;
        }

        eyeRenderer.sprite = openEye;
        ShowPupil();
        ShowVeins(eyes.CurrentState == GlaringEyes.State.Staring);
    }

    // True when the eye should be awake: the player is close, or the eye is already reacting to something
    private bool PlayerIsNear()
    {
        if (eyes.CurrentState != GlaringEyes.State.Idle) return true;

        PlayerMovement player = GameManager.Player;
        if (player == null) return false;

        return Vector2.Distance(transform.position, player.transform.position) <= SenseDistance;
    }

    // Plays the opening frames (frame 1 is the closed eye). Closing plays the same frames backwards.
    private void ShowOpening()
    {
        int frame = Mathf.Min((int)(openTime * ActivatingFramesPerSecond), ActivatingFrameCount - 1);
        eyeRenderer.sprite = activating[frame];
        pupilRenderer.enabled = false;   // these frames have their own pupil drawn in
        ShowVeins(false);
    }

    // Moves the pupil so it looks the same way the vision cone points
    private void ShowPupil()
    {
        float radians = eyes.GazeAngle * Mathf.Deg2Rad;
        Vector3 gaze = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);

        // Turned into the picture's own directions, so it still works if the eye has been rotated in the scene
        gaze = visual.InverseTransformDirection(gaze);

        // Rounded to whole pixels so the pixel art stays lined up
        float x = Mathf.Round(PupilCentreX + gaze.x * PupilReachX);
        float y = Mathf.Round(PupilCentreY + gaze.y * PupilReachY);

        pupilRenderer.transform.localPosition = new Vector3(x, y, 0f) / PixelsPerUnit;
        pupilRenderer.enabled = true;
    }

    // Shows the looping veins around the eye, or hides them
    private void ShowVeins(bool show)
    {
        show = show && veins[0] != null;
        veinsRenderer.enabled = show;
        if (!show) return;

        veinsRenderer.sprite = veins[(int)(Time.time * VeinsFramesPerSecond) % VeinsFrameCount];
        veinsRenderer.flipX = eyeRenderer.flipX;
    }

    // Flips the picture when the cone points to the left
    private void FlipToFaceCone()
    {
        float radians = eyes.GazeAngle * Mathf.Deg2Rad;
        eyeRenderer.flipX = Mathf.Cos(radians) < 0f;
    }
}
