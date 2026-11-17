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

    // True if the door goes somewhere right now (the on-screen hint only shows for doors that work)
    public bool IsUsable { get { return !isLocked && !string.IsNullOrEmpty(targetScene); } }

    // Nin's apartment rooms are all named Apt_Something
    private const string ApartmentScenePrefix = "Apt_";

    // A ToolLock calls this when the right tool has been used (e.g. the wrench on the panel)
    public void Unlock()
    {
        isLocked = false;
    }

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

        // In Nin's apartment the doors teach the player how to use them (until they have left the apartment once)
        bool inApartment = gameObject.scene.name.StartsWith(ApartmentScenePrefix);
        bool hintAlreadyDone = GameManager.Instance != null && GameManager.Instance.DoorHintDone;
        if (inApartment && !hintAlreadyDone && GetComponent<DoorHint>() == null) gameObject.AddComponent<DoorHint>();
    }

    private void Update()
    {
        if (!GameControls.InteractPressed) return;

        // Find the player the first time we need it
        if (playerCollider == null)
        {
            PlayerMovement player = GameManager.Player;
            if (player == null) return;
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (!doorCollider.IsTouching(playerCollider)) return;

        if (isLocked || string.IsNullOrEmpty(targetScene))
        {
            Debug.Log(doorId + " is locked / not connected yet");
            AudioManager.Play(SoundId.DoorLocked);
            return;
        }

        AudioManager.Play(SoundId.DoorOpen);
        // Going out of the apartment: the "press C" hint has done its job, so it never comes back this run
        if (!targetScene.StartsWith(ApartmentScenePrefix) && GameManager.Instance != null) GameManager.Instance.DoorHintDone = true;

        GameFlow.GoToRoom(targetScene, targetDoorId);
    }
}
