using UnityEngine;

// Tells a Wandering Shadow's Animator what it is doing, so it plays the right animation.
// The look of the shadow changes with its mood (Jasmine's three sets of pictures):
//   Neutral (Idle, Stalk)   Inspect (Inspect)   Hostile (Alert, Attack)
// and the Attack animation plays when it swings.
// The Animator Controller (Assets/Animation/Shadow) decides how the animations blend into each other.
// Goes on the WanderingShadow prefab. The "Visual" child holds the SpriteRenderer and the Animator.
[RequireComponent(typeof(WanderingShadow))]
public class ShadowAnimator : MonoBehaviour
{
    [Header("The picture and its animator (both on the Visual child)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Look")]
    // The pictures are small, so they get scaled up until the shadow is about as tall as its 2-unit collider.
    [SerializeField] private float spriteScale = 6f;
    [SerializeField] private float feetNudgeY = 0f;   // nudge the picture up (+) or down (-) if its feet float or sink
    [SerializeField] private bool spritesFaceRight = true;

    // The attack animation has 17 pictures played at 18 per second. We keep the "swinging" flag on for
    // that long so the animation isn't cut off when the attack windup timer finishes.
    [SerializeField] private float swingAnimationTime = 0.95f;

    // Small numbers, named so it's clear what they are
    private const float MovingSpeed = 0.2f;     // slower than this counts as standing still
    private const float NormalWalkSpeed = 2f;   // the shadow's walking speed (animation plays at normal rate at this speed)
    private const int NeutralLook = 0;
    private const int InspectLook = 1;
    private const int HostileLook = 2;

    private WanderingShadow shadow;
    private Rigidbody2D body;
    private float swingTimer;
    private bool wasWindingUp;

    private void Awake()
    {
        shadow = GetComponent<WanderingShadow>();
        body = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        spriteRenderer.color = Color.white;   // the old grey placeholder colour would tint the pictures
        PlaceThePicture();
    }

    // Scales the picture up and puts its feet on the bottom of the collider.
    // (The pictures have their pivot at the feet, so no lifting is needed.)
    private void PlaceThePicture()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        float feetY = box.offset.y - box.size.y / 2f;

        Transform picture = spriteRenderer.transform;
        picture.localScale = new Vector3(spriteScale, spriteScale, 1f);
        picture.localPosition = new Vector3(0f, feetY + feetNudgeY, 0f);
    }

    private void LateUpdate()
    {
        Vector2 velocity = body.linearVelocity;
        float speed = GameFlow.IsFreeMovementScene ? velocity.magnitude : Mathf.Abs(velocity.x);

        animator.SetBool("Moving", speed > MovingSpeed);
        animator.SetInteger("Look", LookForState(shadow.CurrentState));
        animator.SetBool("Swinging", IsSwinging());
        animator.SetFloat("WalkRate", Mathf.Clamp(speed / NormalWalkSpeed, 0.8f, 2f));

        // Face the way the shadow is looking
        bool facingRight = shadow.Facing > 0;
        spriteRenderer.flipX = spritesFaceRight ? !facingRight : facingRight;
    }

    private int LookForState(WanderingShadow.State state)
    {
        switch (state)
        {
            case WanderingShadow.State.Inspect:
                return InspectLook;
            case WanderingShadow.State.Alert:
            case WanderingShadow.State.Attack:
                return HostileLook;
            default:
                return NeutralLook;
        }
    }

    // The swing animation starts when the windup starts, and plays all the way through
    private bool IsSwinging()
    {
        if (shadow.IsWindingUp && !wasWindingUp) swingTimer = swingAnimationTime;
        wasWindingUp = shadow.IsWindingUp;

        if (shadow.CurrentState != WanderingShadow.State.Attack) swingTimer = 0f;

        swingTimer -= Time.deltaTime;
        return swingTimer > 0f;
    }
}
