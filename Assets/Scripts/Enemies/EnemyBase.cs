using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

// The stuff that both enemy types need, so we only write it once:
// finding the player, the vision cone, hearing noises, noticing lights, and the "??" / "!!" signals.
// WanderingShadow and GlaringEyes both build on this.
public abstract class EnemyBase : MonoBehaviour
{
    // ---------------------------------------------------------------- settings you can change in the Inspector

    [Header("Vision cone")]
    [SerializeField] protected Collider2D visionCone;     // the cone object (a child of the enemy, tagged EnemyView)
    // How fast the cone turns, in degrees per second. 0 = turns instantly.
    [SerializeField] protected float turnSpeedDegrees = 0f;
    // How far in front of the enemy's middle the cone starts. 0.5 puts the point of the cone
    // at the front edge of the body, so it looks like it comes out of the enemy.
    [SerializeField] private float coneStartOffset = 0.5f;
    // How visible the cone picture is (0 = invisible, 1 = solid). The picture fades out toward the far end,
    // so it shows roughly which way the enemy is looking without making a hard edge.
    [SerializeField, Range(0f, 1f)] private float coneVisibility = 0.5f;

    [Header("Sight line")]
    // A thin line from the enemy to the player, shown only while the enemy can actually see the player
    [SerializeField] private bool showSightLine = true;
    [SerializeField] private float sightLineThickness = 0.08f;
    [SerializeField] private Color sightLineColor = new Color(1f, 0.25f, 0.25f, 0.75f);

    [Header("Hearing")]
    [SerializeField] protected float hearingRadius = 8f;  // how far away a normal noise can be heard

    [Header("Signals (placeholder text for now)")]
    [SerializeField] private TextMesh signalText;         // shows ?? or !! above the head
    [SerializeField] private TextMesh stateText;          // shows the state name so we can test
    // The audio person can hook the music stings up to these later
    public UnityEvent onQuestionSignal;                   // plays when an enemy gets curious (??)
    public UnityEvent onAlertSignal;                      // plays when an enemy spots the player (!!)

    // ---------------------------------------------------------------- things the enemies share

    protected PlayerMovement player;
    protected HidingScript playerHiding;
    protected Collider2D playerCollider;
    private Light2D playerFlashlight;

    protected int facing = 1;   // 1 = looking right, -1 = looking left

    // The sprite animator reads this to know which way to face the picture
    public int Facing { get { return facing; } }

    private float targetAngle;          // the way the cone should point (degrees)
    private float currentAngle;         // the way it's pointing right now
    private bool angleStarted = false;  // false until the first time we point it somewhere

    private SpriteRenderer sightLine;   // the line to the player (made in code, so no prefab has to change)
    private const int SightLineSortingOrder = 4;   // draws on top of the cone and the enemy's body

    // ---------------------------------------------------------------- setting up and tidying up

    protected virtual void Start()
    {
        SetConeVisibility();
        CreateSightLine();

        player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            Debug.LogWarning(name + " can't find the player, so it won't do anything.");
            return;
        }

