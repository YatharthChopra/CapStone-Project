using UnityEngine;

// The Glaring Eyes enemy. Follows the second FSM from Jasmine's write-up:
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
        Vector2 toBoxes;
        if (!TryFindBoxDirection(out toBoxes))
        {
            // No boxes nearby, so fall back to the direction set in the Inspector
            toBoxes = (restLookDirection != Vector2.zero) ? restLookDirection : (restFacesRight ? Vector2.right : Vector2.left);
        }

        // Make sure there is some left/right in it, so "the other side" is a different direction
        if (Mathf.Abs(toBoxes.x) < 0.3f)
        {
            toBoxes.x = restFacesRight ? 0.3f : -0.3f;
        }

        lookTowardBoxes = toBoxes.normalized;
        lookAway = new Vector2(-toBoxes.x, toBoxes.y).normalized;   // flip left/right, keep the same up/down
    }

    // Finds the boxes close to this eye and gives back the direction to look at them
    private bool TryFindBoxDirection(out Vector2 direction)
    {
        direction = Vector2.zero;
        Vector2 total = Vector2.zero;
        int count = 0;

        foreach (GameObject box in GameObject.FindGameObjectsWithTag("CrouchingSpot"))
        {
            if (Vector2.Distance(transform.position, box.transform.position) > boxSearchRadius) continue;
            total += (Vector2)box.transform.position;
            count++;
        }

        if (count == 0) return false;

        Vector2 middleOfBoxes = total / count;
        direction = middleOfBoxes - (Vector2)transform.position;
        return direction.sqrMagnitude > 0.01f;
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
