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

    // True when the player is inside the vision cone AND not hiding
    protected bool CanSeePlayer()
    {
        if (player == null || visionCone == null) return false;
        if (PlayerIsHidden) return false;
        return visionCone.Distance(playerCollider).isOverlapped;
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
            if (d <= closest)
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
            if (d <= closest)
            {
                closest = d;
                position = lightSource.transform.position;
                found = true;
            }
        }

        return found;
    }

    // ---------------------------------------------------------------- facing

    // Turns the vision cone to look left (-1) or right (1)
    protected void SetFacing(int direction)
    {
        facing = direction;
        if (visionCone != null)
        {
            Vector3 scale = visionCone.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;   // a negative scale flips the cone around
            visionCone.transform.localScale = scale;
        }
    }

    protected void FaceToward(float x)
    {
        SetFacing(x >= transform.position.x ? 1 : -1);
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
