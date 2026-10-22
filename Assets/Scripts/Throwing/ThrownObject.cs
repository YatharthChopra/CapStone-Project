using UnityEngine;

// The thing Nin throws. It flies through the air, and when it lands it makes a noise
// that nearby enemies can hear (see NoiseSystem). Needs a Rigidbody2D and a Collider2D.
[RequireComponent(typeof(Rigidbody2D))]
public class ThrownObject : MonoBehaviour
{
    [SerializeField] private float lifetime = 6f;   // removed after this long even if it never lands

    private float loudness = 1f;
    private bool landed;
    private Rigidbody2D body;

    // Called by PlayerThrow right after the object is created
    public void Launch(Vector2 velocity, float noiseLoudness, Collider2D thrower)
    {
        loudness = noiseLoudness;

        body = GetComponent<Rigidbody2D>();
        body.linearVelocity = velocity;

        // Don't let it bump into Nin herself
        Collider2D mine = GetComponent<Collider2D>();
        if (thrower != null && mine != null) Physics2D.IgnoreCollision(mine, thrower);

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (landed) return;
        if (collision.collider.GetComponentInParent<PlayerMovement>() != null) return;

        landed = true;
        NoiseSystem.MakeNoise(transform.position, loudness);   // enemies close enough will hear it

        // Stop, flash white for a moment so you can see where it landed, then disappear
        body.linearVelocity = Vector2.zero;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
        Destroy(gameObject, 0.2f);
    }
}
