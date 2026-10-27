using System.Collections.Generic;
using UnityEngine;

// The Glaring Eyes enemy
//   Idle       -> stays in place, looking toward the nearby boxes, then looking the other way,
//                 switching every few seconds
//   Distracted -> turned toward a noise ("??"), goes back to Idle after 5 seconds
//   Staring    -> the player is in its vision ("!!"). It can't attack, but the longer the player
//                 stays in view the more the detection meter fills. A full meter means game over.
//                 Goes back to Idle when the player leaves its view.
// The eyes only react to sound (not light) and never move. They only turn their gaze.
public class GlaringEyes : EnemyBase
{
    public enum State { Idle, Distracted, Staring }

    [SerializeField] private State state = State.Idle;

    // The audio manager reads this to decide when to play the tense music
    public State CurrentState { get { return state; } }

    [Header("Looking")]
    [SerializeField] private float lookAtBoxesTime = 4f;    // seconds spent looking toward the boxes
    [SerializeField] private float lookAwayTime = 3f;       // seconds spent looking the other way
    [SerializeField] private float turnSpeed = 360f;        // how fast the gaze turns (degrees per second)
    [SerializeField] private float boxSearchRadius = 25f;   // how far to look for boxes to watch
    // Used only if there are no boxes nearby to look at:
    [SerializeField] private bool restFacesRight = false;
    [SerializeField] private Vector2 restLookDirection = Vector2.zero;

    [Header("Timers")]
    [SerializeField] private float distractedTime = 5f;     // how long it looks toward a noise

    [Header("Staring")]
    // How much the meter goes up each second on top of Alice's normal detection.
    // The meter is 100, so 15 means about 7 seconds by itself.
    [SerializeField] private float stareDetectionPerSecond = 15f;

    private float stateTimer;

    private Vector2 lookTowardBoxes;   // the direction to the boxes
    private Vector2 lookAway;          // the same height, but the other side
    private bool lookingAtBoxes = true;
    private float gazeTimer;

    protected override void Start()
    {
        // The eyes turn smoothly (this goes before base.Start so the first look is set up properly)
        turnSpeedDegrees = turnSpeed;

        base.Start();
        WorkOutGazeDirections();
        ChangeState(State.Idle);
    }

    // Works out the two directions the eyes look between
    private void WorkOutGazeDirections()
    {
        Vector2 first;
        Vector2 second;
        if (!TryFindGapDirections(out first, out second))
        {
            // No boxes nearby, so fall back to the direction set in the Inspector
            first = (restLookDirection != Vector2.zero) ? restLookDirection : (restFacesRight ? Vector2.right : Vector2.left);
            second = new Vector2(-first.x, first.y);   // flip left/right, keep the same up/down
        }

        // Make sure there is some left/right in each one, so the two directions are really different
        if (Mathf.Abs(first.x) < 0.3f) first.x = restFacesRight ? 0.3f : -0.3f;
        if (Mathf.Abs(second.x) < 0.3f) second.x = -Mathf.Sign(first.x) * 0.3f;

        lookTowardBoxes = first.normalized;
        lookAway = second.normalized;
    }

    // Finds the two gaps between boxes that are closest to this eye, and gives back the direction to each.
    // The eye switches between watching one gap and the other, so the player can cross a gap while the eye is
    // looking at the other one. (Looking right at a box would be pointless, Nin is hidden behind it.)
    // With only one gap (or one box) the second direction is the same one mirrored to the other side.
    private bool TryFindGapDirections(out Vector2 nearestGap, out Vector2 secondGap)
    {
        nearestGap = Vector2.zero;
        secondGap = Vector2.zero;
        Vector2 eyePosition = transform.position;

        // All the boxes close to this eye, from left to right
        List<Vector2> boxes = new List<Vector2>();
        foreach (GameObject box in GameObject.FindGameObjectsWithTag("CrouchingSpot"))
        {
            Vector2 boxPosition = box.transform.position;
            if (Vector2.Distance(eyePosition, boxPosition) <= boxSearchRadius) boxes.Add(boxPosition);
        }

        if (boxes.Count == 0) return false;
        boxes.Sort((a, b) => a.x.CompareTo(b.x));

        // The middle of each space between two boxes that are next to each other
        List<Vector2> gaps = new List<Vector2>();
        for (int i = 0; i < boxes.Count - 1; i++)
        {
            gaps.Add((boxes[i] + boxes[i + 1]) / 2f);
        }

        // Only one box: just look at it
        if (gaps.Count == 0) gaps.Add(boxes[0]);

        // The two gaps closest to the eye
        gaps.Sort((a, b) => Vector2.Distance(eyePosition, a).CompareTo(Vector2.Distance(eyePosition, b)));

        nearestGap = gaps[0] - eyePosition;
        if (gaps.Count >= 2) secondGap = gaps[1] - eyePosition;
        else secondGap = new Vector2(-nearestGap.x, nearestGap.y);

        return nearestGap.sqrMagnitude > 0.01f;
    }

    // Points the cone at whichever side the eye is supposed to be watching right now
    private void LookAtCurrentGaze()
    {
        LookInDirection(lookingAtBoxes ? lookTowardBoxes : lookAway);
    }

    protected override void HearNoise(Vector2 position)
    {
        // Already staring at the player, so it ignores noises
        if (state == State.Staring) return;

        LookAt(position);
        ChangeState(State.Distracted);   // (this restarts the 5 seconds if it was already distracted)
    }

    private void Update()
    {
        if (player == null) return;

        stateTimer += Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                if (CanSeePlayer()) ChangeState(State.Staring);
                else SwitchGazeOverTime();
                break;

            case State.Distracted:
                if (CanSeePlayer()) ChangeState(State.Staring);
                else if (stateTimer >= distractedTime) ChangeState(State.Idle);
                break;

            case State.Staring:
                UpdateStaring();
                break;
        }
    }

    // While idle, swap between looking at the boxes and looking the other way
    private void SwitchGazeOverTime()
    {
        gazeTimer += Time.deltaTime;
        float timeForThisGaze = lookingAtBoxes ? lookAtBoxesTime : lookAwayTime;

        if (gazeTimer >= timeForThisGaze)
        {
            gazeTimer = 0f;
            lookingAtBoxes = !lookingAtBoxes;
            LookAtCurrentGaze();
        }
    }

    private void UpdateStaring()
    {
        LookAt(player.transform.position);

        if (!CanSeePlayer())
        {
            // Player walked out of view (or hid)
            ChangeState(State.Idle);
            return;
        }

        // Keep filling the meter while the player is being stared at
        if (playerHiding != null)
        {
            playerHiding.AddDetection(stareDetectionPerSecond * Time.deltaTime);
        }

        if (DetectionMeterFull())
        {
            GameFlow.PlayerDied();
        }
    }

    private void ChangeState(State newState)
    {
        state = newState;
        stateTimer = 0f;
        SetStateLabel(newState.ToString());

        switch (newState)
        {
            case State.Idle:
                ClearSignal();
                gazeTimer = 0f;
                LookAtCurrentGaze();   // go back to watching
                break;
            case State.Distracted:
                ShowQuestion();
                break;
            case State.Staring:
                ShowAlert();
                break;
        }
    }

    // Puts the eyes back to how they started
    public void ResetEnemy()
    {
        lookingAtBoxes = true;
        ChangeState(State.Idle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}
