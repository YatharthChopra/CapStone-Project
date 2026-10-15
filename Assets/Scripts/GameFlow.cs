using UnityEngine;
using UnityEngine.SceneManagement;

// Handles moving between the main menu, the game, the death screen and the end screen.
// It's "static", so any script can call it without needing to find an object first.
// Example (for Alice's fail state):  GameFlow.PlayerDied();
public static class GameFlow
{
    // These must match the scene file names exactly.
    public const string MainMenuScene = "MainMenu";
    public const string GameScene = "Apt_Bedroom";   // the first room Nin starts in
    public const string GameOverScene = "GameOver";
    public const string EndScene = "TheEnd";

    // Which door / spawn point the player should appear at in the next room.
    // "Start" is the default spawn point for a fresh game.
    public static string NextSpawnId = "Start";

    public static void StartGame()
    {
        NextSpawnId = "Start";
        LoadScene(GameScene);
    }

    // Used by doors: load another room and say where the player should show up
    public static void GoToRoom(string sceneName, string spawnId)
    {
        NextSpawnId = spawnId;
        LoadScene(sceneName);
    }

    public static void GoToMainMenu()
    {
        LoadScene(MainMenuScene);
    }

    // Call this when the player gets caught
    public static void PlayerDied()
    {
        LoadScene(GameOverScene);
    }

    // Call this when the end cutscene / ending is reached
    public static void LevelComplete()
    {
        LoadScene(EndScene);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // quits play mode when testing in the editor
#else
        Application.Quit();
#endif
    }

    private static void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;   // in case the game was paused
        SceneManager.LoadScene(sceneName);
    }
}
