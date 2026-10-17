using UnityEngine;

// The Wandering Shadow enemy. Follows the first FSM from Jasmine's write-up:
//   Idle    -> stands still, goes to Stalk after 5 seconds
//   Stalk   -> walks to one end of its wander zone, then back to Idle
//   Inspect -> walks to a light or noise ("??"), then goes to Idle (or Alert if the player is there)
//   Alert   -> has seen the player ("!!"), goes to Attack when the detection meter is full,
//              or back to Idle if the player is out of sight for 5 seconds
//   Attack  -> chases the player and swings at them. A hit means game over.
// Needs: Rigidbody2D, a body Collider2D, and a vision cone (set in the Inspector).
[RequireComponent(typeof(Rigidbody2D))]
public class WanderingShadow : EnemyBase
{
    public enum State { Idle, Stalk, Inspect, Alert, Attack }

    [SerializeField] private State state = State.Idle;

    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float inspectSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Wander zone (the grey box on the paper map)")]
    // The zone is centred on wherever the enemy is placed, and goes this far each way
    [SerializeField] private float wanderHalfWidth = 5f;

    [Header("Timers")]
    [SerializeField] private float idleTime = 5f;        // Idle -> Stalk
    [SerializeField] private float loseSightTime = 5f;   // Alert -> Idle
    // Extra rule (not in the doc): if the player stays hidden this long during Attack,
    // the shadow loses them and drops back to Alert. Set to 0 to turn it off.
    [SerializeField] private float giveUpTime = 4f;

    [Header("Detection")]
    [SerializeField] private float lightRadius = 6f;     // how close a light has to be to catch its attention

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackWindup = 0.5f;  // warning time before the swing lands
    [SerializeField] private float attackCooldown = 1f;

    [Header("Look")]
    [SerializeField] private SpriteRenderer bodySprite;  // just used to flash during the attack windup

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Vector2 home;
    private float zoneLeft, zoneRight;

    private float stateTimer;
    private float lostSightTimer;
    private float hiddenTimer;
    private float targetX;
    private Vector2 inspectTarget;

    private float lightCooldown;   // after checking out a light, ignore lights for a bit so it doesn't loop forever
    private bool heardNoise;
    private Vector2 noisePosition;

    private float moveInput;       // -1 left, 0 stopped, 1 right
    private float currentSpeed;
    private bool isBlocked;

    private bool winding;
    private float windupTimer;
    private float cooldownTimer;
    private Color normalColor;

    protected override void Start()
    {
        base.Start();

        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        bodyCollider = GetComponent<Collider2D>();

        // The enemy shouldn't physically push the player around
        if (playerCollider != null) Physics2D.IgnoreCollision(bodyCollider, playerCollider);

        home = transform.position;
        zoneLeft = home.x - wanderHalfWidth;
        zoneRight = home.x + wanderHalfWidth;

        if (bodySprite != null) normalColor = bodySprite.color;

        SetFacing(1);
        ChangeState(State.Idle);
    }

    protected override void HearNoise(Vector2 position)
    {
        // Only Idle and Stalk shadows get distracted by noise
        if (state == State.Idle || state == State.Stalk)
        {
            heardNoise = true;
            noisePosition = position;
        }
    }

    // ---------------------------------------------------------------- the FSM

