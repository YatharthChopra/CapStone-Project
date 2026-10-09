using UnityEngine;

// Goes on the Player. When a room loads it moves the player to the door
// (or spawn point) that GameFlow says we came through.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRoomSpawn : MonoBehaviour
{
    private void Start()
    {
        string id = GameFlow.NextSpawnId;
        Vector3 position = transform.position;
        bool found = false;

        // Look for a door with that ID first
        foreach (Door door in FindObjectsByType<Door>())
        {
            if (door.DoorId == id)
            {
                position = door.SpawnPosition;
                found = true;
            }
        }

        // If there's no matching door, try the spawn points
        if (!found)
        {
            foreach (SpawnPoint spawn in FindObjectsByType<SpawnPoint>())
            {
                if (spawn.SpawnId == id)
                {
                    position = spawn.transform.position;
                    found = true;
                }
            }
        }

        if (!found) return;   // nothing matched, so the player just stays where it was placed

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        transform.position = position;
    }
}
