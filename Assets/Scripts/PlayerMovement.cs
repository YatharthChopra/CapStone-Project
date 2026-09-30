using UnityEngine;
using UnityEngine.InputSystem;

// Basic side-scroller movement for Nin.
// Controls: A/D = left/right, W/S = up/down, Space = jump, Left Ctrl = crouch
// Put this on the player object together with a Rigidbody2D and a BoxCollider2D.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2f;   // slower while crouching so sneaking feels different
    [SerializeField] private float jumpForce = 12f;

    [Header("Up / Down (W and S)")]
    // W/S are always read (see MoveInput), but the player only moves up/down
    // when this is ticked. Left off for now because it fights with gravity and jumping.
    // We can turn it on later once we know how the level uses it.
    [SerializeField] private bool allowVerticalMove = false;
    [SerializeField] private float verticalSpeed = 3f;

    [Header("Crouch")]
    [Range(0.2f, 1f)]
    [SerializeField] private float crouchHeightMultiplier = 0.5f;  // 0.5 = half height
    // Optional: drag the sprite child in here so it also shrinks when crouching
    [SerializeField] private Transform visual;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.05f;

    // Other scripts (enemy AI, camera, etc.) can read these
    public bool IsCrouching { get; private set; }
    public bool IsGrounded { get; private set; }
    public Vector2 MoveInput { get; private set; }   // x = A/D, y = S/W

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private ContactFilter2D filter;
    private readonly Collider2D[] hits = new Collider2D[8];

    private Vector2 standingSize;
    private Vector2 standingOffset;
    private bool jumpPressed;
    private float originalGravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        rb.freezeRotation = true;   // so the player doesn't tip over
        originalGravity = rb.gravityScale;

        // Remember the standing collider so we can go back to it after crouching
        standingSize = box.size;
        standingOffset = box.offset;

        // Ignore triggers when checking for ground / ceiling
        filter = new ContactFilter2D();
        filter.useTriggers = false;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Left / right
        float x = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;

        // Up / down
        float y = 0f;
        if (kb.sKey.isPressed) y -= 1f;
        if (kb.wKey.isPressed) y += 1f;

        MoveInput = new Vector2(x, y);

        // Jump - we save it here and use it in FixedUpdate so we don't miss a press
        if (kb.spaceKey.wasPressedThisFrame) jumpPressed = true;

        HandleCrouch(kb.leftCtrlKey.isPressed);
    }

    private void FixedUpdate()
    {
        IsGrounded = CheckGrounded();

        float speed = IsCrouching ? crouchSpeed : walkSpeed;
        Vector2 velocity = rb.linearVelocity;
        velocity.x = MoveInput.x * speed;

        if (allowVerticalMove)
        {
            // Turn gravity off so W/S can move us up and down freely
            rb.gravityScale = 0f;
            velocity.y = MoveInput.y * verticalSpeed;
        }
        else
        {
            rb.gravityScale = originalGravity;

            // Can only jump on the ground, and not while crouching
            if (jumpPressed && IsGrounded && !IsCrouching)
            {
                velocity.y = jumpForce;
            }
        }

        jumpPressed = false;
        rb.linearVelocity = velocity;
    }

    private void HandleCrouch(bool ctrlHeld)
    {
        if (ctrlHeld && !IsCrouching)
        {
            SetCrouch(true);
        }
        else if (!ctrlHeld && IsCrouching && CanStandUp())
        {
            // If something is above our head we stay crouched until it's clear
            SetCrouch(false);
        }
    }

    private void SetCrouch(bool crouch)
    {
        IsCrouching = crouch;

        float heightMult = crouch ? crouchHeightMultiplier : 1f;
        float newHeight = standingSize.y * heightMult;

        box.size = new Vector2(standingSize.x, newHeight);

        // Move the offset so the feet stay in the same place (otherwise we'd float or sink)
        float feetY = standingOffset.y - standingSize.y / 2f;
        box.offset = new Vector2(standingOffset.x, feetY + newHeight / 2f);

        if (visual != null)
        {
            Vector3 s = visual.localScale;
            visual.localScale = new Vector3(s.x, crouch ? Mathf.Abs(s.y) * crouchHeightMultiplier : Mathf.Abs(s.y) / crouchHeightMultiplier, s.z);
            // Keep the sprite's bottom lined up with the collider's bottom
            Vector3 p = visual.localPosition;
            visual.localPosition = new Vector3(p.x, box.offset.y, p.z);
        }
    }

    private bool CheckGrounded()
    {
        // Small box just under the feet
        Bounds b = box.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance / 2f);
        Vector2 size = new Vector2(b.size.x * 0.9f, groundCheckDistance);
        return OverlapsSomethingElse(center, size);
    }

    private bool CanStandUp()
    {
        // Box covering the extra space we need to stand back up
        Bounds b = box.bounds;
        float extra = standingSize.y - box.size.y;
        Vector2 center = new Vector2(b.center.x, b.max.y + extra / 2f);
        Vector2 size = new Vector2(b.size.x * 0.9f, extra);
        return !OverlapsSomethingElse(center, size);
    }

    // True if the box touches any collider that isn't the player
    private bool OverlapsSomethingElse(Vector2 center, Vector2 size)
    {
        int count = Physics2D.OverlapBox(center, size, 0f, filter, hits);
        for (int i = 0; i < count; i++)
        {
            if (hits[i] != box) return true;
        }
        return false;
    }
}
