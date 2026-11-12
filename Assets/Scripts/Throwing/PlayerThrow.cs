using UnityEngine;
using UnityEngine.InputSystem;

// Lets Nin throw things to distract enemies.
//   - Hold Z (X / Square on a controller) to charge a throw, or hold the left mouse button. A line of dots shows where it will land.
//   - Let go to throw. A quick tap is a short, quiet throw. Holding longer throws further, and louder.
//   - With Z she throws the way she faces (R1 / L1 on a controller change the arc). With the mouse she throws toward it.
// There is no ammo. After a throw she has to wait for a short cooldown before she can throw again.
public class PlayerThrow : MonoBehaviour
{
    [Header("What gets thrown")]
    [SerializeField] private ThrownObject throwablePrefab;
    [SerializeField] private float throwCooldown = 3f;   // seconds to wait after a throw before the next one

    [Header("Throw strength")]
    [SerializeField] private float chargeTime = 2f;      // seconds of holding to reach a full-power throw
    [SerializeField] private float minSpeed = 10f;       // speed of the shortest throw
    [SerializeField] private float maxSpeed = 22f;       // speed of a full-power throw
    [SerializeField] private float minAngle = 30f;       // the lowest the arc can go (degrees above flat)
    [SerializeField] private float maxAngle = 65f;       // the highest the arc can go

    [Header("Quick tap (a short lob right in front of Nin)")]
    [SerializeField] private float tapTime = 0.2f;       // let go before this long and it counts as a tap
    [SerializeField] private float tapSpeed = 7f;        // a gentle throw, lands about 2 units away
    [SerializeField] private float tapAngle = 45f;       // arc of the lob (degrees above flat)
    [SerializeField] private float tapNoise = 0.4f;      // quiet

    [Header("Noise when it lands")]
    // 1 = a normal noise. Enemies hear a noise from (their hearing radius x loudness) away.
    [SerializeField] private float quietNoise = 0.5f;    // a short throw: less force, less noise
    [SerializeField] private float loudNoise = 1.5f;     // a full-power throw: more force, more noise

    [Header("Aiming dots")]
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private int dotCount = 16;

    // Small numbers used below, named so it's clear what they are
    private const float ThrowHeight = 0.6f;          // how far above her middle the throw starts (around hand height)
    private const float DotSize = 0.18f;             // how big each aiming dot is
    private const float DotTimeGap = 0.08f;          // seconds of flight between one dot and the next
    private const int DotSortingOrder = 20;          // draws on top of everything else
    private const float AimAdjustSpeed = 50f;        // controller: how fast R1 / L1 change the arc (degrees per second)

    private float cooldownLeft;   // seconds until Nin can throw again (0 = ready)
    private bool charging;
    private bool aimingWithMouse;   // true if this throw was started with the mouse (otherwise the keys / controller)
    private float keyAimAngle = 45f;   // the arc when throwing with the keys / controller
    private float chargeTimer;
    private float throwableGravityScale = 3f;

    private Collider2D myCollider;
    private SpriteRenderer[] dots;

    private void Awake()
    {
        // The Player prefab now carries this script. The playtest scene also has its own copy added on top,
        // so if there are two, the later one switches itself off (otherwise every throw would happen twice).
        PlayerThrow[] all = GetComponents<PlayerThrow>();
        if (all[0] != this)
        {
            enabled = false;
            Destroy(this);
        }
    }

    private void Start()
    {
        myCollider = GetComponent<Collider2D>();

        // Shout in the Console if something is missing, so throwing never fails quietly
        if (throwablePrefab == null)
        {
            Debug.LogError("PlayerThrow: no Throwable Prefab is set, so throwing is turned off. Drag Assets/Prefabs/Throwable into that slot.");
        }
        if (dotSprite == null)
        {
            Debug.LogWarning("PlayerThrow: no Dot Sprite is set, so the aiming dots will be invisible.");
        }

        // Use the same gravity as the thrown object, so the dots match where it really goes
        if (throwablePrefab != null)
        {
            Rigidbody2D thrownBody = throwablePrefab.GetComponent<Rigidbody2D>();
            if (thrownBody != null) throwableGravityScale = thrownBody.gravityScale;
        }

        // The aiming dots live in the world, not on the player, so they don't flip or move with her
        GameObject dotParent = new GameObject("Aim Dots");
        dots = new SpriteRenderer[dotCount];
        for (int i = 0; i < dotCount; i++)
        {
            GameObject dot = new GameObject("Dot " + i);
            dot.transform.SetParent(dotParent.transform, false);
            dot.transform.localScale = Vector3.one * DotSize;

            SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.color = new Color(1f, 1f, 1f, 0.7f);
            sr.sortingOrder = DotSortingOrder;
            sr.enabled = false;
            dots[i] = sr;
        }
    }

