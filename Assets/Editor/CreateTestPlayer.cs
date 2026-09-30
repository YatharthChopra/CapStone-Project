using UnityEditor;
using UnityEngine;

// Editor helper: adds a placeholder player and some ground to the open scene
// so we can test movement. Run it from the top menu: Tools > Capstone > Create Test Player And Ground
public static class CreateTestPlayer
{
    [MenuItem("Tools/Capstone/Create Test Player And Ground")]
    private static void Create()
    {
        // Built-in white square sprite, good enough until the real art is in
        Sprite square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // --- Ground ---
        GameObject ground = new GameObject("Ground");
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        ground.transform.position = new Vector3(0f, -3f, 0f);
        ground.transform.localScale = new Vector3(30f, 1f, 1f);
        SpriteRenderer groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.sprite = square;
        groundSr.color = new Color(0.35f, 0.35f, 0.4f);
        ground.AddComponent<BoxCollider2D>();

        // A low ceiling so we can test that crouching under things works
        GameObject ceiling = new GameObject("Low Ceiling");
        Undo.RegisterCreatedObjectUndo(ceiling, "Create Ceiling");
        ceiling.transform.position = new Vector3(6f, -1.4f, 0f);
        ceiling.transform.localScale = new Vector3(4f, 0.5f, 1f);
        SpriteRenderer ceilingSr = ceiling.AddComponent<SpriteRenderer>();
        ceilingSr.sprite = square;
        ceilingSr.color = new Color(0.5f, 0.3f, 0.3f);
        ceiling.AddComponent<BoxCollider2D>();

        // --- Player ---
        GameObject player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        player.transform.position = new Vector3(0f, -1f, 0f);

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Collider is 1 wide, 2 tall, sitting on the player's position
        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 2f);

        // Sprite is a child so it can be squashed when crouching without squashing the collider
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localScale = new Vector3(1f, 2f, 1f);
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.9f, 0.8f, 0.3f);
        sr.sortingOrder = 1;

        PlayerMovement movement = player.AddComponent<PlayerMovement>();
        SerializedObject so = new SerializedObject(movement);
        so.FindProperty("visual").objectReferenceValue = visual.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = player;
    }
}
