using UnityEngine;
using UnityEngine.SceneManagement;

// Teaches the player the basic controls with little tags (drawn by HintTag, the same look as the door one):
//   Walk    -> "< >  Walk" over Nin when she first wakes up. Goes away as soon as she has walked a few steps.
//   Throw   -> "Z  Hold, then release to throw", in the apartment, once she has walked. Goes away when she throws.
//   Hide    -> "^  Hide in the alcove" over an alcove when she walks up to one. Goes away when she hides.
//   Crouch  -> "v  Crouch to hide behind it" over a crouch box when she walks up to one. Goes away when she crouches.
// Each one only shows until she has done the thing once, and then never again for the rest of the run
// (GameManager remembers; going back to the main menu starts a new run). Only one tag shows at a time, and none
// while the door tag is showing, the game is paused, or Nin is dead.
// This makes itself when the game starts, so no scene or prefab has to be changed.
public class ControlHints : MonoBehaviour
{
    private const string WalkName = "walk";
    private const string ThrowName = "throw";
    private const string HideName = "hide";
    private const string CrouchName = "crouch";

    private const int WalkIndex = 0;
    private const int ThrowIndex = 1;
    private const int HideIndex = 2;
    private const int CrouchIndex = 3;
    private const int HintCount = 4;

    private const string ApartmentScenePrefix = "Apt_";   // Nin's apartment rooms
    private const float WalkShowDelay = 0.8f;             // seconds after the room loads before the walk tag appears
    private const float WalkDistance = 3f;                // she has "learned to walk" after moving this far...
    private const float WalkInputTime = 1.5f;             // ...or after holding a direction for this long
    private const float ThrowShowDelay = 2.5f;            // seconds into a room before the throw tag appears
    private const float ThrowShowLimit = 12f;             // the throw tag never stays up longer than this in total
    private const float SpotShowDistance = 2.5f;          // how close to an alcove / crouch box before its tag appears
    private const float SpotShowHeight = 4f;
    private const float FadeSpeed = 4f;                   // alpha per second
    private const float HeightAboveHead = 0.7f;           // world units above Nin's head
    private const float HeightAboveSpot = 0.5f;           // world units above an alcove / box

    private float[] alpha = new float[HintCount];
    private bool[] wanted = new bool[HintCount];
    private Vector3[] anchor = new Vector3[HintCount];    // where each tag points (world position)

    private PlayerMovement nin;
    private float findNinTimer;
    private float sceneTime;           // seconds since this room loaded
    private float walkStartX;
    private bool walkStarted;
    private float walkInputTime;
    private float throwShownTime;      // how long the throw tag has been up in total

