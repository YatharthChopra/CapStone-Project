using System;
using System.Collections.Generic;
using UnityEngine;

// Plays Nin's one-off animations: waking up, interacting, the stairs and the deep breath.
// PlayerAnimator adds this to the Player by itself. It runs after the Animator, so its picture is the one shown.
// While an animation (other than the deep breath) plays, the player can't control Nin.
[DefaultExecutionOrder(100)]
public class NinActions : MonoBehaviour
{
    // How Nin should appear when the next room loads
    private enum Entrance { Normal, WakeInBed, WakeOnFloor, StairsUp, StairsDown }

    private static Entrance nextEntrance = Entrance.Normal;

    private const float StillTimeBeforeBreath = 8f;   // standing still this long makes her take a deep breath
    private const float StillSpeed = 0.2f;
    private const float MaxFloorDistance = 5f;        // how far down to look for the floor when she appears
    private const string BedName = "Bed";             // the bed object in Apt_Bedroom
    private const string StairsSceneEnding = "_Stairs";

    // True while Nin is in an animation the player has to wait for (GameControls ignores the keys)
    public static bool Busy { get; private set; }

    private PlayerMovement movement;
    private PlayerAnimator playerAnimator;
    private HidingScript hiding;
    private Rigidbody2D body;
    private SpriteRenderer picture;

    private readonly Queue<NinClip> waiting = new Queue<NinClip>();
    private NinClip clip;               // the animation playing now (null = none)
    private float clipTime;
    private Action whenFinished;
    private float stillTimer;
    private SpriteRenderer hiddenBed;   // the room's bed, switched off while the waking pictures draw their own

    // ---------------------------------------------------------------- what other scripts call

    // GameFlow calls these before loading the room
    public static void WakeInBedNextRoom() { nextEntrance = Entrance.WakeInBed; }
    public static void WakeOnFloorNextRoom() { nextEntrance = Entrance.WakeOnFloor; }

    // Turns toward the thing being used, plays the "reach out" animation, then runs whatever comes next (if anything)
    public static void Interact(Vector3 target, Action then = null)
    {
        NinActions nin = Find();
        if (nin == null)
        {
            if (then != null) then();
            return;
        }
        nin.playerAnimator.FaceToward(target.x);
        nin.Play(then, NinClip.Interact);
    }

    // A door was used: walk the stairs if it goes between two stairwells, otherwise reach out to the door
    public static void UseDoor(string fromScene, string toScene, Vector3 doorPosition, Action loadRoom)
    {
        NinActions nin = Find();
        bool stairs = fromScene.EndsWith(StairsSceneEnding) && toScene.EndsWith(StairsSceneEnding);

        if (nin == null || !stairs)
        {
            Interact(doorPosition, loadRoom);
            return;
        }

        // The scene names start "Floor1", "Floor2"... so comparing them tells us if we are going up
        bool goingUp = string.CompareOrdinal(toScene, fromScene) > 0;
        nextEntrance = goingUp ? Entrance.StairsUp : Entrance.StairsDown;
        nin.Play(loadRoom, goingUp ? NinClip.StairsRightUp : NinClip.StairsLeftDown);
    }

    private static NinActions Find()
    {
        PlayerAnimator animator = GameManager.PlayerAnimator;
        return animator != null ? animator.GetComponent<NinActions>() : null;
    }

    // ---------------------------------------------------------------- setting up

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        playerAnimator = GetComponent<PlayerAnimator>();
        hiding = GetComponent<HidingScript>();
        body = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        picture = playerAnimator.Picture;

        Entrance entrance = nextEntrance;
        nextEntrance = Entrance.Normal;

        // Spawn points can be a little above the floor, and she shouldn't sink into place mid-animation
        if (entrance != Entrance.Normal) PutFeetOnFloor();

