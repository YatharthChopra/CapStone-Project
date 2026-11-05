using System.Collections.Generic;
using UnityEngine;

public enum ItemKind { Weapon, Tool }

// Something Nin can pick up and carry: a hockey stick, a wrench, bolt cutters...
// Put it on an object with a SpriteRenderer and a Collider2D with "Is Trigger" ticked
// (make the collider a bit bigger than the picture so it's easy to stand in).
// Nin stands in it and presses E to pick it up (see PlayerCarry).
//   Weapon -> Nin swings it to stun a Wandering Shadow. It can break after a few hits.
//   Tool   -> Nin uses it on something in the level (see ToolLock).
// To make a new item: duplicate the HockeyStick or Wrench prefab, change the name, picture and numbers.
public class PickupItem : MonoBehaviour
{
    [Header("What it is")]
    [SerializeField] private string itemName = "Hockey Stick";
    [SerializeField] private ItemKind kind = ItemKind.Weapon;
    // The picture shown in Nin's hand. Leave empty to use this object's own picture.
    [SerializeField] private Sprite heldSprite;

    [Header("Weapon only")]
    [SerializeField] private float stunTime = 4f;        // how long a hit shadow is frozen
    [SerializeField] private int uses = 3;               // hits before it breaks (0 = never breaks)
    [SerializeField] private float swingCooldown = 0.8f; // seconds before Nin can swing again
    [SerializeField] private float reach = 2.2f;         // how far in front of Nin the swing hits
    [SerializeField] private float swingNoise = 0.6f;    // how loud a swing is (1 = a normal noise). Other enemies may come and look.

    // Every item lying around in the scene (Nin looks through this list when she presses E)
    public static readonly List<PickupItem> All = new List<PickupItem>();

    public string ItemName { get { return itemName; } }
    public ItemKind Kind { get { return kind; } }
    public float StunTime { get { return stunTime; } }
    public float SwingCooldown { get { return swingCooldown; } }
    public float Reach { get { return reach; } }
    public float SwingNoise { get { return swingNoise; } }
    public int UsesLeft { get; private set; }

    // Items Nin has picked up (by id). When a room reloads, an item that was picked up doesn't come back.
    // PlayerCarry saves and restores this at checkpoints, so dying puts things back how they were at the checkpoint.
    public static HashSet<string> Taken = new HashSet<string>();

    private Collider2D pickupCollider;
    private SpriteRenderer picture;
    private bool usesSet;

    public string Id { get; private set; }
    public Collider2D PickupCollider { get { return pickupCollider; } }
    public Sprite HeldSprite { get { return heldSprite != null ? heldSprite : (picture != null ? picture.sprite : null); } }
    public Color HeldColor { get { return picture != null ? picture.color : Color.white; } }

    private void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        picture = GetComponent<SpriteRenderer>();
        if (!usesSet) UsesLeft = uses;

        // The same item in the same room has the same id every time the room loads
        Id = gameObject.scene.name + "|" + name + "|" + transform.position.ToString("F1");
        if (Taken.Contains(Id))
        {
            // Already picked up earlier, so it isn't lying here any more
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    // Used when a copy of an item is made, so the copy has the same hits left
    public void SetUsesLeft(int amount)
    {
        UsesLeft = amount;
        usesSet = true;
    }

    // Only items that are lying in the scene are in the list (a carried item is switched off)
    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    // Counts one hit. Gives back true if that was the last one and the item has broken.
    public bool UseOnce()
    {
        if (uses <= 0) return false;   // never breaks

        UsesLeft--;
        return UsesLeft <= 0;
    }
}
