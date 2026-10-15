using UnityEngine;

public class BasicCharacterFlip : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody2D rb;
    bool fliped = false;
    void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if(rb.linearVelocityX > 0 && fliped)
        {
            transform.eulerAngles = new Vector3(0, 180);
            fliped = false;
        }
        else if (rb.linearVelocityX < 0 && !fliped)
        {
            transform.eulerAngles = Vector3.zero;
            fliped = true;
        }
    }
}
