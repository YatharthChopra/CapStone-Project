using UnityEngine;

// The Glaring Eyes enemy. Follows the second FSM from Jasmine's write-up:
//   Idle       -> stays still, watching one spot
//   Distracted -> turned toward a noise ("??"), goes back to Idle after 5 seconds
//   Staring    -> the player is in its vision ("!!"). It can't attack, but the longer the player
//                 stays in view the more the detection meter fills. A full meter means game over.
//                 Goes back to Idle when the player leaves its view.
// The eyes only react to sound (not light) and never move.
public class GlaringEyes : EnemyBase
{
    public enum State { Idle, Distracted, Staring }

    [SerializeField] private State state = State.Idle;

    [Header("Looking")]
    [SerializeField] private bool restFacesRight = false;   // which way it looks when nothing is happening

    [Header("Timers")]
    [SerializeField] private float distractedTime = 5f;     // how long it looks toward a noise

    [Header("Staring")]
    // How much the meter goes up each second on top of Alice's normal detection.
    // The meter is 100, so 15 means about 7 seconds by itself.
    [SerializeField] private float stareDetectionPerSecond = 15f;

    private float stateTimer;

    protected override void Start()
    {
        base.Start();
        SetFacing(restFacesRight ? 1 : -1);
        ChangeState(State.Idle);
    }

    protected override void HearNoise(Vector2 position)
    {
        // Already staring at the player, so it ignores noises
        if (state == State.Staring) return;

        FaceToward(position.x);
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

    private void UpdateStaring()
    {
        FaceToward(player.transform.position.x);

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
                SetFacing(restFacesRight ? 1 : -1);   // go back to looking at its spot
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
        ChangeState(State.Idle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}
