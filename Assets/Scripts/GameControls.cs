using UnityEngine.InputSystem;

// The keys used by the item (pickup) system, all in one place.
// When the control scheme changes (see Jasmine's list), change them here and nothing else needs touching.
// Keyboard: E = pick up / use, Space = swing, Q = drop.   Controller: Y/Triangle = pick up / use, A/Cross = swing.
public static class GameControls
{
    // Pick up an item, or use the held item on something (a door, a panel...)
    public static bool InteractPressed
    {
        get
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            return (kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
        }
    }

    // Swing the held weapon
    public static bool SwingPressed
    {
        get
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            return (kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        }
    }

    // Put the held item back down
    public static bool DropPressed
    {
        get
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb.qKey.wasPressedThisFrame;
        }
    }
}
