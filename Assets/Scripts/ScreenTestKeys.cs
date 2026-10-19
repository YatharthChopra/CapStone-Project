using UnityEngine;
using UnityEngine.InputSystem;

// TEMPORARY - only here so we can test the death and end screens before the real triggers exist.
// K = die, L = reach the end.
// Delete this script (and the object it's on) once Alice's fail state and the end cutscene are in.
public class ScreenTestKeys : MonoBehaviour
{
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.kKey.wasPressedThisFrame) GameFlow.PlayerDied();
        if (kb.lKey.wasPressedThisFrame) GameFlow.LevelComplete();
    }
}
