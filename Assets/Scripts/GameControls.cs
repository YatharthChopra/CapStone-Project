using UnityEngine;
using UnityEngine.InputSystem;

// Every key and button the game uses, in one place. Jasmine's control list is set up here, and the other
// scripts just ask "was interact pressed?" so changing a key only ever means changing it in this file.
//
//   KEYBOARD                                    CONTROLLER
//   Left / Right arrow   move                   D-pad left / right (or left stick)  move
//   Up arrow             hide in an alcove      D-pad up                            hide in an alcove
//   Down arrow           crouch                 D-pad down                          crouch
//   Space                swing the weapon       A / Cross                           swing the weapon
//   Z                    throw (hold + release) X / Square                          throw
//   X                    flashlight on / off    B / Circle                          flashlight on / off
//   C                    interact               Y / Triangle                        interact
//   S                    change flashlight type R2                                  change flashlight type
//   A                    pause                  Options / Start                     pause
//                                               R1 / L1                             throw aim up / down
//   Extras that are not on the list: Q = drop the held item (controller: L2), Left Ctrl = crouch
//   (controller: left stick click), and the left stick moves too.
//
// In the winding levels (see GameFlow.FreeMovementScenes) the Up and Down arrows are needed for walking up and
// down, so there: crouch is Left Ctrl (or the stick click), and hiding in an alcove uses the interact key. (will be changed after we have alice's gravity in)
public static class GameControls
{
    // GameManager sets this while the game is paused, so nothing reacts to the keys until it is resumed
    public static bool Paused;

    private const float StickDeadZone = 0.5f;

    private static Keyboard Kb { get { return Keyboard.current; } }
    private static Gamepad Pad { get { return Gamepad.current; } }

    // ---------------------------------------------------------------- moving

    // -1 = left, +1 = right
    public static float MoveX
    {
        get
        {
            if (Paused) return 0f;
            float x = 0f;
            if (Kb != null)
            {
                if (Kb.leftArrowKey.isPressed) x -= 1f;
                if (Kb.rightArrowKey.isPressed) x += 1f;
            }
            if (Pad != null)
            {
                if (Pad.dpad.left.isPressed) x -= 1f;
                if (Pad.dpad.right.isPressed) x += 1f;
                if (x == 0f && Mathf.Abs(Pad.leftStick.x.ReadValue()) > StickDeadZone) x = Mathf.Sign(Pad.leftStick.x.ReadValue());
            }
            return Mathf.Clamp(x, -1f, 1f);
        }
    }

    // -1 = down, +1 = up. Only used in the winding levels (everywhere else there is no walking up and down)
    public static float MoveY
    {
        get
        {
            if (Paused) return 0f;
            float y = 0f;
            if (Kb != null)
            {
                if (Kb.downArrowKey.isPressed) y -= 1f;
                if (Kb.upArrowKey.isPressed) y += 1f;
            }
            if (Pad != null)
            {
                if (Pad.dpad.down.isPressed) y -= 1f;
                if (Pad.dpad.up.isPressed) y += 1f;
                if (y == 0f && Mathf.Abs(Pad.leftStick.y.ReadValue()) > StickDeadZone) y = Mathf.Sign(Pad.leftStick.y.ReadValue());
            }
            return Mathf.Clamp(y, -1f, 1f);
        }
    }

    // Crouch is held down. (Down arrow, but not in the winding levels, where Down means "walk down")
    public static bool CrouchHeld
    {
        get
        {
            if (Paused) return false;
            bool winding = GameFlow.IsFreeMovementScene;
            if (Kb != null)
            {
                if (Kb.leftCtrlKey.isPressed) return true;
                if (!winding && Kb.downArrowKey.isPressed) return true;
            }
            if (Pad != null)
            {
                if (Pad.leftStickButton.isPressed) return true;
                if (!winding && Pad.dpad.down.isPressed) return true;
            }
            return false;
        }
    }

    // ---------------------------------------------------------------- hiding and interacting

    // Hide in (or come out of) an alcove
    public static bool HidePressed
    {
        get
        {
            if (Paused) return false;
            if (GameFlow.IsFreeMovementScene) return InteractPressed;   // Up is needed for walking in the winding levels

            return (Kb != null && Kb.upArrowKey.wasPressedThisFrame)
                || (Pad != null && Pad.dpad.up.wasPressedThisFrame);
        }
    }

    // Open a door, pick an item up, or use the held item on something
    public static bool InteractPressed
    {
        get
        {
            if (Paused) return false;
            return (Kb != null && Kb.cKey.wasPressedThisFrame)
                || (Pad != null && Pad.buttonNorth.wasPressedThisFrame);
        }
    }

    // ---------------------------------------------------------------- items and throwing

    // Swing the held weapon
    public static bool SwingPressed
    {
        get
        {
            if (Paused) return false;
            return (Kb != null && Kb.spaceKey.wasPressedThisFrame)
                || (Pad != null && Pad.buttonSouth.wasPressedThisFrame);
        }
    }

    // Put the held item back down
    public static bool DropPressed
    {
        get
        {
            if (Paused) return false;
            return (Kb != null && Kb.qKey.wasPressedThisFrame)
                || (Pad != null && Pad.leftTrigger.wasPressedThisFrame);
        }
    }

    // Throw: press to start charging, let go to throw
    public static bool ThrowPressed
    {
        get
        {
            if (Paused) return false;
            return (Kb != null && Kb.zKey.wasPressedThisFrame)
                || (Pad != null && Pad.buttonWest.wasPressedThisFrame);
        }
    }

    public static bool ThrowReleased
    {
        get
        {
            return (Kb != null && Kb.zKey.wasReleasedThisFrame)
                || (Pad != null && Pad.buttonWest.wasReleasedThisFrame);
        }
    }

    // Cancel a throw that is being charged
    public static bool ThrowCancelPressed
    {
        get
        {
            return (Kb != null && Kb.escapeKey.wasPressedThisFrame)
                || (Pad != null && Pad.selectButton.wasPressedThisFrame);
        }
    }

    // Controller only: R1 aims the throw higher (+1), L1 aims it lower (-1)
    public static float ThrowAimAdjust
    {
        get
        {
            if (Pad == null) return 0f;
            float adjust = 0f;
            if (Pad.rightShoulder.isPressed) adjust += 1f;
            if (Pad.leftShoulder.isPressed) adjust -= 1f;
            return adjust;
        }
    }

    // ---------------------------------------------------------------- flashlight and pause

    public static bool FlashlightTogglePressed
    {
        get
        {
            if (Paused) return false;
            return (Kb != null && Kb.xKey.wasPressedThisFrame)
                || (Pad != null && Pad.buttonEast.wasPressedThisFrame);
        }
    }

    public static bool FlashlightTypePressed
    {
        get
        {
            if (Paused) return false;
            return (Kb != null && Kb.sKey.wasPressedThisFrame)
                || (Pad != null && Pad.rightTrigger.wasPressedThisFrame);
        }
    }

    public static bool PausePressed
    {
        get
        {
            return (Kb != null && Kb.aKey.wasPressedThisFrame)
                || (Pad != null && Pad.startButton.wasPressedThisFrame);
        }
    }
}
