using UnityEngine;

// Something Nin can pick up to refill her throwables. Walk into it to collect it.
// Needs a Collider2D with "Is Trigger" ticked. If she's already carrying the most she can, it stays put.
public class ThrowablePickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerThrow thrower = other.GetComponentInParent<PlayerThrow>();
        if (thrower == null) return;

        if (thrower.TryAddThrowable())
        {
            Destroy(gameObject);
        }
    }
}