    private void Update()
    {
        if (player == null) return;

        stateTimer += Time.deltaTime;
        moveInput = 0f;
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (lightCooldown > 0f) lightCooldown -= Time.deltaTime;

        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Stalk: UpdateStalk(); break;
            case State.Inspect: UpdateInspect(); break;
            case State.Alert: UpdateAlert(); break;
            case State.Attack: UpdateAttack(); break;
        }
    }

    private void UpdateIdle()
    {
        if (CheckForPlayerOrDistraction()) return;

        // If it got pulled out of its zone (by an Inspect or Attack), walk back in first
        if (transform.position.x < zoneLeft - 0.5f || transform.position.x > zoneRight + 0.5f)
        {
            MoveToward(Mathf.Clamp(transform.position.x, zoneLeft, zoneRight), walkSpeed);
            stateTimer = 0f;   // the 5 seconds only count once it's back home
            return;
        }

        if (stateTimer >= idleTime)
        {
            ChangeState(State.Stalk);
        }
    }

    private void UpdateStalk()
    {
        if (CheckForPlayerOrDistraction()) return;

        // Walk to the end of the zone, then go back to Idle
        bool arrived = MoveToward(targetX, walkSpeed);
        if (arrived || isBlocked)
        {
            ChangeState(State.Idle);
        }
    }

    private void UpdateInspect()
    {
        bool arrived = MoveToward(inspectTarget.x, inspectSpeed);
        if (arrived || isBlocked)
        {
            // Got to the light / noise. Is the player there?
            if (CanSeePlayer())
            {
                ChangeState(State.Alert);
            }
            else
            {
                lightCooldown = 3f;   // don't get pulled back to the same light straight away
                ChangeState(State.Idle);
            }
        }
    }

    private void UpdateAlert()
    {
        FaceToward(player.transform.position.x);

        if (CanSeePlayer())
        {
            lostSightTimer = 0f;

            // Alice's detection meter decides when it turns into an Attack
            // (if the player has no meter, just attack after a short delay)
            bool readyToAttack = DetectionMeterFull() || (playerHiding == null && stateTimer >= 1.5f);
            if (readyToAttack) ChangeState(State.Attack);
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= loseSightTime) ChangeState(State.Idle);
        }
    }

    private void UpdateAttack()
    {
        float dx = player.transform.position.x - transform.position.x;
        float dy = player.transform.position.y - transform.position.y;
        FaceToward(player.transform.position.x);

        // Lose track of the player if they stay hidden long enough
        if (PlayerIsHidden && giveUpTime > 0f)
        {
            hiddenTimer += Time.deltaTime;
            if (hiddenTimer >= giveUpTime)
            {
                ChangeState(State.Alert);
                return;
            }
        }
        else
        {
            hiddenTimer = 0f;
        }

        // In the middle of a swing: wait for the windup, then see if it hits
        if (winding)
        {
            windupTimer -= Time.deltaTime;
            if (windupTimer <= 0f)
            {
                winding = false;
                cooldownTimer = attackCooldown;
                if (bodySprite != null) bodySprite.color = normalColor;

                bool inReach = Mathf.Abs(dx) <= attackRange + 0.4f && Mathf.Abs(dy) < 2.5f;
                if (inReach && !PlayerIsHidden)
                {
                    GameFlow.PlayerDied();   // the swing connected
                }
            }
            return;
        }

        if (Mathf.Abs(dx) <= attackRange && Mathf.Abs(dy) < 2.5f)
        {
            // Close enough to swing
            if (cooldownTimer <= 0f)
            {
                winding = true;
                windupTimer = attackWindup;
                if (bodySprite != null) bodySprite.color = Color.white;   // flash as a warning
            }
        }
        else
        {
            MoveToward(player.transform.position.x, chaseSpeed);
        }
    }

    // Idle and Stalk both check for the same things
    private bool CheckForPlayerOrDistraction()
    {
        if (CanSeePlayer())
        {
            ChangeState(State.Alert);
            return true;
        }

        if (heardNoise)
        {
            heardNoise = false;
            inspectTarget = noisePosition;
            ChangeState(State.Inspect);
            return true;
        }

        Vector2 lightPosition;
        if (lightCooldown <= 0f && FindLightDistraction(lightRadius, out lightPosition))
        {
            inspectTarget = lightPosition;
            ChangeState(State.Inspect);
            return true;
        }

        return false;
    }

    private void ChangeState(State newState)
    {
        state = newState;
        stateTimer = 0f;
        lostSightTimer = 0f;
        hiddenTimer = 0f;
        winding = false;
        heardNoise = false;
        if (bodySprite != null) bodySprite.color = normalColor;

        SetStateLabel(newState.ToString());

        switch (newState)
        {
            case State.Idle:
            case State.Stalk:
                ClearSignal();
                break;
            case State.Inspect:
                ShowQuestion();
                break;
            case State.Alert:
                ShowAlert();
                break;
            case State.Attack:
                ShowAlert();
                break;
        }

        if (newState == State.Stalk)
        {
            // Pick one end of the wander zone to walk to
            targetX = (Random.value < 0.5f) ? zoneLeft : zoneRight;
        }
    }

    // Puts the enemy back to how it started (for when the player respawns without reloading the scene)
    public void ResetEnemy()
    {
        transform.position = home;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetFacing(1);
        ChangeState(State.Idle);
    }

    // ---------------------------------------------------------------- movement

    // Sets up walking toward x. Gives back true once we've arrived.
    private bool MoveToward(float x, float speed)
    {
        float dx = x - transform.position.x;
        if (Mathf.Abs(dx) < 0.3f) return true;

        moveInput = Mathf.Sign(dx);
        currentSpeed = speed;
        SetFacing((int)moveInput);
        return false;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        isBlocked = false;
        float velocityX = 0f;

        if (moveInput != 0f)
        {
            if (CrouchSpotAhead()) isBlocked = true;   // too tall to follow into a crouch spot
            else velocityX = moveInput * currentSpeed;
        }

        rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
    }

    private bool CrouchSpotAhead()
    {
        Vector2 ahead = (Vector2)transform.position + new Vector2(facing * 0.9f, 0f);
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(ahead, 0.25f))
        {
            if (hit.CompareTag("CrouchingSpot")) return true;
        }
        return false;
    }

    // Draws the wander zone and the hearing/light circles in the Scene view when selected
    private void OnDrawGizmosSelected()
    {
        float centreX = Application.isPlaying ? home.x : transform.position.x;
        Gizmos.color = Color.grey;
        Gizmos.DrawLine(new Vector3(centreX - wanderHalfWidth, transform.position.y - 1f, 0f),
                        new Vector3(centreX + wanderHalfWidth, transform.position.y - 1f, 0f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightRadius);
    }
}
