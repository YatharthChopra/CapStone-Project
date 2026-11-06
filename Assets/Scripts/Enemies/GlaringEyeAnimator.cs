using UnityEngine;

// Gives the Glaring Eyes their drawings. The pictures are loaded from Assets/Resources/GlaringEye, so nothing has
// to be set up in the prefab (GlaringEyes adds this script to itself).
//   Idle        -> the blank eye, with a pupil that looks the way the eye is watching
//   Distracted  -> the "searching" animation
//   Staring     -> the "alert" animation
public class GlaringEyeAnimator : MonoBehaviour
{
    private const string Folder = "GlaringEye/";
    private const int FrameCount = 5;

    private const float SpriteScale = 3f;          // the pictures are 64 pixels, so they are scaled up
    private const float SearchingFramesPerSecond = 6f;
    private const float AlertFramesPerSecond = 10f;
    // How far the pupil can move from the middle of the eye. These are in the picture's own size (the eye is
    // scaled up by SpriteScale after), so they are small: the white of the eye is about 0.4 wide and 0.15 tall.
    private const float PupilReachX = 0.1f;        // sideways
    private const float PupilReachY = 0.015f;      // up/down (the pupil is nearly as tall as the eye, so only a little)

    // The pupil drawing isn't centred in its own picture (it's about 6 pixels left and 4 pixels low),
    // so this puts it back in the middle of the white of the eye
    private static readonly Vector3 PupilCentre = new Vector3(0.06f, 0.04f, 0f);

    private GlaringEyes eyes;
    private SpriteRenderer eyeRenderer;
    private SpriteRenderer pupilRenderer;

    private Sprite blank;
    private Sprite[] searching = new Sprite[FrameCount];
    private Sprite[] alert = new Sprite[FrameCount];

    private GlaringEyes.State shownState;
    private float animationTime;

    private void Awake()
    {
        eyes = GetComponent<GlaringEyes>();

        Transform visual = transform.Find("Visual");
        if (visual == null) { enabled = false; return; }
        eyeRenderer = visual.GetComponent<SpriteRenderer>();
        if (eyeRenderer == null) { enabled = false; return; }

        blank = Resources.Load<Sprite>(Folder + "Glaring_Eye_Blank");
        for (int i = 0; i < FrameCount; i++)
        {
            searching[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Searching" + (i + 1));
            alert[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Alert" + (i + 1));
        }

        if (blank == null)
        {
            // The pictures are missing, so leave the old placeholder as it was
            enabled = false;
            return;
        }

        // Replace the blue placeholder box
        eyeRenderer.color = Color.white;
        visual.localScale = new Vector3(SpriteScale, SpriteScale, 1f);

        // The pupil is its own little picture on top of the eye
        GameObject pupil = new GameObject("Pupil");
        pupil.transform.SetParent(visual, false);
        pupilRenderer = pupil.AddComponent<SpriteRenderer>();
        pupilRenderer.sprite = Resources.Load<Sprite>(Folder + "Glaring_Eye_Pupil");
        pupilRenderer.sharedMaterial = eyeRenderer.sharedMaterial;
        pupilRenderer.sortingLayerID = eyeRenderer.sortingLayerID;
        pupilRenderer.sortingOrder = eyeRenderer.sortingOrder + 1;
    }

    private void LateUpdate()
    {
        GlaringEyes.State state = eyes.CurrentState;
        if (state != shownState) animationTime = 0f;
        shownState = state;
        animationTime += Time.deltaTime;

        switch (state)
        {
            case GlaringEyes.State.Distracted:
                eyeRenderer.sprite = Frame(searching, SearchingFramesPerSecond);
                pupilRenderer.enabled = false;
                break;

            case GlaringEyes.State.Staring:
                eyeRenderer.sprite = Frame(alert, AlertFramesPerSecond);
                pupilRenderer.enabled = false;
                break;

            default:
                eyeRenderer.sprite = blank;
                pupilRenderer.enabled = pupilRenderer.sprite != null;
                float radians = eyes.GazeAngle * Mathf.Deg2Rad;
                pupilRenderer.transform.localPosition = PupilCentre + new Vector3(Mathf.Cos(radians) * PupilReachX, Mathf.Sin(radians) * PupilReachY, 0f);
                break;
        }
    }

    // The picture for this moment of a looping animation (falls back to the blank eye if a picture is missing)
    private Sprite Frame(Sprite[] frames, float framesPerSecond)
    {
        Sprite sprite = frames[(int)(animationTime * framesPerSecond) % FrameCount];
        return sprite != null ? sprite : blank;
    }
}