        switch (entrance)
        {
            case Entrance.WakeInBed:
                HideBed();
                Play(null, NinClip.WakeInBed, NinClip.StandFromBed);
                break;
            case Entrance.WakeOnFloor:
                Play(null, NinClip.WakeOnFloor);
                break;
            case Entrance.StairsUp:
                Play(null, NinClip.StairsLeftUp);
                break;
            case Entrance.StairsDown:
                Play(null, NinClip.StairsRightDown);
                break;
        }
    }

    // Moves her straight down onto the first solid thing under her
    private void PutFeetOnFloor()
    {
        Physics2D.SyncTransforms();   // PlayerRoomSpawn has only just moved her
        Bounds box = GetComponent<BoxCollider2D>().bounds;
        int solid = ~LayerMask.GetMask("CameraColliders", "Camera");   // the camera's colliders aren't floors

        foreach (RaycastHit2D hit in Physics2D.RaycastAll(box.center, Vector2.down, MaxFloorDistance, solid))
        {
            if (hit.collider.isTrigger || hit.collider.gameObject == gameObject) continue;

            float gap = hit.distance - box.extents.y;
            transform.position += Vector3.down * gap;
            body.position = transform.position;
            return;
        }
    }

    private void OnDestroy()
    {
        Busy = false;   // a new room starts with the controls working
    }

    // Stops her walking. In normal gravity she can still fall, in Alice's gravity sections she holds still.
    private void HoldStill()
    {
        body.linearVelocity = movement.GravityFlip ? Vector2.zero : new Vector2(0f, body.linearVelocity.y);
    }

    // ---------------------------------------------------------------- playing

    private void Play(Action then, params NinClip[] clips)
    {
        waiting.Clear();
        foreach (NinClip next in clips) waiting.Enqueue(next);
        whenFinished = then;

        Busy = true;
        movement.enabled = false;
        HoldStill();
        StartNextClip();
    }

    // Moves on to the next animation in the queue, or finishes
    private void StartNextClip()
    {
        clipTime = 0f;

        while (waiting.Count > 0)
        {
            clip = waiting.Dequeue();
            if (clip != NinClip.WakeInBed) ShowBed();
            if (clip.Frames.Length > 0) return;   // skip any with missing pictures
        }

        Finish();
    }

    private void Finish()
    {
        Action then = whenFinished;
        StopPlaying();
        if (then != null) then();
    }

    private void StopPlaying()
    {
        bool wasLocked = Busy;

        clip = null;
        whenFinished = null;
        waiting.Clear();
        stillTimer = 0f;
        Busy = false;
        ShowBed();

        if (wasLocked && !playerAnimator.IsDead) movement.enabled = true;
    }

    private void FixedUpdate()
    {
        if (Busy) HoldStill();   // stay put until the animation is done
    }

    private void LateUpdate()
    {
        if (playerAnimator.IsDead)
        {
            if (clip != null) StopPlaying();   // being caught interrupts everything
            return;
        }

        if (!Busy) UpdateDeepBreath();
        if (clip == null) return;

        clipTime += Time.deltaTime;
        int frame = (int)(clipTime * clip.FramesPerSecond);

        if (frame >= clip.Frames.Length)
        {
            if (Busy) StartNextClip();
            else StopPlaying();
            if (clip == null) return;
            frame = 0;
        }

        picture.sprite = clip.Frames[frame];
        // Only the reach-out and the breath turn with her. The others are drawn facing one set way.
        if (clip != NinClip.Interact && clip != NinClip.DeepBreath) picture.flipX = false;
    }

    // ---------------------------------------------------------------- the deep breath

    // Played after standing still for a while. Any movement cancels it straight away.
    private void UpdateDeepBreath()
    {
        bool still = body.linearVelocity.magnitude < StillSpeed
                     && GameControls.MoveX == 0f
                     && !movement.IsCrouching
                     && !movement.InAlcove
                     && (hiding == null || !hiding.hiding);

        if (!still)
        {
            stillTimer = 0f;
            clip = null;
            return;
        }

        if (clip != null) return;   // already breathing

        stillTimer += Time.deltaTime;
        if (stillTimer >= StillTimeBeforeBreath && NinClip.DeepBreath.Frames.Length > 0)
        {
            stillTimer = 0f;
            clipTime = 0f;
            clip = NinClip.DeepBreath;
        }
    }

    // ---------------------------------------------------------------- the bed

    private void HideBed()
    {
        GameObject bed = GameObject.Find(BedName);
        if (bed == null) return;

        // Line her up with the bed, so the drawn bed appears exactly where the room's one was
        transform.position = new Vector3(bed.transform.position.x, transform.position.y, transform.position.z);
        body.position = transform.position;

        hiddenBed = bed.GetComponent<SpriteRenderer>();
        if (hiddenBed != null) hiddenBed.enabled = false;
    }

    private void ShowBed()
    {
        if (hiddenBed == null) return;

        hiddenBed.enabled = true;
        hiddenBed = null;
    }
}
