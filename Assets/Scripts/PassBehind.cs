using UnityEngine;

// Lets the player walk "behind" part of the level.
// In the real building the Floor 1 hallway is a spiral, so one part passes behind another.
// In our flat 2D drawing those parts overlap, so instead we use two of these zones:
// walk into one and you come out of the other, still walking, as if you went through the back.
// Needs a Collider2D with "Is Trigger" ticked. Each zone points at the other one's "Arrival Point".
public class PassBehind : MonoBehaviour
{
    [SerializeField] private Transform arrivalPoint;     // where the player appears on the other side
    // 1 = the player has to be walking right to use this, -1 = walking left, 0 = either way.
    // (This stops the player bouncing straight back when they arrive.)
    [SerializeField] private int requiredDirection = 1;
    [SerializeField] private float cooldown = 0.5f;      // seconds before any passage can be used again

    // Shared by every passage so arriving at one doesn't set off the other
    private static float nextAllowedTime;

    private Collider2D myCollider;
    private PlayerMovement player;
    private Rigidbody2D playerBody;
    private Collider2D playerCollider;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (Time.time < nextAllowedTime) return;

        // Find the player the first time we need it
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovement>();
            if (player == null) return;
            playerBody = player.GetComponent<Rigidbody2D>();
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (!myCollider.IsTouching(playerCollider)) return;

        // Only go through if the player is walking the right way
        float walking = playerBody.linearVelocity.x;
        if (requiredDirection != 0 && walking * requiredDirection < 0.5f) return;

        // Keep the same height relative to the passage, only move sideways to the other side
        float heightDifference = arrivalPoint.position.y - transform.position.y;
        Vector2 newPosition = new Vector2(arrivalPoint.position.x, playerBody.position.y + heightDifference);

        playerBody.position = newPosition;
        player.transform.position = newPosition;   // keep the Transform in step (velocity is kept as it is)

        nextAllowedTime = Time.time + cooldown;
    }
}
