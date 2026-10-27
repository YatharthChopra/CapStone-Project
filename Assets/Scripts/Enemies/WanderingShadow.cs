using UnityEngine;

// The Wandering Shadow enemy:
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

    // The sprite animator and the audio manager read these
    public State CurrentState { get { return state; } }
    public bool IsWindingUp { get { return isWindingUp; } }

    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float inspectSpeed = 4f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float alertApproachSpeed = 3f;   // how fast it walks toward the player while Alert

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
    // Extra rule (not in the doc): if the player stays hidden (or behind a wall) this long during Attack,
    // the shadow loses them and drops back to Alert. Set to 0 to turn it off.
    [SerializeField] private float giveUpTime = 4f;

    [Header("Detection")]
    [SerializeField] private float lightRadius = 12f;    // how close a light has to be to catch its attention

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackWindup = 0.5f;  // warning time before the swing lands
    [SerializeField] private float attackCooldown = 1f;
    // If the player lets an Alert shadow get this close and stay there, it attacks even if the meter isn't full.
    // (Otherwise you could just walk past one.) The delay gives the player a moment to back off or hide.
    [SerializeField] private float closeAttackRange = 4f;
    [SerializeField] private float closeAttackDelay = 1.5f;

    [Header("Searching")]
    [SerializeField] private float searchTime = 3f;           // how long it looks around after reaching a noise or light
    [SerializeField] private float searchTurnTime = 1f;       // it looks the other way this often while searching
    // Wandering shadows normally stop at crouch boxes. Tick this to let one walk right along a row of boxes
    // when it is wandering or investigating (it still can't chase into them).
    [SerializeField] private bool wanderThroughCrouchSpots = false;

    [Header("Look")]
    [SerializeField] private SpriteRenderer bodySprite;  // just used to flash during the attack windup

    // Small numbers we use in a few places, named so it's clear what they are
    private const float ArriveDistance = 0.3f;          // closer than this to a target counts as "arrived"
    private const float FlatZoneTolerance = 0.5f;       // flat levels: how far outside the zone before we walk back in
    private const float WindingZoneTolerance = 1.5f;    // winding levels: same thing
    private const float LightCooldownTime = 3f;         // after checking out a light, ignore lights for this long
    private const float BackOffTime = 3f;               // after backing away from a crouch spot, ignore the player this long
    private const float NoMeterAttackDelay = 1.5f;      // if the player has no detection meter, attack this long after spotting them
    private const float VerticalReach = 2.5f;           // flat levels: how far above or below we can still hit the player
    private const float ExtraReachDuringSwing = 0.4f;   // the player may move a little while the swing winds up
    private const float StuckSpeed = 0.2f;              // moving slower than this while trying to move means we're stuck
    private const float StuckTime = 0.6f;               // stuck for this long means we give up on the walk
    private const float CrouchCheckDistance = 0.9f;     // how far ahead we look for a crouch spot
    private const float CrouchCheckRadius = 0.25f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private bool freeMovement;     // true in winding levels (no gravity, can move up and down)
    private Vector2 home;
    private Vector2 zoneA, zoneB;  // the two ends of the wander zone

    // Timers
    private float stateTimer;      // how long we've been in the current state
    private float lostSightTimer;  // Alert: how long the player has been out of sight
    private float lostTrackTimer;  // Attack: how long the player has been hidden or behind a wall
    private float closeTimer;      // Alert: how long the player has been close by
    private float searchTimer;     // Inspect: how long we've been looking around at the spot
    private float searchTurnTimer;
    private bool isSearching;      // Inspect: true once we've reached the spot
    private float lightCooldown;
    private float backOffTimer;
    private float windupTimer;
    private float cooldownTimer;
    private float stuckTimer;

    // Targets
    private Vector2 stalkTarget;
    private Vector2 inspectTarget;
    private bool heardNoise;
    private Vector2 noisePosition;

    // Movement
    private Vector2 moveDirection; // which way we want to walk this frame (zero = stand still)
    private float currentSpeed;
    private bool isBlocked;        // true if we're hitting a crouch spot or a wall
    private bool crouchBlocked;    // true if specifically a crouch spot is in the way (we're too tall to go in)

    // Attacking
    private bool isWindingUp;      // true during the warning before a swing lands
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
        IgnoreCollisionsWithPlayerAndOtherShadows();

        home = transform.position;
        SetUpZone();

        if (bodySprite != null) normalColor = bodySprite.color;

        SetFacing(1);
        ChangeState(State.Idle);
    }

    // Shadows shouldn't push the player around, or block each other (the lobby has 8 in one zone)
    private void IgnoreCollisionsWithPlayerAndOtherShadows()
    {
        if (playerCollider != null) Physics2D.IgnoreCollision(bodyCollider, playerCollider);

        foreach (WanderingShadow other in FindObjectsByType<WanderingShadow>(FindObjectsInactive.Exclude))
        {
            if (other != this) Physics2D.IgnoreCollision(bodyCollider, other.GetComponent<Collider2D>());
        }
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
        // Idle and Stalk shadows get distracted by noise
        if (state == State.Idle || state == State.Stalk)
        {
            heardNoise = true;
            noisePosition = position;
        }
        // One that is already investigating goes to the new noise instead (it's trying to work out what's going on)
        else if (state == State.Inspect)
        {
            inspectTarget = position;
            isSearching = false;
            searchTimer = 0f;
        }
    }

    // ---------------------------------------------------------------- the state machine

    private void Update()
    {
        if (player == null) return;

        stateTimer += Time.deltaTime;
        moveDirection = Vector2.zero;   // each state sets this again if it wants to walk
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
        float distanceOutside = freeMovement
            ? Vector2.Distance(transform.position, nearest)
            : Mathf.Abs(transform.position.x - nearest.x);
        float tolerance = freeMovement ? WindingZoneTolerance : FlatZoneTolerance;

        if (distanceOutside > tolerance)
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
        if (!isSearching)
        {
            bool arrived = MoveToward(inspectTarget, inspectSpeed);
            if (!arrived && !isBlocked) return;
            isSearching = true;   // got to the light / noise, now look around for what caused it
        }

        // Is the player there?
        // (Not while we're backing off from a crouch spot, or we'd flip between Inspect and Attack forever)
        if (backOffTimer <= 0f && CanSeePlayer())
        {
            ChangeState(State.Alert);
            return;
        }

        // Stand still and look left and right for a few seconds before giving up
        searchTimer += Time.deltaTime;
        searchTurnTimer += Time.deltaTime;
        if (searchTurnTimer >= searchTurnTime)
        {
            searchTurnTimer = 0f;
            SetFacing(-facing);
        }

        if (searchTimer >= searchTime)
        {
            lightCooldown = LightCooldownTime;   // don't get pulled back to the same light straight away
            ChangeState(State.Idle);
        }
    }

    private void UpdateAlert()
    {
        LookAt(player.transform.position);

        if (!CanSeePlayer())
        {
            // Lost sight of the player. Go back to Idle if it stays that way.
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= loseSightTime) ChangeState(State.Idle);
            return;
        }

        lostSightTimer = 0f;

        // Alice's detection meter decides when it turns into an Attack
        // (if the player has no meter, just attack after a short delay)
        // It also attacks if the player stays close to it for a moment, whatever the meter says
        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        if (IsWithinReach(toPlayer, closeAttackRange - attackRange)) closeTimer += Time.deltaTime;
        else closeTimer = 0f;

        bool readyToAttack = DetectionMeterFull() || closeTimer >= closeAttackDelay
            || (playerHiding == null && stateTimer >= NoMeterAttackDelay);
        if (readyToAttack)
        {
            ChangeState(State.Attack);
            return;
        }

        // Not just standing and staring: it closes in on the player while the meter fills
        MoveToward(player.transform.position, alertApproachSpeed);
    }

    private void UpdateAttack()
    {
        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;

        // The player went into a crouch spot, and we're too tall to follow. Back off to our zone.
        if (crouchBlocked && !isWindingUp)
        {
            backOffTimer = BackOffTime;   // for a few seconds, don't go straight back to chasing
            lightCooldown = BackOffTime;  // ...or get pulled toward a light, which sent it straight back here
            ChangeState(State.Idle);
            return;
        }

        LookAt(player.transform.position);

        // Can the shadow actually see the player? (walls block sight)
        bool wallInTheWay = !LineOfSight.CanSeeBody(transform.position, playerCollider);

        if (HasLostTrackOfPlayer(wallInTheWay))
        {
            ChangeState(State.Alert);
            return;
        }

        // In the middle of a swing: wait for the windup, then see if it hits
        if (isWindingUp)
        {
            UpdateSwing(toPlayer, wallInTheWay);
            return;
        }

        // Close enough to swing (and no wall in the way)? Otherwise keep chasing.
        if (IsWithinReach(toPlayer, 0f) && !wallInTheWay)
        {
            if (cooldownTimer <= 0f) StartSwing();
        }
        else
        {
            MoveToward(player.transform.position, chaseSpeed);
        }
    }

    // The shadow gives up if the player stays hidden, or out of sight behind a wall, long enough
    private bool HasLostTrackOfPlayer(bool wallInTheWay)
    {
        bool outOfSight = PlayerIsHidden || wallInTheWay;
        if (outOfSight && giveUpTime > 0f)
        {
            lostTrackTimer += Time.deltaTime;
            return lostTrackTimer >= giveUpTime;
        }

        lostTrackTimer = 0f;
        return false;
    }

    private void StartSwing()
    {
        isWindingUp = true;
        windupTimer = attackWindup;
        AudioManager.Play(SoundId.EnemySwing, transform.position);
        if (bodySprite != null) bodySprite.color = Color.white;   // flash as a warning
    }

    // Counts down the windup, then checks whether the swing hits
    private void UpdateSwing(Vector2 toPlayer, bool wallInTheWay)
    {
        windupTimer -= Time.deltaTime;
        if (windupTimer > 0f) return;

        isWindingUp = false;
        cooldownTimer = attackCooldown;
        if (bodySprite != null) bodySprite.color = normalColor;

        // The player gets a little extra distance, since they may have moved during the windup
        bool stillInReach = IsWithinReach(toPlayer, ExtraReachDuringSwing);
        if (stillInReach && !PlayerIsHidden && !wallInTheWay)
        {
            GameFlow.PlayerDied();   // the swing connected
        }
    }

    // Is the player close enough to hit? (extraReach makes the reach a bit bigger)
    private bool IsWithinReach(Vector2 toPlayer, float extraReach)
    {
        float reach = attackRange + extraReach;

        if (freeMovement) return toPlayer.magnitude <= reach;
        return Mathf.Abs(toPlayer.x) <= reach && Mathf.Abs(toPlayer.y) < VerticalReach;
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
        State oldState = state;
        state = newState;
        stateTimer = 0f;
        lostSightTimer = 0f;
        lostTrackTimer = 0f;
        stuckTimer = 0f;
        closeTimer = 0f;
        searchTimer = 0f;
        searchTurnTimer = 0f;
        isSearching = false;
        isWindingUp = false;
        heardNoise = false;
        if (bodySprite != null) bodySprite.color = normalColor;

        SetStateLabel(newState.ToString());

        // Show the right signal above the head
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
                // Going from Alert to Attack keeps the same "!!", so the alert sound doesn't play twice
                if (oldState != State.Alert) ShowAlert();
                break;
        }

        if (newState == State.Stalk)
        {
            // Pick one end of the wander zone to walk to
            stalkTarget = (Random.value < 0.5f) ? zoneA : zoneB;
        }
    }

    // Puts the enemy back to how it started. Nothing calls this yet, it's here for if we ever
    // respawn the player without reloading the scene.
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

        if (difference.magnitude < ArriveDistance) return true;

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

    // Movement is done here (not in Update) because it uses physics
    private void FixedUpdate()
    {
        if (rb == null) return;

        isBlocked = false;
        crouchBlocked = false;
        Vector2 wanted = Vector2.zero;

        if (moveDirection != Vector2.zero)
        {
            bool canWalkThroughSpots = wanderThroughCrouchSpots
                && (state == State.Idle || state == State.Stalk || state == State.Inspect);

            if (!canWalkThroughSpots && CrouchSpotAhead())
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
        if (wanted != Vector2.zero && actualSpeed < StuckSpeed) stuckTimer += Time.fixedDeltaTime;
        else stuckTimer = 0f;
        if (stuckTimer > StuckTime) isBlocked = true;

        // On flat levels gravity handles up/down. In winding levels we set both ourselves.
        if (freeMovement) rb.linearVelocity = wanted;
        else rb.linearVelocity = new Vector2(wanted.x, rb.linearVelocity.y);
    }

    // Is there a crouch spot just ahead? Shadows are too tall to go into those.
    private bool CrouchSpotAhead()
    {
        Vector2 direction = freeMovement ? moveDirection : new Vector2(Mathf.Sign(moveDirection.x), 0f);
        Vector2 ahead = (Vector2)transform.position + direction * CrouchCheckDistance;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(ahead, CrouchCheckRadius))
        {
            if (hit.CompareTag("CrouchingSpot")) return true;
        }
        return false;
    }

    // Draws the wander zone and the hearing/light circles in the Scene view when the enemy is selected
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
