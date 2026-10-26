using UnityEngine;
using UnityEngine.Rendering.Universal;

// Plays Nin's own sounds: footsteps, jumping, landing, hiding and the flashlight.
// Goes on the Player, next to PlayerMovement. It only watches the other scripts, so nothing else had to change.
public class PlayerAudio : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private float walkStepTime = 0.4f;     // seconds between footsteps while walking
    [SerializeField] private float crouchStepTime = 0.65f;  // slower while crouched

    private const float MovingSpeed = 0.5f;      // slower than this counts as standing still
    private const float MinAirTimeForLanding = 0.15f;   // tiny hops don't make a landing sound

    private PlayerMovement movement;
    private Rigidbody2D body;
    private HidingScript hiding;
    private GameObject flashlight;

    private float stepTimer;
    private float airTime;
    private bool wasGrounded = true;
    private bool wasHiding;
    private bool flashlightWasOn = true;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody2D>();
        hiding = GetComponent<HidingScript>();

        Light2D light = GetComponentInChildren<Light2D>(true);
        if (light != null)
        {
            flashlight = light.gameObject;
            flashlightWasOn = flashlight.activeSelf;
        }
    }

    private void Update()
    {
        PlayMovementSounds();
        PlayHidingSounds();
        PlayFlashlightSounds();
    }

    private void PlayMovementSounds()
    {
        bool freeMovement = GameFlow.IsFreeMovementScene;   // winding levels have no jumping or landing
        bool onGround = movement.IsGrounded || freeMovement;

        // Footsteps while moving along the ground
        bool moving = body.linearVelocity.magnitude > MovingSpeed;
        if (onGround && moving)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                AudioManager.Play(movement.IsCrouching ? SoundId.FootstepCrouch : SoundId.Footstep);
                stepTimer = movement.IsCrouching ? crouchStepTime : walkStepTime;
            }
        }
        else
        {
            stepTimer = 0f;   // so the first step after stopping plays straight away
        }

        // Jumping and landing
        if (wasGrounded && !onGround && body.linearVelocity.y > 1f) AudioManager.Play(SoundId.Jump);

        if (!onGround) airTime += Time.deltaTime;
        if (!wasGrounded && onGround && airTime >= MinAirTimeForLanding) AudioManager.Play(SoundId.Land);
        if (onGround) airTime = 0f;

        wasGrounded = onGround;
    }

    private void PlayHidingSounds()
    {
        if (hiding == null) return;

        if (hiding.hiding && !wasHiding) AudioManager.Play(SoundId.Hide);
        if (!hiding.hiding && wasHiding) AudioManager.Play(SoundId.Unhide);
        wasHiding = hiding.hiding;
    }

    private void PlayFlashlightSounds()
    {
        if (flashlight == null) return;

        bool isOn = flashlight.activeSelf;
        if (isOn != flashlightWasOn)
        {
            AudioManager.Play(isOn ? SoundId.FlashlightOn : SoundId.FlashlightOff);
            flashlightWasOn = isOn;
        }
    }
}
