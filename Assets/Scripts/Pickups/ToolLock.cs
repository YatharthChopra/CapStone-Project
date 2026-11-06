using UnityEngine;
using UnityEngine.Events;

// Something in the level that needs a tool, like a panel that needs the wrench.
// Nin stands in it holding the right item and presses E. It then unlocks a door, and/or runs whatever
// is hooked up to "On Unlocked" in the Inspector (open a gate, turn a light on...).
public class ToolLock : MonoBehaviour
{
    [SerializeField] private string requiredItemName = "Wrench";   // must match the item's Item Name exactly
    [SerializeField] private bool useUpItem = true;                // true = the tool is gone after it's used
    [SerializeField] private Door doorToUnlock;                    // optional: a locked door that opens up
    [SerializeField] private UnityEvent onUnlocked;

    private bool done;
    private Collider2D lockCollider;
    private PlayerCarry carry;

    private void Awake()
    {
        lockCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (done || !GameControls.InteractPressed) return;

        // Find the player the first time we need it
        if (carry == null)
        {
            carry = GameManager.PlayerCarry;
            if (carry == null) return;
        }

        if (!lockCollider.IsTouching(carry.GetComponent<Collider2D>())) return;

        if (carry.Held == null || carry.Held.ItemName != requiredItemName)
        {
            carry.ShowMessage("You need the " + requiredItemName);
            AudioManager.Play(SoundId.DoorLocked);
            return;
        }

        done = true;
        AudioManager.Play(SoundId.ToolUse);
        if (useUpItem) carry.UseUpHeldItem();
        if (doorToUnlock != null) doorToUnlock.Unlock();
        onUnlocked.Invoke();
    }
}
