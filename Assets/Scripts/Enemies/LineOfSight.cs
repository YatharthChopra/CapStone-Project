using UnityEngine;

// Checks whether something solid (like a wall) is in the way between two spots.
// Enemies use this so they can't see the player through walls.
public static class LineOfSight
{
    // True if nothing solid is between the two points
    public static bool IsClear(Vector2 from, Vector2 to)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(from, to);
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D thing = hit.collider;
            if (thing == null || thing.isTrigger) continue;                          // see-through things (doors, hiding spots, cones)
            if (thing.GetComponentInParent<EnemyBase>() != null) continue;           // other enemies don't block sight
            if (thing.GetComponentInParent<PlayerMovement>() != null) continue;      // the player isn't a wall
            return false;                                                            // a wall (or other solid thing) is in the way
        }
        return true;
    }

    // True if the enemy can see any part of the player (head, middle or feet).
    // We check three spots so the player can't hide just by being half behind a corner.
    public static bool CanSeeBody(Vector2 from, Collider2D body)
    {
        if (body == null) return false;

        Bounds b = body.bounds;
        Vector2 middle = b.center;
        Vector2 head = new Vector2(b.center.x, b.max.y - 0.1f);
        Vector2 feet = new Vector2(b.center.x, b.min.y + 0.1f);

        return IsClear(from, middle) || IsClear(from, head) || IsClear(from, feet);
    }
}
