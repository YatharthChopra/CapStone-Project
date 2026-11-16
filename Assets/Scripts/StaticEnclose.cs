using UnityEngine;
using UnityEngine.UI;

public class StaticEnclose : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Range(0.4f, 1)]
    public float Scale = 1f;
    public Image StaticImage;
    public Transform nin;
    public float adjust = 0.4f;
    public float adjust2 = 2f;
    private Vector2 transformAdjustment;
    private RectTransform rect;
    //public int brightness = 255;
    public bool debugMode = false;
    [Range(0.4f, 1)]
    public float DebugScale = 1f;
    void Start()
    {
        rect = this.GetComponent<RectTransform>();
        transformAdjustment = this.GetComponent<RectTransform>().sizeDelta;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if (debugMode)
        {
            AdjustStatic(DebugScale);
        }
       
        
    }

    public void inputScale(float detection, float MaxDetection)
    {
        if (!debugMode)
        {
            AdjustStatic(Mathf.Clamp(1 - detection / MaxDetection * 0.6f, 0.4f, 1));
        }
        
    }
    private void AdjustStatic(float newScale)
    {
        rect.sizeDelta = transformAdjustment * (newScale * adjust2 + adjust);
        float inverse = 1f - newScale;
        Debug.Log(inverse * 255);
        StaticImage.color = new Color(255, 255, 255, inverse);
        //StaticImage.transform.localScale = Vector3.one * (adjust2- Scale+adjust) * adjust3;
        Camera cam = Camera.main;
        transform.position = cam.WorldToScreenPoint(nin.position);
        //transform.

        //transform.position = nin.position * 400;
    }
}