        playerHiding = player.GetComponent<HidingScript>();
        playerCollider = player.GetComponent<Collider2D>();
        playerFlashlight = player.GetComponentInChildren<Light2D>(true);
    }

    private void OnEnable()
    {
        NoiseSystem.NoiseMade += OnNoiseMade;
    }

    private void OnDisable()
    {
        NoiseSystem.NoiseMade -= OnNoiseMade;
    }

    private void OnDestroy()
    {
        if (sightLine != null) Destroy(sightLine.gameObject);
    }

    // Runs every frame after everything else has moved
    private void LateUpdate()
    {
        UpdateSightLine();
        TurnConeTowardTarget();
    }

    // ---------------------------------------------------------------- hearing

    private void OnNoiseMade(Vector2 position, float loudness)
    {
        // Only react if the noise is close enough
        float distance = Vector2.Distance(transform.position, position);
        if (distance <= hearingRadius * loudness)
        {
            HearNoise(position);
        }
    }

    // Each enemy decides what to do when it hears something
    protected abstract void HearNoise(Vector2 position);

    // ---------------------------------------------------------------- seeing

    protected bool PlayerIsHidden
    {
        get { return playerHiding != null && playerHiding.hiding; }
    }

    // True when the player is inside the vision cone, not hiding, and no wall is in the way
    protected bool CanSeePlayer()
    {
        if (player == null || visionCone == null) return false;
        if (PlayerIsHidden) return false;
        if (!visionCone.Distance(playerCollider).isOverlapped) return false;
        return LineOfSight.CanSeeBody(transform.position, playerCollider);
    }

    // Has the player's meter (Alice's detection system) filled all the way?
    protected bool DetectionMeterFull()
    {
        return playerHiding != null && playerHiding.Detection >= playerHiding.MaxDetection;
    }

    // Looks for a light to go and check out: the player's flashlight, or a flickering wall light.
    // Gives back the closest one inside the radius.
    protected bool FindLightDistraction(float radius, out Vector2 position)
    {
        position = Vector2.zero;
        float closest = radius;
        bool found = false;

        // The flashlight counts while it's switched on (and light doesn't shine through walls)
        if (player != null && playerFlashlight != null && playerFlashlight.gameObject.activeInHierarchy)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance <= closest && LineOfSight.CanSeeBody(transform.position, playerCollider))
            {
                closest = distance;
                position = player.transform.position;
                found = true;
            }
        }

        // Wall lights only count while they are flickering
        foreach (LightSource lightSource in LightSource.All)
        {
            if (!lightSource.IsAttracting) continue;

            float distance = Vector2.Distance(transform.position, lightSource.transform.position);
            if (distance <= closest && LineOfSight.IsClear(transform.position, lightSource.transform.position))
            {
                closest = distance;
                position = lightSource.transform.position;
                found = true;
            }
        }

        return found;
    }

    // ---------------------------------------------------------------- looking (the vision cone)

    // Points the vision cone in a direction (it can point any way, so it follows slanted corridors).
    // If "Turn Speed Degrees" is above 0 the cone turns smoothly, otherwise it snaps round.
    protected void LookInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        if (Mathf.Abs(direction.x) > 0.01f) facing = direction.x > 0f ? 1 : -1;

        targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // The very first time, or if there's no turn speed, point straight at it
        if (!angleStarted || turnSpeedDegrees <= 0f)
        {
            currentAngle = targetAngle;
            angleStarted = true;
            ApplyConeAngle();
        }
    }

    // Looks at a spot in the world (the player, a noise, a light...)
    protected void LookAt(Vector2 worldPosition)
    {
        LookInDirection(worldPosition - (Vector2)transform.position);
    }

    // Looks left (-1) or right (1)
    protected void SetFacing(int direction)
    {
        LookInDirection(new Vector2(direction, 0f));
    }

    // Smooth turning: each frame, move the cone a little closer to where it should point
    private void TurnConeTowardTarget()
    {
        if (turnSpeedDegrees <= 0f || visionCone == null) return;
        if (Mathf.Approximately(currentAngle, targetAngle)) return;

        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeedDegrees * Time.deltaTime);
        ApplyConeAngle();
    }

    // Turns the cone object to the current angle and puts its point at the front of the body
    private void ApplyConeAngle()
    {
        if (visionCone == null) return;
        Transform cone = visionCone.transform;

        cone.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

        float radians = currentAngle * Mathf.Deg2Rad;
        cone.localPosition = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * coneStartOffset;
    }

    // Applies the "Cone Visibility" setting to the cone's picture
    private void SetConeVisibility()
    {
        if (visionCone == null) return;

        SpriteRenderer picture = visionCone.GetComponent<SpriteRenderer>();
        if (picture == null) return;

        Color colour = picture.color;
        colour.a = coneVisibility;
        picture.color = colour;
    }

    // ---------------------------------------------------------------- the sight line

    // Makes the sight line: a thin stretched square that we move between the enemy and the player
    private void CreateSightLine()
    {
        if (!showSightLine) return;

        GameObject lineObject = new GameObject(name + " Sight Line");
        sightLine = lineObject.AddComponent<SpriteRenderer>();
        sightLine.sprite = GetWhiteSprite();
        sightLine.color = sightLineColor;
        sightLine.sortingOrder = SightLineSortingOrder;
        sightLine.enabled = false;
    }

    // Shows the line from the enemy to the player while the enemy can see them, and hides it otherwise
    private void UpdateSightLine()
    {
        if (sightLine == null) return;

        bool seen = player != null && CanSeePlayer();
        sightLine.enabled = seen;
        if (!seen) return;

        Vector2 from = transform.position;
        Vector2 to = playerCollider.bounds.center;
        Vector2 between = to - from;

        // Stretch the square so it reaches from the enemy to the player
        sightLine.transform.position = (from + to) / 2f;
        sightLine.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(between.y, between.x) * Mathf.Rad2Deg);
        sightLine.transform.localScale = new Vector3(between.magnitude, sightLineThickness, 1f);
    }

    // A plain white 1x1 unit picture, made from Unity's built-in white texture
    private static Sprite whiteSprite;
    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D white = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0.5f), white.width);
        }
        return whiteSprite;
    }

    // ---------------------------------------------------------------- signals ("??" and "!!")

    protected void ShowQuestion()
    {
        SetSignal("??");
        AudioManager.Play(SoundId.EnemyQuestion, transform.position);
        if (onQuestionSignal != null) onQuestionSignal.Invoke();
    }

    protected void ShowAlert()
    {
        SetSignal("!!");
        AudioManager.Play(SoundId.EnemyAlert, transform.position);
        if (onAlertSignal != null) onAlertSignal.Invoke();
    }

    protected void ClearSignal()
    {
        SetSignal("");
    }

    protected void SetStateLabel(string stateName)
    {
        if (stateText != null) stateText.text = stateName;
    }

    private void SetSignal(string text)
    {
        if (signalText != null) signalText.text = text;
    }
}
