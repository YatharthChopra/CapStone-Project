using UnityEngine;

// The little "press C to open" tag that floats above a door in Nin's apartment.
//   - It only appears while Nin is standing close to the door (the closest door, if there are two near each other).
//   - On a controller it shows the controller button instead of the key.
//   - Once Nin leaves the apartment (goes through a door to somewhere else) it never shows again for the rest of
//     that run (GameManager.DoorHintDone). Going back to the main menu starts a new run.
// Door adds this script to itself, so no scene or prefab has to be changed. The drawing (and the look) is in HintTag.
[RequireComponent(typeof(Door))]
public class DoorHint : MonoBehaviour
{
    // How close Nin has to be (sideways, from the edge of the door) before the tag shows up
    private const float ShowDistance = 3.5f;
    private const float ShowHeight = 4f;           // and how far above or below the door she can be
    private const float HeightAboveDoor = 0.6f;    // how far above the top of the door the tag floats (world units)
    private const float FadeSpeed = 4f;            // alpha per second

    // Only one tag shows at a time: the nearest door that Nin is close to
    private static int checkedFrame = -1;
    private static float nearestDistance;
    private static DoorHint nearestDoor;

    // The other tags (ControlHints) read this, so two tags never sit on top of each other
    public static bool IsShowing { get; private set; }

    private Door door;
    private Collider2D doorCollider;
    private float alpha;

    private void Awake()
    {
        door = GetComponent<Door>();
        doorCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        HintTag.UpdateDevice();

        // Work out which door is the nearest one Nin is close to (the first hint to run each frame resets this)
        if (Time.frameCount != checkedFrame)
        {
            checkedFrame = Time.frameCount;
            nearestDistance = float.MaxValue;
            nearestDoor = null;
        }

        float distance;
        if (IsNinNear(out distance) && distance < nearestDistance)
        {
            nearestDistance = distance;
            nearestDoor = this;
        }
    }

    private void LateUpdate()
    {
        // (every Update has run by now, so nearestDoor is settled)
        bool wantShown = nearestDoor == this && CanShow();
        alpha = Mathf.MoveTowards(alpha, wantShown ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);

        if (nearestDoor == this || nearestDoor == null) IsShowing = alpha > 0.05f;
    }

    private void OnDisable()
    {
        if (nearestDoor == this) nearestDoor = null;
        IsShowing = false;
    }

    // Is the door usable, and has the tag not already done its job?
    private bool CanShow()
    {
        if (GameManager.Instance == null || GameManager.Instance.DoorHintDone) return false;
        if (GameManager.IsPaused) return false;
        if (!door.IsUsable) return false;

        PlayerAnimator animator = GameManager.PlayerAnimator;
        return animator == null || !animator.IsDead;
    }

    private bool IsNinNear(out float distance)
    {
        distance = float.MaxValue;
        Collider2D nin = GameManager.PlayerCollider;
        if (nin == null || doorCollider == null) return false;

        Bounds doorBounds = doorCollider.bounds;
        Bounds ninBounds = nin.bounds;

        float sideways = Mathf.Abs(ninBounds.center.x - doorBounds.center.x) - doorBounds.extents.x;
        float up = Mathf.Abs(ninBounds.center.y - doorBounds.center.y);
        if (sideways > ShowDistance || up > ShowHeight) return false;

        distance = Mathf.Max(0f, sideways);
        return true;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (alpha <= 0.01f || doorCollider == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // The tag floats above the top of the door
        Bounds doorBounds = doorCollider.bounds;
        Vector3 tip = cam.WorldToScreenPoint(new Vector3(doorBounds.center.x, doorBounds.max.y + HeightAboveDoor, 0f));
        if (tip.z < 0f) return;

        string key = HintTag.UsingController ? "Y" : "C";
        HintTag.Draw(new Vector2(tip.x, tip.y), new string[] { key }, "Open the door", alpha);
    }
}
