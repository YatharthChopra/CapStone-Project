using UnityEngine;
using UnityEngine.SceneManagement;

// Marks a spot as a checkpoint. If the player dies later, Retry sends them back here
// instead of to the very start.
//
// There are two ways it can be triggered (tick the box in the Inspector):
//   - Unticked: the checkpoint is saved as soon as the room loads (the player arrives in the room).
//   - Ticked:   the checkpoint is saved when the player walks into this object (it needs a trigger Collider2D).
//
// "Spawn Id" is where the player appears when they come back. It should match either:
//   - the ID of a Door in the room (the player appears next to that door), or
//   - the ID of a SpawnPoint in the room (the player appears on that spot).
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "Checkpoint";
    [SerializeField] private bool saveWhenPlayerWalksIn = false;

    private void Start()
    {
        if (!saveWhenPlayerWalksIn) Save();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!saveWhenPlayerWalksIn) return;
        if (other.GetComponentInParent<PlayerMovement>() != null) Save();
    }

    private void Save()
    {
        GameFlow.SetCheckpoint(SceneManager.GetActiveScene().name, spawnId);
    }
}
