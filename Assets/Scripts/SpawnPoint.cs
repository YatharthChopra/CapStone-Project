using UnityEngine;

// A spot where the player can appear that isn't a door.
// "Start" is used when a new game begins.
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "Start";

    public string SpawnId { get { return spawnId; } }
}
