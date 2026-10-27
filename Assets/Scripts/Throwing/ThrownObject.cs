using UnityEngine;

// The thing Nin throws. It flies through the air, and when it lands it makes a noise
// that nearby enemies can hear (see NoiseSystem).
public class ThrownObject : MonoBehaviour
{
    [SerializeField] private float lifetime = 6f;   // removed after this long even if it never lands

    private const float EnemyHearingRadius = 12f;   // the same as the enemies' Hearing Radius (a normal noise)

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
        AudioManager.Play(SoundId.ThrowableLand, transform.position);

        // Stop, flash white for a moment so you can see where it landed, then disappear
        body.linearVelocity = Vector2.zero;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;
            // A ring spreads out to show how far the noise carries
            NoiseRing.Create(transform.position, EnemyHearingRadius * loudness, sr.sprite);
        }
        Destroy(gameObject, 0.2f);
    }
}
