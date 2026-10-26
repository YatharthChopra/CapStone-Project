using UnityEngine;

// Tells Nin's Animator what she is doing, so it plays the right animation:
//   Idle, Walk, Jump (in the air), Crawl (crouching and moving), CrouchIdle (crouching still).
// The Animator Controller (Assets/Animation/Nin) decides how the animations blend into each other.
// Goes on the Player, next to PlayerMovement. The "Visual" child holds the SpriteRenderer and the Animator.
public class PlayerAnimator : MonoBehaviour
{
    [Header("The picture and its animator (both on the Visual child)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Look")]
    // The pictures are only 64 pixels, so they come in tiny. This scales them up so a standing Nin
    // is about as tall as her 2-unit collider. Change it if Nin looks too big or too small.
    [SerializeField] private float spriteScale = 3.6f;
    [SerializeField] private float feetNudgeY = 0f;   // nudge the picture up (+) or down (-) if her feet float or sink
    [SerializeField] private bool spritesFaceRight = true;   // which way the drawings face

    [Header("Hiding")]
    [Range(0f, 1f)] [SerializeField] private float hiddenAlpha = 0.45f;   // how see-through Nin is while hiding

    // Small numbers, named so it's clear what they are
    private const float MovingSpeed = 0.2f;        // slower than this counts as standing still
    private const float AirGraceTime = 0.08f;      // must be off the ground this long to count as "in the air"
    private const float FullJumpSpeed = 12f;       // same as the jump force in PlayerMovement
    private const float FullWalkSpeed = 5f;        // same as the walk speed in PlayerMovement
    private const float FullCrouchSpeed = 2f;      // same as the crouch speed in PlayerMovement
    private const float FacingSpeed = 0.05f;       // moving slower than this doesn't change which way she faces

    private PlayerMovement movement;
    private Rigidbody2D body;
    private HidingScript hiding;
    private float airTimer;
    private bool facingRight = true;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody2D>();
        hiding = GetComponent<HidingScript>();   // may be missing, that's fine

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // We have a crawl animation now, so the movement script shouldn't squash the picture
        movement.SetSquashVisualWhenCrouching(false);

        PlaceThePicture();
    }

    // Scales the picture up and stands it on the bottom of the collider.
    // (Every picture has its pivot at Nin's feet, so no lifting is needed.)
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
        bool winding = GameFlow.IsFreeMovementScene;   // winding levels have no jumping, and up/down counts as walking

        float speed = winding ? velocity.magnitude : Mathf.Abs(velocity.x);
        bool moving = speed > MovingSpeed;
        bool crouching = movement.IsCrouching;

        // Only count as "in the air" after a moment, so tiny bumps don't flash the jump picture
        if (movement.IsGrounded || winding) airTimer = 0f;
        else airTimer += Time.deltaTime;
        bool inAir = airTimer > AirGraceTime;

        // Walk and crawl play faster the faster Nin moves, so her feet match the ground
        float fullSpeed = crouching ? FullCrouchSpeed : FullWalkSpeed;
        float jumpProgress = Mathf.InverseLerp(FullJumpSpeed, -FullJumpSpeed, velocity.y);   // 0 = going up, 1 = coming down

        animator.SetBool("Moving", moving);
        animator.SetBool("Crouching", crouching);
        animator.SetBool("InAir", inAir);
        animator.SetFloat("WalkRate", Mathf.Clamp(speed / fullSpeed, 0.5f, 1.5f));
        animator.SetFloat("JumpProgress", jumpProgress);

        // Face the way we're walking
        if (Mathf.Abs(velocity.x) > FacingSpeed) facingRight = velocity.x > 0f;
        spriteRenderer.flipX = spritesFaceRight ? !facingRight : facingRight;

        // See-through while hiding (the hiding script also tints her, but only very slightly)
        bool isHidden = hiding != null && hiding.hiding;
        spriteRenderer.color = isHidden ? new Color(1f, 1f, 1f, hiddenAlpha) : Color.white;
    }
}
