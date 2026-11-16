using UnityEngine;

// Lets Nin carry one item at a time (see PickupItem).
//   E (while standing in an item) = pick it up. If her hands are full she drops what she has and swaps.
//   Q = put the item down.
//   Space = swing a weapon. A Wandering Shadow in front of her is stunned for a few seconds.
// She can't swing while crouching. A swing makes a bit of noise, so other enemies may come and look.
// Tools (wrench...) are used on things in the level with E (see ToolLock).
// The item stays with her when she goes through a door (the GameManager looks after it). Put this on the Player.
public class PlayerCarry : MonoBehaviour
{
    [Header("In her hand")]
    [SerializeField] private Vector2 handOffset = new Vector2(0.5f, 0.2f);   // where the item sits, in front of her middle
    [SerializeField] private float restAngle = 25f;                          // how far it leans forward while she just carries it

    // Small numbers, named so it's clear what they are
    private const float SwingTime = 0.25f;         // how long the swing picture takes
    private const float SwingStartAngle = 60f;     // the item starts raised behind her...
    private const float SwingEndAngle = -80f;      // ...and ends pointing forward
    private const float HitHeight = 2.4f;          // how tall the swing's hit area is
    private const float HitStartDistance = 0.3f;   // the hit area starts this far in front of her middle
    private const float MessageTime = 2f;          // how long an on-screen message stays

    private PickupItem held;
    private PlayerMovement movement;
    private PlayerAnimator playerAnimator;
    private Collider2D body;

    private Transform handPivot;            // the item turns around this when she swings
    private SpriteRenderer handPicture;     // the picture of the item in her hand
    private float swingTimer;
    private float swingCooldownLeft;
    private bool lastFacingRight = true;

    private string message = "";
    private float messageTimer;

