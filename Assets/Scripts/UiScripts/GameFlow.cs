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

    // Scenes where the level winds up and down instead of being flat.
    // In these gravity is turned off and W/S move the player up and down.
    // To add another level like this, just add its scene name to the list.
    private static readonly string[] FreeMovementScenes = { "Floor1_Corridor", "Floor4_Hall" };

    public static bool IsFreeMovementScene
    {
        get
        {
            string current = SceneManager.GetActiveScene().name;
            return System.Array.IndexOf(FreeMovementScenes, current) >= 0;
        }
    }

    // Which door / spawn point the player should appear at in the next room.
    // "Start" is the default spawn point for a fresh game.
    // Other scripts can read this (PlayerRoomSpawn does), but only GameFlow changes it.
    public static string NextSpawnId { get; private set; } = "Start";

    // The last checkpoint the player reached (an empty scene name means none yet).
    // When the player dies and presses Retry, they come back here.
    private static string checkpointScene = "";
    private static string checkpointSpawnId = "";

    // Starts a brand new game: forgets any checkpoint and goes to the first room
    public static void StartGame()
    {
        checkpointScene = "";
        checkpointSpawnId = "";
        NextSpawnId = "Start";
        LoadScene(GameScene);
    }

    // Called by Checkpoint objects when the player arrives in a checkpoint room
    public static void SetCheckpoint(string sceneName, string spawnId)
    {
        checkpointScene = sceneName;
        checkpointSpawnId = spawnId;
    }

    // Retry after dying: go back to the last checkpoint, or the very start if there isn't one
    public static void RetryFromCheckpoint()
    {
        if (checkpointScene == "")
        {
            StartGame();
            return;
        }

        NextSpawnId = checkpointSpawnId;
        LoadScene(checkpointScene);
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
