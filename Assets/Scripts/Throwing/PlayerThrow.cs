using UnityEngine;
using UnityEngine.InputSystem;

// Lets Nin throw things to distract enemies.
//   - Hold the left mouse button to charge a throw. A line of dots shows where it will land.
//   - Let go to throw. A quick click is a short, quiet throw. Holding longer throws further, and louder.
//   - The object is thrown toward the mouse. Moving the mouse up or down changes how high the arc is.
// Nin carries a limited number of throwables (see Max Throwables). Throwable pickups refill them.
// Put this on the Player.
public class PlayerThrow : MonoBehaviour
{
    [Header("What gets thrown")]
    [SerializeField] private ThrownObject throwablePrefab;
    [SerializeField] private int maxThrowables = 3;

    [Header("Throw strength")]
    [SerializeField] private float chargeTime = 1f;      // seconds of holding to reach a full-power throw
    [SerializeField] private float minSpeed = 10f;       // speed of the shortest throw
    [SerializeField] private float maxSpeed = 22f;       // speed of a full-power throw
    [SerializeField] private float minAngle = 30f;       // the lowest the arc can go (degrees above flat)
    [SerializeField] private float maxAngle = 65f;       // the highest the arc can go

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

    private int throwablesLeft;
    private bool charging;
    private float chargeTimer;
    private float throwableGravityScale = 3f;

    private Collider2D myCollider;
    private SpriteRenderer[] dots;

    private void Start()
    {
        throwablesLeft = maxThrowables;
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
        Mouse mouse = Mouse.current;
        if (mouse == null || Time.timeScale == 0f || throwablePrefab == null || Camera.main == null) return;

        // Start charging
        if (mouse.leftButton.wasPressedThisFrame && throwablesLeft > 0)
        {
            charging = true;
            chargeTimer = 0f;
        }

        if (!charging) return;

        // Right-click (or Esc) cancels the throw and you keep the throwable
        Keyboard keyboard = Keyboard.current;
        bool cancelPressed = mouse.rightButton.wasPressedThisFrame || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
        if (cancelPressed)
        {
            charging = false;
            HideDots();
            return;
        }

        chargeTimer += Time.deltaTime;
        float power = Mathf.Clamp01(chargeTimer / chargeTime);

        Vector2 origin;
        Vector2 velocity = GetLaunchVelocity(power, out origin);
        ShowDots(origin, velocity);

        // Let go to throw
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            Throw(origin, velocity, power);
            charging = false;
            HideDots();
        }
    }

    // Works out where the throw starts and how fast it leaves, from the mouse position and how long we've charged
    private Vector2 GetLaunchVelocity(float power, out Vector2 origin)
    {
        origin = (Vector2)transform.position + new Vector2(0f, ThrowHeight);

        Camera cam = Camera.main;
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(cam.transform.position.z)));

        float sideways = mouseWorld.x - origin.x;
        float up = mouseWorld.y - origin.y;

        float directionX = sideways >= 0f ? 1f : -1f;                                // throw left or right
        float angle = Mathf.Atan2(up, Mathf.Abs(sideways)) * Mathf.Rad2Deg;          // how steep the mouse is
        angle = Mathf.Clamp(angle, minAngle, maxAngle);

        float speed = Mathf.Lerp(minSpeed, maxSpeed, power);
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(directionX * Mathf.Cos(radians), Mathf.Sin(radians)) * speed;
    }

    private void Throw(Vector2 origin, Vector2 velocity, float power)
    {
        ThrownObject thrown = Instantiate(throwablePrefab, origin, Quaternion.identity);
        float loudness = Mathf.Lerp(quietNoise, loudNoise, power);   // a harder throw makes more noise
        thrown.Launch(velocity, loudness, myCollider);

        throwablesLeft--;
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

    // Used by pickups. Gives back false if Nin is already carrying the most she can.
    public bool TryAddThrowable()
    {
        if (throwablesLeft >= maxThrowables) return false;
        throwablesLeft++;
        return true;
    }

    // A simple on-screen counter for testing (bottom-left)
    private void OnGUI()
    {
        GUI.Label(new Rect(10, Screen.height - 32, 450, 25),
            "Throwables: " + throwablesLeft + " / " + maxThrowables + "     (hold left mouse to throw further)");

        if (charging)
        {
            float power = Mathf.Clamp01(chargeTimer / chargeTime);
            GUI.Box(new Rect(10, Screen.height - 52, 200f * power, 12), GUIContent.none);
            GUI.Label(new Rect(220, Screen.height - 56, 300, 25), "Let go to throw    Right-click or Esc to cancel");
        }
    }
}
