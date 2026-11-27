using UnityEngine;

// One of Nin's one-off animations: where its pictures are, how fast it plays and where her feet are in them.
// The pictures are loaded from Assets/Resources, so nothing has to be set up in the Animator or the prefab.
public class NinClip
{
    private const string NewFolder = "New Nin Animations/New Nin Animations/";
    private const float PixelsPerUnit = 100f;   // same as Nin's other pictures

    // Frame 1 of the waking pictures is left out: an .aseprite file there has the same name, and it looks like frame 2
    public static readonly NinClip WakeInBed = new NinClip(NewFolder + "Nin Waking Up/Bed_Prop{0}", 2, 10, 7f, 32, 4);
    public static readonly NinClip StandFromBed = new NinClip("sleep stand up/sleep stand up/frame{0:0000}", 0, 8, 8f, 32, 6);
    public static readonly NinClip WakeOnFloor = new NinClip(NewFolder + "Wakes Up/Frame{0:0000}", 1, 4, 3f, 37, 23);
    public static readonly NinClip Interact = new NinClip(NewFolder + "Interact/frame{0:0000}", 0, 3, 10f, 24, 5);
    public static readonly NinClip DeepBreath = new NinClip(NewFolder + "Deep Breath/frame{0:0000}", 0, 18, 8f, 35, 6);

    // Leaving a floor (we see her back)
    public static readonly NinClip StairsRightUp = new NinClip("stair right up/stair right up/frame{0:0000}", 0, 13, 12f, 34, 13);
    public static readonly NinClip StairsLeftDown = new NinClip("stair left down/stair left down/frame{0:0000}", 0, 13, 12f, 33, 29);
    // Arriving on the next floor (we see her front)
    public static readonly NinClip StairsLeftUp = new NinClip("stairs left up/stairs left up/frame{0:0000}", 0, 13, 12f, 34, 29);
    public static readonly NinClip StairsRightDown = new NinClip("stair right down/stair right down/frame{0:0000}", 0, 13, 12f, 33, 13);

    private readonly string path;         // {0} is replaced with the frame number
    private readonly int firstNumber;
    private readonly int frameCount;
    private readonly Vector2 feet;        // where her feet are in the picture, in pixels from the bottom left

    private Sprite[] frames;

    public float FramesPerSecond { get; private set; }

    private NinClip(string path, int firstNumber, int frameCount, float framesPerSecond, float feetX, float feetY)
    {
        this.path = path;
        this.firstNumber = firstNumber;
        this.frameCount = frameCount;
        FramesPerSecond = framesPerSecond;
        feet = new Vector2(feetX, feetY);
    }

    // The pictures, loaded the first time they are needed
    public Sprite[] Frames
    {
        get
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) Load();
            return frames;
        }
    }

    // Makes a sprite out of each whole picture, so it doesn't matter how Unity sliced it on import
    private void Load()
    {
        frames = new Sprite[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            Texture2D picture = Resources.Load<Texture2D>(string.Format(path, firstNumber + i));
            if (picture == null)
            {
                Debug.LogWarning("Missing animation picture: " + string.Format(path, firstNumber + i));
                frames = new Sprite[0];
                return;
            }

            picture.filterMode = FilterMode.Point;   // keeps the pixel art sharp

            Vector2 pivot = new Vector2(feet.x / picture.width, feet.y / picture.height);
            frames[i] = Sprite.Create(picture, new Rect(0f, 0f, picture.width, picture.height), pivot, PixelsPerUnit);
        }
    }
}
