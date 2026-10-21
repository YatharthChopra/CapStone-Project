using UnityEngine;
using UnityEngine.InputSystem;

// A door that takes the player to another room when they stand in it and press E.
// Every door has an ID. The door on the other side uses that ID as its "target",
// so the player appears next to the right door when the new room loads.
// Needs a Collider2D with "Is Trigger" ticked.
public class Door : MonoBehaviour
{
    [SerializeField] private string doorId;          // this door's own name, e.g. "LR_Kitchen"
    [SerializeField] private string targetScene;     // the room this door leads to
    [SerializeField] private string targetDoorId;    // the door in that room we come out of
    [SerializeField] private bool isLocked = false;  // locked doors do nothing for now
    // How far sideways from the door the player appears. Useful when a door is set into a wall,
    // so the player doesn't appear inside it. (Positive = to the right, negative = to the left.)
    [SerializeField] private float spawnOffsetX = 0f;

    public string DoorId { get { return doorId; } }

    // Where the player should stand when they arrive through this door
    public Vector3 SpawnPosition
    {
        get
        {
            float bottom = transform.position.y - transform.lossyScale.y / 2f;
            return new Vector3(transform.position.x + spawnOffsetX, bottom + 1.05f, 0f);
        }
    }

    private Collider2D doorCollider;
    private Collider2D playerCollider;

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
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

        if (!doorCollider.IsTouching(playerCollider)) return;

        if (isLocked || string.IsNullOrEmpty(targetScene))
        {
            Debug.Log(doorId + " is locked / not connected yet");
            return;
        }

        GameFlow.GoToRoom(targetScene, targetDoorId);
    }
}
