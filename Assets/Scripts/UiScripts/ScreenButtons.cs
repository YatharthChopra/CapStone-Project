using UnityEngine;

// Goes on an object in the menu / death / end scenes.
// UI buttons can only call methods on a component, so these just pass the click on to GameFlow.
public class ScreenButtons : MonoBehaviour
{
    public void Play()
    {
        AudioManager.Play(SoundId.ButtonClick);
        GameFlow.StartGame();
    }

    // Retry goes back to the last checkpoint (or the start if no checkpoint was reached)
    public void Retry()
    {
        AudioManager.Play(SoundId.ButtonClick);
        GameFlow.RetryFromCheckpoint();
    }

    public void MainMenu()
    {
        AudioManager.Play(SoundId.ButtonClick);
        GameFlow.GoToMainMenu();
    }

    public void Quit()
    {
        GameFlow.QuitGame();
    }
}
