using UnityEngine;

public class BasicCharacterFlip : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody2D rb;
    bool fliped = false;
    private Vector3 baseRotation;
    void Start()
    {
        rb = this.GetComponentInParent<Rigidbody2D>();
        baseRotation = transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        if(rb.linearVelocityX > 0 && fliped)
        {
            transform.eulerAngles = baseRotation;
            fliped = false;
        }
        else if (rb.linearVelocityX < 0 && !fliped)
        {
            transform.eulerAngles = baseRotation + new Vector3(0, 180, 0);
            fliped = true;
        }
    }
}
