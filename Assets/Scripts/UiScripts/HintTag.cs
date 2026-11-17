using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Draws the little "how to play" tags (the door one and the walk / hide / throw ones) so they all look the same:
// A tag floats a bit, and has a small pointer under it aimed at what it is about.
//
// Keys are written as text: "C", "Z"... or an arrow: "<" ">" "^" "v" (those are drawn as arrow shapes, so we do not
// depend on the font having arrow letters). The pictures (rounded boxes, arrow) are made in code, so there is
// nothing to import. Call Draw from an OnGUI that is handling the Repaint event.
public static class HintTag
{
    private const float BobHeight = 6f;            // pixels the tag floats up and down (at 1080p)
    private const float PressLength = 0.3f;        // how long a key cap stays down (seconds)
    private const float SecondsPerKey = 0.9f;      // with several keys, each one gets pressed in turn for this long

    private static readonly Color Brass = new Color(0.66f, 0.48f, 0.24f);
    private static readonly Color PillColor = new Color(0.14f, 0.09f, 0.06f);
    private static readonly Color CapTop = new Color(0.9f, 0.82f, 0.64f);
    private static readonly Color CapSide = new Color(0.46f, 0.33f, 0.18f);
    private static readonly Color InkColor = new Color(0.16f, 0.1f, 0.06f);
    private static readonly Color WordColor = new Color(0.95f, 0.87f, 0.68f);

    private static Color roomTint = Color.white;     // the room's light colour and brightness, multiplied into every colour
    private static string roomTintScene = "";        // which scene roomTint was worked out for

    // True if the last thing the player touched was a controller (so the tags show controller buttons)
    public static bool UsingController { get; private set; }

    private static int deviceCheckedFrame = -1;
    private static Texture2D roundedTexture;
    private static Texture2D triangleTexture;
    private static GUIStyle keyStyle;
    private static GUIStyle labelStyle;

