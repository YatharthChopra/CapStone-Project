using UnityEngine;
using UnityEngine.SceneManagement;

// Marks a room as a checkpoint. When the player arrives in the room, the game remembers it.
// If the player dies later, Retry sends them back here instead of to the very start.
//
// "Spawn Id" is where the player appears when they come back. It should match either:
//   - the ID of a Door in the room (the player appears next to that door), or
//   - the ID of a SpawnPoint in the room (the player appears on that spot).
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "Checkpoint";

    private void Start()
    {
        GameFlow.SetCheckpoint(SceneManager.GetActiveScene().name, spawnId);
    }
}
