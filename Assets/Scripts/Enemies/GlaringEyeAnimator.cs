using UnityEngine;

// Gives the Glaring Eyes their drawings. The pictures are loaded from Assets/Resources/GlaringEye, so nothing has
// to be set up in the prefab (GlaringEyes adds this script to itself).
//   Idle        -> the "searching" animation, played slowly (the eye looking around)
//   Distracted  -> the same animation, played faster (it heard something)
//   Staring     -> the "alert" animation
public class GlaringEyeAnimator : MonoBehaviour
{
    private const string Folder = "GlaringEye/";
    private const int FrameCount = 5;

    private const float SpriteScale = 3f;                 // the pictures are 64 pixels, so they are scaled up
    private const float IdleFramesPerSecond = 5f;
    private const float DistractedFramesPerSecond = 10f;
    private const float AlertFramesPerSecond = 10f;

    private GlaringEyes eyes;
    private SpriteRenderer eyeRenderer;

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

        for (int i = 0; i < FrameCount; i++)
        {
            searching[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Searching" + (i + 1));
            alert[i] = Resources.Load<Sprite>(Folder + "Glaring_Eye_Alert" + (i + 1));
        }

        if (searching[0] == null)
        {
            // The pictures are missing, so leave the old placeholder as it was
            enabled = false;
            return;
        }

        // Replace the blue placeholder box
        eyeRenderer.color = Color.white;
        visual.localScale = new Vector3(SpriteScale, SpriteScale, 1f);
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
                eyeRenderer.sprite = Frame(searching, DistractedFramesPerSecond);
                break;

            case GlaringEyes.State.Staring:
                eyeRenderer.sprite = Frame(alert, AlertFramesPerSecond);
                break;

            default:
                eyeRenderer.sprite = Frame(searching, IdleFramesPerSecond);
                break;
        }
    }

    // The picture for this moment of a looping animation (falls back to the first searching picture if one is missing)
    private Sprite Frame(Sprite[] frames, float framesPerSecond)
    {
        Sprite sprite = frames[(int)(animationTime * framesPerSecond) % FrameCount];
        return sprite != null ? sprite : searching[0];
    }
}
