using System.Collections.Generic;
using UnityEngine;

// The one object that lives for the whole game (it is never destroyed when a room loads). It does two jobs:
//
//   1. Finds the player once, so no script has to search for them:
//        GameManager.Player, GameManager.PlayerCollider, GameManager.PlayerCarry ...
//
//   2. Remembers things between rooms: the last checkpoint, the item Nin is carrying, and which items
//      have been picked up. (These used to be spread over a few scripts.)
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateIfMissing()
    {
        if (Instance != null) return;

        GameObject manager = new GameObject("GameManager");
        manager.AddComponent<GameManager>();
    }

    private void Awake()
    {
        // Only one allowed. If a second one ever turns up, it goes away.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------------------------------------------------------------- pausing

    // The pause key (A, or Options / Start on a controller) freezes the game and shows a "Paused" message
    public static bool IsPaused { get; private set; }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // A new room or screen always starts un-paused
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        SetPaused(false);
    }

    private void Update()
    {
        if (!GameControls.PausePressed) return;

        if (IsPaused)
        {
            SetPaused(false);
            return;
        }

        // Only pause when there is a player to play (not on the menu or the Game Over screen, and not while dying)
        PlayerAnimator animator = PlayerAnimator;
        bool dying = animator != null && animator.IsDead;
        if (Player != null && !dying) SetPaused(true);
    }

    private static void SetPaused(bool paused)
    {
        IsPaused = paused;
        GameControls.Paused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    private void OnGUI()
    {
        if (!IsPaused) return;

        GUI.Box(new Rect(Screen.width / 2f - 120f, Screen.height / 2f - 40f, 240f, 80f), "PAUSED\n\nPress A (or Start) to resume");
    }

    // ---------------------------------------------------------------- finding the player

    // The player is looked for once per room, then remembered. (If the room changes, the old player is
    // destroyed, so the next time anyone asks we look again.)
    private static PlayerMovement player;
    private static Collider2D playerCollider;
    private static PlayerCarry playerCarry;
    private static PlayerAnimator playerAnimator;
    private static HidingScript playerHiding;

    public static PlayerMovement Player { get { if (player == null) FindPlayer(); return player; } }
    public static Collider2D PlayerCollider { get { if (player == null) FindPlayer(); return playerCollider; } }
    public static PlayerCarry PlayerCarry { get { if (player == null) FindPlayer(); return playerCarry; } }
    public static PlayerAnimator PlayerAnimator { get { if (player == null) FindPlayer(); return playerAnimator; } }
    public static HidingScript PlayerHiding { get { if (player == null) FindPlayer(); return playerHiding; } }

    private static void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            playerCollider = null;
            playerCarry = null;
            playerAnimator = null;
            playerHiding = null;
            return;
        }

        // Some of these may be missing on a test player, and that's fine (they just stay null)
        playerCollider = player.GetComponent<Collider2D>();
        playerCarry = player.GetComponent<PlayerCarry>();
        playerAnimator = player.GetComponent<PlayerAnimator>();
        playerHiding = player.GetComponent<HidingScript>();
    }

    // ---------------------------------------------------------------- what the game remembers

    // The last checkpoint Nin reached (an empty scene name means none yet). Retry after dying comes back here.
    public string CheckpointScene { get; private set; } = "";
    public string CheckpointSpawnId { get; private set; } = "";

    // The item Nin is carrying. It is kept switched off, as a child of this object, while she walks between rooms.
    public PickupItem CarriedItem { get; private set; }

    // Items that have been picked up (by id), so they don't come back when a room reloads
    public HashSet<string> TakenItems { get; private set; } = new HashSet<string>();

    // What Nin had at the last checkpoint: a spare copy of her item, and the picked-up list
    private PickupItem checkpointItem;
    private HashSet<string> checkpointTakenItems = new HashSet<string>();

    // Nin picks an item up: switch it off and keep it safe here
    public void HoldItem(PickupItem item)
    {
        CarriedItem = item;
        if (!string.IsNullOrEmpty(item.Id)) TakenItems.Add(item.Id);   // it's not lying in its room any more

        item.gameObject.SetActive(false);
        item.transform.SetParent(transform);
    }

    // Nin puts the item down. It goes back out into the room she is in, and is switched on.
    public void DropItem(PickupItem item, Vector3 position)
    {
        CarriedItem = null;
        if (!string.IsNullOrEmpty(item.Id)) TakenItems.Remove(item.Id);   // it's lying on the floor again

        item.transform.SetParent(null);
        item.transform.position = position;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item.gameObject, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        item.gameObject.SetActive(true);
    }

    // The item is gone for good (it broke, or a tool was used up)
    public void DestroyCarriedItem()
    {
        if (CarriedItem != null) Destroy(CarriedItem.gameObject);
        CarriedItem = null;
    }

    // Called when Nin reaches a checkpoint: remember where, and what she is carrying
    public void SaveCheckpoint(string sceneName, string spawnId)
    {
        CheckpointScene = sceneName;
        CheckpointSpawnId = spawnId;

        if (checkpointItem != null) Destroy(checkpointItem.gameObject);
        checkpointItem = MakeCopy(CarriedItem);
        checkpointTakenItems = new HashSet<string>(TakenItems);
    }

    // Called when Nin retries after dying: she gets back what she had at the checkpoint,
    // and nothing she picked up since
    public void RestoreCheckpoint()
    {
        DestroyCarriedItem();
        CarriedItem = MakeCopy(checkpointItem);
        TakenItems = new HashSet<string>(checkpointTakenItems);
    }

    // A new game: forget everything
    public void ForgetEverything()
    {
        DestroyCarriedItem();
        if (checkpointItem != null) Destroy(checkpointItem.gameObject);
        checkpointItem = null;

        CheckpointScene = "";
        CheckpointSpawnId = "";
        TakenItems = new HashSet<string>();
        checkpointTakenItems = new HashSet<string>();
    }

    // A switched-off copy of an item, kept here (null if there's nothing to copy)
    private PickupItem MakeCopy(PickupItem original)
    {
        if (original == null) return null;

        PickupItem copy = Instantiate(original, transform);
        copy.gameObject.SetActive(false);
        copy.SetUsesLeft(original.UsesLeft);
        return copy;
    }
}