    private void Update()
    {
        if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;

        if (Time.timeScale == 0f || throwablePrefab == null) return;

        Mouse mouse = Mouse.current;

        // Start charging: the throw key / button (Z, or X / Square), or the left mouse button
        if (!charging && cooldownLeft <= 0f)
        {
            if (GameControls.ThrowPressed)
            {
                charging = true;
                chargeTimer = 0f;
                aimingWithMouse = false;
                keyAimAngle = tapAngle;
            }
            else if (mouse != null && Camera.main != null && mouse.leftButton.wasPressedThisFrame)
            {
                charging = true;
                chargeTimer = 0f;
                aimingWithMouse = true;
            }
        }

        if (!charging) return;

        // Esc, Select or right-click cancels the throw and you keep the throwable
        bool cancelPressed = GameControls.ThrowCancelPressed || (mouse != null && mouse.rightButton.wasPressedThisFrame);
        if (cancelPressed)
        {
            charging = false;
            HideDots();
            return;
        }

        chargeTimer += Time.deltaTime;
        float power = Mathf.Clamp01(chargeTimer / chargeTime);

        // On a controller R1 / L1 raise and lower the aim while charging
        keyAimAngle = Mathf.Clamp(keyAimAngle + GameControls.ThrowAimAdjust * AimAdjustSpeed * Time.deltaTime, minAngle, maxAngle);

        Vector2 origin;
        Vector2 velocity = GetLaunchVelocity(power, out origin);

        // A quick tap is a short lob just in front of Nin, so we only show the aiming dots once she's held it a bit
        bool isTap = chargeTimer < tapTime;
        if (isTap) HideDots();
        else ShowDots(origin, velocity);

        // Let go to throw
        bool released = aimingWithMouse ? (mouse != null && mouse.leftButton.wasReleasedThisFrame) : GameControls.ThrowReleased;
        if (released)
        {
            if (isTap) Throw(origin, MakeTapVelocity(velocity.x), tapNoise);
            else Throw(origin, velocity, Mathf.Lerp(quietNoise, loudNoise, power));
            charging = false;
            HideDots();
        }
    }

    // A tap always makes the same small, high lob toward whichever side Nin is aiming
    private Vector2 MakeTapVelocity(float sideways)
    {
        float directionX = sideways >= 0f ? 1f : -1f;
        float radians = tapAngle * Mathf.Deg2Rad;
        return new Vector2(directionX * Mathf.Cos(radians), Mathf.Sin(radians)) * tapSpeed;
    }

    // Works out where the throw starts and how fast it leaves, from how long we've charged and where we're aiming.
    // With the keys / controller she throws the way she is facing (the angle comes from R1 / L1, or 45 degrees).
    // With the mouse she throws toward the mouse, as before.
    private Vector2 GetLaunchVelocity(float power, out Vector2 origin)
    {
        origin = (Vector2)transform.position + new Vector2(0f, ThrowHeight);

        float directionX;
        float angle;

        if (aimingWithMouse)
        {
            Camera cam = Camera.main;
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(cam.transform.position.z)));

            float sideways = mouseWorld.x - origin.x;
            float up = mouseWorld.y - origin.y;

            directionX = sideways >= 0f ? 1f : -1f;                                      // throw left or right
            angle = Mathf.Atan2(up, Mathf.Abs(sideways)) * Mathf.Rad2Deg;                // how steep the mouse is
            angle = Mathf.Clamp(angle, minAngle, maxAngle);
        }
        else
        {
            PlayerAnimator animator = GetComponent<PlayerAnimator>();
            directionX = (animator == null || animator.FacingRight) ? 1f : -1f;
            angle = keyAimAngle;
        }

        float speed = Mathf.Lerp(minSpeed, maxSpeed, power);
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(directionX * Mathf.Cos(radians), Mathf.Sin(radians)) * speed;
    }

    private void Throw(Vector2 origin, Vector2 velocity, float loudness)
    {
        ThrownObject thrown = Instantiate(throwablePrefab, origin, Quaternion.identity);
        thrown.Launch(velocity, loudness, myCollider);
        AudioManager.Play(SoundId.Throw);

        cooldownLeft = throwCooldown;
    }

    // Draws the dotted line showing the path of the throw
    private void ShowDots(Vector2 origin, Vector2 velocity)
    {
        Vector2 gravity = Physics2D.gravity * throwableGravityScale;
        Vector2 previous = origin;
        bool hitSomething = false;

        for (int i = 0; i < dots.Length; i++)
        {
            float t = (i + 1) * DotTimeGap;
            Vector2 point = origin + velocity * t + 0.5f * gravity * t * t;

            // Stop the line where it would hit a wall or the floor
            if (!hitSomething && HitsSolid(previous, point))
            {
                hitSomething = true;
            }

            dots[i].enabled = !hitSomething;
            dots[i].transform.position = new Vector3(point.x, point.y, 0f);
            previous = point;
        }
    }

    private void HideDots()
    {
        foreach (SpriteRenderer dot in dots) dot.enabled = false;
    }

    private bool HitsSolid(Vector2 from, Vector2 to)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(from, to))
        {
            Collider2D thing = hit.collider;
            if (thing == null || thing.isTrigger) continue;
            if (thing.GetComponentInParent<PlayerMovement>() != null) continue;
            return true;
        }
        return false;
    }

    // Used by the old pickups. With no ammo there is nothing to refill, so a pickup just skips the
    // cooldown. Gives back false (and stays put) if Nin can already throw.
    public bool TryAddThrowable()
    {
        if (cooldownLeft <= 0f) return false;
        cooldownLeft = 0f;
        return true;
    }

    // A simple on-screen counter for testing (bottom-left)
    private void OnGUI()
    {
        string status = cooldownLeft > 0f ? "Throw: recharging " + cooldownLeft.ToString("0.0") + "s" : "Throw: ready";
        GUI.Label(new Rect(10, Screen.height - 32, 450, 25), status + "     (hold left mouse to throw further)");

        if (charging)
        {
            float power = Mathf.Clamp01(chargeTimer / chargeTime);
            GUI.Box(new Rect(10, Screen.height - 52, 200f * power, 12), GUIContent.none);
            GUI.Label(new Rect(220, Screen.height - 56, 300, 25), "Let go to throw    Right-click or Esc to cancel");
        }
    }
}
