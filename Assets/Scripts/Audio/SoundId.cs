// Every sound effect in the game has a name in this list.
// Code plays a sound with:  AudioManager.Play(SoundId.Jump);
// To add a new sound: add a name here, call Play from the place it should happen,
// then add a matching entry in the Sounds list on the AudioManager prefab.
public enum SoundId
{
    // Nin
    Footstep,
    FootstepCrouch,
    Jump,
    Land,
    Hide,
    Unhide,
    FlashlightOn,
    FlashlightOff,
    PlayerDeath,

    // Rooms and objects
    DoorOpen,
    DoorLocked,
    Checkpoint,
    LightFlicker,

    // Throwing
    Throw,
    ThrowableLand,
    ThrowablePickup,

    // Enemies
    EnemyQuestion,   // the "??" signal
    EnemyAlert,      // the "!!" signal
    EnemySwing,      // a Wandering Shadow swinging at Nin

    // Menus
    ButtonClick,
    LevelComplete
}