    public PickupItem Held { get { return held; } }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        playerAnimator = GetComponent<PlayerAnimator>();   // may be missing, that's fine
        body = GetComponent<Collider2D>();
        BuildHandPicture();
    }

    private void Start()
    {
        // Came through a door carrying something? Pick it back up.
        PickupItem carried = GameManager.Instance.CarriedItem;
        if (carried != null) Hold(carried);
    }

    // Makes the little object that shows the item in her hand
    private void BuildHandPicture()
    {
        // (find Nin's own picture first, before we add one of ours)
        SpriteRenderer ninPicture = GetComponentInChildren<SpriteRenderer>();

        GameObject pivot = new GameObject("Held Item");
        pivot.transform.SetParent(transform, false);
        handPivot = pivot.transform;

        GameObject picture = new GameObject("Picture");
        picture.transform.SetParent(handPivot, false);
        handPicture = picture.AddComponent<SpriteRenderer>();

        // Draw it in front of Nin's own picture
        if (ninPicture != null)
        {
            handPicture.sortingLayerID = ninPicture.sortingLayerID;
            handPicture.sortingOrder = ninPicture.sortingOrder + 1;
        }
        handPicture.enabled = false;
    }

    private void Update()
    {
        if (swingTimer > 0f) swingTimer -= Time.deltaTime;
        if (swingCooldownLeft > 0f) swingCooldownLeft -= Time.deltaTime;
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;

        if (Time.timeScale == 0f) return;
        if (playerAnimator != null && playerAnimator.IsDead) return;   // no picking up or swinging while dying

        if (GameControls.InteractPressed) TryPickUp();
        if (held != null && GameControls.DropPressed) Drop();

        bool canSwing = held != null && held.Kind == ItemKind.Weapon && swingCooldownLeft <= 0f && !movement.IsCrouching;
        if (canSwing && GameControls.SwingPressed) Swing();
    }

    private void LateUpdate()
    {
        UpdateHandPicture();
    }

    // ---------------------------------------------------------------- picking up and dropping

    private void TryPickUp()
    {
        PickupItem item = FindItemHere();
        if (item == null) return;

        if (held != null) Drop();   // swap: the old one goes on the floor
        Hold(item);
        AudioManager.Play(SoundId.ItemPickup);
    }

    // The closest item Nin is standing in (null if there isn't one)
    private PickupItem FindItemHere()
    {
        PickupItem closest = null;
        float closestDistance = float.MaxValue;

        foreach (PickupItem item in PickupItem.All)
        {
            if (item.PickupCollider == null || !item.PickupCollider.IsTouching(body)) continue;

            float distance = Vector2.Distance(transform.position, item.transform.position);
            if (distance < closestDistance)
            {
                closest = item;
                closestDistance = distance;
            }
        }
        return closest;
    }

    private void Hold(PickupItem item)
    {
        held = item;
        GameManager.Instance.HoldItem(item);   // the GameManager keeps it safe, so it survives a change of room

        handPicture.sprite = item.HeldSprite;
        handPicture.color = item.HeldColor;
        handPicture.transform.localScale = item.transform.lossyScale;
        float halfLength = item.HeldSprite != null ? item.HeldSprite.bounds.extents.y * item.transform.lossyScale.y : 0.5f;
        handPicture.transform.localPosition = new Vector3(0f, halfLength, 0f);   // the end of the item sits in her hand

        swingTimer = 0f;
        swingCooldownLeft = 0f;
    }

    private void Drop()
    {
        PickupItem item = held;
        held = null;

        // Put it back in this room, on the floor in front of her
        float halfHeight = item.HeldSprite != null ? item.HeldSprite.bounds.extents.y * item.transform.lossyScale.y : 0.5f;
        float x = transform.position.x + FacingSide() * 0.8f;
        float y = body.bounds.min.y + halfHeight;
        GameManager.Instance.DropItem(item, new Vector3(x, y, 0f));

        AudioManager.Play(SoundId.ItemDrop);
    }

    // Gets rid of the held item for good (it broke, or a ToolLock used it up)
    public void UseUpHeldItem()
    {
        if (held == null) return;

        GameManager.Instance.DestroyCarriedItem();
        held = null;
    }

    // ---------------------------------------------------------------- swinging

    private void Swing()
    {
        swingTimer = SwingTime;
        swingCooldownLeft = held.SwingCooldown;
        AudioManager.Play(SoundId.ItemSwing);
        NoiseSystem.MakeNoise(transform.position, held.SwingNoise);

        // The hit area: a box in front of her
        float side = FacingSide();
        Vector2 centre = (Vector2)body.bounds.center + new Vector2(side * (held.Reach / 2f + HitStartDistance), 0f);
        Collider2D[] found = Physics2D.OverlapBoxAll(centre, new Vector2(held.Reach, HitHeight), 0f);

        bool hitSomething = false;
        foreach (Collider2D thing in found)
        {
            WanderingShadow shadow = thing.GetComponentInParent<WanderingShadow>();
            if (shadow == null || shadow.IsStunned) continue;

            // Can't hit one through a wall
            if (!LineOfSight.IsClear(body.bounds.center, shadow.transform.position)) continue;

            shadow.Stun(held.StunTime);
            hitSomething = true;
        }

        if (!hitSomething) return;

        AudioManager.Play(SoundId.ItemHit);
        if (held.UseOnce())
        {
            ShowMessage("The " + held.ItemName + " broke!");
            AudioManager.Play(SoundId.ItemBreak);
            UseUpHeldItem();
        }
    }

    // ---------------------------------------------------------------- the picture in her hand

    private void UpdateHandPicture()
    {
        // Nothing to show if she's empty-handed, or crouching (the item is tucked away)
        bool show = held != null && !movement.IsCrouching;
        handPicture.enabled = show;
        if (!show) return;

        float side = FacingSide();
        handPivot.localPosition = new Vector3(side * handOffset.x, handOffset.y, 0f);
        handPicture.flipX = side < 0f;

        float angle = -restAngle;
        if (swingTimer > 0f)
        {
            float progress = 1f - swingTimer / SwingTime;
            angle = Mathf.Lerp(SwingStartAngle, SwingEndAngle, progress);
        }
        handPivot.localRotation = Quaternion.Euler(0f, 0f, angle * side);
    }

    // 1 = facing right, -1 = facing left
    private float FacingSide()
    {
        if (playerAnimator != null) return playerAnimator.FacingRight ? 1f : -1f;

        // No animator on this player: go by the last way she walked
        if (movement.MoveInput.x != 0f) lastFacingRight = movement.MoveInput.x > 0f;
        return lastFacingRight ? 1f : -1f;
    }

    // ---------------------------------------------------------------- messages and the on-screen text

    // Shows a short message at the bottom of the screen (other scripts use this too)
    public void ShowMessage(string text)
    {
        message = text;
        messageTimer = MessageTime;
    }

    private void OnGUI()
    {
        string status = "Hands: empty";
        if (held != null)
        {
            status = "Holding: " + held.ItemName;
            if (held.Kind == ItemKind.Weapon && held.UsesLeft > 0) status += " (" + held.UsesLeft + " hits left)";
            status += "     Q = drop";
            if (held.Kind == ItemKind.Weapon) status += ",  Space = swing";
        }
        GUI.Label(new Rect(10, Screen.height - 84, 600, 25), status);

        PickupItem here = FindItemHere();
        if (here != null) GUI.Label(new Rect(Screen.width / 2f - 150f, Screen.height - 110f, 300, 25), "C = pick up " + here.ItemName);

        if (messageTimer > 0f) GUI.Label(new Rect(Screen.width / 2f - 150f, Screen.height - 136f, 300, 25), message);
    }
}
