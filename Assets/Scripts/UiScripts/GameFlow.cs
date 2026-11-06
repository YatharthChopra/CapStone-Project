using UnityEngine;
using UnityEngine.SceneManagement;

// Handles moving between the main menu, the game, the death screen and the end screen.
// It's "static", so any script can call it without needing to find an object first.
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

    // The last checkpoint the player reached, and what they were carrying, is remembered by the GameManager
    // (an empty scene name means no checkpoint yet). When the player dies and presses Retry, they come back here.

    // Starts a brand new game: forgets any checkpoint and goes to the first room
    public static void StartGame()
    {
        GameManager.Instance.ForgetEverything();   // no checkpoint, and Nin starts empty-handed
        NextSpawnId = "Start";
        LoadScene(GameScene);
    }

    // Called by Checkpoint objects when the player arrives in a checkpoint room
    public static void SetCheckpoint(string sceneName, string spawnId)
    {
        GameManager.Instance.SaveCheckpoint(sceneName, spawnId);   // also remembers what Nin is carrying right now
    }

    // Retry after dying: go back to the last checkpoint, or the very start if there isn't one
    public static void RetryFromCheckpoint()
    {
        GameManager manager = GameManager.Instance;
        if (string.IsNullOrEmpty(manager.CheckpointScene))
        {
            StartGame();
            return;
        }

        manager.RestoreCheckpoint();   // she gets back what she was carrying at the checkpoint
        NextSpawnId = manager.CheckpointSpawnId;
        LoadScene(manager.CheckpointScene);
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
    // Nin plays her death animation first, then ShowGameOverScreen is called
    public static void PlayerDied()
    {
        PlayerAnimator animator = GameManager.PlayerAnimator;
        if (animator != null && animator.IsDead) return;   // already dying (the enemy may call this again)

        AudioManager.Play(SoundId.PlayerDeath);

        if (animator == null)
        {
            ShowGameOverScreen();   // no animated Nin in this scene, so go straight to the screen
            return;
        }

        animator.PlayDeath();
    }

    public static void ShowGameOverScreen()
    {
        LoadScene(GameOverScene);
    }

    // Call this when the end cutscene / ending is reached
    public static void LevelComplete()
    {
        AudioManager.Play(SoundId.LevelComplete);
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
