using UnityEngine;

// This used to hold the test keys (K = die, L = reach the end, N = make a noise). They are removed so players
// can't skip the game by accident. The script is kept (and does nothing) because the Player prefab still has it
// on it, and deleting it would leave a "missing script" warning on the Player.
public class ScreenTestKeys : MonoBehaviour
{
}