    // Call this each frame from an Update. Works out whether the player is on the keyboard or a controller.
    public static void UpdateDevice()
    {
        if (Time.frameCount == deviceCheckedFrame) return;
        deviceCheckedFrame = Time.frameCount;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            UsingController = false;
        }
        else if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame && ControllerButtonDown(Gamepad.current))
        {
            UsingController = true;
        }
    }

    private static bool ControllerButtonDown(Gamepad pad)
    {
        return pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed || pad.buttonNorth.isPressed
            || pad.dpad.ReadValue().sqrMagnitude > 0.1f || pad.leftStick.ReadValue().sqrMagnitude > 0.25f;
    }

    // Draws a tag. "tip" is where the pointer should touch, in screen pixels (y goes up, like Camera.WorldToScreenPoint).
    // alpha is 0 to 1 (used for fading in and out).
    public static void Draw(Vector2 tip, string[] keys, string words, float alpha)
    {
        if (alpha <= 0.01f || keys == null || keys.Length == 0) return;

        MakeTextures();
        WorkOutRoomLook();
        float s = Mathf.Clamp(Screen.height / 1080f, 0.6f, 1.6f);   // everything scales with the screen height
        MakeStyles(s);

        // A slow, slight flicker, like the hanging lamps in the hall
        alpha *= 0.93f + 0.07f * Mathf.PerlinNoise(Time.unscaledTime * 2.5f, 0.3f);

        float capSize = 58f * s;
        float padding = 16f * s;
        float gap = 12f * s;
        float wordsGap = 16f * s;
        float wordsWidth = labelStyle.CalcSize(new GUIContent(words)).x;

        float width = padding + keys.Length * capSize + (keys.Length - 1) * gap + wordsGap + wordsWidth + padding * 1.2f;
        float height = capSize + padding * 1.2f;
        float pointer = 20f * s;

        // Floating a little up and down
        float bob = Mathf.Sin(Time.unscaledTime * 2.4f) * BobHeight * s;
        float guiTipY = Screen.height - tip.y;                              // the same spot, with y going down
        Rect pill = new Rect(tip.x - width / 2f, guiTipY - pointer * 0.65f - height - 4f * s - bob, width, height);

        // A soft shadow, then a brass edge, then the dark wood body with a thin brass line inside it (like a plaque)
        DrawRounded(new Rect(pill.x, pill.y + 5f * s, pill.width, pill.height), new Color(0f, 0f, 0f, 0.35f * alpha));
        DrawRounded(new Rect(pill.x - 3f * s, pill.y - 3f * s, pill.width + 6f * s, pill.height + 6f * s), Fade(Brass, 0.95f, alpha));
        DrawRounded(pill, Fade(PillColor, 0.96f, alpha));
        DrawRounded(new Rect(pill.x + 5f * s, pill.y + 5f * s, pill.width - 10f * s, pill.height - 10f * s), Fade(Brass, 0.4f, alpha));
        DrawRounded(new Rect(pill.x + 6.5f * s, pill.y + 6.5f * s, pill.width - 13f * s, pill.height - 13f * s), Fade(PillColor, 0.98f, alpha));

        // The pointer under the tag
        Rect pointerRect = new Rect(tip.x - pointer / 2f, pill.yMax + 1f * s, pointer, pointer * 0.65f);
        GUI.color = Fade(Brass, 0.95f, alpha);
        GUI.DrawTexture(new Rect(pointerRect.x - 3f * s, pointerRect.y - 3f * s, pointerRect.width + 6f * s, pointerRect.height + 6f * s), triangleTexture);
        GUI.color = Fade(PillColor, 0.96f, alpha);
        GUI.DrawTexture(pointerRect, triangleTexture);

        // The key caps. They sink into their base now and then, like they are being pressed (one at a time if there are several)
        float slot = Time.unscaledTime / SecondsPerKey;
        int pressedKey = (int)slot % keys.Length;
        bool pressNow = (slot % 1f) * SecondsPerKey < PressLength;

        for (int i = 0; i < keys.Length; i++)
        {
            float sink = (pressNow && i == pressedKey) ? 5f * s : 0f;
            Rect cap = new Rect(pill.x + padding + i * (capSize + gap), pill.y + (pill.height - capSize) / 2f, capSize, capSize);
            DrawKeyCap(cap, keys[i], sink, s, alpha);
        }

        // The words. GUI.color has to be white here, because it is multiplied into the text colour 
        float wordsX = pill.x + padding + keys.Length * capSize + (keys.Length - 1) * gap + wordsGap;
        Rect wordsRect = new Rect(wordsX, pill.y, wordsWidth + 4f, pill.height);

        GUI.color = Color.white;
        labelStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f * alpha);                  // a small dark shadow so it always reads
        GUI.Label(new Rect(wordsRect.x + 2f * s, wordsRect.y + 2f * s, wordsRect.width, wordsRect.height), words, labelStyle);
        labelStyle.normal.textColor = Fade(WordColor, 1f, alpha);
        GUI.Label(wordsRect, words, labelStyle);

        GUI.color = Color.white;
    }

    private static void DrawKeyCap(Rect area, string key, float sink, float s, float alpha)
    {
        DrawRounded(new Rect(area.x, area.y + 5f * s, area.width, area.height - 5f * s), Fade(CapSide, 1f, alpha));   // the base
        Rect top = new Rect(area.x, area.y + sink, area.width, area.height - 5f * s);
        DrawRounded(top, Fade(CapTop, 1f, alpha));

        if (key == "<" || key == ">" || key == "^" || key == "v")
        {
            // An arrow shape. The picture points down, so turn it for the other ways.
            float size = top.width * 0.42f;
            Rect arrow = new Rect(top.center.x - size / 2f, top.center.y - size * 0.325f, size, size * 0.65f);
            float angle = key == "<" ? 90f : key == "^" ? 180f : key == ">" ? 270f : 0f;

            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, top.center);
            GUI.color = Fade(InkColor, 1f, alpha);
            GUI.DrawTexture(arrow, triangleTexture);
            GUI.matrix = old;
        }
        else
        {
            GUI.color = Color.white;   // (so the text colour below is not tinted by the key cap's colour)
            keyStyle.normal.textColor = Fade(InkColor, 1f, alpha);
            GUI.Label(top, key, keyStyle);
        }
    }

    // Every colour is multiplied by the room's light, so in a yellow room the tag turns a little yellow and in a
    // dim room it dims a little. (Never fully: the words must stay readable.)
    private static Color Fade(Color color, float strength, float alpha)
    {
        return new Color(color.r * roomTint.r, color.g * roomTint.g, color.b * roomTint.b, strength * alpha);
    }

    // Looks at the room's main (global) light once per room and works out the tint from it
    private static void WorkOutRoomLook()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == roomTintScene) return;
        roomTintScene = scene;
        roomTint = Color.white;

        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
        {
            if (light.lightType != Light2D.LightType.Global) continue;

            // Take on half of the light's colour, and a bit of how bright the room is (but never darker than 80%)
            Color lightColor = Color.Lerp(Color.white, light.color, 0.5f);
            float brightness = Mathf.Lerp(1f, Mathf.Clamp(light.intensity, 0.8f, 1f), 0.7f);
            roomTint = new Color(lightColor.r * brightness, lightColor.g * brightness, lightColor.b * brightness, 1f);
            return;
        }
    }

    private static void DrawRounded(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, roundedTexture, ScaleMode.StretchToFill, true);
    }

    private static void MakeStyles(float s)
    {
        if (keyStyle == null)
        {
            keyStyle = new GUIStyle(GUI.skin.label);
            keyStyle.alignment = TextAnchor.MiddleCenter;
            keyStyle.fontStyle = FontStyle.Bold;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.wordWrap = false;
        }

        keyStyle.fontSize = Mathf.RoundToInt(34f * s);
        labelStyle.fontSize = Mathf.RoundToInt(28f * s);
    }

    // A white rounded square and a white triangle (we tint them with GUI.color). Made once, in code.
    private static void MakeTextures()
    {
        if (roundedTexture == null)
        {
            const int size = 64;
            const float radius = 22f;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            roundedTexture.filterMode = FilterMode.Bilinear;
            roundedTexture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // How far this pixel is outside the rounded rectangle (negative = inside)
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius), 0f);
                    float outside = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    roundedTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - outside)));   // a soft one-pixel edge
                }
            }
            roundedTexture.Apply();
        }

        if (triangleTexture == null)
        {
            const int size = 64;
            triangleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            triangleTexture.filterMode = FilterMode.Bilinear;
            triangleTexture.wrapMode = TextureWrapMode.Clamp;

            // A triangle pointing down. Row 0 is the bottom of the picture, so the tip is at the bottom.
            for (int y = 0; y < size; y++)
            {
                float halfWidth = (y / (float)(size - 1)) * (size / 2f);
                for (int x = 0; x < size; x++)
                {
                    float edge = halfWidth - Mathf.Abs(x + 0.5f - size / 2f);
                    triangleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(edge + 0.5f)));
                }
            }
            triangleTexture.Apply();
        }
    }
}
