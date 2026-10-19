using UnityEngine;

// The Wandering Shadow enemy. Follows the first FSM from Jasmine's write-up:
//   Idle    -> stands still, goes to Stalk after 5 seconds
//   Stalk   -> walks to one end of its wander zone, then back to Idle
//   Inspect -> walks to a light or noise ("??"), then goes to Idle (or Alert if the player is there)
//   Alert   -> has seen the player ("!!"), goes to Attack when the detection meter is full,
//              or back to Idle if the player is out of sight for 5 seconds
//   Attack  -> chases the player and swings at them. A hit means game over.
// Needs: Rigidbody2D, a body Collider2D, and a vision cone (set in the Inspector).
//
// On flat levels it only walks left and right. On winding levels (see GameFlow.FreeMovementScenes)
// there is no gravity and it moves in any direction, so it can follow diagonal corridors.
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
    // Flat levels: use a grey zone box's exact left and right edges instead
    [SerializeField] private bool useZoneEdges = false;
    [SerializeField] private float zoneLeftEdge;
    [SerializeField] private float zoneRightEdge;
    // Winding levels: the zone is a slanted box, so we store the two ends of its long side
    [SerializeField] private bool useZoneEnds = false;
    [SerializeField] private Vector2 zoneEndA;
    [SerializeField] private Vector2 zoneEndB;

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
    private bool freeMovement;     // true in winding levels (no gravity, can move up and down)
    private Vector2 home;
    private Vector2 zoneA, zoneB;  // the two ends of the wander zone

    private float stateTimer;
    private float lostSightTimer;
    private float hiddenTimer;
    private Vector2 stalkTarget;
    private Vector2 inspectTarget;

    private float lightCooldown;   // after checking out a light, ignore lights for a bit so it doesn't loop forever
    private bool heardNoise;
    private Vector2 noisePosition;

    private Vector2 moveDirection; // which way we want to walk this frame (zero = stand still)
    private float currentSpeed;
    private bool isBlocked;        // true if we're hitting a crouch spot or a wall
    private bool crouchBlocked;    // true if specifically a crouch spot is in the way (we're too tall to go in)
    private float backOffTimer;    // after backing away from a crouch spot, ignore the player for a moment
    private float stuckTimer;

    private bool winding;
    private float windupTimer;
    private float cooldownTimer;
    private Color normalColor;

    protected override void Start()
    {
        base.Start();

        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        // In winding levels the player has no gravity, so the shadows don't either
        freeMovement = GameFlow.IsFreeMovementScene;
        if (freeMovement) rb.gravityScale = 0f;

        bodyCollider = GetComponent<Collider2D>();

        // The enemy shouldn't physically push the player around
        if (playerCollider != null) Physics2D.IgnoreCollision(bodyCollider, playerCollider);

        // Shadows shouldn't block each other either (the lobby has 8 in one zone)
        foreach (WanderingShadow other in FindObjectsByType<WanderingShadow>(FindObjectsInactive.Exclude))
        {
            if (other != this) Physics2D.IgnoreCollision(bodyCollider, other.GetComponent<Collider2D>());
        }

        home = transform.position;
        SetUpZone();

        if (bodySprite != null) normalColor = bodySprite.color;

        SetFacing(1);
        ChangeState(State.Idle);
    }

    // Works out the two ends of the wander zone
    private void SetUpZone()
    {
        if (freeMovement)
        {
            if (useZoneEnds)
            {
                zoneA = zoneEndA;
                zoneB = zoneEndB;
            }
            else
            {
                zoneA = home + Vector2.left * wanderHalfWidth;
                zoneB = home + Vector2.right * wanderHalfWidth;
            }
        }
        else
        {
            float left = useZoneEdges ? zoneLeftEdge : home.x - wanderHalfWidth;
            float right = useZoneEdges ? zoneRightEdge : home.x + wanderHalfWidth;
            zoneA = new Vector2(left, home.y);
            zoneB = new Vector2(right, home.y);
        }
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
        moveDirection = Vector2.zero;
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (lightCooldown > 0f) lightCooldown -= Time.deltaTime;
        if (backOffTimer > 0f) backOffTimer -= Time.deltaTime;

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
        Vector2 nearest = NearestPointOnZone(transform.position);
        float away = freeMovement
            ? Vector2.Distance(transform.position, nearest)
            : Mathf.Abs(transform.position.x - nearest.x);
        float allowed = freeMovement ? 1.5f : 0.5f;

        if (away > allowed)
        {
            MoveToward(nearest, walkSpeed);
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
        bool arrived = MoveToward(stalkTarget, walkSpeed);
        if (arrived || isBlocked)
        {
            ChangeState(State.Idle);
        }
    }

    private void UpdateInspect()
    {
        bool arrived = MoveToward(inspectTarget, inspectSpeed);
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
        LookAt(player.transform.position);

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
        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;

        // The player went into a crouch spot, and we're too tall to follow. Back off to our zone.
        if (crouchBlocked && !winding)
        {
            backOffTimer = 3f;   // for a few seconds, don't go straight back to chasing
            ChangeState(State.Idle);
            return;
        }

        LookAt(player.transform.position);

        // Can the shadow actually see the player? (walls block sight)
        bool wallInTheWay = !LineOfSight.CanSeeBody(transform.position, playerCollider);

        // Lose track of the player if they stay hidden, or out of sight behind a wall, long enough
        if ((PlayerIsHidden || wallInTheWay) && giveUpTime > 0f)
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

        // Is the player close enough to hit?
        bool inReach = freeMovement
            ? toPlayer.magnitude <= attackRange
            : Mathf.Abs(toPlayer.x) <= attackRange && Mathf.Abs(toPlayer.y) < 2.5f;
        inReach = inReach && !wallInTheWay;   // no swinging through walls

        // In the middle of a swing: wait for the windup, then see if it hits
        if (winding)
        {
            windupTimer -= Time.deltaTime;
            if (windupTimer <= 0f)
            {
                winding = false;
                cooldownTimer = attackCooldown;
                if (bodySprite != null) bodySprite.color = normalColor;

                // Give the player a little extra distance, since they may have moved during the windup
                bool stillInReach = freeMovement
                    ? toPlayer.magnitude <= attackRange + 0.4f
                    : Mathf.Abs(toPlayer.x) <= attackRange + 0.4f && Mathf.Abs(toPlayer.y) < 2.5f;
                if (stillInReach && !PlayerIsHidden && !wallInTheWay)
                {
                    GameFlow.PlayerDied();   // the swing connected
                }
            }
            return;
        }

        if (inReach)
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
            MoveToward(player.transform.position, chaseSpeed);
        }
    }

    // Idle and Stalk both check for the same things
    private bool CheckForPlayerOrDistraction()
    {
        if (backOffTimer <= 0f && CanSeePlayer())
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
        stuckTimer = 0f;
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
            stalkTarget = (Random.value < 0.5f) ? zoneA : zoneB;
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

    // Sets up walking toward a spot. Gives back true once we've arrived.
    // On flat levels only the left/right part counts (gravity looks after up/down).
    private bool MoveToward(Vector2 target, float speed)
    {
        Vector2 difference = target - (Vector2)transform.position;
        if (!freeMovement) difference.y = 0f;

        if (difference.magnitude < 0.3f) return true;

        moveDirection = difference.normalized;
        currentSpeed = speed;
        LookInDirection(difference);
        return false;
    }

    // The closest point on the line between the two ends of the zone
    private Vector2 NearestPointOnZone(Vector2 point)
    {
        Vector2 line = zoneB - zoneA;
        if (line.sqrMagnitude < 0.0001f) return zoneA;
        float t = Mathf.Clamp01(Vector2.Dot(point - zoneA, line) / line.sqrMagnitude);
        return zoneA + line * t;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        isBlocked = false;
        crouchBlocked = false;
        Vector2 wanted = Vector2.zero;

        if (moveDirection != Vector2.zero)
        {
            if (CrouchSpotAhead())
            {
                // too tall to follow into a crouch spot
                isBlocked = true;
                crouchBlocked = true;
            }
            else
            {
                wanted = moveDirection * currentSpeed;
            }
        }

        // If we want to move but aren't getting anywhere, we've hit a wall. Give up so we don't freeze there.
        float actualSpeed = freeMovement ? rb.linearVelocity.magnitude : Mathf.Abs(rb.linearVelocity.x);
        if (wanted != Vector2.zero && actualSpeed < 0.2f) stuckTimer += Time.fixedDeltaTime;
        else stuckTimer = 0f;
        if (stuckTimer > 0.6f) isBlocked = true;

        // On flat levels gravity handles up/down. In winding levels we set both ourselves.
        if (freeMovement) rb.linearVelocity = wanted;
        else rb.linearVelocity = new Vector2(wanted.x, rb.linearVelocity.y);
    }

    private bool CrouchSpotAhead()
    {
        Vector2 direction = freeMovement ? moveDirection : new Vector2(Mathf.Sign(moveDirection.x), 0f);
        Vector2 ahead = (Vector2)transform.position + direction * 0.9f;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(ahead, 0.25f))
        {
            if (hit.CompareTag("CrouchingSpot")) return true;
        }
        return false;
    }

    // Draws the wander zone and the hearing/light circles in the Scene view when selected
    private void OnDrawGizmosSelected()
    {
        Vector2 a = zoneA, b = zoneB;
        if (!Application.isPlaying)
        {
            a = (Vector2)transform.position + Vector2.left * wanderHalfWidth;
            b = (Vector2)transform.position + Vector2.right * wanderHalfWidth;
            if (useZoneEnds) { a = zoneEndA; b = zoneEndB; }
        }
        Gizmos.color = Color.grey;
        Gizmos.DrawLine(a, b);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightRadius);
    }
}
