using UnityEngine;
using UnityEngine.InputSystem;

// Goes on Nin's storage in the storage room. Stand next to it and press E to finish the game.
// For now it goes straight to the end screen. Later this is where the end cutscene would start.
// Needs a Collider2D with "Is Trigger" ticked.
public class EndInteract : MonoBehaviour
{
    private Collider2D myCollider;
    private Collider2D playerCollider;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame) return;

        // Find the player the first time we need it
        if (playerCollider == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player == null) return;
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (myCollider.IsTouching(playerCollider))
        {
            GameFlow.LevelComplete();
        }
    }
}
