using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Goes on the lights on the walls. Every so often the light flickers,
// and while it flickers the Wandering Shadows are drawn toward it.
// (On the paper map: "Light flickers & enemy is attracted to the light".)
public class LightSource : MonoBehaviour
{
    // Every light in the scene adds itself here so enemies can find them
    public static readonly List<LightSource> All = new List<LightSource>();

    [SerializeField] private float timeBetweenFlickers = 6f;   // seconds of normal light
    [SerializeField] private float flickerLength = 2f;         // seconds of flickering
    [SerializeField] private SpriteRenderer glow;              // optional, just so we can see it flicker
    [SerializeField] private Light2D lamp;                     // optional, a real light that visibly flickers too

    // True while the light is flickering (this is what attracts enemies)
    public bool IsAttracting { get; private set; }

    private float timer;
    private Color normalColor;
    private float normalIntensity;

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Start()
    {
        // Start each light at a random point so they don't all flicker together
        timer = Random.Range(0f, timeBetweenFlickers + flickerLength);
        if (glow != null) normalColor = glow.color;
        if (lamp != null) normalIntensity = lamp.intensity;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float cycle = timeBetweenFlickers + flickerLength;
        IsAttracting = (timer % cycle) >= timeBetweenFlickers;

        // Blink quickly while flickering
        bool blinkOn = Mathf.Sin(Time.time * 30f) > 0f;

        if (glow != null)
        {
            glow.color = (IsAttracting && blinkOn) ? Color.white : normalColor;
        }

        if (lamp != null)
        {
            // While flickering the light jumps between dim and normal
            lamp.intensity = (IsAttracting && !blinkOn) ? normalIntensity * 0.15f : normalIntensity;
        }
    }
}
