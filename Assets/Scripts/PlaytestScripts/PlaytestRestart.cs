using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// For the playtest playground only: press R to start the whole playground over from the beginning.
// (Dying and pressing Retry goes back to the last station instead. This is for starting completely fresh.)
public class PlaytestRestart : MonoBehaviour
{
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.rKey.wasPressedThisFrame) return;

        string sceneName = SceneManager.GetActiveScene().name;

        // Make the checkpoint the very start of this scene, then reload it at that spot
        GameFlow.SetCheckpoint(sceneName, "Start");
        GameFlow.GoToRoom(sceneName, "Start");
    }
}
