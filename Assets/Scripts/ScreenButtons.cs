using UnityEngine;

// Goes on an object in the menu / death / end scenes.
// UI buttons can only call methods on a component, so these just pass the click on to GameFlow.
public class ScreenButtons : MonoBehaviour
{
    public void Play()
    {
        GameFlow.StartGame();
    }

    // Retry just starts the game scene again
    public void Retry()
    {
        GameFlow.StartGame();
    }

    public void MainMenu()
    {
        GameFlow.GoToMainMenu();
    }

    public void Quit()
    {
        GameFlow.QuitGame();
    }
}
