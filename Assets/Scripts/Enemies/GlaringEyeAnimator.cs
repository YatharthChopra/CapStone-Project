using UnityEngine;

// Gives the Glaring Eyes their drawings. The pictures are loaded from Assets/Resources/GlaringEye, so nothing has
// to be set up in the prefab (GlaringEyes adds this script to itself).
//   Asleep      -> a closed eye while the player is far away
//   Activating  -> the eye opens when the player comes within the sense distance, and closes again when they leave
//   Idle        -> the "searching" animation, played slowly (the eye looking around)
//   Distracted  -> the same animation, played faster (it heard something)
//   Staring     -> the "alert" animation, with the veins coming out around the eye
// The picture stays still and is only flipped when the vision cone points left.
public class GlaringEyeAnimator : MonoBehaviour
{
    private const string Folder = "GlaringEye/";
    private const int FrameCount = 5;
    private const int ActivatingFrameCount = 10;
    private const int VeinsFrameCount = 8;

    private const float SpriteScale = 4.5f;               // the pictures are 64 pixels, so they are scaled up
    private const float SenseDistance = 10f;              // the eye wakes up when the player is closer than this
    private const float IdleFramesPerSecond = 5f;
    private const float DistractedFramesPerSecond = 10f;
    private const float AlertFramesPerSecond = 10f;
    private const float ActivatingFramesPerSecond = 10f;
    private const float VeinsFramesPerSecond = 10f;

    private GlaringEyes eyes;
    private SpriteRenderer eyeRenderer;
    private SpriteRenderer veinsRenderer;                 // drawn around the eye while staring
    private Transform visual;

    private Sprite[] searching = new Sprite[FrameCount];
    private Sprite[] alert = new Sprite[FrameCount];
    private Sprite[] activating = new Sprite[ActivatingFrameCount];
    private Sprite[] veins = new Sprite[VeinsFrameCount];

    private GlaringEyes.State shownState;
    private float animationTime;
    private float openTime;                               // seconds into the opening animation (0 = closed)

    private void Awake()
    {
        eyes = GetComponent<GlaringEyes>();

        visual = transform.Find("Visual");
        if (visual == null) { enabled = false; return; }
        eyeRenderer = visual.GetComponent<SpriteRenderer>();
        if (eyeRenderer == null) { enabled = false; return; }

        LoadFrames();

        if (searching[0] == null || activating[0] == null)
        {
            // The pictures are missing, so leave the old placeholder as it was
            enabled = false;
            return;
        }

        // Replace the blue placeholder box
        eyeRenderer.color = Color.white;
        visual.localScale = new Vector3(SpriteScale, SpriteScale, 1f);

        CreateVeinsRenderer();
    }

    private void LoadFrames()
    {
        for (int i = 0; i < FrameCount; i++)
        {
            searching[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Searching" + (i + 1));
            alert[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Alert" + (i + 1));
        }
        for (int i = 0; i < ActivatingFrameCount; i++)
        {
            activating[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Activating" + (i + 1));
        }
        for (int i = 0; i < VeinsFrameCount; i++)
        {
            veins[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Veins" + (i + 1));
        }
    }

    // The veins are a second picture around the outside of the eye, drawn just behind it
    // (made in code, so the prefab doesn't change)
    private void CreateVeinsRenderer()
    {
        if (veins[0] == null) return;

        GameObject veinsObject = new GameObject("Veins");
        veinsObject.transform.SetParent(visual, false);

        veinsRenderer = veinsObject.AddComponent<SpriteRenderer>();
        veinsRenderer.sortingLayerID = eyeRenderer.sortingLayerID;
        veinsRenderer.sortingOrder = eyeRenderer.sortingOrder - 1;
        veinsRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        GlaringEyes.State state = eyes.CurrentState;
        if (state != shownState) animationTime = 0f;
        shownState = state;
        animationTime += Time.deltaTime;

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

        switch (state)
        {
            case GlaringEyes.State.Distracted:
                eyeRenderer.sprite = Frame(searching, DistractedFramesPerSecond);
                break;

            case GlaringEyes.State.Staring:
                eyeRenderer.sprite = Frame(alert, AlertFramesPerSecond);
                break;

            default:
                eyeRenderer.sprite = Frame(searching, IdleFramesPerSecond);
                break;
        }

        ShowVeins(state == GlaringEyes.State.Staring);
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
        ShowVeins(false);
    }

    // Shows the looping veins around the eye, or hides them
    private void ShowVeins(bool show)
    {
        if (veinsRenderer == null) return;

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

    // The picture for this moment of a looping animation (falls back to the first searching picture if one is missing)
    private Sprite Frame(Sprite[] frames, float framesPerSecond)
    {
        Sprite sprite = frames[(int)(animationTime * framesPerSecond) % FrameCount];
        return sprite != null ? sprite : searching[0];
    }
}
