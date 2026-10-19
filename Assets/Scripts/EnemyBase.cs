using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

// Stuff that both enemy types need: finding the player, the vision cone,
// hearing noises, and the "??" / "!!" signals.
// WanderingShadow and GlaringEyes both build on this, so we only write it once.
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField] protected Collider2D visionCone;     // the cone object (child, tagged EnemyView)

    // How fast the cone turns, in degrees per second. 0 = turns instantly.
    [SerializeField] protected float turnSpeedDegrees = 0f;

    [Header("Hearing")]
    [SerializeField] protected float hearingRadius = 8f;  // how far away a normal noise can be heard

    [Header("Signals (placeholder text for now)")]
    [SerializeField] private TextMesh signalText;         // shows ?? or !! above the head
    [SerializeField] private TextMesh stateText;          // shows the state name so we can test
    // The audio person can hook the music stings up to these later
    public UnityEvent onQuestionSignal;                   // plays when an enemy gets curious (??)
    public UnityEvent onAlertSignal;                      // plays when an enemy spots the player (!!)

    protected PlayerMovement player;
    protected HidingScript playerHiding;
    protected Collider2D playerCollider;
    private Light2D playerFlashlight;

    protected int facing = 1;   // 1 = looking right, -1 = looking left

    private float targetAngle;          // the way the cone should point (degrees)
    private float currentAngle;         // the way it's pointing right now
    private bool angleStarted = false;  // false until the first time we point it somewhere

    protected virtual void Start()
    {
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

        // The flashlight counts while it's switched on
        if (player != null && playerFlashlight != null && playerFlashlight.gameObject.activeInHierarchy)
        {
            float d = Vector2.Distance(transform.position, player.transform.position);
            // Light doesn't shine through walls either
            if (d <= closest && LineOfSight.CanSeeBody(transform.position, playerCollider))
            {
                closest = d;
                position = player.transform.position;
                found = true;
            }
        }

        // Wall lights only count while they are flickering
        foreach (LightSource lightSource in LightSource.All)
        {
            if (!lightSource.IsAttracting) continue;
            float d = Vector2.Distance(transform.position, lightSource.transform.position);
            if (d <= closest && LineOfSight.IsClear(transform.position, lightSource.transform.position))
            {
                closest = d;
                position = lightSource.transform.position;
                found = true;
            }
        }

        return found;
    }

    // ---------------------------------------------------------------- facing

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

    // Smooth turning: each frame, move the cone a little closer to where it should point
    private void LateUpdate()
    {
        if (turnSpeedDegrees <= 0f || visionCone == null) return;
        if (Mathf.Approximately(currentAngle, targetAngle)) return;

        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeedDegrees * Time.deltaTime);
        ApplyConeAngle();
    }

    private void ApplyConeAngle()
    {
        if (visionCone == null) return;
        Transform cone = visionCone.transform;

        Vector3 scale = cone.localScale;
        scale.x = Mathf.Abs(scale.x);   // (older versions flipped the cone with a negative scale, so make sure it's the normal way round)
        cone.localScale = scale;
        cone.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
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

    // ---------------------------------------------------------------- signals

    protected void ShowQuestion()
    {
        SetSignal("??");
        if (onQuestionSignal != null) onQuestionSignal.Invoke();
    }

    protected void ShowAlert()
    {
        SetSignal("!!");
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