    private GameObject[] alcoves = new GameObject[0];
    private GameObject[] crouchBoxes = new GameObject[0];
    private float refreshSpotsTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindAnyObjectByType<ControlHints>() != null) return;

        GameObject holder = new GameObject("ControlHints");
        holder.AddComponent<ControlHints>();
        DontDestroyOnLoad(holder);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // A new room: start the room timers again, and forget the old room's things
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        nin = null;
        sceneTime = 0f;
        walkStarted = false;
        refreshSpotsTimer = 0f;
        alcoves = new GameObject[0];
        crouchBoxes = new GameObject[0];
        for (int i = 0; i < HintCount; i++) alpha[i] = 0f;
    }

    private void Update()
    {
        HintTag.UpdateDevice();

        for (int i = 0; i < HintCount; i++) wanted[i] = false;

        GameManager manager = GameManager.Instance;
        if (manager != null && FindNin())
        {
            sceneTime += Time.unscaledDeltaTime;
            RefreshSpots();
            CheckHints(manager);
        }

        // Fade each tag in or out
        for (int i = 0; i < HintCount; i++)
        {
            alpha[i] = Mathf.MoveTowards(alpha[i], wanted[i] ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
        }
    }

    // Finds Nin (not every frame, in the menus there is no Nin to find)
    private bool FindNin()
    {
        if (nin == null)
        {
            findNinTimer -= Time.unscaledDeltaTime;
            if (findNinTimer > 0f) return false;

            findNinTimer = 0.5f;
            nin = GameManager.Player;
        }
        return nin != null;
    }

    // Alcoves and crouch boxes are looked up now and then (they don't move)
    private void RefreshSpots()
    {
        refreshSpotsTimer -= Time.unscaledDeltaTime;
        if (refreshSpotsTimer > 0f) return;

        refreshSpotsTimer = 1f;
        alcoves = GameObject.FindGameObjectsWithTag("HidingSpot");
        crouchBoxes = GameObject.FindGameObjectsWithTag("CrouchingSpot");
    }

    private void CheckHints(GameManager manager)
    {
        // Learning things: these count even while no tag is showing
        if (GameControls.ThrowPressed) manager.MarkHintDone(ThrowName);

        HidingScript hiding = GameManager.PlayerHiding;
        if (hiding != null && hiding.NearHidingPlace && GameControls.HidePressed) manager.MarkHintDone(HideName);

        bool nearCrouchBox = NearestSpot(crouchBoxes, out _);
        if (nearCrouchBox && GameControls.CrouchHeld) manager.MarkHintDone(CrouchName);

        // No tags while paused, while Nin is dead, or while the door tag is up
        PlayerAnimator animator = GameManager.PlayerAnimator;
        bool dead = animator != null && animator.IsDead;
        if (GameManager.IsPaused || dead || DoorHint.IsShowing) return;

        Collider2D ninCollider = GameManager.PlayerCollider;
        if (ninCollider == null) return;
        Vector3 aboveNin = new Vector3(ninCollider.bounds.center.x, ninCollider.bounds.max.y + HeightAboveHead, 0f);

        // Hide and crouch tags are the most useful, so they come first
        Vector3 spotAnchor;
        bool hidden = hiding != null && hiding.hiding;

        if (!manager.IsHintDone(HideName) && !hidden && NearestSpot(alcoves, out spotAnchor))
        {
            wanted[HideIndex] = true;
            anchor[HideIndex] = spotAnchor;
            return;
        }

        if (!manager.IsHintDone(CrouchName) && !GameManager.Player.IsCrouching && NearestSpot(crouchBoxes, out spotAnchor))
        {
            wanted[CrouchIndex] = true;
            anchor[CrouchIndex] = spotAnchor;
            return;
        }

        // Walking and throwing are taught in Nin's apartment
        bool inApartment = SceneManager.GetActiveScene().name.StartsWith(ApartmentScenePrefix);
        if (!inApartment) return;

        if (!manager.IsHintDone(WalkName))
        {
            if (!walkStarted)
            {
                walkStarted = true;
                walkStartX = nin.transform.position.x;
            }
            if (Mathf.Abs(GameControls.MoveX) > 0.1f) walkInputTime += Time.unscaledDeltaTime;

            bool hasWalked = Mathf.Abs(nin.transform.position.x - walkStartX) >= WalkDistance || walkInputTime >= WalkInputTime;
            if (hasWalked)
            {
                manager.MarkHintDone(WalkName);
            }
            else if (sceneTime >= WalkShowDelay)
            {
                wanted[WalkIndex] = true;
                anchor[WalkIndex] = aboveNin;
            }
            return;
        }

        if (!manager.IsHintDone(ThrowName) && sceneTime >= ThrowShowDelay && throwShownTime < ThrowShowLimit)
        {
            wanted[ThrowIndex] = true;
            anchor[ThrowIndex] = aboveNin;
            throwShownTime += Time.unscaledDeltaTime;
        }
    }

    // Is there an alcove / box close to Nin? Gives back the spot above the nearest one for the tag to point at.
    private bool NearestSpot(GameObject[] spots, out Vector3 tipPosition)
    {
        tipPosition = Vector3.zero;
        if (nin == null) return false;

        Vector3 ninPosition = nin.transform.position;
        float nearest = float.MaxValue;
        bool found = false;

        foreach (GameObject spot in spots)
        {
            if (spot == null || !spot.activeInHierarchy) continue;

            Collider2D spotCollider = spot.GetComponent<Collider2D>();
            Bounds bounds = spotCollider != null ? spotCollider.bounds : new Bounds(spot.transform.position, Vector3.one);

            float sideways = Mathf.Abs(ninPosition.x - bounds.center.x) - bounds.extents.x;
            float up = Mathf.Abs(ninPosition.y - bounds.center.y);
            if (sideways > SpotShowDistance || up > SpotShowHeight) continue;

            if (sideways < nearest)
            {
                nearest = sideways;
                tipPosition = new Vector3(bounds.center.x, bounds.max.y + HeightAboveSpot, 0f);
                found = true;
            }
        }
        return found;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        bool pad = HintTag.UsingController;
        DrawHint(cam, WalkIndex, new string[] { "<", ">" }, "Walk");
        DrawHint(cam, ThrowIndex, new string[] { pad ? "X" : "Z" }, "Hold, then release to throw");
        DrawHint(cam, HideIndex, new string[] { "^" }, "Hide in the alcove");
        DrawHint(cam, CrouchIndex, new string[] { "v" }, "Crouch to hide behind it");
    }

    private void DrawHint(Camera cam, int index, string[] keys, string words)
    {
        if (alpha[index] <= 0.01f) return;

        Vector3 tip = cam.WorldToScreenPoint(anchor[index]);
        if (tip.z < 0f) return;

        HintTag.Draw(new Vector2(tip.x, tip.y), keys, words, alpha[index]);
    }
}
