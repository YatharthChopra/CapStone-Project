using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BasicCharacterFlip : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody2D rb;
    public bool fliped = false;
    private Transform visual;
    private Transform flashlight;
    void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
        visual = GetComponentInChildren<SpriteRenderer>().transform;
        flashlight = GetComponentInChildren<Light2D>().transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(rb.linearVelocityX > 0 && fliped)
        {
            
            fliped = false;
            Flipping(visual);
            Flipping(flashlight);
        }
        {
            fliped = true;
            Flipping(visual);
            Flipping(flashlight);
        }
    }

    private void Flipping(Transform ToBeFLipped)
    {
        if (fliped)
        {
            visual.localEulerAngles = new Vector3(visual.localEulerAngles.x, 0, visual.localEulerAngles.z);
            flashlight.localEulerAngles = new Vector3(flashlight.localEulerAngles.x, 0, flashlight.localEulerAngles.z);
            
        }
        else
        {
            visual.localEulerAngles = new Vector3(visual.localEulerAngles.x, 180, visual.localEulerAngles.z);
            flashlight.localEulerAngles = new Vector3(flashlight.localEulerAngles.x, 180, flashlight.localEulerAngles.z);
        }
        
    }

}
